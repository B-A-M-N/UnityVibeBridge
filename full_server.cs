// BUNDLED VIBEBRIDGE SERVER v15-FINAL-FIX
// Version: 15.0-final-fix
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System;
using UnityEditor;
using UnityEngine;

namespace Thry {
public enum BlendOp
    {
        Add = 0, Subtract = 1, ReverseSubtract = 2, Min = 3, Max = 4,
        LogicalClear = 5, LogicalSet = 6, LogicalCopy = 7, LogicalCopyInverted = 8,
        LogicalNoop = 9, LogicalInvert = 10, LogicalAnd = 11, LogicalNand = 12,
        LogicalOr = 13, LogicalNor = 14, LogicalXor = 15, LogicalEquivalence = 16,
        LogicalAndReverse = 17, LogicalAndInverted = 18, LogicalOrReverse = 19, LogicalOrInverted = 20
    }
    public enum ColorMask
    {
        None = 0, Alpha = 1, Blue = 2, BA = 3, Green = 4, GA = 5, GB = 6, GBA = 7,
        Red = 8, RA = 9, RB = 10, RBA = 11, RG = 12, RGA = 13, RGB = 14, RGBA = 15
    }
}

namespace VibeBridge {
    public enum EditorCapability { None, Read, MutateScene, MutateAsset, Structural, Admin }

    [UnityEditor.InitializeOnLoad]
    public static partial class VibeBridgeServer {
        private static string _activeTransactionId = null;
        private static HashSet<int> _createdObjectIds = new HashSet<int>();
        private static string _persistentNonce = null;
        private const string SESSION_PATH = "metadata/vibe_session.json";

        static VibeBridgeServer() {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorApplication.quitting -= OnQuitting;
            EditorApplication.quitting += OnQuitting;
            Init();
        }

        private static void OnBeforeAssemblyReload() { Teardown(); }
        private static void OnQuitting() { Teardown(); }

        public static void Init() { Startup(); }

        private static void Startup() {
            _queuePath = Path.Combine(Directory.GetCurrentDirectory(), "vibe_queue");
            _inboxPath = Path.Combine(_queuePath, "inbox");
            _outboxPath = Path.Combine(_queuePath, "outbox");
            if (!Directory.Exists(_inboxPath)) Directory.CreateDirectory(_inboxPath);
            if (!Directory.Exists(_outboxPath)) Directory.CreateDirectory(_outboxPath);
            LoadOrCreateSession();
            EditorApplication.update -= PollAirlock;
            EditorApplication.update += PollAirlock;
            _currentState = BridgeState.Running;
        }

        public static void Teardown() {
            EditorApplication.update -= PollAirlock;
            _currentState = BridgeState.Stopped;
        }

