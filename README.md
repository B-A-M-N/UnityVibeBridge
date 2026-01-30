# 🌌 UnityVibeBridge

> [!WARNING]
> **EXPERIMENTAL & IN-DEVELOPMENT**  
> This project is currently an active research prototype. APIs, security protocols, and core logic are subject to rapid, breaking changes. This software performs mutations on the Asset Database and Scene; **MANDATORY BACKUPS** are required before use.

### The "One-Click" Technical Director for Unity
*A production-grade AI control interface for deterministic, undo-safe Unity Editor operations.*

**UnityVibeBridge** is a professional-grade intelligent interface that transforms the Unity Editor into a **Governed Creation Kernel**. It allows AI agents to interact safely, deterministically, and artistically with Unity’s core engine—turning natural language intents into professional production operations.

---

## ⚠️ Read This First (Why This Exists)

**UnityVibeBridge** is not a toy, a prompt wrapper, or a "magic AI button." 

It is a **reference implementation of AI-assisted systems design** in a high-fidelity, stateful environment. It allows Large Language Models (LLMs) to safely operate inside Unity without risking project corruption, infinite import loops, or domain reload hell.

This project answers a critical engineering question: 
> *How do you let an AI act inside a complex, stateful application—without trusting it?*

**The answer is: You don’t. You constrain it.**

| **Capability** | **Feature** |
| :--- | :--- |
| 🛡️ **Iron Box** | Zero-trust security via `GuardModule` (blocks compilation/playmode mutations) and `ForensicModule` (audit logs). |
| ⚛️ **Kernel Integrity** | Real-time invariant enforcement (No mutations during domain reloads, atomic transactions). |
| 🏃 **Stable Lifecycle** | Thread-safe main-loop dispatching via `EditorApplication.update` and explicit Lifecycle signals. |
| 🧠 **Epistemic Control** | Truth-reconciliation tools (`TelemetryModule`, `HeartbeatModule`) that prevent AI hallucinations about editor state. |

---

## 🧠 What This Project Demonstrates (For Engineers & Hiring Managers)

If you are evaluating this project as an engineer or hiring manager, this repository is a working demonstration of **AI Systems Engineering**:

*   **Control-Plane vs. Execution-Plane Separation**: LLMs generate *intent* (Mechanistic Intents), never raw code execution.
*   **Iron Box Security**: Hardened via local binding, static AST auditing, and file-based heartbeats.
*   **Transactional State Mutation**: Every operation is wrapped in undo-safe, atomic blocks. **One AI request = One Undo Group.**
*   **Deterministic Asset Manipulation**: Control over materials, prefabs, and hierarchies is handled mechanistically, eliminating the fragility of raw scripts.
*   **Truth Reconciliation Loop**: Tools like `telemetry/get-errors` and `health/check` allow the agent to verify multiple assumptions about the scene in a single round-trip.

---

## 🛠️ Complete Tool Reference (Exhaustive)

### 1. 🧠 Epistemic & Cognitive Governance (Anti-Hallucination)
*   **`telemetry/get_errors`**: Forces the agent to see the actual Console errors it generated.
*   **`health/check`**: Verifies if Unity is "Ready", "Compiling", or "Playing" before attempting mutations.
*   **`audit/log_event`**: Creates an immutable record of AI intent vs. execution.
*   **Lifecycle Signals**: `metadata/vibe_status.json` automatically signals "Reloading" to prevent bridge death.

### 2. 🛡️ Kernel & Integrity (The Guardrails)
*   **`guard/status`**: Checks for unsafe states (Play Mode, Compilation).
*   **`guard/await_compilation`**: Blocks execution until the Asset Database is quiescent.
*   **`transaction/begin`**: Starts an atomic Undo Group.
*   **`transaction/commit`**: Finalizes the Undo Group.
*   **`transaction/abort`**: Reverts the entire group if a step fails.

### 3. 🏗️ Scene Manipulation & Strategic Intent
*   **`hierarchy`**: Dumps the scene graph for AI analysis.
*   **`inspect`**: Returns component data for a specific GameObject.
*   **`object/active`**: Toggles GameObject state.
*   **`object/rename`**: Safely renames objects.
*   **`world/spawn`**: Instantiates prefabs with Undo support.

### 4. 🎨 Technical Art & Surfacing
*   **`material/list`**: Enumerates materials on a renderer.
*   **`material/set_color`**: Adjusts standard shader properties atomically.
*   **`material/set_slot_texture`**: Swaps textures with GUID validation.
*   **`material/snapshot`**: Backs up material assignments before destructive edits.

