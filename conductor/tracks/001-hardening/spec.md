# Specification: Project Hardening, Documentation & Security Review

## 1. Objective
Conduct a comprehensive review of the `UnityVibeBridge` project to identify and implement improvements in:
1.  **Documentation:** Clarity, architectural flow, and "Zero Trust" operational guides.
2.  **Flow & Logic:** Streamlining the "Iron Box" and "Triple-Lock" protocols with **automated safety commits**.
3.  **Invariance:** Strengthening state checks and self-healing mechanisms without disrupting distributed constraint prompts.
4.  **Security:** Hardening the codebase against attack vectors (local and theoretical).

## 2. Core Constraints & Principles
*   **Context Efficiency:** Documentation and code changes must be concise. Avoid "wall of text" logs.
*   **Zero New Attack Vectors:** Every change must decrease, not increase, the attack surface. No new external dependencies without strict vetting.
*   **Pain-Point Mitigation:** Changes must be reliable. If a "hardening" feature causes legitimate workflow failures (false positives), it is a failure.
*   **Atomic Changes:** Implementation will happen in small, reversible steps.
*   **Preservation of Distributed Prompts:** Do not consolidate strategic AI instructions (e.g., in `.gemini/` or extensions) if their location serves a specific invariance purpose.

## 3. Proposed Audit Areas

### A. Documentation & Knowledge Graph
*   Review `README.md` and `AI_CONTEXT.md` for outdated info.
*   **Action:** Update textual architectural descriptions (Mermaid diagrams optional/secondary).
*   **Action:** Verify consistency across distributed constraint files (ensure no contradictions).

### B. Invariant & State Hardening
*   Review `check_errors.py` and `security_gate.py`.
*   Propose "Pre-Flight" checks for critical scripts.
*   **New Feature:** Implement "Auto-Commit" hooks for critical state changes (using `.git_safety`).

### C. Security Hardening
*   Audit `mcp-server` for potential injection risks.
*   Verify permissions on generated scripts in `vibe_queue`.
*   **Log Management:** Implement a safe "Log Rotation" strategy (keep tail) to prevent context flooding, rather than aggressive trimming.

## 4. Success Criteria
*   A cleaner, more navigable documentation set.
*   Reduced ambiguity in AI operational prompts.
*   At least 3 concrete "Hardening" patches applied to core logic.
*   Critical tasks automatically trigger safety snapshots.
