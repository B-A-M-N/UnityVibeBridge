# 🛡️ MCP Unity Control Plane: The Kernel Operating System

**Philosophy:** Unity is a non-deterministic environment. The MCP must act as a **Kernel Operating System**, managing state and execution context to prevent project corruption.

---

## 🏛️ The Kernel Plane (Infrastructure)
**Objective:** Prevent Context Desync, Compilation Poisoning, and UI Fragmentation.

### 🔒 Kernel Guard
- **Mechanism:** Mechanical gating in `VibeBridgeKernel.cs`.
- **Invariant:** Rejects all mutations if Unity is not in a "Ready" state.
- **Performance:** Processes requests in 5ms slices to maintain 60+ FPS stability.

### ⚛️ Atomic Mutator
- **Mechanism:** `Undo.IncrementCurrentGroup` + `Undo.CollapseUndoOperations`.
- **Invariant:** One AI Intent = One Atomic Undo Step.
- **Batching:** Uses `AssetDatabase.StartAssetEditing()` to prevent re-import churn.

### 🔐 Iron Box Security
- **Auth:** Mandatory `X-Vibe-Token` validation for all HTTP creation primitives.
- **Audit:** Every modification is validated against the `security_gate.py` AST patterns.

---

## 🧱 The Intelligence Plane (Payloads)
**Objective:** Transform raw data into creation wisdom.

### 🧪 Modular Audit Suite
- **Avatar/Physics/Animation**: Deep hierarchical scans for performance and stability.
- **Semantic Registry**: Role-based pathing (`sem:Target`) to survive InstanceID volatility.

### 🎨 Tech-Art Automation
- **Express Bakes**: Shader swapping and texture crushing via modular payloads.
- **Material Fidelity**: Persistent state snapshots and granular property control.

---

## 🛰️ The Communication Plane (Planes)
- **Port 8085**: Control Plane (Authenticated JSON Commands).
- **Port 8086**: Vision Plane (MJPEG Real-time Stream).
- **Airlock**: File-based safety layer for high-reliability mutation survival.

---

## 📜 Mandatory AI Directives
1. **Adhere to the Iron Box**: Never mutate without a transaction.
2. **Prove your Targets**: Never act on an object you haven't `inspected` in the current session.
3. **Respect the Flow**: Use **Stealth Framing** to avoid hijacking the human camera.
4. **Obey the Gate**: Any code attempting to bypass the Kernel will be blocked by `security_gate.py`.

---
**Copyright (C) 2026 B-A-M-N**
