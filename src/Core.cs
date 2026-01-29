using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        private enum BridgeState { Stopped, Starting, Running, Stopping }
        private static BridgeState _currentState = BridgeState.Stopped;
        private static string _inboxPath, _outboxPath;
        private static bool _isProcessing = false;

        static VibeBridgeServer() {
            AssemblyReloadEvents.beforeAssemblyReload += () => Teardown();
            EditorApplication.quitting += () => Teardown();
            Startup();
        }

        public static void Init() { Startup(); }
        public static void ManualInit() { Startup(); }

        private static void Startup() {
            _inboxPath = Path.Combine(Directory.GetCurrentDirectory(), "vibe_queue/inbox");
            _outboxPath = Path.Combine(Directory.GetCurrentDirectory(), "vibe_queue/outbox");
            if (!Directory.Exists(_inboxPath)) Directory.CreateDirectory(_inboxPath);
            if (!Directory.Exists(_outboxPath)) Directory.CreateDirectory(_outboxPath);
            
            LoadOrCreateSession();
            LoadRegistry();
            StartHttpServer();
            EditorApplication.update -= PollAirlock;
            EditorApplication.update += PollAirlock;
            _currentState = BridgeState.Running;
            Debug.Log("[VibeBridge] Modular Server v16 Running.");
        }

        private static void Teardown() {
            _currentState = BridgeState.Stopping;
            StopHttpServer();
            EditorApplication.update -= PollAirlock;
            _currentState = BridgeState.Stopped;
        }

        private static void PollAirlock() {
            if (_currentState != BridgeState.Running || _isProcessing) return;
            // if (EditorApplication.isPlaying || EditorApplication.isPaused) return;

            UpdateColorSync();
            HandleHttpRequests();

            string[] pending = Directory.GetFiles(_inboxPath, "*.json");
            if (pending.Length == 0) return;

            _isProcessing = true;
            try {
                var sortedFiles = pending.Select(f => new FileInfo(f)).OrderBy(f => f.CreationTime).ToArray();
                foreach (var fileInfo in sortedFiles) {
                    ProcessAirlockFile(fileInfo.FullName);
                }
            } finally { _isProcessing = false; }
        }

        private static void ProcessAirlockFile(string path) {
            string content = File.ReadAllText(path);
            string fileName = Path.GetFileName(path);
            string responsePath = Path.Combine(_outboxPath, "res_" + fileName);

            try {
                var command = JsonUtility.FromJson<AirlockCommand>(content);
                string result = ExecuteAirlockCommand(command);
                File.WriteAllText(responsePath, result);
            } catch (Exception e) {
                File.WriteAllText(responsePath, "{\"error\":\"" + e.Message + "\"}");
            } finally {
                try { File.Delete(path); } catch {}
            }
        }
    }
}