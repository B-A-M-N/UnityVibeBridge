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
            StartHttpServer();
            EditorApplication.update -= PollAirlock;
            EditorApplication.update += PollAirlock;
            _currentState = BridgeState.Running;
            Debug.Log("[VibeBridge] Modular Server v16 Running.");
        }

        private static void Teardown() {
            _currentState = BridgeState.Stopping;
            StopHttpServer();
            EditorApplication.update -= PollAirlock;
            _currentState = BridgeState.Stopped;
        }

        private static void PollAirlock() {
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
public static string VibeTool_audit_animator(Dictionary<string, string> query) {
            string path = query["path"];
            GameObject obj = null;
            if (int.TryParse(path, out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(path);
            var anim = obj?.GetComponent<Animator>();
            if (anim == null) return "{\"error\":\"No Animator found\"}";
            var controller = anim.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            if (controller == null) return "{\"error\":\"No AnimatorController asset attached\"}";
            
            var layers = controller.layers.Select(l => "{\"name\":\"" + EscapeJson(l.name) + "\",\"stateCount\":" + l.stateMachine.states.Length + "}");
            return "{\"layers\":[" + string.Join(",", layers) + "]}";
        }

        private static string EscapeJson(string s) {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
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
                if (path == "vrc/param/get") methodName = "VibeTool_vrc_param_get";
                if (path == "vrc/param/set") methodName = "VibeTool_vrc_param_set";

                var method = typeof(VibeBridgeServer).GetMethod(methodName, 
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);

                if (method == null) return "{\"error\":\"Tool not found: " + path + " (looked for " + methodName + ")\"}";

                var query = new Dictionary<string, string>();
                if (cmd.keys != null && cmd.values != null) {
                    for (int i = 0; i < Math.Min(cmd.keys.Length, cmd.values.Length); i++) {
                        query[cmd.keys[i]] = cmd.values[i];
                    }
                }
                return (string)method.Invoke(null, new object[] { query });
            } catch (Exception e) {
                return "{\"error\":\"" + e.Message.Replace("\"", "\\\"") + "\"}";
            }
        }

        public static string VibeTool_status(Dictionary<string, string> query) {
            return "{\"status\":\"connected\",\"mode\":\"modular-v16-router-robust\"}";
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

