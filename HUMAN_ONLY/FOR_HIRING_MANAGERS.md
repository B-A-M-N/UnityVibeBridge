# 👔 For Hiring Managers: Engineering Audit & Systems Design

If you are evaluating this repository for a technical role, this document provides a high-level audit of the architectural decisions, security invariants, and systems engineering principles implemented in **UnityVibeBridge**.

---

## 🏛️ 1. Control-Plane vs. Execution-Plane Separation
The core innovation of this project is the strict decoupling of **AI Intent** from **Unity Execution**.
*   **The Problem**: LLMs are non-deterministic and prone to "hallucination drift." Allowing them to generate and execute raw C# code inside the Unity Editor is a critical stability and security risk.
*   **The Solution**: We implemented a **Mechanistic ISA (Instruction Set Architecture)**. The AI issues high-level JSON "Intents" (e.g., `world/spawn`, `material/set-value`). These are validated by a Python middleware and then dispatched to a C# Kernel that executes them using pre-defined, safe primitives.
*   **Engineering Impact**: This ensures that even if the AI "hallucinates," it can only express actions within the bounds of the provided toolset.

## 🛡️ 2. Adversarial Security & "Iron Box" Isolation
Unlike standard script-bridges, UnityVibeBridge treats the AI as an **untrusted operator**.
*   **Recursive AST Auditing**: Before any payload is accepted, the Python middleware performs a recursive Abstract Syntax Tree (AST) scan. It blocks high-risk namespaces (`System.Reflection`, `System.Diagnostics`) and patterns (obfuscated strings, dynamic attribute access) that could be used to bypass the bridge.
*   **Token-Based Auth**: The communication between the MCP server and Unity is authenticated via a session-specific token (`X-Vibe-Token`).
*   **Provenance Tracking**: Every object created by the AI is tagged with a session-nonce. The Kernel implements a **Actuator Lock** that prevents the AI from deleting or modifying any object it did not personally create, preventing "Hallucinated Purges" of critical project assets.

## ⚛️ 3. Transactional State Mutation (Atomic Undo)
Unity's Editor state is notoriously fragile during heavy automation.
*   **Mechanism**: Every AI request is wrapped in a single, atomic `Undo` group. 
*   **Failure Protocol**: If a mutation throws a C# exception mid-execution, the Kernel catches it, generates a structured `ErrorResponse`, and triggers an immediate `transaction_abort` (Undo).
*   **Outcome**: The project remains in a valid state. One AI Intent = One Clean Undo Step. This prevents "scar tissue" in the Scene hierarchy.

## 🏃 4. Performance Budgeting & Main-Thread Integrity
AI agents often generate massive batches of commands that can freeze the Unity UI.
*   **The 5ms Frame Slice**: The Kernel implements a time-budgeted execution loop. Request processing is throttled to **5ms per frame**, ensuring the Unity Editor maintains 60+ FPS even during heavy automation or texture crushing.
*   **Non-Blocking Telemetry**: The "Truth Loop" (telemetry streaming) runs on a separate dispatcher to avoid blocking the creation primitives.

## 🧠 5. Epistemic Governance (Truth Reconciliation)
To solve the "Blind AI" problem without expensive vision-compute overhead:
*   **Semantic Pathing**: We implemented a **Vibe Registry** that maps functional roles (e.g., `sem:MainBody`) to InstanceIDs. This allows the AI to maintain a persistent mental model of the scene that survives domain reloads and hierarchy changes.
*   **Telemetry Feedback**: Tools like `get_telemetry_errors` force the AI to verify its work against actual Unity console output, creating a closed-loop system for self-correction.

---

## 🛠️ Tech Stack Summary
- **Languages**: C# (Unity Editor Scripting), Python (MCP Server/Middleware).
- **Security**: AST Parsing, SHA-256 Fingerprinting, Token-Auth.
- **Architecture**: Modular "Payload" system using Partial Classes for horizontal scalability.
- **Stability**: Time-Budgeted IPC, Atomic Transaction Wrapping.

**Conclusion**: This repository demonstrates more than just "AI integration"—it demonstrates **Governance Engineering**. It provides a blueprint for how to deploy powerful, non-deterministic models into high-stakes, stateful environments safely and efficiently.
