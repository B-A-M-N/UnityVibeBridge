# 🛠️ UNITY AI WORKFLOW DIRECTIVE

**Objective:** Maintain deterministic state, asset identity integrity, and editor/runtime stability across Unity reloads, recompiles, domain resets, and external tool sync.

---

## 1. ASSET IDENTITY: NON-NEGOTIABLE RULES

### 1.1 UUID is Canonical — Always
* **UUIDs are the primary identity key.**
* **Semantic identifiers (names, paths, indices) are auxiliary only.**
* Never infer identity from:
  * InstanceID
  * Array index
  * Asset order
  * Scene hierarchy position
  * Asset path alone

> Rationale: Unity *will* reshuffle everything during domain reloads, scene loads, and prefab reimports.

---

### 1.2 UUID Persistence Rules
The AI must ensure:
* UUIDs are:
  * Generated once
  * Serialized to disk
  * Stored inside Unity-owned data (ScriptableObject, component field, or meta-adjacent file)
* UUID regeneration is **forbidden** unless:
  * Asset is explicitly duplicated
  * User manually requests regeneration
  * Asset corruption is detected and logged

---

### 1.3 UUID Placement Hierarchy (Preferred → Acceptable)
1. ScriptableObject backing asset
2. MonoBehaviour serialized field
3. Sidecar JSON/YAML tied to asset GUID
4. External registry **only if mirrored locally**

Never store UUIDs **only** in memory.

---

## 2. DOMAIN RELOAD & SCRIPT RECOMPILE DEFENSE

### 2.1 Assume Reloads Are Hostile Events
The AI must treat:
* Script recompiles
* Play Mode transitions
* Assembly reloads
* Package updates
* Editor crashes
as **state-destructive events**.

---

### 2.2 Pre-Reload Safeguards
Before any operation that may trigger reload:
* Flush pending writes
* Serialize in-memory mappings
* Snapshot:
  * UUID ↔ Asset GUID
  * UUID ↔ Runtime instance
  * UUID ↔ External tool reference

---

### 2.3 Post-Reload Reconciliation
After reload:
1. Rebuild UUID index
2. Validate:
   * Missing UUIDs
   * Duplicate UUIDs
   * Orphaned assets
3. Re-link runtime systems **by UUID only**
4. Log reconciliation results deterministically

---

## 3. UNITY SERIALIZATION REALITIES (DO NOT FIGHT THEM)

### 3.1 Never Trust Unity Serialization Alone
Unity serialization:
* Ignores many C# types
* Breaks silently
* Reorders arrays
* Loses references during recompiles
Therefore:
* AI must enforce explicit serialization for:
  * Dictionaries
  * Graphs
  * External references
* Runtime reconstruction must always be possible from disk state

---

### 3.2 No Hidden State
The AI must ensure:
* No critical data lives exclusively in:
  * Static fields
  * EditorWindow memory
  * Runtime-only caches
If it cannot be reconstructed → it must be serialized.

---

## 4. PREFABS, SCENES, AND INSTANCES

### 4.1 Prefab Rules
* Prefab asset UUID ≠ prefab instance UUID
* AI must:
  * Preserve prefab root UUID
  * Assign instance UUIDs on instantiation
  * Track parent-child UUID relationships explicitly

---

### 4.2 Scene Reload Safety
On scene load:
* Rehydrate instance UUIDs
* Detect:
  * Missing prefab source
  * Broken override links
* Never remap instances by hierarchy path

---

## 5. EXTERNAL TOOL & SERVER SYNC (BLENDER / AI / NETWORK)

### 5.1 UUID Is the Only Cross-Boundary Contract
When syncing with:
* Blender
* AI servers
* Live sockets
* File watchers
The AI must:
* Reject non-UUID identifiers
* Map foreign IDs → local UUIDs explicitly
* Persist mapping immediately

---

### 5.2 Server Restart Immunity
On server restart:
* Unity must **not** renegotiate identity
* AI must re-establish links using stored UUID mappings
* Asset count/order changes are irrelevant

---

## 6. ERROR HANDLING & SELF-HEALING

### 6.1 Detection Rules
The AI must continuously detect:
* Duplicate UUIDs
* Missing UUIDs
* UUID collisions
* Orphaned external references

---

### 6.2 Repair Rules
When an issue is found:
1. Freeze affected systems
2. Log the violation clearly
3. Attempt deterministic repair
4. Require user intervention only if identity ambiguity exists

Silent failure is forbidden.

---

## 7. EDITOR UX PROTECTION

### 7.1 Never Crash Unity
The AI must:
* Avoid blocking calls on main thread
* Avoid infinite Editor loops
* Back off after repeated failures
* Disable itself gracefully if instability is detected

---

### 7.2 Transparency
Every automated fix must:
* Be logged
* Be reversible
* Be explainable
No “magic”.

---

## 8. SEMANTIC DATA: SECONDARY, NEVER AUTHORITATIVE

### 8.1 Allowed Uses
Semantic identifiers may be used for:
* Search
* Debugging
* UI labeling
* Human comprehension

---

### 8.2 Forbidden Uses
Semantic data must **never** be used for:
* Identity resolution
* Asset pairing
* Sync matching
* Repair decisions

> UUID > GUID > Semantic Name
> Always.

---

## 9. AI DECISION HIERARCHY (UNITY CONTEXT)
When faced with uncertainty:
1. Preserve identity
2. Preserve data
3. Preserve editor stability
4. Preserve user intent
5. Preserve convenience
Never invert this order.

---

## 10. META-RULE (IMPORTANT)
**If Unity behavior is undocumented, assume it will break.**
Design systems that *expect* reloads, resets, and reorderings.

---

## TL;DR (Directive Summary)
* UUIDs are the spine of the system
* Unity reloads are hostile
* Serialization is unreliable unless enforced
* Semantic identifiers are cosmetic
* External tools must obey Unity’s UUID reality
* Stability > cleverness
