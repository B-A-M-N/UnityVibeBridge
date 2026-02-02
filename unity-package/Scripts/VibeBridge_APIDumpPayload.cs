#if UNITY_EDITOR
// UnityVibeBridge: Enterprise Hardening
// Copyright (C) 2026 B-A-M-N

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityVibeBridge.Kernel {
    public static partial class VibeBridgeServer {

        [VibeTool("system/api_dump", "Exports all available VibeBridge tool signatures for AI context injection.", "path")]
        public static string VibeTool_system_api_dump(Dictionary<string, string> q) {
            var tools = new List<ApiDumpRes.ApiNode>();
            var type = typeof(VibeBridgeServer);
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var method in methods) {
                if (!method.Name.StartsWith("VibeTool_")) continue;

                var attr = method.GetCustomAttribute<VibeToolAttribute>();
                if (attr != null) {
                    tools.Add(new ApiDumpRes.ApiNode {
                        name = attr.Name,
                        description = attr.Description,
                        parameters = attr.Params
                    });
                } else {
                    // Fallback for undecorated tools
                    string inferredName = method.Name.Replace("VibeTool_", "").Replace("_", "/");
                    tools.Add(new ApiDumpRes.ApiNode {
                        name = inferredName,
                        description = "Legacy tool (Undocumented)",
                        parameters = new string[] { "path" }
                    });
                }
            }

            var res = new ApiDumpRes { tools = tools.ToArray() };
            string json = JsonUtility.ToJson(res);
            
            try {
                string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "metadata", "unity_api_map.json");
                File.WriteAllText(outputPath, json);
            } catch (Exception e) {
                Debug.LogError($"[VibeBridge] Failed to write API map: {e.Message}");
            }

            return json;
        }
    }
}
#endif
