#if UNITY_EDITOR
using System;

namespace UnityVibeBridge.Kernel.Core {
    public class VibeValidationException : Exception {
        public string Conclusion { get; }
        public VibeValidationException(string message, string conclusion = "VALIDATION_FAILED") : base(message) {
            Conclusion = conclusion;
        }
    }

    public class VibeSecurityException : Exception {
        public VibeSecurityException(string message) : base(message) { }
    }
}
#endif
