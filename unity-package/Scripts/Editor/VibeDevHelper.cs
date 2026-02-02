#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
// UnityVibeBridge: The Governed Creation Kernel for Unity
// Copyright (C) 2026 B-A-M-N
//
// This software is dual-licensed under the GNU AGPLv3 and a 
// Commercial "Work-or-Pay" Maintenance Agreement.
//
// You may use this file under the terms of the AGPLv3, provided 
// you meet all requirements (including source disclosure).
//
// For commercial use, or to keep your modifications private, 
// you must satisfy the requirements of the Commercial Path 
// as defined in the LICENSE file at the project root.

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityVibeBridge.Kernel;

namespace UnityVibeBridge.Kernel.Editor {
    public static class VibeDevHelper
    {
        [MenuItem("VibeBridge/Reload Bridge %#r")] // Ctrl/Cmd + Shift + R
        public static void ReloadBridge()
        {
            try {
                VibeBridgeServer.Reinitialize();
                Debug.Log("<color=green>[VibeBridge] Bridge Session Re-Initialized Successfully!</color>");
            } catch (Exception e) {
                Debug.LogError($"[VibeBridge] Failed to re-initialize bridge: {e}");
            }
        }

        [MenuItem("VibeBridge/Security/Unlock Emergency Switch")]
        public static void UnlockBridge()
        {
            VibeBridgeServer.VibeTool_system_unveto(new Dictionary<string, string>());
            Debug.Log("[VibeBridge] Manual Unlock via Editor Menu.");
        }

        // --- SELECTION TOOLS ---

        [MenuItem("VibeBridge/Selection/Copy Selected ID %#i")] // Ctrl/Cmd + Shift + I
        public static void CopySelectedID() {
            GameObject go = Selection.activeGameObject;
            if (go == null) { Debug.LogWarning("[Vibe] Nothing selected."); return; }
            GUIUtility.systemCopyBuffer = go.GetInstanceID().ToString();
            Debug.Log($"[Vibe] Copied ID to clipboard: {go.GetInstanceID()} ({go.name})");
        }

        [MenuItem("VibeBridge/Selection/Copy Selected Path %#p")] // Ctrl/Cmd + Shift + P
        public static void CopySelectedPath() {
            GameObject go = Selection.activeGameObject;
            if (go == null) { Debug.LogWarning("[Vibe] Nothing selected."); return; }
            
            string path = AssetDatabase.GetAssetPath(go);
            if (string.IsNullOrEmpty(path)) {
                // Scene Object: Build hierarchy path
                path = go.name;
                Transform t = go.transform;
                while (t.parent != null) {
                    t = t.parent;
                    path = t.name + "/" + path;
                }
            }
            
            GUIUtility.systemCopyBuffer = path;
            Debug.Log($"[Vibe] Copied Path to clipboard: {path}");
        }

        [MenuItem("VibeBridge/Selection/Log Details")]
        public static void LogSelectionDetails() {
            GameObject go = Selection.activeGameObject;
            if (go == null) { Debug.LogWarning("[Vibe] Nothing selected."); return; }
            
            string info = $"<b>{go.name}</b>\nID: {go.GetInstanceID()}\nLayer: {LayerMask.LayerToName(go.layer)} ({go.layer})\nTag: {go.tag}\n";
            info += $"Static: {GameObjectUtility.GetStaticEditorFlags(go)}\n";
            info += $"Components: {string.Join(", ", System.Linq.Enumerable.Select(go.GetComponents<Component>(), c => c != null ? c.GetType().Name : "<Missing Script>"))}";
            Debug.Log($"[Vibe] Selection Details:\n{info}");
        }
    }
}
#endif
