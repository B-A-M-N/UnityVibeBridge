using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        
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
    }
}