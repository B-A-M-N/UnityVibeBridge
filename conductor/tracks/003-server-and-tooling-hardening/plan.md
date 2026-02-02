# Implementation Plan: Server and Tooling Hardening (v11.1 - "Autonomous Grade")

## Phase 1: Deep Diagnostics & Invisible Friction
- [x] **Task 0.1:** ENV CHECK: Add dependency verification to `server.py` to ensure all hardening tools (psutil, etc) are installed.
- [x] **Task 1.1:** Audit `VibeBridgeKernel.Core.cs` server thread logic. Replace silent `catch {}` with internal error buffering and "Port Busy" reporting.
- [x] **Task 1.2:** Enhance `ProcessHttpContext` to return C# Stack Traces and structured validation errors.
- [x] **Task 1.3:** Implement a "Main Thread Watchdog" that reports if a tool execution exceeded the 5ms budget.
- [x] **Task 1.4:** Implement "Zombie Port" detection: Bridge should write a diagnostic file if Port 8085 is held.
- [x] **Task 1.5:** CORRELATION: Add `state_hash` and `monotonic_tick` to screenshot responses.
- [x] **Task 1.6:** VISION HARDENING: Implement automated resolution downscaling for screenshots exceeding 4MB.

## Phase 2: Roslyn, Assembly & Emergency Fixes
- [x] **Task 2.1:** Implement `scripts/roslyn_auditor.py` with C# Version Detection.
- [x] **Task 2.2:** Update Python mutation tools (`mutate_script`) to block mutation if the Roslyn auditor fails.
- [x] **Task 2.3:** EMERGENCY REPAIR: Implement Python-side `Editor.log` parser to diagnose red errors when the Bridge is offline.
- [x] **Task 2.4:** SELF-HEALING: Python tool to automatically detect and offer removal of broken C# scripts via direct log audit.
- [x] **Task 2.5:** SDK AUTO-DETECTION: Implement `#if` gated code blocks and a `system/sdk_report` tool to detect VRChat/Poiyomi presence.

## Phase 3: Lifecycle & Communication Hardening
- [x] **Task 3.1:** Harden "Safety Guards": Block mutations during `AssetDatabase.IsImportingAssets` or `EditorApplication.isPlayingOrWillChangePlaymode`.
- [x] **Task 3.2:** Implement "Scene Affinity": Bind the `vibe_registry` to the Active Scene GUID.
- [x] **Task 3.3:** implement "Registry Sanitization": Prune dead UUIDs and verify InstanceID health.
- [x] **Task 3.4:** UNIFORM AIRLOCK: Normalize HTTP and File-System response wrappers.
- [x] **Task 3.5:** AUTOMATED DISPATCH: Replace manual switch-blocks in `StandardPayload.cs` with attribute-based reflection.

## Phase 4: Unified Error & Performance
- [x] **Task 4.1:** Update `get_telemetry_errors` to aggregate Unity Logs (including Warnings), Server Exceptions, and Roslyn failures.
- [x] **Task 4.2:** RECOMPILE AVOIDANCE: Expand `VibeISA` to allow more complex logic via Reflection/SerializedObjects.
- [x] **Task 4.3:** Automate `AssetDatabase.Refresh()` after all AI disk mutations.
- [x] **Task 4.4:** UNDO HARDENING: Implement Logical Undo Grouping.
- [x] **Task 4.5:** MODAL GUARD: Detect if a Unity Modal Window is blocking the heartbeat.
- [x] **Task 4.6:** PAGINATION: Add chunking to `get_hierarchy` to prevent timeouts.
- [x] **Task 4.7:** Update `docs/DEV_NOTES.md` with "The Complete Friction Audit."
