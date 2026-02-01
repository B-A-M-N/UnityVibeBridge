# 📋 UNITY AI PROCEDURAL WORKFLOW

**Goal:** Prevent asset breakage, identity drift, crashes, and desync across reloads, recompiles, and external systems.

---

## PHASE 0 — BOOT / ATTACH

1. **Detect Unity State**
   * Confirm:
     * Editor vs Play Mode
     * Domain Reload enabled/disabled
     * Assembly reload status
   * Abort if Unity is compiling.

2. **Load Persistent Identity Registry**
   * Load UUID registry from disk.
   * Validate registry integrity (checksum or schema).
   * If registry missing → create empty registry.

3. **Index Existing Assets**
   * Scan:
     * ScriptableObjects
     * Scene objects
     * Prefabs (asset + instances)
   * Build temporary map:
     ```
     AssetGUID → UUID
     UUID → AssetGUID
     ```

---

## PHASE 1 — UUID ENFORCEMENT

4. **Validate UUID Presence**
   * For each asset/object:
     * If UUID exists → continue
     * If UUID missing → generate UUID
     * Immediately serialize UUID to asset

5. **Detect UUID Collisions**
   * If duplicate UUID found:
     * Freeze write operations
     * Log collision
     * Regenerate UUID only for newest asset
     * Persist fix

6. **Persist UUID Registry**
   * Write updated registry to disk.
   * Flush file system buffer.

---

## PHASE 2 — PRE-OPERATION SNAPSHOT (CRITICAL)
**Before any action that may cause reload, recompile, or sync**

7. **Snapshot Identity State**
   * Serialize:
     * UUID ↔ Asset GUID
     * UUID ↔ Scene instance
     * UUID ↔ External references
   * Timestamp snapshot.

8. **Verify Snapshot Completeness**
   * If snapshot incomplete → abort operation.

---

## PHASE 3 — OPERATION EXECUTION

9. **Execute Requested Operation**
   * Examples:
     * Asset sync
     * External import
     * Scene modification
     * Tool call
   * Use **UUID only** for lookups.
   * Never rely on:
     * Index
     * Order
     * Name
     * Path alone

10. **Monitor Unity Stability**
    * Detect:
      * Editor freezes
      * Repeated exceptions
      * Infinite callbacks
    * If instability detected → halt operation safely.

---

## PHASE 4 — POST-RELOAD RECONCILIATION
**Triggered after:**
* Domain reload
* Script recompile
* Scene reload
* Server reconnect

### FLOW 11 — HYBRID IDENTITY CACHING
1. UUIDs are the **authoritative** identity of all project objects.
2. `InstanceID`s are **volatile session caches**.
3. Every operation must start by resolving the `UUID` to the current `InstanceID`.
4. If an `InstanceID` fails (e.g. after reload), re-resolve from the `vibe_registry.json`.

12. **Reattach External Systems**
    * Reconnect server/tools.
    * Rebind references by UUID.
    * Ignore asset order or count changes.

13. **Validate Rehydration**
    * Confirm:
      * No missing UUIDs
      * No duplicate UUIDs
      * No orphaned external refs

---

## PHASE 5 — SELF-HEALING

14. **Repair Missing UUIDs**
    * Assign UUID
    * Serialize immediately
    * Update registry

15. **Repair Broken References**
    * Attempt rebind via UUID
    * If ambiguous → mark for user resolution
    * Never guess silently

16. **Clean Orphaned Data**
    * Remove unused mappings
    * Archive (do not delete) uncertain data

---

## PHASE 6 — VALIDATION PASS

17. **Run Consistency Check**
    * UUID uniqueness
    * Registry ↔ Unity parity
    * External mappings valid

18. **Log Results**
    * Summary:
      * Assets scanned
      * Fixes applied
      * Warnings
    * Persist log.

---

## PHASE 7 — SAFE IDLE

19. **Enter Watch Mode**
    * Listen for:
      * Asset changes
      * Scene loads
      * Server events
    * Throttle checks to avoid Editor load.

20. **Fail Gracefully**
    * On repeated failure:
      * Disable AI automation
      * Preserve data
      * Notify user

---

## PHASE 8 — ADVANCED SAFETY FLOWS

### FLOW 12 — BOOTSTRAP HARDENING
1. Detect last shutdown state.
2. If previous shutdown was abnormal:
   * Enter **Safe Mode**.
   * Disable automation layers.
3. Require explicit user opt-in to re-enable automation.
4. Log boot mode.

### FLOW 13 — CAPABILITY NEGOTIATION
1. Query editor capabilities (domain reload on/off, enter play options).
2. Adjust automation behavior accordingly.
3. Abort if required capabilities are unavailable.

### FLOW 14 — TRANSACTION BOUNDARIES
1. Wrap every multi-step operation in a transaction: `begin → mutate → validate → commit | rollback`.
2. On failure: Roll back to snapshot; never partially apply changes.

### FLOW 15 — WRITE SERIALIZATION DISCIPLINE
1. Serialize writes through a **single writer queue**.
2. Enforce one write at a time and deterministic order.
3. Reject concurrent writes.

