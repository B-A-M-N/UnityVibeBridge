#if UNITY_EDITOR
// UnityVibeBridge: The Governed Creation Kernel for Unity
// Copyright (C) 2026 B-A-M-N
//
// This software is dual-licensed under the GNU AGPLv3 and a 
// Commercial "Work-or-Pay" Maintenance Agreement.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    [Serializable] public class RegistryData { public List<RegistryEntry> entries = new List<RegistryEntry>(); }
    [Serializable] public class RegistryEntry { 
        public string uuid, role, group; 
        public int lastKnownID; 
        public Fingerprint fingerprint; 
    }
    [Serializable] public class Fingerprint { public string meshName; public int triangles, vertices; public string[] components; }

    public static partial class VibeBridgeServer {
        private static RegistryData _registry = new RegistryData();
        private const string REGISTRY_PATH = "metadata/vibe_registry.json";

        public static string VibeTool_registry_add(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            
            var smr = go.GetComponent<SkinnedMeshRenderer>();
            var mf = go.GetComponent<MeshFilter>();
            Mesh m = smr != null ? smr.sharedMesh : mf?.sharedMesh;
            if (m == null) return JsonUtility.ToJson(new BasicRes { error = "No mesh found" });

            var entry = new RegistryEntry {
                uuid = q.ContainsKey("uuid") ? q["uuid"] : Guid.NewGuid().ToString().Substring(0, 8),
                role = q["role"], group = q.ContainsKey("group") ? q["group"] : "default",
                lastKnownID = go.GetInstanceID(),
                fingerprint = new Fingerprint {
                    meshName = m.name, triangles = m.triangles.Length / 3, vertices = m.vertexCount,
                    components = go.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name).ToArray()
                }
            };

            _registry.entries.RemoveAll(e => e.uuid == entry.uuid || e.role == entry.role);
            _registry.entries.Add(entry);
            SaveRegistry();
            return JsonUtility.ToJson(new BasicRes { message = "Registered", id = go.GetInstanceID() });
        }

        public static string VibeTool_registry_list(Dictionary<string, string> q) {
            LoadRegistry();
            return JsonUtility.ToJson(_registry);
        }

        public static void LoadRegistry() {
            if (File.Exists(REGISTRY_PATH)) try { _registry = JsonUtility.FromJson<RegistryData>(File.ReadAllText(REGISTRY_PATH)); } catch { _registry = new RegistryData(); }
        }
        private static void SaveRegistry() {
            if (!Directory.Exists("metadata")) Directory.CreateDirectory("metadata");
            File.WriteAllText(REGISTRY_PATH, JsonUtility.ToJson(_registry, true));
        }
    }
}
#endif
