#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityVibeBridge.Kernel.Core;

namespace UnityVibeBridge.Kernel {
    public static partial class VibeBridgeServer {
        
        [VibeTool("registry/add", "Registers an object with a semantic role and optional group.", "path", "role", "group", "slotIndex")]
        public static string VibeTool_registry_add(Dictionary<string, string> q) {
            string path = q["path"];
            string role = q["role"];
            string group = q.ContainsKey("group") ? q["group"] : null;
            int slotIndex = q.ContainsKey("slotIndex") ? int.Parse(q["slotIndex"]) : -1;
            
            VibeMetadataProvider.Register(path, role, group, slotIndex);
            return JsonUtility.ToJson(new BasicRes { message = $"Registered {path} as {role}" + (group != null ? $" in group {group}" : "") });
        }

        [VibeTool("registry/list", "Returns all registered semantic targets.")]
        public static string VibeTool_registry_list(Dictionary<string, string> q) {
            var data = VibeMetadataProvider.Registry;
            return JsonUtility.ToJson(data);
        }
    }
}
#endif
