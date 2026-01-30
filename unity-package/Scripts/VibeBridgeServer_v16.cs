// BUNDLED VIBEBRIDGE MODULAR SERVER
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
public enum EditorCapability { None, Read, MutateScene, MutateAsset, Structural, Admin }

[Serializable] public class RegistryData { public List<RegistryEntry> entries = new List<RegistryEntry>(); }
    [Serializable] public class RegistryEntry { 
        public string uuid, role, group; 
        public int lastKnownID; 
        public int slotIndex = -1; // -1 means all slots
        public Fingerprint fingerprint; 
    }
    [Serializable] public class Fingerprint { public string meshName; public int triangles, vertices; public string[] shaders, components; }
    [Serializable] public class SessionData { public string sessionNonce; public List<int> createdObjectIds = new List<int>(); }
    [Serializable] public class AirlockCommand { public string action, id, capability; public string[] keys, values; }

    [Serializable]
    public class MaterialSnapshot {
        public string avatarName;
        public List<RendererSnapshot> renderers = new List<RendererSnapshot>();
    }

    [Serializable]
    public class RendererSnapshot {
        public string path;
        public List<string> materialGuids = new List<string>();
    }

    [UnityEditor.InitializeOnLoad]
    public static partial class VibeBridgeServer {

// --- FROM SecurityModule.cs ---
private static HashSet<int> _createdObjectIds = new HashSet<int>();
        private static string _persistentNonce = null;
        private const string SESSION_PATH = "metadata/vibe_session.json";

        public static void LoadOrCreateSession() {
            if (File.Exists(SESSION_PATH)) {
                try {
                    string json = File.ReadAllText(SESSION_PATH);
                    var data = JsonUtility.FromJson<SessionData>(json);
                    _persistentNonce = data.sessionNonce;
                    _createdObjectIds = new HashSet<int>(data.createdObjectIds);
                } catch { CreateNewSession(); }
            } else { CreateNewSession(); }
        }

        private static void CreateNewSession() {
            _persistentNonce = Guid.NewGuid().ToString().Substring(0, 8);
            _createdObjectIds = new HashSet<int>();
            SaveSession();
        }

        public static void SaveSession() {
            if (!Directory.Exists("metadata")) Directory.CreateDirectory("metadata");
            var data = new SessionData {
                sessionNonce = _persistentNonce,
                createdObjectIds = new List<int>(_createdObjectIds)
            };
            File.WriteAllText(SESSION_PATH, JsonUtility.ToJson(data, true));
        }

// --- FROM Core.cs ---
private enum BridgeState { Stopped, Starting, Running, Stopping }
        private static BridgeState _currentState = BridgeState.Stopped;
        private static string _inboxPath, _outboxPath;
        private static bool _isProcessing = false;

        static VibeBridgeServer() {
            AssemblyReloadEvents.beforeAssemblyReload += () => Teardown();
            EditorApplication.quitting += () => Teardown();
            Startup();
        }

        public static void Init() { Startup(); }
        public static void ManualInit() { Startup(); }

        private static void Startup() {
            _inboxPath = Path.Combine(Directory.GetCurrentDirectory(), "vibe_queue/inbox");
            _outboxPath = Path.Combine(Directory.GetCurrentDirectory(), "vibe_queue/outbox");
            if (!Directory.Exists(_inboxPath)) Directory.CreateDirectory(_inboxPath);
            if (!Directory.Exists(_outboxPath)) Directory.CreateDirectory(_outboxPath);
            
            LoadOrCreateSession();
            LoadRegistry();
            InitializeTelemetry();
            InitializeLifecycle(); // New Hook
            StartHttpServer();
            EditorApplication.update -= PollAirlock;
            EditorApplication.update += PollAirlock;
            _currentState = BridgeState.Running;
            Debug.Log("[VibeBridge] Modular Server v16 Running with Telemetry & Lifecycle.");
        }

        private static void Teardown() {
            _currentState = BridgeState.Stopping;
            StopHttpServer();
            EditorApplication.update -= PollAirlock;
            SetStatus("Stopped"); // Explicit signal
            _currentState = BridgeState.Stopped;
        }

        private static void PollAirlock() {
            UpdateHeartbeat();
            if (_currentState != BridgeState.Running || _isProcessing) return;
            // if (EditorApplication.isPlaying || EditorApplication.isPaused) return;

            UpdateColorSync();
            HandleHttpRequests();

            string[] pending = Directory.GetFiles(_inboxPath, "*.json");
            if (pending.Length == 0) return;

            _isProcessing = true;
            try {
                var sortedFiles = pending.Select(f => new FileInfo(f)).OrderBy(f => f.CreationTime).ToArray();
                foreach (var fileInfo in sortedFiles) {
                    ProcessAirlockFile(fileInfo.FullName);
                }
            } finally { _isProcessing = false; }
        }

        private static void ProcessAirlockFile(string path) {
            string content = File.ReadAllText(path);
            string fileName = Path.GetFileName(path);
            string responsePath = Path.Combine(_outboxPath, "res_" + fileName);

            try {
                var command = JsonUtility.FromJson<AirlockCommand>(content);
                string result = ExecuteAirlockCommand(command);
                File.WriteAllText(responsePath, result);
            } catch (Exception e) {
                File.WriteAllText(responsePath, "{\"error\":\"" + e.Message + "\"}");
            } finally {
                try { File.Delete(path); } catch {}
            }
        }

// --- FROM AuditModule.cs ---
// --- AUDIT MODULE (Cognitive Upgrade) ---
        // Provides deep, single-call analysis of complex hierarchies.

        public static string VibeTool_audit_avatar(Dictionary<string, string> q) {
            GameObject root = null;
            if (int.TryParse(q["path"], out int id)) root = EditorUtility.InstanceIDToObject(id) as GameObject;
            else root = GameObject.Find(q["path"]);
            if (root == null) return "{\"error\":\"Root not found\"}";

            var report = new AvatarAuditReport {
                name = root.name,
                instanceID = root.GetInstanceID(),
                isPrefab = PrefabUtility.IsPartOfAnyPrefab(root)
            };

            var allChildren = root.GetComponentsInChildren<Transform>(true);
            report.objectCount = allChildren.Length;

            foreach (var t in allChildren) {
                GameObject go = t.gameObject;
                
                // 1. Check for Renderers
                var r = go.GetComponent<Renderer>();
                if (r != null) {
                    var rs = new RendererAudit {
                        path = GetGameObjectPath(go, root),
                        type = r.GetType().Name,
                        materialCount = r.sharedMaterials.Length,
                        materials = r.sharedMaterials.Select(m => m != null ? m.name : "null").ToArray()
                    };
                    
                    var mf = go.GetComponent<MeshFilter>();
                    var smr = go.GetComponent<SkinnedMeshRenderer>();
                    Mesh mesh = smr != null ? smr.sharedMesh : (mf != null ? mf.sharedMesh : null);
                    if (mesh != null) {
                        rs.vertexCount = mesh.vertexCount;
                        rs.meshName = mesh.name;
                    }
                    report.renderers.Add(rs);
                }

                // 2. Check for Issues
                var components = go.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++) {
                    if (components[i] == null) {
                        report.issues.Add(new IssueAudit {
                            path = GetGameObjectPath(go, root),
                            type = "MissingScript",
                            severity = "High"
                        });
                    }
                }
            }

            return JsonUtility.ToJson(report);
        }

        private static string GetGameObjectPath(GameObject obj, GameObject root) {
            if (obj == root) return ".";
            string path = obj.name;
            while (obj.transform.parent != null && obj.transform.parent.gameObject != root) {
                obj = obj.transform.parent.gameObject;
                path = obj.name + "/" + path;
            }
            return path;
        }

        [Serializable]
        public class AvatarAuditReport {
            public string name;
            public int instanceID;
            public bool isPrefab;
            public int objectCount;
            public List<RendererAudit> renderers = new List<RendererAudit>();
            public List<IssueAudit> issues = new List<IssueAudit>();
        }

        [Serializable]
        public class RendererAudit {
            public string path;
            public string type;
            public int vertexCount;
            public string meshName;
            public int materialCount;
            public string[] materials;
        }

        [Serializable]
        public class IssueAudit {
            public string path;
            public string type;
            public string severity;
        }

