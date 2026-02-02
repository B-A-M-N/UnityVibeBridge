#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace UnityVibeBridge.Kernel {
    public static partial class VibeBridgeServer {
        private static void LoadOrCreateSession() {
            string path = "metadata/vibe_session.json";
            if (File.Exists(path)) try { _persistentNonce = JsonUtility.FromJson<SessionData>(File.ReadAllText(path)).sessionNonce; } catch { _persistentNonce = Guid.NewGuid().ToString().Substring(0, 8); } 
            else _persistentNonce = Guid.NewGuid().ToString().Substring(0, 8);
        }
    }
}
#endif
