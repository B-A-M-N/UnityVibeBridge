using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        public static string VibeTool_audit_animator(Dictionary<string, string> query) {
            string path = query["path"];
            GameObject obj = null;
            if (int.TryParse(path, out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(path);
            var anim = obj?.GetComponent<Animator>();
            if (anim == null) return "{\"error\":\"No Animator found\"}";
            var controller = anim.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            if (controller == null) return "{\"error\":\"No AnimatorController asset attached\"}";
            
            var layers = controller.layers.Select(l => "{\"name\":\"" + EscapeJson(l.name) + "\",\"stateCount\":" + l.stateMachine.states.Length + "}");
            return "{\"layers\":[" + string.Join(",", layers) + "]}";
        }

        private static string EscapeJson(string s) {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}