using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public enum EditorCapability { None, Read, MutateScene, MutateAsset, Structural, Admin }

    public static partial class VibeBridgeServer {
        private static HashSet<int> _createdObjectIds = new HashSet<int>();
        private static string _persistentNonce = null;
        private const string SESSION_PATH = "metadata/vibe_session.json";

        public static void LoadOrCreateSession() {
            if (File.Exists(SESSION_PATH)) {
                try {
                    string json = File.ReadAllText(SESSION_PATH);
                    var data = JsonUtility.FromJson<SessionData>(json);
                    _persistentNonce = data.sessionNonce;
                    _createdObjectIds = new HashSet<int>(data.createdObjectIds);
                } catch { CreateNewSession(); }
            } else { CreateNewSession(); }
        }

        private static void CreateNewSession() {
            _persistentNonce = Guid.NewGuid().ToString().Substring(0, 8);
            _createdObjectIds = new HashSet<int>();
            SaveSession();
        }

        public static void SaveSession() {
            if (!Directory.Exists("metadata")) Directory.CreateDirectory("metadata");
            var data = new SessionData {
                sessionNonce = _persistentNonce,
                createdObjectIds = new List<int>(_createdObjectIds)
            };
            File.WriteAllText(SESSION_PATH, JsonUtility.ToJson(data, true));
        }
    }
}
