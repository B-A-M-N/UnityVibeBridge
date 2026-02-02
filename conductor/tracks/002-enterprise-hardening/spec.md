# Specification: Enterprise Hardening (C# & API)

## 1. Objective
Enable professional-grade stability for the `UnityVibeBridge` by implementing pre-execution validation and API intelligence layers. This minimizes the "20-second compile cycle" for failed code and prevents AI hallucinations.

## 2. Components
1.  **C# Syntax Validator (Python-side)**: A lightweight C# parser integrated into `security_gate.py` to catch syntax errors (missing semicolons, mismatched braces) before writing to disk.
2.  **Unity API Mapper (C#-side)**: A tool that exports the bridge's available methods and common Unity types into a "Cheat Sheet" for the AI.
3.  **JSON Schema Enforcement**: Strict parameter validation for all tool calls to ensure the AI doesn't send malformed data.

## 3. Components
1.  **C# Syntax Validator (Python-side)**: A lightweight C# parser integrated into `security_gate.py` to catch syntax errors (missing semicolons, mismatched braces) before writing to disk.
2.  **Unity API Mapper (C#-side)**: A tool that exports the bridge's available methods and common Unity types into a "Cheat Sheet" for the AI.
3.  **JSON Schema Enforcement**: Strict parameter validation for all tool calls to ensure the AI doesn't send malformed data.
4.  **In-Editor Test Runner**: Integration with the Unity Test Framework (UTF) to allow the AI to trigger and verify unit tests within the Unity process.

## 4. Monetization & Licensing
*   **Permissive Libraries**: All external tools (Roslyn-based logic, JSON schema) must use MIT or Apache 2.0 licenses to ensure full compatibility with the project's dual-license commercial model.
*   **Engine-Native**: Leverage built-in Unity features (UTF) to avoid proprietary "black box" dependencies.

## 5. Success Criteria
*   Zero "Syntax Error" compiler hangs during AI-driven Payload generation.
*   Automatic rejection of malformed tool calls before they reach the Unity Kernel.
*   Existence of a `metadata/unity_api_map.json` that the AI can consult to verify method signatures.
*   AI-triggered verification: The agent can say "run tests" and receive a pass/fail report from inside Unity.
