using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace VibeBridge.Core
{
    /// <summary>
    /// HARDENING LAYER: Wraps UniTask to enforce Zero Trust concurrency constraints.
    /// Prevents 'Zombie Tasks' and execution during compilation/reloads.
    /// </summary>
    public static class AsyncUtils
    {
        /// <summary>
        /// Safely switches to the Main Thread, asserting that the Editor is stable.
        /// Throws if the Editor is compiling.
        /// </summary>
        public static async UniTask SwitchToMainThreadSafe(CancellationToken token = default)
        {
            await UniTask.SwitchToMainThread(token);

            // 1. Availability Check
            if (EditorApplication.isCompiling)
            {
                throw new OperationCanceledException("VibeBridge: Aborting async operation due to active compilation.");
            }

            // 2. Cancellation Check
            if (token.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }
        }

        /// <summary>
        /// Runs a task with a guaranteed timeout to prevent deadlocks in the bridge.
        /// </summary>
        public static async UniTask<T> RunWithTimeout<T>(Func<UniTask<T>> taskFactory, int timeoutMs, string debugName)
        {
            var timeoutSpan = TimeSpan.FromMilliseconds(timeoutMs);
            try
            {
                return await taskFactory().Timeout(timeoutSpan);
            }
            catch (TimeoutException)
            {
                Debug.LogError($"[VibeAsync] Task '{debugName}' timed out after {timeoutMs}ms. Failing fast.");
                throw;
            }
        }
    }
}
