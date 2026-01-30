using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        
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
    }
}
