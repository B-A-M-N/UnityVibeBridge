#if UNITY_EDITOR
// UnityVibeBridge: The Governed Creation Kernel for Unity
// Copyright (C) 2026 B-A-M-N
//
// This software is dual-licensed under the GNU AGPLv3 and a 
// Commercial "Work-or-Pay" Maintenance Agreement.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        
        [Serializable] public class MatListRes { public MatNode[] materials; [Serializable] public struct MatNode { public int index; public string name; } }
        [Serializable] public class MatPropRes { public string name, shader; public string[] properties; }
        [Serializable] public class MatSnapshot { public string avatarName; public List<RendererSnapshot> renderers = new List<RendererSnapshot>(); }
        [Serializable] public class RendererSnapshot { public string path; public List<string> materialGuids = new List<string>(); }

        public static string VibeTool_material_list(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            var r = go?.GetComponent<Renderer>();
            if (r == null) return JsonUtility.ToJson(new BasicRes { error = "No renderer" });
            var nodes = r.sharedMaterials.Select((m, i) => new MatListRes.MatNode { index = i, name = m != null ? m.name : "null" }).ToArray();
            return JsonUtility.ToJson(new MatListRes { materials = nodes });
        }

        public static string VibeTool_material_inspect_properties(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            var r = go?.GetComponent<Renderer>();
            int idx = int.Parse(q["index"]);
            if (r == null || idx >= r.sharedMaterials.Length) return JsonUtility.ToJson(new BasicRes { error = "Invalid target" });
            var m = r.sharedMaterials[idx];
            if (m == null) return JsonUtility.ToJson(new BasicRes { error = "Material is null" });
            
            var props = new List<string>();
            int count = ShaderUtil.GetPropertyCount(m.shader);
            for (int i = 0; i < count; i++) props.Add(ShaderUtil.GetPropertyName(m.shader, i));
            
            return JsonUtility.ToJson(new MatPropRes { name = m.name, shader = m.shader.name, properties = props.ToArray() });
        }

        public static string VibeTool_material_set_color(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            var r = go?.GetComponent<Renderer>();
            int idx = int.Parse(q["index"]);
            if (r == null || idx >= r.sharedMaterials.Length) return JsonUtility.ToJson(new BasicRes { error = "Invalid target" });
            
            var p = q["color"].Split(',').Select(float.Parse).ToArray();
            Color col = new Color(p[0], p[1], p[2], p.Length > 3 ? p[3] : 1f);
            var m = r.sharedMaterials[idx];
            
            Undo.RecordObject(m, "Set Color");
            string[] targets = { "_Color", "_BaseColor", "_MainColor", "_EmissionColor" };
            foreach (var t in targets) if (m.HasProperty(t)) m.SetColor(t, col);
            
            return JsonUtility.ToJson(new BasicRes { message = "Color updated" });
        }

        public static string VibeTool_material_set_texture(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            var r = go?.GetComponent<Renderer>();
            int idx = int.Parse(q["index"]);
            string field = q["field"], texPath = q["texture"];
            if (r == null || idx >= r.sharedMaterials.Length) return JsonUtility.ToJson(new BasicRes { error = "Invalid target" });
            
            Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
            if (tex == null && !string.IsNullOrEmpty(texPath)) return JsonUtility.ToJson(new BasicRes { error = "Texture not found" });
            
            var m = r.sharedMaterials[idx];
            Undo.RecordObject(m, "Set Texture");
            m.SetTexture(field, tex);
            
            return JsonUtility.ToJson(new BasicRes { message = "Texture updated" });
        }

        public static string VibeTool_material_set_float(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            var r = go?.GetComponent<Renderer>();
            int idx = int.Parse(q["index"]);
            if (r == null || idx >= r.sharedMaterials.Length) return JsonUtility.ToJson(new BasicRes { error = "Invalid target" });
            
            var m = r.sharedMaterials[idx];
            Undo.RecordObject(m, "Set Float");
            m.SetFloat(q["field"], float.Parse(q["value"]));
            return JsonUtility.ToJson(new BasicRes { message = "Float updated" });
        }

        public static string VibeTool_material_toggle_keyword(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            var r = go?.GetComponent<Renderer>();
            int idx = int.Parse(q["index"]);
            if (r == null || idx >= r.sharedMaterials.Length) return JsonUtility.ToJson(new BasicRes { error = "Invalid target" });
            
            var m = r.sharedMaterials[idx];
            string kw = q["keyword"];
            bool state = q["state"].ToLower() == "true";
            
            Undo.RecordObject(m, "Toggle Keyword");
            if (state) m.EnableKeyword(kw);
            else m.DisableKeyword(kw);
            
            return JsonUtility.ToJson(new BasicRes { message = $"Keyword {kw} set to {state}" });
        }

        public static string VibeTool_material_snapshot(Dictionary<string, string> q) {
            GameObject root = Resolve(q["path"]);
            if (root == null) return JsonUtility.ToJson(new BasicRes { error = "Root not found" });

            var snapshot = new MatSnapshot { avatarName = root.name };
            foreach (var r in root.GetComponentsInChildren<Renderer>(true)) {
                var rs = new RendererSnapshot { path = GetGameObjectPath(r.gameObject, root) };
                foreach (var m in r.sharedMaterials) {
                    rs.materialGuids.Add(m != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m)) : "null");
                }
                snapshot.renderers.Add(rs);
            }

            string path = $"metadata/snapshots/{root.name}_mats.json";
            if (!Directory.Exists("metadata/snapshots")) Directory.CreateDirectory("metadata/snapshots");
            File.WriteAllText(path, JsonUtility.ToJson(snapshot, true));
            return JsonUtility.ToJson(new BasicRes { message = $"Snapshot saved to {path}" });
        }

        public static string VibeTool_material_restore(Dictionary<string, string> q) {
            GameObject root = Resolve(q["path"]);
            if (root == null) return JsonUtility.ToJson(new BasicRes { error = "Root not found" });

            string path = $"metadata/snapshots/{root.name}_mats.json";
            if (!File.Exists(path)) return JsonUtility.ToJson(new BasicRes { error = "Snapshot not found" });

            var snapshot = JsonUtility.FromJson<MatSnapshot>(File.ReadAllText(path));
            int count = 0;
            foreach (var rs in snapshot.renderers) {
                GameObject target = (rs.path == ".") ? root : root.transform.Find(rs.path)?.gameObject;
                var renderer = target?.GetComponent<Renderer>();
                if (renderer != null) {
                    Undo.RecordObject(renderer, "Restore Materials");
                    var mats = new Material[rs.materialGuids.Count];
                    for (int i = 0; i < rs.materialGuids.Count; i++) {
                        if (rs.materialGuids[i] == "null") mats[i] = null;
                        else mats[i] = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(rs.materialGuids[i]));
                    }
                    renderer.sharedMaterials = mats;
                    count++;
                }
            }
            return JsonUtility.ToJson(new BasicRes { message = $"Restored {count} renderers" });
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
    }
}
#endif
