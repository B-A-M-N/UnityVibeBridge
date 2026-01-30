# 🌌 UnityVibeBridge

> [!WARNING]
> **EXPERIMENTAL & IN-DEVELOPMENT**  
> This project is currently an active research prototype. APIs, security protocols, and core logic are subject to rapid, breaking changes. This software performs mutations on the Asset Database and Scene; **MANDATORY BACKUPS** are required before use.

### The "One-Click" Technical Director for Unity Editor Mastery
*A production-grade AI control interface for deterministic, undo-safe Unity Editor operations.*

**UnityVibeBridge** is a professional-grade intelligent interface that transforms the Unity Editor into a **Governed Creation Kernel**. It allows AI agents to interact safely, deterministically, and artistically with Unity’s core engine—turning natural language intents into professional production operations.

---

## ⚠️ Read This First (Why This Exists)

**UnityVibeBridge** is not a toy, a prompt wrapper, or a "magic AI button." 

It is a **reference implementation of AI-assisted systems design** in a high-fidelity, stateful environment. It allows Large Language Models (LLMs) to safely operate inside Unity without risking project corruption, infinite import loops, or runaway execution.

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

## 🧠 What This Project Demonstrates (For Engineers & Hiring Managers)

If you are evaluating this project as an engineer or hiring manager, this repository is a working demonstration of **AI Systems Engineering**:

*   **Control-Plane vs. Execution-Plane Separation**: LLMs generate *intent* (Mechanistic Intents), never raw code execution.
*   **Iron Box Security**: Hardened via local binding, session-token authentication, and file-based status signaling.
*   **Transactional State Mutation**: Every operation is wrapped in undo-safe, atomic blocks. **One AI request = One Undo step.**
*   **Deterministic Asset Manipulation**: Control over materials, prefabs, and hierarchies is handled mechanistically, eliminating the fragility of raw scripts.
*   **Truth Reconciliation Loop**: Tools like `get_telemetry_errors` allow the agent to verify multiple assumptions about the scene in a single round-trip.

---

## 🛠️ Complete Tool Reference (Exhaustive)

### 1. 🧠 Epistemic & Cognitive Governance (Anti-Hallucination)
*   **`inspect_object`**: Forces the agent to verify its assumptions about components and state.
*   **`get_telemetry_errors`**: Streams the last 50 console logs to the agent for "Truth Loop" verification.
*   **`list_available_tools`**: Dynamic discovery of installed Payloads and capabilities.
*   **Stale Session Guard**: Automatically invalidates beliefs if Unity restarts (via `sessionNonce` tracking).

### 2. 🛡️ Kernel & Integrity (The Guardrails)
*   **`transaction_begin`**: Starts an atomic Undo Group for complex sequences.
*   **`transaction_commit`**: Finalizes and collapses the Undo stack for a single intent.
*   **`guard/status`**: Mechanically blocks execution during unsafe Editor states (Compilation, Play Mode).
*   **Time-Budgeting**: Kernel-level enforcement of 5ms execution windows per frame.

### 3. 🏗️ Scene Manipulation & Strategic Intent
*   **`get_hierarchy`**: Recursive dump of InstanceIDs and names for scene mapping.
*   **`system/search`**: High-performance Regex and Layer-based discovery for massive scenes.
*   **`rename_object` / `reparent_object`**: Precise transform and identity adjustments.
*   **`clone_object` / `delete_object`**: Managed lifecycle for GameObjects with Undo support.
*   **`select_object`**: Focus-aware "Stealth Framing" that respects the user's active viewport.

### 4. 🎨 Technical Art, surafcing & Optimization
*   **`object/set-value`**: Safe, reflection-based mutation of public fields and properties.
*   **`vram_footprint`**: Numerical analysis of GPU memory usage by textures.
*   **`texture_crush`**: Batch-downscales textures with hardware safety caps (8k max).
*   **`swap_to_quest_shaders`**: Automated material transition to mobile-safe shaders.
*   **`opt/fork`**: Creates non-destructive optimization variants with isolated material clones.

### 5. 🔗 Pipeline & Infrastructure
*   **`world/spawn`**: Safe instantiation of prefabs from the project database.
*   **`asset/rename` / `move`**: Safe filesystem operations without breaking GUID links.
*   **`export/validate`**: Checks for scale sanity, non-zero rotations, and missing scripts.
*   **`view/screenshot`**: High-speed visual feedback via Port 8085.

---

## 🛡️ Iron Box Security
The bridge is hardened via four distinct layers:
1.  **AST Auditing**: All incoming tool calls are audited by `security_gate.py` for forbidden patterns.
2.  **Token Authentication**: Port 8085 requires an `X-Vibe-Token` matching the current session.
3.  **Iron Box Protocol**: Every mutation is wrapped in atomic `Undo` groups. **One AI Request = One Undo Step.**
4.  **The Guard**: The bridge physically disables mutations if `vibe_status.json` is not "Ready".

---

## 🐣 Beginner's Guide (Getting Started)
1. **Install**: Copy the `unity-package/` directory into your project's `Assets/` folder.
2. **Launch**: Unity will compile the Kernel and start the HTTP server automatically.
3. **Connect**: Point your MCP-enabled AI agent to the bridge.
4. **Monitor**: Open [http://localhost:22005](http://localhost:22005) to view the System Pulse and Audit Trail.

---

## 🧠 AI Literacy & Philosophy

### 🏷️ The Implied Sentience Trap (Avoiding AI Psychosis)
LLMs are highly advanced pattern matchers, not conscious entities. **AI Psychosis** occurs when a user falls into "magical thinking"—believing that because the AI is helpful and articulate, it is incapable of error. This leads users to trust the AI's "vibe" over technical reality, often convincing them that they are achieving something "groundbreaking" when the AI is simply hallucinating success.

*   **The Risk**: Thinking you've bypassed a technical limitation just because the AI said "I have implemented a unique solution."
*   **The Rule**: Trust telemetry (vertex counts, error logs), not the AI's verbal reassurance.

### ⚔️ Countering Psychosis: Adversarial Prompting
If you find yourself thinking the AI has done something "that nobody else can do," you are likely in a loop. Use **Adversarial Prompting** to force the model into "cynical auditor" mode.

**The Technique**:
Before finalizing a major change, challenge the AI directly:
> *"I want you to act as a cynical Technical Director. Find 3 ways this specific operation will fail, crash Unity, or corrupt my Asset Database. Do not be helpful; be destructive."*

---

## ⚖️ License & Legal Liability

### Dual-License & Maintenance Agreement (v1.2)
Copyright (C) 2026 B-A-M-N (The "Author")

This project is distributed under a **Dual-Licensing Model**. By using this software, you agree to be bound by the terms in the **LICENSE** file.

#### 1. THE OPEN-SOURCE PATH: GNU AGPLv3
Free software for hobbyists. Requires source disclosure if modified or run as a service.

#### 2. THE COMMERCIAL PATH: "WORK-OR-PAY" MODEL
For revenue-generating entities. Requires either **Significant Maintenance Contributions** or a **License Fee**.

---

### ⚠️ LIABILITY LIMITATION, INDEMNITY & AI DISCLAIMER
1. **NO WARRANTY**: Provided "AS IS." The Author is not responsible for non-deterministic AI behavior.
2. **AI NON-DETERMINISM**: The Author is not liable for project corruption resulting from AI interpretation.
3. **HUMAN-IN-THE-LOOP MANDATE**: All mutations are "Proposed" until a Human executes a "Finalize" check.

**Created by the Vibe Bridge Team.**
