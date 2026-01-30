using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        public static string VibeTool_visual_point(Dictionary<string, string> q) {
            Vector3 pos = ResolvePosition(q);
            GameObject pointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pointer.name = "[VIBE_POINT] " + (q.ContainsKey("label") ? q["label"] : "Attention");
            pointer.transform.position = pos;
            pointer.transform.localScale = Vector3.one * 0.2f;
            pointer.tag = "EditorOnly";
            var renderer = pointer.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Hidden/Internal-Colored"));
            renderer.material.color = Color.red;
            LogMutation("VISUAL", "global", "point", "Spawned pointer at " + pos);
            return "{\"message\":\"Pointer spawned\",\"instanceID\":" + pointer.GetInstanceID() + "}";
        }

        public static string VibeTool_visual_line(Dictionary<string, string> q) {
            Vector3 start = ResolvePosition(q, "start");
            Vector3 end = ResolvePosition(q, "end");
            GameObject lineObj = new GameObject("[VIBE_LINE] " + (q.ContainsKey("label") ? q["label"] : "Connection"));
            lineObj.transform.position = start;
            lineObj.tag = "EditorOnly";
            var lr = lineObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            lr.material = new Material(Shader.Find("Hidden/Internal-Colored"));
            lr.material.color = Color.yellow;
            LogMutation("VISUAL", "global", "line", "Line spawned");
            return "{\"message\":\"Line spawned\",\"instanceID\":" + lineObj.GetInstanceID() + "}";
        }

        public static string VibeTool_visual_clear(Dictionary<string, string> q) {
            var all = GameObject.FindObjectsOfType<GameObject>();
            int count = 0;
            foreach (var go in all) {
                if (go.name.StartsWith("[VIBE_POINT]") || go.name.StartsWith("[VIBE_LINE]")) {
                    GameObject.DestroyImmediate(go);
                    count++;
                }
            }
            return "{\"message\":\"Cleared " + count + " visual markers\"}";
        }

        private static Vector3 ResolvePosition(Dictionary<string, string> q, string prefix = "") {
            string pathKey = string.IsNullOrEmpty(prefix) ? "path" : prefix + "Path";
            string posKey = string.IsNullOrEmpty(prefix) ? "pos" : prefix + "Pos";
            if (q.ContainsKey(pathKey)) {
                GameObject target = null;
                if (int.TryParse(q[pathKey], out int id)) target = EditorUtility.InstanceIDToObject(id) as GameObject;
                else target = GameObject.Find(q[pathKey]);
                if (target != null) return target.transform.position;
            }
            if (q.ContainsKey(posKey)) {
                var p = q[posKey].Split(',');
                if (p.Length == 3) return new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
            }
            return Vector3.zero;
        }
    }
}
