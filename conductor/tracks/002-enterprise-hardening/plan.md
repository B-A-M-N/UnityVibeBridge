# Implementation Plan: Enterprise Hardening

## Phase 1: API Intelligence
- [x] **Task 1.1:** Create `VibeTool_APIDump.cs` payload to export available method signatures to `metadata/unity_api_map.json`. (In Progress - Waiting for Compile)
- [ ] **Task 1.2:** Implement an AI "Lookup" mechanism to read this map during planning.

## Phase 2: C# Pre-Flight Validation
- [x] **Task 2.1:** Integrate a basic C# syntax auditor into `scripts/security_gate.py` (checking for `{}` balance and `;` presence).
- [ ] **Task 2.2:** (Optional/Stretch) Integrate a lightweight C# tokenizer for more advanced validation.

## Phase 3: JSON Schema & Integrity
- [ ] **Task 3.1:** Define JSON Schemas for core tool calls (`world/spawn`, `object/set-value`).
- [ ] **Task 3.2:** Update `mcp-server/ipc/airlock.py` to enforce these schemas.

## Phase 4: Final Verification
- [ ] **Task 4.1:** Perform a "Fault Injection" test (intentionally send broken code) to verify the gate catches it.

## Phase 5: Automated Integrity Testing (The "Pytest-Blender" Equivalent)
- [ ] **Task 5.1:** Implement `VibeTool_TestRunner.cs` to wrap Unity's `EditorSceneManager` and `TestRunnerApi`.
- [ ] **Task 5.2:** Add `system/run-tests` tool to the MCP server.
- [ ] **Task 5.3:** Create a baseline "Kernel Integrity" test suite in Unity to verify core bridge stability after updates.
