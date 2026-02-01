#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Net;
using UnityEditor;
using UnityEngine;
using VibeBridge.Core; // Hardening Layer
using Cysharp.Threading.Tasks; // UniTask

namespace VibeBridge {
    [Serializable] public class SessionData { public string sessionNonce; public List<int> createdObjectIds = new List<int>(); }
    [Serializable] public class AirlockCommand { public string action, id, capability; public string[] keys, values; }
    [Serializable] public class RecipeCommand { public AirlockCommand[] tools; }
    [Serializable] public class KernelSettings {
        public PortSettings ports = new PortSettings();
        [Serializable] public class PortSettings { public int control = 8085, vision = 8086; }
    }

    [Serializable] public class BasicRes { public string message, error; public int id; }
    [Serializable] public class InspectRes {
        public string name, tag, error;
        public bool active;
        public int layer;
        public Vector3 pos, rot, scale;
        public string[] components;
        public string[] blendshapes;
    }
    [Serializable] public class HierarchyRes { public ObjectNode[] objects; [Serializable] public struct ObjectNode { public string name; public int id; } }
    [Serializable] public class ToolListRes { public string[] tools; }
    [Serializable] public class ErrorRes { public string[] errors; }
    
    [Serializable] public class AuditEntry {
        public string prevHash, timestamp, capability, action, details, entryHash;
    }

    [InitializeOnLoad]
    public static partial class VibeBridgeServer {
        private enum BridgeState { Stopped, Starting, Running, Stopping }
        private static BridgeState _currentState = BridgeState.Stopped;
        private static string _inboxPath, _outboxPath;
        private static bool _isProcessing = false;
        private static bool _isVetoed = false;
        private static string _persistentNonce = null;
        private static string _lastAuditHash = "GENESIS";
        private static List<string> _errors = new List<string>();
        private static KernelSettings _settings = new KernelSettings();

        static VibeBridgeServer() {
            LoadSettings();
            AssemblyReloadEvents.beforeAssemblyReload += () => Teardown();
            EditorApplication.quitting += () => Teardown();
            Startup();
        }

        private static void LoadSettings() {
            string path = "metadata/vibe_settings.json";
            if (File.Exists(path)) try { _settings = JsonUtility.FromJson<KernelSettings>(File.ReadAllText(path)); } catch { }
        }

        private static void Startup() {
            _inboxPath = Path.Combine(Directory.GetCurrentDirectory(), "vibe_queue/inbox");
            _outboxPath = Path.Combine(Directory.GetCurrentDirectory(), "vibe_queue/outbox");
            if (!Directory.Exists(_inboxPath)) Directory.CreateDirectory(_inboxPath);
            if (!Directory.Exists(_outboxPath)) Directory.CreateDirectory(_outboxPath);
            LoadOrCreateSession();
            InitializeTelemetry();
            StartHttpServer();
            StartVisionBroadcaster();
            EditorApplication.update -= PollAirlock;
            EditorApplication.update += PollAirlock;
            _currentState = BridgeState.Running;
            SetStatus("Ready");
        }

        private static void Teardown() {
            _currentState = BridgeState.Stopping;
            StopHttpServer();
            StopVisionBroadcaster();
            EditorApplication.update -= PollAirlock;
            SetStatus("Stopped");
            _currentState = BridgeState.Stopped;
        }

        private static void PollAirlock() {
            UpdateHeartbeat();
            if (_currentState != BridgeState.Running || _isProcessing || _isVetoed) return;
            HandleHttpRequests();
            UpdateColorSync(); 
            string[] pending = Directory.GetFiles(_inboxPath, "*.json");
            if (pending.Length == 0) return;
            
            // ASYNC DISPATCH: Fire and forget from the Update loop
            ProcessQueueAsync(pending).Forget();
        }

        /// <summary>
        /// New Async Queue Processor. 
        /// Ensures sequential execution of pending files without freezing the Editor.
        /// </summary>
        private static async UniTaskVoid ProcessQueueAsync(string[] pending) {
            if (_isProcessing) return;
            _isProcessing = true;
            try { 
                AssetDatabase.StartAssetEditing();
                foreach (var f in pending) {
                    await ProcessAirlockFileAsync(f); 
                }
            } 
            catch (Exception e) {
                Debug.LogError($"[VibeBridge] Queue Critical Failure: {e}");
            }
            finally { 
                AssetDatabase.StopAssetEditing();
                _isProcessing = false; 
            }
        }

