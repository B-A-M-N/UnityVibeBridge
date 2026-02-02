#if UNITY_EDITOR
// UnityVibeBridge: The Governed Creation Kernel for Unity
// Copyright (C) 2026 B-A-M-N
//
// This software is dual-licensed under the GNU AGPLv3 and a 
// Commercial "Work-or-Pay" Maintenance Agreement.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityVibeBridge.Kernel.Core;

namespace UnityVibeBridge.Kernel {
    public static partial class VibeBridgeServer {
        
        public static string ExecuteMaterialTool(string toolName, Dictionary<string, string> q) {
            try {
                AsyncUtils.SwitchToMainThreadSafe();
            } catch (Exception e) {
                return JsonUtility.ToJson(new BasicRes { error = $"Concurrency Failure: {e.Message}" });
            }

            return toolName switch {
                "VibeTool_material_dump_state" => VibeTool_material_dump_state(q),
                "VibeTool_material_get_color" => VibeTool_material_get_color(q),
                "VibeTool_material_get_info" => VibeTool_material_get_info(q),
                "VibeTool_material_list" => VibeTool_material_list(q),
                "VibeTool_material_inspect_properties" => VibeTool_material_inspect_properties(q),
                "VibeTool_material_set_color" => VibeTool_material_set_color(q),
                "VibeTool_material_set_texture" => VibeTool_material_set_texture(q),
                "VibeTool_material_set_float" => VibeTool_material_set_float(q),
                "VibeTool_material_toggle_keyword" => VibeTool_material_toggle_keyword(q),
                "VibeTool_material_sync_slots" => VibeTool_material_sync_slots(q),
                "VibeTool_material_assign" => VibeTool_material_assign(q),
                "VibeTool_material_snapshot" => VibeTool_material_snapshot(q),
                "VibeTool_material_restore" => VibeTool_material_restore(q),
                "VibeTool_material_fix_broken_mat" => VibeTool_material_fix_broken_mat(q),
                "VibeTool_material_hide_slot" => VibeTool_material_hide_slot(q),
                "VibeTool_material_poiyomi_lock" => VibeTool_material_poiyomi_lock(q),
                "VibeTool_material_poiyomi_unlock" => VibeTool_material_poiyomi_unlock(q),
                "VibeTool_material_swap_to_quest_shaders" => VibeTool_material_swap_to_quest_shaders(q),
                _ => JsonUtility.ToJson(new BasicRes { error = "Tool not mapped." })
            };
        }

        [VibeTool("material/dump-state", "Generates a full manifest of shaders and textures for reconstruction.", "path")]
        public static string VibeTool_material_dump_state(Dictionary<string, string> q) {
            try {
                GameObject go = Resolve(q["path"]);
                if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Target not found." });
                
                var dump = new List<ShadeInfo>();
                var renderers = go.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers) {
                    for (int i = 0; i < r.sharedMaterials.Length; i++) {
                        Material m = r.sharedMaterials[i];
                        if (m == null) continue;
                        
                        var info = new ShadeInfo {
                            mesh = r.name,
                            slot = i,
                            material = m.name,
                            shader = m.shader.name,
                            textures = new List<TexMap>()
                        };
                        
                        int propCount = ShaderUtil.GetPropertyCount(m.shader);
                        for (int j = 0; j < propCount; j++) {
                            if (ShaderUtil.GetPropertyType(m.shader, j) == ShaderUtil.ShaderPropertyType.TexEnv) {
                                string propName = ShaderUtil.GetPropertyName(m.shader, j);
                                Texture t = m.GetTexture(propName);
                                if (t != null) info.textures.Add(new TexMap { key = propName, path = AssetDatabase.GetAssetPath(t) });
                            }
                        }
                        dump.Add(info);
                    }
                }
                return "{\"shade_info\":" + JsonUtility.ToJson(new ShadeInfoList { list = dump.ToArray() }) + "}";
            } catch (Exception e) { return JsonUtility.ToJson(new BasicRes { error = e.Message }); }
        }

        [Serializable] public class TexMap { public string key; public string path; }
        [Serializable] public class ShadeInfo { public string mesh; public int slot; public string material; public string shader; public List<TexMap> textures; }
        [Serializable] public class ShadeInfoList { public ShadeInfo[] list; }

