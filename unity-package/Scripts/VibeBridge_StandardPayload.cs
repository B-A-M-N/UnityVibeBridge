// UnityVibeBridge: The Governed Creation Kernel for Unity
// Copyright (C) 2026 B-A-M-N
//
// This software is dual-licensed under the GNU AGPLv3 and a 
// Commercial "Work-or-Pay" Maintenance Agreement.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VibeBridge.Core;
using Cysharp.Threading.Tasks;

namespace VibeBridge {
    public static partial class VibeBridgeServer {

        public static async UniTask<string> ExecuteStandardTool(string toolName, Dictionary<string, string> q) {
            // Mapping for tools that don't follow the simple VibeTool_object_ prefix
            string lookupName = toolName.Replace("VibeTool_", "");
            if (lookupName.StartsWith("object_")) lookupName = lookupName.Replace("object_", "Object");
            else if (lookupName.StartsWith("system_")) lookupName = lookupName.Replace("system_", "System");
            else if (lookupName.StartsWith("texture_")) lookupName = lookupName.Replace("texture_", "Texture");
            else if (lookupName.StartsWith("world_")) lookupName = lookupName.Replace("world_", "World");
            else if (lookupName.StartsWith("asset_")) lookupName = lookupName.Replace("asset_", "Asset");
            else if (lookupName.StartsWith("prefab_")) lookupName = lookupName.Replace("prefab_", "Prefab");
            else if (lookupName.StartsWith("view_")) lookupName = lookupName.Replace("view_", "View");
            else if (lookupName.StartsWith("opt_")) lookupName = lookupName.Replace("opt_", "Opt");
            else if (lookupName.StartsWith("material_")) lookupName = lookupName.Replace("material_", "Material");

            if (!Enum.TryParse(lookupName, true, out ToolID toolID)) {
                // Try literal mapping for compound names
                string literal = string.Concat(lookupName.Split('_').Select(s => s.Length > 0 ? char.ToUpper(s[0]) + s.Substring(1) : ""));
                if (!Enum.TryParse(literal, true, out toolID)) {
                     // Fallback to the Switch below if enum parsing is too complex for this pattern
                }
            }

            try {
                await AsyncUtils.SwitchToMainThreadSafe();
            } catch (Exception e) {
                return JsonUtility.ToJson(new BasicRes { error = $"Concurrency Failure: {e.Message}" });
            }

            return toolName switch {
                "VibeTool_object_set_active" => VibeTool_object_set_active(q),
                "VibeTool_object_set_blendshape" => VibeTool_object_set_blendshape(q),
                "VibeTool_system_vram_footprint" => VibeTool_system_vram_footprint(q),
                "VibeTool_texture_crush" => VibeTool_texture_crush(q),
                "VibeTool_world_spawn" => VibeTool_world_spawn(q),
                "VibeTool_world_spawn_primitive" => VibeTool_world_spawn_primitive(q),
                "VibeTool_world_static_list" => VibeTool_world_static_list(q),
                "VibeTool_world_static_set" => VibeTool_world_static_set(q),
                "VibeTool_asset_move" => VibeTool_asset_move(q),
                "VibeTool_prefab_apply" => VibeTool_prefab_apply(q),
                "VibeTool_view_screenshot" => VibeTool_view_screenshot(q),
                "VibeTool_material_batch_replace" => VibeTool_material_batch_replace(q),
                "VibeTool_system_find_by_component" => VibeTool_system_find_by_component(q),
                "VibeTool_system_search" => VibeTool_system_search(q),
                "VibeTool_opt_fork" => VibeTool_opt_fork(q),
                "VibeTool_system_git_checkpoint" => VibeTool_system_git_checkpoint(q),
                "VibeTool_inspect" => VibeTool_inspect(q),
                "VibeTool_hierarchy" => VibeTool_hierarchy(q),
                "VibeTool_audit_avatar" => VibeTool_audit_avatar(q),
                "VibeTool_physics_audit" => VibeTool_physics_audit(q),
                "VibeTool_animation_audit" => VibeTool_animation_audit(q),
                "VibeTool_physbone_rank_importance" => VibeTool_physbone_rank_importance(q),
                "VibeTool_visual_point" => VibeTool_visual_point(q),
                "VibeTool_visual_line" => VibeTool_visual_line(q),
                "VibeTool_visual_clear" => VibeTool_visual_clear(q),
                "VibeTool_animator_set_param" => VibeTool_animator_set_param(q),
                "VibeTool_export_validate" => VibeTool_export_validate(q),
                "VibeTool_system_undo" => VibeTool_system_undo(q),
                "VibeTool_system_redo" => VibeTool_system_redo(q),
                "VibeTool_system_list_tools" => VibeTool_system_list_tools(q),
                "VibeTool_transaction_begin" => VibeTool_transaction_begin(q),
                "VibeTool_transaction_commit" => VibeTool_transaction_commit(q),
                "VibeTool_transaction_abort" => VibeTool_transaction_abort(q),
                "VibeTool_status" => VibeTool_status(q),
                _ => JsonUtility.ToJson(new BasicRes { error = "Tool not mapped." })
            };
        }
        
