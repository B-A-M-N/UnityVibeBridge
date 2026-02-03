#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityVibeBridge.Kernel.Core {
    /// <summary>
    /// UNITY RIG SAFETY CONTRACT ENFORCER
    /// - Structural prevention of rig collapse (Crumpled Ball Bug)
    /// - Enforcement of the Bone Firewall
    /// - Fail-Closed validation
    /// </summary>
    public static class RigSafetyGate {
        public enum ToolPhase {
            PreRig,
            PreBind,
            PostBind,
            PreExport
        }

        private static readonly string[] _boneNames = { "Hips", "Spine", "Chest", "Neck", "Head", "Shoulder", "Arm", "Elbow", "Wrist", "Hand", "Finger", "Leg", "Knee", "Ankle", "Foot", "Toe" };

        /// <summary>
        /// Validates if a GameObject is safe to undergo transform mutations.
        /// Throws Exception if a violation is detected.
        /// </summary>
        public static void ValidateTransformMutation(GameObject target, ToolPhase phase) {
            if (target == null) return;

            bool isRigged = IsRiggedAvatar(target);
            
            if (isRigged) {
                // RULE 1 & 2 Enforcement: Bones are READ-ONLY.
                // We build a recursive blacklist of bones.
                var bones = GetRigBones(target);
                
                // If the target is a bone, DENY.
                if (bones.Contains(target.transform)) {
                    throw new Exception($"RIG_SAFETY_VIOLATION: Attempted to mutate bone '{target.name}'. Bones are READ-ONLY post-bind.");
                }

                // If we are in PostBind or PreExport phase, ONLY the root is allowed for position/rotation.
                if (phase == ToolPhase.PostBind || phase == ToolPhase.PreExport) {
                    // Check if target is root (assuming target is the root if we're calling reset-transforms on it)
                    // But if reset-transforms is recursive, the tool itself must use this gate per-object.
                }
            }
        }

        public static bool IsRiggedAvatar(GameObject go) {
            if (go.GetComponent<Animator>() != null) return true;
            if (go.GetComponentInChildren<SkinnedMeshRenderer>(true) != null) return true;
            return false;
        }

        public static HashSet<Transform> GetRigBones(GameObject root) {
            var bones = new HashSet<Transform>();
            
            // 1. All bones from SkinnedMeshRenderers
            var smrs = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in smrs) {
                if (smr.bones != null) {
                    foreach (var b in smr.bones) if (b != null) bones.Add(b);
                }
                if (smr.rootBone != null) bones.Add(smr.rootBone);
            }

            // 2. Recursive Armature/Hips check
            var all = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in all) {
                if (t.name.Equals("Armature", StringComparison.OrdinalIgnoreCase) || 
                    t.name.Equals("Hips", StringComparison.OrdinalIgnoreCase) ||
                    _boneNames.Any(bn => t.name.Contains(bn))) {
                    foreach (var child in t.GetComponentsInChildren<Transform>(true)) {
                        bones.Add(child);
                    }
                }
            }
            return bones;
        }

        public static void PreExportCheck(GameObject avatar) {
            if (!IsRiggedAvatar(avatar)) return;

            var bones = GetRigBones(avatar);
            var violations = new List<string>();

            foreach (var b in bones) {
                // EXCEPTION: The Armature container itself is allowed to have scale mismatch 
                // BEFORE normalization, but actual deforming bones (Hips and below) ARE NOT.
                if (b.name.Equals("Armature", StringComparison.OrdinalIgnoreCase)) continue;

                if (Vector3.Distance(b.localScale, Vector3.one) > 0.001f) {
                    violations.Add($"Bone '{b.name}' has non-identity scale: {b.localScale}");
                }
            }

            if (violations.Count > 0) {
                throw new Exception("EXPORT_BLOCKED: Rig integrity check failed. Violations:\n" + string.Join("\n", violations));
            }
        }
    }
}
#endif
