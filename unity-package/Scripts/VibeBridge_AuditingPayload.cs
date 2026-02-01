#if UNITY_EDITOR
// UnityVibeBridge: The Governed Creation Kernel for Unity
// Copyright (C) 2026 B-A-M-N
//
// This software is dual-licensed under the GNU AGPLv3 and a 
// Commercial "Work-or-Pay" Maintenance Agreement.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        
        public static string VibeTool_audit_avatar(Dictionary<string, string> q) {
            GameObject root = VibeBridgeServer.ResolveAssetPath(q["path"]) != null ? Resolve(q["path"]) : null;
            if (root == null) return JsonUtility.ToJson(new BasicRes { error = "Root not found" });
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var results = renderers.Select(r => {
                Mesh m = (r is SkinnedMeshRenderer smr) ? smr.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
                return new RendererAudit { path = r.name, verts = (m != null ? m.vertexCount : 0), mats = r.sharedMaterials.Length };
            }).ToArray();
            return JsonUtility.ToJson(new AvatarAuditRes { name = root.name, renderers = results });
        }

        public static string VibeTool_physics_audit(Dictionary<string, string> q) {
            var rb = UnityEngine.Object.FindObjectsOfType<Rigidbody>();
            var col = UnityEngine.Object.FindObjectsOfType<Collider>();
            var results = rb.Select(r => new PhysicsAuditRes.PhysicsNode { name = r.name, type = "Rigidbody", isKinematic = r.isKinematic, isTrigger = false })
                .Concat(col.Select(c => new PhysicsAuditRes.PhysicsNode { name = c.name, type = "Collider", isKinematic = false, isTrigger = c.isTrigger })).ToArray();
            return JsonUtility.ToJson(new PhysicsAuditRes { physicsObjects = results });
        }

        public static string VibeTool_animation_audit(Dictionary<string, string> q) {
            var animators = UnityEngine.Object.FindObjectsOfType<Animator>();
            var results = animators.Select(a => {
                var ctrl = a.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                int missingClips = 0;
                if (ctrl != null) {
                    foreach (var layer in ctrl.layers) {
                        foreach (var state in layer.stateMachine.states) if (state.state.motion == null) missingClips++;
                    }
                }
                return new AnimationAuditRes.AnimatorNode { name = a.name, missingClips = missingClips };
            }).ToArray();
            return JsonUtility.ToJson(new AnimationAuditRes { animators = results });
        }

        public static string VibeTool_physbone_rank_importance(Dictionary<string, string> q) {
            GameObject root = Resolve(q["path"]);
            if (root == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });

            // Note: Uses reflection to support projects without VRC SDK installed safely
            var pbType = System.Type.GetType("VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone, VRC.SDK3.Dynamics.PhysBone.Runtime");
            if (pbType == null) return JsonUtility.ToJson(new BasicRes { error = "VRChat SDK (PhysBones) not found in project" });

            var pbs = root.GetComponentsInChildren(pbType, true);
            var ranks = new List<PhysBoneRankRes.BoneRankNode>();

            foreach (var pb in pbs) {
                var go = ((Component)pb).gameObject;
                var transform = go.transform;
                
                // Heuristic Ranking: weight = (child depth * 10) + (distance from root)
                float weight = transform.GetComponentsInChildren<Transform>(true).Length * 1.5f;
                if (go.name.ToLower().Contains("hair")) weight += 10f;
                if (go.name.ToLower().Contains("tail") || go.name.ToLower().Contains("ear")) weight += 20f;
                if (go.name.ToLower().Contains("breast") || go.name.ToLower().Contains("butt")) weight += 5f;

                ranks.Add(new PhysBoneRankRes.BoneRankNode {
                    name = go.name,
                    weight = weight,
                    childCount = transform.childCount
                });
            }

            return JsonUtility.ToJson(new PhysBoneRankRes { bones = ranks.OrderByDescending(b => b.weight).ToArray() });
        }
    }
}
#endif
