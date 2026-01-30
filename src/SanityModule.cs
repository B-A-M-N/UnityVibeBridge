using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        
        // --- SANITY MODULE (Hardware Safety Railings) ---
        // Enforces hard caps on values to prevent Editor crashes, VRAM overflow, or DoS.

        private const float MAX_LIGHT_INTENSITY = 10000f;
        private const float MAX_LIGHT_RANGE = 1000f;
        private const int MAX_SPAWN_COUNT = 50; // Per single request
        private const int MAX_TEXTURE_SIZE = 4096;

        public static void ValidateSanity(AirlockCommand cmd) {
            string action = cmd.action.ToLower();

            // 1. Light Safety
            if (action.Contains("light") || action.Contains("intensity")) {
                CheckLightSanity(cmd);
            }

            // 2. Spawn Safety
            if (action.Contains("spawn") || action.Contains("instantiate")) {
                CheckSpawnSanity(cmd);
            }

            // 3. Texture Safety
            if (action.Contains("texture") || action.Contains("crush")) {
                CheckTextureSanity(cmd);
            }
        }

        private static void CheckLightSanity(AirlockCommand cmd) {
            for (int i = 0; i < cmd.keys.Length; i++) {
                if (cmd.keys[i].ToLower() == "intensity") {
                    if (float.TryParse(cmd.values[i], out float val)) {
                        if (val > MAX_LIGHT_INTENSITY) throw new Exception($"SANITY_CHECK: Intensity {val} exceeds cap ({MAX_LIGHT_INTENSITY})");
                    }
                }
                if (cmd.keys[i].ToLower() == "range") {
                    if (float.TryParse(cmd.values[i], out float val)) {
                        if (val > MAX_LIGHT_RANGE) throw new Exception($"SANITY_CHECK: Range {val} exceeds cap ({MAX_LIGHT_RANGE})");
                    }
                }
            }
        }

        private static void CheckSpawnSanity(AirlockCommand cmd) {
            // If we add a batch spawn tool, we'd check count here.
            // For single spawn, we just ensure it's not being spammed in a loop (handled by rate limiting/server).
        }

        private static void CheckTextureSanity(AirlockCommand cmd) {
            for (int i = 0; i < cmd.keys.Length; i++) {
                if (cmd.keys[i].ToLower() == "maxsize") {
                    if (int.TryParse(cmd.values[i], out int val)) {
                        if (val > MAX_TEXTURE_SIZE) throw new Exception($"SANITY_CHECK: Texture size {val} exceeds cap ({MAX_TEXTURE_SIZE})");
                    }
                }
            }
        }
    }
}
