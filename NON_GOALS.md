# UnityVibeBridge: Non-Goals & Doctrine

This document defines the intentional limitations of the UnityVibeBridge system. Adhering to these non-goals prevents "AI Psychosis" and ensures deterministic project outcomes.

---

## 🚫 1. No Creative Autonomy
UnityVibeBridge will **never** attempt to "improve" or "optimize" your scene creatively without a specific, bounded command.
- It will not "fix" your material assignments unless told to.
- It will not auto-rename objects to "clean up" the hierarchy.
- It will not suggest "better" lighting settings.

## 🚫 2. No Silent Self-Healing
If a mutation fails or reality diverges from AI intent, the system will **never** silently try to "make it work."
- Failure is a first-class, desirable outcome.
- We would rather provide a 500 Error and halt than perform a 90% accurate mutation.

## 🚫 3. No Guessing Intent
The bridge will **never** infer intent from ambiguous natural language.
- If you say "Make it bright," and the AI doesn't have a specific target light or value, it MUST fail and ask for clarification.
- Ambiguity is a system risk.

## 🚫 4. No Trust in Editor Success
The system will **never** assume a "Success" message from Unity means the operation is finished.
- Only independent verification via `inspect_object` or `get_hierarchy` constitutes proof.
- If the AI cannot *prove* the change happened through data, it must assume it failed.

## 🚫 5. No Persistent Bridge State
The bridge is a **Stateless Translator**. 
- It does not "remember" the scene between sessions beyond what is persisted in `vibe_registry.json`.
- It does not maintain a private "shadow scene" map.

---
**Copyright (C) 2026 B-A-M-N**
