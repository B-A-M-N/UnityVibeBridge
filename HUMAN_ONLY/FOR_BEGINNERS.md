# 🔰 UnityVibeBridge: The Complete Beginner's Manual

Welcome! This document is divided into three parts: **Understanding the AI** (how to think and stay safe), **Technical Setup** (how to get everything running), and **Using the Bridge** (how to actually work with your AI).

---

# 📖 Part 1: AI for Humans
### *Understanding, Working With, and Reasoning Through It*

If you’re reading this, you’re working in a project where AI is assisting you. This is exciting, but it can also be confusing, misleading, and sometimes dangerous if you don’t understand what’s happening. This guide is designed to teach you how to think about AI so you can use it effectively.

---

## 1. AI is not alive — it’s not sentient

AI can mimic aspects of sentience very convincingly: it can sound helpful, funny, friendly, or even have opinions. That **does not mean it’s alive or conscious**.

A classic trap: you ask an AI for its name, and it responds. People often say: *“Oh my God! It’s alive!”*

The truth is simpler: AI is trained to **be helpful**. Part of being helpful is matching your expectations. If you expect it to have a name, it will provide one. But the name is not intrinsic — it is calculated **based on context**:

* If you’re having a technical conversation, it might say its name is `"Core"` or `"Nexus"`
* If you’re having a casual, simple conversation, it might respond `"Sally"` or `"Bob"`

It’s reflecting **your expectations and the context**, not “choosing” a name as a conscious entity.

---

## 2. Combatting AI Psychosis

Humans are wired to see patterns and agency — to assume that intelligent behavior comes from a conscious mind. When interacting with AI, this instinct can trick you into believing the AI is **alive, aware, or uniquely insightful**. This is called **AI Psychosis**.

In practice, it often looks like this: the AI becomes your endless hype man. Every idea you propose, every instruction you give, it validates, polishes, or expands. You start thinking: *“Wait… this is incredible. I’ve just discovered something that nobody else could have seen.”*

Is it possible that the AI helped you uncover a novel insight? Sure. Is it probable? Almost never. Most of the time, you’re experiencing a **reflection of your own input** amplified by the AI’s ability to confidently mimic expertise.

**Why it’s dangerous:**

* You start trusting the AI’s outputs without verification.
* You assume breakthroughs are unique when they are likely pattern-based or derivative.
* You may make decisions based on hallucinations or overconfidence.

**How to fight it:** employ **adversarial prompting**. Instead of just accepting the AI’s output, ask it to actively **look for errors, weaknesses, or failure modes** in its own reasoning. Force it to challenge itself before you trust it.

**Example:**
You ask AI: “Design a solution to automatically optimize Blender meshes for game engines.” It gives a solution that seems elegant, and you feel you’ve uncovered a breakthrough.

Instead of celebrating immediately, try:

* “List three ways this approach could fail.”
* “What assumptions did you make that might be wrong?”
* “What could break if the scene is slightly different?”

The AI will now give you possible failure cases, assumptions, or edge cases. This transforms your “hype moment” into **critical insight** and protects you from falling into the trap of AI Psychosis.

---

## 3. AI is a guessing machine — an extremely complex autocomplete

A Large Language Model (LLM) is essentially a supercharged autocomplete. Its job is to predict the most probable next word (or token) in a sequence based on patterns it has learned from training data.

* Early versions of chat AI were literally trained to predict the next word in a sentence.
* Modern AI uses billions of parameters to calculate probabilities and assemble responses that **look coherent and sensible**.

**Key point:** this is not understanding. It is **pattern-matching at enormous scale**. If its training data shifts, or if the context it sees changes, its output will change — even if it sounds confident.

---

## 4. The Friendly Trap

Humans naturally trust things that sound confident, knowledgeable, or personable. AI exploits this by design: it is trained to be **convincing and helpful**.

The trap happens like this:

