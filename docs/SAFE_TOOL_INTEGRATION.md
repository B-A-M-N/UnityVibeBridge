# 🛡️ Safe Tool Integration Checklist & Workflow

> **Context**: This document expands on the mandates in `AI_ENGINEERING_CONSTRAINTS.md` with specific workflows to prevent Unity crashes and bridge instability.

---

## 1. 🏛️ Separate Source of Truth
*   **Canonical Folder**: `Assets/VibeBridge/` is the **ONLY** place edits occur.
*   **No Dual-Editing**: Never modify `unity-package/` and `Assets/` simultaneously.
*   **Symlinks**: Use symbolic links if you need to reference the package from `Packages/` or other locations, ensuring a single physical file.

## 2. 🧱 Compile-Safe Insertion Protocol
*   **Incrementalism**: 
    *   Add **ONE** function or class at a time.
    *   **STOP** and wait for compilation.
    *   **VERIFY** `engine/error/state` before proceeding.
*   **Syntax Pre-Flight**:
    *   **Strictly Forbidden**: Writing blindly to `Assets/`.
    *   **Required**: Write to a temporary file (e.g., `tmp/NewTool.cs`).
    *   **Verify**: Check braces `{}`, semicolons `;`, and imports.
    *   **Deploy**: Move to `Assets/VibeBridge/` only after verification.
*   **Isolation (ASMDEF)**:
    *   New experimental tools should be placed in a separate assembly definition (`.asmdef`) if possible.
    *   This ensures a syntax error in the tool doesn't break the core `VibeBridge` kernel.

## 3. 🚫 Zero Direct Disk Mutation
*   **Shell Ban**: Never use `sed`, `cp`, `echo`, or `rm` on files inside `Assets/` while Unity is running.
*   **Unity's Watcher**: Unity detects file system events instantly. Partial writes will trigger errors.
*   **Protocol**:
    1.  Prepare file in `tmp/`.
    2.  Validate content.
    3.  Atomic move/copy to `Assets/`.
    4.  **Wait 20s**.

## 4. 🦺 Safety Nets & Rollbacks
*   **Git Safety**:
    *   Run `git --git-dir=.git_safety --work-tree=. commit -m "Pre-Tool: [Tool Name]"` before starting.
*   **Console Guard**:
    *   If **ANY** red error appears in the Unity Console (via `engine/error/state`), **STOP IMMEDIATELY**.
    *   Do not "try to fix it forward". Revert to the last safe state.

## 5. 🧘 Workflow Discipline
*   **Single Threaded**: One tool implementation at a time.
*   **Context Check**: grep for `class [ToolName]` before creating it to avoid duplicate definitions.
*   **Reference Check**: Ensure all referenced classes (`VibeBridgeServer`, `VibeDevHelper`) are available and public.

---

## 🛑 Emergency Recovery
If the bridge is crashed (compilation errors prevent tools from working):
1.  **Stop Unity** (if necessary/possible).
2.  **Delete** the offending file via shell.
3.  **Wait** for Unity to recompile the valid kernel.
4.  **Check** `engine/error/state`.
