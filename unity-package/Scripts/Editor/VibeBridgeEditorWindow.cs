#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityVibeBridge.Kernel.Core;
using UnityVibeBridge.Kernel;

namespace UnityVibeBridge.Kernel.Editor {
    /// <summary>
    /// Vibe Panel: The Human-In-The-Loop Approval Gate.
    /// v1.0: Industrial Safety Gate
    /// </summary>
    public class VibeBridgeEditorWindow : EditorWindow {
        private static string _pendingIntent = null;
        private static string _pendingRationale = "No rationale provided.";
        
        [MenuItem("Vibe/Control Panel")]
        public static void ShowWindow() {
            GetWindow<VibeBridgeEditorWindow>("Vibe Panel");
        }

        public static void RequestApproval(string intent, string rationale) {
            _pendingIntent = intent;
            _pendingRationale = rationale;
            // Force focus
            EditorWindow.FocusWindowIfItsOpen<VibeBridgeEditorWindow>();
        }

        private void OnGUI() {
            GUILayout.Label("UnityVibeBridge: Industrial Kernel", EditorStyles.boldLabel);
            
            if (string.IsNullOrEmpty(_pendingIntent)) {
                EditorGUILayout.HelpBox("System Idle. Waiting for Work Order...", MessageType.Info);
            } else {
                EditorGUILayout.BeginVertical("box");
                GUILayout.Label("🚨 PENDING MUTATION", EditorStyles.whiteLargeLabel);
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Intent:", _pendingIntent, EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(_pendingRationale, MessageType.Warning);
                GUILayout.Space(10);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("APPROVE", GUILayout.Height(40))) {
                    VibeBridgeServer.ApproveMutation(_pendingIntent);
                    _pendingIntent = null;
                }
                if (GUILayout.Button("VETO", GUILayout.Height(40))) {
                    VibeBridgeServer.ReportViolation("Human Vetoed Intent: " + _pendingIntent);
                    _pendingIntent = null;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear Panic State")) {
                VibeBridgeServer.ResetSecurity();
            }
        }
    }
}
#endif
