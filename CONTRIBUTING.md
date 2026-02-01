# Contributing to UnityVibeBridge

Thank you for your interest in improving the Governed Creation Kernel. This project is a research prototype, and we welcome contributions that strengthen its security, performance, and cross-tool synchronization.

## 🛠️ Developer Setup
1.  **Unity**: Install Unity 2019.4 LTS or newer.
2.  **Python**: Ensure Python 3.10+ is installed.
3.  **Dependencies**: Run `pip install -r mcp-server/requirements.txt`.
4.  **Sandbox (Optional)**: See `scripts/Dockerfile.sandbox` for an isolated development environment.

## 🏛️ Project Structure
- `unity-package/`: The core C# Kernel and Payloads.
- `mcp-server/`: The Python middleware and AST auditing logic.
- `metadata/`: Technical specifications and persistent registries.
- `docs/`: AI system prompts and security specifications.

## 🛡️ Security & Engineering Standards
All code contributions **MUST** adhere to:
- `AI_ENGINEERING_CONSTRAINTS.md` (Zero-Trust behavior).
- `AI_SECURITY_THREAT_ACCEPTANCE.md` (Risk model).
- **Split Architecture**: Foundations in `VibeBridgeKernel.cs`; all new features in separate "Payload" files.
- **The 5ms Rule**: Operations must be time-sliced to maintain Unity's framerate.
- **Atomic Mutations**: Use `Undo.RecordObject` and `EnsureSafeMutationContext` for all changes.

## 🔬 Testing & Validation
- **C#**: Add Editor tests to `unity-package/Tests/Editor/`.
- **Python**: Add integration tests to `security_tests/` and run with `pytest`.
- **AST Gate**: Ensure new tools don't trigger false positives in `scripts/security_gate.py`.

## 📘 Further Reading
- [Technical Specifications](metadata/TECHNICAL_SPECS.md)
- [Failure Modes & Recovery](HUMAN_ONLY/FAILURE_MODES.md)
- [For Hiring Managers](HUMAN_ONLY/FOR_HIRING_MANAGERS.md)
- [Beginner's Guide](HUMAN_ONLY/FOR_BEGINNERS.md)

---
**Copyright (C) 2026 B-A-M-N**