// --- FROM BuilderModule.cs ---
public static string VibeTool_object_active(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            if (obj == null) return "{\"error\":\"Not found\"}";
            obj.SetActive(q["active"].ToLower() == "true");
            return "{\"message\":\"Success\"}";
        }

        public static string VibeTool_object_rename(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            if (obj == null) return "{\"error\":\"Not found\"}";
            obj.name = q["newName"];
            return "{\"message\":\"Success\"}";
        }

// --- FROM ColorSyncModule.cs ---
private static float _lastC = -1f, _lastP = -1f, _lastS = -1f;
        private static float _lastC2 = -1f, _lastP2 = -1f, _lastS2 = -1f;
        private static float _lastHC = -1f, _lastHP = -1f, _lastHS = -1f;
        private static bool _lastHornOff = false;

        private static void UpdateColorSync() {
            GameObject obj = GameObject.Find("ExtoPc");
            if (obj == null) return;
            Animator anim = obj.GetComponent<Animator>();
            if (anim == null) return;

            try {
                float c = anim.GetFloat("Color");
                float p = anim.GetFloat("ColorPitch");
                float s = anim.GetFloat("ColorSat");
                
                float c2 = 0, p2 = 0, s2 = 0;
                try { c2 = anim.GetFloat("Color2"); p2 = anim.GetFloat("ColorPitch2"); s2 = anim.GetFloat("ColorSat2"); } catch {}
                
                float hc = 0, hp = 0, hs = 0;
                try { hc = anim.GetFloat("HairColor"); hp = anim.GetFloat("HairPitch"); hs = anim.GetFloat("HairSat"); } catch {}

                bool h = false;
                try { h = anim.GetBool("Horns"); } catch {}
                
                bool hornOff = false;
                try { hornOff = anim.GetBool("HornColorOff"); } catch {}

                if (c != _lastC || p != _lastP || s != _lastS || hornOff != _lastHornOff) {
                    _lastC = c; _lastP = p; _lastS = s; _lastHornOff = hornOff;
                    ApplyDynamicGroupSync("AccentAll", c, s, p);
                    if (h) {
                        if (hornOff) ApplyDynamicGroupSync("Horns", 0f, 0f, 0f);
                        else ApplyDynamicGroupSync("Horns", c, s, p);
                    }
                }
                
                if (c2 != _lastC2 || p2 != _lastP2 || s2 != _lastS2) {
                    _lastC2 = c2; _lastP2 = p2; _lastS2 = s2;
                    ApplyDynamicGroupSync("Secondary", c2, s2, p2);
                }
                
                if (hc != _lastHC || hp != _lastHP || hs != _lastHS) {
                    _lastHC = hc; _lastHP = hp; _lastHS = hs;
                    ApplyDynamicGroupSync("Hair", hc, hs, hp);
                }
            } catch {}
        }

        private static void ApplyDynamicGroupSync(string groupName, float h, float s, float v) {
            Color col = Color.HSVToRGB(h, s, v);
            if (_registry == null || _registry.entries == null) return;
            
            var entries = _registry.entries.Where(e => e.group == groupName);
            foreach (var entry in entries) {
                GameObject go = ResolveTarget(entry);
                if (go != null) {
                    var r = go.GetComponent<Renderer>();
                    if (r != null) {
                        if (entry.slotIndex >= 0 && entry.slotIndex < r.sharedMaterials.Length) {
                            var mat = r.sharedMaterials[entry.slotIndex];
                            if (mat != null) SetColorInternal(mat, col);
                        } else {
                            foreach (var mat in r.sharedMaterials) {
                                if (mat == null) continue;
                                SetColorInternal(mat, col);
                            }
                        }
                    }
                }
            }
        }

