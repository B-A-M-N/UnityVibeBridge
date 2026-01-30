# AI Philosophy & Safety: UnityVibeBridge

This document explains the core concepts behind interacting with AI agents in Unity and how to prevent "hallucination drift" through specific techniques.

---

## 🧠 1. The "Probability Engine" Reality
AI agents (LLMs) do not "see" your Unity Scene. They are **Probability Engines** calculating the most statistically likely set of tool calls based on your natural language intent.

### The cognitive gap:
- **Human**: "I know shoes go on feet."
- **AI**: "I see an object named 'Shoes' and a bone named 'Foot'. I will guess they belong together."

**Example of failure**: If you have two objects named "Body," the AI may pick the first one it sees, even if it's the wrong one, because it lacks the visual context you have.

---

## 🏷️ 2. Avoiding "AI Psychosis"
"AI Psychosis" occurs when a user treats the AI as a sentient technical director rather than a simulation. This leads to the user trusting the AI's "overconfidence" even when its logic is flawed.

**The Rule**: Never ask the AI what it "thinks" or "feels" about the scene. Only ask for data-driven results (vertex counts, component lists).

---

## ⚔️ 3. Adversarial Prompting (The Auditor Pattern)
When you are about to perform a major change (like crushing all textures or forking an avatar), you should use **Adversarial Prompting** to force the AI to double-check its own logic.

### How to do it:
Ask the AI: *"I think this logic is perfect. Now, act as a cynical auditor. Find 3 ways this could fail, crash Unity, or corrupt my Asset Database."*

### Example:
**User**: "Fork this avatar for Quest."
**AI**: "Ready to fork. I will duplicate the object and swap shaders."
**User (Adversarial)**: "Wait. Act as a cynical auditor. Why will this fail?"
**AI (Re-evaluating)**: "Actually, if I swap shaders now, I might overwrite the materials on your PC version because they are shared. I should isolate the materials into a new folder first."

---

## 🕵️ 4. Read-Before-Write
Before allowing the AI to mutate any object, it must prove it understands the target.

**Bad Flow**:
- **User**: "Delete the cube."
- **AI**: `delete_object(path="Cube")`

**Good Flow**:
- **User**: "Delete the cube."
- **AI**: `inspect_object(path="Cube")` -> Returns components.
- **AI**: "I see this Cube has a 'BoxCollider' and 'MeshRenderer'. Proceeding to delete."
- **AI**: `delete_object(path="Cube")`

---
**Copyright (C) 2026 B-A-M-N**
