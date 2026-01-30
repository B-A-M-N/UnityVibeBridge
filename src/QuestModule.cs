using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        public static string VibeTool_material_snapshot(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            if (obj == null) return "{\"error\":\"Avatar root not found\"}";

            var snapshot = new MaterialSnapshot { avatarName = obj.name };
            foreach (var r in obj.GetComponentsInChildren<Renderer>(true)) {
                var rs = new RendererSnapshot { path = GetGameObjectPath(r.gameObject) };
                foreach (var m in r.sharedMaterials) {
                    if (m == null) { rs.materialGuids.Add("null"); continue; }
                    string path = AssetDatabase.GetAssetPath(m);
                    rs.materialGuids.Add(AssetDatabase.AssetPathToGUID(path));
                }
                snapshot.renderers.Add(rs);
            }

            if (!Directory.Exists("metadata/snapshots")) Directory.CreateDirectory("metadata/snapshots");
            string snapPath = "metadata/snapshots/" + obj.name + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";
            File.WriteAllText(snapPath, JsonUtility.ToJson(snapshot, true));
            return "{\"message\":\"Snapshot created\",\"path\":\"" + snapPath + "\"}";
        }

        public static string VibeTool_opt_fork(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            if (obj == null) return "{\"error\":\"Avatar root not found\"}";

            // 1. Duplicate Object
            GameObject fork = UnityEngine.Object.Instantiate(obj);
            fork.name = obj.name + "_MQ_Build";
            Undo.RegisterCreatedObjectUndo(fork, "Fork Avatar for MQ");

            // 2. Create Isolation Folder
            string folderPath = "Assets/_QuestGenerated/" + fork.name + "_" + Guid.NewGuid().ToString().Substring(0, 4);
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            // 3. Isolate Materials
            var renderers = fork.GetComponentsInChildren<Renderer>(true);
            var matMap = new Dictionary<Material, Material>();

            foreach (var r in renderers) {
                Material[] shared = r.sharedMaterials;
                for (int i = 0; i < shared.Length; i++) {
                    if (shared[i] == null) continue;
                    if (!matMap.ContainsKey(shared[i])) {
                        Material newMat = new Material(shared[i]);
                        string safeName = shared[i].name.Replace("(Instance)", "").Trim();
                        string assetPath = folderPath + "/" + safeName + ".mat";
                        AssetDatabase.CreateAsset(newMat, assetPath);
                        matMap[shared[i]] = newMat;
                    }
                    shared[i] = matMap[shared[i]];
                }
                r.sharedMaterials = shared;
            }

            AssetDatabase.SaveAssets();
            return "{\"message\":\"Fork complete\",\"instanceID\":" + fork.GetInstanceID() + "}";
        }

        [Serializable] public class MaterialSnapshot { public string avatarName; public List<RendererSnapshot> renderers = new List<RendererSnapshot>(); }
        [Serializable] public class RendererSnapshot { public string path; public List<string> materialGuids = new List<string>(); }
    }
}
