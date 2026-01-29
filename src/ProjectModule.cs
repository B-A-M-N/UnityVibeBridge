using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        public static string VibeTool_project_missing_scripts(Dictionary<string, string> query) {
            var report = new List<string>();
            var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>(true);
            foreach (var go in allObjects) {
                var components = go.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++) {
                    if (components[i] == null) {
                        report.Add("{\"name\":\"" + go.name + "\",\"path\":\"" + GetGameObjectPath(go) + "\",\"index\":" + i + "}");
                    }
                }
            }
            return "{\"missingScripts\":[" + string.Join(",", report) + "]}";
        }

        private static string GetGameObjectPath(GameObject obj) {
            string path = "/" + obj.name;
            while (obj.transform.parent != null) {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }

        private static int _focusedAvatarId = -1;
        private static string _focusedAssetPath = "Assets";

        public static string VibeTool_system_focus(Dictionary<string, string> q) {
            if (q.ContainsKey("avatar")) int.TryParse(q["avatar"], out _focusedAvatarId);
            if (q.ContainsKey("assets")) _focusedAssetPath = q["assets"];
            
            return "{\"message\":\"Focus Locked\",\"avatar\":" + _focusedAvatarId + ",\"assets\":\"" + _focusedAssetPath + "\"}";
        }
    }
}