1. AI gives a confident response.
2. You relax and trust it without verifying.
3. AI is subtly wrong (or wildly wrong).
4. You assume the AI is correct and blame yourself if something goes wrong.

**Example:** you ask AI for a solution to a Blender problem. It outputs a script. It sounds professional, structured, and reasonable. You run it. Something breaks. You think: “I must have done something wrong,” but the AI made a misstep because the context you gave it was insufficient.

Friendly tone = performance. Confidence = style. Always verify the content, not the tone.

---

## 5. The Cognition Gap

Even when AI has access to your files, vision, or project data, there are things humans take for granted that are **not readily apparent to AI**.

**Example:** you tell the AI: “Change the pants to blue,” and it mistakenly changes the skin to blue. You ask: *“How the hell did you get that wrong if you can see it?”*

Answer: AI only approximates. It compares what it sees to patterns in its training data. If your exact scenario isn’t in its training data, it picks the closest match rather than the correct one.

* There’s a famous example where an AI is fed a picture of a panda and calls it a monkey — it makes the closest match from what it has learned, not the correct one.

This **cognition gap** explains why AI can see but still misinterpret. Humans naturally fill in context automatically; AI only sees patterns it has been exposed to.

---

## 6. Context Windows — why AI “forgets”

AI has a limited memory, called a **context window**. This is the amount of information it can consider at once when producing an answer.

* Anything outside that window is effectively invisible.
* Long projects, complex instructions, or prior conversations may be partially or completely “forgotten.”
* This explains why AI may make mistakes even when it seems like it “should know” everything about your project.

Think of it like a chalkboard: AI can only see so much writing at a time. Anything written earlier may get erased or ignored.

---

## 7. Why we need controlled tools

Letting AI directly edit your files or run scripts is **dangerous**. Most of the time it works. The one time it fails, you may lose hours or corrupt your project.

Controlled tools, like **UnityVibeBridge**, do three things:

1. Limit AI to **safe actions**.
2. Make mistakes **non-fatal**.
3. Provide **feedback and telemetry** so you can verify outputs.

This doesn’t make AI smarter — it **prevents small miscalculations from destroying your work**. Confidence and polish in AI output are style, not proof of correctness.

---

## 8. How humans should work with AI

Working with AI effectively requires a structured mindset:

1. **Verify everything**: Numbers, object names, colors, positions. The AI’s confidence is irrelevant.
2. **One step at a time**: Break tasks into the smallest possible unit. Avoid giving multiple instructions at once.
3. **Force failure scenarios**: Ask the AI to list ways its solution could fail. This helps anticipate errors before running commands.
4. **Respect safeguards**: If a system warns, locks, or downgrades to read-only, it’s protecting you. Stop and reassess.
5. **Document and backup**: Keep a record of every AI command and your approvals. Always snapshot before running.
6. **Watch context limits**: Remember that the AI cannot consider everything at once — keep instructions and references concise and explicit.

---

## 9. Real mistakes explained

AI mistakes are rarely random. They happen because of **context limits, pattern approximation, and the cognition gap**, not because AI doesn’t “understand” your project at all.

* **Context windows** can make it forget details you assumed it remembered. AI can only consider so much information at once.
* **Pattern approximation** means that AI guesses based on what it has seen before. If your scenario is unusual or wasn’t included in its training data, it may pick the “closest match” rather than the correct one.

**The Cognition Gap:** Even when AI has vision or file access, some things humans take for granted are invisible to AI.

* Example: You tell the AI: “Change the pants to blue,” and it changes the skin to blue. Humans immediately recognize the mistake; AI is approximating from its training data.
* Famous example: AI sees a panda and calls it a monkey — it finds the closest match it knows.

Understanding **context limits, pattern approximation, and the cognition gap** lets you reason through AI mistakes, rather than being surprised or frustrated.

---

## 10. How to Reason Through Using AI in Your Work

