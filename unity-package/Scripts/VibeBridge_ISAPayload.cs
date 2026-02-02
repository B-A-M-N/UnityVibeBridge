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

        [VibeTool("system/propose_plan", "Submits a multi-step strategic plan for Human-In-The-Loop approval.", "goal", "steps")]
        public static string VibeTool_system_propose_plan(Dictionary<string, string> q) {
            var plan = new StrategicPlan {
                id = Guid.NewGuid().ToString().Substring(0, 8),
                goal = q["goal"]
            };

            if (q.ContainsKey("steps")) {
                var stepList = q["steps"].Split('|');
                for (int i = 0; i < stepList.Length; i++) {
                    plan.steps.Add(new StrategicPlan.PlanStep { order = i + 1, description = stepList[i] });
                }
            }

            // Trigger the Vibe Panel for approval
            UnityVibeBridge.Kernel.Editor.VibeBridgeEditorWindow.RequestApproval("STRATEGIC_PLAN", JsonUtility.ToJson(plan, true));

            return JsonUtility.ToJson(new BasicRes { 
                conclusion = "PLAN_PROPOSED", 
                message = $"Plan {plan.id} queued for approval." 
            });
        }

        [VibeTool("isa/execute", "Executes a complex ISA Work Order.", "intent", "target_uuid")]
        public static string VibeTool_isa_execute(Dictionary<string, string> q) {
            try {
                var order = new WorkOrder {
                    intent = q["intent"],
                    target_uuid = q.ContainsKey("target_uuid") ? q["target_uuid"] : ""
                };
                return VibeISA.ExecuteIntent(order);
            } catch (Exception e) {
                return JsonUtility.ToJson(new BasicRes { error = "ISA Execution Failed: " + e.Message });
            }
        }
    }
}
#endif
// Force Recompile Sun Feb  1 09:45:28 PM CST 2026
