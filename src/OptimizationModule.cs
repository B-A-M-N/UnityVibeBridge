using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        public static string VibeTool_texture_crush(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            if (obj == null) return "{\"error\":\"Object not found\"}";
            int size = int.Parse(q["maxSize"]);
            var textures = new HashSet<Texture2D>();
            foreach (var r in obj.GetComponentsInChildren<Renderer>(true)) {
                foreach (var mat in r.sharedMaterials) {
                    if (mat == null) continue;
                    int propCount = ShaderUtil.GetPropertyCount(mat.shader);
                    for (int i = 0; i < propCount; i++) {
                        if (ShaderUtil.GetPropertyType(mat.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv) {
                            Texture t = mat.GetTexture(ShaderUtil.GetPropertyName(mat.shader, i));
                            if (t is Texture2D t2d) textures.Add(t2d);
                        }
                    }
                }
            }
            int count = 0;
            foreach (var tex in textures) {
                string ap = AssetDatabase.GetAssetPath(tex);
                if (string.IsNullOrEmpty(ap)) continue;
                TextureImporter ti = AssetImporter.GetAtPath(ap) as TextureImporter;
                if (ti != null) { ti.maxTextureSize = size; AssetDatabase.ImportAsset(ap); count++; }
            }
            return "{\"message\":\"Crushed " + count + " textures to " + size + "\"}";
        }

        public static string VibeTool_shader_swap_quest(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            if (obj == null) return "{\"error\":\"Object not found\"}";
            Shader qs = Shader.Find("VRChat/Mobile/Toon Lit");
            int count = 0;
            foreach (var r in obj.GetComponentsInChildren<Renderer>(true)) {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) {
                    if (mats[i] != null) { mats[i].shader = qs; count++; }
                }
                r.sharedMaterials = mats;
            }
            return "{\"message\":\"Swapped " + count + " materials\"}";
        }
    }
}