### FLOW 16 — SCHEMA VERSION ENFORCEMENT
1. Read schema version for UUID registry and external payloads.
2. If mismatch: Block execution and run migration or abort.

### FLOW 17 — HOT-RELOAD AWARENESS
1. Detect Unity assembly reload.
2. Immediately: Drop caches, Re-index, and Rebind by UUID.

### FLOW 18 — DUPLICATION INTERCEPT
1. Detect `OnValidate` + duplicated GUID.
2. Apply duplication rules: Preserve source UUID, Regenerate duplicate UUID for scene instance.
3. Persist immediately.

### FLOW 19 — DELETE INTERCEPT
1. Detect deletion.
2. Tombstone UUID, notify external systems, and archive mapping.

### FLOW 20 — EXTERNAL SYNC CONTRACT
1. Require handshake (project_id, schema_version, UUID_namespace).
2. Reject sync if mismatch; lock state during sync and unlock after validation.

### FLOW 21 — ORDER-INDEPENDENT RESOLUTION
1. Never assume arrival order.
2. Buffer external events and resolve only when dependencies are satisfied.

### FLOW 22 — PLAY / SIMULATION ISOLATION
1. Snapshot editor state before Play.
2. Block persistence during Play unless explicit.
3. Restore editor state on exit.

### FLOW 23 — MEMORY & HANDLE DISCIPLINE
1. Treat all editor object handles as volatile.
2. Resolve fresh by UUID on every use; garbage-collect stale handles.

### FLOW 24 — PERFORMANCE GOVERNORS
1. Set hard limits (Max scan frequency, Max repair ops per tick).
2. Yield execution if limits exceeded.

### FLOW 25 — ERROR ESCALATION LADDER
1. On first failure → retry safely.
2. On repeated failure → degrade feature set.
3. On persistent failure → disable automation.

### FLOW 26 — USER OVERRIDE DETECTION
1. Detect manual edits (selection changes, field focus).
2. Pause automation immediately; resume only when editor is idle.

### FLOW 27 — AUDIT & TRACEABILITY
1. Emit structured logs (Timestamp, UUID, Operation, Result).
2. Persist logs and make them replayable.

### FLOW 28 — SAFE IDLE STATE
1. Enter watch mode.
2. Throttle listeners and await explicit trigger.

---

## PHASE 9 — LOG-AS-STATE (LOG CONSULTATION GATING)

### FLOW L1 — LOG INDEXING (BOOT TIME)
1. On attach / boot:
   * Load last **N** log entries (configurable).
2. Build in-memory index:
   ```
   UUID → last_known_state
   UUID → last_error
   operation → last_outcome
   ```
3. Validate log schema.
4. Abort automation if logs are unreadable.

### FLOW L2 — LOG CONSULTATION GATE (PRE-ACTION)
**Before every operation that mutates state:**
1. Query log index for:
   * Prior failures on same UUID
   * Recent crash flags
   * Incomplete transactions
2. If found:
   * Adjust behavior (retry, degrade, block)
3. Log the consultation itself.
> **No consultation → no mutation**

### FLOW L3 — LOG-DRIVEN DECISION OVERRIDE
1. If log indicates:
   * Repeated failure
   * Partial commit
   * Identity ambiguity
2. Then:
   * Override AI’s current plan.
   * Enter safe or degraded mode.
   * Require explicit user confirmation to proceed.
> **Logs now outrank inference.**

### FLOW L4 — TRANSACTION-BOUND LOGGING
1. Every transaction must log: `BEGIN → STEP(S) → COMMIT | ROLLBACK`.
2. On boot: Scan for any `BEGIN` without `COMMIT`.
3. Auto-rollback or quarantine state.

### FLOW L5 — LOG-AS-MEMORY PROMOTION
1. Promote qualifying log entries into **operational memory**:
   * Known bad assets
   * Known unstable operations
   * Crash-triggering sequences
2. Treat these as **hard constraints**, not suggestions.

### FLOW L6 — FAILURE ESCALATION VIA LOGS
1. Maintain failure counters in logs.
2. On threshold:
   * Disable specific features (not entire system).
3. Log escalation decision.

### FLOW L7 — POST-ACTION LOG VALIDATION
1. After operation: Re-read the log entry just written.
2. Confirm it exists and matches expected state.
3. If not → treat as failed operation.

### FLOW L8 — CROSS-PROCESS LOG COHERENCE
1. When multiple processes exist (Unity + Blender + Server):
   * Sync log sequence numbers.
   * Detect divergence.
2. Block actions on divergence.

---

# HARD RULES (ENFORCED AT EVERY STEP)

* **UUID always wins**
* **Logs > Inference > Action**
* **No state-changing operation may execute without explicit log consultation and acknowledgment.**
* **No Direct Disk Mutation (sed/cp/mv) of assets while Unity is running.**
* **Never trust Unity ordering**
* **Never assume state survives reload**
* **Never auto-repair identity ambiguity**
* **Never block the main thread**
* **Never silently fix critical data**
* **Automation never fights the editor or the user**
* **Automation knows when to stop**
