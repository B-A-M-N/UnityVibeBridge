using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        
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
    }
}
