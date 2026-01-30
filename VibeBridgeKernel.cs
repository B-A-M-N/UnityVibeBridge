// UnityVibeBridge: The Governed Creation Kernel for Unity
// Copyright (C) 2026 B-A-M-N
//
// This software is dual-licensed under the GNU AGPLv3 and a 
// Commercial "Work-or-Pay" Maintenance Agreement.
//
// You may use this file under the terms of the AGPLv3, provided 
// you meet all requirements (including source disclosure).
//
// For commercial use, or to keep your modifications private, 
// you must satisfy the requirements of the Commercial Path 
// as defined in the LICENSE file at the project root.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Net;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    [Serializable] public class SessionData { public string sessionNonce; public List<int> createdObjectIds = new List<int>(); }
    [Serializable] public class AirlockCommand { public string action, id, capability; public string[] keys, values; }
    [Serializable] public class RecipeCommand { public AirlockCommand[] tools; }
    [Serializable] public class KernelSettings {
        public PortSettings ports = new PortSettings();
        [Serializable] public class PortSettings { public int control = 8085, vision = 8086; }
    }

    // --- RESPONSE WRAPPERS ---
    [Serializable] public class BasicRes { public string message, error; public int id; }
    [Serializable] public class InspectRes {
        public string name, tag, error;
        public bool active;
        public int layer;
        public Vector3 pos, rot, scale;
        public string[] components;
    }
    [Serializable] public class HierarchyRes { public ObjectNode[] objects; [Serializable] public struct ObjectNode { public string name; public int id; } }
    [Serializable] public class ToolListRes { public string[] tools; }
    [Serializable] public class ErrorRes { public string[] errors; }

    [InitializeOnLoad]
    public static partial class VibeBridgeServer {
        private enum BridgeState { Stopped, Starting, Running, Stopping }
        private static BridgeState _currentState = BridgeState.Stopped;
        private static string _inboxPath, _outboxPath;
        private static bool _isProcessing = false;
        private static string _persistentNonce = null;
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
            Debug.Log("[VibeBridge] Kernel Active.");
        }

        public static void Reinitialize() {
            Teardown();
            Startup();
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
            if (_currentState != BridgeState.Running || _isProcessing) return;
            HandleHttpRequests();
            UpdateColorSync(); // Restored background engine
            
            string[] pending = Directory.GetFiles(_inboxPath, "*.json");
            if (pending.Length == 0) return;
            
            _isProcessing = true;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try { 
                AssetDatabase.StartAssetEditing();
                // Process as many as possible within 5ms
                foreach (var f in pending) {
                    ProcessAirlockFile(f); 
                    if (stopwatch.ElapsedMilliseconds > 5) break; 
                }
            } finally { 
                AssetDatabase.StopAssetEditing();
                _isProcessing = false; 
            }
        }

        private static void ProcessAirlockFile(string f) {
            string responsePath = Path.Combine(_outboxPath, "res_" + Path.GetFileName(f));
            try {
                var command = JsonUtility.FromJson<AirlockCommand>(File.ReadAllText(f));
                string result = ExecuteAirlockCommand(command);
                File.WriteAllText(responsePath, result);
            } catch (Exception e) { File.WriteAllText(responsePath, "{\"error\":\"" + e.Message + "\"}"); }
            finally { 
                try { File.Delete(f); } catch {} 
            }
        }

        public static string ExecuteAirlockCommand(AirlockCommand cmd) {
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
                return (string)method.Invoke(null, new object[] { query });
            } catch (Exception e) { return JsonUtility.ToJson(new BasicRes { error = e.Message }); }
        }

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
                } else {
                    parsedVal = Convert.ChangeType(val, targetType);
                }

                if (field != null) field.SetValue(c, parsedVal);
                else prop.SetValue(c, parsedVal);
                
                return JsonUtility.ToJson(new BasicRes { message = "Value updated" });
            } catch (Exception e) { return JsonUtility.ToJson(new BasicRes { error = e.Message }); }
        }

        public static string VibeTool_status(Dictionary<string, string> q) { return "{\"status\":\"connected\",\"kernel\":\"v1.1\"}"; }
        public static string VibeTool_system_list_tools(Dictionary<string, string> q) {
            var tools = typeof(VibeBridgeServer).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(m => m.Name.StartsWith("VibeTool_")).Select(m => m.Name.Substring(9).Replace("_", "/")).ToArray();
            return JsonUtility.ToJson(new ToolListRes { tools = tools });
        }

        public static string VibeTool_system_execute_recipe(Dictionary<string, string> q) {
            // Recipies arrive as a JSON encoded string in the 'data' key for Airlock compatibility
            if (!q.ContainsKey("data")) return JsonUtility.ToJson(new BasicRes { error = "No recipe data" });
            
            var recipe = JsonUtility.FromJson<RecipeCommand>(q["data"]);
            if (recipe == null || recipe.tools == null) return JsonUtility.ToJson(new BasicRes { error = "Malformed recipe" });

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Recipe: " + (q.ContainsKey("name") ? q["name"] : "Unnamed"));
            int group = Undo.GetCurrentGroup();

            var results = new List<string>();
            try {
                foreach (var cmd in recipe.tools) results.Add(ExecuteAirlockCommand(cmd));
                Undo.CollapseUndoOperations(group);
                return "{\"message\":\"Recipe executed\",\"results\":[" + string.Join(",", results) + "]}";
            } catch (Exception e) {
                Undo.RevertAllDownToGroup(group);
                return JsonUtility.ToJson(new BasicRes { error = "Recipe aborted: " + e.Message });
            }
        }

        public static string VibeTool_inspect(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            return JsonUtility.ToJson(new InspectRes {
                name = go.name, active = go.activeSelf, tag = go.tag, layer = go.layer,
                pos = go.transform.localPosition, rot = go.transform.localEulerAngles, scale = go.transform.localScale,
                components = go.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name).ToArray()
            });
        }

        public static string VibeTool_hierarchy(Dictionary<string, string> q) {
            var all = GameObject.FindObjectsOfType<GameObject>();
            return JsonUtility.ToJson(new HierarchyRes { objects = all.Select(o => new HierarchyRes.ObjectNode { name = o.name, id = o.GetInstanceID() }).ToArray() });
        }

        public static string VibeTool_object_rename(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            Undo.RecordObject(go, "Rename"); go.name = q["newName"];
            return JsonUtility.ToJson(new BasicRes { message = "Renamed" });
        }

        public static string VibeTool_object_reparent(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]), p = q.ContainsKey("newParent") ? Resolve(q["newParent"]) : null;
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Target not found" });
            Undo.SetTransformParent(go.transform, p != null ? p.transform : null, "Reparent");
            return JsonUtility.ToJson(new BasicRes { message = "Reparented" });
        }

        public static string VibeTool_object_clone(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            GameObject c = UnityEngine.Object.Instantiate(go); c.name = go.name + "_Clone";
            Undo.RegisterCreatedObjectUndo(c, "Clone");
            return JsonUtility.ToJson(new BasicRes { message = "Cloned", id = c.GetInstanceID() });
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
            
            // Mitigation: Only frame if requested OR if the user isn't currently looking at the Scene
            bool forceFrame = q.ContainsKey("frame") && q["frame"].ToLower() == "true";
            bool userBusy = SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.hasFocus;
            
            if (forceFrame || !userBusy) SceneView.FrameLastActiveSceneView();
            return JsonUtility.ToJson(new BasicRes { message = "Selected" });
        }

        public static string VibeTool_system_batch_select(Dictionary<string, string> q) {
            string[] paths = q["paths"].Split(',');
            var gos = paths.Select(p => Resolve(p.Trim())).Where(g => g != null).ToArray();
            Selection.objects = gos; 
            
            bool forceFrame = q.ContainsKey("frame") && q["frame"].ToLower() == "true";
            bool userBusy = SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.hasFocus;

            if (gos.Length > 0 && (forceFrame || !userBusy)) SceneView.FrameLastActiveSceneView();
            return JsonUtility.ToJson(new BasicRes { message = $"Selected {gos.Length} objects" });
        }

        public static string VibeTool_transaction_begin(Dictionary<string, string> q) {
            Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName(q.ContainsKey("name") ? q["name"] : "AI Op");
            return JsonUtility.ToJson(new BasicRes { message = "Started", id = Undo.GetCurrentGroup() });
        }

        public static string VibeTool_transaction_commit(Dictionary<string, string> q) {
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup()); return JsonUtility.ToJson(new BasicRes { message = "Committed" });
        }

        public static string VibeTool_transaction_abort(Dictionary<string, string> q) {
            Undo.RevertAllDownToGroup(Undo.GetCurrentGroup()); return JsonUtility.ToJson(new BasicRes { message = "Aborted" });
        }

        public static string VibeTool_telemetry_get_errors(Dictionary<string, string> q) {
            return JsonUtility.ToJson(new ErrorRes { errors = _errors.ToArray() });
        }

        public static bool IsSafeToMutate() { return !EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isUpdating; }
        
        private static void ValidateSanity(AirlockCommand cmd) {
            // Hardware Safety Railings (Prevent GPU/VRAM bombs)
            for (int i = 0; i < cmd.keys.Length; i++) {
                string k = cmd.keys[i].ToLower();
                if (k == "intensity" && float.TryParse(cmd.values[i], out float v) && v > 10000f) throw new Exception("Sanity: Light Intensity > 10k rejected.");
                if (k == "range" && float.TryParse(cmd.values[i], out float r) && r > 5000f) throw new Exception("Sanity: Light Range > 5k rejected.");
                if (k == "maxsize" && int.TryParse(cmd.values[i], out int s) && s > 8192) throw new Exception("Sanity: Texture Size > 8k rejected.");
            }
        }

        private static void LoadOrCreateSession() {
            string path = "metadata/vibe_session.json";
            if (File.Exists(path)) try { _persistentNonce = JsonUtility.FromJson<SessionData>(File.ReadAllText(path)).sessionNonce; } catch { _persistentNonce = Guid.NewGuid().ToString().Substring(0, 8); }
            else _persistentNonce = Guid.NewGuid().ToString().Substring(0, 8);
        }
        private static void InitializeTelemetry() { Application.logMessageReceived += (msg, stack, type) => { if (type == LogType.Error || type == LogType.Exception) { _errors.Add(msg); if (_errors.Count > 50) _errors.RemoveAt(0); } }; }
        private static void SetStatus(string s) { try { File.WriteAllText("metadata/vibe_status.json", "{\"state\":\"" + s + "\",\"nonce\":\"" + _persistentNonce + "\"}"); } catch {} }
        private static float _lastHeartbeat = 0;
        private static void UpdateHeartbeat() { 
            if (Time.realtimeSinceStartup - _lastHeartbeat < 1.0f) return;
            _lastHeartbeat = Time.realtimeSinceStartup;
            try { File.WriteAllText("metadata/vibe_health.json", "{\"editorState\":\"Ready\",\"errorCount\":" + _errors.Count + "}"); } catch {} 
        }
        private static GameObject Resolve(string p) { 
            if (string.IsNullOrEmpty(p)) return null;
            if (int.TryParse(p, out int id)) return EditorUtility.InstanceIDToObject(id) as GameObject; 
            
            // --- SEMANTIC RESOLUTION ---
            if (p.StartsWith("sem:")) {
                string role = p.Substring(4);
                LoadRegistry(); // Dependency on RegistryPayload logic
                var entry = _registry.entries.FirstOrDefault(e => e.role == role);
                if (entry != null) {
                    var go = EditorUtility.InstanceIDToObject(entry.lastKnownID) as GameObject;
                    if (go != null) return go;
                    // Fallback: search by mesh fingerprint if ID is lost
                    return ResolveByFingerprint(entry.fingerprint);
                }
            }

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (asset != null) return asset;
            return GameObject.Find(p); 
        }

        private static GameObject ResolveByFingerprint(Fingerprint fp) {
            if (fp == null) return null;
            var all = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var go in all) {
                var smr = go.GetComponent<SkinnedMeshRenderer>();
                var mf = go.GetComponent<MeshFilter>();
                Mesh m = smr != null ? smr.sharedMesh : mf?.sharedMesh;
                if (m != null && m.name == fp.meshName && m.vertexCount == fp.vertices) return go;
            }
            return null;
        }

        private static HttpListener _listener;
        private static readonly Queue<HttpListenerContext> _httpQueue = new Queue<HttpListenerContext>();
        private static void StartHttpServer() {
            try {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://127.0.0.1:{_settings.ports.control}/");
                _listener.Start();
                _listener.BeginGetContext(OnHttpRequest, _listener);
            } catch (Exception e) { if (!Directory.Exists("logs")) Directory.CreateDirectory("logs"); File.AppendAllText("logs/vibe_kernel.log", e.Message); }
        }
        private static void StopHttpServer() {
            if (_listener != null) {
                try {
                    _listener.Stop();
                    _listener.Close();
                } catch (Exception) { /* Ignore disposal errors */ }
                finally { _listener = null; }
            }
        }
        private static void OnHttpRequest(IAsyncResult ar) {
            try { 
                var l = (HttpListener)ar.AsyncState; 
                if (l == null || !l.IsListening) return;
                var c = l.EndGetContext(ar); 
                l.BeginGetContext(OnHttpRequest, l); 
                lock (_httpQueue) _httpQueue.Enqueue(c); 
            } catch (Exception e) { Debug.LogError($"[VibeBridge] HTTP Error: {e.Message}"); }
        }
        private static void HandleHttpRequests() {
            lock (_httpQueue) {
                if (_httpQueue.Count == 0) return;
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                try {
                    AssetDatabase.StartAssetEditing();
                    while (_httpQueue.Count > 0 && stopwatch.ElapsedMilliseconds < 5) {
                        var c = _httpQueue.Dequeue();
                        
                        // Security: Token Verification
                        string incomingToken = c.Request.Headers["X-Vibe-Token"];
                        if (string.IsNullOrEmpty(incomingToken) || incomingToken != _persistentNonce) {
                            c.Response.StatusCode = 401;
                            byte[] err = System.Text.Encoding.UTF8.GetBytes("{\"error\":\"Unauthorized: Missing or invalid token\"}");
                            c.Response.OutputStream.Write(err, 0, err.Length);
                            c.Response.Close();
                            continue;
                        }

                        string action = c.Request.Url.AbsolutePath.TrimStart('/');
                        var cmd = new AirlockCommand { 
                            action = action, 
                            keys = c.Request.QueryString.AllKeys, 
                            values = c.Request.QueryString.AllKeys.Select(k => c.Request.QueryString[k]).ToArray() 
                        };
                        string res = ExecuteAirlockCommand(cmd); 
                        byte[] buf = System.Text.Encoding.UTF8.GetBytes(res); 
                        c.Response.ContentLength64 = buf.Length; 
                        c.Response.OutputStream.Write(buf, 0, buf.Length); 
                        c.Response.Close();
                    }
                } finally {
                    AssetDatabase.StopAssetEditing();
                }
            }
        }
    }
}
#endif
