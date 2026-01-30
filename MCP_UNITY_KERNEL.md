# 🛡️ MCP Unity Kernel: The Canonical Creation Plane

**Philosophy:** Unity is a hostile environment. This Kernel transforms it into a deterministic, irreducible Control Plane.
**Status:** KERNEL v1.1 - IRREDUCIBLE CORE.

---

## 🏛️ 1. THE IRREDUCIBLE CORE (VibeBridgeKernel.cs)
*These must be active for the system to be considered "Governed".*

### 🔒 Operational Atoms
- **Single API Layer**: All mutations MUST go through `VibeBridgeKernel.cs`.
- **Atomic Operations**: `transaction_begin` -> `Mutation` -> `transaction_commit`. Revert on fail via `transaction_abort`.
- **The Guard**: Mechanical blocking of mutations during **Compilation**, **Play Mode**, or **Asset Import**.

### 🛑 Corruption Defense
- **Read-Before-Write**: You MUST `inspect_object` before any mutation. Hallucination is strictly forbidden.
- **Fail Fast**: Exceptions are never swallowed. They propagate directly to the AI/Human.
- **Telemetry Hook**: Structured access to the last 50 Console Errors via `get_telemetry_errors`.

### 🧹 Infrastructure
- **Airlock IPC**: File-based JSON queue (`vibe_queue/`) for high-reliability mutation survival.
- **Express HTTP**: Port 8085 for telemetry and high-speed screenshots. Port 8086 for dedicated Vision Stream.

---

## 📦 2. THE PAYLOAD LAYER (VibeBridge_StandardPayload.cs)
*High-level "Blades" for the Kernel.*

- **Technical Art**: `vram_footprint`, `texture_crush`, `swap_to_quest_shaders`.
- **Intelligence**: `audit_avatar`, `run_physics_audit`, `run_animation_audit`.
- **World Building**: `spawn_prefab`, `set_static_flags`.

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
| **Undo Fragmentation** | **Atomic Undo Groups** |
| **Compilation Death** | **Kernel Guard State Gating** |
| **Blind AI** | **Select & Frame (`select_object`)** |
| **Architecture Drift** | **Irreducible Kernel + Modular Payloads** |