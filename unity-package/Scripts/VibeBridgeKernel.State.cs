#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        public static string VibeTool_scene_state(Dictionary<string, string> q) {
            Scene activeScene = SceneManager.GetActiveScene();
            var roots = activeScene.GetRootGameObjects();
            
            var res = new HierarchyRes {
                objects = roots.Select(o => new HierarchyRes.ObjectNode {
                    name = o.name,
                    id = o.GetInstanceID()
                }).ToArray()
            };

            // Custom wrapper for extended scene state if needed by contract
            return JsonUtility.ToJson(res);
        }

        public static string VibeTool_assets_state(Dictionary<string, string> q) {
            var res = new AssetStateRes {
                asset_db_state = "Healthy",
                refresh_in_progress = EditorApplication.isUpdating,
                file_hashes = new AssetStateRes.FileHash[0] // Logic for hashing specific Vibe files
            };

            if (q.ContainsKey("paths")) {
                string[] paths = q["paths"].Split(',');
                var hashes = new List<AssetStateRes.FileHash>();
                foreach (var p in paths) {
                    string guid = AssetDatabase.AssetPathToGUID(p.Trim());
                    if (!string.IsNullOrEmpty(guid)) {
                        hashes.Add(new AssetStateRes.FileHash { path = p.Trim(), hash = guid });
                    }
                }
                res.file_hashes = hashes.ToArray();
            }

            return JsonUtility.ToJson(res);
        }
    }
}
#endif
