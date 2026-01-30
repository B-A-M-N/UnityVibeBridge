using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        
        // --- AUDIT MODULE (Cognitive Upgrade) ---
        // Provides deep, single-call analysis of complex hierarchies.

        public static string VibeTool_audit_avatar(Dictionary<string, string> q) {
            GameObject root = null;
            if (int.TryParse(q["path"], out int id)) root = EditorUtility.InstanceIDToObject(id) as GameObject;
            else root = GameObject.Find(q["path"]);
            if (root == null) return "{\"error\":\"Root not found\"}";

            var report = new AvatarAuditReport {
                name = root.name,
                instanceID = root.GetInstanceID(),
                isPrefab = PrefabUtility.IsPartOfAnyPrefab(root)
            };

            var allChildren = root.GetComponentsInChildren<Transform>(true);
            report.objectCount = allChildren.Length;

            foreach (var t in allChildren) {
                GameObject go = t.gameObject;
                
                // 1. Check for Renderers
                var r = go.GetComponent<Renderer>();
                if (r != null) {
                    var rs = new RendererAudit {
                        path = GetGameObjectPath(go, root),
                        type = r.GetType().Name,
                        materialCount = r.sharedMaterials.Length,
                        materials = r.sharedMaterials.Select(m => m != null ? m.name : "null").ToArray()
                    };
                    
                    var mf = go.GetComponent<MeshFilter>();
                    var smr = go.GetComponent<SkinnedMeshRenderer>();
                    Mesh mesh = smr != null ? smr.sharedMesh : (mf != null ? mf.sharedMesh : null);
                    if (mesh != null) {
                        rs.vertexCount = mesh.vertexCount;
                        rs.meshName = mesh.name;
                    }
                    report.renderers.Add(rs);
                }

                // 2. Check for Issues
                var components = go.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++) {
                    if (components[i] == null) {
                        report.issues.Add(new IssueAudit {
                            path = GetGameObjectPath(go, root),
                            type = "MissingScript",
                            severity = "High"
                        });
                    }
                }
            }

            return JsonUtility.ToJson(report);
        }

        private static string GetGameObjectPath(GameObject obj, GameObject root) {
            if (obj == root) return ".";
            string path = obj.name;
            while (obj.transform.parent != null && obj.transform.parent.gameObject != root) {
                obj = obj.transform.parent.gameObject;
                path = obj.name + "/" + path;
            }
            return path;
        }

        [Serializable]
        public class AvatarAuditReport {
            public string name;
            public int instanceID;
            public bool isPrefab;
            public int objectCount;
            public List<RendererAudit> renderers = new List<RendererAudit>();
            public List<IssueAudit> issues = new List<IssueAudit>();
        }

        [Serializable]
        public class RendererAudit {
            public string path;
            public string type;
            public int vertexCount;
            public string meshName;
            public int materialCount;
            public string[] materials;
        }

        [Serializable]
        public class IssueAudit {
            public string path;
            public string type;
            public string severity;
        }
    }
}