// --- FROM ExportModule.cs ---
// --- EXPORT MODULE ---
        // Handles safe data flow from Unity to external tools (Blender).

        public static string VibeTool_object_export_fbx(Dictionary<string, string> q) {
            EnforceGuard();
            
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            if (obj == null) return "{\"error\":\"Object not found\"}";

            string exportPath = q["exportPath"];
            if (string.IsNullOrEmpty(exportPath)) exportPath = "Assets/_Exported/" + obj.name + ".fbx";
            
            // Ensure directory exists
            string dir = Path.GetDirectoryName(exportPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            // 1. Validate for Blender Compatibility
            var validation = ValidateForExport(root: obj);
            if (validation.hasErrors && !q.ContainsKey("force")) {
                return "{\"error\":\"Export validation failed\",\"issues\":" + JsonUtility.ToJson(validation) + ",\"hint\":\"Use force=true to bypass\"}";
            }

            // 2. Execute Export (Requires Unity FBX Exporter package)
            try {
                var fbxExporterType = Type.GetType("UnityEditor.Formats.Fbx.Exporter.ModelExporter, Unity.Formats.Fbx.Editor");
                if (fbxExporterType == null) return "{\"error\":\"Unity FBX Exporter package missing. Please install it via Package Manager.\"}";

                var exportMethod = fbxExporterType.GetMethod("ExportObject", new Type[] { typeof(string), typeof(UnityEngine.Object) });
                if (exportMethod == null) return "{\"error\":\"FBX Exporter API mismatch. Could not find ExportObject.\"}";

                exportMethod.Invoke(null, new object[] { exportPath, obj });
                
                LogMutation("EXPORT", obj.name, "fbx_export", exportPath);
                return "{\"message\":\"Successfully exported to " + exportPath + ",\"validation\":" + JsonUtility.ToJson(validation) + "}";
            } catch (Exception e) {
                return "{\"error\":\"Export failed: " + e.Message + "}";
            }
        }

        public static string VibeTool_export_validate(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            if (obj == null) return "{\"error\":\"Object not found\"}";

            return JsonUtility.ToJson(ValidateForExport(obj));
        }

        private static ExportValidationReport ValidateForExport(GameObject root) {
            var report = new ExportValidationReport();
            
            // Check Scale
            if (Vector3.Distance(root.transform.localScale, Vector3.one) > 0.001f) {
                report.issues.Add("Root scale is not (1,1,1). This causes size mismatch in Blender.");
                report.hasErrors = true;
            }

            // Check Rotation
            if (Quaternion.Angle(root.transform.localRotation, Quaternion.identity) > 0.001f) {
                report.issues.Add("Root has non-zero rotation. FBX export may introduce -90 degree offsets.");
            }

            // Check for Missing Scripts
            var components = root.GetComponentsInChildren<Component>(true);
            foreach (var c in components) {
                if (c == null) {
                    report.issues.Add("Hierarchy contains missing scripts. Cleanup recommended before export.");
                    report.hasErrors = true;
                    break;
                }
            }

            return report;
        }

        [Serializable]
        public class ExportValidationReport {
            public bool hasErrors = false;
            public List<string> issues = new List<string>();
        }

// --- FROM ForensicModule.cs ---
// --- FORENSIC MODULE (The Black Box) ---
        // Immutable audit log of every mutation.

        private const string AUDIT_LOG_PATH = "logs/vibe_audit.jsonl";

        public static void LogMutation(string capability, string targetGuid, string action, string details) {
            if (!Directory.Exists("logs")) Directory.CreateDirectory("logs");
            
            string entry = JsonUtility.ToJson(new AuditEntry {
                timestamp = DateTime.UtcNow.ToString("o"),
                requestId = _persistentNonce, // From SecurityModule
                capability = capability,
                targetGuid = targetGuid,
                action = action,
                details = details
            });

            File.AppendAllText(AUDIT_LOG_PATH, entry + "\n");
        }

        [Serializable]
        private class AuditEntry {
            public string timestamp;
            public string requestId;
            public string capability;
            public string targetGuid;
            public string action;
            public string details;
        }

        public static string VibeTool_audit_log_event(Dictionary<string, string> q) {
            LogMutation(q.ContainsKey("cap") ? q["cap"] : "UNKNOWN", 
                        q.ContainsKey("target") ? q["target"] : "global", 
                        q["action"],
                        q["details"]);
            return "{\"message\":\"Logged\"}";
        }

// --- FROM GuardModule.cs ---
// --- GUARD MODULE (The Gatekeeper) ---
        // Prevents mutations during unstable Editor states.

        public static bool IsSafeToMutate() {
            if (EditorApplication.isCompiling) return false;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return false;
            if (EditorApplication.isUpdating) return false;
            return true;
        }

        public static string VibeTool_guard_status(Dictionary<string, string> q) {
            return "{\"isCompiling\":" + EditorApplication.isCompiling.ToString().ToLower() + 
                   ",\"isPlaying\":" + EditorApplication.isPlaying.ToString().ToLower() + 
                   ",\"isUpdating\":" + EditorApplication.isUpdating.ToString().ToLower() + 
                   ",\"safe\":" + IsSafeToMutate().ToString().ToLower() + "}";
        }

        public static string VibeTool_guard_await_compilation(Dictionary<string, string> q) {
            if (EditorApplication.isCompiling) {
                return "{\"status\":\"waiting\",\"message\":\"Editor is compiling\"}";
            }
            return "{\"status\":\"ready\"}";
        }

        private static void EnforceGuard() {
            if (!IsSafeToMutate()) {
                throw new Exception("UNSAFE_STATE: Editor is compiling, playing, or updating. Operation rejected.");
            }
        }

// --- FROM HeartbeatModule.cs ---
// --- HEARTBEAT MODULE ---
        // Pushes Unity state to a persistent file for external health monitoring.

        private const string HEALTH_PATH = "metadata/vibe_health.json";
        private static float _lastHeartbeatTime = 0f;

        public static void UpdateHeartbeat() {
            if (Time.realtimeSinceStartup - _lastHeartbeatTime < 1.0f) return;
            _lastHeartbeatTime = Time.realtimeSinceStartup;

            var report = new HealthReport {
                timestamp = DateTime.UtcNow.ToString("o"),
                editorState = GetEditorState(),
                isCompiling = EditorApplication.isCompiling,
                isPlaying = EditorApplication.isPlaying,
                isUpdating = EditorApplication.isUpdating,
                errorCount = GetRecentErrors().Count,
                sessionNonce = _persistentNonce
            };

            try {
                if (!Directory.Exists("metadata")) Directory.CreateDirectory("metadata");
                File.WriteAllText(HEALTH_PATH, JsonUtility.ToJson(report, true));
            } catch {
                // Fail silently to avoid recursion if disk is full/locked
            }
        }

        private static string GetEditorState() {
            if (EditorApplication.isCompiling) return "Compiling";
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "Playing";
            if (EditorApplication.isUpdating) return "Importing";
            return "Ready";
        }

        [Serializable]
        public class HealthReport {
            public string timestamp;
            public string editorState;
            public bool isCompiling;
            public bool isPlaying;
            public bool isUpdating;
            public int errorCount;
            public string sessionNonce;
        }

        public static string VibeTool_health_check(Dictionary<string, string> q) {
            var report = new HealthReport {
                timestamp = DateTime.UtcNow.ToString("o"),
                editorState = GetEditorState(),
                isCompiling = EditorApplication.isCompiling,
                isPlaying = EditorApplication.isPlaying,
                isUpdating = EditorApplication.isUpdating,
                errorCount = GetRecentErrors().Count,
                sessionNonce = _persistentNonce
            };
            return JsonUtility.ToJson(report);
        }

// --- FROM HttpModule.cs ---
private static HttpListener _listener;
        private static Thread _serverThread;
        private static readonly Queue<HttpListenerContext> _requestQueue = new Queue<HttpListenerContext>();
        private const string PORT = "8085";

        // Extend Startup to include HTTP
        private static void StartHttpServer() {
            if (_serverThread != null && _serverThread.IsAlive) return;
            _serverThread = new Thread(Listen);
            _serverThread.IsBackground = true;
            _serverThread.Start();
            Debug.Log("[VibeBridge] HTTP Server started on port " + PORT);
        }

        private static void StopHttpServer() {
            if (_listener != null) { 
                try { _listener.Stop(); _listener.Close(); } catch {}
            }
        }

        private static void Listen() {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://127.0.0.1:" + PORT + "/");
            _listener.Prefixes.Add("http://[::1]:" + PORT + "/");
            _listener.Start();
            while (_currentState == BridgeState.Running) {
                try { 
                    var context = _listener.GetContext(); 
                    lock (_requestQueue) { _requestQueue.Enqueue(context); } 
                } catch { break; }
            }
        }

        private static void HandleHttpRequests() {
            lock (_requestQueue) {
                while (_requestQueue.Count > 0) {
                    ProcessHttpRequest(_requestQueue.Dequeue());
                }
            }
        }

        private static void ProcessHttpRequest(HttpListenerContext context) {
            try {
                string action = context.Request.Url.AbsolutePath.TrimStart('/');
                var query = context.Request.QueryString;
                
                var cmd = new AirlockCommand {
                    action = action,
                    keys = query.AllKeys,
                    values = Array.ConvertAll(query.AllKeys, k => query[k])
                };

                string result = ExecuteAirlockCommand(cmd);
                byte[] buffer = Encoding.UTF8.GetBytes(result);
                context.Response.ContentType = "application/json";
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            } catch (Exception e) {
                context.Response.StatusCode = 500;
                byte[] buffer = Encoding.UTF8.GetBytes("{\"error\":\"" + e.Message + "\"}");
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            } finally {
                context.Response.OutputStream.Close();
            }
        }

// --- FROM LifecycleModule.cs ---
// --- LIFECYCLE MODULE ---
        // Manages Domain Reloads and explicit state signaling.

        private const string STATUS_PATH = "metadata/vibe_status.json";

        static void InitializeLifecycle() {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            
            // If we just loaded and we are here, we are Ready.
            // Unless we are compiling (which InitOnLoad might trigger).
            if (!EditorApplication.isCompiling) {
                SetStatus("Ready");
            }
        }

        private static void OnBeforeAssemblyReload() {
            Debug.Log("[VibeBridge] Domain Reload Imminent. Closing Bridge.");
            SetStatus("Reloading");
            Teardown(); // Clean shutdown of HTTP server
        }

        private static void OnAfterAssemblyReload() {
            Debug.Log("[VibeBridge] Domain Reload Complete. Restoring Bridge.");
            // Startup is called by static constructor/InitializeOnLoad, 
            // but we can enforce status here.
            SetStatus("Ready");
        }

        public static void SetStatus(string state) {
            var status = new StatusReport {
                state = state,
                timestamp = DateTime.UtcNow.ToString("o"),
                sessionNonce = _persistentNonce,
                pid = System.Diagnostics.Process.GetCurrentProcess().Id
            };
            
            try {
                if (!Directory.Exists("metadata")) Directory.CreateDirectory("metadata");
                File.WriteAllText(STATUS_PATH, JsonUtility.ToJson(status, true));
            } catch {}
        }

        [Serializable]
        public class StatusReport {
            public string state; // "Ready", "Reloading", "Compiling", "Error"
            public string timestamp;
            public string sessionNonce;
            public int pid;
        }

// --- FROM MaterialModule.cs ---
public static string VibeTool_material_list(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            var r = obj?.GetComponent<Renderer>();
            if (r == null) return "{\"error\":\"No renderer\"}";
            return "{\"materials\":[" + string.Join(",", r.sharedMaterials.Select((m, i) => "{\"index\":" + i + ",\"name\":\"" + (m != null ? m.name : "null") + "\"}")) + "]}";
        }

        public static string VibeTool_material_set_color(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            var r = obj?.GetComponent<Renderer>();
            if (r == null) return "{\"error\":\"No renderer\"}";
            int index = int.Parse(q["index"]);
            var p = q["color"].Split(',');
            Color col = new Color(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]), p.Length > 3 ? float.Parse(p[3]) : 1f);
            var m = r.sharedMaterials[index];
            Undo.RecordObject(m, "Set Color");
            SetColorInternal(m, col);
            return "{\"message\":\"Success\"}";
        }

        public static string VibeTool_material_set_slot_material(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            var r = obj?.GetComponent<Renderer>();
            int index = int.Parse(q["index"]);
            string matName = q["material"];
            if (r == null || index >= r.sharedMaterials.Length) return "{\"error\":\"Invalid target\"}";
            
            Material mat = null;
            string[] guids = AssetDatabase.FindAssets(matName + " t:Material");
            if (guids.Length > 0) mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (mat == null) return "{\"error\":\"Material not found: " + matName + "\"}";

            Undo.RecordObject(r, "Set Material Slot");
            Material[] mats = r.sharedMaterials;
            mats[index] = mat;
            r.sharedMaterials = mats;
            return "{\"message\":\"Success\"}";
        }

        public static string VibeTool_material_insert_slot(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            var r = obj?.GetComponent<Renderer>();
            if (r == null) return "{\"error\":\"No renderer found\"}";
            
            int index = int.Parse(q["index"]);
            string matName = q["material"];
            
            Material mat = null;
            string[] guids = AssetDatabase.FindAssets(matName + " t:Material");
            if (guids.Length > 0) mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (mat == null) return "{\"error\":\"Material not found: " + matName + "\"}";
            
            Material[] oldMats = r.sharedMaterials;
            if (index < 0 || index > oldMats.Length) return "{\"error\":\"Index out of range\"}";
            
            Material[] newMats = new Material[oldMats.Length + 1];
            for (int i = 0, j = 0; i < newMats.Length; i++) {
                if (i == index) {
                    newMats[i] = mat;
                } else {
                    newMats[i] = oldMats[j++];
                }
            }
            
            Undo.RecordObject(r, "Insert Material Slot");
            r.sharedMaterials = newMats;
            return "{\"message\":\"Inserted slot " + index + "\"}";
        }

        public static string VibeTool_material_remove_slot(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            
            var r = obj?.GetComponent<Renderer>();
            if (r == null) return "{\"error\":\"No renderer found\"}";
            
            int index = int.Parse(q["index"]);
            Material[] oldMats = r.sharedMaterials;
            
            if (index < 0 || index >= oldMats.Length) return "{\"error\":\"Index out of range\"}";
            
            Material[] newMats = new Material[oldMats.Length - 1];
            for (int i = 0, j = 0; i < oldMats.Length; i++) {
                if (i == index) continue;
                newMats[j++] = oldMats[i];
            }
            
            Undo.RecordObject(r, "Remove Material Slot");
            r.sharedMaterials = newMats;
            return "{\"message\":\"Removed slot " + index + "\"}";
        }

        public static string VibeTool_material_inspect_properties(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            var r = obj?.GetComponent<Renderer>();
            int index = int.Parse(q["index"]);
            if (r == null || index >= r.sharedMaterials.Length) return "{\"error\":\"Invalid target\"}";
            var m = r.sharedMaterials[index];
            if (m == null) return "{\"error\":\"Material is null\"}";
            var props = new List<string>();
            int count = ShaderUtil.GetPropertyCount(m.shader);
            for (int i = 0; i < count; i++) { props.Add("\"" + ShaderUtil.GetPropertyName(m.shader, i) + "\""); }
            return "{\"name\":\"" + m.name + "\",\"shader\":\"" + m.shader.name + "\",\"properties\":[" + string.Join(",", props) + "]}";
        }

        public static string VibeTool_material_inspect_slot(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            var r = obj?.GetComponent<Renderer>();
            int index = int.Parse(q["index"]);
            if (r == null || index >= r.sharedMaterials.Length) return "{\"error\":\"Invalid target\"}";
            var m = r.sharedMaterials[index];
            if (m == null) return "{\"error\":\"Material is null\"}";
            return "{\"name\":\"" + m.name + "\",\"shader\":\"" + m.shader.name + "\"}";
        }

        public static string VibeTool_material_set_slot_texture(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            var r = obj?.GetComponent<Renderer>();
            int index = int.Parse(q["index"]);
            string field = q["field"];
            string texPath = q["value"];
            
            if (r == null || index >= r.sharedMaterials.Length) return "{\"error\":\"Invalid target\"}";
            var m = r.sharedMaterials[index];
            if (m == null) return "{\"error\":\"Material is null\"}";
            
            Texture tex = null;
            if (!string.IsNullOrEmpty(texPath)) {
                tex = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
                if (tex == null) return "{\"error\":\"Texture not found: " + texPath + "\"}";
            }
            
            Undo.RecordObject(m, "Set Texture");
            m.SetTexture(field, tex);
            return "{\"message\":\"Success\"}";
        }

        private static void SetColorInternal(Material m, Color col) {
            if (m == null) return;
            string[] targets = { "_Color", "_BaseColor", "_MainColor", "_EmissionColor" };
            foreach (var t in targets) { if (m.HasProperty(t)) m.SetColor(t, col); }
        }

        public static string VibeTool_material_clear_block(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            var r = obj?.GetComponent<Renderer>();
            if (r != null) { Undo.RecordObject(r, "Clear Block"); r.SetPropertyBlock(null); }
            return "{\"message\":\"Success\"}";
        }

        public static string VibeTool_asset_set_internal_name(Dictionary<string, string> q) {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(q["path"]);
            if (asset != null) { asset.name = q["newName"]; AssetDatabase.SaveAssets(); return "{\"message\":\"Success\"}"; }
            return "{\"error\":\"Not found\"}";
        }

