# Implementation Plan: Project Hardening

> **Status:** ✅ Done

## Phase 1: Discovery & Audit
- [x] **Task 1.1:** Recursive file scan completed. Found path fragmentation and hardcoded `/home/bamn/ALCOM/...` strings.
- [x] **Task 1.2:** Security Audit of `mcp-server` and `security_gate.py` completed. Identified path traversal risk in `_is_path_safe` and dummy script redundancy.
- [x] **Task 1.3:** Identified fragile scripts: `unity_agent.py`, root `security_gate.py`, and `airlock.py` (port mismatch).

## Phase 2: Documentation Refinement
- [x] **Task 2.1:** Update `README.md` with clear textual architecture description.
- [x] **Task 2.2:** Verify consistency of distributed constraints (check for contradictions in `AI_ENGINEERING_CONSTRAINTS.md` vs local `.gemini` prompts).
- [x] **Task 2.3:** Perform deep audit of system-level instructions and propose specific hardening tweaks (Focus: Path Resolution & State Verification).

## Phase 3: Hardening Implementation
- [x] **Task 3.1:** Implement "Pre-Flight" checks in identified fragile scripts.
- [x] **Task 3.2:** Harden `security_gate.py` based on audit findings.
- [x] **Task 3.3:** Implement "Auto-Snapshot" wrapper for critical mutations (automating `.git_safety` commits).
- [x] **Task 3.4:** Implement Log Rotation (keep last N lines) for `logs/` directory.
