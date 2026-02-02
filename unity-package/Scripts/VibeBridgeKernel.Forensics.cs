#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityVibeBridge.Kernel {
    public static partial class VibeBridgeServer {
        [VibeTool("system/forensic_snapshot", "Bundles recent telemetry and viewport state for post-mortem analysis.", "path")]
        public static string VibeTool_system_forensic_snapshot(Dictionary<string, string> q) {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string dir = $"logs/forensics/snapshot_{timestamp}";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            try {
                // 1. Capture SITREP
                if (q.ContainsKey("path")) {
                    File.WriteAllText(Path.Combine(dir, "sitrep.json"), VibeTool_system_generate_sitrep(q));
                }

                // 2. Export Audit Tail
                if (File.Exists("logs/vibe_audit.jsonl")) {
                    File.Copy("logs/vibe_audit.jsonl", Path.Combine(dir, "audit_tail.jsonl"));
                }

                // 3. Viewport Thumbnail (Reusing existing screenshot logic)
                var ssQ = new Dictionary<string, string> { { "w", "512" }, { "h", "512" } };
                string ssJson = VibeTool_view_screenshot(ssQ);
                File.WriteAllText(Path.Combine(dir, "thumbnail.json"), ssJson);

                return JsonUtility.ToJson(new BasicRes { message = $"Forensic snapshot saved to {dir}" });
            } catch (Exception e) {
                return JsonUtility.ToJson(new BasicRes { error = "Forensic capture failed: " + e.Message });
            }
        }

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
    }
}
#endif
