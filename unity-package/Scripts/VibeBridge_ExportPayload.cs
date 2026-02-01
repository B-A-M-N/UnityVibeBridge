#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    [Serializable] public class ExportAuditRes {
        public string status;
        public float height;
        public Vector3 rootScale;
        public Vector3 rootRotation;
        public string[] warnings;
    }

    public static partial class VibeBridgeServer {
        public static string VibeTool_export_validate(Dictionary<string, string> q) {
            GameObject obj = Resolve(q["path"]);
            if (obj == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });

            var warnings = new List<string>();
            
            // 1. Scale Check
            float height = 0;
            var renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0) {
                Bounds b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                height = b.size.y;
                if (height > 50f) warnings.Add("Height Alert: Object is > 50m tall. Blender import might be huge.");
                if (height < 0.1f) warnings.Add("Scale Alert: Object is < 10cm tall. Blender import might be tiny.");
            }

            if (obj.transform.localScale != Vector3.one) {
                warnings.Add($"Root Scale is {obj.transform.localScale}. Blender prefers 1,1,1 for rigging.");
            }

            // 2. Rotation Check (-90 X is bad)
            if (Mathf.Abs(obj.transform.eulerAngles.x - 270) < 1f || Mathf.Abs(obj.transform.eulerAngles.x + 90) < 1f) {
                warnings.Add("Rotation Alert: Root has -90/270 X rotation. This often causes flipped bones in Blender.");
            }

            // 3. Mesh Integrity
            var filters = obj.GetComponentsInChildren<MeshFilter>();
            foreach (var f in filters) {
                if (f.sharedMesh == null) warnings.Add($"Missing Mesh: {f.name} has a MeshFilter but no assigned mesh.");
            }

            // 4. Missing Scripts (Common in VRChat avatars)
            int missing = 0;
            foreach (var go in obj.GetComponentsInChildren<Transform>(true)) {
                missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go.gameObject);
            }
            if (missing > 0) warnings.Add($"Missing Scripts: {missing} components are missing their scripts. Clean these before export.");

            return JsonUtility.ToJson(new ExportAuditRes {
                status = warnings.Count == 0 ? "Ready" : "Action Required",
                height = height,
                rootScale = obj.transform.localScale,
                rootRotation = obj.transform.eulerAngles,
                warnings = warnings.ToArray()
            });
        }
    }
}
#endif
