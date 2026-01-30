# UnityVibeBridge: The Governed Creation Kernel

## 🧠 Understanding Your AI "Co-Pilot"

### ⚙️ Demystifying the Magic: High-Fidelity Simulation
Large Language Models (LLMs) are **Probability Engines** that have become exceptionally good at **simulating aspects of sentience**. They don't "know" facts or feel emotions; they predict the next most likely sequence of tokens based on patterns in their training data and the "vibe" of your current conversation. 

In the context of Unity, the AI is not "visualizing" your Scene in a mind's eye. It is calculating the most statistically probable set of commands that align with your natural language intent.

### 🏷️ The Implied Sentience Trap (Combating AI Psychosis)
It is easy to fall into "magical thinking" when an AI responds with human-like warmth or technical authority. However, treating the AI as a sentient being—asking it what it "thinks" or "feels"—can lead to **AI Psychosis**: a state where the user forgets the AI is a simulation and begins to trust its hallucinations as objective truth.

**The Name Example**:
One of the best ways to combat "magical thinking" is to understand that the AI is simply reflecting your own intent. For example, if you ask an AI its name:
*   **Highly Technical Conversation**: It might name itself **"Nexus"** or **"Core"** to match your energy.
*   **ELI5 (Simple) Conversation**: It will likely choose **"Buddy"** or **"Sparky"** to please you.

The AI doesn't "have" a name; it calculates that giving you a name fits the pattern of a helpful assistant. Currently, we remain in the realm of high-fidelity simulation, not "True Sentience."

### ⚔️ Combatting Overconfidence: Adversarial Prompting
If you ever feel like you are doing something "groundbreaking" with the AI, that is the moment you need to be the most careful. It is easy to fall into a feedback loop where the AI just agrees with your greatness.

**The Strategy**: Use **Adversarial Prompting**. 
Ask the AI: *"I think this new logic is perfect. Now, I want you to act as a cynical auditor. Find 3 ways this could fail, crash Unity, or corrupt my Asset Database."*

Forcing the AI to argue *against* your ideas keeps you grounded in reality. Use the AI to test and destroy your own assumptions.

### 🧩 The Cognitive Gap
Humans have a mental map of reality (e.g., you know that **Shoes are on Feet**). An AI does not "see" your Unity Scene; it only sees data patterns and names. If it picks the wrong object, it's because it lacks your "human context." 
*   **The Fix**: Break tasks into the smallest units possible. Avoid "Vague Vibes." Do not assume the AI knows that a "Prop" should be parented to the "Hand" unless you explicitly tell it.

**UnityVibeBridge is designed to cure this drift by providing:**
1. **Numerical Telemetry**: Replacing "imagined" scenes with hard vertex counts and coordinates.
2. **Epistemic Reconciliation**: Forcing the AI to prove its assumptions against the actual Unity state via `audit_avatar`.
3. **Kernel Governance**: Ensuring that even if the AI "hallucinates" a dangerous intent, the system mechanically prevents the damage via the `SanityModule`.

---

## Concept: Mechanistic Vibe Coding

**UnityVibeBridge** bridges the gap between AI agents and the Unity Editor, specifically optimized for VRChat avatar creation (Vibe Coding). Instead of generating fragile C# scripts, it exposes a **Mechanistic Interface**—a set of deterministic tools to query state, inspect assets, and perform non-destructive modifications.

### Core Architecture

```mermaid
graph LR
    A[AI Agent] <-->|MCP Stdio/SSE| B[MCP Server]
    B <-->|HTTP JSON| C[Unity Editor]
    C -->|Console Logs| B
```

1.  **AI Agent (Director)**: Issues high-level intents via MCP tool calls.
2.  **MCP Server (Translator)**: Python FastAPI/FastMCP server that translates agent calls into Unity HTTP requests.
3.  **Unity Editor (Rigger)**: C# `InitializeOnLoad` server hook (`VibeBridgeServer.cs`). Executes operations using `AssetDatabase`, `Undo`, and Reflection.

## Completed Features (Phase 3 Hardened)

