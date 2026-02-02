#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityVibeBridge.Kernel.Core;

namespace UnityVibeBridge.Kernel {
    public static partial class VibeBridgeServer {
        private static float _lastSyncTime = 0;

        public static void UpdateColorSync() {
            if (Time.realtimeSinceStartup - _lastSyncTime < 0.1f) return; 
            _lastSyncTime = Time.realtimeSinceStartup;

            // Resolve the visual root via UMP or fallback
            GameObject root = VibeMetadataProvider.ResolveRole("sem:AvatarRoot") ?? GameObject.Find("ExtoPc");
            if (root == null) return;

            var anim = root.GetComponent<Animator>();
            if (anim == null) return;

            try {
                // Fetch all entries in the "AccentAll" group via UMP
                var entries = VibeMetadataProvider.GetGroup("AccentAll");
                if (entries.Count == 0) return;

                float h = anim.GetFloat("Color");
                float s = anim.GetFloat("ColorSat");
                float v = anim.GetFloat("ColorPitch");
                Color col = Color.HSVToRGB(h, s, v);

                foreach (var entry in entries) {
                    GameObject go = VibeMetadataProvider.ResolveRole(entry.role);
                    var r = go?.GetComponent<Renderer>();
                    if (r != null) {
                        if (entry.slotIndex >= 0 && entry.slotIndex < r.sharedMaterials.Length) {
                            var m = r.sharedMaterials[entry.slotIndex];
                            if (m != null) ApplyColor(m, col);
                        } else {
                            foreach (var m in r.sharedMaterials) if (m != null) ApplyColor(m, col);
                        }
                    }
                }
            } catch { }
        }

        private static void ApplyColor(Material m, Color col) {
            if (m.HasProperty("_Color")) m.SetColor("_Color", col);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", col);
        }
    }
}
#endif
