using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        
        // --- TRANSACTION MODULE (The Atomic Operator) ---
        // Wraps operations in undo groups and handles failures.

        private static int _currentTransactionGroup = -1;

        public static void BeginTransaction(string name) {
            EnforceGuard(); // Dependency on GuardModule
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(name);
            _currentTransactionGroup = Undo.GetCurrentGroup();
        }

        public static void CommitTransaction() {
            if (_currentTransactionGroup == -1) return;
            Undo.CollapseUndoOperations(_currentTransactionGroup);
            _currentTransactionGroup = -1;
        }

        public static void RollbackTransaction() {
            if (_currentTransactionGroup != -1) {
                Undo.RevertAllDownToGroup(_currentTransactionGroup);
                _currentTransactionGroup = -1;
            }
        }

        public static string VibeTool_transaction_begin(Dictionary<string, string> q) {
            BeginTransaction(q.ContainsKey("name") ? q["name"] : "Unnamed Transaction");
            return "{\"message\":\"Transaction started\",\"groupId\":" + _currentTransactionGroup + "}";
        }

        public static string VibeTool_transaction_commit(Dictionary<string, string> q) {
            CommitTransaction();
            return "{\"message\":\"Transaction committed\"}";
        }

        public static string VibeTool_transaction_abort(Dictionary<string, string> q) {
            RollbackTransaction();
            return "{\"message\":\"Transaction rolled back\"}";
        }
    }
}
