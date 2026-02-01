# 🛡️ UNITY + BLENDER LIFECYCLE HARDENING

**Objective:** Prevent secondary failure classes including IO races, TOCTOU errors, event storms, and cross-tool desync.

---

## 1. FILE SYSTEM & IO RACE CONDITIONS (BOTH)
1. Never read files currently being written.
2. Require a **write-complete marker** (`.done`, checksum, or rename).
3. Use the atomic pattern: `write → flush → fsync → rename`.
4. Reject partial files silently.

---

## 2. TIME-OF-CHECK vs TIME-OF-USE (TOCTOU)
1. Re-validate UUID → object mapping **immediately before use**.
2. If missing: Re-scan, Rebind, and Abort if still missing.
3. Never trust cached validation from a previous editor tick.

---

## 3. EVENT STORM CONTROL (EDITOR SAFETY)
1. All handlers must be re-entrancy guarded and rate-limited.
2. Maintain an `event_depth_counter`.
3. Abort if depth > safe threshold (e.g., 3 levels deep).
4. Defer heavy work to the next editor tick or use a work queue.

---

## 4. VERSIONED STATE MIGRATION
1. Every UUID registry must include a `schema_version`.
2. On load: Detect version, Migrate explicitly, and Log migration results.
3. Never auto-upgrade without persistent backup.

---

## 5. DUPLICATION SEMANTICS
### Unity
1. Detect `OnValidate` + duplicated GUID.
2. Regenerate UUID for the scene instance only (never the prefab source).
### Blender
1. Detect duplicated datablock with same UUID.
2. Regenerate UUID for the duplicated datablock immediately.

---

## 6. DELETION HANDLING
1. Intercept deletion events.
2. Mark UUID as `tombstoned` in the registry.
3. Notify external systems of the tombstone.
4. Archive the mapping — never erase it immediately.

---

## 7. MULTI-SCENE / MULTI-FILE COHERENCE
1. Namespace UUIDs by `project_id + uuid`.
2. Detect and flag cross-scene UUID collisions.
3. Block merges or imports that introduce identity ambiguity.

---

## 8. PLAY MODE / SIMULATION CONTAMINATION (UNITY)
1. Snapshot pre-play state.
2. On play exit: Restore editor-only data and discard runtime-generated UUIDs.
3. Never persist Play Mode changes to the registry unless explicitly authorized by the user.

---

## 9. BLENDER PYTHON HANDLE INVALIDATION
1. Treat all `bpy` object references as ephemeral (invalid after undo/reload).
2. Store authoritative `uuid → datablock name/type` maps.
3. Re-resolve the actual `bpy` handle on every access.

---

## 10. CRASH LOOP BREAKER
1. Maintain a `last_boot_success` persistent flag.
2. If multiple crashes occur on startup:
   * Start in Safe Mode.
   * Disable all AI automation.
   * Require manual user re-enablement.

---

## 11. USER INTENT PROTECTION
1. Detect manual user edits (selection changes, field focus).
2. Pause AI automation during direct user manipulation.
3. Resume automation only after a period of user idle state.

---

## 12. EXTERNAL SYSTEM TRUST MODEL
1. Treat all external data as **untrusted input**.
2. Strict UUID validation on every incoming packet.
3. Reject unknown UUIDs, schema mismatches, or partial updates.

---

## 13. LOGGING AS A RECOVERY TOOL
1. Logs must be structured (JSONL), timestamped, and persisted.
2. Every auto-fix must be logged with its "Before" and "After" state.
3. Logs must be replayable to reconstruct state if the registry is corrupted.

---

## 14. AI FAILURE BOUNDARY
1. Maintain a persistent failure counter.
2. On hitting the threshold:
   * STOP all automation immediately.
   * Preserve current state (do not attempt "one last fix").
   * Notify the user with a diagnostic report.

---

## FINAL META-RULE
> **Identity is not enough — lifecycle discipline is what keeps systems alive.**
