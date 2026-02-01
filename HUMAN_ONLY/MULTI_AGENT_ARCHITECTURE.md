# 🤖 UnityVibeBridge: Multi-Agent Isolation Architecture

This document defines the strategy for high-scale creation using multiple, isolated AI instances. This architecture prevents "Context Poisoning" and ensures engine-specific implementation details do not destabilize the global state model.

---

## 🏛️ 1. The Architecture of Isolation
We run three distinct AI instances, each with a strictly scoped **"Mental Sandbox"**:

### 🛰️ Agent Alpha: The Kernel Coordinator
- **Scope**: Sees only the Orchestrator/VibeSync MCP.
- **Rules**: Follows `BRIDGE_CONTRACT.md`. Deals exclusively in **UUIDs and Intents**.
- **Role**: Manages the **Write-Ahead Log (WAL)**. Decides *what* needs to happen (e.g., "The character must move from A to B").
- **Sandboxing**: Never sees a line of C# or Python. Oblivious to engine-specific limitations.

### 🎨 Agent Beta: The Blender Specialist
- **Scope**: Sees only the `BlenderVibeBridge` MCP.
- **Rules**: Follows the Blender-specific `FREEZE_PROOF_GUIDE.md`.
- **Role**: Receives high-level intents from Alpha and translates them into precise `bpy` operations.
- **Sandboxing**: Never knows Unity exists. Only knows it is fulfilling a contract for the Orchestrator.

### ⚛️ Agent Gamma: The Unity Specialist
- **Scope**: Sees only the `UnityVibeBridge` MCP.
- **Rules**: Follows the C# `AI_ENGINEERING_CONSTRAINTS.md`.
- **Role**: Ensures materials and physics components in Unity match the incoming hashes. 
- **Sandboxing**: Operates entirely in Y-up space and Left-Handed coordinates. Oblivious to Blender's Z-up world.

---

## 🛡️ 2. Preventing "Context Poisoning"
Poisoning occurs when engine-specific leakage (e.g., Unity "GameObjects" in a Blender context) confuses an agent. We stop this via **Strict Message Filtering**:

1.  **Protocol-Only IPC**: When Agent Alpha (Kernel) speaks to specialists, the message is filtered through a script that strips engine-specific terms. It only sends the **Protocol JSON**.
2.  **Stateless Specialist Prompts**: Specialists are treated as "fresh" or "ephemeral." They do not need turn-history; they only need the **Current State Hash** and the **Target Intent**.
3.  **The UUID Firewall**: All three agents use the same `global_id_map` provided by the Orchestrator. They can discuss "Crate_01" without ever sharing underlying technical handles (InstanceIDs or bpy pointers).

---

## ⚙️ 3. The "Ideal" Configuration
- **The Kernel (Alpha)**: Use a high-context, deep-reasoning model (e.g., **Gemini 1.5 Pro** or **Claude 3.5 Sonnet**). It needs the reasoning power to maintain the distributed state model.
- **The Adapters (Beta/Gamma)**: Use smaller, faster, "Highly Skilled Literalist" models (e.g., **Gemini Flash** or **Claude Haiku**). They just need to follow freeze-proof patterns and return valid JSON.

---

## 🏁 Summary
This protocol mandates multi-agent usage for high-scale work. The **VibeSync Orchestrator** acts as the "Air Gap" that keeps your Unity context and Blender context from ever touching each other.
