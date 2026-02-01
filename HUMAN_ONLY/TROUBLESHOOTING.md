# 🎨 Artist's Troubleshooting & Recovery Guide

This guide is for humans. If you are an artist using UnityVibeBridge and things seem "broken" or the AI is acting strangely, follow these steps.

---

## 🆘 The "I'm Stuck" Quick Fixes

### 1. The Bridge is Unresponsive
- **Symptoms**: AI says "Timeout" or "Connection Refused".
- **Fix**: Check the top-right of your Unity Editor. If there are **Red Compilation Errors**, the Bridge cannot run. Fix the script errors or delete the offending scripts, and the Bridge will restart automatically.
- **Manual Kick**: In Unity, go to `Window -> Vibe Toolkit`. Click **"Restart Kernel"**.

### 2. The AI deleted something it shouldn't have
- **Fix**: Press `Ctrl + Z` (Undo) in Unity. The Bridge is designed so that **One AI Action = One Undo Step**. You can undo anything the AI did just like a manual edit.

### 3. The "Veto" Lock
- **Symptoms**: AI says "VETOED" and won't do anything.
- **Fix**: You (the human) likely pressed the Emergency Stop or the AI triggered a safety violation. To re-enable, go to the Vibe Toolkit window and click **"RE-ARM"**.

---

## 🛠️ Common Error Messages

| What the AI says | What it actually means | What you should do |
| :--- | :--- | :--- |
| "Guard Block: Compiling" | You are currently saving scripts or Unity is thinking. | Wait 5 seconds for the progress bar in the bottom right to finish. |
| "Guard Block: PlayMode" | You are in Play Mode (The 'Play' button is active). | Exit Play Mode. The Bridge only works in Edit Mode for safety. |
| "Hierarchy Drift" | You moved or renamed something while the AI was thinking. | Tell the AI: "Refresh your hierarchy and try again." |
| "Transaction Failed" | A technical error happened during the change. | Check the Unity Console for a red error and share it with the developer. |

---

## 🔄 Manual Recovery Protocol

If the AI has made a mess of your project and Undo isn't working:

1.  **Close the AI Server**: Stop the MCP server (Python).
2.  **Check 'metadata/vibe_registry.json'**: This file contains the names and roles of important objects. If you manually renamed a "MainBody", you might need to update this file or tell the AI the new name.
3.  **The "Nuclear" Option**: Delete the `Assets/VibeBridge` folder and re-import it from the package. This resets the Kernel logic but **will not** touch your 3D models or textures.

---
**Remember: You are the Director. The AI is the Rigger. If the Rigger is confused, take the mouse back.**
