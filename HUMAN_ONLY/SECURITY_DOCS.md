# UnityVibeBridge Security & Architecture

This document outlines the security measures, architectural invariants, and safety features implemented to ensure a secure environment for AI-assisted Unity development.

## The "Iron Box" Safety Model

Security has been moved from **Instructions** (which an agent can ignore) to **Infrastructure** (which an agent cannot change). The system uses a multi-layered defense-in-depth approach.

### 1. OS-Level Isolation (The Sandbox)
The agent is designed to run within a **Docker Container** (`Dockerfile.sandbox`).
*   **Filesystem Scoping:** The agent only sees the `/workspace` directory (the project folder). It has no access to host SSH keys, documents, or system configurations.
*   **Ephemeral State:** Any malicious tools or persistent backdoors installed by a hijacked agent are wiped when the container is stopped.
*   **Non-Root Execution:** The agent runs as a restricted `sandbox` user inside the container.

### 2. The Security Gate (Static Analysis)
Every file write and shell command passes through a robust, context-aware auditor (`security_gate.py`) that uses **AST (Abstract Syntax Tree)** parsing rather than brittle regex.

*   **Python AST Audit:**
    *   **Logic over Text:** Detects malicious intent even if obfuscated (e.g., handles dynamic attribute access).
    *   **Forbidden Modules:** Hard blocks `os`, `subprocess`, `socket`, and `pty` unless used through "Safe Lane" wrappers.
    *   **Network Firewall:** Only allows network requests to `localhost` and `127.0.0.1` (Unity Bridge). All external URLs are blocked.
*   **C# Lexical Audit:**
    *   **De-Noising:** Strips all comments and string literals before scanning to prevent "obfuscation in strings" attacks.
    *   **Namespace Whitelisting:** Blocks high-risk namespaces like `System.Diagnostics` and `System.Reflection`.
*   **Shell Whitelist:**
    *   Only allows a specific list of "Safe" commands (`git`, `python`, `cargo`).
    *   Blocks pipes (`|`) and redirects (`>`) to prevent data exfiltration.

### 3. Context-Aware Invariants
The system is "Smart" to reduce approval fatigue:
*   **Safe-Path Enforcement:** File operations (`open`, `write`) are allowed silently IF the path is strictly within the project directory.
*   **Safe-Target Enforcement:** Network calls are allowed silently IF the target is a local Unity server port.

### 4. Trusted Signature System (Human-in-the-Loop)
A persistent hashing system (`trusted_signatures.json`) allows for "Risky but Safe" code to be authorized.
*   **Fingerprinting:** Generates a SHA-256 hash of approved code blocks.
*   **Zero-Trust AI:** The AI agent is **hard-blocked** from modifying the trust registry. It has no tools to "Trust" its own code and the shell auditor blocks use of the `--trust` flag by the agent.
*   **Manual Override:** Only a human user at the physical terminal can authorize a high-risk script by running `python3 security_gate.py <file> --trust`.

## Core Features

### Integrated Agent Environment
*   **Unified CLI:** Combines the `goose` Rust engine with a Python MCP server.
*   **One-Click Launch:** `start_sandbox.sh` automates the build and deployment of the secure environment.

### Unity VibeBridge API
*   **Transaction Safety:** All mutations (colors, transforms, components) are wrapped in Unity Undo transactions.
*   **Performance Auditing:** Built-in tools for polycount checks, Quest compatibility fixes, and shader auditing.
*   **Semantic Registry:** `vibe_registry.json` allows the agent to persist "Project Knowledge" (e.g., "This material is for the fingernails") across sessions.

## Security Commandments
1.  **Never mount the Docker Socket.**
2.  **Never run the sandbox with `--privileged`.**
3.  **Always verify "Trust" requests manually.**
4.  **Keep the `security_gate.py` outside of the AI's write-access if running "on the metal".**

---
*Generated on January 26, 2026, for the UnityVibeBridge Project.*
