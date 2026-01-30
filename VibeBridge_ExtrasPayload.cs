#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer { 
        
        // --- VISUAL EXTRAS ---
        public static string VibeTool_visual_point(Dictionary<string, string> q) {
            Vector3 pos = ResolvePosition(q);
            GameObject pointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pointer.name = "[VIBE_POINT] " + (q.ContainsKey("label") ? q["label"] : "Attention");
            pointer.transform.position = pos;
            pointer.transform.localScale = Vector3.one * 0.2f;
            pointer.tag = "EditorOnly";
            var r = pointer.GetComponent<Renderer>();
            if (r) { 
                r.sharedMaterial = new Material(Shader.Find("Hidden/Internal-Colored")); 
                r.sharedMaterial.color = Color.red; 
            }
            Undo.RegisterCreatedObjectUndo(pointer, "Spawn Visual");
            return JsonUtility.ToJson(new BasicRes { message = "Pointer spawned", id = pointer.GetInstanceID() });
        }

        public static string VibeTool_visual_line(Dictionary<string, string> q) {
            Vector3 start = ResolvePosition(q, "start");
            Vector3 end = ResolvePosition(q, "end");
            GameObject lineObj = new GameObject("[VIBE_LINE]");
            lineObj.tag = "EditorOnly";
            var lr = lineObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = 0.05f; lr.endWidth = 0.05f;
            lr.positionCount = 2;
            lr.SetPosition(0, start); lr.SetPosition(1, end);
            lr.sharedMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
            lr.sharedMaterial.color = Color.yellow;
            Undo.RegisterCreatedObjectUndo(lineObj, "Spawn Visual");
            return JsonUtility.ToJson(new BasicRes { message = "Line spawned", id = lineObj.GetInstanceID() });
        }

        public static string VibeTool_visual_clear(Dictionary<string, string> q) {
            var all = GameObject.FindObjectsOfType<GameObject>();
            int count = 0;
            foreach (var go in all) {
                if (go.name.StartsWith("[VIBE_POINT]") || go.name.StartsWith("[VIBE_LINE]")) {
                    Undo.DestroyObjectImmediate(go);
                    count++;
                }
            }
            return JsonUtility.ToJson(new BasicRes { message = $"Cleared {count} markers" });
        }

        public static string VibeTool_animator_set_param(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            var anim = go?.GetComponent<Animator>();
            if (anim == null) return JsonUtility.ToJson(new BasicRes { error = "Animator not found" });
            
            string name = q["name"], val = q["value"];
            if (bool.TryParse(val, out bool b)) anim.SetBool(name, b);
            else if (float.TryParse(val, out float f)) anim.SetFloat(name, f);
            else if (int.TryParse(val, out int i)) anim.SetInteger(name, i);
            
            return JsonUtility.ToJson(new BasicRes { message = "Parameter set" });
        }

        private static Vector3 ResolvePosition(Dictionary<string, string> q, string prefix = "") {
            string pk = string.IsNullOrEmpty(prefix) ? "path" : prefix + "Path";
            string ok = string.IsNullOrEmpty(prefix) ? "pos" : prefix + "Pos";
            if (q.ContainsKey(pk)) {
                GameObject t = Resolve(q[pk]);
                if (t != null) return t.transform.position;
            }
            if (q.ContainsKey(ok)) {
                var p = q[ok].Split(',').Select(float.Parse).ToArray();
                if (p.Length == 3) return new Vector3(p[0], p[1], p[2]);
            }
            return Vector3.zero;
        }
    }
}
#endif
