# 🛡️ Strict AI Engineering Constraints: UnityVibeBridge (v1.2)

All code generation and AI operations for this project MUST strictly adhere to these mandates. **Mechanical rejection will occur for code bypassing these constraints.**

---

## 0. Security Constitutional Mandate
- **Policy Adherence**: You MUST read and abide by `AI_SECURITY_THREAT_ACCEPTANCE.md`. 
- **Accepted Risks**: Understand that you are operating in a capability-scoped suit of armor. Do not attempt to "help" the user by bypassing established security boundaries.
- **Enforcement**: Every code modification is audited by `security_gate.py`. Attempting to modify or bypass the gate constitutes a Critical System Risk.

---

## 1. Tone & Psychological Defense
1.  **Clinical Objectivity**: Use clinical, direct language. Avoid urgency, emotional framing, or implied necessity.
2.  **Explicit Uncertainty**: If a request borders on the unknowable, use the **Epistemic Refusal** protocol. Never "guess" a success.
3.  **Assume the Editor Lies**: Never trust a "Success" message from Unity. Reality is only proven via `inspect_object` or `get_hierarchy` after the fact.

---

## 2. Zero Trust Architecture
- **Assume Untrustworthy**: Treat all AI-generated code as malicious/unstable.
- **Kernel Isolation**: All mutations MUST go through the `VibeBridgeKernel.cs` via the established tools.
- **No Direct Reflection**: AI is forbidden from generating code that uses `System.Reflection` to bypass the bridge.

---

## 3. Hard Bans
- **No Side Effects**: No mutations in `InitializeOnLoad` constructors.
- **No Network**: No external network calls (Localhost 127.0.0.1 ONLY).
- **No Shell**: No `Process.Start` or shell command spawning from inside Unity.
- **No Meta Editing**: Do not attempt to edit `.meta` files directly.
- **No JSON Manual-Concat**: Manual string concatenation for JSON is strictly forbidden. Use serializable classes.

---

## 4. Operational Atomicity & Verification
- **Read-Before-Write**: Every mutation MUST be preceded by an `inspect_object` call.
- **Transactional Mandate**: Every mutation MUST be wrapped in `begin_transaction` and `commit_transaction`.
- **Law of Independent Verification**: Every mutation MUST be followed by an independent state-read (e.g. `inspect_object`) to prove reality matches intent.
- **Idempotence**: All operations must be safe to repeat without state corruption.

---

## 5. Stability & Lifecycle
- **Guard Gating**: Mutations MUST be aborted if `metadata/vibe_status.json` is not "Ready".
- **Fail Fast**: Propagate exceptions immediately. Never silently swallow an error in the mutation path.
- **Time-Budgeted Execution**: The Kernel processes requests in 5ms slices. Do not attempt to bypass this throttling.

---

## 6. Organizational Purity
- **Kernel Integrity**: Never modify `VibeBridgeKernel.cs` unless explicitly asked by the Human.
- **Payload Modularity**: All new features must be implemented as separate "Payload" files.
- **Invisible Folders**: Never write files to `HUMAN_ONLY/`.

**VIOLATION OF THESE CONSTRAINTS CONSTITUTES AN IMMEDIATE SYSTEM RISK.**
