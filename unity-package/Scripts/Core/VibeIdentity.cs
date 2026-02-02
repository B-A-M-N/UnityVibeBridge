#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;

namespace UnityVibeBridge.Kernel.Core {
    /// <summary>
    /// VibeIdentity: Authoritative UUID Persistence.
    /// Ensures AI references remain stable across renames and reparents.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class VibeIdentity : MonoBehaviour {
        [SerializeField, HideInInspector]
        private string _vibeUuid;

        public string Uuid => _vibeUuid;

        private void Awake() {
            if (string.IsNullOrEmpty(_vibeUuid)) {
                GenerateId();
            }
        }

        private void Reset() {
            GenerateId();
        }

        public void GenerateId() {
            _vibeUuid = Guid.NewGuid().ToString();
            EditorUtility.SetDirty(this);
        }

        [MenuItem("GameObject/Vibe/Add Identity", false, 10)]
        private static void AddIdentity(MenuCommand menuCommand) {
            GameObject go = menuCommand.context as GameObject;
            if (go != null && go.GetComponent<VibeIdentity>() == null) {
                Undo.AddComponent<VibeIdentity>(go);
            }
        }
    }
}
#endif
