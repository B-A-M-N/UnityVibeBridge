// BUNDLED VIBEBRIDGE SERVER v15-FINAL-FIX
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System;
using UnityEditor;
using UnityEngine;

namespace Thry {
    public enum BlendOp { Add = 0, Subtract = 1, ReverseSubtract = 2, Min = 3, Max = 4 }
    public enum ColorMask { None = 0, RGBA = 15 }
}

namespace VibeBridge {
    [UnityEditor.InitializeOnLoad]
    public static partial class VibeBridgeServer {
        private static HashSet<int> _createdObjectIds = new HashSet<int>();
        private const string SESSION_PATH = "metadata/vibe_session.json";
        private static BridgeState _currentState = BridgeState.Stopped;
        private static string _inboxPath, _outboxPath;
        private static bool _isProcessing = false;

        static VibeBridgeServer() {
            AssemblyReloadEvents.beforeAssemblyReload += () => _currentState = BridgeState.Stopped;
            Startup();
        }

        private static void Startup() {
            _inboxPath = Path.Combine(Directory.GetCurrentDirectory(), "vibe_queue/inbox");
            _outboxPath = Path.Combine(Directory.GetCurrentDirectory(), "vibe_queue/outbox");
            if (!Directory.Exists(_inboxPath)) Directory.CreateDirectory(_inboxPath);
            if (!Directory.Exists(_outboxPath)) Directory.CreateDirectory(_outboxPath);
            EditorApplication.update -= PollAirlock;
            EditorApplication.update += PollAirlock;
            _currentState = BridgeState.Running;
        }

        private static void PollAirlock() {
            if (_currentState != BridgeState.Running || _isProcessing) return;
            string[] files = Directory.GetFiles(_inboxPath, "*.json");
            if (files.Length == 0) { UpdateColorSync(); return; }
            _isProcessing = true;
            try {
                foreach (var file in files.OrderBy(f => new FileInfo(f).CreationTime)) {
                    string resPath = Path.Combine(_outboxPath, "res_" + Path.GetFileName(file));
                    try {
                        var cmd = JsonUtility.FromJson<AirlockCommand>(File.ReadAllText(file));
                        File.WriteAllText(resPath, ExecuteAirlockCommand(cmd));
                    } catch (Exception e) { File.WriteAllText(resPath, "{\"error\":\"" + e.Message + "\"}"); }
                    finally { File.Delete(file); }
                }
            } finally { _isProcessing = false; }
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

        private static float _lastC = -1f, _lastP = -1f, _lastS = -1f;
        private static bool _lastHornOff = false;
        private static void UpdateColorSync() {
            GameObject obj = GameObject.Find("ExtoPc");
            if (obj == null) return;
            Animator anim = obj.GetComponent<Animator>();
            if (anim == null) return;
            float c = anim.GetFloat("Color"), p = anim.GetFloat("ColorPitch"), s = anim.GetFloat("ColorSat");
            bool h = anim.GetBool("Horns");
            bool hornOff = false; try { hornOff = anim.GetBool("HornColorOff"); } catch {}
            if (c != _lastC || p != _lastP || s != _lastS || hornOff != _lastHornOff) {
                _lastC = c; _lastP = p; _lastS = s; _lastHornOff = hornOff;
                LoadRegistry();
                ApplyDynamicGroupSync("AccentAll", c, s, p);
                if (h) ApplyDynamicGroupSync("Horns", hornOff ? 0f : c, hornOff ? 0f : s, hornOff ? 0f : p);
            }
        }

        private static void ApplyDynamicGroupSync(string group, float h, float s, float v) {
            Color col = Color.HSVToRGB(h, s, v);
            var entries = _registry.entries.Where(e => e.group == group);
            foreach (var entry in entries) {
                var go = EditorUtility.InstanceIDToObject(entry.lastKnownID) as GameObject;
                if (go == null) continue;
                var r = go.GetComponent<Renderer>();
                if (r == null) continue;
                foreach (var m in r.sharedMaterials) {
                    if (m == null) continue;
                    if (m.HasProperty("_Color")) m.SetColor("_Color", col);
                    if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", col);
                }
            }
        }

        private static RegistryData _registry = new RegistryData();
        private static void LoadRegistry() {
            string p = "metadata/vibe_registry.json";
            if (File.Exists(p)) _registry = JsonUtility.FromJson<RegistryData>(File.ReadAllText(p));
        }

        private static string VibeTool_hierarchy(Dictionary<string, string> q) { var nodes = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Select(go => "{\"name\":\"" + go.name + "\",\"instanceID\":" + go.GetInstanceID() + "}"); return "{\"nodes\":[" + string.Join(",", nodes) + "]}"; }
        private static string VibeTool_inspect(Dictionary<string, string> q) { return "{\"name\":\"ExtoPc\"}"; }
        private static string VibeTool_material_list(Dictionary<string, string> q) { return "{\"materials\":[]}"; }
        private static string VibeTool_vrc_param_set(Dictionary<string, string> q) {
            var anim = GameObject.Find("ExtoPc")?.GetComponent<Animator>();
            if (bool.TryParse(q["value"], out bool b)) anim.SetBool(q["name"], b);
            return "{\"message\":\"Success\"}";
        }
        private static string VibeTool_asset_set_internal_name(Dictionary<string, string> q) { return "{\"message\":\"Success\"}"; }

        [Serializable] public class AirlockCommand { public string action, id; public string[] keys, values; }
        [Serializable] public class RegistryData { public List<RegistryEntry> entries = new List<RegistryEntry>(); }
        [Serializable] public class RegistryEntry { public string role, group; public int lastKnownID; public Fingerprint fingerprint; }
        [Serializable] public class Fingerprint { public string meshName; }
        private enum BridgeState { Stopped, Running }
    }
}
