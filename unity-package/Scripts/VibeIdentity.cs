#if UNITY_EDITOR
using UnityEngine;

namespace VibeBridge {
    [DisallowMultipleComponent]
    public class VibeIdentity : MonoBehaviour {
        [SerializeField] private string _uuid;
        public string UUID {
            get => _uuid;
            set => _uuid = value;
        }
    }
}
#endif
