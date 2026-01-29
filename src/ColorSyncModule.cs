using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        private static float _lastC = -1f, _lastP = -1f, _lastS = -1f;
        private static float _lastC2 = -1f, _lastP2 = -1f, _lastS2 = -1f;
        private static float _lastHC = -1f, _lastHP = -1f, _lastHS = -1f;
        private static bool _lastHornOff = false;

        private static void UpdateColorSync() {
            GameObject obj = GameObject.Find("ExtoPc");
            if (obj == null) return;
            Animator anim = obj.GetComponent<Animator>();
            if (anim == null) return;

            try {
                float c = anim.GetFloat("Color");
                float p = anim.GetFloat("ColorPitch");
                float s = anim.GetFloat("ColorSat");
                
                float c2 = 0, p2 = 0, s2 = 0;
                try { c2 = anim.GetFloat("Color2"); p2 = anim.GetFloat("ColorPitch2"); s2 = anim.GetFloat("ColorSat2"); } catch {}
                
                float hc = 0, hp = 0, hs = 0;
                try { hc = anim.GetFloat("HairColor"); hp = anim.GetFloat("HairPitch"); hs = anim.GetFloat("HairSat"); } catch {}

                bool h = false;
                try { h = anim.GetBool("Horns"); } catch {}
                
                bool hornOff = false;
                try { hornOff = anim.GetBool("HornColorOff"); } catch {}

                if (c != _lastC || p != _lastP || s != _lastS || hornOff != _lastHornOff) {
                    _lastC = c; _lastP = p; _lastS = s; _lastHornOff = hornOff;
                    ApplyDynamicGroupSync("AccentAll", c, s, p);
                    if (h) {
                        if (hornOff) ApplyDynamicGroupSync("Horns", 0f, 0f, 0f);
                        else ApplyDynamicGroupSync("Horns", c, s, p);
                    }
                }
                
                if (c2 != _lastC2 || p2 != _lastP2 || s2 != _lastS2) {
                    _lastC2 = c2; _lastP2 = p2; _lastS2 = s2;
                    ApplyDynamicGroupSync("Secondary", c2, s2, p2);
                }
                
                if (hc != _lastHC || hp != _lastHP || hs != _lastHS) {
                    _lastHC = hc; _lastHP = hp; _lastHS = hs;
                    ApplyDynamicGroupSync("Hair", hc, hs, hp);
                }
            } catch {}
        }

        private static void ApplyDynamicGroupSync(string groupName, float h, float s, float v) {
            Color col = Color.HSVToRGB(h, s, v);
            if (_registry == null || _registry.entries == null) return;
            
            var entries = _registry.entries.Where(e => e.group == groupName);
            foreach (var entry in entries) {
                GameObject go = ResolveTarget(entry);
                if (go != null) {
                    var r = go.GetComponent<Renderer>();
                    if (r != null) {
                        if (entry.slotIndex >= 0 && entry.slotIndex < r.sharedMaterials.Length) {
                            var mat = r.sharedMaterials[entry.slotIndex];
                            if (mat != null) SetColorInternal(mat, col);
                        } else {
                            foreach (var mat in r.sharedMaterials) {
                                if (mat == null) continue;
                                SetColorInternal(mat, col);
                            }
                        }
                    }
                }
            }
        }
    }
}