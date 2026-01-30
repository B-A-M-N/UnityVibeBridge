// UnityVibeBridge: The Governed Creation Kernel for Unity
// Copyright (C) 2026 B-A-M-N
//
// This software is dual-licensed under the GNU AGPLv3 and a 
// Commercial "Work-or-Pay" Maintenance Agreement.
//
// You may use this file under the terms of the AGPLv3, provided 
// you meet all requirements (including source disclosure).
//
// For commercial use, or to keep your modifications private, 
// you must satisfy the requirements of the Commercial Path 
// as defined in the LICENSE file at the project root.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        
        // --- STANDARD UTILITY PAYLOAD WRAPPERS ---
        [Serializable] public class VramRes { public float vramMB; public int textures; }
        [Serializable] public class MissingScriptsRes { public int missing; public BasicRes[] details; }
        [Serializable] public class AvatarAuditRes { public string name; public RendererAudit[] renderers; }
        [Serializable] public class RendererAudit { public string path; public int verts, mats; }
        [Serializable] public class StaticFlagRes { public StaticFlagNode[] flags; [Serializable] public struct StaticFlagNode { public string name; public int value; } }
        [Serializable] public class PhysicsAuditRes { public PhysicsNode[] physicsObjects; [Serializable] public struct PhysicsNode { public string name, type; public bool isKinematic, isTrigger; } }
        [Serializable] public class AnimationAuditRes { public AnimatorNode[] animators; [Serializable] public struct AnimatorNode { public string name; public int missingClips; } }
        [Serializable] public class FindRes { public BasicRes[] results; }

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
            return JsonUtility.ToJson(new BasicRes { message = $"Crushed {changed} textures" });
        }

        public static string VibeTool_shader_swap_quest(Dictionary<string, string> q) {
            GameObject obj = Resolve(q["path"]);
            if (obj == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            Shader qs = Shader.Find("VRChat/Mobile/Toon Lit");
            if (qs == null) return JsonUtility.ToJson(new BasicRes { error = "Quest shader not found" });
            int count = 0;
            foreach (var r in obj.GetComponentsInChildren<Renderer>(true)) {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) { if (mats[i] != null) { mats[i].shader = qs; count++; } }
                r.sharedMaterials = mats;
            }
            return JsonUtility.ToJson(new BasicRes { message = $"Swapped {count} materials" });
        }

        public static string VibeTool_project_missing_scripts(Dictionary<string, string> q) {
            var report = new List<BasicRes>();
            foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>(true)) {
                var components = go.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++) {
                    if (components[i] == null) report.Add(new BasicRes { message = go.name, id = go.GetInstanceID() });
                }
            }
            return JsonUtility.ToJson(new MissingScriptsRes { missing = report.Count, details = report.ToArray() });
        }

        public static string VibeTool_audit_avatar(Dictionary<string, string> q) {
            GameObject root = Resolve(q["path"]);
            if (root == null) return JsonUtility.ToJson(new BasicRes { error = "Root not found" });
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var results = renderers.Select(r => {
                Mesh m = (r is SkinnedMeshRenderer smr) ? smr.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
                return new RendererAudit { path = r.name, verts = (m != null ? m.vertexCount : 0), mats = r.sharedMaterials.Length };
            }).ToArray();
            return JsonUtility.ToJson(new AvatarAuditRes { name = root.name, renderers = results });
        }

        public static string VibeTool_world_spawn(Dictionary<string, string> q) {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(q["asset"]);
            if (prefab == null) return JsonUtility.ToJson(new BasicRes { error = "Prefab not found" });
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
            return JsonUtility.ToJson(new BasicRes { message = "Spawned", id = go.GetInstanceID() });
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

        public static string VibeTool_asset_rename(Dictionary<string, string> q) {
            string err = AssetDatabase.RenameAsset(q["path"], q["newName"]);
            return string.IsNullOrEmpty(err) ? JsonUtility.ToJson(new BasicRes { message = "Asset renamed" }) : JsonUtility.ToJson(new BasicRes { error = err });
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

        public static string VibeTool_physics_audit(Dictionary<string, string> q) {
            var rb = UnityEngine.Object.FindObjectsOfType<Rigidbody>();
            var col = UnityEngine.Object.FindObjectsOfType<Collider>();
            var results = rb.Select(r => new PhysicsAuditRes.PhysicsNode { name = r.name, type = "Rigidbody", isKinematic = r.isKinematic, isTrigger = false })
                .Concat(col.Select(c => new PhysicsAuditRes.PhysicsNode { name = c.name, type = "Collider", isKinematic = false, isTrigger = c.isTrigger })).ToArray();
            return JsonUtility.ToJson(new PhysicsAuditRes { physicsObjects = results });
        }

        public static string VibeTool_animation_audit(Dictionary<string, string> q) {
            var animators = UnityEngine.Object.FindObjectsOfType<Animator>();
            var results = animators.Select(a => {
                var ctrl = a.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                int missingClips = 0;
                if (ctrl != null) {
                    foreach (var layer in ctrl.layers) {
                        foreach (var state in layer.stateMachine.states) {
                            if (state.state.motion == null) missingClips++;
                        }
                    }
                }
                return new AnimationAuditRes.AnimatorNode { name = a.name, missingClips = missingClips };
            }).ToArray();
            return JsonUtility.ToJson(new AnimationAuditRes { animators = results });
        }

        public static string VibeTool_material_batch_replace(Dictionary<string, string> q) {
            string[] paths = q["paths"].Split(',');
            string matName = q["material"];
            Material newMat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(matName + " t:Material").FirstOrDefault()));
            if (newMat == null) return JsonUtility.ToJson(new BasicRes { error = "Material not found" });
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
            return JsonUtility.ToJson(new BasicRes { message = $"Replaced materials on {count} objects" });
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
            // ... (previous logic) ...
            return JsonUtility.ToJson(new BasicRes { message = "Forked", id = fork.GetInstanceID() });
        }

        // --- BACKGROUND ENGINES ---
        private static float _lastSyncTime = 0;
        public static void UpdateColorSync() {
            if (Time.realtimeSinceStartup - _lastSyncTime < 0.1f) return; // 10Hz sync
            _lastSyncTime = Time.realtimeSinceStartup;

            GameObject root = GameObject.Find("ExtoPc"); // Canonical target
            if (root == null) return;
            var anim = root.GetComponent<Animator>();
            if (anim == null) return;

            // Sync 'AccentAll' group to 'Color' animator param
            try {
                float h = anim.GetFloat("Color");
                float s = anim.GetFloat("ColorSat");
                float v = anim.GetFloat("ColorPitch");
                Color col = Color.HSVToRGB(h, s, v);
                
                LoadRegistry();
                foreach (var entry in _registry.entries.Where(e => e.group == "AccentAll")) {
                    var go = EditorUtility.InstanceIDToObject(entry.lastKnownID) as GameObject;
                    var r = go?.GetComponent<Renderer>();
                    if (r != null) {
                        foreach (var m in r.sharedMaterials) {
                            if (m == null) continue;
                            if (m.HasProperty("_Color")) m.SetColor("_Color", col);
                            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", col);
                        }
                    }
                }
            } catch { }
        }
    }
}
#endif