        public static void LoadOrCreateSession() {
            if (File.Exists(SESSION_PATH)) {
                try {
                    var data = JsonUtility.FromJson<SessionData>(File.ReadAllText(SESSION_PATH));
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
            File.WriteAllText(SESSION_PATH, JsonUtility.ToJson(new SessionData { sessionNonce = _persistentNonce, createdObjectIds = _createdObjectIds.ToList() }, true));
        }

        private static void PollAirlock() {
            if (_currentState != BridgeState.Running || _isProcessing) return;
            string[] pending = Directory.GetFiles(_inboxPath, "*.json");
            if (pending.Length == 0) return;
            _isProcessing = true;
            try {
                foreach (var file in pending.OrderBy(f => new FileInfo(f).CreationTime)) {
                    ProcessAirlockFile(file);
                }
            } finally { _isProcessing = false; }
        }

        private static void ProcessAirlockFile(string path) {
            string content = File.ReadAllText(path);
            string fileName = Path.GetFileName(path);
            string resPath = Path.Combine(_outboxPath, "res_" + fileName);
            try {
                var cmd = JsonUtility.FromJson<AirlockCommand>(content);
                string result = ExecuteAirlockCommand(cmd);
                File.WriteAllText(resPath, result);
            } catch (Exception e) { File.WriteAllText(resPath, "{\"error\":\"" + e.Message + "\"}"); }
            finally { try { File.Delete(path); } catch {} }
        }

        private static string ExecuteAirlockCommand(AirlockCommand cmd) {
            string path = cmd.action.TrimStart('/');
            string methodName = "VibeTool_" + path.Replace("/", "_").Replace("-", "_");
            if (path == "asset/set-internal-name") methodName = "VibeTool_asset_set_internal_name";
            var method = typeof(VibeBridgeServer).GetMethod(methodName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.IgnoreCase);
            if (method == null) return "{\"error\":\"Tool not found: " + path + "\"}";
            var query = new Dictionary<string, string>();
            if (cmd.keys != null && cmd.values != null) {
                for (int i = 0; i < Math.Min(cmd.keys.Length, cmd.values.Length); i++) query[cmd.keys[i]] = cmd.values[i];
            }
            return (string)method.Invoke(null, new object[] { query });
        }

        // --- COLOR SYNC ---
        private static float _lastC = -1f, _lastP = -1f, _lastS = -1f;
        private static bool _lastHornOff = false;
        private static int _lockFrames = 10;
        private static bool _syncLocked = true;

        private static void UpdateColorSync() {
            GameObject obj = GameObject.Find("ExtoPc");
            if (obj == null) return;
            Animator anim = obj.GetComponent<Animator>();
            if (anim == null) return;
            try {
                float c = anim.GetFloat("Color");
                float p = anim.GetFloat("ColorPitch");
                float s = anim.GetFloat("ColorSat");
                float c2 = anim.GetFloat("Color2");
                float p2 = anim.GetFloat("ColorPitch2");
                float s2 = anim.GetFloat("ColorSat2");
                float hc = anim.GetFloat("HairColor");
                float hp = anim.GetFloat("HairPitch");
                float hs = anim.GetFloat("HairSat");
                bool h = anim.GetBool("Horns");
                bool hornColorOff = false;
                try { hornColorOff = anim.GetBool("HornColorOff"); } catch {}

                if (_syncLocked) { if (--_lockFrames <= 0) { _syncLocked = false; LoadRegistry(); SyncAllDynamic(c, p, s, c2, p2, s2, hc, hp, hs, h && !hornColorOff); } return; }
                
                bool hornToggleChanged = (hornColorOff != _lastHornOff);
                if (c != _lastC || p != _lastP || s != _lastS || hornToggleChanged) { 
                    _lastC = c; _lastP = p; _lastS = s; _lastHornOff = hornColorOff;
                    if (hornToggleChanged) LoadRegistry();
                    ApplyDynamicGroupSync("AccentAll", c, s, p); 
                    if (h) {
                        if (hornColorOff) ApplyDynamicGroupSync("Horns", 0f, 0f, 0f);
                        else ApplyDynamicGroupSync("Horns", c, s, p);
                    }
                }
                if (c2 != _lastC2 || p2 != _lastP2 || s2 != _lastS2) { _lastC2 = c2; _lastP2 = p2; _lastS2 = s2; ApplyDynamicGroupSync("Secondary", c2, s2, p2); }
                if (hc != _lastHC || hp != _lastHP || hs != _lastHS) { _lastHC = hc; _lastHP = hp; _lastHS = hs; ApplyDynamicGroupSync("Hair", hc, hs, hp); }
            } catch {}
        }

        private static void SyncAllDynamic(float c, float p, float s, float c2, float p2, float s2, float hc, float hp, float hs, bool h) {
            ApplyDynamicGroupSync("AccentAll", c, s, p);
            ApplyDynamicGroupSync("Secondary", c2, s2, p2);
            ApplyDynamicGroupSync("Hair", hc, hs, hp);
            if (h) ApplyDynamicGroupSync("Horns", c, s, p);
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
                        foreach (var mat in r.sharedMaterials) {
                            if (mat == null) continue;
                            if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
                            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", col);
                        }
                    }
                }
            }
        }

        // --- REGISTRY ---
        private static RegistryData _registry = new RegistryData();
        private const string REGISTRY_PATH = "metadata/vibe_registry.json";
        private static void LoadRegistry() {
            if (File.Exists(REGISTRY_PATH)) {
                try { _registry = JsonUtility.FromJson<RegistryData>(File.ReadAllText(REGISTRY_PATH)); }
                catch { _registry = new RegistryData(); }
            }
        }
        private static GameObject ResolveTarget(RegistryEntry entry) {
            var obj = EditorUtility.InstanceIDToObject(entry.lastKnownID) as GameObject;
            if (obj != null && VerifyRegistryFingerprint(obj, entry.fingerprint)) return obj;
            return null;
        }
        private static bool VerifyRegistryFingerprint(GameObject go, Fingerprint fp) { return true; }

        // --- TOOLS ---
        private static string VibeTool_hierarchy(Dictionary<string, string> query) {
            string rootId = query.ContainsKey("root") ? query["root"] : null;
            GameObject root = null;
            if (string.IsNullOrEmpty(rootId)) {
                var nodes = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Select(go => "{\"name\":\"" + go.name + "\",\"instanceID\":" + go.GetInstanceID() + "}").ToArray();
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

        private static string VibeTool_inspect(Dictionary<string, string> query) {
            string path = query["path"];
            GameObject obj = null;
            if (int.TryParse(path, out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(path);
            if (obj == null) return "{\"error\":\"Not found\"}";
            var names = obj.GetComponents<Component>().Select(c => "\"" + (c != null ? c.GetType().Name : "null") + "\"");
            return "{\"name\":\"" + obj.name + "\",\"components\":[" + string.Join(",", names) + "]}";
        }

        private static string VibeTool_material_list(Dictionary<string, string> query) {
            string path = query["path"];
            GameObject obj = null;
            if (int.TryParse(path, out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(path);
            Renderer r = obj?.GetComponent<Renderer>();
            if (r == null) return "{\"error\":\"No renderer\"}";
            return "{\"materials\":[" + string.Join(",", r.sharedMaterials.Select((m, i) => "{\"index\":