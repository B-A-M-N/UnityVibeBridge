#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityVibeBridge.Kernel {
    public static partial class VibeBridgeServer {
        
        [VibeTool("vrc/menu/add", "Adds a control to a VRChat Expressions Menu asset.", "path", "name", "type", "parameter")]
        public static string VibeTool_vrc_menu_add(Dictionary<string, string> q) {
            string assetPath = ResolveAssetPath(q["path"], "t:VRCExpressionsMenu");
            if (string.IsNullOrEmpty(assetPath)) return JsonUtility.ToJson(new BasicRes { error = "Menu asset not found." });

            try {
                System.Type menuType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu"))
                    .FirstOrDefault(t => t != null);

                if (menuType == null) return JsonUtility.ToJson(new BasicRes { error = "VRChat SDK (VRCExpressionsMenu) not found." });

                ScriptableObject menuAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (menuAsset == null || menuAsset.GetType() != menuType) return JsonUtility.ToJson(new BasicRes { error = "Invalid menu asset type." });

                Undo.RecordObject(menuAsset, "Add VRC Menu Control");

                var controlsField = menuType.GetField("controls");
                var controls = controlsField.GetValue(menuAsset) as System.Collections.IList;
                if (controls == null) return JsonUtility.ToJson(new BasicRes { error = "Could not access menu controls list." });
                if (controls.Count >= 8) return JsonUtility.ToJson(new BasicRes { error = "Menu already has 8 controls (max)." });

                System.Type controlClass = menuType.GetNestedType("Control");
                object newControl = Activator.CreateInstance(controlClass);

                controlClass.GetField("name").SetValue(newControl, q["name"]);
                
                System.Type controlTypeEnum = controlClass.GetNestedType("ControlType");
                object typeValue = Enum.Parse(controlTypeEnum, q["type"], true);
                controlClass.GetField("type").SetValue(newControl, typeValue);

                var paramClass = controlClass.GetNestedType("Parameter");
                object paramObj = Activator.CreateInstance(paramClass);
                paramClass.GetField("name").SetValue(paramObj, q["parameter"]);
                controlClass.GetField("parameter").SetValue(newControl, paramObj);

                controls.Add(newControl);
                EditorUtility.SetDirty(menuAsset);
                AssetDatabase.SaveAssets();

                return JsonUtility.ToJson(new BasicRes { message = $"Added {q["name"]} to menu." });
            } catch (Exception e) {
                return JsonUtility.ToJson(new BasicRes { error = "VRC Menu Add failed: " + e.Message });
            }
        }

        [VibeTool("vrc/params/add", "Adds a parameter to a VRChat Expression Parameters asset.", "path", "name", "type", "saved", "default")]
        public static string VibeTool_vrc_params_add(Dictionary<string, string> q) {
            string assetPath = ResolveAssetPath(q["path"], "t:VRCExpressionParameters");
            if (string.IsNullOrEmpty(assetPath)) return JsonUtility.ToJson(new BasicRes { error = "Parameters asset not found." });

            try {
                System.Type paramsType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters"))
                    .FirstOrDefault(t => t != null);

                if (paramsType == null) return JsonUtility.ToJson(new BasicRes { error = "VRChat SDK (VRCExpressionParameters) not found." });

                ScriptableObject paramsAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (paramsAsset == null || paramsAsset.GetType() != paramsType) return JsonUtility.ToJson(new BasicRes { error = "Invalid parameters asset type." });

                Undo.RecordObject(paramsAsset, "Add VRC Parameter");

                var parametersField = paramsType.GetField("parameters");
                var parametersArray = parametersField.GetValue(paramsAsset) as Array;
                
                System.Type paramClass = paramsType.GetNestedType("Parameter");
                object newParam = Activator.CreateInstance(paramClass);

                paramClass.GetField("name").SetValue(newParam, q["name"]);
                
                System.Type valueTypeEnum = paramClass.GetNestedType("ValueType");
                object typeValue = Enum.Parse(valueTypeEnum, q["type"], true);
                paramClass.GetField("valueType").SetValue(newParam, typeValue);

                if (q.ContainsKey("saved")) paramClass.GetField("saved").SetValue(newParam, q["saved"].ToLower() == "true");
                if (q.ContainsKey("default")) {
                    float defaultVal = float.Parse(q["default"]);
                    paramClass.GetField("defaultValue").SetValue(newParam, defaultVal);
                }

                // Resize array
                var newArray = Array.CreateInstance(paramClass, parametersArray.Length + 1);
                Array.Copy(parametersArray, newArray, parametersArray.Length);
                newArray.SetValue(newParam, parametersArray.Length);
                
                parametersField.SetValue(paramsAsset, newArray);

                EditorUtility.SetDirty(paramsAsset);
                AssetDatabase.SaveAssets();

                return JsonUtility.ToJson(new BasicRes { message = $"Added parameter {q["name"]} to asset." });
            } catch (Exception e) {
                return JsonUtility.ToJson(new BasicRes { error = "VRC Params Add failed: " + e.Message });
            }
        }
    }
}
#endif
