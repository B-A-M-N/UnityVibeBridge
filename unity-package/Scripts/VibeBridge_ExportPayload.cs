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
        [VibeTool("export/prepare-clone", "Clones and surgically strips an avatar for export inspection.", "path")]
        public static string VibeTool_export_prepare_clone(Dictionary<string, string> q) {
            try {
                GameObject go = Resolve(q["path"]);
                if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Target not found." });

                // 1. T-Pose Lockdown (Disable Animator to prevent warped clone)
                var animator = go.GetComponent<Animator>();
                bool wasEnabled = animator != null && animator.enabled;
                if (animator != null) animator.enabled = false;

                // 2. Duplicate
                GameObject exportClone = UnityEngine.Object.Instantiate(go);
                exportClone.name = go.name + "_STAGED_FOR_EXPORT";
                exportClone.transform.position += new Vector3(2, 0, 0); 

                // 3. Restore Original
                if (animator != null) animator.enabled = wasEnabled;

                // 4. Surgical Strip (Blacklist approach)
                var allComponents = exportClone.GetComponentsInChildren<Component>(true);
                foreach (var c in allComponents) {
                    if (c == null || c is Transform || c is MeshRenderer || c is SkinnedMeshRenderer || c is MeshFilter) continue;
                    
                    string typeName = c.GetType().Name;
                    bool shouldStrip = typeName.Contains("VRC") || 
                                     typeName.Contains("PhysBone") || 
                                     typeName.Contains("Constraint") || 
                                     typeName.Contains("Contact") ||
                                     typeName.Contains("Pipeline") ||
                                     typeName.Contains("ONSP");
                    
                    if (shouldStrip) {
                        UnityEngine.Object.DestroyImmediate(c);
                    }
                }

                Selection.activeGameObject = exportClone;
                return JsonUtility.ToJson(new BasicRes { 
                    message = $"SUCCESS: Prepared {exportClone.name}. It is offset by 2m. Check for warping.",
                    id = exportClone.GetInstanceID()
                });
            } catch (Exception e) { return JsonUtility.ToJson(new BasicRes { error = e.Message }); }
        }

        [VibeTool("export/fbx", "Exports a GameObject to FBX with full Material Manifest and Texture Collection (Safe Reflection Mode).", "path", "dest")]
        public static string VibeTool_export_fbx(Dictionary<string, string> q) {
            try {
                GameObject go = Resolve(q["path"]);
                if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Target not found." });
                
                string dest = q.ContainsKey("dest") ? q["dest"] : "Export_Blender/" + go.name + ".fbx";
                string fullPath = Path.Combine(Directory.GetCurrentDirectory(), dest);
                string exportDir = Path.GetDirectoryName(fullPath);
                
                if (!Directory.Exists(exportDir)) Directory.CreateDirectory(exportDir);

                // 3. Generate Material Manifest and Copy Textures
                Debug.Log("[VibeBridge] Generating Material Manifest and Collecting Textures...");
                var manifest = MaterialMapper.GenerateManifest(go, exportDir);
                string manifestPath = Path.Combine(exportDir, go.name + "_materials.json");
                File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true));

                // 4. Execute FBX Export via Reflection
                Debug.Log($"[VibeBridge] Searching for FBX Exporter...");
                
                Type exporterType = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    if (assembly.GetName().Name.Contains("Fbx.Editor") || assembly.GetName().Name.Contains("Fbx.Runtime")) {
                        exporterType = assembly.GetType("UnityEditor.Formats.Fbx.Exporter.ModelExporter") ?? 
                                       assembly.GetType("Unity.Formats.Fbx.Exporter.ModelExporter");
                        if (exporterType != null) break;
                    }
                }

                if (exporterType == null) {
                    return JsonUtility.ToJson(new BasicRes { 
                        error = "FBX Exporter package not found. Please install 'com.unity.formats.fbx'.",
                        conclusion = "PACKAGE_NOT_LOADED"
                    });
                }

                // Discovery for static method
                var exportMethod = exporterType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "Export" && m.GetParameters().Length == 2);

                if (exportMethod == null) {
                    exportMethod = exporterType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(m => m.Name == "ExportObject" && m.GetParameters().Length == 2);
                }

                if (exportMethod == null) {
                    return JsonUtility.ToJson(new BasicRes { error = "ModelExporter entry point not found." });
                }

                AssetDatabase.SaveAssets();
                Selection.activeGameObject = go;

                // UI HARDENING: The FBX Exporter expects the Settings Window to be initialized
                System.Type windowType = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    if (assembly.GetName().Name.Contains("Fbx.Editor")) {
                        windowType = assembly.GetType("UnityEditor.Formats.Fbx.Exporter.FbxExportSettingsWindow");
                        if (windowType != null) break;
                    }
                }

                if (windowType != null) {
                    EditorWindow.GetWindow(windowType, false, "FBX Exporter", false);
                }

                Debug.Log($"[VibeBridge] Executing UI-Hardened Export: {go.name}");
                
                // --- STEP 5: Surgical Clone & Strip (Fixes 'm_IsActive' and constraint errors) ---
                GameObject exportClone = UnityEngine.Object.Instantiate(go);
                exportClone.name = go.name + "_ExportInternal";
                
                var allComponents = exportClone.GetComponentsInChildren<Component>(true);
                foreach (var c in allComponents) {
                    if (c == null) continue;
                    
                    // The Whitelist: Keep ONLY what is needed for 3D Geometry and Rigging
                    // This surgically removes Scripts, PhysBones, and Constraints.
                    bool isEssential = c is Transform || 
                                     c is SkinnedMeshRenderer || 
                                     c is MeshRenderer || 
                                     c is MeshFilter;
                    
                    if (!isEssential) {
                        UnityEngine.Object.DestroyImmediate(c);
                    }
                }

                try {
                    Selection.activeGameObject = exportClone;
                    Debug.Log($"[VibeBridge] Invoking Sync Export on Surgical Clone: {exportClone.name}");
                    exportMethod.Invoke(null, new object[] { fullPath, exportClone });
                    
                    return JsonUtility.ToJson(new BasicRes { 
                        message = $"SUCCESS: Exported {go.name} to {dest}.",
                        conclusion = "EXPORT_COMPLETE"
                    });
                } catch (Exception e) {
                    return JsonUtility.ToJson(new BasicRes { 
                        error = "Export Failed (Sync): " + (e.InnerException?.Message ?? e.Message),
                        message = e.StackTrace
                    });
                } finally {
                    // Cleanup the clone
                    if (exportClone != null) UnityEngine.Object.DestroyImmediate(exportClone);
                }
            } catch (Exception e) { 
                return JsonUtility.ToJson(new BasicRes { 
                    error = "Export Failed: " + (e.InnerException?.Message ?? e.Message),
                    message = e.StackTrace
                }); 
            }
        }
    }
}
#endif