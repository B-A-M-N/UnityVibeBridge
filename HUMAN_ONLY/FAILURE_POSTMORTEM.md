# Failure Postmortem: The "Hallucinated Purge" Scenario

## The Scenario
An AI Agent is tasked with "cleaning up" an avatar hierarchy. Due to a model hallucination or a malformed regex, the agent identifies the `Armature/Hips` bone as "temporary junk" and attempts to delete it.

In a naive implementation, the bridge would execute `DestroyImmediate(hips)`, immediately corrupting the avatar's skeletal structure and potentially breaking nested prefabs or breaking the scene state beyond a simple undo.

## Why it fails safely in UnityVibeBridge

### 1. Safe Deletion Semantics (The Actuator Lock)
*   **Mechanism**: The server maintains a `_agentCreatedIDs` whitelist.
*   **Result**: When the agent calls `destroy("Armature/Hips")`, the bridge checks if that InstanceID was created by the agent in the current session. Since the Armature is part of the user's original model, the ID is not in the whitelist.
*   **Outcome**: The bridge returns a `Security Block: Cannot destroy an object not created by the agent.` error. The mutation is blocked at the gate.

### 2. Transactional Bounding (The Blast Shield)
*   **Mechanism**: All multi-step operations are wrapped in `begin_transaction` / `rollback_transaction`.
*   **Result**: Even if a minor (allowed) deletion occurred before the hallucinated purge, the agent (or the user) can invoke a rollback.
*   **Outcome**: Unity's `Undo` system reverts the scene to the exact state before the transaction began. No "scar tissue" is left in the hierarchy.

### 3. Capability Discovery (The Safety Manual)
*   **Mechanism**: `get_object_capabilities`
*   **Result**: If the agent is well-behaved, it calls `get_object_capabilities("Armature/Hips")` first. The bridge returns `isPrefab: true`, `canReparent: false` (or other safety flags).
*   **Outcome**: A competent agent sees these flags and recognizes that deleting or modifying this object is a high-risk operation, likely avoiding it before ever sending the `destroy` command.

### 4. Read-Only Mode (The Kill Switch)
*   **Mechanism**: `set_server_mode("readonly")`
*   **Result**: If the user sees the agent behaving erratically in the logs, they can toggle the kill switch.
*   **Outcome**: All subsequent mutation attempts return `403 Forbidden`, effectively "freezing" the agent in a pure observation state until the user intervenes.

## Conclusion
The system does not rely on the agent's "good intentions." It relies on **structural impossibilities**. By making corruption technically impossible to express through the API, we achieve "Mechanistic Vibe Coding."