        private static async UniTask ProcessAirlockFileAsync(string f) {
            string responsePath = Path.Combine(_outboxPath, "res_" + Path.GetFileName(f));
            try {
                // Read off main thread if possible, but File.ReadAllText is fast enough for small payloads
                var command = JsonUtility.FromJson<AirlockCommand>(File.ReadAllText(f));
                
                // --- EXECUTE ASYNC ---
                string result = await ExecuteAirlockCommandAsync(command);
                
                File.WriteAllText(responsePath, result);
            } catch (Exception e) { 
                File.WriteAllText(responsePath, "{\"error\":\"" + e.Message.Replace("\"", "'" ) + "\"}"); 
            } finally { try { File.Delete(f); } catch {} }
        }

        /// <summary>
        /// The new Async Dispatcher.
        /// Routes 'material' commands to the new hardened implementation.
        /// Routes everything else to the Legacy Reflection Dispatcher (but wrapped).
        /// </summary>
        public static async UniTask<string> ExecuteAirlockCommandAsync(AirlockCommand cmd) {
            try {
                // 1. Sanity Checks (Synchronous)
                if (!string.IsNullOrEmpty(cmd.capability) && !cmd.capability.Equals("read", StringComparison.OrdinalIgnoreCase)) {
                    if (!IsSafeToMutate()) return JsonUtility.ToJson(new BasicRes { error = "UNSAFE_STATE" });
                    ValidateSanity(cmd);
                }

                string path = cmd.action.TrimStart('/');

                // 2. Query Construction
                var query = new Dictionary<string, string>();
                if (cmd.keys != null && cmd.values != null) 
                    for (int i = 0; i < Math.Min(cmd.keys.Length, cmd.values.Length); i++) 
                        query[cmd.keys[i]] = cmd.values[i];

                // 3. ROUTING
                if (path.StartsWith("material/")) {
                    string toolName = "VibeTool_" + path.Replace("/", "_").Replace("-", "_");
                    return await ExecuteMaterialTool(toolName, query);
                }
                
                if (path.StartsWith("registry/")) {
                    string toolName = "VibeTool_" + path.Replace("/", "_").Replace("-", "_");
                    return await ExecuteRegistryTool(toolName, query);
                }

                if (path.StartsWith("vrc/")) {
                    string toolName = "VibeTool_" + path.Replace("/", "_").Replace("-", "_");
                    return await ExecuteVrcTool(toolName, query);
                }

                if (path.StartsWith("object/") || path.StartsWith("system/") || path.StartsWith("texture/") || 
                    path.StartsWith("world/") || path.StartsWith("asset/") || path.StartsWith("prefab/") || 
                    path.StartsWith("view/") || path.StartsWith("opt/")) {
                    string toolName = "VibeTool_" + path.Replace("/", "_").Replace("-", "_");
                    return await ExecuteStandardTool(toolName, query);
                }

                if (path.StartsWith("audit/") || path.StartsWith("physics/") || path.StartsWith("animation/") || 
                    path.StartsWith("physbone/") || path.StartsWith("visual/") || path.StartsWith("animator/") || 
                    path.StartsWith("export/") || path == "status" || path.StartsWith("transaction/")) {
                    string toolName = "VibeTool_" + path.Replace("/", "_").Replace("-", "_");
                    return await ExecuteStandardTool(toolName, query);
                }

                // Special case for 'inspect' which doesn't have a category prefix in some calls
                if (path == "inspect") {
                    return await ExecuteStandardTool("VibeTool_inspect", query);
                }
                
                // 4. LEGACY ROUTING (Wrapped in Task)
                // This preserves existing functionality for non-migrated tools
                string methodName = "VibeTool_" + path.Replace("/", "_").Replace("-", "_");
                if (path == "inspect") methodName = "VibeTool_inspect";

                var method = typeof(VibeBridgeServer).GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (method == null) return JsonUtility.ToJson(new BasicRes { error = "Tool not found: " + path });

                if (!string.IsNullOrEmpty(cmd.capability) && !cmd.capability.Equals("read", StringComparison.OrdinalIgnoreCase)) {
                    LogMutation(cmd.capability, cmd.action, JsonUtility.ToJson(cmd));
                }

                // Execute synchronously but await on Main Thread to respect async contract
                await AsyncUtils.SwitchToMainThreadSafe(); 
                return (string)method.Invoke(null, new object[] { query });

            } catch (Exception e) { return JsonUtility.ToJson(new BasicRes { error = e.Message }); }
        }

