# Contributing to UnityVibeBridge

Thank you for your interest in improving UnityVibeBridge! This project is built on the principle of **Mechanistic Vibe Coding**. While we maintain strict technical standards, we are committed to building a community where everyone can contribute safely and effectively.

---

## 👋 Welcome to the Vibe
Don't be intimidated by the "Iron Box" or the technical jargon. These safeguards exist so you can experiment, build, and break things **safely**. We prioritize Editor stability and user flow, but we also value creativity and new ideas.

**How you can help:**
- **Payloads**: Create new tools for Technical Art, Animation, or World-building.
- **Documentation**: Help us make our guides even more accessible to beginners.
- **Testing**: Find edge cases and help us harden the Kernel.

---

## 🛡️ The Creation Kernel Standards
All pull requests must adhere to the **Kernel v1.1 Architecture**. 

### 1. **Zero-Trust Adherence**
Every new tool or modification **MUST** adhere to `AI_ENGINEERING_CONSTRAINTS.md`. 
- No use of `System.Reflection` to bypass the bridge or private member access.
- No direct file mutations inside Unity; use the provided Kernel utilities.
- No network or process spawning.
- No modification of `.meta` files or `ProjectSettings` programmatically.

### 2. **Split Architecture Mandate**
- **Kernel (`VibeBridgeKernel.cs`)**: The core engine. Modification is restricted to foundational IPC or security logic.
- **Payloads**: All new creative features (Tech Art, Animation, etc.) MUST be implemented as separate "Payload" files.

### 3. **Structured Response Mandate**
ALL new tool methods must return JSON generated via **Serializable Response Classes** (e.g., `BasicRes`). Manual string concatenation for JSON is strictly forbidden.

### 4. **Performance (The 5ms Rule)**
Every operation must be compatible with the **Time-Budgeted Execution** model. If a tool performs heavy computation, it must be designed to yield or be time-sliced to prevent dropping Unity's framerate.

### 5. **Workflow Privacy**
Never attempt to access or write to the `HUMAN_ONLY/` directory. This is a void mechanically protected from the bridge.

---

## 🏗️ Mutation Guidelines
Any new mutation tool must follow the **Atomic Guard** pattern:

1.  **Call `EnsureSafeMutationContext`**: Always verify that Unity is not compiling, not in play mode, and not in read-only mode at the very start of the operation.
2.  **Verify Provenance**: If a tool deletes or moves objects, it must check for the `VibeBridgeAgentTag` component.
3.  **Support Undo**: Every write operation MUST use `Undo.RecordObject`, `Undo.AddComponent`, or `Undo.DestroyObjectImmediate`.
4.  **No Silent Failures**: Propagate all exceptions as `ErrorResponse` with `HardStop` severity.

---

## 🛠️ Local Development & Testing
1.  **C# Audit**: Ensure your code passes the `scripts/security_gate.py` patterns.
2.  **Telemetry**: Verify that exceptions are correctly propagated back to the AI through the telemetry loop.
3.  **Undo System**: Every mutation MUST be registered with the `Undo` system.
4.  **Capability Discovery**: If you add a new type of manipulation, you MUST update `GetObjectCapabilities` to reflect the risks and requirements.

---

## ⚖️ Contributor Grant of Rights
By submitting any contribution (including code, assets, documentation, or feedback), you grant the Author a perpetual, worldwide, non-exclusive, no-charge, royalty-free, irrevocable copyright license to use, reproduce, prepare derivative works of, and sublicense your contributions in any current or future version of the software (including commercial derivatives). 

**ADMINISTRATIVE WAIVER:** You explicitly waive any requirement for the Author to track, notify, or seek further permission for any future use of your contributions. This ensures the project remains viable and under the Author's absolute control.

---
**Copyright (C) 2026 B-A-M-N**
