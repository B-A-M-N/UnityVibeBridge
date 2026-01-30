using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        
        // --- EXPORT MODULE ---
        // Handles safe data flow from Unity to external tools (Blender).

        public static string VibeTool_object_export_fbx(Dictionary<string, string> q) {
            EnforceGuard();
            
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            if (obj == null) return "{\"error\":\"Object not found\"}";

            string exportPath = q["exportPath"];
            if (string.IsNullOrEmpty(exportPath)) exportPath = "Assets/_Exported/" + obj.name + ".fbx";
            
            // Ensure directory exists
            string dir = Path.GetDirectoryName(exportPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            // 1. Validate for Blender Compatibility
            var validation = ValidateForExport(root: obj);
            if (validation.hasErrors && !q.ContainsKey("force")) {
                return "{\"error\":\"Export validation failed\",\"issues\":" + JsonUtility.ToJson(validation) + ",\"hint\":\"Use force=true to bypass\"}";
            }

            // 2. Execute Export (Requires Unity FBX Exporter package)
            try {
                var fbxExporterType = Type.GetType("UnityEditor.Formats.Fbx.Exporter.ModelExporter, Unity.Formats.Fbx.Editor");
                if (fbxExporterType == null) return "{\"error\":\"Unity FBX Exporter package missing. Please install it via Package Manager.\"}";

                var exportMethod = fbxExporterType.GetMethod("ExportObject", new Type[] { typeof(string), typeof(UnityEngine.Object) });
                if (exportMethod == null) return "{\"error\":\"FBX Exporter API mismatch. Could not find ExportObject.\"}";

                exportMethod.Invoke(null, new object[] { exportPath, obj });
                
                LogMutation("EXPORT", obj.name, "fbx_export", exportPath);
                return "{\"message\":\"Successfully exported to " + exportPath + ",\"validation\":" + JsonUtility.ToJson(validation) + "}";
            } catch (Exception e) {
                return "{\"error\":\"Export failed: " + e.Message + "}";
            }
        }

        public static string VibeTool_export_validate(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            if (obj == null) return "{\"error\":\"Object not found\"}";

            return JsonUtility.ToJson(ValidateForExport(obj));
        }

        private static ExportValidationReport ValidateForExport(GameObject root) {
            var report = new ExportValidationReport();
            
            // Check Scale
            if (Vector3.Distance(root.transform.localScale, Vector3.one) > 0.001f) {
                report.issues.Add("Root scale is not (1,1,1). This causes size mismatch in Blender.");
                report.hasErrors = true;
            }

            // Check Rotation
            if (Quaternion.Angle(root.transform.localRotation, Quaternion.identity) > 0.001f) {
                report.issues.Add("Root has non-zero rotation. FBX export may introduce -90 degree offsets.");
            }

            // Check for Missing Scripts
            var components = root.GetComponentsInChildren<Component>(true);
            foreach (var c in components) {
                if (c == null) {
                    report.issues.Add("Hierarchy contains missing scripts. Cleanup recommended before export.");
                    report.hasErrors = true;
                    break;
                }
            }

            return report;
        }

        [Serializable]
        public class ExportValidationReport {
            public bool hasErrors = false;
            public List<string> issues = new List<string>();
        }
    }
}
