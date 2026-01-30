# UnityVibeBridge: The Governed Creation Kernel

## 🏛️ Technical Architecture
UnityVibeBridge transforms the Unity Editor into a deterministic Control Plane. It allows AI agents to interact safely with Unity's core engine via a mechanistic interface.

### Core Architecture

```mermaid
graph LR
    A[AI Agent] <-->|MCP Stdio/SSE| B[MCP Server]
    B <-->|Airlock JSON Queue| C[Unity Kernel]
    C -->|Express HTTP| B
```

1.  **AI Agent (Director)**: Issues high-level intents via MCP tool calls.
2.  **MCP Server (Translator)**: Python server that translates agent calls into Unity requests.
3.  **Unity Editor (Rigger)**: `VibeBridgeKernel.cs` executes operations using `Undo` and Reflection.

### The "Director" Workflow
Agents must follow a strict execution lifecycle to ensure state integrity:
1. **Discover**: `get_hierarchy` / `search_objects` -> Build scene map.
2. **Verify**: `inspect_object` -> Prove assumptions about components.
3. **Protect**: `begin_transaction` -> Create an undo safety net.
4. **Execute**: `rename` / `set_value` / `clone` -> Perform the mutation.
5. **Observe**: Check `_vibe_warning` in the response for project errors.
6. **Finalize**: `commit_transaction`.

### Core Safety Layers
1. **The Kernel Guard**: Mechanically blocks mutations during **Compilation**, **Play Mode**, or **Asset Import**. Check `metadata/vibe_status.json` before any mutation.
2. **Iron Box Protocol**: Every mutation MUST be wrapped in `begin_transaction` and `commit_transaction`. All AI actions are single, clean Undo steps in Unity.
3. **Time-Budgeted IPC**: Requests are processed in 5ms slices to maintain 60+ FPS in the Unity Editor.
4. **Token Security**: All mutations are authenticated via `X-Vibe-Token`.

---

## 📘 Further Reading
- For instructions on how to manage AI behavior and prevent hallucinations, see [AI_PHILOSOPHY.md](AI_PHILOSOPHY.md).
- For strict engineering rules, see [AI_ENGINEERING_CONSTRAINTS.md](AI_ENGINEERING_CONSTRAINTS.md).

## 🛠️ Unified Tool Inventory

### 1. 🧠 Epistemic & Cognitive Governance
*   **`inspect_object`**: Returns detailed components, tags, and transform state.
*   **`get_telemetry_errors`**: Streams last 50 console errors for truth reconciliation.
*   **`list_available_tools`**: Dynamic discovery of Payload capabilities.

### 2. 🛡️ Kernel & Integrity
*   **`transaction_begin` / `commit` / `abort`**: Atomic Undo-Group management.
*   **`system/execute-recipe`**: Atomic multi-tool batch execution.
*   **`guard/status`**: Checks for unsafe states (Compiling, Playing).
*   **Time-Budgeting**: Enforcement of 5ms slices (Automatic).

### 3. 🏗️ Scene Manipulation
*   **`get_hierarchy`**: Recursive Scene graph mapping.
*   **`system/search`**: Regex & Layer-based discovery.
*   **`object/set-value`**: Generic reflection-based property mutation (Supports Vector3, Color).
*   **`rename_object` / `reparent_object`**: Identity and hierarchy mutations.
*   **`clone_object` / `delete_object`**: Lifecycle management.
*   **`select_object`**: Focus-aware selection (Stealth framing).

### 4. 🎨 Technical Art & Optimization
*   **`material/list`**: Lists material slots on an object.
*   **`material/inspect-properties`**: Returns all shader properties for a slot.
*   **`material/set-color` / `set-texture`**: High-fidelity slot mutations.
*   **`material/set-float` / `toggle-keyword`**: Granular shader control.

### 5. 🔗 Pipeline & Infrastructure
*   **`registry/add`**: Persists a semantic role (e.g. "MainBody") for an object.
*   **`registry/list`**: Returns all registered semantic targets.
*   **`world/spawn`**: Prefab instantiation.
*   **`asset/rename` / `move`**: Project database management.
*   **`export/validate`**: Blender-readiness checks.
*   **`view/screenshot`**: High-speed visual verification.

### 🧹 Organizational Purity
All agent outputs are neatly sorted to prevent root directory clutter:
*   `captures/`: Timestamped screenshots and visual test history.
*   `metadata/`: Discovery logs and semantic object registries.
*   `optimizations/`: Output from automated optimization runs.
*   `HUMAN_ONLY/`: A sanctuary folder for human notes that is **mechanically invisible** to AI.

## Installation & Security

### 🚀 One-Click Bootstrap
If you point the agent to a new project, it can "self-install" the bridge:
`bootstrap_vibe_bridge(project_path="/path/to/project")`

### 🛡️ Recommended: The "Iron Box" Sandbox
For maximum safety, run the agent in an isolated Docker sandbox. This prevents the agent from seeing your personal files and restricts it to your project folder.

### 🔐 The Security Gate
Every code modification and shell command is audited by `security_gate.py` using AST logic analysis.
*   **Automatic Blocking**: Malicious imports and external network calls are blocked silently.
*   **Human Trust**: High-risk operations must be manually authorized.
