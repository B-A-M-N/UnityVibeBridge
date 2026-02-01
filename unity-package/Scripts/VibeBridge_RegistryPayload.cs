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
using VibeBridge.Core;
using Cysharp.Threading.Tasks;
using MemoryPack;

namespace VibeBridge {
    [MemoryPackable] [Serializable] public partial class RegistryData { public List<RegistryEntry> entries = new List<RegistryEntry>(); }
    [MemoryPackable] [Serializable] public partial class RegistryEntry { 
        public string uuid, role, group; 
        public int lastKnownID, slotIndex; 
        public Fingerprint fingerprint; 
    }
    [MemoryPackable] [Serializable] public partial class Fingerprint { public string meshName; public int triangles, vertices; public string[] components; }

    public static partial class VibeBridgeServer {
        private static RegistryData _registry = new RegistryData();
        private const string REGISTRY_PATH = "metadata/vibe_registry.json";

        public static async UniTask<string> ExecuteRegistryTool(string toolName, Dictionary<string, string> q) {
            if (!Enum.TryParse(toolName.Replace("VibeTool_registry_", ""), true, out ToolID toolID)) {
                return JsonUtility.ToJson(new BasicRes { error = $"Unknown registry tool: {toolName}" });
            }

            try {
                await AsyncUtils.SwitchToMainThreadSafe();
            } catch (Exception e) {
                return JsonUtility.ToJson(new BasicRes { error = $"Concurrency Failure: {e.Message}" });
            }

            return toolName switch {
                "VibeTool_registry_add" => VibeTool_registry_add(q),
                "VibeTool_registry_list" => VibeTool_registry_list(q),
                _ => JsonUtility.ToJson(new BasicRes { error = "Tool not mapped." })
            };
        }

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
                slotIndex = q.ContainsKey("slotIndex") ? int.Parse(q["slotIndex"]) : 0,
                fingerprint = new Fingerprint {
                    meshName = m.name, triangles = m.triangles.Length / 3, vertices = m.vertexCount,
                    components = go.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name).ToArray()
                }
            };

            _registry.entries.RemoveAll(e => e.uuid == entry.uuid || e.role == entry.role);
            _registry.entries.Add(entry);
            SaveRegistry();
            return JsonUtility.ToJson(new BasicRes { message = entry.uuid, id = go.GetInstanceID() });
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