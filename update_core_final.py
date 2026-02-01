import os

core_file = "/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/VibeBridge/VibeBridgeKernel.Core.cs"
with open(core_file, 'r') as f:
    content = f.read()

# 1. Add ping tool
# 2. Add enforced commit tool (overwriting existing simple one)
# 3. Add rationale/tick fields to the partial class if they aren't there

if "VibeTool_system_ping" not in content:
    # Insert before the closing brace of the class
    pos = content.rfind("    }")
    if pos != -1:
        new_tools = """
        public static string VibeTool_system_ping(Dictionary<string, string> q) {
            return "{\\"message\\":\\"pong\\",\\"tick\\":\\" + _monotonicTick + "\\"}";
        }

        public static string VibeTool_transaction_commit(Dictionary<string, string> q) {
            if (!q.ContainsKey("rationale") || !q.ContainsKey("state_hash") || !q.ContainsKey("monotonic_tick")) {
                return JsonUtility.ToJson(new BasicRes { error = "TRIPLE_LOCK_VIOLATION: commit_transaction requires rationale, state_hash, and monotonic_tick." });
            }
            _monotonicTick++;
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            return JsonUtility.ToJson(new BasicRes { message = "Committed with rationale: " + q["rationale"], id = _monotonicTick });
        }
"""
        # Find where VibeTool_transaction_commit is currently and remove it to avoid duplicate
        old_commit_start = content.find("public static string VibeTool_transaction_commit")
        if old_commit_start != -1:
            old_commit_end = content.find("}", old_commit_start) + 1
            content = content[:old_commit_start] + content[old_commit_end:]
            # Re-find pos after removal
            pos = content.rfind("    }")

        new_content = content[:pos] + new_tools + content[pos:]
        with open(core_file, 'w') as f:
            f.write(new_content)
        print("Successfully updated VibeBridgeKernel.Core.cs with Enforced Commit and Ping.")
else:
    print("Updates already present.")
