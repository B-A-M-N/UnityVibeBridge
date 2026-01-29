using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        private static RegistryData _registry = new RegistryData();
        private const string REGISTRY_PATH = "metadata/vibe_registry.json";

        private static void LoadRegistry() {
            if (File.Exists(REGISTRY_PATH)) {
                try {
                    string json = File.ReadAllText(REGISTRY_PATH);
                    _registry = JsonUtility.FromJson<RegistryData>(json);
                } catch { _registry = new RegistryData(); }
            }
        }

        private static void SaveRegistry() {
            if (!Directory.Exists("metadata")) Directory.CreateDirectory("metadata");
            string json = JsonUtility.ToJson(_registry, true);
            File.WriteAllText(REGISTRY_PATH, json);
        }

        public static string VibeTool_registry_add(Dictionary<string, string> query) {
            string path = query["path"];
            string uuid = query["uuid"];
            string role = query["role"];
            string group = query["group"];

            GameObject obj = null;
            if (int.TryParse(path, out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(path);
            if (obj == null) return "{\"error\":\"Object not found\"}";

            var smr = obj.GetComponent<SkinnedMeshRenderer>();
            var mf = obj.GetComponent<MeshFilter>();
            var renderer = obj.GetComponent<Renderer>();
            var componentTypes = obj.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name).OrderBy(n => n).ToArray();

            Mesh mesh = smr != null ? smr.sharedMesh : (mf != null ? mf.sharedMesh : null);
            if (mesh == null) return "{\"error\":\"No mesh found\"}";

            var entry = new RegistryEntry {
                uuid = uuid, role = role, group = group, lastKnownID = obj.GetInstanceID(),
                fingerprint = new Fingerprint {
                    meshName = mesh.name, triangles = mesh.triangles.Length / 3, vertices = mesh.vertexCount,
                    shaders = renderer != null ? renderer.sharedMaterials.Select(m => m != null ? m.shader.name : "null").ToArray() : new string[0],
                    components = componentTypes
                }
            };

            _registry.entries.RemoveAll(e => e.uuid == uuid);
            _registry.entries.Add(entry);
            SaveRegistry();
            return "{\"message\":\"Asset registered\",\"uuid\":\"" + uuid + "\"}";
        }

        public static string VibeTool_registry_save(Dictionary<string, string> query) {
            SaveRegistry();
            return "{\"message\":\"Saved\"}";
        }

        public static string VibeTool_registry_load(Dictionary<string, string> query) {
            LoadRegistry();
            return "{\"message\":\"Loaded\"}";
        }

        private static GameObject ResolveTarget(RegistryEntry entry) {
            var obj = EditorUtility.InstanceIDToObject(entry.lastKnownID) as GameObject;
            if (obj != null && VerifyRegistryFingerprint(obj, entry.fingerprint)) return obj;

            var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var go in allObjects) {
                if (VerifyRegistryFingerprint(go, entry.fingerprint)) {
                    entry.lastKnownID = go.GetInstanceID();
                    return go;
                }
            }
            return null;
        }

        private static bool VerifyRegistryFingerprint(GameObject go, Fingerprint fp) {
            var smr = go.GetComponent<SkinnedMeshRenderer>();
            var mf = go.GetComponent<MeshFilter>();
            Mesh mesh = smr != null ? smr.sharedMesh : (mf != null ? mf.sharedMesh : null);
            if (mesh == null) return false;

            if (mesh.triangles.Length / 3 != fp.triangles || 
                mesh.vertexCount != fp.vertices || 
                mesh.name != fp.meshName) return false;

            if (fp.components != null && fp.components.Length > 0) {
                var currentComponents = go.GetComponents<Component>()
                    .Where(c => c != null)
                    .Select(c => c.GetType().Name)
                    .OrderBy(n => n)
                    .ToArray();
                
                if (currentComponents.Length != fp.components.Length) return false;
                for (int i = 0; i < currentComponents.Length; i++) {
                    if (currentComponents[i] != fp.components[i]) return false;
                }
            }
            return true;
        }
    }
}