# 🛡️ MCP Unity Kernel: The Canonical Creation Plane

**Philosophy:** Unity is a hostile environment. This Kernel transforms it into a deterministic, irreducible Control Plane.
**Status:** KERNEL v1.2 - PRODUCTION HARDENED.

---

## 🏛️ 1. THE IRREDUCIBLE CORE (VibeBridgeKernel.cs)
*These must be active for the system to be considered "Governed".*

### 🔒 Operational Atoms
- **Single API Layer**: All mutations MUST go through `VibeBridgeKernel.cs`.
- **Atomic Operations**: `transaction_begin` -> `Mutation` -> `transaction_commit`. Revert on fail via `transaction_abort`.
- **The Guard**: Mechanical blocking of mutations during **Compilation**, **Play Mode**, or **Asset Import**.
- **Time-Budgeted IPC**: Requests are processed in 5ms slices to maintain 60+ FPS responsiveness.

### 🛑 Corruption Defense
- **Token-Auth Security**: Every HTTP request MUST include a valid `X-Vibe-Token` matching the session nonce.
- **Read-Before-Write**: You MUST `inspect_object` before any mutation. Hallucination is strictly forbidden.
- **Fail Fast**: Exceptions are never swallowed. They propagate directly to the AI/Human.
- **Batch Editing**: HTTP mutations are wrapped in `AssetDatabase.StartAssetEditing()` for performance.

### 🧹 Infrastructure
- **Airlock IPC**: File-based JSON queue (`vibe_queue/`) for high-reliability mutation survival.
- **Express HTTP**: Port 8085 for low-latency telemetry and screenshots. Port 8086 for dedicated Vision Stream.

---

## 📦 2. THE PAYLOAD LAYER
*Modular "Blades" for specific technical art and rigging domains.*

- **Standard**: `vram_footprint`, `texture_crush`, `swap_to_quest_shaders`.
- **Material**: Atomic control over shader properties, slots, and persistent snapshots.
- **Registry**: Semantic role memory (e.g. `sem:MainBody`) with mesh fingerprint fallback.
- **Auditing**: `audit_avatar`, `physics_audit`, `animation_audit`.
- **Vision**: MJPEG high-speed stream for AI visual verification.

---

## 📜 3. THE HARD LAWS (Mandatory AI Behavior)
1. **Constitutional Adherence**: You are bound by `AI_SECURITY_THREAT_ACCEPTANCE.md`.
2. **Iron Box Protocol**: Transactions are mandatory for all mutations. One AI request = One Undo Group.
3. **Zero Trust**: Always verify object existence and components via `inspect_object`.
4. **Human Sanctuary**: `HUMAN_ONLY/` is invisible and inaccessible.
5. **Gate Audited**: All code is subject to the `security_gate.py` AST scan.

---

## 💀 UNITY PAIN MAP vs. COUNTERMEASURES

| Pain Point | Kernel Countermeasure |
| :--- | :--- |
| **Silent Corruption** | **Truth Loop Warning + Telemetry** |
| **Undo Fragmentation** | **Atomic Undo Groups (Collapsed)** |
| **Compilation Death** | **Kernel Guard State Gating** |
| **Blind AI** | **Select & Frame (`select_object`) + Vision Plane** |
| **Architecture Drift** | **Irreducible Kernel + Modular Payloads** |
| **UI Stutter** | **5ms Main-Thread Time Budgeting** |

---
**Copyright (C) 2026 B-A-M-N**
