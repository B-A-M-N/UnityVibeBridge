#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        public static void LogMutation(string capability, string action, string details) {
            if (!Directory.Exists("logs")) Directory.CreateDirectory("logs");
            var entry = new AuditEntry {
                prevHash = _lastAuditHash,
                timestamp = DateTime.UtcNow.ToString("o"),
                capability = capability,
                action = action,
                details = details
            };
            string jsonWithoutHash = JsonUtility.ToJson(entry);
            _lastAuditHash = ComputeHash(jsonWithoutHash);
            entry.entryHash = _lastAuditHash;
            File.AppendAllText("logs/vibe_audit.jsonl", JsonUtility.ToJson(entry) + "\n");
        }

        private static string ComputeHash(string input) {
            using (var sha = System.Security.Cryptography.SHA256.Create()) {
                byte[] bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }
    }
}
#endif