// --- FROM OptimizationModule.cs ---
public static string VibeTool_texture_crush(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            if (obj == null) return "{\"error\":\"Object not found\"}";
            int size = int.Parse(q["maxSize"]);
            var textures = new HashSet<Texture2D>();
            foreach (var r in obj.GetComponentsInChildren<Renderer>(true)) {
                foreach (var mat in r.sharedMaterials) {
                    if (mat == null) continue;
                    int propCount = ShaderUtil.GetPropertyCount(mat.shader);
                    for (int i = 0; i < propCount; i++) {
                        if (ShaderUtil.GetPropertyType(mat.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv) {
                            Texture t = mat.GetTexture(ShaderUtil.GetPropertyName(mat.shader, i));
                            if (t is Texture2D t2d) textures.Add(t2d);
                        }
                    }
                }
            }
            int count = 0;
            foreach (var tex in textures) {
                string ap = AssetDatabase.GetAssetPath(tex);
                if (string.IsNullOrEmpty(ap)) continue;
                TextureImporter ti = AssetImporter.GetAtPath(ap) as TextureImporter;
                if (ti != null) { ti.maxTextureSize = size; AssetDatabase.ImportAsset(ap); count++; }
            }
            return "{\"message\":\"Crushed " + count + " textures to " + size + "\"}";
        }

        public static string VibeTool_shader_swap_quest(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            if (obj == null) return "{\"error\":\"Object not found\"}";
            Shader qs = Shader.Find("VRChat/Mobile/Toon Lit");
            int count = 0;
            foreach (var r in obj.GetComponentsInChildren<Renderer>(true)) {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) {
                    if (mats[i] != null) { mats[i].shader = qs; count++; }
                }
                r.sharedMaterials = mats;
            }
            return "{\"message\":\"Swapped " + count + " materials\"}";
        }