### 5. 🔗 Pipeline & Infrastructure
*   **`snapshot/create`**: Creates a project-level metadata checkpoint.
*   **`snapshot/restore`**: Restores `Registry` and `Session` data.
*   **`opt/texture/crush`**: Batch-processes texture max sizes (optimization).
*   **`opt/fork`**: Creates a safe duplicate of an avatar for destructive optimization.

---

## 🛡️ Iron Box Security
The bridge is hardened via three distinct layers:
1.  **Guard Module**: Blocks mutations during unsafe Editor states (Compilation, Play Mode).
2.  **Forensic Module**: Logs every capability usage to `logs/vibe_audit.jsonl`.
3.  **Lifecycle Manager**: Explicitly handles Domain Reloads to prevent "zombie" processes.

---

## 🐣 Beginner's Guide (Getting Started)
1. **Install**: 
   - Copy `unity-package/` to your project's `Assets/` folder.
   - Wait for Unity to compile `VibeBridgeServer_v16.cs`.
2. **Start Server**:
   - The server starts automatically via `[InitializeOnLoad]`.
   - Check `metadata/vibe_health.json` to confirm it is "Ready".
3. **Build**: Use natural language to orchestrate your production pipeline.

---

## 🧠 AI Literacy & Philosophy (Important)

### 🏷️ The Implied Sentience Trap (Combating AI Psychosis)
It is easy to fall into "magical thinking" when an AI responds with warmth. This project deliberately demystifies the LLM. Asking an AI *"What do you think?"* does not demonstrate consciousness; the AI is simply reflecting your intent to treat it as a thinking being.

### ⚔️ Combatting Overconfidence: Adversarial Prompting
If you feel you are doing something "groundbreaking," use **Adversarial Prompting**:
Ask the AI: *"I think this logic is perfect. Now, act as a cynical auditor. Find 3 ways this could fail, crash Unity, or corrupt my Asset Database."*
Force the AI to argue *against* your ideas to stay grounded in reality.

---

## 🧑‍💻 About the Author

I specialize in **Local LLM applications and secure AI-Human interfaces**. This system was built end-to-end to empower human craftsmanship and creativity. UnityVibeBridge was born from a desire for creative freedom—building the tools I didn't know how to use manually. It is a gift to the community to level the playing field.

---

## ⚖️ License & Legal Liability

### Dual-License & Maintenance Agreement (v1.1)

Copyright (C) 2026 B-A-M-N (The "Author")

This project is distributed under a **Dual-Licensing Model**. By using this software, you agree to be bound by one of the two licensing paths described below.

#### 1. THE OPEN-SOURCE PATH: GNU AGPLv3
For non-commercial use, hobbyists, and open-source contributors.
This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License (AGPLv3) as published by the Free Software Foundation. 

#### 2. THE COMMERCIAL PATH: "WORK-OR-PAY" MODEL
For entities generating revenue, commercial studios, or those who wish to keep their modifications private. Pursuant to Section 7 of the GNU AGPLv3, commercial use requires either **Significant Maintenance Contributions** or payment of a **Commercial License Fee**.

---

### ⚠️ LIABILITY LIMITATION, INDEMNITY & AI DISCLAIMER

1. **NO WARRANTY**: This software is provided "AS IS." The Author makes no representations concerning the safety, stability, or non-destructive nature of AI-interpreted operations.
2. **AI NON-DETERMINISM**: This software translates natural language via LLMs into Unity mutations. AI is non-deterministic; the Author is not liable for "hallucination drift," unintended asset deletion, or scene corruption resulting from AI interpretation.
3. **HUMAN-IN-THE-LOOP MANDATE**: All AI-suggested mutations are considered "Proposed" until a Human User executes a "Finalize" or "Undo" check. THE USER ACCEPTS FULL LEGAL AND TECHNICAL RESPONSIBILITY FOR ANY CODE OR MUTATION THEY ALLOW THE AI TO EXECUTE.
4. **INDEMNIFICATION**: You agree to indemnify, defend, and hold harmless the Author from and against any and all claims, liabilities, damages, and expenses (including attorney fees) arising from your use of the software, your breach of this license, or any assets produced using this software.
5. **PLATFORM COMPLIANCE**: The Author is NOT responsible for any violations of Third-Party Terms of Service (e.g., VRChat TOS, Unity EULA). Use of this tool is at the User's sole risk.
6. **LIMITATION OF LIABILITY**: TO THE MAXIMUM EXTENT PERMITTED BY LAW, IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, OR CONSEQUENTIAL DAMAGES (INCLUDING LOSS OF DATA, PROFITS, OR "VIBE") ARISING OUT OF THE USE OR INABILITY TO USE THIS SOFTWARE.

**Created by the Vibe Bridge Team.**