        [VibeTool("material/get-color", "Returns the color of a material property.", "path", "index", "field")]
        public static string VibeTool_material_get_color(Dictionary<string, string> q) {
            try {
                GameObject target = Resolve(q["path"]);
                if (target == null) return JsonUtility.ToJson(new BasicRes { error = "Target not found." });
                int idx = int.Parse(q["index"]);
                string field = q["field"];
                var renderer = target.GetComponent<Renderer>();
                if (renderer == null) return JsonUtility.ToJson(new BasicRes { error = "Renderer not found." });
                if (idx < 0 || idx >= renderer.sharedMaterials.Length) return JsonUtility.ToJson(new BasicRes { error = "Index out of range." });
                Material mat = renderer.sharedMaterials[idx];
                if (mat == null) return JsonUtility.ToJson(new BasicRes { error = "Material null." });
                if (!mat.HasProperty(field)) return JsonUtility.ToJson(new BasicRes { error = "Property not found: " + field });
                Color col = mat.GetColor(field);
                return JsonUtility.ToJson(new BasicRes { message = $"{col.r},{col.g},{col.b},{col.a}" });
            } catch (Exception e) { return JsonUtility.ToJson(new BasicRes { error = e.Message }); }
        }

        [VibeTool("material/get-info", "Returns the asset path and shader of a material in a specific slot.", "path", "index")]
        public static string VibeTool_material_get_info(Dictionary<string, string> q) {
            try {
                GameObject target = Resolve(q["path"]);
                if (target == null) return JsonUtility.ToJson(new BasicRes { error = "Target not found." });
                int idx = int.Parse(q["index"]);
                var renderer = target.GetComponent<Renderer>();
                if (renderer == null) return JsonUtility.ToJson(new BasicRes { error = "Renderer not found." });
                if (idx < 0 || idx >= renderer.sharedMaterials.Length) return JsonUtility.ToJson(new BasicRes { error = "Index out of range." });
                Material mat = renderer.sharedMaterials[idx];
                if (mat == null) return JsonUtility.ToJson(new BasicRes { message = "Empty Slot" });
                return JsonUtility.ToJson(new BasicRes { 
                    message = AssetDatabase.GetAssetPath(mat),
                    conclusion = mat.shader.name
                });
            } catch (Exception e) { return JsonUtility.ToJson(new BasicRes { error = e.Message }); }
        }

        [VibeTool("material/inspect-texture", "Returns the asset path of a texture assigned to a material property.", "path", "index", "field")]
        public static string VibeTool_material_inspect_texture(Dictionary<string, string> q) {
            try {
                GameObject target = Resolve(q["path"]);
                if (target == null) return JsonUtility.ToJson(new BasicRes { error = "Target not found." });
                int idx = int.Parse(q["index"]);
                string field = q["field"];
                var renderer = target.GetComponent<Renderer>();
                if (renderer == null) return JsonUtility.ToJson(new BasicRes { error = "Renderer not found." });
                if (idx < 0 || idx >= renderer.sharedMaterials.Length) return JsonUtility.ToJson(new BasicRes { error = "Index out of range." });
                Material mat = renderer.sharedMaterials[idx];
                if (mat == null) return JsonUtility.ToJson(new BasicRes { error = "Material null." });
                if (!mat.HasProperty(field)) return JsonUtility.ToJson(new BasicRes { error = "Property not found: " + field });
                Texture tex = mat.GetTexture(field);
                if (tex == null) return JsonUtility.ToJson(new BasicRes { message = "None" });
                string assetPath = AssetDatabase.GetAssetPath(tex);
                return JsonUtility.ToJson(new BasicRes { message = assetPath });
            } catch (Exception e) { return JsonUtility.ToJson(new BasicRes { error = e.Message }); }
        }

