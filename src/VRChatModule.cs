using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        public static string VibeTool_vrc_param_set(Dictionary<string, string> q) {
            GameObject obj = GameObject.Find("ExtoPc");
            var anim = obj?.GetComponent<Animator>();
            if (anim == null) return "{\"error\":\"Animator not found\"}";
            string name = q["name"], val = q["value"];
            if (bool.TryParse(val, out bool b)) anim.SetBool(name, b);
            else if (float.TryParse(val, out float f)) anim.SetFloat(name, f);
            else if (int.TryParse(val, out int i)) anim.SetInteger(name, i);
            return "{\"message\":\"Success\"}";
        }

        public static string VibeTool_vrc_param_get(Dictionary<string, string> q) {
            GameObject obj = GameObject.Find("ExtoPc");
            var anim = obj?.GetComponent<Animator>();
            if (anim == null) return "{\"error\":\"Animator not found\"}";
            string name = q["name"];
            foreach (var p in anim.parameters) {
                if (p.name == name) {
                    if (p.type == AnimatorControllerParameterType.Bool) return "{\"value\":" + anim.GetBool(name).ToString().ToLower() + "}";
                    if (p.type == AnimatorControllerParameterType.Float) return "{\"value\":" + anim.GetFloat(name) + "}";
                    if (p.type == AnimatorControllerParameterType.Int) return "{\"value\":" + anim.GetInteger(name) + "}";
                }
            }
            return "{\"error\":\"Parameter not found\"}";
        }
    }
}
