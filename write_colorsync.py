code = r"""using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        private static float _lastColorValue = -1f;
        private static float _lastPitchValue = -1f;
        private static bool _lastHornsValue = false;

        private static readonly (string path, int index)[] ColorTargets = new [] {
            ("28566", 2), ("28566", 6), ("27190", 0), ("27190", 1), ("27190", 2),
            ("27442", 2), ("27162", 1), ("27384", 1), ("28202", 3), ("27804", 0), ("27792", 0)
        };

        private static void UpdateColorSync() {
            GameObject obj = GameObject.Find("ExtoPc");
            if (obj == null) return;
            Animator anim = obj.GetComponent<Animator>();
            if (anim == null) return;

            float colorParam = 0f;
            float pitchParam = 1f;
            bool hornsParam = false;

            try {
                colorParam = anim.GetFloat("Color");
                pitchParam = anim.GetFloat("ColorPitch");
                hornsParam = anim.GetBool("Horns");
            } catch { return; }

            if (colorParam == _lastColorValue && pitchParam == _lastPitchValue && hornsParam == _lastHornsValue) return;

            _lastColorValue = colorParam;
            _lastPitchValue = pitchParam;
            _lastHornsValue = hornsParam;

            Color finalColor = Color.HSVToRGB(colorParam, 1.0f, pitchParam);
            
            foreach (var target in ColorTargets) {
                SetSlotColorInternal(target.path, target.index, "_Color", finalColor);
                SetSlotColorInternal(target.path, target.index, "_EmissionColor", finalColor);
            }

            Color hornColor = hornsParam ? finalColor : Color.black;
            SetSlotColorInternal("27832", 1, "_Color", hornColor);
            SetSlotColorInternal("28566", 3, "_Color", hornColor);
        }

        private static void SetSlotColorInternal(string path, int index, string field, Color color) {
            GameObject targetObj = null;
            if (int.TryParse(path, out int id)) targetObj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else targetObj = GameObject.Find(path);
            
            Renderer r = targetObj?.GetComponent<Renderer>();
            if (r == null || index >= r.sharedMaterials.Length || r.sharedMaterials[index] == null) return;
            
            Material mat = r.sharedMaterials[index];
            if (mat.HasProperty(field)) {
                mat.SetColor(field, color);
            }
        }
    }
}
"""
with open("src/ColorSyncModule.cs", "w") as f:
    f.write(code)
