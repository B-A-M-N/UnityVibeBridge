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
            Debug.Log("[Vibe] Kernel Constructor.");
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
            if (_currentState != BridgeState.Running || _isProcessing || _isVetoed) return;
            HandleHttpRequests();
            UpdateColorSync(); 
            string[] pending = Directory.GetFiles(_inboxPath, "*.json");
            if (pending.Length == 0) return;
            _isProcessing = true;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try { 
                AssetDatabase.StartAssetEditing();
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
            } catch (Exception e) { 
                string safeMsg = e.Message.Replace("\"", "'" ).Replace("\\", "\\\\");
                File.WriteAllText(responsePath, "{\"error\":\"" + safeMsg + "\"}"); 
            } 
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
                string methodName = "VibeTool_" + path.Replace("/", "_");
                if (path == "inspect") methodName = "VibeTool_inspect";
                var method = typeof(VibeBridgeServer).GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (method == null) return JsonUtility.ToJson(new BasicRes { error = "Tool not found: " + path });
                var query = new Dictionary<string, string>();
                if (cmd.keys != null && cmd.values != null) for (int i = 0; i < Math.Min(cmd.keys.Length, cmd.values.Length); i++) query[cmd.keys[i]] = cmd.values[i];
                
                // --- FORENSIC LOGGING ---
                if (!string.IsNullOrEmpty(cmd.capability) && !cmd.capability.Equals("read", StringComparison.OrdinalIgnoreCase)) {
                    LogMutation(cmd.capability, cmd.action, JsonUtility.ToJson(cmd));
                }

                return (string)method.Invoke(null, new object[] { query });
            } catch (Exception e) { return JsonUtility.ToJson(new BasicRes { error = e.Message }); }
        }
    }
}
#endif
