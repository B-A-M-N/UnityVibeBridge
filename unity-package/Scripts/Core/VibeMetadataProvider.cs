#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityVibeBridge.Kernel.Core {
    /// <summary>
    /// Unified Metadata Provider (UMP).
    /// Authoritative source for object identity, semantic roles, and registry persistence.
    /// </summary>
    public static class VibeMetadataProvider {
        private static RegistryData _registry = new RegistryData();
        private static string RegistryPath => Path.Combine(Directory.GetCurrentDirectory(), "metadata/vibe_registry.json");

        public static void SanitizeRegistry() {
            int removed = 0;
            var toRemove = new List<RegistryEntry>();

            foreach (var entry in _registry.entries) {
                // Try UUID first
                bool found = false;
                var identities = UnityEngine.Object.FindObjectsOfType<VibeIdentity>();
                if (identities.Any(id => id.Uuid == entry.uuid)) {
                    found = true;
                } else {
                    // Try Path
                    var go = GameObject.Find(entry.path);
                    if (go != null) found = true;
                }

                if (!found) toRemove.Add(entry);
            }

            foreach (var r in toRemove) {
                _registry.entries.Remove(r);
                removed++;
            }

            if (removed > 0) {
                Debug.Log($"[VibeBridge] Registry Sanitized: Removed {removed} ghost entries.");
                Save();
            }
        }

        public static RegistryData Registry => _registry;

        public static void Initialize() {
            Load();
            
            // --- SCENE AFFINITY CHECK ---
            string currentSceneGuid = AssetDatabase.AssetPathToGUID(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
            if (!string.IsNullOrEmpty(currentSceneGuid) && _registry.sceneGuid != currentSceneGuid) {
                Debug.LogWarning($"[VibeBridge] Scene Switch Detected! Previous: {_registry.sceneGuid} New: {currentSceneGuid}. Registry preserved but affinity updated.");
                _registry.sceneGuid = currentSceneGuid;
                Save();
            }

            SanitizeRegistry();
            Debug.Log($"[VibeBridge] UMP Initialized with {_registry.entries.Count} entries.");
        }

        public static void Load() {
            if (File.Exists(RegistryPath)) {
                try {
                    _registry = JsonUtility.FromJson<RegistryData>(File.ReadAllText(RegistryPath));
                } catch (Exception e) {
                    Debug.LogError($"[VibeBridge] Failed to load registry: {e.Message}");
                }
            }
        }

        public static void Save() {
            try {
                _registry.lastUpdate = DateTime.UtcNow.ToString("o");
                string dir = Path.GetDirectoryName(RegistryPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(RegistryPath, JsonUtility.ToJson(_registry, true));
            } catch (Exception e) {
                Debug.LogError($"[VibeBridge] Failed to save registry: {e.Message}");
            }
        }

        public static GameObject ResolveRole(string role) {
            // 1. Try UUID resolution first (Industrial Standard)
            var identities = UnityEngine.Object.FindObjectsOfType<VibeIdentity>();
            foreach (var id in identities) {
                if (id.Uuid == role) return id.gameObject;
            }

            // 2. Fallback to Role Registry
            var entry = _registry.entries.FirstOrDefault(e => e.role == role || e.uuid == role);
            if (entry == null) return null;

            // 1. Try last known InstanceID (fastest, but volatile)
            GameObject go = EditorUtility.InstanceIDToObject(entry.lastKnownID) as GameObject;
            if (go != null && IsPathMatch(go, entry.path)) return go;

            // 2. Fallback to Path resolution
            go = GameObject.Find(entry.path);
            if (go != null) {
                entry.lastKnownID = go.GetInstanceID();
                return go;
            }

            return null;
        }

        public static List<RegistryEntry> GetGroup(string group) {
            return _registry.entries.Where(e => e.group == group).ToList();
        }

        public static void Register(string path, string role, string group = null, int slotIndex = -1) {
            GameObject go = GameObject.Find(path);
            if (go == null) return;

            var entry = _registry.entries.FirstOrDefault(e => e.role == role);
            if (entry == null) {
                entry = new RegistryEntry { uuid = Guid.NewGuid().ToString(), role = role };
                _registry.entries.Add(entry);
            }

            entry.path = path;
            entry.group = group;
            entry.slotIndex = slotIndex;
            entry.lastKnownID = go.GetInstanceID();
            Save();
        }

        private static bool IsPathMatch(GameObject go, string expectedPath) {
            // Simple check for now; can be expanded for robust verification
            return go.name == expectedPath.Split('/').Last();
        }
    }
}
#endif
