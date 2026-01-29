# 🌉 UnityVibeBridge

> [!WARNING]
> **EXPERIMENTAL & IN-DEVELOPMENT**  
> This project is currently an active research prototype. APIs, security protocols, and core logic are subject to rapid, breaking changes. This software performs destructive mutations on Unity projects; **MANDATORY BACKUPS** are required before use.

### The "One-Click" Technical Artist for VRChat & World Building
*A production-grade AI control interface for deterministic, undo-safe Unity Editor operations.*

---

## 🖼️ Previews

| **Unity Bridge Interface** | **AI Logic & Security Gate** |
| :---: | :---: |
| ![Unity Bridge](captures/preview_ui_1.png) | ![Security Gate](captures/preview_ui_2.png) |

---

## ⚠️ Read This First (Why This Exists)

**UnityVibeBridge** is not a toy, a prompt wrapper, or a "magic AI button." 

It is a **reference implementation of AI-assisted systems design** in a hostile, stateful environment. It allows Large Language Models (LLMs) to safely operate inside the Unity Editor without risking project corruption, data exfiltration, or runaway execution.

This project answers a critical engineering question: 
> *How do you let an AI act inside a complex, stateful application—without trusting it?*

**The answer is: You don’t. You constrain it.**

---

## 🚀 What You Can Do With This (In Practice)

**Today, with a real VRChat avatar or world:**

*   **PC → Quest Conversion**: Safely migrate avatars to mobile without destroying the original.
*   **VRAM Auditing**: Find and eliminate the "hidden killers" tanking your performance.
*   **Non-Destructive Optimization**: Generate variants and bake textures safely.
*   **Mechanistic Rigging**: Rewire materials, menus, and parameters without touching Unity by hand.
*   **Secure AI Assistance**: Let an AI assist your workflow without ever letting it "free roam" your project.

This system is actively used on a production VRChat avatar and was built end-to-end in under a week to survive mistakes.

---

## 🧠 What This Project Demonstrates (For Engineers & Hiring Managers)

If you are evaluating this project as an engineer or hiring manager, this repository is a working demonstration of **AI Systems Engineering**:

*   **Control-Plane vs. Execution-Plane Separation**: LLMs generate *intent* (Mechanistic Intents), never raw code execution.
*   **Iron Box Security**: Hardened via Docker isolation, static AST auditing, and header-based heartbeats.
*   **Transactional State Mutation**: Every operation is wrapped in undo-safe, atomic transactions. **One AI request = One Undo step.**
*   **Deterministic Asset Manipulation**: Control over material slots, colors, and hierarchies is handled mechanistically, eliminating the fragility of raw reflection scripts.
*   **Provenance Tagging**: The AI can ONLY destroy or modify assets it created in-session.

---

## 🎨 Technical Artist Toolkit

UnityVibeBridge transforms natural language intents into professional-grade Unity operations. 

| **Capability** | **Feature** |
| :--- | :--- |
| 🛡️ **Iron Box** | Hardened security via Docker, AST Auditing, and Header-Based Heartbeats. |
| 🎨 **Mechanistic Rigging** | Deterministic control over material slots, colors, and hierarchies. |
| 🚀 **High-Speed Ops** | Batched asset reimports and implicit transactions for lag-free flow. |
| 🧠 **Semantic Memory** | Persistent `vibe_registry.json` tracks asset roles across sessions. |

### 🛠️ Advanced Automation Suite
*   **VRAM Auditing**: `calculate_vram_footprint` finds massive textures and PC "kill-streaks."
*   **One-Click Quest Bake**: `swap_to_quest_shaders` and `crush_textures` automate the mobile transition.
*   **PhysBone Ranking**: Intelligently prunes bones to meet Quest Excellent/Good limits.
*   **Bake Guard**: `validate_bake_readiness` ensures world assets are correctly configured for lightmapping.

---

## 🐣 Beginner's Guide (Start Here)

If you have never used a command line or written a line of code, follow these steps exactly.

