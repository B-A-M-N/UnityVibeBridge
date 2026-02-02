#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityVibeBridge.Kernel {
    public static partial class VibeBridgeServer {
        private static void InitializeTelemetry() {
            Application.logMessageReceived -= OnLogReceived;
            Application.logMessageReceived += OnLogReceived;
            Debug.Log("[Vibe] Telemetry initialized.");
        }

        private static void OnLogReceived(string log, string stackTrace, LogType type) {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Warning) {
                string prefix = type == LogType.Warning ? "[WARN]" : $"[{type}]";
                _errors.Add($"{prefix} {log}");
                if (_errors.Count > 100) _errors.RemoveAt(0);
            }
        }

        [VibeTool("telemetry/get/errors", "Returns the last 100 errors captured by the telemetry engine.")]
        public static string VibeTool_telemetry_get_errors(Dictionary<string, string> q) { 
            return JsonUtility.ToJson(new ErrorRes { errors = _errors.ToArray() }); 
        } 
    }
}
#endif