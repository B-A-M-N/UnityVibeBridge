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
using System.Reflection;

namespace VibeBridge {
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
            // We can add logic here to send an unlock command if we want
            Debug.Log("[VibeBridge] Manual Unlock Requested via Editor Menu.");
        }
    }
}
#endif