### 1. The Core Environment
#### 🟢 A. Unity & VRChat Creator Companion (VCC)
1.  **Download VCC**: [vrc.com/download](https://vrchat.com/home/download).
2.  **Install Unity**: Use the version recommended by VCC (currently **Unity 2022.3.x LTS**).
3.  **Required Packages**: Add **VRChat SDK (Avatars)**, **Modular Avatar**, and **Poiyomi Shaders**.

#### 🟡 B. Python (The "Bridge" Engine)
1.  **Download**: [Python.org](https://www.python.org/downloads/).
2.  **⚠️ CRITICAL**: During installation, check **"Add Python to PATH"**.

#### 🛡️ C. Docker (The "Safety Cage") - *Highly Recommended*
1.  **Download**: [Docker Desktop](https://www.docker.com/products/docker-desktop/).
2.  **Start**: Keep it running in the background to cage the AI and protect your private files.

---

## ▶️ Launching the Bridge

```bash
cd UnityVibeBridge
./start_sandbox.sh     # Recommended (Docker)
# or
python mcp-server/server.py
```

---

## ✨ Master the Art of Vibe Coding

### 1. Mechanistic Benchmarking
| **Quality** | **Example Prompt** | **The Result** |
| :--- | :--- | :--- |
| ❌ **Terrible** | *"Fix my hair."* | **Confusion.** AI might target the ears or use wrong params. |
| 🟡 **Advanced** | *"The hair is its own group. Only change the hair. Each side needs its own color wheel."* | **Good.** AI understands constraints but must guess the technical names. |
| 💎 **Optimal** | *"Target 'Hair_L'. In a sub-menu 'Stylist', create a Color Wheel (Hue) and Pitch (Val). Set params to Float and Saved."* | **Perfect.** Zero ambiguity. Precise direction executed flawlessly. |

### 2. The Art of Specificity
Precision is king. 
*   ❌ **Vague**: *"Make it red."*
*   ✅ **Pro**: *"On the 'Head' object, set '_EmissionColor' on Slot 3 to '1,0,0,1'."*

---

## 🧠 AI Literacy & Philosophy (Important)

### 🏷️ The Implied Sentience Trap (Combating AI Psychosis)
It is easy to fall into "magical thinking" when an AI responds with warmth. This project deliberately demystifies the LLM. Asking an AI *"What do you think?"* does not demonstrate consciousness; the AI is simply reflecting your intent to treat it as a thinking being.

**The Name Example**:
If you ask an AI its name, it calculates that giving you a name fits the pattern of a helpful assistant. It does not "have" a name. Until physical computing architecture evolves, we remain in the realm of high-fidelity simulation, not AGI.

### ⚔️ Combatting Overconfidence: Adversarial Prompting
If you feel you are doing something "groundbreaking," use **Adversarial Prompting**:
Ask the AI: *"I think this logic is perfect. Now, act as a cynical auditor. Find 3 ways this could fail, crash Unity, or corrupt my metadata."*
Force the AI to argue *against* your ideas to stay grounded in reality.

---

## 🏛️ Recommended Architecture: The "Airlock" Setup
Keep your **Tools** and your **Content** separate.
1.  **Central Toolkit**: Keep `UnityVibeBridge` in its own repository.
2.  **The Project**: Keep your Unity project in its own separate folder.
3.  **Portable Memory**: Move the `/metadata` folder into your Unity project to ensure the AI "remembers" your avatar perfectly on any machine.

---

## 🆘 Troubleshooting
*   **Invalid Host**: Use `127.0.0.1` instead of `localhost`.
*   **Permission Denied**: Run `python3 security_gate.py <file> --trust` if a modification is blocked.
*   **Unity "Hiccups"**: The AI is likely batching changes. Wait for it to finish or call `AssetDatabase.Refresh`.

---

## 🧑‍💻 About the Author
I specialize in **Local LLM applications and secure AI-Human interfaces**. This system was built end-to-end by a single developer in under a week and is actively used on a production VRChat avatar. UnityVibeBridge was born from a desire for creative freedom—building the tools I didn't know how to use manually. It is a gift to the community to level the playing field and empower human craftsmanship.
