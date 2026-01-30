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

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        
        public static string VibeTool_vrc_menu_add(Dictionary<string, string> q) {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(q["path"]);
            if (asset == null) return JsonUtility.ToJson(new BasicRes { error = "Menu asset not found" });
            
            var so = new SerializedObject(asset);
            var controls = so.FindProperty("controls");
            controls.InsertArrayElementAtIndex(controls.arraySize);
            
            var ctrl = controls.GetArrayElementAtIndex(controls.arraySize - 1);
            ctrl.FindPropertyRelative("name").stringValue = q["name"];
            ctrl.FindPropertyRelative("type").enumValueIndex = int.Parse(q["type"]); // 0=Button, 1=Toggle, etc.
            ctrl.FindPropertyRelative("parameter").FindPropertyRelative("name").stringValue = q["parameter"];
            
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            return JsonUtility.ToJson(new BasicRes { message = "Control added to menu" });
        }

        public static string VibeTool_vrc_params_add(Dictionary<string, string> q) {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(q["path"]);
            if (asset == null) return JsonUtility.ToJson(new BasicRes { error = "Params asset not found" });
            
            var so = new SerializedObject(asset);
            var parameters = so.FindProperty("parameters");
            parameters.InsertArrayElementAtIndex(parameters.arraySize);
            
            var param = parameters.GetArrayElementAtIndex(parameters.arraySize - 1);
            param.FindPropertyRelative("name").stringValue = q["name"];
            param.FindPropertyRelative("valueType").enumValueIndex = int.Parse(q["type"]); // 0=Float, 1=Int, 2=Bool
            param.FindPropertyRelative("saved").boolValue = true;
            
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            return JsonUtility.ToJson(new BasicRes { message = "Parameter added to asset" });
        }
    }
}
#endif