// --- FROM ProjectModule.cs ---
public static string VibeTool_project_missing_scripts(Dictionary<string, string> query) {
            var report = new List<string>();
            var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>(true);
            foreach (var go in allObjects) {
                var components = go.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++) {
                    if (components[i] == null) {
                        report.Add("{\"name\":\"" + go.name + "\",\"path\":\"" + GetGameObjectPath(go) + "\",\"index\":" + i + "}");
                    }
                }
            }
            return "{\"missingScripts\":[" + string.Join(",", report) + "]}";
        }

        private static string GetGameObjectPath(GameObject obj) {
            string path = "/" + obj.name;
            while (obj.transform.parent != null) {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }

        private static int _focusedAvatarId = -1;
        private static string _focusedAssetPath = "Assets";

        public static string VibeTool_system_focus(Dictionary<string, string> q) {
            if (q.ContainsKey("avatar")) int.TryParse(q["avatar"], out _focusedAvatarId);
            if (q.ContainsKey("assets")) _focusedAssetPath = q["assets"];
            
            return "{\"message\":\"Focus Locked\",\"avatar\":" + _focusedAvatarId + ",\"assets\":\"" + _focusedAssetPath + "\"}";
        }

// --- FROM QuestModule.cs ---
public static string VibeTool_material_snapshot(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            if (obj == null) return "{\"error\":\"Avatar root not found\"}";

            var snapshot = new MaterialSnapshot { avatarName = obj.name };
            foreach (var r in obj.GetComponentsInChildren<Renderer>(true)) {
                var rs = new RendererSnapshot { path = GetGameObjectPath(r.gameObject) };
                foreach (var m in r.sharedMaterials) {
                    if (m == null) { rs.materialGuids.Add("null"); continue; }
                    string path = AssetDatabase.GetAssetPath(m);
                    rs.materialGuids.Add(AssetDatabase.AssetPathToGUID(path));
                }
                snapshot.renderers.Add(rs);
            }

            if (!Directory.Exists("metadata/snapshots")) Directory.CreateDirectory("metadata/snapshots");
            string snapPath = "metadata/snapshots/" + obj.name + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";
            File.WriteAllText(snapPath, JsonUtility.ToJson(snapshot, true));
            return "{\"message\":\"Snapshot created\",\"path\":\"" + snapPath + "\"}";
        }

        public static string VibeTool_opt_fork(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            if (obj == null) return "{\"error\":\"Avatar root not found\"}";

            // 1. Duplicate Object
            GameObject fork = UnityEngine.Object.Instantiate(obj);
            fork.name = obj.name + "_MQ_Build";
            Undo.RegisterCreatedObjectUndo(fork, "Fork Avatar for MQ");

            // 2. Create Isolation Folder
            string folderPath = "Assets/_QuestGenerated/" + fork.name + "_" + Guid.NewGuid().ToString().Substring(0, 4);
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            // 3. Isolate Materials
            var renderers = fork.GetComponentsInChildren<Renderer>(true);
            var matMap = new Dictionary<Material, Material>();

            foreach (var r in renderers) {
                Material[] shared = r.sharedMaterials;
                for (int i = 0; i < shared.Length; i++) {
                    if (shared[i] == null) continue;
                    if (!matMap.ContainsKey(shared[i])) {
                        Material newMat = new Material(shared[i]);
                        string safeName = shared[i].name.Replace("(Instance)", "").Trim();
                        string assetPath = folderPath + "/" + safeName + ".mat";
                        AssetDatabase.CreateAsset(newMat, assetPath);
                        matMap[shared[i]] = newMat;
                    }
                    shared[i] = matMap[shared[i]];
                }
                r.sharedMaterials = shared;
            }

            AssetDatabase.SaveAssets();
            return "{\"message\":\"Fork complete\",\"instanceID\":" + fork.GetInstanceID() + "}";
        }

        [Serializable] public class MaterialSnapshot { public string avatarName; public List<RendererSnapshot> renderers = new List<RendererSnapshot>(); }
        [Serializable] public class RendererSnapshot { public string path; public List<string> materialGuids = new List<string>(); }

// --- FROM RegistryModule.cs ---
private static RegistryData _registry = new RegistryData();
        private const string REGISTRY_PATH = "metadata/vibe_registry.json";

        private static void LoadRegistry() {
            if (File.Exists(REGISTRY_PATH)) {
                try {
                    string json = File.ReadAllText(REGISTRY_PATH);
                    _registry = JsonUtility.FromJson<RegistryData>(json);
                } catch { _registry = new RegistryData(); }
            }
        }

        private static void SaveRegistry() {
            if (!Directory.Exists("metadata")) Directory.CreateDirectory("metadata");
            string json = JsonUtility.ToJson(_registry, true);
            File.WriteAllText(REGISTRY_PATH, json);
        }

        public static string VibeTool_registry_add(Dictionary<string, string> query) {
            string path = query["path"];
            string uuid = query["uuid"];
            string role = query["role"];
            string group = query["group"];

            GameObject obj = null;
            if (int.TryParse(path, out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(path);
            if (obj == null) return "{\"error\":\"Object not found\"}";

            var smr = obj.GetComponent<SkinnedMeshRenderer>();
            var mf = obj.GetComponent<MeshFilter>();
            var renderer = obj.GetComponent<Renderer>();
            var componentTypes = obj.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name).OrderBy(n => n).ToArray();

            Mesh mesh = smr != null ? smr.sharedMesh : (mf != null ? mf.sharedMesh : null);
            if (mesh == null) return "{\"error\":\"No mesh found\"}";

            var entry = new RegistryEntry {
                uuid = uuid, role = role, group = group, lastKnownID = obj.GetInstanceID(),
                fingerprint = new Fingerprint {
                    meshName = mesh.name, triangles = mesh.triangles.Length / 3, vertices = mesh.vertexCount,
                    shaders = renderer != null ? renderer.sharedMaterials.Select(m => m != null ? m.shader.name : "null").ToArray() : new string[0],
                    components = componentTypes
                }
            };

            _registry.entries.RemoveAll(e => e.uuid == uuid);
            _registry.entries.Add(entry);
            SaveRegistry();
            return "{\"message\":\"Asset registered\",\"uuid\":\"" + uuid + "\"}";
        }

        public static string VibeTool_registry_save(Dictionary<string, string> query) {
            SaveRegistry();
            return "{\"message\":\"Saved\"}";
        }

        public static string VibeTool_registry_load(Dictionary<string, string> query) {
            LoadRegistry();
            return "{\"message\":\"Loaded\"}";
        }

        private static GameObject ResolveTarget(RegistryEntry entry) {
            var obj = EditorUtility.InstanceIDToObject(entry.lastKnownID) as GameObject;
            if (obj != null && VerifyRegistryFingerprint(obj, entry.fingerprint)) return obj;

            var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var go in allObjects) {
                if (VerifyRegistryFingerprint(go, entry.fingerprint)) {
                    entry.lastKnownID = go.GetInstanceID();
                    return go;
                }
            }
            return null;
        }

        private static bool VerifyRegistryFingerprint(GameObject go, Fingerprint fp) {
            var smr = go.GetComponent<SkinnedMeshRenderer>();
            var mf = go.GetComponent<MeshFilter>();
            Mesh mesh = smr != null ? smr.sharedMesh : (mf != null ? mf.sharedMesh : null);
            if (mesh == null) return false;

            if (mesh.triangles.Length / 3 != fp.triangles || 
                mesh.vertexCount != fp.vertices || 
                mesh.name != fp.meshName) return false;

            if (fp.components != null && fp.components.Length > 0) {
                var currentComponents = go.GetComponents<Component>()
                    .Where(c => c != null)
                    .Select(c => c.GetType().Name)
                    .OrderBy(n => n)
                    .ToArray();
                
                if (currentComponents.Length != fp.components.Length) return false;
                for (int i = 0; i < currentComponents.Length; i++) {
                    if (currentComponents[i] != fp.components[i]) return false;
                }
            }
            return true;
        }

// --- FROM Router.cs ---
public static string ExecuteAirlockCommand(AirlockCommand cmd) {
            try {
                // SAFETY CHECK: Enforce Guard for non-read operations
                if (!string.IsNullOrEmpty(cmd.capability) && 
                    !cmd.capability.Equals("read", StringComparison.OrdinalIgnoreCase) && 
                    !cmd.capability.Equals("none", StringComparison.OrdinalIgnoreCase)) {
                    
                    if (!IsSafeToMutate()) {
                        return "{\"error\":\"UNSAFE_STATE: Editor is compiling, playing, or updating. Operation rejected.\"}";
                    }

                    // SANITY CHECK: Enforce hardware safety railings
                    ValidateSanity(cmd);
                }

                string path = cmd.action.TrimStart('/');
                // Default mapping: replace / and - with _
                string methodName = "VibeTool_" + path.Replace("/", "_").Replace("-", "_");
                
                // Specific manual overrides for tools that might fail the auto-map
                if (path == "asset/set-internal-name") methodName = "VibeTool_asset_set_internal_name";
                if (path == "material/inspect-properties") methodName = "VibeTool_material_inspect_properties";
                if (path == "material/inspect-slot") methodName = "VibeTool_material_inspect_slot";
                if (path == "material/clear-block") methodName = "VibeTool_material_clear_block";
                if (path == "material/set-color") methodName = "VibeTool_material_set_color";
                if (path == "material/remove-slot") methodName = "VibeTool_material_remove_slot";
                if (path == "material/insert-slot") methodName = "VibeTool_material_insert_slot";
                if (path == "material/set-slot-texture") methodName = "VibeTool_material_set_slot_texture";
                if (path == "unity/mesh-info") methodName = "VibeTool_unity_mesh_info";
                if (path == "material/snapshot") methodName = "VibeTool_material_snapshot";
                if (path == "material/restore") methodName = "VibeTool_material_restore";
                if (path == "system/focus") methodName = "VibeTool_system_focus";
                if (path == "opt/fork") methodName = "VibeTool_opt_fork";
                if (path == "opt/shader/quest") methodName = "VibeTool_shader_swap_quest";
                if (path == "opt/texture/crush") methodName = "VibeTool_texture_crush";
                if (path == "opt/mesh/simplify") methodName = "VibeTool_opt_mesh_simplify";
                if (path == "audit/avatar") methodName = "VibeTool_audit_avatar";
                if (path == "visual/point") methodName = "VibeTool_visual_point";
                if (path == "visual/line") methodName = "VibeTool_visual_line";
                if (path == "visual/clear") methodName = "VibeTool_visual_clear";
                if (path == "vrc/param/get") methodName = "VibeTool_vrc_param_get";
                if (path == "vrc/param/set") methodName = "VibeTool_vrc_param_set";
                if (path == "object/export-fbx") methodName = "VibeTool_object_export_fbx";
                if (path == "export/validate") methodName = "VibeTool_export_validate";
                if (path == "view/screenshot") methodName = "VibeTool_view_screenshot";

                var method = typeof(VibeBridgeServer).GetMethod(methodName, 
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);

                if (method == null) return "{\"error\":\"Tool not found: " + path + " (looked for " + methodName + ")\"}";

                var query = new Dictionary<string, string>();
                if (cmd.keys != null && cmd.values != null) {
                    for (int i = 0; i < Math.Min(cmd.keys.Length, cmd.values.Length); i++) {
                        query[cmd.keys[i]] = cmd.values[i];
                    }
                }
                
                // FORENSIC LOG: If this is a mutation, log it
                if (!string.IsNullOrEmpty(cmd.capability) && 
                    !cmd.capability.Equals("read", StringComparison.OrdinalIgnoreCase)) {
                    LogMutation(cmd.capability, cmd.id, cmd.action, JsonUtility.ToJson(cmd));
                }

                return (string)method.Invoke(null, new object[] { query });
            } catch (Exception e) {
                return "{\"error\":\"" + e.Message.Replace("\"", "\\\"") + "\"}";
            }
        }

        public static string VibeTool_status(Dictionary<string, string> query) {
            return "{\"status\":\"connected\",\"mode\":\"modular-v16-router-robust\"}";
        }

// --- FROM SanityModule.cs ---
// --- SANITY MODULE (Hardware Safety Railings) ---
        // Enforces hard caps on values to prevent Editor crashes, VRAM overflow, or DoS.

        private const float MAX_LIGHT_INTENSITY = 10000f;
        private const float MAX_LIGHT_RANGE = 1000f;
        private const int MAX_SPAWN_COUNT = 50; // Per single request
        private const int MAX_TEXTURE_SIZE = 4096;

        public static void ValidateSanity(AirlockCommand cmd) {
            string action = cmd.action.ToLower();

            // 1. Light Safety
            if (action.Contains("light") || action.Contains("intensity")) {
                CheckLightSanity(cmd);
            }

            // 2. Spawn Safety
            if (action.Contains("spawn") || action.Contains("instantiate")) {
                CheckSpawnSanity(cmd);
            }

            // 3. Texture Safety
            if (action.Contains("texture") || action.Contains("crush")) {
                CheckTextureSanity(cmd);
            }
        }

        private static void CheckLightSanity(AirlockCommand cmd) {
            for (int i = 0; i < cmd.keys.Length; i++) {
                if (cmd.keys[i].ToLower() == "intensity") {
                    if (float.TryParse(cmd.values[i], out float val)) {
                        if (val > MAX_LIGHT_INTENSITY) throw new Exception($"SANITY_CHECK: Intensity {val} exceeds cap ({MAX_LIGHT_INTENSITY})");
                    }
                }
                if (cmd.keys[i].ToLower() == "range") {
                    if (float.TryParse(cmd.values[i], out float val)) {
                        if (val > MAX_LIGHT_RANGE) throw new Exception($"SANITY_CHECK: Range {val} exceeds cap ({MAX_LIGHT_RANGE})");
                    }
                }
            }
        }

        private static void CheckSpawnSanity(AirlockCommand cmd) {
            // If we add a batch spawn tool, we'd check count here.
            // For single spawn, we just ensure it's not being spammed in a loop (handled by rate limiting/server).
        }

        private static void CheckTextureSanity(AirlockCommand cmd) {
            for (int i = 0; i < cmd.keys.Length; i++) {
                if (cmd.keys[i].ToLower() == "maxsize") {
                    if (int.TryParse(cmd.values[i], out int val)) {
                        if (val > MAX_TEXTURE_SIZE) throw new Exception($"SANITY_CHECK: Texture size {val} exceeds cap ({MAX_TEXTURE_SIZE})");
                    }
                }
            }
        }

// --- FROM SnapshotModule.cs ---
// --- SNAPSHOT MODULE (The Safety Net) ---
        // Handles project-level state checkpoints (simple asset database backup simulation).
        // Real full-project backup is heavy, so we focus on Scene + Registry snapshots.

        public static string VibeTool_snapshot_create(Dictionary<string, string> q) {
            string name = q.ContainsKey("name") ? q["name"] : "Snapshot_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string path = Path.Combine("metadata", "snapshots", name);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            // 1. Save Scene
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            
            // 2. Backup Registry
            if (File.Exists(REGISTRY_PATH)) {
                File.Copy(REGISTRY_PATH, Path.Combine(path, "vibe_registry.json"));
            }

            // 3. Backup Session
            if (File.Exists(SESSION_PATH)) {
                File.Copy(SESSION_PATH, Path.Combine(path, "vibe_session.json"));
            }

            return "{\"message\":\"Snapshot created\",\"path\":\"" + path + "\"}";
        }

        public static string VibeTool_snapshot_restore(Dictionary<string, string> q) {
            string name = q["name"];
            string path = Path.Combine("metadata", "snapshots", name);
            if (!Directory.Exists(path)) return "{\"error\":\"Snapshot not found\"}";

            // 1. Restore Registry
            if (File.Exists(Path.Combine(path, "vibe_registry.json"))) {
                File.Copy(Path.Combine(path, "vibe_registry.json"), REGISTRY_PATH, true);
                LoadRegistry();
            }

            // 2. Restore Session
            if (File.Exists(Path.Combine(path, "vibe_session.json"))) {
                File.Copy(Path.Combine(path, "vibe_session.json"), SESSION_PATH, true);
                LoadOrCreateSession();
            }
            
            // Note: Scene restore is complex (requires scene reload).
            // For now we just restore metadata and warn.
            
            return "{\"message\":\"Metadata restored. Please reload scene manually if needed.\"}";
        }

// --- FROM TelemetryModule.cs ---
// --- TELEMETRY MODULE ---
        // Captures Unity console logs and exceptions into a machine-readable format.

        private static List<LogEntry> _recentErrors = new List<LogEntry>();
        private const int MAX_LOGS = 50;

        public static void InitializeTelemetry() {
            Application.logMessageReceived -= HandleLog;
            Application.logMessageReceived += HandleLog;
        }

        private static void HandleLog(string logString, string stackTrace, LogType type) {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) {
                var entry = new LogEntry {
                    timestamp = DateTime.UtcNow.ToString("o"),
                    message = logString,
                    stackTrace = stackTrace,
                    type = type.ToString()
                };
                
                lock (_recentErrors) {
                    _recentErrors.Add(entry);
                    if (_recentErrors.Count > MAX_LOGS) _recentErrors.RemoveAt(0);
                }
            }
        }

        [Serializable]
        public class LogEntry {
            public string timestamp;
            public string message;
            public string stackTrace;
            public string type;
        }

        public static List<LogEntry> GetRecentErrors() {
            lock (_recentErrors) {
                return new List<LogEntry>(_recentErrors);
            }
        }
        
        public static string VibeTool_telemetry_get_errors(Dictionary<string, string> q) {
            var errors = GetRecentErrors();
            return "{\"errors\":" + JsonUtility.ToJson(new LogList { logs = errors }) + "}";
        }

        [Serializable]
        private class LogList { public List<LogEntry> logs; }

