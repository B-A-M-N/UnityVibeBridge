# 🔒 VibeBridge Governance & Human Override Policy

This document defines the "Fourth Order" of invariance: **Governance**. While the technical kernel enforces physical and epistemic constraints, this policy governs the human-in-the-loop procedures required to maintain system integrity.

---

## 🛑 1. The Veto Protocol (Emergency Stop)
The `VETOED` state is the primary mechanism for a human to seize control from the AI.

- **Trigger Conditions**: 
    - AI thrashing detected (repeated failed commits).
    - Violation of `AI_SECURITY_THREAT_ACCEPTANCE.md`.
    - Unintended destructive operations in progress.
- **Enforcement**: Once `VETOED` is active, the C# Kernel mechanically blocks all mutation tool calls. 
- **Re-Arming**: Only a human may call `system/unveto` after verifying the project state and clearing the `vibe_audit.jsonl` error markers.

---

## 📸 2. Snapshot Responsibility (Manual Recovery)
Absolute invariance requires a verifiable rollback point.

- **Pre-Flight Rule**: Before initiating an AI-driven automation batch (e.g., full avatar rigging), the human operator **MUST** save the Unity Scene and commit current changes to Git.
- **Atomic Undo**: If the AI fails Layer 3 verification (State Hash Mismatch), the human should use Unity's `Undo (Ctrl+Z)` to return to the last known-good WAL hash before attempting a re-sync.

---

## 🔍 3. Audit Cycles (WAL Verification)
The `vibe_audit.jsonl` is a cryptographically chained ledger that must be reviewed regularly.

- **Frequency**: A human lead should review the Audit Ledger every 24 hours of operation or after every "Critical" intent (e.g., `RIG`, `EXPORT`).
- **Chain Integrity**: If the `entryHash` chain is broken, the system must be considered compromised. The human must rotate credentials and re-bootstrap the project.

---

## 🔑 4. Credential & Token Management
Authentication is the root of trust for the "Iron Box."

- **Rotation**: The `X-Vibe-Token` (Session Nonce) is regenerated on every Unity Domain Reload.
- **Bootstrap Security**: Never commit `metadata/vibe_session.json` to public repositories.
- **Compromise Protocol**: If the port 8085 is exposed to an untrusted network, immediately close Unity and delete the `metadata/` folder to force a complete re-authentication cycle.

---

## 📦 5. Binary Integrity (Git LFS)
Large assets are critical to the "State Hash."

- **LFS Audit**: Periodically run `git lfs ls-files` to ensure all high-scale assets are correctly tracked.
- **Verification**: If an AI fails a `STATE_HASH_MISMATCH` because of a binary change, verify that the LFS pointer is correctly resolved on the local machine before overriding.

---

## 🏁 The Final Boundary
The system is designed to make violations **visible, costly, and undeniable**. If a human chooses to ignore a `STATE_HASH_MISMATCH` or overrides a `DRIFT_BUDGET` block, the responsibility shifts from the **Engine** to the **Governance Layer**.

**Invariance is a process, not just a product.**
