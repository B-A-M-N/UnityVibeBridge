# Security Policy

UnityVibeBridge is a **Governed Creation Kernel**. We take state integrity and code safety extremely seriously.

## Supported Versions
Only the latest version of the Kernel (currently v1.1) is supported for security updates.

## Reporting a Vulnerability
If you discover a security vulnerability (e.g., a way to bypass the Kernel Guard or the Security Gate), please **do not open a public issue.**

Instead, please send a detailed report to the Author.

### What to include:
- A description of the vulnerability.
- A proof-of-concept (PoC) script if possible.
- The version of the Kernel and the Unity Editor version you are using.

We will acknowledge your report within 48 hours and work with you to resolve the issue before a public disclosure.

## Security Mandates
All contributors and AI agents must adhere to the rules in `AI_ENGINEERING_CONSTRAINTS.md`, which include:
- No dynamic code execution inside Unity (`Reflection`, `eval`).
- Mandatory Transaction wrapping.
- Zero-Trust "Read-Before-Write" verification.
- Strictly Localhost binding.
