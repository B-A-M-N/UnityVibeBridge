#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
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
