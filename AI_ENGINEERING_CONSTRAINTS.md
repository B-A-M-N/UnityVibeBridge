# AI Engineering Constraints & Safety Contract

This document defines the non-negotiable structural constraints for AI-generated code in this project. All contributions must adhere to these rules. Violations are considered critical bugs.

## 1. The "Iron Box" (Capability Scoping)
*   **Principle of Least Privilege**: All bridge requests MUST specify a required capability (e.g., `READ`, `MUTATE_SCENE`, `MUTATE_ASSET`, `STRUCTURAL_CHANGE`).
*   **Non-Composition**: Capabilities are single-use and non-composable.
*   **Implicit Deny**: Any request lacking the necessary capability token is rejected before processing.

## 2. Forensic Audit & Replay
*   **Immutable Log**: Every mutation must be logged to `logs/vibe_audit.jsonl` with: `Timestamp`, `RequestID`, `Capability`, `TargetGUID`, and `SerializedDelta`.
*   **State Checkpointing**: Strategic Actions must be reproducible via the audit log on a clean clone of the project.

## 3. Serialization & Asset Paranoia
*   **YAML Validation**: All `.asset`, `.prefab`, and `.unity` mutations must undergo schema validation.
*   **GUID Integrity**: Block any operation that creates GUID collisions or references scripts outside the trusted `/src` or whitelisted SDK directories.
*   **Type Matching**: Strictly enforce that serialized values match the field type in the target component.

## 4. The Persistence Boundary
*   **Restart Isolation**: MCP-originated changes are considered "Transient" until a human performs a `Finalize Workspace` action.
*   **Auto-Revert**: On editor startup, the bridge will diff the current state against the last `Trusted Snapshot`. Discrepancies trigger a mandatory human review.

## 5. Human-in-the-Loop (HITL)
*   **Structural Triggers**: The following require explicit, out-of-band human confirmation (via CLI or Modal):
    *   Creation/Deletion of any `.cs` or `.asmdef` file.
    *   Modification of `ProjectSettings` or `BuildPipeline` state.
    *   Adding components not in the "Safe Set".

## 6. Emergency Kill Switch
*   **The Red Button**: A physical toggle (or `KILL_VIBE` env var) that instantly places the bridge in a read-only, non-persistent state.
*   **Self-Destruct**: The bridge must self-disable if it detects unauthorized tampering with `SecurityModule.cs` or the `vibe_audit.log`.

## 3. The "Universal Freeze" (Stability)
*   **The Engine (C#)**: **ENGINE IS NOW FROZEN (v3.6-stable)**. No further modifications to `src/*.cs` are permitted. All creative intents must be executed via the existing API endpoints.
*   **The Heuristics**: Search logic must be trait-based (Fingerprinting), not path-based.
*   **The Map (JSON)**: Avatar-specific data stays in `vibe_registry.json`.

## 4. Fingerprinting & Confidence Tiers (Anti-Hallucination)
*   **Trait Signatures**: Every target must be verified via Fingerprint: `(MeshName + VertexCount + MaterialCount + ShaderNames)`.
*   **Tier 1: Perfect Match (Silent)**: ID change + 100% Fingerprint match = Silent Update.
*   **Tier 2: Fuzzy Match (Passive Warning)**: Minor changes (vertex count shift) = Proceed with Log Warning.
*   **Tier 3: Ambiguous Match (Hard Stop)**: Double-match or broken signature = **HALT**. Re-discovery required.

## 5. Visual Anchors & Cognitive Load
*   **Visual Audit Rule**: For all Tier 3 ambiguities or Strategic Actions, the AI **MUST** trigger a screenshot and present it to the human for visual confirmation.
*   **Low-Load Interface**: Use thumbnails/screenshots over raw technical logs for human verification.

## 6. Intent-Based Grouping (Beginner Guardrails)
*   **Strategic Actions**: Group tactical operations (e.g. crushing 50 textures) into 1 Strategic Intent.
*   **ELI5 Impact Statements**: Every Strategic Action requires a Human-Friendly Impact Statement explaining the **visual and performance result**.
*   **Forced Snapshots**: Strategic Actions MUST trigger a `create_checkpoint` before execution.

## 7. Context Management (Anti-Bloat)
*   **JIT (Just-In-Time) Context**: Do NOT read the entire `vibe_registry.json`. Use specific lookup tools (`get_target_data`) to prevent context confusion.

## 8. Single Narrow API Layer
*   All Unity mutations must go through the designated `VibeBridgeServer` class.
*   New operations must be added by extending this API, not by writing ad-hoc scripts.

## 9. Mandatory Transactions
*   **Implicit Wrapping**: Every mutation MUST be performed within a named transaction group.
*   **Auto-Rollback**: On any server-side exception (500), the bridge must rollback the active transaction.

## 12. Hardware Safety Railings
*   **Physics & Light Caps**: Do not exceed hard caps for intensity (10k) or range (1k). The bridge will mechanically reject these "Light Bombs."
*   **Spawn Limits**: Avoid spawning more than 50 objects in a single intent. Batch operations should be throttled.
*   **VRAM Protection**: Texture resolutions are capped at 4096px. Attempting to set higher resolutions on importers will trigger a `SANITY_CHECK` failure.

## The Meta-Rule
If a proposed solution is unusually short, clever, or bypasses a limitation, assume it is wrong. Prioritize safety and explicit verification over brevity.