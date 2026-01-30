# Contributing to UnityVibeBridge

Thank you for your interest in improving UnityVibeBridge! Because this is a **Governed Creation Kernel**, we maintain strict standards for security, stability, and performance.

---

## 🛡️ The Creation Kernel Standards
All pull requests must adhere to the **Kernel v1.1 Architecture**. We prioritize Editor stability and user flow over feature speed.

### 1. **Zero-Trust Adherence**
Every new tool or modification **MUST** adhere to `AI_ENGINEERING_CONSTRAINTS.md`. 
- No use of `System.Reflection` to bypass the bridge.
- No direct file mutations inside Unity; use the provided Kernel utilities.
- No network or process spawning.

### 2. **Split Architecture Mandate**
- **Kernel (`VibeBridgeKernel.cs`)**: The core engine. Modification is restricted to foundational IPC or security logic.
- **Payloads**: All new creative features (Tech Art, Animation, etc.) MUST be implemented as separate "Payload" files.

### 3. **Structured Response Mandate**
ALL new tool methods must return JSON generated via **Serializable Response Classes** (e.g., `BasicRes`). Manual string concatenation for JSON is strictly forbidden to prevent injection and syntax errors.

### 4. **Performance (The 5ms Rule)**
Every operation must be compatible with the **Time-Budgeted Execution** model. If a tool performs heavy computation, it must be designed to yield or be time-sliced to prevent dropping Unity's framerate.

### 5. **Workflow Privacy**
Never attempt to access or write to the `HUMAN_ONLY/` directory. This is a void mechanically protected from the bridge.

---

## 🛠️ Local Development & Testing
1.  **C# Audit**: Ensure your code passes the `security_gate.py` patterns.
2.  **Telemetry**: Verify that exceptions are correctly propagated back to the AI through the telemetry loop.
3.  **Undo System**: Every mutation MUST be registered with the `Undo` system.

---

## ⚖️ Contributor Grant of Rights
By submitting any contribution (including code, assets, documentation, or feedback), you grant the Author a perpetual, worldwide, non-exclusive, no-charge, royalty-free, irrevocable copyright license to use, reproduce, prepare derivative works of, and sublicense your contributions in any current or future version of the software (including commercial derivatives). 

**ADMINISTRATIVE WAIVER:** You explicitly waive any requirement for the Author to track, notify, or seek further permission for any future use of your contributions. This ensures the project remains viable and under the Author's absolute control.

---
**Copyright (C) 2026 B-A-M-N**
