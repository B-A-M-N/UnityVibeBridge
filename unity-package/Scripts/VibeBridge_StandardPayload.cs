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
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityVibeBridge.Kernel.Core;

namespace UnityVibeBridge.Kernel {
    public static partial class VibeBridgeServer {

        public static string ExecuteStandardTool(string toolName, Dictionary<string, string> q) {
            try {
                AsyncUtils.SwitchToMainThreadSafe();
            } catch (Exception e) {
                return JsonUtility.ToJson(new BasicRes { error = $"Concurrency Failure: {e.Message}" });
            }

            // --- DYNAMIC DISPATCH (REplaces manual switch) ---
            var method = typeof(VibeBridgeServer).GetMethod(toolName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            
            if (method != null) {
                return (string)method.Invoke(null, new object[] { q });
            }

            return JsonUtility.ToJson(new BasicRes { error = "Tool not found in Standard Payload: " + toolName, conclusion = "DISPATCH_FAILURE" });
        }
        
        [VibeTool("object/set-active", "Sets the active state of a GameObject.", "path", "active")]
        public static string VibeTool_object_set_active(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Object not found" });
            bool state = q.ContainsKey("active") && q["active"].ToLower() == "true";
            Undo.RecordObject(go, (state ? "Activate " : "Deactivate ") + go.name);
            go.SetActive(state);
            return JsonUtility.ToJson(new BasicRes { message = "Object " + (state ? "activated" : "deactivated") });
        }

        [VibeTool("object/set-blendshape", "Sets the weight of a blendshape on a SkinnedMeshRenderer.", "path", "name", "value")]
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

        [VibeTool("world/spawn", "Instantiates a prefab from the project at a given position and rotation.", "asset", "pos", "rot")]
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

        [VibeTool("world/spawn-primitive", "Creates a standard Unity primitive (Cube, Sphere, etc).", "type", "pos")]
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

        [VibeTool("world/static-list", "Returns all available Static Editor Flags.")]
        public static string VibeTool_world_static_list(Dictionary<string, string> q) {
            var names = Enum.GetNames(typeof(StaticEditorFlags));
            var values = Enum.GetValues(typeof(StaticEditorFlags));
            var list = new List<StaticFlagRes.StaticFlagNode>();
            for (int i = 0; i < names.Length; i++) {
                list.Add(new StaticFlagRes.StaticFlagNode { name = names[i], value = (int)values.GetValue(i) });
            }
            return JsonUtility.ToJson(new StaticFlagRes { flags = list.ToArray() });
        }

        [VibeTool("world/static-set", "Sets the Static Editor Flags for a GameObject.", "path", "flags")]
        public static string VibeTool_world_static_set(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            StaticEditorFlags flags = (StaticEditorFlags)int.Parse(q["flags"]);
            GameObjectUtility.SetStaticEditorFlags(go, flags);
            return JsonUtility.ToJson(new BasicRes { message = "Static flags updated" });
        }

        [VibeTool("asset/move", "Moves an asset to a new path in the project.", "path", "newPath")]
        public static string VibeTool_asset_move(Dictionary<string, string> q) {
            string err = AssetDatabase.MoveAsset(q["path"], q["newPath"]);
            return string.IsNullOrEmpty(err) ? JsonUtility.ToJson(new BasicRes { message = "Asset moved" }) : JsonUtility.ToJson(new BasicRes { error = err });
        }

        [VibeTool("asset/rename", "Renames an existing asset file.", "path", "newName")]
        public static string VibeTool_asset_rename(Dictionary<string, string> q) {
            string err = AssetDatabase.RenameAsset(q["path"], q["newName"]);
            return string.IsNullOrEmpty(err) ? JsonUtility.ToJson(new BasicRes { message = "Asset renamed" }) : JsonUtility.ToJson(new BasicRes { error = err });
        }

        [VibeTool("prefab/apply", "Applies all overrides on a prefab instance to its source asset.", "path")]
        public static string VibeTool_prefab_apply(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null || !PrefabUtility.IsPartOfAnyPrefab(go)) return JsonUtility.ToJson(new BasicRes { error = "Not a prefab instance" });
            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            PrefabUtility.ApplyPrefabInstance(root, InteractionMode.UserAction);
            return JsonUtility.ToJson(new BasicRes { message = "Changes applied to prefab: " + root.name });
        }

