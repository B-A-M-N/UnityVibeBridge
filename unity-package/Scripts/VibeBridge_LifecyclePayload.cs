#if UNITY_EDITOR
// UnityVibeBridge: The Governed Creation Kernel for Unity
// Copyright (C) 2026 B-A-M-N
//
// This software is dual-licensed under the GNU AGPLv3 and a 
// Commercial "Work-or-Pay" Maintenance Agreement.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        
        public static string VibeTool_object_set_active(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Object not found" });
            
            bool state = q.ContainsKey("active") && q["active"].ToLower() == "true";
            Undo.RecordObject(go, (state ? "Activate " : "Deactivate ") + go.name);
            go.SetActive(state);
            
            return JsonUtility.ToJson(new BasicRes { message = "Object " + (state ? "activated" : "deactivated") });
        }
    }
}
#endif
