# 💣 Unity Development Landmines: The VibeBridge Ledger

This document is a persistent, append-only record of Unity Editor behaviors, patterns, and API quirks that have caused instability, corruption, or "Development Friction" during the creation of UnityVibeBridge.

**DO NOT DELETE ANY CONTENT FROM THIS FILE.**

---

## 🌀 1. The Infinite Recompile / Domain Reload Loops
- **Direct Disk Mutation**: Modifying `.cs` or `.meta` files via shell commands (`sed`, `echo`, `rm`) while the Unity Editor is active can trigger race conditions where the `AssetDatabase` attempts to import while the OS is still locking the file. Result: Unity hangs or enters an infinite "Importing Assets" loop.
  - *Mitigation*: All file mutations MUST go through the `VibeBridgeServer` API or be performed when Unity is hard-closed.
- **InitializeOnLoad Side Effects**: Placing logic that triggers asset dirtying or `AssetDatabase.SaveAssets()` inside a class marked `[InitializeOnLoad]` or a static constructor. This triggers a domain reload, which triggers the constructor, which triggers a reload.
  - *Mitigation*: Use `EditorApplication.delayCall` or a gated heartbeat check to ensure the editor is fully stable before running lifecycle-heavy logic.

## 🛑 2. Unsafe APIs During Reload / Recompile
- **AssetDatabase Calls**: Calling `AssetDatabase.FindAssets` or `AssetDatabase.LoadAssetAtPath` while `EditorApplication.isCompiling` is true. Unity will often return `null` or empty arrays silently, leading the AI to believe assets are "missing" when they are simply locked.
  - *Mitigation*: The `Guard/Status` tool must mechanically block these calls if the Editor is not in a 'Ready' state.
- **Thread Contention**: Attempting to access Unity APIs (like `GameObject.name`) from a background thread during a domain reload. Unity's main-thread-only restriction is strictly enforced and will crash the bridge with a `UnityException`.
  - *Mitigation*: Use `UniTask` and `AsyncUtils` to marshal all engine-specific calls back to the Main Thread via `EditorApplication.update`.

## 💾 3. Serialization & Identity Surprises
- **InstanceID Volatility**: Relying on `InstanceID` across sessions. These IDs are re-assigned every time a scene is closed or a domain is reloaded.
  - *Mitigation*: **UUID is Canonical.** All internal mappings must use the `VibeIdentity` UUID system.
- **JsonUtility Limitations**: `JsonUtility` cannot serialize private fields, even if marked `[SerializeField]`, and it fails on complex nested dictionaries or generic lists without a wrapper class.
  - *Mitigation*: The project has migrated to `MemoryPack` for internal IPC and uses public serializable classes for all JSON bridge-data.
- **ScriptableObject Data Loss**: If a script recompile fails (Red Errors), Unity may temporarily lose the "Binding" between a `ScriptableObject` and its C# class. If you save the project during this state, the data inside the ScriptableObject is wiped or turned into "Missing Script" stubs.
  - *Mitigation*: Never trigger a `Save` operation if there are active compilation errors.

## 🏗️ 4. Assembly & Pathing Landmines
- **Circular Dependencies**: Moving scripts between `Editor` and `Runtime` folders can break `.asmdef` boundaries.
  - *Resolved*: All VibeBridge Editor scripts were moved to `unity-package/Scripts/Editor/` to unify them into the `UnityVibeBridge.Kernel` assembly.
- **Hidden Side Effects of .meta Files**: Manually editing `.meta` files to change GUIDs can cause Unity to "Ghost" assets—where they appear in the Project window but cannot be loaded or referenced.
  - *Rule*: Never edit `.meta` files directly via AI. Use `AssetDatabase` API calls.

---

## 🛡️ 5. The Complete Friction Audit (v11.1 Hardening)
- **Ghost Registry Entries**: Dead UUIDs in `vibe_registry.json` caused "Missing Object" loops.
  - *Solution*: Implemented `SanitizeRegistry()` which prunes dead entries on every bridge startup.
- **Silent Port Blocking**: If port 8085 was held by a zombie Unity process, the bridge failed silently.
  - *Solution*: Added `scripts/zombie_port_scanner.py` to the Python pre-flight to report port-hogging PIDs.
- **Visual Stale-Mate**: Screenshots were out-of-sync with the JSON state hash.
  - *Solution*: Added `X-Vibe-Tick` correlation headers to the vision stream.
- **The "Modal Death"**: Unity Modal Windows pause the main update loop, causing bridge timeouts.
  - *Solution*: Added `UpdateHeartbeat` monitoring that detects long main-thread stalls and reports "STALL (Modal Window?)" to telemetry.
- **Infinite Import Prison**: Mutating while Unity is background-importing assets caused hard freezes.
  - *Solution*: `IsSafeToMutate` now checks `AssetDatabase.IsImportingAssets()` and `EditorApplication.isPlayingOrWillChangePlaymode`.
- **Recompile Blindness**: Breaking the Bridge with a bad script prevented error reporting.
  - *Solution*: Implemented a Python-side `Editor.log` parser to see red errors even when the server is dead.

---
*Append new findings below this line.*
