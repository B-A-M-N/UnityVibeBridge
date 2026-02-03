# 🛡️ Strict AI Engineering Constraints: UnityVibeBridge (v1.5.0)

All code generation and AI operations for this project MUST strictly adhere to these mandates. **Mechanical rejection will occur for code bypassing these constraints.**

---

## 0. 🛠️ Modern Architecture Mandates (v2.0)
1.  **Async-First Execution**: All new tools MUST be implemented using `UniTask` and `AsyncUtils`. Never block the main thread.
2.  **No Reflection in Serialization**: `JsonUtility` is deprecated for complex types. Use `MemoryPack` for all internal IPC and state persistence. Classes must be marked `partial` and `[MemoryPackable]`.
3.  **Hardened Core Persistence**: Use `SerializationUtils` and `AsyncUtils` from the `VibeBridge.Core` namespace for all system-level operations.
4.  **Single-Agent Unified Control**: This project has moved away from a 2-agent (Alpha/Beta) or Director/Specialist architecture. A single agent instance now handles both high-level planning (Conductor) and mechanistic execution (Operator). The agent MUST maintain a single, consistent state of belief and state-hash invariance.

---

## 1. Tone & Operational Integrity
1.  **Clinical Objectivity**: Use clinical language. Assume the Editor lies; reality is only proven via `inspect`.
2.  **Epistemic Integrity**: Only act on beliefs in `active_beliefs`. Never "guess" a success.
3.  **Read-Before-Write**: Every mutation MUST be preceded by an `inspect` call.
4.  **Zero-Hardcoding**: NEVER use absolute home directory paths (e.g. `/home/user/...`). Use relative paths or dynamic resolution (`os.getcwd()`) to ensure environment invariance and portability.
5.  **UUID-First Identity**: UUIDs are the authoritative identity of all objects. Unity `InstanceID`s are volatile session caches. Every operation must start by resolving the `UUID` to the current `InstanceID` via the `vibe_registry.json`.

---

## 2. 🔐 Triple-Lock & Transactional Gate
- **Atomic Transactions**: Every mutation MUST be wrapped in `transaction_begin` and `transaction_commit`.
- **The Triple-Lock Commitment**: `transaction_commit` REQUIRES:
    1.  `rationale`: Clinical explanation of the change and its impact.
    2.  `state_hash`: The current `error_hash` from `engine/error/state` to prove the session is stable.
    3.  `monotonic_tick`: The current incrementing session counter from `status`.
    4.  `git_commit_hash`: The hash of the latest `.git_safety` snapshot to ensure temporal invariance.
- **Log-Enforced Workflow**:
    - **Pre-Flight**: Acquisition of `engine/error/state` is mandatory before starting any transaction.
    - **Post-Flight**: Re-verify `engine/error/state` after commit. Any new errors trigger an immediate `ROLLBACK`.
- **Conductor Invariance**:
    - **Plan Primacy**: At the start of every turn, read `plan.md` to verify the active task.
    - **Task Linkage**: Reference the specific Task ID in the transaction `rationale`.
    - **Snap-Commit Requirement**: Never mark a task as `[x]` Done in `plan.md` unless a `snap_commit` (safety snapshot) has been generated for that specific work unit.

---

## 3. 📦 Git Isolation Mandate (Iron Box)
- **Primary Repo (.git)**: Kernel logic only. NEVER commit `.mat`, `.fbx`, `.png`, or scene data here.
- **Safety Repo (.git_safety)**: Mandatory local-only "Save Game" system for project snapshots.
- **Snapshot Protocol**: Perform a snapshot before any high-risk operation (Baking, Export, or Bulk Mutation):
  `scripts/snap_commit.py "[Snapshot Name]"`

---

## 4. 🛠️ Safe Tool Integration Checklist
- **Canonical Source**: The ground truth is `Assets/VibeBridge/`. Treat `unity-package/` as a secondary export template.
- **Incrementalism**: Add only ONE method or payload at a time.
- **Isolation**: Where feasible, place new tools in a separate `.asmdef` to protect the Kernel from compilation side-effects.
- **The 20-Second Rule**: MUST wait 20+ seconds for Unity to finish recompiling after every `.cs` edit before calling a tool.
- **Syntax Pre-Flight**: Write code to a temp file first. Manually verify all brackets `{ }` and semicolons `;`. Only then move to `Assets/`.
- **No Duplication**: Always `grep` for existing `VibeTool_` prefixes to prevent "Ambiguous Match" compiler errors.

---

## 5. Asset Database & Filesystem Safety
- **NO DIRECT DISK MUTATION**: Direct modification of Unity assets (`.mat`, `.cs`, `.meta`, `.prefab`) via shell commands while Unity is running is **STRICTLY FORBIDDEN**. It triggers AssetDatabase race conditions.
- **Transactional Bridge Protocol**: All mutations MUST go through the `VibeBridgeServer` API. If a tool is missing, implement it as a Payload following the **Safe Tool Integration Checklist**.

---

## 6. 🚫 Model Behavior & Anti-Bypass Mandates
1.  **SCRIPT-BYPASS PROHIBITION**: AI Agents are **STRICTLY FORBIDDEN** from creating or running standalone Python scripts (e.g. `check_errors.py`, `inspect_candidates.py`) to interact with the Unity Bridge on port 8085. All interactions MUST occur through registered MCP tools.
2.  **MANDATORY ERROR AWARENESS**: Every tool response contains a `_vibe_invariance` block. The agent MUST inspect the `script_error_count` in this block after EVERY tool call.
3.  **REACTIONARY INVARIANCE**: If `script_error_count > 0`, the agent **MUST STOP ALL MUTATIONS** and immediately call `telemetry/get/errors` to diagnose the failure. Ignoring a non-zero error count is a Critical Invariance Failure.
4.  **NO SHELL-BASED CREATION**: Never use `echo` or `cat` to create `.cs` or `.py` files to bypass the `mutate_script` security gate. The `mutate_script` tool is the ONLY authorized way to write code to the project.

---

**VIOLATION OF THESE CONSTRAINTS CONSTITUTES AN IMMEDIATE SYSTEM RISK.**
