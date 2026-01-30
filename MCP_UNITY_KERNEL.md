# 🛡️ MCP Unity Kernel: The Control Plane Specification

**Philosophy:** Unity is a hostile, non-deterministic environment. This Kernel transforms it into a verified, transactional Control Plane.
**Status:** DEFINITIVE TOOL INVENTORY.

---

## 🟢 1. THE KERNEL (Irreducible Safety Core)
*These must be active for the system to be considered "Safe".*

### 🔒 Execution Safety
*   **Atomic Operation Wrapper**: `transaction/begin` -> `try` -> `transaction/commit` / `transaction/abort`. (**ACTIVE**)
*   **Guard Module**: Blocks mutations during Compilation, Play Mode, or Asset Import. (**ACTIVE**)
*   **Lifecycle Manager**: Explicitly handles Domain Reloads to prevent "zombie" processes. (**ACTIVE**)

### 🛑 Corruption Defense
*   **Compiling Trap**: Rejects all commands if `EditorApplication.isCompiling` is true. (**ACTIVE**)
*   **Sanity Module**: Enforces hardware safety railings (Light intensity < 10k, Range < 1k, Texture < 4k) to prevent GPU/VRAM bombs. (**ACTIVE**)
*   **Telemetry Hook**: Captures all Console Errors and Exceptions into a machine-readable buffer. (**ACTIVE**)
*   **Heartbeat Monitor**: Writes `metadata/vibe_health.json` every 1s to prove liveness. (**ACTIVE**)

### 🧹 State Hygiene
*   **Audit Logging**: Every mutation is logged to `logs/vibe_audit.jsonl` with timestamp and capability. (**ACTIVE**)
*   **Session Nonce**: Verifies that the server and Unity are talking about the same session (prevents cross-talk). (**ACTIVE**)

### 📤 Pipeline Gate
*   **Forensic Audit**: Requires `structural` capability for scene/project-level changes. (**ACTIVE**)
*   **Export Contract Enforcer**: Hard-fails FBX export if Scale != 1, rotation is non-zero, or missing scripts exist. (**ACTIVE**)

---

## 🟡 2. THE SURVIVAL SUIT (Workflow Protection)
*Tools that prevent "lost hours".*

*   **Manual/Auto Checkpoints**: `snapshot/create` (Metadata only). (**ACTIVE**)
*   **Material Snapshots**: Backs up material assignments before swapping shaders/textures. (**ACTIVE**)
*   **Optimization Fork**: Creates a safe `_QuestGenerated` duplicate before destructive optimization. (**ACTIVE**)

---

## ⚪ 3. THE INSPECTOR (Forensics & validation)
*Tools that detect "silent" errors.*

*   **Hierarchy Dump**: `hierarchy` (Full scene graph analysis). (**ACTIVE**)
*   **Component Inspector**: `inspect` (Reflection-based component dump). (**ACTIVE**)
*   **Mesh Info**: `unity/mesh-info` (Vertex/Triangle count verification). (**ACTIVE**)
*   **Error Telemetry**: `telemetry/get_errors` (Remote console access). (**ACTIVE**)

---

## 🔴 4. THE INVENTORY (Full Tool Surface)

### Core Safety
*   Incremental Snapshot Manager (`snapshot/create`)
*   Undo Stack Extender (`transaction/begin`)
*   Crash Fingerprint Logger (`telemetry`)

### Context Control
*   Guard Status Check (`guard/status`)
*   Wait for Compilation (`guard/await_compilation`)
*   System Focus (`system/focus`)

### Resource Integrity
*   Texture Crusher (`opt/texture/crush`)
*   Mesh Simplifier (`opt/mesh/simplify`)
*   Shader Swapper (`opt/shader/quest`)

### Viewport & Scene
*   Object Active Toggle (`object/active`)
*   Object Rename (`object/rename`)
*   World Spawn (`world/spawn`)
*   Static Flags (`world/static`)
*   Visual Point (`visual/point`)
*   Visual Line (`visual/line`)
*   Visual Clear (`visual/clear`)

### Material & Art
*   Material Slot Manager (`material/insert-slot`, `material/remove-slot`)
*   Color Sync (`material/set-color`)
*   Texture Swap (`material/set-slot-texture`)

---

## 💀 UNITY PAIN MAP vs. COUNTERMEASURES

| Pain Point | Implemented Countermeasure | Status |
| :--- | :--- | :--- |
| **Silent State Corruption** | **Telemetry + Heartbeat** | ✅ ACTIVE |
| **Undo Is Unreliable** | **Atomic Transactions** | ✅ ACTIVE |
| **Compilation Poisoning** | **Guard Module (Compiling Trap)** | ✅ ACTIVE |
| **Domain Reload Chaos** | **Lifecycle Manager** | ✅ ACTIVE |
| **Play Mode Corruption** | **Guard Module (Play Mode Block)** | ✅ ACTIVE |
| **Material Mismatch** | **Material Snapshots** | ✅ ACTIVE |
| **Optimization Destructiveness** | **Opt Fork (Safe Duplicate)** | ✅ ACTIVE |
| **Server Desync** | **Session Nonce + Status File** | ✅ ACTIVE |
| **Blind Server** | **Telemetry/Get Errors** | ✅ ACTIVE |

---

## 🚀 Implementation Priority

1.  **Asset Database Integrity** (Meta file validation, GUID collision check). 🚧 PHASE 2
2.  **Prefab Drift Detector** (Verify overrides). 🚧 PHASE 2
3.  **Build Pipeline Gates** (Pre-build validation). 🚧 PHASE 2