        [VibeTool("material/swap-to-quest-shaders", "Swaps all materials on a target to VRChat Mobile shaders.", "path")]
        public static string VibeTool_material_swap_to_quest_shaders(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Target not found" });

            Shader questShader = ShaderUtils.FindShader("VRChat/Mobile/Toon Lit", "Mobile/Diffuse");
            if (questShader == null) return JsonUtility.ToJson(new BasicRes { error = "No mobile-safe shader found." });

            int count = 0;
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers) {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) {
                    if (mats[i] != null && mats[i].shader != questShader) {
                        Undo.RecordObject(mats[i], "Swap to Quest Shader");
                        ShaderUtils.MigrateMaterialProperties(mats[i], questShader);
                        count++;
                    }
                }
            }
            return JsonUtility.ToJson(new BasicRes { message = $"Swapped {count} materials to {questShader.name}" });
        }

        [VibeTool("material/list", "Lists all material slots and assigned materials on a target.", "path")]
        public static string VibeTool_material_list(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            var r = go?.GetComponent<Renderer>();
            if (r == null) return JsonUtility.ToJson(new BasicRes { error = "No renderer" });
            var nodes = r.sharedMaterials.Select((m, i) => new MatListRes.MatNode { index = i, name = m != null ? m.name : "null" }).ToArray();
            return JsonUtility.ToJson(new MatListRes { materials = nodes });
        }

        [VibeTool("material/inspect-properties", "Returns all shader properties for a material slot.", "path", "index")]
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

        [VibeTool("material/set-color", "Sets common color properties (_Color, _BaseColor, etc) on a material.", "path", "index", "color")]
        public static string VibeTool_material_set_color(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            var r = go?.GetComponent<Renderer>();
            int idx = int.Parse(q["index"]);
            if (r == null || idx >= r.sharedMaterials.Length) return JsonUtility.ToJson(new BasicRes { error = "Invalid target" });
            
            var p = q["color"].Split(',').Select(float.Parse).ToArray();
            Color col = new Color(p[0], p[1], p[2], p.Length > 3 ? p[3] : 1f);
            var m = r.sharedMaterials[idx];
            if (m == null) return JsonUtility.ToJson(new BasicRes { error = "Material is null" });
            
            Undo.RecordObject(m, "Set Color");
            string[] targets = { "_Color", "_BaseColor", "_MainColor", "_EmissionColor" };
            foreach (var t in targets) if (m.HasProperty(t)) m.SetColor(t, col);
            
            return JsonUtility.ToJson(new BasicRes { message = "Color updated" });
        }

        [VibeTool("material/set-texture", "Assigns a texture asset to a specific shader property.", "path", "index", "field", "texture")]
        public static string VibeTool_material_set_texture(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            var r = go?.GetComponent<Renderer>();
            int idx = int.Parse(q["index"]);
            string field = q["field"], texPath = q["texture"];
            if (r == null || idx >= r.sharedMaterials.Length) return JsonUtility.ToJson(new BasicRes { error = "Invalid target" });
            
            Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
            if (tex == null && !string.IsNullOrEmpty(texPath)) return JsonUtility.ToJson(new BasicRes { error = "Texture not found" });
            
            var m = r.sharedMaterials[idx];
            if (m == null) return JsonUtility.ToJson(new BasicRes { error = "Material is null" });
            Undo.RecordObject(m, "Set Texture");
            m.SetTexture(field, tex);
            
            return JsonUtility.ToJson(new BasicRes { message = "Texture updated" });
        }

        [VibeTool("material/set-float", "Sets a float or range property on a material.", "path", "index", "field", "value")]
        public static string VibeTool_material_set_float(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            var r = go?.GetComponent<Renderer>();
            int idx = int.Parse(q["index"]);
            if (r == null || idx >= r.sharedMaterials.Length) return JsonUtility.ToJson(new BasicRes { error = "Invalid target" });
            
            var m = r.sharedMaterials[idx];
            if (m == null) return JsonUtility.ToJson(new BasicRes { error = "Material is null" });
            Undo.RecordObject(m, "Set Float");
            m.SetFloat(q["field"], float.Parse(q["value"]));
            return JsonUtility.ToJson(new BasicRes { message = "Float updated" });
        }

        [VibeTool("material/toggle-keyword", "Enables or disables a shader keyword on a material.", "path", "index", "keyword", "state")]
        public static string VibeTool_material_toggle_keyword(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            var r = go?.GetComponent<Renderer>();
            int idx = int.Parse(q["index"]);
            if (r == null || idx >= r.sharedMaterials.Length) return JsonUtility.ToJson(new BasicRes { error = "Invalid target" });
            
            var m = r.sharedMaterials[idx];
            if (m == null) return JsonUtility.ToJson(new BasicRes { error = "Material is null" });
            string kw = q["keyword"];
            bool state = q["state"].ToLower() == "true";
            
            Undo.RecordObject(m, "Toggle Keyword");
            if (state) m.EnableKeyword(kw);
            else m.DisableKeyword(kw);
            
            return JsonUtility.ToJson(new BasicRes { message = $"Keyword {kw} set to {state}" });
        }

        [VibeTool("material/sync-slots", "Copies all properties from one material slot to another.", "path", "srcIdx", "dstIdx")]
        public static string VibeTool_material_sync_slots(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            var r = go?.GetComponent<Renderer>();
            if (r == null) return JsonUtility.ToJson(new BasicRes { error = "No renderer" });
            int srcIdx = int.Parse(q["srcIdx"]), dstIdx = int.Parse(q["dstIdx"]);
            if (srcIdx >= r.sharedMaterials.Length || dstIdx >= r.sharedMaterials.Length) return JsonUtility.ToJson(new BasicRes { error = "Invalid indices" });
            
            Material srcMat = r.sharedMaterials[srcIdx];
            Material dstMat = r.sharedMaterials[dstIdx];
            if (srcMat == null || dstMat == null) return JsonUtility.ToJson(new BasicRes { error = "Material is null" });
            
            Undo.RecordObject(dstMat, "Sync Material Slots");
            dstMat.CopyPropertiesFromMaterial(srcMat);
            dstMat.shader = srcMat.shader;
            
            EditorUtility.SetDirty(dstMat);
            AssetDatabase.SaveAssets();
            return JsonUtility.ToJson(new BasicRes { message = $"Synced slot {srcIdx} to {dstIdx}" });
        }

        [VibeTool("material/assign", "Assigns a material asset to a specific renderer slot.", "path", "index", "material")]
        public static string VibeTool_material_assign(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            var r = go?.GetComponent<Renderer>();
            int idx = int.Parse(q["index"]);
            string matName = q["material"];
            
            if (r == null || idx >= r.sharedMaterials.Length) return JsonUtility.ToJson(new BasicRes { error = "Invalid target" });
            
            Material newMat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(matName + " t:Material").FirstOrDefault()));
            if (newMat == null) return JsonUtility.ToJson(new BasicRes { error = "Material not found: " + matName });
            
            Undo.RecordObject(r, "Assign Material");
            Material[] mats = r.sharedMaterials;
            mats[idx] = newMat;
            r.sharedMaterials = mats;
            
            return JsonUtility.ToJson(new BasicRes { message = "Material " + matName + " assigned to slot " + idx });
        }

        [VibeTool("material/snapshot", "Takes a persistent snapshot of all materials on an avatar.", "path")]
        public static string VibeTool_material_snapshot(Dictionary<string, string> q) {
            GameObject root = Resolve(q["path"]);
            if (root == null) return JsonUtility.ToJson(new BasicRes { error = "Root not found" });
            return TakeAutoSnapshot(root);
        }

        private static string TakeAutoSnapshot(GameObject root) {
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

        [VibeTool("material/restore", "Restores materials from a previously saved snapshot.", "path")]
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

        [VibeTool("material/fix-broken-mat", "Attempts to fix pink (broken) shaders by migrating to Standard/URP fallback.", "path")]
        public static string VibeTool_material_fix_broken_mat(Dictionary<string, string> q) {
            GameObject obj = Resolve(q["path"]);
            if (obj == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });

            TakeAutoSnapshot(obj);

            Shader safeShader = ShaderUtils.FindShader("Standard", "Universal Render Pipeline/Lit");
            if (safeShader == null) safeShader = ShaderUtils.FindShader("Mobile/Diffuse");
            if (safeShader == null) return JsonUtility.ToJson(new BasicRes { error = "No valid fallback shader found in project." });

            int count = 0;
            foreach (var r in obj.GetComponentsInChildren<Renderer>(true)) {
                bool changed = false;
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) {
                    if (mats[i] == null) continue;
                    if (mats[i].shader == null || mats[i].shader.name == "Hidden/InternalErrorShader" || mats[i].shader.name == "") {
                        Undo.RecordObject(mats[i], "Fix Broken Material");
                        ShaderUtils.MigrateMaterialProperties(mats[i], safeShader);
                        EditorUtility.SetDirty(mats[i]);
                        count++;
                        changed = true;
                    }
                }
                if (changed) r.sharedMaterials = mats;
            }
            
            return JsonUtility.ToJson(new BasicRes { message = $"Fixed {count} broken materials using {safeShader.name}" });
        }

        [VibeTool("material/hide-slot", "Makes a material slot transparent to effectively 'hide' it.", "path", "index")]
        public static string VibeTool_material_hide_slot(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            var r = go?.GetComponent<Renderer>();
            if (r == null) return JsonUtility.ToJson(new BasicRes { error = "No renderer" });
            int idx = int.Parse(q["index"]);
            if (idx >= r.sharedMaterials.Length) return JsonUtility.ToJson(new BasicRes { error = "Invalid index" });

            Material m = r.sharedMaterials[idx];
            if (m == null) return JsonUtility.ToJson(new BasicRes { error = "Material slot is empty." });

            Undo.RecordObject(m, "Hide Material Slot");
            if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 3); 
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0);
            
            m.SetColor("_Color", new Color(0,0,0,0));
            m.renderQueue = 3000;
            m.EnableKeyword("_ALPHABLEND_ON");
            
            EditorUtility.SetDirty(m);
            return JsonUtility.ToJson(new BasicRes { message = $"Slot {idx} hidden by transparency." });
        }

        [VibeTool("material/poiyomi-lock", "Triggers the Poiyomi Shader Optimizer to lock materials.", "path")]
        public static string VibeTool_material_poiyomi_lock(Dictionary<string, string> q) {
            return ProcessPoiyomiLock(q, true);
        }

        [VibeTool("material/poiyomi-unlock", "Triggers the Poiyomi Shader Optimizer to unlock materials.", "path")]
        public static string VibeTool_material_poiyomi_unlock(Dictionary<string, string> q) {
            return ProcessPoiyomiLock(q, false);
        }

        private static string ProcessPoiyomiLock(Dictionary<string, string> q, bool isLocking) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Target object not found" });
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            var matsToProcess = new List<Material>();
            foreach (var r in renderers) {
                foreach (var m in r.sharedMaterials) {
                    if (m != null && m.shader != null && m.shader.name.Contains("Poiyomi")) {
                        if (isLocking && !m.shader.name.Contains("Locked")) matsToProcess.Add(m);
                        if (!isLocking && m.shader.name.Contains("Locked")) matsToProcess.Add(m);
                    }
                }
            }
            if (matsToProcess.Count == 0) return JsonUtility.ToJson(new BasicRes { message = "No materials found to " + (isLocking ? "lock" : "unlock") + "." });
            try {
                System.Type optimizerType = null;
                foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies()) {
                    optimizerType = a.GetType("Thry.ShaderOptimizer") ?? a.GetType("Thry.ThryEditor.ShaderOptimizer");
                    if (optimizerType != null) break;
                }

                if (optimizerType == null) return JsonUtility.ToJson(new BasicRes { error = "Thry ShaderOptimizer not found." });
                
                string methodName = isLocking ? "LockMaterials" : "UnlockMaterials";
                System.Reflection.MethodInfo method = optimizerType.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                if (method == null) return JsonUtility.ToJson(new BasicRes { error = methodName + " method not found." });
                
                method.Invoke(null, new object[] { matsToProcess.ToArray(), 0 }); // 0 = ProgressBar.None

                return JsonUtility.ToJson(new BasicRes { message = (isLocking ? "Locked " : "Unlocked ") + matsToProcess.Count + " materials." });
            } catch (Exception e) { return JsonUtility.ToJson(new BasicRes { error = "Operation failed: " + e.Message }); }
        }
    }
}
#endif