        public static string VibeTool_object_set_active(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Object not found" });
            bool state = q.ContainsKey("active") && q["active"].ToLower() == "true";
            Undo.RecordObject(go, (state ? "Activate " : "Deactivate ") + go.name);
            go.SetActive(state);
            return JsonUtility.ToJson(new BasicRes { message = "Object " + (state ? "activated" : "deactivated") });
        }

        public static string VibeTool_object_set_blendshape(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            var smr = go?.GetComponent<SkinnedMeshRenderer>();
            if (smr == null) return JsonUtility.ToJson(new BasicRes { error = "No SkinnedMeshRenderer" });
            string name = q["name"];
            float val = float.Parse(q["value"]);
            int idx = smr.sharedMesh.GetBlendShapeIndex(name);
            if (idx == -1) return JsonUtility.ToJson(new BasicRes { error = "BlendShape not found: " + name });
            Undo.RecordObject(smr, "Set BlendShape");
            smr.SetBlendShapeWeight(idx, val);
            EditorUtility.SetDirty(smr);
            return JsonUtility.ToJson(new BasicRes { message = "BlendShape " + name + " set to " + val });
        }

        public static string VibeTool_system_vram_footprint(Dictionary<string, string> q) {
            GameObject obj = Resolve(q["path"]);
            if (obj == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            long totalBytes = 0;
            var textures = new HashSet<Texture2D>();
            foreach (var r in obj.GetComponentsInChildren<Renderer>(true)) {
                foreach (var mat in r.sharedMaterials) {
                    if (mat == null) continue;
                    int count = ShaderUtil.GetPropertyCount(mat.shader);
                    for (int i = 0; i < count; i++) {
                        if (ShaderUtil.GetPropertyType(mat.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv) {
                            Texture t = mat.GetTexture(ShaderUtil.GetPropertyName(mat.shader, i));
                            if (t is Texture2D t2d) textures.Add(t2d);
                        }
                    }
                }
            }
            foreach (var tex in textures) totalBytes += (long)(tex.width * tex.height * 4);
            return JsonUtility.ToJson(new VramRes { vramMB = totalBytes / (1024f * 1024f), textures = textures.Count });
        }

        public static string VibeTool_texture_crush(Dictionary<string, string> q) {
            GameObject obj = Resolve(q["path"]);
            if (obj == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            int size = int.Parse(q["maxSize"]);
            var textures = new HashSet<Texture2D>();
            foreach (var r in obj.GetComponentsInChildren<Renderer>(true)) {
                foreach (var mat in r.sharedMaterials) {
                    if (mat == null) continue;
                    int count = ShaderUtil.GetPropertyCount(mat.shader);
                    for (int i = 0; i < count; i++) {
                        if (ShaderUtil.GetPropertyType(mat.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv) {
                            Texture t = mat.GetTexture(ShaderUtil.GetPropertyName(mat.shader, i));
                            if (t is Texture2D t2d) textures.Add(t2d);
                        }
                    }
                }
            }
            int changed = 0;
            foreach (var tex in textures) {
                string path = AssetDatabase.GetAssetPath(tex);
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti != null && ti.maxTextureSize > size) { ti.maxTextureSize = size; AssetDatabase.ImportAsset(path); changed++; }
            }
            return JsonUtility.ToJson(new BasicRes { message = "Crushed " + changed + " textures" });
        }

        public static string VibeTool_world_spawn(Dictionary<string, string> q) {
            string rawAsset = q["asset"];
            string resolvedPath = ResolveAssetPath(rawAsset);
            if (string.IsNullOrEmpty(resolvedPath)) return JsonUtility.ToJson(new BasicRes { error = "Asset not found: " + rawAsset });
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(resolvedPath);
            if (prefab == null) return JsonUtility.ToJson(new BasicRes { error = "Prefab failed to load: " + resolvedPath });
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (q.ContainsKey("pos")) {
                var p = q["pos"].Split(',').Select(float.Parse).ToArray();
                if (p.Length == 3) go.transform.position = new Vector3(p[0], p[1], p[2]);
            }
            if (q.ContainsKey("rot")) {
                var r = q["rot"].Split(',').Select(float.Parse).ToArray();
                if (r.Length == 3) go.transform.eulerAngles = new Vector3(r[0], r[1], r[2]);
            }
            Undo.RegisterCreatedObjectUndo(go, "Spawn " + go.name);
            Selection.activeGameObject = go;
            return JsonUtility.ToJson(new BasicRes { message = "Spawned " + go.name, id = go.GetInstanceID() });
        }

        public static string VibeTool_world_spawn_primitive(Dictionary<string, string> q) {
            PrimitiveType type = (PrimitiveType)Enum.Parse(typeof(PrimitiveType), q["type"], true);
            GameObject go = GameObject.CreatePrimitive(type);
            Undo.RegisterCreatedObjectUndo(go, "Spawn " + type);
            if (q.ContainsKey("pos")) {
                var p = q["pos"].Split(',').Select(float.Parse).ToArray();
                if (p.Length == 3) go.transform.position = new Vector3(p[0], p[1], p[2]);
            }
            Selection.activeGameObject = go;
            return JsonUtility.ToJson(new BasicRes { message = "Spawned " + type, id = go.GetInstanceID() });
        }

        public static string VibeTool_world_static_list(Dictionary<string, string> q) {
            var names = Enum.GetNames(typeof(StaticEditorFlags));
            var values = Enum.GetValues(typeof(StaticEditorFlags));
            var list = new List<StaticFlagRes.StaticFlagNode>();
            for (int i = 0; i < names.Length; i++) {
                list.Add(new StaticFlagRes.StaticFlagNode { name = names[i], value = (int)values.GetValue(i) });
            }
            return JsonUtility.ToJson(new StaticFlagRes { flags = list.ToArray() });
        }

        public static string VibeTool_world_static_set(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            StaticEditorFlags flags = (StaticEditorFlags)int.Parse(q["flags"]);
            GameObjectUtility.SetStaticEditorFlags(go, flags);
            return JsonUtility.ToJson(new BasicRes { message = "Static flags updated" });
        }

        public static string VibeTool_asset_move(Dictionary<string, string> q) {
            string err = AssetDatabase.MoveAsset(q["path"], q["newPath"]);
            return string.IsNullOrEmpty(err) ? JsonUtility.ToJson(new BasicRes { message = "Asset moved" }) : JsonUtility.ToJson(new BasicRes { error = err });
        }

        public static string VibeTool_prefab_apply(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null || !PrefabUtility.IsPartOfAnyPrefab(go)) return JsonUtility.ToJson(new BasicRes { error = "Not a prefab instance" });
            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            PrefabUtility.ApplyPrefabInstance(root, InteractionMode.UserAction);
            return JsonUtility.ToJson(new BasicRes { message = "Changes applied to prefab: " + root.name });
        }

        public static string VibeTool_view_screenshot(Dictionary<string, string> q) {
            Camera cam = SceneView.lastActiveSceneView.camera;
            if (cam == null) return JsonUtility.ToJson(new BasicRes { error = "No SceneView" });
            int w = q.ContainsKey("w") ? int.Parse(q["w"]) : 1280, h = q.ContainsKey("h") ? int.Parse(q["h"]) : 720;
            RenderTexture rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            Texture2D ss = new Texture2D(w, h, TextureFormat.RGB24, false);
            cam.Render();
            RenderTexture.active = rt;
            ss.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            cam.targetTexture = null;
            RenderTexture.active = null;
            byte[] bytes = ss.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(rt);
            return "{\"base64\":\"" + Convert.ToBase64String(bytes) + "\"}";
        }

        public static string VibeTool_material_batch_replace(Dictionary<string, string> q) {
            string[] paths = q["paths"].Split(',');
            string matName = q["material"];
            string resolvedPath = ResolveAssetPath(matName, "t:Material");
            if (string.IsNullOrEmpty(resolvedPath)) return JsonUtility.ToJson(new BasicRes { error = "Material not found: " + matName });
            Material newMat = AssetDatabase.LoadAssetAtPath<Material>(resolvedPath);
            if (newMat == null) return JsonUtility.ToJson(new BasicRes { error = "Material failed to load: " + resolvedPath });
            int count = 0;
            foreach (var p in paths) {
                GameObject go = Resolve(p.Trim());
                var r = go?.GetComponent<Renderer>();
                if (r != null) {
                    Undo.RecordObject(r, "Batch Material");
                    Material[] mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = newMat;
                    r.sharedMaterials = mats;
                    count++;
                }
            }
            return JsonUtility.ToJson(new BasicRes { message = "Replaced materials on " + count + " objects" });
        }

        public static string VibeTool_system_find_by_component(Dictionary<string, string> q) {
            string typeName = q["type"];
            var all = UnityEngine.Object.FindObjectsOfType<GameObject>(true);
            var results = all.Where(go => go.GetComponent(typeName) != null)
                .Select(go => new BasicRes { message = go.name, id = go.GetInstanceID() }).ToArray();
            return JsonUtility.ToJson(new FindRes { results = results });
        }

        public static string VibeTool_system_search(Dictionary<string, string> q) {
            string pattern = q.ContainsKey("name") ? q["name"] : "";
            int layer = q.ContainsKey("layer") ? int.Parse(q["layer"]) : -1;
            
            var all = UnityEngine.Object.FindObjectsOfType<GameObject>(true);
            var results = all.Where(go => {
                bool match = true;
                if (!string.IsNullOrEmpty(pattern)) match = System.Text.RegularExpressions.Regex.IsMatch(go.name, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (layer != -1) match = match && go.layer == layer;
                return match;
            }).Take(100).Select(go => new BasicRes { message = go.name, id = go.GetInstanceID() }).ToArray();
            
            return JsonUtility.ToJson(new FindRes { results = results });
        }

        public static string VibeTool_opt_fork(Dictionary<string, string> q) {
            GameObject original = Resolve(q["path"]);
            if (original == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });

            GameObject fork = UnityEngine.Object.Instantiate(original, original.transform.parent);
            fork.name = "Fork_" + original.name;
            fork.transform.localPosition = original.transform.localPosition;
            fork.transform.localRotation = original.transform.localRotation;
            fork.transform.localScale = original.transform.localScale;

            Undo.RegisterCreatedObjectUndo(fork, "Fork Object");
            Undo.RecordObject(original, "Disable Original");
            original.SetActive(false);

            return JsonUtility.ToJson(new BasicRes { message = "Forked", id = fork.GetInstanceID() });
        }

        public static string VibeTool_system_git_checkpoint(Dictionary<string, string> q) {
            string msg = q.ContainsKey("message") ? q["message"] : "Bridge Checkpoint";
            string path = q.ContainsKey("path") ? q["path"] : ".";
            AssetDatabase.SaveAssets();
            
            var proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = "git";
            proc.StartInfo.Arguments = $"-C {Directory.GetCurrentDirectory()} add {path}";
            proc.Start(); proc.WaitForExit();
            
            proc.StartInfo.Arguments = $"-C {Directory.GetCurrentDirectory()} commit -m \"{msg}\" ";
            proc.Start(); proc.WaitForExit();
            
            return JsonUtility.ToJson(new BasicRes { message = "Git checkpoint created for " + path });
        }
    }
}
#endif