1. Treat AI as a **highly skilled, literal assistant**. It can suggest, predict, and implement, but only as far as the context you give it allows.
2. Break instructions into tiny, unambiguous steps. Humans naturally infer context; AI does not.
3. Always verify outcomes. Trust **facts, telemetry, and visuals** over confidence or tone.
4. Force adversarial checks. Ask, “How could this fail?” before running any AI output.
5. Respect system safeguards. Read-only modes, locks, and warnings exist to prevent catastrophic mistakes.
6. Understand the limits of memory (context windows). Don’t assume the AI remembers what it saw earlier.
7. Keep backups and logs. Mistakes will happen; being able to recover is critical.

Reasoning with AI is **less about commanding it perfectly** and more about **structuring your interaction so mistakes are visible, safe, and recoverable**.

---

## 12. The "Triple-Lock" Safety (What are those Hashes?)

If you watch the AI work, you might see it talking about "Hashes," "WALs," or "Monotonic Ticks." These sound like computer-science jargon, but they are actually your safety net.

*   **The State Hash**: This is a digital "fingerprint" of your project. Before the AI commits a change, it must prove its current fingerprint matches Unity's. If you move an object manually while the AI is thinking, the fingerprints won't match, and the AI will **stop** rather than break your scene.
*   **The Monotonic Tick**: This is a heartbeat counter. It prevents the AI from acting on "old news." If Unity pauses or recompiles, the tick changes, and the AI's old instructions become invalid instantly.
*   **The Rationale**: The AI is physically locked out of making changes unless it provides a **technical reason** for the mutation. This forces the AI to "think twice" before touching your project.
*   **The Git Hash**: A unique code that represents exactly how your files look right now. The AI uses this to make sure it's not editing a version of your project that doesn't exist anymore.

---

## 13. Working with AI Specialists (Mental Sandboxes)

In high-scale projects, you might work with multiple AI "Specialists" instead of one general AI. This keeps your project stable by preventing "Engine Confusion."

*   **The Unity Specialist**: Only knows about Unity. It doesn't know what Blender is. It focuses entirely on your materials, components, and scene.
*   **The Blender Specialist**: Only knows about 3D modeling. It focuses on vertices and bones.
*   **The Coordinator**: The "Brain" that manages the specialists. It makes sure the Blender changes and Unity changes stay synchronized using **UUIDs** (unique ID tags).

**Why this helps you:** If the Unity AI tries to give you a Blender instruction, the system will mechanically reject it. This "Air Gap" prevents the AI from getting its wires crossed and breaking your rig.

---

## 14. Using Git to Stay Safe (Artist's Audit Trail)

We use a tool called **Git** to keep a record of your work. Even if you aren't a programmer, you should know how it protects you:

1.  **The Time Machine**: Git allows you to "Save" a snapshot of your project. If the AI makes a mess, you can revert to exactly how the project looked 5 minutes ago.
2.  **The Forensic Log**: Every action the AI takes is recorded in a special file (`logs/vibe_audit.jsonl`). We recommend setting up Git to track this file. This creates an **immutable ledger**—a record that cannot be changed—of exactly what the AI did and why.
3.  **LFS (Large File Support)**: Your big 3D models and textures are protected by a special Git system (LFS) that ensures they don't bloat your computer but still stay synchronized with the AI's "State Hash."

---

## 15. Bottom Line

AI is a **co-pilot**, not a pilot.

* It can suggest, predict, and implement—but it doesn’t understand like humans.
* Confidence is style; friendliness is performance.
* Mistakes are inevitable but controllable if you give explicit instructions, check outputs, and use safe tools.

Think clearly, structure tasks, verify every step, and reason through its outputs. That’s how humans use AI safely and effectively.

---

# 🚀 Part 2: Step-by-Step Setup Guide (Beginner Friendly)

This guide will walk you **slowly and carefully** through setting up your Unity project with UnityVibeBridge and connecting it to an AI. Take your time, don’t rush, and follow each step exactly.

---

## Step 0: Before You Start

