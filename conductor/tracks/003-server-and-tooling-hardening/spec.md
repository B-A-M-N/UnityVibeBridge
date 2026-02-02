# Specification: Server and Tooling Hardening (v2.0)

## 1. Objective
Iron out development friction by hardening the Unity server, automating Roslyn-based syntax and semantic checks, and eliminating "Invisible Stalls" that block developer flow.

## 2. Problem Statement (Friction Audit)
-   **Manual Roslyn Checks**: AI-generated code often lacks the `partial` keyword or uses unsafe static patterns, leading to Unity compilation hangs.
-   **Silent Server Failure**: The HTTP listener contains silent `catch` blocks that swallow runtime exceptions, making the bridge appear "frozen" without a clear error.
-   **Generic Error Messages**: Exceptions in the Unity server return basic error strings to the agent without stack traces, preventing rapid self-correction.
-   **Asset Database Contention**: Mutations triggered during background imports cause 30-second Editor hangs.

## 3. Core Components
1.  **Automated Roslyn Auditor (Python-side)**: Mandatory syntax/safety check before writing C# payloads to disk.
2.  **Verbose Exception Reporting**: A development-mode feature that serializes full C# stack traces into the bridge JSON response.
3.  **Lifecycle Heartbeat**: Enhanced detection of 'Compiling', 'Importing', and 'Updating' states to prevent "Import Prison" hangs.
4.  **Self-Healing Session**: Automated verification of registry state-hash consistency after every Unity recompile.

## 4. Success Criteria

-   Zero "Syntax Error" compiler hangs during AI-driven Payload generation.

-   Full C# stack traces are visible to the agent for debugging internal bridge errors.

-   Bridge mechanically blocks mutations during background imports with a "TRY_AGAIN_LATER" status.



## 5. Friction-Reduction Constraints (Mandatory)

1.  **Tail-Only Log Scanning**: The Python `Editor.log` parser MUST use `seek()` to read only the final 1000 lines to prevent CPU/IO spikes.

2.  **Soft Registry Sanitization**: Dead UUIDs MUST be marked as `MISSING` and preserved for 3 session heartbeats before deletion to handle temporary prefab unloads.

3.  **Gated Asset Refresh**: `AssetDatabase.Refresh()` MUST only be triggered if `.cs` or `.meta` files were modified. It MUST be suppressed for JSON metadata updates.

4.  **Atomic Undo Blocks**: All C# `Undo.BeginUndoGroup` calls MUST be wrapped in `try-finally` blocks to ensure the group is closed even if the tool execution fails.
