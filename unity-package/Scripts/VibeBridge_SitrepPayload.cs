#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityVibeBridge.Kernel.Core;

namespace UnityVibeBridge.Kernel {
    public static partial class VibeBridgeServer {

        [VibeTool("system/generate_sitrep", "Generates a detailed Situation Report including an Affordance Map.", "path")]
        public static string VibeTool_system_generate_sitrep(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Target not found" });

            var res = new SitrepRes {
                target_uuid = GetUuid(go),
                state_summary = $"Object {go.name} is {(go.activeInHierarchy ? "Active" : "Inactive")}."
            };

            // --- AFFORDANCE MAPPING ---
            
            // 1. Rigging Affordance
            var smr = go.GetComponentInChildren<SkinnedMeshRenderer>(true);
            res.affordances.Add(new AffordanceNode {
                capability = "RIGGING",
                available = smr != null,
                reason = smr != null ? "SkinnedMeshRenderer present." : "No SkinnedMeshRenderer found in hierarchy."
            });

            // 2. Baking Affordance
            var renderer = go.GetComponent<Renderer>();
            bool hasMaterials = renderer != null && renderer.sharedMaterials.Any(m => m != null);
            res.affordances.Add(new AffordanceNode {
                capability = "BAKING",
                available = hasMaterials,
                reason = hasMaterials ? "Materials assigned." : "No materials found to bake."
            });

            // 3. Physics Affordance
            var rb = go.GetComponent<Rigidbody>();
            res.affordances.Add(new AffordanceNode {
                capability = "PHYSICS_SIM",
                available = rb != null,
                reason = rb != null ? "Rigidbody present." : "Missing Rigidbody for simulation."
            });

            return JsonUtility.ToJson(res);
        }

        private static string GetUuid(GameObject go) {
            var id = go.GetComponent<VibeIdentity>();
            return id != null ? id.Uuid : "NON_VIBE_OBJECT";
        }
    }
}
#endif