// --- FROM TransactionModule.cs ---
// --- TRANSACTION MODULE (The Atomic Operator) ---
        // Wraps operations in undo groups and handles failures.

        private static int _currentTransactionGroup = -1;

        public static void BeginTransaction(string name) {
            EnforceGuard(); // Dependency on GuardModule
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(name);
            _currentTransactionGroup = Undo.GetCurrentGroup();
        }

        public static void CommitTransaction() {
            if (_currentTransactionGroup == -1) return;
            Undo.CollapseUndoOperations(_currentTransactionGroup);
            _currentTransactionGroup = -1;
        }

        public static void RollbackTransaction() {
            if (_currentTransactionGroup != -1) {
                Undo.RevertAllDownToGroup(_currentTransactionGroup);
                _currentTransactionGroup = -1;
            }
        }

        public static string VibeTool_transaction_begin(Dictionary<string, string> q) {
            BeginTransaction(q.ContainsKey("name") ? q["name"] : "Unnamed Transaction");
            return "{\"message\":\"Transaction started\",\"groupId\":" + _currentTransactionGroup + "}";
        }

        public static string VibeTool_transaction_commit(Dictionary<string, string> q) {
            CommitTransaction();
            return "{\"message\":\"Transaction committed\"}";
        }

        public static string VibeTool_transaction_abort(Dictionary<string, string> q) {
            RollbackTransaction();
            return "{\"message\":\"Transaction rolled back\"}";
        }

