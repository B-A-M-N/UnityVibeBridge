# 🔐 UnityVibeBridge: Security & AST Auditing Specifications

This document defines the technical invariants of the "Iron Box" security model and the logic of the `security_gate.py` auditor.

---

## 🛡️ The "Iron Box" Protocol
The Iron Box is a four-layer defense-in-depth strategy:
1.  **Transport Security**: Port 8085 is bound to `localhost` and requires an `X-Vibe-Token`.
2.  **Structural Security**: Mutations are only possible via a whitelist of C# primitives in the Kernel.
3.  **AST Auditing**: All incoming tool payloads are scanned for malicious logic.
4.  **Transactional Security**: Atomic Undo/Redo ensures any "Logic Bomb" can be instantly reversed.

---

## 🔬 Recursive AST Auditing (`security_gate.py`)
Before a command is sent to Unity, the Python middleware parses the payload.

### 🚫 Forbidden Keywords (Namespace Blocking)
Any payload containing these strings is mechanically rejected:
- `System.Reflection`
- `System.Diagnostics.Process`
- `System.IO` (except for whitelisted Airlock paths)
- `UnityEngine.Windows`
- `UnityEngine.Apple`
- `UnityEditor.ProjectSettings`

### 🚫 Behavioral Blocking
- **Obfuscation Detection**: Rejects strings containing high-entropy base64 or hex blocks that look like encoded shellcode.
- **Dynamic Attribute Access**: In Python payloads, `getattr()` and `setattr()` are blocked if they target built-in restricted objects.
- **Recursion Depth**: Limits tool-nesting depth in `execute-recipe` to prevent StackOverflow DoS.

---

## 📝 The Audit Ledger (`vibe_audit.jsonl`)
Every mutation is recorded in a cryptographically chained ledger.
- **Structure**: `{"timestamp": "...", "action": "...", "hash": "...", "prevHash": "..."}`
- **Immutability**: The Kernel appends to this file in a "Write-Once" mode.
- **Verification**: On startup, the Kernel verifies the chain. If a link is broken, the Kernel enters `VETO` mode.

---

## 🛡️ "Zero-Trust" Identity (Session Nonces)
Objects created by the AI are tagged with a `SessionNonce` (int).
- **Rule**: The AI can only delete or modify objects that have a matching Nonce.
- **Exception**: Objects in the `vibe_registry.json` are explicitly whitelisted by the Human for AI modification.
- **Goal**: Prevents "Hallucinated Purges" of pre-existing project assets.

---
**Copyright (C) 2026 B-A-M-N**
