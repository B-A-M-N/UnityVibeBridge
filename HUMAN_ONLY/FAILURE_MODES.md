# UnityVibeBridge: Failure Modes & Recovery Protocols

This document defines the canonical taxonomy of failures within the Bridge/Kernel cluster and the required response for each.

---

## 🟥 1. Terminal Failures (Immediate PANIC)
*Definition: Any failure that compromises state integrity or trust boundaries.*

| Failure | Cause | Protocol |
| :--- | :--- | :--- |
| **Contract Violation** | MCP server sends malformed JSON. | Instant **HALT**; Return 500; Log to Audit. |
| **Security Breach** | Blocked pattern (e.g., `Reflection`) detected. | Instant **HALT**; Set `vibe_status` to `Error`. |
| **Deadlock** | Heartbeat timeout (>15s). | Trigger **Circuit Breaker**; Invalidate Session. |
| **Sandbox Leak** | Attempt to access `HUMAN_ONLY/`. | Mechanical Rejection; Log Critical Violation. |

---

## 🟨 2. Recoverable Failures (Auto-Rollback)
*Definition: Operational errors that can be reverted via the Undo system.*

| Failure | Cause | Protocol |
| :--- | :--- | :--- |
| **Mutation Fail** | Unity exception during tool call. | Execute `transaction_abort`; Notify AI. |
| **Guard Block** | Mutation attempted during compilation. | Abort; AI MUST wait for `vibe_status` = `Ready`. |
| **Sanity Fail** | GPU/VRAM limit exceeded (e.g. 16k tex). | Mechanical Rejection; Error returned to AI. |
| **Hierarchy Drift** | Target InstanceID no longer valid. | Trigger **Re-Discovery** via `get_hierarchy`. |

---

## 🟦 3. Truth Reconciliation
*Definition: Protocol for when the "Numerical Truth" of the scene is in doubt.*

1.  **Halt**: If a tool fails due to a missing component, the AI **MUST NOT** attempt to "re-invent" the component.
2.  **Inspect**: AI must call `inspect_object` on the parent to verify state.
3.  **Audit**: If multiple errors occur, the AI must call `get_telemetry_errors` to see the actual Unity console output before retrying.

---

## 🛠️ Human Intervention Policy
Human intervention is mandatory **ONLY** when:
- The Kernel is in an `Error` state (Requires `VibeDevHelper` reload).
- A `Terminal Failure` is logged in the `vibe_audit.jsonl`.
- The AI has attempted 3 re-discovery loops without finding the target.

---

## 📝 Case Study: The "Hallucinated Purge" Scenario

**The Scenario**: An AI Agent is tasked with "cleaning up" an avatar hierarchy. Due to a model hallucination or a malformed regex, the agent identifies the `Armature/Hips` bone as "temporary junk" and attempts to delete it.

In a naive implementation, the bridge would execute `DestroyImmediate(hips)`, immediately corrupting the avatar's skeletal structure and potentially breaking nested prefabs or breaking the scene state beyond a simple undo.

### Why it fails safely in UnityVibeBridge

1.  **Safe Deletion Semantics (The Actuator Lock)**:
    *   **Mechanism**: The server maintains a `_agentCreatedIDs` whitelist.
    *   **Result**: When the agent calls `destroy("Armature/Hips")`, the bridge checks if that InstanceID was created by the agent in the current session.
    *   **Outcome**: The bridge returns a `Security Block: Cannot destroy an object not created by the agent.` error. The mutation is blocked at the gate.

2.  **Transactional Bounding (The Blast Shield)**:
    *   **Mechanism**: All multi-step operations are wrapped in `begin_transaction` / `rollback_transaction`.
    *   **Result**: Even if a minor (allowed) deletion occurred before the hallucinated purge, the agent (or the user) can invoke a rollback.
    *   **Outcome**: Unity's `Undo` system reverts the scene to the exact state before the transaction began.

3.  **Capability Discovery (The Safety Manual)**:
    *   **Mechanism**: `get_object_capabilities`
    *   **Result**: A well-behaved agent calls this first. The bridge returns `isPrefab: true`, `canReparent: false`.
    *   **Outcome**: The agent recognizes that deleting this object is a high-risk operation and avoids it.

4.  **Read-Only Mode (The Kill Switch)**:
    *   **Mechanism**: `set_server_mode("readonly")`
    *   **Outcome**: All subsequent mutation attempts return `403 Forbidden`, freezing the agent in an observation state until a human intervenes.

---
**Copyright (C) 2026 B-A-M-N**