        // --- LEGACY SYNC WRAPPER FOR HTTP SERVER ---
        // The HTTP Server is still sync, so it must bridge clumsily or block.
        // For now, we keep it sync via .GetAwaiter().GetResult() ONLY for HTTP GETs which are usually read-only.
        // Writes (POSTs) go to the inbox loop anyway.
        public static string ExecuteAirlockCommand(AirlockCommand cmd) {
             // WARNING: calling async from sync. 
             // Since we are likely on the main thread (from PollAirlock calling HandleHttpRequests),
             // blocking here is dangerous if the task yields.
             // HOWEVER, existing HTTP handlers in `server.py` usually write to Inbox for mutations.
             // Direct HTTP execution is mostly for 'read' ops which don't yield in legacy tools.
             // For safety, we just run the legacy reflection logic here directly if it's not a hardened tool.
             
             // If it's a hardened tool (async), we CANNOT run it synchronously safely without deadlock risk.
             // We return an error instructing to use the Inbox.
             
             string path = cmd.action.TrimStart('/');
             if (path.StartsWith("material/")) {
                 return JsonUtility.ToJson(new BasicRes { error = "ASYNC_REQUIRED: Use File Inbox for Material Tools" });
             }
             
             // Fallback to old sync reflection for reads/legacy
             return ExecuteLegacySync(cmd);
        }

        private static string ExecuteLegacySync(AirlockCommand cmd) {
             try {
                if (!string.IsNullOrEmpty(cmd.capability) && !cmd.capability.Equals("read", StringComparison.OrdinalIgnoreCase)) {
                    if (!IsSafeToMutate()) return JsonUtility.ToJson(new BasicRes { error = "UNSAFE_STATE" });
                    ValidateSanity(cmd);
                }
                string path = cmd.action.TrimStart('/');
                string methodName = "VibeTool_" + path.Replace("/", "_").Replace("-", "_");
                if (path == "inspect") methodName = "VibeTool_inspect";
                
                var method = typeof(VibeBridgeServer).GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (method == null) return JsonUtility.ToJson(new BasicRes { error = "Tool not found: " + path });
                
                var query = new Dictionary<string, string>();
                if (cmd.keys != null && cmd.values != null) for (int i = 0; i < Math.Min(cmd.keys.Length, cmd.values.Length); i++) query[cmd.keys[i]] = cmd.values[i];
                
                if (!string.IsNullOrEmpty(cmd.capability) && !cmd.capability.Equals("read", StringComparison.OrdinalIgnoreCase)) {
                    LogMutation(cmd.capability, cmd.action, JsonUtility.ToJson(cmd));
                }
                return (string)method.Invoke(null, new object[] { query });
            } catch (Exception e) { return JsonUtility.ToJson(new BasicRes { error = e.Message }); }
        }

        public static string VibeTool_status(Dictionary<string, string> q) { 
            return "{\"status\":\"connected\",\"kernel\":\"v1.2.1\",\"vetoed\":" + _isVetoed.ToString().ToLower() + "}"; 
        } 
        
        public static string VibeTool_system_undo(Dictionary<string, string> q) { Undo.PerformUndo(); return JsonUtility.ToJson(new BasicRes { message = "Undo performed" }); }
        public static string VibeTool_system_redo(Dictionary<string, string> q) { Undo.PerformRedo(); return JsonUtility.ToJson(new BasicRes { message = "Redo performed" }); }

        public static string VibeTool_system_list_tools(Dictionary<string, string> q) {
            var tools = typeof(VibeBridgeServer).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.Name.StartsWith("VibeTool_"))
                .Select(m => m.Name.Substring(9).Replace("_", "/")).ToArray();
            return JsonUtility.ToJson(new ToolListRes { tools = tools });
        }

        public static string VibeTool_transaction_begin(Dictionary<string, string> q) { Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName(q.ContainsKey("name") ? q["name"] : "AI Op"); return JsonUtility.ToJson(new BasicRes { message = "Started", id = Undo.GetCurrentGroup() }); } 
        public static string VibeTool_transaction_commit(Dictionary<string, string> q) { Undo.CollapseUndoOperations(Undo.GetCurrentGroup()); return JsonUtility.ToJson(new BasicRes { message = "Committed" }); } 
        public static string VibeTool_transaction_abort(Dictionary<string, string> q) { Undo.RevertAllDownToGroup(Undo.GetCurrentGroup()); return JsonUtility.ToJson(new BasicRes { message = "Aborted" }); } 
        
