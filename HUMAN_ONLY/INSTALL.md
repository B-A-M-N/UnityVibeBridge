# 🛠️ UnityVibeBridge: Technical Engineering & Installation Guide

This guide is intended for **Engineers, Technical Artists, and Power Users**. It provides a deep dive into the bridge architecture, security invariants, and advanced orchestration patterns.

---

## 🏗️ 1. System Architecture Overview

UnityVibeBridge operates as a **Split-Plane Kernel**:

1.  **The Control Plane (Python/MCP)**: A high-performance middleware that handles AST auditing, token-based authentication, and protocol translation.
2.  **The Execution Plane (C#/Kernel)**: A time-budgeted Unity Editor extension that executes mutations within atomic `Undo` transactions.

### Architectural Invariants:
- **Zero-Trust Persistence**: The AI never holds a direct reference to Unity objects. It uses `InstanceID` or `Semantic Paths` which are validated every frame.
- **Main-Thread Budgeting**: The Kernel enforces a **5ms execution slice** per frame to prevent Editor hitching.
- **Atomicity**: One AI request = One Undo Group. Partial failures trigger an immediate `transaction_abort`.

---

## 🚀 2. Advanced Installation & Hardening

### Prerequisites
- **Unity**: 2021.3 LTS or newer (Recommended).
- **Python**: 3.10+ with `mcp` and `uvicorn` packages.
- **Node.js**: (Optional) Required if using the SSE (Server-Sent Events) transport layer.

### Deployment Steps
1.  **Kernel Injection**:
    ```bash
    cp -r unity-package/* /your-project-path/Assets/
    ```
2.  **Environment Setup**:
    Initialize the virtual environment to ensure the `scripts/security_gate.py` can audit payloads:
    ```bash
    python3 -m venv venv
    source venv/bin/activate
    pip install -r mcp-server/requirements.txt
    ```
3.  **Security Hardening (The "Iron Box")**:
    If running in a production environment, use the provided Docker sandbox to isolate the AI from your host system:
    ```bash
    ./scripts/start_sandbox.sh
    ```

---

## 📦 3. Git Infrastructure & Forensic Auditing

### A. Git LFS (Large File Support)
Unity projects are binary-heavy. We mandate Git LFS to maintain stable state hashes:
1.  **Initialize**: `git lfs install`
2.  **Track Assets**: The project includes a pre-configured `.gitattributes` file that tracks `.fbx`, `.png`, `.mat`, and `.prefab` files.
3.  **Verify**: Run `git lfs ls-files` to ensure high-scale assets are offloaded.

### B. Forensic Git Logging (Secondary Ledger)
Every AI mutation is logged to `logs/vibe_audit.jsonl`. For production-grade immutability, we recommend tracking this folder in Git:
1.  **Whitelist Logs**: Ensure `!logs/vibe_audit.jsonl` is in your `.gitignore` (Default in v1.5).
2.  **Commit Frequency**: Configure a local hook or manual process to commit the audit log after every major session. This creates a permanent, non-repudiable history of AI actions.

---

## 🧠 4. Advanced Epistemic Governance

To prevent "AI Psychosis" at an engineering level, the bridge implements **Truth Reconciliation Loops**.

### A. Semantic Pathing (`sem:`)
Instead of brittle names, use the **Vibe Registry**. This allows you to tag objects with persistent roles that survive hierarchy changes:
- **Command**: `registry/tag(path="Root/Hips/Spine", role="MainBody")`
- **AI Access**: `inspect_object(path="sem:MainBody")`

### B. Telemetry & Console Streaming
The AI has direct access to the Unity Log Buffer. This is used for "Self-Correction" loops:
- **Tool**: `get_telemetry_errors`
- **Pattern**: If a mutation fails, the AI is instructed to pull the last 50 logs to identify the specific C# exception (`NullReference`, `MissingComponent`, etc.) and adjust its logic.

---

## 🛠️ 4. Transactional Mutation Mastery

### Atomic Recipes
For complex tasks (like a full Quest conversion), never send multiple individual tool calls. Use the `system/execute-recipe` tool to batch operations.
```json
{
  "recipe": [
    {"tool": "texture/crush", "args": {"maxSize": 512}},
    {"tool": "material/swap-shader", "args": {"target": "VRChat/Mobile/ToonLit"}}
  ]
}
```
*The Kernel will wrap this entire list in a single Undo group. If the shader swap fails, the textures are automatically un-crushed.*

---

## 🛡️ 5. Security & AST Auditing

The `scripts/security_gate.py` uses **Recursive AST Parsing** to block malicious payloads. 

### Custom Whitelisting
If you need to allow a specific "Risky" namespace (e.g., `System.IO`), you must manually sign the script:
1. Attempt the operation (it will be blocked).
2. Run: `python3 scripts/security_gate.py --trust Assets/YourScript.cs`
3. The SHA-256 fingerprint is added to `metadata/trusted_signatures.json`.

---

## 📊 6. Performance & VRAM Profiling

### VRAM Footprint Analysis
The `vram_footprint` tool doesn't just look at file size; it queries the **GPU Upload Buffer**.
- **Usage**: `vram_footprint(path="Assets/MyAvatar")`
- **Insight**: Returns the actual uncompressed size on the GPU, helping you identify 8k textures hidden behind "High Quality" presets.

---

## 💀 7. Debugging the Bridge

| Symptom | Diagnosis | Solution |
| :--- | :--- | :--- |
| **403 Forbidden** | Token Mismatch | Check `X-Vibe-Token` in your MCP config. |
| **503 Service Unavailable** | Kernel Guard active | Unity is likely Compiling or in Play Mode. |
| **Hanging Requests** | Main-thread Deadlock | Ensure no `EditorUtility.DisplayDialog` is open in Unity. |

---
**Created by the Vibe Bridge Engineering Team.**
