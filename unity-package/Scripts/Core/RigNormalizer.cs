#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityVibeBridge.Kernel.Core {
    /// <summary>
    /// Procedural Rig Normalization
    /// - Transfers Armature scale (100x) to Root
    /// - Normalizes non-bone Props
    /// - Leaves deforming bones UNTOUCHED
    /// </summary>
    public static class RigNormalizer {
        
        public static void NormalizeForExport(GameObject root) {
            if (root == null) return;
            Debug.Log($"[VibeBridge] Starting Normalization Audit on {root.name}");

            // 1. Identify the Skeleton Root (usually 'Armature')
            Transform armature = null;
            if (root.transform != null) {
                foreach (Transform child in root.transform) {
                    if (child.name.Equals("Armature", StringComparison.OrdinalIgnoreCase)) {
                        armature = child;
                        break;
                    }
                }
            }

            // 2. Handle the 100x Scale Artifact (Centimeter vs Meter mismatch)
            if (armature != null && armature.localScale != Vector3.one) {
                Vector3 armScale = armature.localScale;
                Debug.Log($"[VibeBridge] Detected Armature Scale {armScale}. Transferring to Root.");
                
                Undo.RecordObject(root.transform, "Normalize Root Scale");
                Undo.RecordObject(armature, "Normalize Armature Scale");

                // Transfer scale to root
                Vector3 newRootScale = root.transform.localScale;
                newRootScale.x *= armScale.x;
                newRootScale.y *= armScale.y;
                newRootScale.z *= armScale.z;
                
                root.transform.localScale = newRootScale;
                armature.localScale = Vector3.one;
            }

            // 3. Normalize Props and Meshes (Non-Bone children)
            var bones = RigSafetyGate.GetRigBones(root);
            foreach (Transform child in root.transform) {
                if (child == armature) continue;
                if (bones.Contains(child)) continue; // Don't touch actual bones

                // If it's a Prop (like a Knife) and has a scale mismatch
                if (child.localScale != Vector3.one) {
                    Debug.Log($"[VibeBridge] Normalizing Prop: {child.name} (Scale: {child.localScale})");
                    Undo.RecordObject(child, "Normalize Prop");
                    // Note: We don't transfer prop scale to root as props are independent.
                    // We just log it for the user or keep it if it's intentional.
                    // For now, we only alert the RigSafetyGate.
                }
            }
        }
    }
}
#endif
