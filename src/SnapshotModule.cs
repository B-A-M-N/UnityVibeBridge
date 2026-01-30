using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        
        // --- SNAPSHOT MODULE (The Safety Net) ---
        // Handles project-level state checkpoints (simple asset database backup simulation).
        // Real full-project backup is heavy, so we focus on Scene + Registry snapshots.

        public static string VibeTool_snapshot_create(Dictionary<string, string> q) {
            string name = q.ContainsKey("name") ? q["name"] : "Snapshot_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string path = Path.Combine("metadata", "snapshots", name);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            // 1. Save Scene
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            
            // 2. Backup Registry
            if (File.Exists(REGISTRY_PATH)) {
                File.Copy(REGISTRY_PATH, Path.Combine(path, "vibe_registry.json"));
            }

            // 3. Backup Session
            if (File.Exists(SESSION_PATH)) {
                File.Copy(SESSION_PATH, Path.Combine(path, "vibe_session.json"));
            }

            return "{\"message\":\"Snapshot created\",\"path\":\"" + path + "\"}";
        }

        public static string VibeTool_snapshot_restore(Dictionary<string, string> q) {
            string name = q["name"];
            string path = Path.Combine("metadata", "snapshots", name);
            if (!Directory.Exists(path)) return "{\"error\":\"Snapshot not found\"}";

            // 1. Restore Registry
            if (File.Exists(Path.Combine(path, "vibe_registry.json"))) {
                File.Copy(Path.Combine(path, "vibe_registry.json"), REGISTRY_PATH, true);
                LoadRegistry();
            }

            // 2. Restore Session
            if (File.Exists(Path.Combine(path, "vibe_session.json"))) {
                File.Copy(Path.Combine(path, "vibe_session.json"), SESSION_PATH, true);
                LoadOrCreateSession();
            }
            
            // Note: Scene restore is complex (requires scene reload).
            // For now we just restore metadata and warn.
            
            return "{\"message\":\"Metadata restored. Please reload scene manually if needed.\"}";
        }
    }
}
