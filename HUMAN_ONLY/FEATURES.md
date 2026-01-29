# 🌉 UnityVibeBridge: Full Feature Manifest (v3.6-Stable)

This document serves as the authoritative list of all functional abilities, security protocols, and planned expansions for the UnityVibeBridge system.

---

## 🛡️ 1. Security & Orchestration (The "Iron Box")
The foundational layer that ensures AI agents can interact with Unity without compromising project integrity or system security.

### Current Implementation:
*   **IP Whitelisting**: The bridge only accepts traffic from `127.0.0.1` (localhost) to prevent external network access.
*   **Session-Based Tagging**: Every object created by the AI is marked with a unique `_sessionNonce`. 
*   **Destruction Safety**: A hard-coded check (`SafeDestroy`) prevents the AI from deleting any object it did not personally create during the current session.
*   **Transactional Undo/Redo**: Mutations are wrapped in named Unity `Undo` groups, allowing for a single-command `Rollback` of complex AI operations.
*   **Dry-Run Verification**: A pre-flight check that blocks intent if it targets forbidden project directories (`ProjectSettings`, `Library`, `.meta`).
*   **AST Security Gate**: A Python-side gate that scans any code the AI attempts to write for dangerous patterns (`os.system`, `subprocess`, obfuscation).

---

## 🎨 2. Avatar Synchronization (The "Body Sync")
Real-time logic that bridges the gap between the VRChat Animator and complex Material properties.

### Current Implementation:
*   **5-Group Independent Color Sync**:
    *   **Accent All**: Master wheel for primary avatar accents.
    *   **Secondary**: Independent control for specific clothing items (e.g., Skeleton Hoodie).
    *   **Hair Master**: Simultaneous control of all hair assets.
    *   **Hair Slot 1 / Slot 2**: Independent control for multi-layered or dual-tone hair setups.
*   **Manual Overrides**: Instant toggles for `CollarBlack` and `WarmerBlack` to force materials to a specific state regardless of wheel position.
*   **Blackout Prevention**: Automatic normalization logic that ensures Pitch and Saturation default to 1.0 on boot, preventing the avatar from appearing "invisible" or blacked out.

---

## 🚀 3. Optimization & Quest Conversion (The "Muscle")
Tools designed to handle the heavy lifting of preparing an avatar for both PC and Quest (MQ) platforms.

### Current Implementation:
*   **Recursive Texture Crushing**: Automatically finds all unique textures in a hierarchy and downscales them to a user-defined limit (e.g., 512px) using `AssetImporter`.
*   **Quest Shader Batching**: Automated swapping of high-end shaders (like Poiyomi) to `VRChat/Mobile/Toon Lit` across all material slots.
*   **Non-Destructive Variants**: Creates "Quest Clones" of objects to allow side-by-side comparison and separate optimization paths.
*   **Performance Audits**:
    *   **Mesh Audit**: Scans Static and Skinned meshes for triangle counts and vertex density.
    *   **Collider Audit**: Identifies performance-heavy `MeshColliders`.
    *   **VRAM Profiler**: Calculates the real-time memory footprint of all textures in a specific scope.
    *   **Contact Audit**: Tracks VRChat Contact Receivers/Senders against performance budgets.

---

## 🌍 4. World Creation & Editing (The "Environment")
Tools for populating and preparing Unity scenes for VRChat worlds.

### Current Implementation:
*   **Prefab Spawning**: Instance-based placement of assets at specific XYZ coordinates with automatic AI provenance tagging.
*   **Static Flag Management**: Batch setting of Unity Static flags for occlusion and lightmapping.
*   **Animator Safety Check**: Hard-coded logic that blocks setting "Static" flags on any object with an Animator or Animation component (preventing "Ghost Shadow" bugs).
*   **Lighting Environment Audit**: Real-time reporting of Skybox settings, Fog parameters, and Ambient modes.

### Planned Expansions (Roadmap):
*   **Terrain API**: AI-mediated heightmap and alpha-map (texture) manipulation.
*   **Navmesh/Occlusion Bakes**: Programmatic triggering of Unity’s baking engines.
*   **Probe Factory**: Automated density-based placement of Reflection and Light probes.
*   **Bake-Readiness Guard**: A final audit that queries `Lightmapping.isRunning` to prevent mutation-conflicts during long bakes.

---

## 🛠️ 5. Technical Artist Toolkit
Lower-level utilities for high-precision project maintenance.

### Current Implementation:
*   **Semantic Hierarchy Inspection**: Allows the AI to "see" the scene tree and component data without requiring hardcoded file paths.
*   **Material Decoupling**: Automatically duplicates and reassigns materials to ensure edits to one object don't "bleed" into others.
*   **Bone Resolution**: Maps humanoid rig bones (Head, Spine, etc.) regardless of non-standard naming conventions.
*   **Blendshape Control**: Direct weight manipulation of Skinned Mesh blendshapes by name.
*   **Visual Feedback**: Integrated screenshot engine for "Human-in-the-Loop" verification of AI actions.

---

## ⚖️ 6. Legal Protection & Liability
Crucial safeguards for the Author and User in an AI-driven environment.

### Licensing Model:
*   **Base**: AGPLv3 (Network-aware copyleft to prevent proprietary wrapping).
*   **The "Work or Pay" Clause**: Commercial entities must either pay a license fee or contribute significantly to project maintenance.
*   **Discretionary Waiver**: The Author maintains the right to waive all fees for individuals or indie creators at their sole discretion.
*   **Contributor Agreement (CLA)**: Ensures all community contributions can be legally included in the commercial handshake versions.

### Liability Clauses:
*   **AI Non-Determinism Warning**: User acknowledges that AI interpretation can vary and produces non-deterministic results.
*   **Mandatory Backup Requirement**: User assumes all risk; external backups are required before any AI mutation.
*   **Limitation of Liability**: All-caps disclaimer excluding all direct or consequential damages resulting from asset corruption or project loss.

---

## ⚠️ Pitfalls & "How Not to Overcorrect"
*   **Pitfall**: The "Infinity Bake" (Baking while moving objects). **Fix**: Implement the `Bake-Readiness Guard`.
*   **Pitfall**: Static Batching Conflicts (Setting static on Udon/Pickups). **Fix**: Component-aware static blocking.
*   **Safety Overcorrection**: Avoid making the "Iron Box" so small that the AI can't be creative. **Solution**: Rely on the **Transaction/Rollback** system instead of hard-banning creative operations.