# Contributing to UnityVibeBridge

## Safety-First Architecture
This project is built on the principle of **Mechanistic Vibe Coding**. All contributors must respect the structural safety constraints that prevent AI agents from corrupting user data.

## Mutation Guidelines
Any new mutation tool must follow the **Atomic Guard** pattern:

1.  **Call `EnsureSafeMutationContext`**: Always verify that Unity is not compiling, not in play mode, and not in read-only mode at the very start of the operation.
2.  **Verify Provenance**: If a tool deletes or moves objects, it must check for the `VibeBridgeAgentTag` component to ensure it isn't touching user-owned assets.
3.  **Support Undo**: Every write operation MUST use `Undo.RecordObject`, `Undo.AddComponent`, or `Undo.DestroyObjectImmediate`.
4.  **No Silent Failures**: Propagate all exceptions as `ErrorResponse` with `HardStop` severity.

## Capability Discovery
If you add a new type of manipulation (e.g., modifying Animator Controllers), you MUST update `GetObjectCapabilities` to reflect the risks and requirements of that operation.

## Code Review Standard
We reject any code that:
*   Uses `System.Reflection` to bypass private member access.
*   Caches `InstanceID` values across domain reloads.
*   Modifies `.meta` files or `ProjectSettings` programmatically.
*   Introduces global static state that doesn't survive a domain reload.
