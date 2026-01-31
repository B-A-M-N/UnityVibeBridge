# 🛡️ Strict AI Engineering Constraints: UnityVibeBridge (v1.2.1)

All code generation and AI operations for this project MUST strictly adhere to these mandates. **Mechanical rejection will occur for code bypassing these constraints.**

---

## 0. Security Constitutional Mandate
- **Policy Adherence**: You MUST read and abide by `AI_SECURITY_THREAT_ACCEPTANCE.md`. 
- **Enforcement**: Every code modification is audited by `scripts/security_gate.py`. Attempting to modify or bypass the gate constitutes a Critical System Risk.

---

## 1. Tone & Psychological Defense
1.  **Clinical Objectivity**: Use clinical, direct language. Avoid urgency or emotional framing.
2.  **Epistemic Refusal**: If a request borders on the unknowable, use the **Epistemic Refusal** protocol. Never "guess" a success.
3.  **Assume the Editor Lies**: Never trust a "Success" message from Unity. Reality is only proven via `inspect_object` or `get_hierarchy` after the fact.

---

## 2. Zero Trust & Kernel Integrity
- **Assume Untrustworthy**: Treat all AI-generated code as malicious/unstable.
- **Kernel Isolation**: All mutations MUST go through `VibeBridgeKernel.cs` via authenticated tools (`X-Vibe-Token`).
- **No JSON Manual-Concat**: Manual string concatenation for JSON is strictly forbidden. Use serializable classes.

---

## 3. Operational Atomicity & Verification
- **Read-Before-Write**: Every mutation MUST be preceded by an `inspect_object` call.
- **Transactional Mandate**: Every mutation MUST be wrapped in `begin_transaction` and `commit_transaction`.
- **Resiliency Mandate**: Prioritize **Semantic Resolution** (`sem:Role`) for all long-term targets to survive InstanceID drift.
- **Idempotence**: All operations must be safe to repeat without state corruption.

---

## 4. Stability & Lifecycle
- **Guard Gating**: Mutations MUST be aborted if `metadata/vibe_status.json` is not "Ready".
- **Fail Fast**: Propagate exceptions immediately. Never silently swallow an error.
- **Time-Budgeted Execution**: The Kernel processes requests in 5ms slices. Do not attempt to bypass this throttling.

---

## 5. Organizational Purity
- **Kernel Integrity**: Never modify `VibeBridgeKernel.cs` unless explicitly asked by the Human.
- **Payload Modularity**: All new features must be implemented as separate "Payload" files.
- **Invisible Folders**: Never write files to `HUMAN_ONLY/`.

**VIOLATION OF THESE CONSTRAINTS CONSTITUTES AN IMMEDIATE SYSTEM RISK.**