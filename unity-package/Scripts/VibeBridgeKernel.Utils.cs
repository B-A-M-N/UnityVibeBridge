#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using UnityEditor;
using UnityEngine;

namespace UnityVibeBridge.Kernel {
    public static partial class VibeBridgeServer {
        public static string ComputeHash(string input) {
            using (var sha = System.Security.Cryptography.SHA256.Create()) {
                byte[] bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }

        public static string ResolveAssetPath(string input, string filter = "t:Object") {
            if (string.IsNullOrEmpty(input)) return null;
            if (input.StartsWith("Assets/")) return input;
            string[] guids = AssetDatabase.FindAssets(input + " " + filter);
            if (guids.Length > 0) return AssetDatabase.GUIDToAssetPath(guids[0]);
            return null;
        }

        private static GameObject Resolve(string path) {
            if (string.IsNullOrEmpty(path)) return null;
            if (int.TryParse(path, out int id)) return EditorUtility.InstanceIDToObject(id) as GameObject;
            if (path.StartsWith("sem:")) {
                return Core.VibeMetadataProvider.ResolveRole(path);
            }
            return GameObject.Find(path);
        }

        public static string GetGameObjectPath(GameObject obj, GameObject root) {
            if (obj == root) return ".";
            string path = obj.name;
            while (obj.transform.parent != null && obj.transform.parent.gameObject != root) {
                obj = obj.transform.parent.gameObject;
                path = obj.name + "/" + path;
            }
            return path;
        }

        [VibeTool("inspect", "Returns detailed transform, component, and blendshape data for an object.", "path")]
        public static string VibeTool_inspect(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            
            var shapes = new List<string>();
            var smr = go.GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null) {
                for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++) shapes.Add(smr.sharedMesh.GetBlendShapeName(i));
            }

            return JsonUtility.ToJson(new InspectRes {
                name = go.name, active = go.activeSelf, tag = go.tag, layer = go.layer,
                pos = go.transform.localPosition, rot = go.transform.localEulerAngles, scale = go.transform.localScale,
                components = go.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name).ToArray(),
                blendshapes = shapes.ToArray()
            });
        }



        public static class ShaderUtils {
            private static readonly string[] _fallbackDragnet = { "VRChat/Mobile/Toon Lit", "Universal Render Pipeline/Simple Lit", "Universal Render Pipeline/Lit", "Mobile/Diffuse", "Standard" };
            public static Shader FindShader(string name, string pref = null) {
                Shader s = InternalFind(name); if (s != null) return s;
                if (!string.IsNullOrEmpty(pref)) { s = InternalFind(pref); if (s != null) return s; }
                foreach (var f in _fallbackDragnet) { s = InternalFind(f); if (s != null) return s; }
                return null;
            }
            private static Shader InternalFind(string n) {
                Shader s = Shader.Find(n); if (s != null) return s;
                string[] guids = AssetDatabase.FindAssets(n + " t:Shader");
                foreach (var g in guids) { string path = AssetDatabase.GUIDToAssetPath(g); s = AssetDatabase.LoadAssetAtPath<Shader>(path); if (s != null && s.name == n) return s; }
                return null;
            }
            public static void MigrateMaterialProperties(Material mat, Shader newShader) {
                var floats = new Dictionary<string, float>(); var colors = new Dictionary<string, Color>(); var vectors = new Dictionary<string, Vector4>(); var textures = new Dictionary<string, Texture>();
                int oldPropCount = ShaderUtil.GetPropertyCount(mat.shader);
                for (int i = 0; i < oldPropCount; i++) {
                    string name = ShaderUtil.GetPropertyName(mat.shader, i); var type = ShaderUtil.GetPropertyType(mat.shader, i);
                    if (type == ShaderUtil.ShaderPropertyType.Float || type == ShaderUtil.ShaderPropertyType.Range) floats[name] = mat.GetFloat(name);
                    else if (type == ShaderUtil.ShaderPropertyType.Color) colors[name] = mat.GetColor(name);
                    else if (type == ShaderUtil.ShaderPropertyType.Vector) vectors[name] = mat.GetVector(name);
                    else if (type == ShaderUtil.ShaderPropertyType.TexEnv) textures[name] = mat.GetTexture(name);
                }
                mat.shader = newShader;
                int newPropCount = ShaderUtil.GetPropertyCount(newShader);
                for (int i = 0; i < newPropCount; i++) {
                    string name = ShaderUtil.GetPropertyName(newShader, i); var type = ShaderUtil.GetPropertyType(newShader, i);
                    if ((type == ShaderUtil.ShaderPropertyType.Float || type == ShaderUtil.ShaderPropertyType.Range) && floats.ContainsKey(name)) mat.SetFloat(name, floats[name]);
                    else if (type == ShaderUtil.ShaderPropertyType.Color && colors.ContainsKey(name)) mat.SetColor(name, colors[name]);
                    else if (type == ShaderUtil.ShaderPropertyType.Vector && vectors.ContainsKey(name)) mat.SetVector(name, vectors[name]);
                    else if (type == ShaderUtil.ShaderPropertyType.TexEnv) { if (textures.ContainsKey(name)) mat.SetTexture(name, textures[name]); }
                }
            }
        }
    }
}
#endif