        [VibeTool("view/screenshot", "Captures a screenshot of the active SceneView and returns it as Base64.", "w", "h")]
        public static string VibeTool_view_screenshot(Dictionary<string, string> q) {
            if (SceneView.lastActiveSceneView == null) return JsonUtility.ToJson(new BasicRes { error = "No active SceneView found." });
            Camera cam = SceneView.lastActiveSceneView.camera;
            if (cam == null) return JsonUtility.ToJson(new BasicRes { error = "No camera on SceneView" });
            
            int w = q.ContainsKey("w") ? int.Parse(q["w"]) : 1280, h = q.ContainsKey("h") ? int.Parse(q["h"]) : 720;
            
            // VISION HARDENING: Limit resolution to prevent Base64 buffer overflow (approx 4MB limit)
            if (w * h > 1000000) { // If > 1MP, downscale
                float scale = Mathf.Sqrt(1000000f / (w * h));
                w = Mathf.RoundToInt(w * scale);
                h = Mathf.RoundToInt(h * scale);
            }

            RenderTexture rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            Texture2D ss = new Texture2D(w, h, TextureFormat.RGB24, false);
            cam.Render();
            RenderTexture.active = rt;
            ss.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            cam.targetTexture = null;
            RenderTexture.active = null;
            byte[] bytes = ss.EncodeToJPG(75); // Use JPG for better compression
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(ss);

            return "{\"base64\":\"" + Convert.ToBase64String(bytes) + "\", \"tick\":" + _monotonicTick + ", \"state\":\"" + _lastAuditHash + "\"}";
        }

        [VibeTool("material/batch-replace", "Replaces the material on multiple objects at once.", "paths", "material")]
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

        [VibeTool("system/find-by-component", "Locates all GameObjects in the scene with a specific component type.", "type")]
        public static string VibeTool_system_find_by_component(Dictionary<string, string> q) {
            string typeName = q["type"];
            var all = UnityEngine.Object.FindObjectsOfType<GameObject>(true);
            var results = all.Where(go => go.GetComponent(typeName) != null)
                .Select(go => new BasicRes { message = go.name, id = go.GetInstanceID() }).ToArray();
            return JsonUtility.ToJson(new FindRes { results = results });
        }

        [VibeTool("system/search", "Searches GameObjects by name (regex) or layer.", "name", "layer")]
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

        [VibeTool("system/git-checkpoint", "Creates a git commit checkpoint for the project.", "message", "path")]
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

        [VibeTool("animator/set-param", "Sets a parameter value on an Animator.", "path", "name", "type", "value")]
        public static string VibeTool_animator_set_param(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            var animator = go?.GetComponent<Animator>();
            if (animator == null) return JsonUtility.ToJson(new BasicRes { error = "Animator not found" });

            string name = q["name"];
            string type = q["type"].ToLower();
            string val = q["value"];

            Undo.RecordObject(animator, "Set Animator Param");
            switch (type) {
                case "float": animator.SetFloat(name, float.Parse(val)); break;
                case "int": animator.SetInteger(name, int.Parse(val)); break;
                case "bool": animator.SetBool(name, val.ToLower() == "true"); break;
                case "trigger": animator.SetTrigger(name); break;
                default: return JsonUtility.ToJson(new BasicRes { error = "Invalid param type" });
            }
            return JsonUtility.ToJson(new BasicRes { message = $"Param {name} set to {val}" });
        }

        [VibeTool("system/reset-transforms", "Resets position to 0, rotation to 0, and scale to 1,1,1 (Reinforced Rig Safety).", "path")]
        public static string VibeTool_system_reset_transforms(Dictionary<string, string> q) {
            try {
                GameObject go = Resolve(q["path"]);
                if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Target not found." });
                Debug.Log($"[VibeBridge] Starting Rig-Safe Reset on {go.name}");
                
                // --- CONTRACT ENFORCEMENT: IDENTIFY EXCLUSION ZONE (BONES) ---
                var blacklist = RigSafetyGate.GetRigBones(go);

                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Reinforced Rig-Safe Reset");
                int group = Undo.GetCurrentGroup();

                // 1. Reset the Root (Explicitly allowed by Contract Rule 2)
                Undo.RecordObject(go.transform, "Reset Root");
                go.transform.localPosition = Vector3.zero;
                go.transform.localEulerAngles = Vector3.zero;
                go.transform.localScale = Vector3.one;

                // 2. Targeted traversal (MESHES ONLY)
                var allTransforms = go.GetComponentsInChildren<Transform>(true);
                int count = 0;
                int skipped = 0;
                foreach (var t in allTransforms) {
                    if (t == go.transform) continue;
                    
                    // --- THE FIREWALL (Contract Rule 1) ---
                    if (blacklist.Contains(t)) {
                        skipped++;
                        continue; 
                    }

                    // Safety Check: We ONLY reset objects that are explicitly Renderers (Meshes).
                    // This prevents us from accidentally resetting armature root or other non-mesh utility nodes.
                    if (t.GetComponent<Renderer>() == null) {
                        skipped++;
                        continue;
                    }

                    Undo.RecordObject(t, "Reset Mesh Container");
                    t.localPosition = Vector3.zero;
                    t.localEulerAngles = Vector3.zero;
                    t.localScale = Vector3.one;
                    count++;
                }
                
                Undo.CollapseUndoOperations(group);
                Debug.Log($"[VibeBridge] Reset {count} meshes, skipped {skipped} rig objects.");
                return JsonUtility.ToJson(new BasicRes { message = "Transforms reset (MAX_SAFETY_ENFORCED)." });
            } catch (Exception e) { return JsonUtility.ToJson(new BasicRes { error = e.Message }); }
        }

