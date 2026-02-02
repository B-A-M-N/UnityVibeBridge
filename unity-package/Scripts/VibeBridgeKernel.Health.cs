#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityVibeBridge.Kernel {
    public static partial class VibeBridgeServer {
        private static float _lastHeartbeat = 0;
        private static double _lastEditorUpdate = 0;
        private static void UpdateHeartbeat() { 
            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - _lastEditorUpdate > 2.0 && _lastEditorUpdate > 0) {
                // If more than 2 seconds passed between updates, a Modal or Hang occurred
                _errors.Add($"[STALL] Unity Main Thread blocked for {currentTime - _lastEditorUpdate:F1}s (Modal Window?)");
            }
            _lastEditorUpdate = currentTime;

            if (Time.realtimeSinceStartup - _lastHeartbeat < 1.0f) return;
            _lastHeartbeat = Time.realtimeSinceStartup;
            
            // Background fact flush for "Ghost Watcher" (Step 12 of recovery)
            try {
                var hb = new HeartbeatRes {
                    unity_pid = System.Diagnostics.Process.GetCurrentProcess().Id,
                    editor_responsive = true,
                    domain_reload_in_progress = EditorApplication.isUpdating,
                    compilation_in_progress = EditorApplication.isCompiling,
                    script_error_count = _errors.Count
                };
                
                // MODAL DETECTION: If we are here, we are alive, but check if we were just stuck
                File.WriteAllText("metadata/vibe_health.json", JsonUtility.ToJson(hb));
            } catch { }
        }

        [VibeTool("engine/heartbeat", "Returns the current editor responsiveness and error count.")]
        public static string VibeTool_engine_heartbeat(Dictionary<string, string> q) {
            return JsonUtility.ToJson(new HeartbeatRes {
                unity_pid = System.Diagnostics.Process.GetCurrentProcess().Id,
                editor_responsive = true,
                domain_reload_in_progress = EditorApplication.isUpdating,
                compilation_in_progress = EditorApplication.isCompiling,
                last_compile_success = !EditorApplication.isCompiling && _errors.Count == 0,
                script_error_count = _errors.Count
            });
        }

        [VibeTool("engine/execution_mode", "Returns whether Unity is in Play Mode or Edit Mode.")]
        public static string VibeTool_engine_execution_mode(Dictionary<string, string> q) {
            return JsonUtility.ToJson(new ExecutionModeRes {
                mode = EditorApplication.isPlaying ? "PlayMode" : "EditMode",
                entering_playmode = EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying,
                exiting_playmode = !EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying,
                time_scale = Time.timeScale
            });
        }
    }
}
#endif