        public static string VibeTool_object_set_value(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Object not found" });
            string compName = q["component"], fieldName = q["field"], val = q["value"];
            Component c = go.GetComponent(compName);
            if (c == null) return JsonUtility.ToJson(new BasicRes { error = "Component not found" });
            Undo.RecordObject(c, "Set Value");
            var type = c.GetType();
            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            var prop = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
            try {
                object parsedVal = null;
                var targetType = field?.FieldType ?? prop?.PropertyType;
                if (targetType == typeof(Vector3)) {
                    var p = val.Split(',').Select(float.Parse).ToArray();
                    parsedVal = new Vector3(p[0], p[1], p[2]);
                } else if (targetType == typeof(Color)) {
                    var p = val.Split(',').Select(float.Parse).ToArray();
                    parsedVal = new Color(p[0], p[1], p[2], p.Length > 3 ? p[3] : 1f);
                } else { parsedVal = Convert.ChangeType(val, targetType); }
                if (field != null) field.SetValue(c, parsedVal); else prop.SetValue(c, parsedVal);
                return JsonUtility.ToJson(new BasicRes { message = "Value updated" });
            } catch (Exception e) { return JsonUtility.ToJson(new BasicRes { error = e.Message }); }
        }

        public static string VibeTool_object_rename(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            Undo.RecordObject(go, "Rename"); go.name = q["newName"]; return JsonUtility.ToJson(new BasicRes { message = "Renamed" });
        }

        public static string VibeTool_object_reparent(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]), p = q.ContainsKey("newParent") ? Resolve(q["newParent"]) : null; 
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Target not found" });
            Undo.SetTransformParent(go.transform, p != null ? p.transform : null, "Reparent"); return JsonUtility.ToJson(new BasicRes { message = "Reparented" });
        }
        public static string VibeTool_object_clone(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            GameObject c = UnityEngine.Object.Instantiate(go); c.name = go.name + "_Clone"; Undo.RegisterCreatedObjectUndo(c, "Clone"); return JsonUtility.ToJson(new BasicRes { message = "Cloned", id = c.GetInstanceID() });
        }
        public static string VibeTool_object_delete(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            Undo.DestroyObjectImmediate(go); return JsonUtility.ToJson(new BasicRes { message = "Deleted" });
        }
        public static string VibeTool_system_select(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            Selection.activeGameObject = go;
            bool forceFrame = q.ContainsKey("frame") && q["frame"].ToLower() == "true";
            if (forceFrame || SceneView.lastActiveSceneView == null || !SceneView.lastActiveSceneView.hasFocus) SceneView.FrameLastActiveSceneView(); 
            return JsonUtility.ToJson(new BasicRes { message = "Selected" });
        }

        public static bool IsSafeToMutate() { return !EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isUpdating; } 
        private static void ValidateSanity(AirlockCommand cmd) {
            for (int i = 0; i < cmd.keys.Length; i++) {
                string k = cmd.keys[i].ToLower();
                if (k == "intensity" && float.TryParse(cmd.values[i], out float v) && v > 10000f) throw new Exception("Sanity: Light Intensity > 10k rejected.");
                if (k == "maxsize" && int.TryParse(cmd.values[i], out int s) && s > 8192) throw new Exception("Sanity: Texture Size > 8k rejected.");
            }
        }
        private static void LoadOrCreateSession() {
            string path = "metadata/vibe_session.json";
            if (File.Exists(path)) try { _persistentNonce = JsonUtility.FromJson<SessionData>(File.ReadAllText(path)).sessionNonce; } catch { _persistentNonce = Guid.NewGuid().ToString().Substring(0, 8); } 
            else _persistentNonce = Guid.NewGuid().ToString().Substring(0, 8);
        }

        public static void LogMutation(string capability, string action, string details) {
            if (!Directory.Exists("logs")) Directory.CreateDirectory("logs");
            var entry = new AuditEntry { prevHash = _lastAuditHash, timestamp = DateTime.UtcNow.ToString("o"), capability = capability, action = action, details = details };
            string jsonWithoutHash = JsonUtility.ToJson(entry);
            _lastAuditHash = ComputeHash(jsonWithoutHash); entry.entryHash = _lastAuditHash;
            File.AppendAllText("logs/vibe_audit.jsonl", JsonUtility.ToJson(entry) + "\n");
        }

        private static string ComputeHash(string input) {
            using (var sha = System.Security.Cryptography.SHA256.Create()) {
                byte[] bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }

        private static void InitializeTelemetry() { Application.logMessageReceived += (msg, stack, type) => { if (type == LogType.Error || type == LogType.Exception) { _errors.Add(msg); if (_errors.Count > 50) _errors.RemoveAt(0); } }; }
        private static void SetStatus(string s) { try { File.WriteAllText("metadata/vibe_status.json", "{\"state\":\"" + s + "\",\"nonce\":\"" + _persistentNonce + "\"}"); } catch {} }
    }
}
#endif