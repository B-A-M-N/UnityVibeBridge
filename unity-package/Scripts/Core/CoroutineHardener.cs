using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace VibeBridge.Core
{
    public static class CoroutineHardener
    {
        /// <summary>
        /// Yields one frame, then aggressively verifies the world state hasn't shifted.
        /// </summary>
        public static IEnumerator YieldAndVerify(object trackedObject = null)
        {
            yield return null; // Wait for next frame

            if (EditorApplication.isCompiling)
            {
                throw new System.Exception("VibeBridge: Abort - Compilation started during coroutine.");
            }

            if (trackedObject != null && trackedObject.Equals(null))
            {
                throw new System.Exception("VibeBridge: Abort - Tracked object was destroyed.");
            }
        }
    }
}