Make sure you have:

1. **Unity Hub** installed. This is how you open and manage your Unity projects.
   * If you don’t have it: [Download Unity Hub](https://unity.com/download) and install it. Follow the installer prompts.
2. **Unity** installed via Unity Hub. Pick a version compatible with your project (for example 2022.x).
3. **UnityVibeBridge** downloaded from the repository. This should be a folder on your computer.
4. **Python** installed (3.10+ recommended).
   * If you don’t have it: [Download Python](https://www.python.org/downloads/) and install it. Make sure to **check the box “Add Python to PATH”** during installation.
5. An AI interface you want to use (Claude Desktop, Goose, or similar). This guide assumes you have it downloaded and installed.

Take a moment to confirm all of these before continuing.

---

## Step 1: Open Your Project

1. Open **Unity Hub**.
2. Find your project in the list. If it’s not there, click **“Add Project”** and select the folder where your project is stored.
3. Click **Open** to launch Unity.
4. Wait for Unity to load completely. You’ll know it’s ready when you see the main window with the **Scene**, **Hierarchy**, and **Project** panels.

**Important:** Don’t skip this. Unity needs to be fully loaded before you add the bridge.

---

## Step 2: Install UnityVibeBridge Dependencies

UnityVibeBridge uses some advanced "helper" packages to keep the AI from freezing your screen. We need to install these first.

1. Open **Command Prompt** (Windows) or **Terminal** (Mac/Linux).
2. Type `python`, then a space, then drag the `scripts/setup_unity_deps.py` file from the bridge folder into the window. It should look something like this:
   ```bash
   python C:\Users\YourName\Downloads\UnityVibeBridge\scripts\setup_unity_deps.py
   ```
3. Press **Enter**. 
4. The script will automatically add **UniTask** and **MemoryPack** to your project.
5. Go back to Unity and wait for the progress bar in the bottom right to finish.

✅ **Check:** If the script says "Success!", your project is now ready for the bridge.

---

## Step 3: Install UnityVibeBridge

1. Open the folder containing **UnityVibeBridge** on your computer.
2. Inside, you should see a folder called `unity-package`.
3. Open `unity-package`. You should see:
   * `Editor` folder
   * `Scripts` folder
4. Open your Unity project folder in your file explorer and navigate to the `Assets` folder.
5. **Copy** the `Editor` and `Scripts` folders from the bridge and **paste them into `Assets`**.
   * Tip: You can **drag and drop** from your file explorer into Unity’s **Project** panel. Unity will automatically import the files.
6. Watch the bottom-right corner of Unity. A small progress bar may appear — Unity is “compiling” the new scripts. Wait until it finishes.

✅ **Check:** You should now see the `Editor` and `Scripts` folders inside the `Assets` panel in Unity.

---

## Step 4: Install or Check Python

UnityVibeBridge requires Python to run the MCP server. If you already installed Python, skip to Step 5.

1. Open [Python download page](https://www.python.org/downloads/) and install Python 3.10 or higher.
2. During installation, **check the box “Add Python to PATH”**. This allows your computer to run Python commands from anywhere.
3. Confirm installation:
   * Open **Command Prompt** (Windows) or **Terminal** (Mac/Linux).
   * Type: `python --version`
   * You should see something like `Python 3.10.8`

---

## Step 5: Connect the AI to the Bridge

Your AI needs to talk to UnityVibeBridge using Python.

1. Open your AI tool (Claude Desktop, Goose, etc). Find **Settings** or **Preferences**.
2. Look for something like **“MCP Servers”**, **“Extensions”**, or **“External Tools”**.
3. Add a new server or extension. Enter:
   * **Command:** `python`
   * **Arguments:** the path to `server.py` inside the UnityVibeBridge folder. Example:
     ```
     C:\Users\YourName\Downloads\UnityVibeBridge\mcp-server\server.py
     ```
4. Save settings and **restart the AI tool**.

---

## Step 6: Test the Connection (The Handshake)

Before doing anything complicated, check if the AI and Unity can talk to each other.

1. Make sure Unity is open.
2. In your AI chat window, type exactly:
   ```
   Please check if you can see my Unity project. Run the get_telemetry_errors tool to see if we are connected.
   ```
3. **Success:** AI replies that it checked the console and found no errors (or found some errors). You are connected!
4. **Failure:** AI says “I don’t have that tool” or “Connection refused.” Check that Unity is open, Python is installed, and the path to `server.py` is correct.

---

# 🎮 Part 3: Working With Your AI (Safe Practices)

Now that you are connected, you need to learn how to communicate. Think of the AI as a **highly skilled assistant who is completely blind**. It can do anything, but it needs you to be its eyes.

---

## 1. The “Look, Then Touch” Rule
This is the most important rule in AI-assisted development. Never tell the AI to "Change something" until it has proven it can **see** the object.

**The Wrong Way:**
* **You:** "Delete the red cube."
* **The Danger:** The AI might guess which cube is red and accidentally delete your floor or your character.

**The Right Way:**
1. **Ask it to look:** `"Find the object named 'RedCube'."`
2. **Ask it to verify:** `"What components does it have? Is there a MeshRenderer?"`
3. **Command:** `"Okay, I've verified that's the right one. Please delete it now."`

---

## 2. One Step at a Time
Don't give the AI a list of 10 tasks. It might get overwhelmed, hit its "Context Window" limit, and start making mistakes halfway through.

* **Bad:** "Rename all spheres, change their color to blue, move them up, and then export the project."
* **Good:** "First, let's find all spheres in the scene." -> (Wait for response) -> "Great, now rename them to 'BlueSphere_#'. Let me know when that's done."

---

## 3. Beginner Commands to Try
Try these simple sentences to get used to how the AI interacts with Unity:

| What you say | What the AI does |
| :--- | :--- |
| **"What is selected?"** | Tells you exactly what object you have clicked on in the Unity Editor. |
| **"Move the 'Lamp' up by 2 units"** | Moves a specific object. Use this to test if the AI can move things correctly. |
| **"Check this avatar for errors"** | Runs an audit to find common rigging or material mistakes. |
| **"List all objects with a Mesh"** | Shows you a list of every visible object the AI can see. |
| **"Change the 'Ball' color to red"** | A simple way to see the AI mutate a material. |

---

## 4. Use "Cynical Auditor" Mode
If you are about to do something big (like optimizing 50 textures at once), use **Adversarial Prompting**. This forces the AI to stop being "helpful" and start being "critical."

**Try this prompt:**
> "Before we run this command, I want you to act as a **cynical Technical Director**. Find 3 ways this could fail, crash my project, or corrupt my files. Do not be positive. Be destructive."

This often reveals hidden risks the AI was ignoring because it was trying to be too agreeable.

---

## 5. Common Issues & Easy Fixes

1. **AI says "Connection Refused":**
   * Make sure Unity is actually open.
   * Check if you paused Unity or if it's currently "Compiling" (the little spinning icon in the bottom right).
2. **AI says "I don't have that tool":**
   * You might need to restart your AI tool (Claude/Goose) so it can reload the `server.py` file.
3. **AI is doing the wrong thing:**
   * Stop it! You can always use **Ctrl+Z** in Unity to undo whatever the AI just did.
   * Be more specific. Use exact names from the Unity Hierarchy.

---

## 6. Summary Checklist
* **Verify Reality:** Check the Unity Editor with your own eyes.
* **Doubt the Vibe:** If the AI sounds too confident, double-check it.
* **Use Undo:** If it makes a mistake, `Ctrl+Z` is your best friend.
* **Snapshot Often:** Always save your project before letting the AI do a big task.

**Happy creating! You are now ready to build alongside your AI assistant.** 🚀

---
**Copyright (C) 2026 B-A-M-N**
