using System;
using UnityEngine;

namespace UnityVibeBridge.Kernel.Core
{
    /// <summary>
    /// Threading and Concurrency Utilities.
    /// Neutralized to remove third-party package dependencies (UniTask).
    /// </summary>
    public static class AsyncUtils
    {
        /// <summary>
        /// Placeholder for main-thread switching.
        /// In standard Unity, this is handled by the main thread polling loop.
        /// </summary>
        public static void SwitchToMainThreadSafe()
        {
            if (!UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
            {
                throw new System.Exception("VibeBridge: Concurrency Violation. Operation must run on Unity Main Thread.");
            }
        }
    }
}