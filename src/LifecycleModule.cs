using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        
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
    }
}
