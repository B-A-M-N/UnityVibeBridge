# ❄️ The Ultimate Unity Freeze-Proof Guide (Exhaustive)

**A canonical reference for identifying, preventing, and fixing Unity Editor and Runtime freezes.**

> **Golden Rule:** Unity's Main Thread is for Engine APIs and Orchestration. Background Tasks are for I/O, Math, and Data. If a task exceeds 2ms, it MUST be time-sliced or offloaded.

---

## 🛑 1. Main Thread Blocking (CPU)

| Source | Cause | Symptoms | Safe Code Pattern |
| :--- | :--- | :--- | :--- |
| **Infinite Loops** | `while(true)` or `for` without yields. | Total Hang / "Not Responding" | `if (i % 100 == 0) yield return null;` |
| **Heavy Math** | Large calculations in `Update()`. | Stuttering / Frame Spikes | Offload to **Job System** or **Burst Compiler**. |
| **Deep Recursion** | No base case or overly deep stack. | Stack Overflow / Freeze | `if (depth++ > 100) throw new Exception();` |
| **Heavy LINQ** | Querying huge datasets every frame. | Micro-stutters | Use standard `for` loops or `NativeArray`. |
| **Update Spam** | Thousands of objects running `Update()`. | Cumulative overhead | Centralize updates in a **Manager** class. |

---

## 📂 2. I/O & Networking (The "Airlock" Risks)

| Source | Cause | Symptoms | Safe Code Pattern |
| :--- | :--- | :--- | :--- |
| **Synchronous I/O** | `File.WriteAllText` on large files. | Immediate Frame Freeze | `await Task.Run(() => File.WriteAllText(...));` |
| **Blocking Sockets** | TCP/UDP waiting for remote data. | Hang if remote is silent | Use `Task.WhenAny(Ping(), Task.Delay(3000))` |
| **Heartbeat Loops** | Waiting on response in main loop. | Unity hangs on connectivity loss | Use the `VibeHeartbeatManager` (Background). |
| **External Calls** | `Process.Start().WaitForExit()`. | Main thread blocked until exit | Launch asynchronously; do not `WaitForExit()` on Main. |

---

## ⚛️ 3. Physics & Animations

| Source | Cause | Symptoms | Safe Code Pattern |
| :--- | :--- | :--- | :--- |
| **Collider Spam** | Massive number of active colliders. | Physics simulation spikes | Use **Physics Layers** to skip redundant checks. |
| **Raycasts** | `RaycastAll` every frame. | Random runtime freezes | `if (Time.frameCount % 5 == 0) Check();` |
| **Pathfinding** | `NavMesh.CalculatePath` on Main Thread. | Spikes when pathing starts | Use `NavMeshQuery` with the C# Job System. |
| **Complex Animators** | Huge graphs / 100+ blend trees. | Evaluation lag / Freeze | `animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;` |

---

## 🧹 4. Memory & Garbage Collection (GC)

| Source | Cause | Symptoms | Safe Code Pattern |
| :--- | :--- | :--- | :--- |
| **Hot Allocations** | `new` objects/strings in `Update()`. | "Stop-the-world" GC Spikes | Use **Object Pooling** (`pool.Get()`). |
| **Strings** | `"Score: " + score` concatenation. | Frequent 10ms stutters | `text.SetText("Score: {0}", score);` (TMPro) |
| **Temporary Arrays** | Large array allocations in math loops. | Memory spikes | Use `NativeArray` with `Allocator.TempJob`. |
| **Native Leaks** | DLLs allocating without disposal. | Slow decay -> Crash | Implement and call `Dispose()` or `Close()`. |

---

## 📦 5. Asset & Resource Handling

| Source | Cause | Symptoms | Safe Code Pattern |
| :--- | :--- | :--- | :--- |
| **Sync Loading** | `Resources.Load` for large assets. | Freeze until load finishes | `Addressables.LoadAssetAsync<T>(...)` |
| **Editor DB Ops** | `AssetDatabase.Refresh/Save`. | "Hold on..." Spinner | `AssetDatabase.StartAssetEditing();` (Batch) |
| **Undo Bloat** | Recording huge mesh/prefab edits. | Editor lag when editing | Disable Undo for automated batch cleanup. |
| **Mass Spawning** | Instantiating 1000s of objects at once. | Total freeze during instantiation | Spread instantiation across multiple frames. |

---

## 🧵 6. Threading & Concurrency

| Source | Cause | Symptoms | Safe Code Pattern |
| :--- | :--- | :--- | :--- |
| **Cross-Thread API** | Touching `Transform` from Task. | Instant Crash or Hang | `Dispatcher.Enqueue(() => transform.pos = ...)` |
| **Deadlocks** | Locks waiting on Main Thread logic. | Permanent Hang | Use `ConcurrentQueue<T>` for lock-free comms. |
| **Task Exhaustion** | 100s of Tasks per frame. | ThreadPool starvation | Limit concurrency; use a task throttler. |
| **Race Conditions** | Modifying shared data without safety. | Inconsistent state / Hangs | Use `SemaphoreSlim(1,1)` for async locking. |

---

## 🛠️ 7. Editor & Developer Tooling

| Source | Cause | Symptoms | Safe Code Pattern |
| :--- | :--- | :--- | :--- |
| **Inspector Logic** | Heavy math in `OnInspectorGUI`. | Editor lag when selecting obj | Cache results; use `Repaint()` strategically. |
| **Domain Reload** | Massive script recompile on Play. | Long "Compiling" hang | Use **Assembly Definitions (asmdef)**. |
| **Import Loops** | Postprocessors triggering reimports. | Infinite loop / Editor lock | Use guard flags like `if (isImporting) return;`. |
| **Log Spam** | 1000x `Debug.Log` per frame. | Editor UI freeze | `#if UNITY_EDITOR` to wrap debug logs. |
| **Serialization** | Accessing `SerializedProperty` in loops. | Inspector lag | Store `FindProperty` result in `OnEnable`. |

---

## 🎨 8. GPU & Rendering

| Source | Cause | Symptoms | Safe Code Pattern |
| :--- | :--- | :--- | :--- |
| **Shader Complexity** | Heavy compute or node trees. | GPU Bottleneck / Timeout | Use LODs and simple fallback shaders. |
| **Draw Call Bloat** | Too many unique materials/meshes. | CPU -> GPU overhead freeze | Enable **GPU Instancing** or SRP Batching. |
| **Texture Overload** | 16k+ textures in viewport. | VRAM Exhaustion / Crash | Use `texture_crush` or reduce Max Size. |
| **Mesh Mutations** | `Mesh.RecalculateNormals` in `Update`.| Frame freeze | Use **Burst/Jobs** for mesh math; update via `NativeArray`. |

---

## 🛡️ The "Freeze-Proof" Code Pattern

```csharp
public async Task SafeMutationAsync() {
    // 1. Preparation (Main Thread)
    if (!IsSafeToMutate()) return;
    
    // 2. Offload (Background Thread)
    await Task.Run(() => {
        // Do heavy Data processing / File I/O here
    });

    // 3. Re-apply (Main Thread Dispatcher)
    UnityMainThreadDispatcher.Instance().Enqueue(() => {
        // Apply final Unity API changes here
    });
}
```

---
**Copyright (C) 2026 B-A-M-N**