### 1. Safety & Stability (The Iron Box)
*   **Zero-Latency Heartbeat**: Domain reloads are detected via `metadata/vibe_health.json`. The AI monitors `isCompiling` and `state` to prevent command poisoning.
*   **Machine-Readable Diagnostics**: Unity streams structured console errors via `telemetry/get-errors`, replacing manual console inspection.
*   **Liveness Verification**: A 1-second interval heartbeat ensures the bridge is alive and not wedged in a deadlocked thread.
*   **Implicit Transactions**: The bridge automatically wraps mutations in atomic `Undo` groups. Every AI action is a single, clean Undo step in Unity.
*   **Provenance Tagging**: Every object created by the AI is session-tagged.
*   **Destruction Safety**: The `destroy_object` tool mechanically protects master files by refusing to delete any object created outside the current session.
*   **IP Hardening**: Strict local-only binding (`127.0.0.1`) ensures zero external attack surface.
*   **Capability Discovery**: Tools to check if an object is static, a prefab, or valid for specific operations.
*   **Fail Fast**: Unity console logs and exceptions are streamed back to the agent for immediate feedback.

### 2. Deep Inspection
*   **Hierarchy & Components**: Detailed dumps of scene structure and object state.
*   **Asset Inspection**: Peek inside prefabs and material shader properties without instantiating them.
*   **Render Summary**: Lightweight visual verification (renderer counts, material lists, bounds).

### 3. Precise Manipulation
*   **Transforms**: Set position, rotation, and scale locally or globally.
*   **Component Editing**: Read and write public fields/properties via reflection.
*   **Asset Management**: Find, instantiate, and reparent assets safely.

## Key Principles (The AI Safety Manual)
See [AI_ENGINEERING_CONSTRAINTS.md](AI_ENGINEERING_CONSTRAINTS.md) for the full list.
*   **Read-Before-Write**: Always `Inspect → Validate → Mutate → Verify`.
*   **Idempotence**: Every operation must be safe to repeat.
*   **Zero Trust**: All AI mutations are strictly boxed and logged.
*   **No Hidden Side Effects**: No file writes outside whitelist, no arbitrary C# execution.

### 🎨 Technical Artist Tools
The bridge includes a suite of tools for professional avatar optimization and world building:
*   **VRAM Auditing**: `calculate_vram_footprint` finds "PC Hidden Killers" (massive textures).
*   **One-Click Quest Bake**: `swap_to_quest_shaders` and `crush_textures` automate the mobile transition.
*   **PhysBone Ranking**: `rank_physbone_importance` intelligently prunes bones to meet Quest limits.
*   **Bake Guard**: `validate_bake_readiness` and `set_static_flags` ensure world assets are correctly configured for lightmapping.
*   **Non-Destructive**: `create_optimization_variant` creates copies so your master files stay safe.

### 🧹 Organizational Purity
All agent outputs are neatly sorted to prevent root directory clutter:
*   `captures/`: Timestamped screenshots and visual test history.
*   `metadata/`: Discovery logs and semantic object registries.
*   `optimizations/`: Output from automated optimization runs.
*   `HUMAN_ONLY/`: A sanctuary folder for human notes that is **mechanically invisible** to AI.

## Installation & Security

### 🚀 One-Click Bootstrap
If you point the agent to a new project, it can "self-install" the bridge:
```bash
goose run -t "bootstrap the VibeBridge into /path/to/my/project"
```

### 🛡️ Recommended: The "Iron Box" Sandbox

For maximum safety, run the agent in an isolated Docker sandbox. This prevents the agent from seeing your personal files (SSH keys, documents) and restricts it to your project folder.



1.  **Build & Launch**:

    ```bash

    ./start_sandbox.sh

    ```

    *Note: Requires Docker. Automatically builds the Goose CLI and sets up the Python environment.*



### 🔐 The Security Gate

Every code modification and shell command is audited by `security_gate.py` using AST logic analysis.

*   **Automatic Blocking**: Malicious imports, external network calls, and unsafe file paths are blocked silently.

*   **Human Trust**: If a high-risk operation is necessary, you must manually authorize it:

    ```bash

    python3 security_gate.py <file_path> --trust

    ```



### Manual Setup (On-the-Metal)

1.  **Unity**: Copy `unity-package/Scripts/VibeBridgeServer.cs` to your `Assets` folder.

2.  **Dependencies**: `pip install -r mcp-server/requirements.txt`.

3.  **Run**: `python mcp-server/server.py`.
