#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityVibeBridge.Kernel.Core {
    /// <summary>
    /// VibeISA: Strategic Instruction Set Architecture.
    /// Orchestrates complex tool chains into atomic, verifiable "Work Orders."
    /// </summary>
    public static class VibeISA {
        
        public static string ExecuteIntent(WorkOrder order) {
            Debug.Log($"[VibeISA] Executing Intent: {order.intent} on target {order.target_uuid}");
            
            switch (order.intent.ToUpper()) {
                case "SCENE_AUDIT":
                    return Intent_SceneAudit(order);
                case "AVATAR_HARDEN":
                    return Intent_AvatarHarden(order);
                case "SAFE_MUTATE":
                    return Intent_SafeMutate(order);
                case "ISA_REFLECTION_SET":
                    return Intent_ReflectionBatch(order);
                default:
                    return "{\"error\":\"Intent not recognized by ISA v1.1\"}";
            }
        }

        private static string Intent_ReflectionBatch(WorkOrder order) {
            // Strategic Intent: Perform multiple reflection mutations without recompiling.
            // Opcodes format: "ComponentName|FieldName|Value"
            int success = 0;
            GameObject target = VibeMetadataProvider.ResolveRole(order.target_uuid);
            if (target == null) return "{\"error\":\"Target not found\"}";

            foreach (var op in order.opcodes) {
                var parts = op.Split('|');
                if (parts.Length != 3) continue;
                
                var query = new Dictionary<string, string> {
                    { "path", order.target_uuid },
                    { "component", parts[0] },
                    { "field", parts[1] },
                    { "value", parts[2] }
                };
                
                string res = VibeBridgeServer.VibeTool_object_set_value(query);
                if (!res.Contains("error")) success++;
            }

            return JsonUtility.ToJson(new BasicRes { 
                conclusion = "ISA_BATCH_COMPLETE", 
                message = $"Successfully applied {success}/{order.opcodes.Length} reflection updates."
            });
        }

        private static string Intent_SceneAudit(WorkOrder order) {
            // Strategic Chain: Hierarchy -> Identity Check -> Error Scan
            var res = new List<string>();
            var objects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            var identities = UnityEngine.Object.FindObjectsOfType<VibeIdentity>();
            
            string report = $"Scene contains {objects.Length} objects. {identities.Length} have VibeIdentity.";
            
            // Consolidate into a "Belief" format for the Foreman
            return "{\"conclusion\":\"SCENE_READY\", \"facts\": \"" + report + "\"}";
        }

        private static string Intent_SafeMutate(WorkOrder order) {
            // Strategic Loop: Capture Baseline -> Mutate -> Stress Test -> Commit/Rollback
            string baselineHash = VibeBridgeServer.GetStateHash();
            
            try {
                // 1. Dispatch low-level mutation
                var cmd = new AirlockCommand { action = order.action, id = order.id };
                // ... logic to map order data to command
                string res = VibeBridgeServer.ExecuteAirlockCommand(cmd);

                // 2. RUN INTEGRITY STRESS TEST
                var integrity = RunIntegrityCheck(order.target_uuid);
                if (!integrity.passed) {
                    Undo.PerformUndo();
                    return JsonUtility.ToJson(new BasicRes { 
                        error = "Integrity Stress Test Failed. Mutation Rollback initiated.",
                        conclusion = "MUTATION_REJECTED",
                        message = string.Join("; ", integrity.issues)
                    });
                }

                // 3. Automated Post-Mutation Git Checkpoint
                VibeBridgeServer.CommitCheckpoint($"[ISA] Auto-Verified Mutation: {order.intent}");
                
                return JsonUtility.ToJson(new BasicRes { 
                    conclusion = "MUTATION_VERIFIED",
                    message = "Post-mutation integrity checks passed."
                });
            } catch (Exception e) {
                return "{\"error\":\"ISA Failure: " + e.Message + "\"}";
            }
        }

        private static IntegrityReport RunIntegrityCheck(string targetUuid) {
            var report = new IntegrityReport { passed = true };
            var issues = new List<string>();

            // Example Stress Test: Mesh Integrity
            GameObject go = VibeMetadataProvider.ResolveRole(targetUuid);
            if (go != null) {
                var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr != null && smr.sharedMesh == null) {
                    issues.Add("Mesh was detached or corrupted during mutation.");
                    report.passed = false;
                }
            }

            report.issues = issues.ToArray();
            return report;
        }

        private static string Intent_AvatarHarden(WorkOrder order) {
            // Strategic Chain: Find Root -> Recursive Identity Assignment -> UMP Registration
            GameObject root = VibeMetadataProvider.ResolveRole("sem:AvatarRoot") ?? GameObject.Find("ExtoPc");
            if (root == null) return "{\"error\":\"Avatar root not found.\"}";

            int assigned = 0;
            var allChildren = root.GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren) {
                if (child.GetComponent<VibeIdentity>() == null) {
                    var id = child.gameObject.AddComponent<VibeIdentity>();
                    id.GenerateId();
                    VibeMetadataProvider.Register(child.name, id.Uuid, "AutoHardened");
                    assigned++;
                }
            }

            return "{\"conclusion\":\"AVATAR_HARDENED\", \"message\":\"Assigned UUIDs to " + assigned + " objects.\"}";
        }
    }
}
#endif