        [VibeTool("system/reset-blendshapes", "Resets all blendshapes on a target and its children to 0.", "path")]
        public static string VibeTool_system_reset_blendshapes(Dictionary<string, string> q) {
            try {
                GameObject go = Resolve(q["path"]);
                if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Target not found." });
                
                var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                int count = 0;
                foreach (var smr in smrs) {
                    Undo.RecordObject(smr, "Reset BlendShapes");
                    for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++) {
                        smr.SetBlendShapeWeight(i, 0);
                    }
                    count++;
                }
                
                return JsonUtility.ToJson(new BasicRes { message = $"Reset blendshapes on {count} SkinnedMeshRenderers." });
            } catch (Exception e) { return JsonUtility.ToJson(new BasicRes { error = e.Message }); }
        }

        [VibeTool("export/validate", "Checks for scale sanity, non-zero rotations, and missing scripts.", "path")]
        public static string VibeTool_export_validate(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Target not found" });

            var issues = new List<string>();
            if (Math.Abs(go.transform.localScale.x - 1.0f) > 0.01f || Math.Abs(go.transform.localScale.y - 1.0f) > 0.01f || Math.Abs(go.transform.localScale.z - 1.0f) > 0.01f) {
                issues.Add("Root scale is not 1,1,1.");
            }
            if (go.transform.localEulerAngles.sqrMagnitude > 0.01f) {
                issues.Add("Root rotation is not 0,0,0.");
            }
            var all = go.GetComponentsInChildren<Component>(true);
            int missingCount = all.Count(c => c == null);
            if (missingCount > 0) issues.Add($"{missingCount} missing scripts found.");

            return JsonUtility.ToJson(new IntegrityReport {
                passed = issues.Count == 0,
                issues = issues.ToArray()
            });
        }

        [VibeTool("object/set-value", "Sets a field or property value on a component using reflection.", "path", "component", "field", "value")]
        public static string VibeTool_object_set_value(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Object not found" });
            string compName = q["component"], fieldName = q["field"], val = q["value"];
            Component c = go.GetComponent(compName);
            if (c == null) return JsonUtility.ToJson(new BasicRes { error = "Component not found" });
            Undo.RecordObject(c, "Set Value");
            var type = c.GetType();
            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            var prop = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
            try {
                object parsedVal = null;
                var targetType = field?.FieldType ?? prop?.PropertyType;
                if (targetType == typeof(Vector3)) {
                    var p = val.Split(',').Select(float.Parse).ToArray();
                    parsedVal = new Vector3(p[0], p[1], p[2]);
                } else if (targetType == typeof(Color)) {
                    var p = val.Split(',').Select(float.Parse).ToArray();
                    parsedVal = new Color(p[0], p[1], p[2], p.Length > 3 ? p[3] : 1f);
                } else { parsedVal = Convert.ChangeType(val, targetType); }
                if (field != null) field.SetValue(c, parsedVal); else prop.SetValue(c, parsedVal);
                return JsonUtility.ToJson(new BasicRes { message = "Value updated" });
            } catch (Exception e) { return JsonUtility.ToJson(new BasicRes { error = e.Message }); }
        }

        [VibeTool("object/rename", "Renames a GameObject.", "path", "newName")]
        public static string VibeTool_object_rename(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            Undo.RecordObject(go, "Rename"); 
            go.name = q["newName"]; 
            return JsonUtility.ToJson(new BasicRes { message = "Renamed" });
        }

