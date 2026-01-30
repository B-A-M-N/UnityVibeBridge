using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        
        // --- GUARD MODULE (The Gatekeeper) ---
        // Prevents mutations during unstable Editor states.

        public static bool IsSafeToMutate() {
            if (EditorApplication.isCompiling) return false;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return false;
            if (EditorApplication.isUpdating) return false;
            return true;
        }

        public static string VibeTool_guard_status(Dictionary<string, string> q) {
            return "{\"isCompiling\":" + EditorApplication.isCompiling.ToString().ToLower() + 
                   ",\"isPlaying\":" + EditorApplication.isPlaying.ToString().ToLower() + 
                   ",\"isUpdating\":" + EditorApplication.isUpdating.ToString().ToLower() + 
                   ",\"safe\":" + IsSafeToMutate().ToString().ToLower() + "}";
        }

        public static string VibeTool_guard_await_compilation(Dictionary<string, string> q) {
            if (EditorApplication.isCompiling) {
                return "{\"status\":\"waiting\",\"message\":\"Editor is compiling\"}";
            }
            return "{\"status\":\"ready\"}";
        }

        private static void EnforceGuard() {
            if (!IsSafeToMutate()) {
                throw new Exception("UNSAFE_STATE: Editor is compiling, playing, or updating. Operation rejected.");
            }
        }
    }
}

