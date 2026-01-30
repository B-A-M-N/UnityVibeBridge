# 🛡️ MCP Unity Control Plane: The Operating System

**Core Philosophy:** Unity is a hostile, non-deterministic environment. The MCP must act as an **Operating System**, managing state, memory, and execution context to prevent silent corruption.

---

## 1. 🧬 The Kernel Tier (Deep Safety Primitives)
**Objective:** Prevent Race Conditions, Compilation Poisoning, and Context Desync.

### 🔒 Guard Module (The Gatekeeper)
*   **Mechanism:** `IsSafeToMutate()` check before every mutation.
*   **Function:** Returns `false` if `EditorApplication.isCompiling`, `isPlaying`, or `isUpdating`.
*   **Defense:** Prevents the "Script Reload Death Loop" where an external tool tries to mutate assets while Unity is locking the Asset Database.

### 🛑 Lifecycle Manager (Domain Reload Handler)
*   **Mechanism:** `AssemblyReloadEvents` hooks.
*   **Function:** Writes `metadata/vibe_status.json` = `Reloading` before reload, and `Ready` after.
*   **Defense:** Prevents the server from sending requests into the void during the 5-30s reload window.

### ⚡ Telemetry & Heartbeat (Liveness Proof)
*   **Mechanism:** `Application.logMessageReceived` + `EditorApplication.update`.
*   **Function:** Buffers errors and writes a timestamped health file every 1s.
*   **Defense:** Distinguishes between "Unity is thinking" (Heartbeat active) and "Unity is dead/crashed" (Heartbeat stale).

---

## 2. 🧱 The Filesystem Tier (Data Integrity)
**Objective:** Manage Assets like inodes.

### 🧹 Optimization Fork
*   **Tool:** `opt/fork`
*   **Function:** Duplicates an Avatar/Object hierarchy into a `_QuestGenerated` folder before applying destructive modifiers (Decimation, Shader Swap).
*   **Defense:** "Non-destructive Destruction". If the optimization fails, the original asset is untouched.

### 📄 Export Contract
*   **Mechanism**: Strict Schema Validation via `export/validate`.
*   **Function**: Rejects FBX export if: `Scale != 1.0`, `Rotation != 0`, or `Missing Scripts > 0`.
*   **Defense**: Fails fast in Unity, not slowly in Blender.


---

## 3. 🛡️ The Application Tier (Recovery)
**Objective:** Undo, Redo, and Audit.

### ⚛️ Atomic Transaction Wrapper (Implemented)
*   **Function:** Wraps multi-step operations (e.g., "Swap 5 Materials") in `Undo.IncrementCurrentGroup` -> `Try` -> `Undo.RevertAllDownToGroup (on Fail)`.
*   **Status:** **ACTIVE**.

### 🚑 Forensic Audit (Implemented)
*   **Tool:** `logs/vibe_audit.jsonl`
*   **Function:** Logs every capability request (`MUTATE_ASSET`, `STRUCTURAL`) with a timestamp.
*   **Defense:** Provides a "Black Box" to replay the sequence of events that led to a crash.

---

## 🚀 Implementation Roadmap

1.  **Phase 1 (Complete):** Guard, Lifecycle, Telemetry, Transactions.
2.  **Phase 2 (Immediate):** **Asset Database Lock** (Prevent infinite import loops).
3.  **Phase 3 (Next):** Prefab & Scene Drift Detection.
