#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        private static float _lastSyncTime = 0;
        private static DateTime _lastRegistryWrite;
        public static void UpdateColorSync() {
            if (Time.realtimeSinceStartup - _lastSyncTime < 0.1f) return; 
            _lastSyncTime = Time.realtimeSinceStartup;

            GameObject root = GameObject.Find("ExtoPc");
            if (root == null) return;
            var anim = root.GetComponent<Animator>();
            if (anim == null) return;

            try {
                // Optimization: Only load registry if file changed
                string p = "metadata/vibe_registry.json";
                if (File.Exists(p)) {
                    var writeTime = File.GetLastWriteTime(p);
                    if (writeTime != _lastRegistryWrite) {
                        _lastRegistryWrite = writeTime;
                        LoadRegistry();
                    }
                }
                
                float h = anim.GetFloat("Color"), s = anim.GetFloat("ColorSat"), v = anim.GetFloat("ColorPitch");
                Color col = Color.HSVToRGB(h, s, v);
                foreach (var entry in _registry.entries.Where(e => e.group == "AccentAll")) {
                    var go = EditorUtility.InstanceIDToObject(entry.lastKnownID) as GameObject;
                    var r = go?.GetComponent<Renderer>();
                    if (r != null) {
                        // Support for selective slot sync
                        if (entry.slotIndex >= 0 && entry.slotIndex < r.sharedMaterials.Length) {
                            var m = r.sharedMaterials[entry.slotIndex];
                            if (m != null) {
                                if (m.HasProperty("_Color")) m.SetColor("_Color", col);
                                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", col);
                            }
                        } else {
                            // Fallback to all slots if index is invalid or -1
                            foreach (var m in r.sharedMaterials) {
                                if (m == null) continue;
                                if (m.HasProperty("_Color")) m.SetColor("_Color", col);
                                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", col);
                            }
                        }
                    }
                }
            } catch { }
        }
    }
}
#endif
