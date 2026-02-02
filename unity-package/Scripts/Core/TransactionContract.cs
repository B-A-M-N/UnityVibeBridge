#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityVibeBridge.Kernel.Core
{
    /// <summary>
    /// The Contract: Every critical mutation must carry this context.
    /// If missing, the tool must FAIL FAST.
    /// </summary>
    [Serializable]
    public struct TransactionContext
    {
        public string transactionId;       // UUID from Python
        public string expectedStateHash;   // The hash the AI thinks it's mutating
        public string gitCommitHash;       // The specific git commit the working tree MUST match
        public long issuedAtTick;          // Monotonic tick to prevent replay attacks

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(transactionId) && 
                   !string.IsNullOrEmpty(gitCommitHash);
        }
    }

    /// <summary>
    /// Interface for any tool that mutates state.
    /// </summary>
    public interface ITransactionalTool
    {
        ToolID ID { get; }
        bool RequiresSnapshot { get; }
        
        /// <summary>
        /// Validates the pre-conditions (Git Hash, State Hash) BEFORE execution.
        /// </summary>
        void ValidateContext(TransactionContext ctx);
    }
}
#endif