# 🛡️ MCP Unity Control Plane: The Kernel Operating System

**Philosophy:** Unity is a non-deterministic environment. The MCP must act as a **Kernel Operating System**, managing state and execution context to prevent project corruption.

---

## 🏛️ The Kernel Plane (Infrastructure)
**Objective:** Prevent Context Desync and Compilation Poisoning.

### 🔒 Kernel Guard
- **Mechanism:** Mechanical gating in `VibeBridgeKernel.cs`.
- **Invariant:** Rejects all mutations if Unity is not in a "Ready" state.
- **Defense:** Prevents the script-reload corruption loops.

### ⚛️ Atomic Mutator
- **Mechanism:** `Undo.IncrementCurrentGroup` + `Undo.CollapseUndoOperations`.
- **Invariant:** One AI Intent = One Atomic Undo Step.
- **Defense:** Ensures project can always revert to a clean state.

---

## 🧱 The Intelligence Plane (Payloads)
**Objective:** Transform raw data into creation wisdom.

### 🧪 Standard Audit Suite
- **Avatar Audit**: Single-call mesh/material totals.
- **Physics Audit**: Identifies unstable or high-cost Rigidbodies.
- **Animation Audit**: Detects missing clips and null states.

### 🎨 Tech-Art Automation
- **Express Bakes**: Shader swapping and texture crushing via modular payloads.
- **VRAM Verification**: Proactive detection of PC/Quest performance killers.

---

## 🛰️ The Communication Plane (Planes)
- **Port 8085**: Control Plane (Commands & JSON).
- **Port 8086**: Vision Plane (MJPEG Stream).
- **Airlock**: File-based safety layer for high-risk operations.

---

## 📜 Mandatory AI Directives
1. **Adhere to the Iron Box**: Never mutate without a transaction.
2. **Prove your Targets**: Never act on an object you haven't `inspected` in the current session.
3. **Respect the Sanctuary**: `HUMAN_ONLY/` is a void. It does not exist in your memory.
4. **Obey the Gate**: Any code attempting to bypass the Kernel will be blocked by `security_gate.py`.