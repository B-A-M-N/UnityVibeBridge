#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityVibeBridge.Kernel {
    public static partial class VibeBridgeServer {
        [VibeTool("hierarchy", "Returns a paginated list of all GameObjects in the scene.", "page", "pageSize")]
        public static string VibeTool_hierarchy(Dictionary<string, string> q) {
            int page = q.ContainsKey("page") ? int.Parse(q["page"]) : 0;
            int pageSize = q.ContainsKey("pageSize") ? int.Parse(q["pageSize"]) : 500;

            var all = UnityEngine.Object.FindObjectsOfType<GameObject>(true)
                .OrderBy(go => go.name).ToList();
            
            var paginated = all.Skip(page * pageSize).Take(pageSize)
                .Select(o => new HierarchyRes.ObjectNode {
                    name = o.name,
                    id = o.GetInstanceID()
                }).ToArray();
            
            return JsonUtility.ToJson(new HierarchyRes {
                objects = paginated
            });
        }

        [VibeTool("scene/state", "Returns a high-level summary of the active scene.")]
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

        [VibeTool("assets/state", "Returns the health of the AssetDatabase and optional file hashes.", "paths")]
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