        [VibeTool("object/reparent", "Changes the parent of a GameObject.", "path", "newParent")]
        public static string VibeTool_object_reparent(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]), p = q.ContainsKey("newParent") ? Resolve(q["newParent"]) : null; 
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Target not found" });
            Undo.SetTransformParent(go.transform, p != null ? p.transform : null, "Reparent"); 
            return JsonUtility.ToJson(new BasicRes { message = "Reparented" });
        }

        [VibeTool("object/clone", "Duplicates a GameObject.", "path")]
        public static string VibeTool_object_clone(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            GameObject c = UnityEngine.Object.Instantiate(go); 
            c.name = go.name + "_Clone"; 
            Undo.RegisterCreatedObjectUndo(c, "Clone"); 
            return JsonUtility.ToJson(new BasicRes { message = "Cloned", id = c.GetInstanceID() });
        }

        [VibeTool("object/delete", "Deletes a GameObject.", "path")]
        public static string VibeTool_object_delete(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            Undo.DestroyObjectImmediate(go); 
            return JsonUtility.ToJson(new BasicRes { message = "Deleted" });
        }

        [VibeTool("system/select", "Selects a GameObject in the editor.", "path", "frame")]
        public static string VibeTool_system_select(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            Selection.activeGameObject = go;
            bool forceFrame = q.ContainsKey("frame") && q["frame"].ToLower() == "true";
            if (forceFrame || SceneView.lastActiveSceneView == null || !SceneView.lastActiveSceneView.hasFocus) SceneView.FrameLastActiveSceneView(); 
            return JsonUtility.ToJson(new BasicRes { message = "Selected" });
        }

        [VibeTool("system/veto", "Manually locks the bridge, preventing all further mutations.")]
        public static string VibeTool_system_veto(Dictionary<string, string> q) {
            _isVetoed = true;
            SetStatus("VETOED");
            Debug.LogError("[Vibe] Human Veto Triggered. Bridge Locked.");
            return JsonUtility.ToJson(new BasicRes { message = "Bridge Vetoed" });
        }

        [VibeTool("system/unveto", "Unlocks the bridge after a veto.")]
        public static string VibeTool_system_unveto(Dictionary<string, string> q) {
            _isVetoed = false;
            _violationCount = 0;
            _panicMode = false;
            SetStatus("Ready");
            Debug.Log("[Vibe] Bridge Unlocked.");
            return JsonUtility.ToJson(new BasicRes { message = "Bridge Unlocked" });
        }

        [VibeTool("system/execute-recipe", "Executes a batch of tools in a single request.", "recipe")]
        public static string VibeTool_system_execute_recipe(Dictionary<string, string> q) {
            string recipeJson = q["recipe"];
            try {
                var recipe = JsonUtility.FromJson<RecipeCommand>("{\"tools\":" + recipeJson + "}");
                var results = new List<string>();
                foreach (var tool in recipe.tools) {
                    results.Add(ExecuteAirlockCommand(tool));
                }
                return "[" + string.Join(",", results) + "]";
            } catch (Exception e) {
                return JsonUtility.ToJson(new BasicRes { error = "Recipe execution failed: " + e.Message });
            }
        }

                [VibeTool("system/undo", "Performs an Undo operation in Unity.")]
                public static string VibeTool_system_undo(Dictionary<string, string> q) { Undo.PerformUndo(); return JsonUtility.ToJson(new BasicRes { message = "Undo" }); }
        
                [VibeTool("system/redo", "Performs a Redo operation in Unity.")]
                public static string VibeTool_system_redo(Dictionary<string, string> q) { Undo.PerformRedo(); return JsonUtility.ToJson(new BasicRes { message = "Redo" }); }
        
                [VibeTool("system/list-tools", "Returns a list of all available VibeBridge tools.")]
                public static string VibeTool_system_list_tools(Dictionary<string, string> q) {
                    var tools = new List<BasicRes>();
                    var methods = typeof(VibeBridgeServer).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var m in methods) {
                        if (m.Name.StartsWith("VibeTool_")) {
                            var attr = m.GetCustomAttribute<VibeToolAttribute>();
                            tools.Add(new BasicRes { 
                                message = attr != null ? attr.Name : m.Name.Replace("VibeTool_", "").Replace("_", "/"),
                                error = attr != null ? attr.Description : ""
                            });
                        }
                    }
                    return JsonUtility.ToJson(new FindRes { results = tools.ToArray() });
                }
        
                [VibeTool("transaction/begin", "Starts a new undo group for atomic operations.", "name")]
                public static string VibeTool_transaction_begin(Dictionary<string, string> q) {
                    Undo.IncrementCurrentGroup();
                    Undo.SetCurrentGroupName(q.ContainsKey("name") ? q["name"] : "AI Op");
                    return JsonUtility.ToJson(new BasicRes { message = "Started", id = Undo.GetCurrentGroup() });
                }
        
                [VibeTool("transaction/commit", "Collapses all operations in the current undo group.")]
                public static string VibeTool_transaction_commit(Dictionary<string, string> q) {
                    Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                    return JsonUtility.ToJson(new BasicRes { message = "Committed" });
                }
        
                [VibeTool("transaction/abort", "Reverts all operations in the current undo group.")]
                public static string VibeTool_transaction_abort(Dictionary<string, string> q) {
                    Undo.RevertAllDownToGroup(Undo.GetCurrentGroup());
                    return JsonUtility.ToJson(new BasicRes { message = "Aborted" });
                }
        
                [VibeTool("status", "Returns the current kernel status.")]
                public static string VibeTool_status(Dictionary<string, string> q) {
                    return "{\"status\":\"connected\",\"kernel\":\"v1.2.5\",\"vetoed\":" + _isVetoed.ToString().ToLower() + "}";
                }            
                }
            
            }
            
            #endif