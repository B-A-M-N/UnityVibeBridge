#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityVibeBridge.Kernel {
    public static partial class VibeBridgeServer {

        [VibeTool("opt/fork", "Duplicates an object and disables the original for non-destructive experimentation.", "path")]
        public static string VibeTool_opt_fork(Dictionary<string, string> q) {
            GameObject original = Resolve(q["path"]);
            if (original == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });

            GameObject fork = UnityEngine.Object.Instantiate(original, original.transform.parent);
            fork.name = "Fork_" + original.name;
            fork.transform.localPosition = original.transform.localPosition;
            fork.transform.localRotation = original.transform.localRotation;
            fork.transform.localScale = original.transform.localScale;

            Undo.RegisterCreatedObjectUndo(fork, "Fork Object");
            Undo.RecordObject(original, "Disable Original");
            original.SetActive(false);

            return JsonUtility.ToJson(new BasicRes { message = "Forked", id = fork.GetInstanceID() });
        }

        [VibeTool("visual/point", "Spawns a debug sphere at a coordinate for visual confirmation.", "pos", "color")]
        public static string VibeTool_visual_point(Dictionary<string, string> q) {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "VibeDebug_Point";
            if (q.ContainsKey("pos")) {
                var p = q["pos"].Split(',').Select(float.Parse).ToArray();
                go.transform.position = new Vector3(p[0], p[1], p[2]);
            }
            go.transform.localScale = Vector3.one * 0.05f;
            var r = go.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Hidden/Internal-Colored"));
            if (q.ContainsKey("color")) {
                var c = q["color"].Split(',').Select(float.Parse).ToArray();
                r.material.color = new Color(c[0], c[1], c[2]);
            }
            Undo.RegisterCreatedObjectUndo(go, "Spawn Debug Point");
            return JsonUtility.ToJson(new BasicRes { message = "Point spawned", id = go.GetInstanceID() });
        }

        [VibeTool("visual/line", "Draws a debug line between two coordinates.", "start", "end", "color")]
        public static string VibeTool_visual_line(Dictionary<string, string> q) {
            var go = new GameObject("VibeDebug_Line");
            var lr = go.AddComponent<LineRenderer>();
            lr.startWidth = 0.01f; lr.endWidth = 0.01f;
            lr.material = new Material(Shader.Find("Hidden/Internal-Colored"));
            var s = q["start"].Split(',').Select(float.Parse).ToArray();
            var e = q["end"].Split(',').Select(float.Parse).ToArray();
            lr.SetPositions(new Vector3[] { new Vector3(s[0], s[1], s[2]), new Vector3(e[0], e[1], e[2]) });
            if (q.ContainsKey("color")) {
                var c = q["color"].Split(',').Select(float.Parse).ToArray();
                lr.startColor = lr.endColor = new Color(c[0], c[1], c[2]);
            }
            Undo.RegisterCreatedObjectUndo(go, "Spawn Debug Line");
            return JsonUtility.ToJson(new BasicRes { message = "Line spawned", id = go.GetInstanceID() });
        }

        [VibeTool("visual/clear", "Destroys all active debug visuals.")]
        public static string VibeTool_visual_clear(Dictionary<string, string> q) {
            int count = 0;
            foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>()) {
                if (go.name.StartsWith("VibeDebug_")) {
                    Undo.DestroyObjectImmediate(go);
                    count++;
                }
            }
            return JsonUtility.ToJson(new BasicRes { message = $"Cleared {count} debug objects" });
        }
    }
}
#endif
