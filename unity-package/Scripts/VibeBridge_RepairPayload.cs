#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityVibeBridge.Kernel {
    public static partial class VibeBridgeServer {
        [VibeTool("avatar/audit-bones", "Audits all SkinnedMeshRenderers for null bones or missing root bones.", "path")]
        public static string VibeTool_avatar_audit_bones(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return "{\"error\":\"Target not found\"}";

            var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var report = new List<string>();

            foreach (var smr in smrs) {
                int nullCount = 0;
                if (smr.bones != null) {
                    for (int i = 0; i < smr.bones.Length; i++) {
                        if (smr.bones[i] == null) nullCount++;
                    }
                }

                if (nullCount > 0 || smr.rootBone == null) {
                    report.Add($"Mesh '{smr.name}': {nullCount} null bones, RootBone: {(smr.rootBone != null ? "OK" : "MISSING")}");
                }
            }

            if (report.Count == 0) {
                return "{\"conclusion\":\"PASS\", \"message\":\"No null bones or missing root bones found.\"}";
            } else {
                return JsonUtility.ToJson(new BasicRes { 
                    conclusion = "FAIL", 
                    message = "Audit found issues:\n" + string.Join("\n", report)
                });
            }
        }

        [VibeTool("avatar/repair-bones", "Attempts to repair SkinnedMeshRenderers by removing null bone entries.", "path")]
        public static string VibeTool_avatar_repair_bones(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return "{\"error\":\"Target not found\"}";

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Repair Null Bones");
            int group = Undo.GetCurrentGroup();

            var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            int fixedMeshes = 0;

            foreach (var smr in smrs) {
                if (smr.bones != null && smr.bones.Any(b => b == null)) {
                    Undo.RecordObject(smr, "Repair Bones");
                    smr.bones = smr.bones.Where(b => b != null).ToArray();
                    fixedMeshes++;
                }
            }

            Undo.CollapseUndoOperations(group);
            return "{\"conclusion\":\"REPAIRED\", \"message\":\"Cleaned null bones from " + fixedMeshes + " meshes.\"}";
        }

        [Serializable]
        public class RendererAuditRes {
            public string name;
            public bool enabled;
            public bool hasMesh;
            public string rootBone;
            public Vector3 boundsCenter;
            public Vector3 boundsSize;
            public bool updateWhenOffscreen;
        }

        [VibeTool("avatar/audit-renderer", "Checks the enabled state and bounds of a SkinnedMeshRenderer.", "path")]
        public static string VibeTool_avatar_audit_renderer(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return "{\"error\":\"Target not found\"}";

            var smr = go.GetComponent<SkinnedMeshRenderer>();
            if (smr == null) return "{\"error\":\"No SkinnedMeshRenderer found on target.\"}";

            string rootBoneName = smr.rootBone != null ? smr.rootBone.name : "NULL";
            string center = $"{smr.localBounds.center.x:F4},{smr.localBounds.center.y:F4},{smr.localBounds.center.z:F4}";
            string size = $"{smr.localBounds.size.x:F4},{smr.localBounds.size.y:F4},{smr.localBounds.size.z:F4}";

            return "{\"name\":\"" + go.name + "\",\"enabled\":" + smr.enabled.ToString().ToLower() + 
                   ",\"hasMesh\":" + (smr.sharedMesh != null).ToString().ToLower() + 
                   ",\"rootBone\":\"" + rootBoneName + "\",\"boundsCenter\":\"" + center + 
                   "\",\"boundsSize\":\"" + size + "\",\"updateWhenOffscreen\":" + 
                   smr.updateWhenOffscreen.ToString().ToLower() + "}";
        }
        [VibeTool("/avatar/get-path", "Returns the full hierarchy path of a GameObject.", "path")]
        public static string VibeTool_avatar_get_path(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return "{\"error\":\"Target not found\"}";

            string path = go.name;
            Transform current = go.transform.parent;
            while (current != null) {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return "{\"path\":\"" + path + "\"}";
        }
        [Serializable]
        public class MaterialAuditRes {
            public string[] materials;
            public string[] shaders;
        }

        [VibeTool("/avatar/audit-materials", "Checks the shaders and materials on a renderer.", "path")]
        public static string VibeTool_avatar_audit_materials(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return "{\"error\":\"Target not found\"}";

            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return "{\"error\":\"No Renderer found.\"}";

            var res = new MaterialAuditRes {
                materials = renderer.sharedMaterials.Select(m => m != null ? m.name : "NULL").ToArray(),
                shaders = renderer.sharedMaterials.Select(m => m != null ? m.shader.name : "NULL").ToArray()
            };

            return JsonUtility.ToJson(res);
        }
        [VibeTool("/avatar/refresh-renderer", "Forces Unity to rebuild the render state by reassigning the shared mesh.", "path")]
        public static string VibeTool_avatar_refresh_renderer(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return "{\"error\":\"Target not found\"}";

            var smr = go.GetComponent<SkinnedMeshRenderer>();
            if (smr == null) return "{\"error\":\"No SkinnedMeshRenderer found.\"}";

            Mesh m = smr.sharedMesh;
            smr.sharedMesh = null;
            smr.sharedMesh = m;

            return "{\"conclusion\":\"REFRESHED\", \"message\":\"Render state rebuilt for " + go.name + "\"}";
        }
        [VibeTool("/avatar/audit-world-scale", "Checks the absolute world scale (lossyScale) of an object.", "path")]
        public static string VibeTool_avatar_audit_world_scale(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return "{\"error\":\"Target not found\"}";
            return "{\"lossyScale\":\"" + go.transform.lossyScale.ToString() + "\"}";
        }
        [VibeTool("/avatar/audit-vertex-count", "Checks the vertex count of a SkinnedMeshRenderer.", "path")]
        public static string VibeTool_avatar_audit_vertex_count(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return "{\"error\":\"Target not found\"}";
            var smr = go.GetComponent<SkinnedMeshRenderer>();
            if (smr == null || smr.sharedMesh == null) return "{\"vertexCount\":0}";
            return "{\"vertexCount\":" + smr.sharedMesh.vertexCount + "}";
        }
        [VibeTool("/avatar/fix-ears", "Specific fix for ear texture linking using internal resolution.", "path")]
        public static string VibeTool_avatar_fix_ears(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return "{\"error\":\"Target not found\"}";

            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return "{\"error\":\"No Renderer found.\"}";

            // Find the ears material (usually slot 0)
            Material mat = null;
            for (int i = 0; i < renderer.sharedMaterials.Length; i++) {
                if (renderer.sharedMaterials[i] != null && renderer.sharedMaterials[i].name.ToLower().Contains("ears")) {
                    mat = renderer.sharedMaterials[i];
                    break;
                }
            }

            if (mat == null) return "{\"error\":\"Ears material not found on target.\"}";

            Undo.RecordObject(mat, "Fix Ears Texture");

            // Find the correct texture by name to avoid path encoding issues
            string[] guids = AssetDatabase.FindAssets("Blush Ears_edited t:Texture");
            if (guids.Length == 0) return "{\"error\":\"Blush Ears_edited texture not found in project.\"}";
            
            Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (tex == null) return "{\"error\":\"Failed to load texture from GUID.\"}";

            mat.SetTexture("_MainTex", tex);
            
            // Clean up unwanted links
            if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", null);
            if (mat.HasProperty("_Matcap")) mat.SetTexture("_Matcap", null);
            if (mat.HasProperty("_MatcapEnable")) mat.SetFloat("_MatcapEnable", 0);

            EditorUtility.SetDirty(mat);
            return "{\"conclusion\":\"FIXED\", \"message\":\"Ears texture reassigned and cleaned up.\"}";
        }
        [VibeTool("/avatar/audit-constraints", "Scans for constraints with null or missing sources.", "path")]
        public static string VibeTool_avatar_audit_constraints(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return "{\"error\":\"Target not found\"}";

            var constraints = go.GetComponentsInChildren<UnityEngine.Animations.IConstraint>(true);
            var issues = new List<string>();

            foreach (var c in constraints) {
                bool hasNull = false;
                for (int i = 0; i < c.sourceCount; i++) {
                    try {
                        if (c.GetSource(i).sourceTransform == null) {
                            hasNull = true;
                            break;
                        }
                    } catch {
                        hasNull = true;
                        break;
                    }
                }
                if (hasNull) {
                    var behaviour = c as MonoBehaviour;
                    string name = behaviour != null ? behaviour.name : "Unknown";
                    issues.Add($"Constraint on '{name}' ({c.GetType().Name}) has NULL source.");
                }
            }

            if (issues.Count == 0) return "{\"conclusion\":\"PASS\", \"message\":\"No broken constraints found.\"}";
            return "{\"conclusion\":\"FAIL\", \"message\":\"Broken Constraints found: " + string.Join(" | ", issues) + "\"}";
        }
        [VibeTool("/avatar/find-property", "Locates components that contain a specific property name.", "path", "property")]
        public static string VibeTool_avatar_find_property(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return "{\"error\":\"Target not found\"}";
            string search = q["property"];

            var results = new List<string>();
            var all = go.GetComponentsInChildren<Component>(true);
            foreach (var c in all) {
                if (c == null) continue;
                var so = new SerializedObject(c);
                var iterator = so.GetIterator();
                while (iterator.NextVisible(true)) {
                    if (iterator.name.Contains(search)) {
                        results.Add($"{c.gameObject.name} -> {c.GetType().Name} ({iterator.name})");
                        break;
                    }
                }
            }

            return "{\"results\":\"" + string.Join(" | ", results) + "\"}";
        }
        [VibeTool("/avatar/remove-component", "Recursively deletes a component of a specific type from an object and its children.", "path", "type")]
        public static string VibeTool_avatar_remove_component(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return "{\"error\":\"Target not found\"}";
            string typeName = q["type"];

            var components = go.GetComponentsInChildren<Component>(true);
            int count = 0;
            foreach (var c in components) {
                if (c != null && c.GetType().Name.Contains(typeName)) {
                    Undo.DestroyObjectImmediate(c);
                    count++;
                }
            }

            return "{\"conclusion\":\"REMOVED\", \"message\":\"Deleted " + count + " components of type " + typeName + " in hierarchy.\"}";
        }
    }
}
#endif
