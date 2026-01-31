# 👁️ UnityVibeBridge: Vision, Philosophy & Technical Audit

This document contains the deep architectural goals, engineering audits, and security philosophies that drive the UnityVibeBridge project.

---

## ⚠️ Why This Exists (The Problem Space)

**UnityVibeBridge** is a **reference implementation of AI-assisted systems design** in a high-fidelity, stateful environment. It allows Large Language Models (LLMs) to safely operate inside Unity without risking project corruption, infinite import loops, or runaway execution.

This project answers a critical engineering question: 
> *How do you let an AI act inside a complex, stateful application—without trusting it?*

**The answer is: You don’t. You constrain it.**

| **Capability** | **Feature** |
| :--- | :--- |
| 🛡️ **Iron Box** | Zero-trust security via the **Kernel Guard**, Token-Auth, and AST-validated IPC. |
| ⚛️ **Kernel Integrity** | Real-time invariant enforcement (No mutations during domain reloads, atomic transactions). |
| 🏃 **Stable Lifecycle** | Time-budgeted main-loop dispatching (5ms slices) ensuring a smooth 60+ FPS. |
| 🧠 **Epistemic Control** | Truth-reconciliation tools (`telemetry`, `vibe_status`) that prevent AI hallucinations. |

---

## 🧠 What This Project Demonstrates (Technical Audit)

If you are evaluating this project as an engineer or hiring manager, this repository is a working demonstration of **AI Systems Engineering**:

*   **Control-Plane vs. Execution-Plane Separation**: LLMs generate *intent* (Mechanistic Intents), never raw code execution.
*   **Adversarial Security**: Hardened via local binding, session-token authentication, and **Recursive AST Auditing** of Python payloads.
*   **Transactional State Mutation**: Every operation is wrapped in undo-safe, atomic blocks. **One AI request = One Undo step.**
*   **Performance Budgeting**: Implements **5ms Main-Thread Time Budgeting** to ensure the Unity Editor maintains 60+ FPS even during heavy AI automation.
*   **Truth Reconciliation Loop**: Tools like `get_telemetry_errors` force the agent to verify reality against intent in a closed feedback loop.

---

## 🛡️ Iron Box Security Detail
The bridge is hardened via four distinct layers:
1.  **AST Auditing**: All incoming tool calls are audited by `scripts/security_gate.py` for forbidden patterns.
2.  **Token Authentication**: Port 8085 requires an `X-Vibe-Token` matching the current session.
3.  **Iron Box Protocol**: Every mutation is wrapped in atomic `Undo` groups. **One AI Request = One Undo Step.**
4.  **The Guard**: The bridge physically disables mutations if `vibe_status.json` is not "Ready".

---
**Copyright (C) 2026 B-A-M-N**