// --- FROM UnityModule.cs ---
public static string VibeTool_hierarchy(Dictionary<string, string> q) {
            string rootId = q.ContainsKey("root") ? q["root"] : null;
            GameObject root = null;
            if (string.IsNullOrEmpty(rootId)) {
                var nodes = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Select(go => "{\"name\":\"" + go.name + "\",\"instanceID\":" + go.GetInstanceID() + "}");
                return "{\"nodes\":[" + string.Join(",", nodes) + "]}";
            }
            if (int.TryParse(rootId, out int id)) root = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (root == null) return "{\"error\":\"Root not found\"}";
            var children = new List<string>();
            for (int i = 0; i < root.transform.childCount; i++) {
                var child = root.transform.GetChild(i).gameObject;
                children.Add("{\"name\":\"" + child.name + "\",\"instanceID\":" + child.GetInstanceID() + "}");
            }
            return "{\"nodes\":[" + string.Join(",", children) + "]}";
        }

        public static string VibeTool_inspect(Dictionary<string, string> query) {
            string path = query["path"];
            GameObject obj = null;
            if (int.TryParse(path, out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(path);
            if (obj == null) return "{\"error\":\"Not found\"}";
            var names = obj.GetComponents<Component>().Select(c => "\"" + (c != null ? c.GetType().Name : "null") + "\"");
            return "{\"name\":\"" + obj.name + "\",\"components\":[" + string.Join(",", names) + "]}";
        }

        public static string VibeTool_undo(Dictionary<string, string> query) {
            Undo.PerformUndo();
            return "{\"message\":\"Undone\"}";
        }

        public static string VibeTool_unity_mesh_info(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            if (obj == null) return "{\"error\":\"Not found\"}";
            var smr = obj.GetComponent<SkinnedMeshRenderer>();
            var mf = obj.GetComponent<MeshFilter>();
            Mesh mesh = smr != null ? smr.sharedMesh : (mf != null ? mf.sharedMesh : null);
            if (mesh == null) return "{\"error\":\"No mesh\"}";
            return "{\"vertices\":" + mesh.vertexCount + ",\"triangles\":" + (mesh.triangles.Length/3) + "}";
        }

// --- FROM VRChatModule.cs ---
public static string VibeTool_vrc_param_set(Dictionary<string, string> q) {
            GameObject obj = GameObject.Find("ExtoPc");
            var anim = obj?.GetComponent<Animator>();
            if (anim == null) return "{\"error\":\"Animator not found\"}";
            string name = q["name"], val = q["value"];
            if (bool.TryParse(val, out bool b)) anim.SetBool(name, b);
            else if (float.TryParse(val, out float f)) anim.SetFloat(name, f);
            else if (int.TryParse(val, out int i)) anim.SetInteger(name, i);
            return "{\"message\":\"Success\"}";
        }

        public static string VibeTool_vrc_param_get(Dictionary<string, string> q) {
            GameObject obj = GameObject.Find("ExtoPc");
            var anim = obj?.GetComponent<Animator>();
            if (anim == null) return "{\"error\":\"Animator not found\"}";
            string name = q["name"];
            foreach (var p in anim.parameters) {
                if (p.name == name) {
                    if (p.type == AnimatorControllerParameterType.Bool) return "{\"value\":" + anim.GetBool(name).ToString().ToLower() + "}";
                    if (p.type == AnimatorControllerParameterType.Float) return "{\"value\":" + anim.GetFloat(name) + "}";
                    if (p.type == AnimatorControllerParameterType.Int) return "{\"value\":" + anim.GetInteger(name) + "}";
                }
            }
            return "{\"error\":\"Parameter not found\"}";
        }

// --- FROM ViewModule.cs ---
// --- VIEW MODULE ---
        // Handles visual feedback and viewport captures.

        public static string VibeTool_view_screenshot(Dictionary<string, string> q) {
            string filename = q.ContainsKey("filename") ? q["filename"] : "screenshot_latest.png";
            string path = Path.Combine("captures", filename);
            if (!Directory.Exists("captures")) Directory.CreateDirectory("captures");

            // Capture the active Scene View
            Camera cam = SceneView.lastActiveSceneView.camera;
            if (cam == null) return "{\"error\":\"No active SceneView found\"}";

            int width = q.ContainsKey("width") ? int.Parse(q["width"]) : 1280;
            int height = q.ContainsKey("height") ? int.Parse(q["height"]) : 720;

            RenderTexture rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
            cam.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            cam.targetTexture = null;
            RenderTexture.active = null;
            GameObject.DestroyImmediate(rt);

            byte[] bytes = screenShot.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            
            // ALWAYS update the monitor's latest file
            if (filename != "screenshot_latest.png") {
                File.WriteAllBytes(Path.Combine("captures", "screenshot_latest.png"), bytes);
            }

            // Log mutation for audit trail
            LogMutation("VIEW", "global", "screenshot", path);

            return "{\"message\":\"Screenshot saved\",\"path\":\"" + path + ",\"base64\":\"" + Convert.ToBase64String(bytes) + "\"}";
        }

// --- FROM VisualModule.cs ---
public static string VibeTool_visual_point(Dictionary<string, string> q) {
            Vector3 pos = ResolvePosition(q);
            GameObject pointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pointer.name = "[VIBE_POINT] " + (q.ContainsKey("label") ? q["label"] : "Attention");
            pointer.transform.position = pos;
            pointer.transform.localScale = Vector3.one * 0.2f;
            pointer.tag = "EditorOnly";
            var renderer = pointer.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Hidden/Internal-Colored"));
            renderer.material.color = Color.red;
            LogMutation("VISUAL", "global", "point", "Spawned pointer at " + pos);
            return "{\"message\":\"Pointer spawned\",\"instanceID\":" + pointer.GetInstanceID() + "}";
        }

        public static string VibeTool_visual_line(Dictionary<string, string> q) {
            Vector3 start = ResolvePosition(q, "start");
            Vector3 end = ResolvePosition(q, "end");
            GameObject lineObj = new GameObject("[VIBE_LINE] " + (q.ContainsKey("label") ? q["label"] : "Connection"));
            lineObj.transform.position = start;
            lineObj.tag = "EditorOnly";
            var lr = lineObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            lr.material = new Material(Shader.Find("Hidden/Internal-Colored"));
            lr.material.color = Color.yellow;
            LogMutation("VISUAL", "global", "line", "Line spawned");
            return "{\"message\":\"Line spawned\",\"instanceID\":" + lineObj.GetInstanceID() + "}";
        }

        public static string VibeTool_visual_clear(Dictionary<string, string> q) {
            var all = GameObject.FindObjectsOfType<GameObject>();
            int count = 0;
            foreach (var go in all) {
                if (go.name.StartsWith("[VIBE_POINT]") || go.name.StartsWith("[VIBE_LINE]")) {
                    GameObject.DestroyImmediate(go);
                    count++;
                }
            }
            return "{\"message\":\"Cleared " + count + " visual markers\"}";
        }

        private static Vector3 ResolvePosition(Dictionary<string, string> q, string prefix = "") {
            string pathKey = string.IsNullOrEmpty(prefix) ? "path" : prefix + "Path";
            string posKey = string.IsNullOrEmpty(prefix) ? "pos" : prefix + "Pos";
            if (q.ContainsKey(pathKey)) {
                GameObject target = null;
                if (int.TryParse(q[pathKey], out int id)) target = EditorUtility.InstanceIDToObject(id) as GameObject;
                else target = GameObject.Find(q[pathKey]);
                if (target != null) return target.transform.position;
            }
            if (q.ContainsKey(posKey)) {
                var p = q[posKey].Split(',');
                if (p.Length == 3) return new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
            }
            return Vector3.zero;
        }

// --- FROM WorldModule.cs ---
private static string VibeTool_world_static(Dictionary<string, string> query) {
            GameObject obj = null;
            if (int.TryParse(query["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(query["path"]);
            if (obj == null) return "{\"error\":\"Object not found\"}";

            StaticEditorFlags staticFlags = (StaticEditorFlags)Enum.Parse(typeof(StaticEditorFlags), query["flags"]);
            Undo.RecordObject(obj, "Set Static Flags");
            GameObjectUtility.SetStaticEditorFlags(obj, staticFlags);
            return "{\"message\":\"Success\",\"flags\":\"" + GameObjectUtility.GetStaticEditorFlags(obj).ToString() + "\"}";
        }

        private static string VibeTool_world_navmesh_bake(Dictionary<string, string> query) {
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            return "{\"message\":\"NavMesh Bake Triggered\"}";
        }

        private static string VibeTool_world_spawn(Dictionary<string, string> query) {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(query["asset"]);
            if (prefab == null) return "{\"error\":\"Prefab not found\"}";
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(go, "Spawn Object");
            if (query.ContainsKey("name")) go.name = query["name"];
            return "{\"message\":\"Spawned\",\"instanceID\":" + go.GetInstanceID() + "}";
        }

    }
}

