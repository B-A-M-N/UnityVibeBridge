# 🌌 UnityVibeBridge: The Creation Kernel

> [!WARNING]
> **EXPERIMENTAL & IN-DEVELOPMENT**  
> This project is currently an active research prototype. APIs, security protocols, and core logic are subject to rapid, breaking changes. This software performs mutations on the Asset Database and Scene; **MANDATORY BACKUPS** are required before use.

### The "One-Click" Technical Director for Unity
*A production-grade, irreducible AI control interface for deterministic Unity Editor operations.*

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
| 🛡️ **Iron Box** | Zero-trust security via the **Kernel Guard** (blocks compilation/playmode mutations). |
| ⚛️ **Kernel Integrity** | Real-time invariant enforcement (No mutations during domain reloads, atomic transactions). |
| 🏃 **Stable Lifecycle** | Thread-safe main-loop dispatching via `EditorApplication.update` and the **Airlock IPC**. |
| 🧠 **Epistemic Control** | Truth-reconciliation tools (`telemetry`, `vibe_status`) that prevent AI hallucinations. |

---

## 🏛️ Architecture: The Motor and the Bits

The system is split into two distinct layers to ensure absolute project safety:

1.  **The Kernel (`VibeBridgeKernel.cs`)**: The irreducible engine. It handles IPC, Safety-Gating (Guard), and Atomic Mutations (Undo). It is designed to be permanent and universal.
2.  **The Payloads**: Modular "Blades" for the motor.
    *   `VibeBridge_StandardPayload.cs`: Core tech-art and optimization tools.
    *   `VibeBridge_ExtrasPayload.cs`: Visual debugging and animator tools.
    *   `VibeBridge_VisionPayload.cs`: High-speed MJPEG stream for AI "Eyes".

---

## 🛠️ Complete Tool Reference (Exhaustive)

### 1. 🏛️ Kernel Primitives (Irreducible MVC)
*   **`get_hierarchy`**: Dumps the scene graph (InstanceIDs & Names).
*   **`inspect_object`**: Returns detailed component data and state.
*   **`select_object`**: Physically frames a target in Unity (Visual Feedback).
*   **`rename_object` / `reparent_object`**: Standard transform and name mutations.
*   **`clone_object` / `delete_object`**: Manages the full lifecycle of GameObjects.
*   **`batch_*`**: Bulk operations for high-velocity creation.
*   **`transaction_begin` / `commit` / `abort`**: Atomic state protection.
*   **`get_telemetry_errors`**: Streams Console logs to the AI.
*   **`list_available_tools`**: Dynamic discovery of all installed Payloads.

### 2. 📦 Standard Payload (Professional Utilities)
*   **`audit_avatar`**: A "Mega-Tool" returning mesh, vertex, and material reports.
*   **`run_physics_audit`**: Finds physics instabilities (Rigidbodies/Colliders).
*   **`vram_footprint`**: Estimates GPU texture memory usage in MB.
*   **`texture_crush`**: Batch-downscales textures for mobile optimization.
*   **`opt/fork`**: Creates a non-destructive optimization variant with cloned materials.
*   **`swap_to_quest_shaders`**: Automated material transition to mobile shaders.
*   **`spawn_prefab`**: Instantiates assets directly from the project folder.
*   **`visual/point` / `line`**: Spawns temporary debug markers in the scene.
*   **`animator/set-param`**: Manipulates Animator parameters for VRChat/System testing.

---

## 📘 User Guides & Philosophy
*   **[AI Philosophy & Safety](AI_PHILOSOPHY.md)**: Learn how to manage AI behavior, prevent hallucinations, and use Adversarial Prompting.
*   **[Engineering Constraints](AI_ENGINEERING_CONSTRAINTS.md)**: The strict technical rules governing all code generation.
*   **[Contributing](CONTRIBUTING.md)**: Guidelines for extending the Kernel or adding new Payloads.

---

## ⚖️ License & Legal Liability

### Dual-License & Maintenance Agreement (v1.1)

Copyright (C) 2026 B-A-M-N (The "Author")

This project is distributed under a **Dual-Licensing Model**. By using this software, you agree to be bound by one of the two licensing paths described below.

#### 1. THE OPEN-SOURCE PATH: GNU AGPLv3
For non-commercial use, hobbyists, and open-source contributors.
This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License (AGPLv3) as published by the Free Software Foundation. 

#### 2. THE COMMERCIAL PATH: "WORK-OR-PAY" MODEL
For entities generating revenue, commercial studios, or those who wish to keep their modifications private. Pursuant to Section 7 of the GNU AGPLv3, commercial use requires either **Significant Maintenance Contributions**, payment of a **Commercial License Fee**, or through express waiver by the Author, as the Author reserves the right to waive any costs or fees for whatever reason.

---

### 3. CONTRIBUTOR GRANT OF RIGHTS
By submitting any contribution, you grant the Author a perpetual, irrevocable license to use and sublicense your work in any version of the software. The Author reserves the right to use and re-license contributions without having to track down or seek further permission from the original contributor.

---

### ⚠️ LIABILITY LIMITATION, INDEMNITY & AI DISCLAIMER

1. **NO WARRANTY**: This software is provided "AS IS." The Author makes no representations concerning the safety, stability, or non-destructive nature of AI-interpreted operations.
2. **AI NON-DETERMINISM**: This software translates natural language via LLMs into Unity mutations. AI is non-deterministic; the Author is not liable for "hallucination drift," unintended asset deletion, or scene corruption resulting from AI interpretation.
3. **HUMAN-IN-THE-LOOP MANDATE**: All AI-suggested mutations are considered "Proposed" until a Human User executes a "Finalize" or "Undo" check. THE USER ACCEPTS FULL LEGAL AND TECHNICAL RESPONSIBILITY FOR ANY CODE OR MUTATION THEY ALLOW THE AI TO EXECUTE.
4. **INDEMNIFICATION**: You agree to indemnify, defend, and hold harmless the Author from and against any and all claims, liabilities, damages, and expenses (including attorney fees) arising from your use of the software, your breach of this license, or any assets produced using this software.
5. **PLATFORM COMPLIANCE**: The Author is NOT responsible for any violations of Third-Party Terms of Service (e.g., VRChat TOS, Unity EULA). Use of this tool is at the User's sole risk.
6. **LIMITATION OF LIABILITY**: TO THE MAXIMUM EXTENT PERMITTED BY LAW, IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, OR CONSEQUENTIAL DAMAGES (INCLUDING LOSS OF DATA, PROFITS, OR "VIBE") ARISING OUT OF THE USE OR INABILITY TO USE THIS SOFTWARE.

**Created by the Vibe Bridge Team.**