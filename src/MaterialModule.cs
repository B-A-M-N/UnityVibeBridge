using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        public static string VibeTool_material_list(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            var r = obj?.GetComponent<Renderer>();
            if (r == null) return "{\"error\":\"No renderer\"}";
            return "{\"materials\":[" + string.Join(",", r.sharedMaterials.Select((m, i) => "{\"index\":" + i + ",\"name\":\"" + (m != null ? m.name : "null") + "\"}")) + "]}";
        }

        public static string VibeTool_material_set_color(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            var r = obj?.GetComponent<Renderer>();
            if (r == null) return "{\"error\":\"No renderer\"}";
            int index = int.Parse(q["index"]);
            var p = q["color"].Split(',');
            Color col = new Color(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]), p.Length > 3 ? float.Parse(p[3]) : 1f);
            var m = r.sharedMaterials[index];
            Undo.RecordObject(m, "Set Color");
            SetColorInternal(m, col);
            return "{\"message\":\"Success\"}";
        }

        public static string VibeTool_material_set_slot_material(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            var r = obj?.GetComponent<Renderer>();
            int index = int.Parse(q["index"]);
            string matName = q["material"];
            if (r == null || index >= r.sharedMaterials.Length) return "{\"error\":\"Invalid target\"}";
            
            Material mat = null;
            string[] guids = AssetDatabase.FindAssets(matName + " t:Material");
            if (guids.Length > 0) mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (mat == null) return "{\"error\":\"Material not found: " + matName + "\"}";

            Undo.RecordObject(r, "Set Material Slot");
            Material[] mats = r.sharedMaterials;
            mats[index] = mat;
            r.sharedMaterials = mats;
            return "{\"message\":\"Success\"}";
        }

        public static string VibeTool_material_insert_slot(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            var r = obj?.GetComponent<Renderer>();
            if (r == null) return "{\"error\":\"No renderer found\"}";
            
            int index = int.Parse(q["index"]);
            string matName = q["material"];
            
            Material mat = null;
            string[] guids = AssetDatabase.FindAssets(matName + " t:Material");
            if (guids.Length > 0) mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (mat == null) return "{\"error\":\"Material not found: " + matName + "\"}";
            
            Material[] oldMats = r.sharedMaterials;
            if (index < 0 || index > oldMats.Length) return "{\"error\":\"Index out of range\"}";
            
            Material[] newMats = new Material[oldMats.Length + 1];
            for (int i = 0, j = 0; i < newMats.Length; i++) {
                if (i == index) {
                    newMats[i] = mat;
                } else {
                    newMats[i] = oldMats[j++];
                }
            }
            
            Undo.RecordObject(r, "Insert Material Slot");
            r.sharedMaterials = newMats;
            return "{\"message\":\"Inserted slot " + index + "\"}";
        }

        public static string VibeTool_material_remove_slot(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            
            var r = obj?.GetComponent<Renderer>();
            if (r == null) return "{\"error\":\"No renderer found\"}";
            
            int index = int.Parse(q["index"]);
            Material[] oldMats = r.sharedMaterials;
            
            if (index < 0 || index >= oldMats.Length) return "{\"error\":\"Index out of range\"}";
            
            Material[] newMats = new Material[oldMats.Length - 1];
            for (int i = 0, j = 0; i < oldMats.Length; i++) {
                if (i == index) continue;
                newMats[j++] = oldMats[i];
            }
            
            Undo.RecordObject(r, "Remove Material Slot");
            r.sharedMaterials = newMats;
            return "{\"message\":\"Removed slot " + index + "\"}";
        }

        public static string VibeTool_material_inspect_properties(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            var r = obj?.GetComponent<Renderer>();
            int index = int.Parse(q["index"]);
            if (r == null || index >= r.sharedMaterials.Length) return "{\"error\":\"Invalid target\"}";
            var m = r.sharedMaterials[index];
            if (m == null) return "{\"error\":\"Material is null\"}";
            var props = new List<string>();
            int count = ShaderUtil.GetPropertyCount(m.shader);
            for (int i = 0; i < count; i++) { props.Add("\"" + ShaderUtil.GetPropertyName(m.shader, i) + "\""); }
            return "{\"name\":\"" + m.name + "\",\"shader\":\"" + m.shader.name + "\",\"properties\":[" + string.Join(",", props) + "]}";
        }

        public static string VibeTool_material_inspect_slot(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            var r = obj?.GetComponent<Renderer>();
            int index = int.Parse(q["index"]);
            if (r == null || index >= r.sharedMaterials.Length) return "{\"error\":\"Invalid target\"}";
            var m = r.sharedMaterials[index];
            if (m == null) return "{\"error\":\"Material is null\"}";
            return "{\"name\":\"" + m.name + "\",\"shader\":\"" + m.shader.name + "\"}";
        }

        public static string VibeTool_material_set_slot_texture(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            var r = obj?.GetComponent<Renderer>();
            int index = int.Parse(q["index"]);
            string field = q["field"];
            string texPath = q["value"];
            
            if (r == null || index >= r.sharedMaterials.Length) return "{\"error\":\"Invalid target\"}";
            var m = r.sharedMaterials[index];
            if (m == null) return "{\"error\":\"Material is null\"}";
            
            Texture tex = null;
            if (!string.IsNullOrEmpty(texPath)) {
                tex = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
                if (tex == null) return "{\"error\":\"Texture not found: " + texPath + "\"}";
            }
            
            Undo.RecordObject(m, "Set Texture");
            m.SetTexture(field, tex);
            return "{\"message\":\"Success\"}";
        }

        private static void SetColorInternal(Material m, Color col) {
            if (m == null) return;
            string[] targets = { "_Color", "_BaseColor", "_MainColor", "_EmissionColor" };
            foreach (var t in targets) { if (m.HasProperty(t)) m.SetColor(t, col); }
        }

        public static string VibeTool_material_clear_block(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            var r = obj?.GetComponent<Renderer>();
            if (r != null) { Undo.RecordObject(r, "Clear Block"); r.SetPropertyBlock(null); }
            return "{\"message\":\"Success\"}";
        }

        public static string VibeTool_asset_set_internal_name(Dictionary<string, string> q) {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(q["path"]);
            if (asset != null) { asset.name = q["newName"]; AssetDatabase.SaveAssets(); return "{\"message\":\"Success\"}"; }
            return "{\"error\":\"Not found\"}";
        }
    }
}