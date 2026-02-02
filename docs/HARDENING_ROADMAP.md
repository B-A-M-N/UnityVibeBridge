# 🛣️ UnityVibeBridge: Hardening Roadmap

This document tracks critical architectural transitions required before the system moves from "Research Prototype" to "Production Grade."

## 1. Transition to Virtual Rooting (Final Phase)
**Objective**: Decouple the AI's internal path representation from the physical host filesystem to solve the "Disparate Root" problem.

### The Strategy: Multi-Zone Access Control
Near application completion, the system MUST transition to a **Virtual Path Resolver**.

*   **Zone A (Bridge Core)**: `Bridge://` -> Mapped to the installation directory. Read-Only access.
*   **Zone B (Active Project)**: `Project://` -> Mapped to the currently mounted Unity project. Read-Write access.
*   **Zone C (Peer Projects)**: `Peer:[Name]://` -> Mapped to registered workspace neighbors (Blender, VibeSync). Read-Only access.

### Security Mandates for Virtual Rooting:
1.  **Strict Path Resolution**: Use `os.path.realpath()` and verify the result is a subdirectory of the intended Zone root before execution.
2.  **Symlink Gating**: Block any path resolution that traverses a symbolic link pointing outside of the defined Zones.
3.  **Log Anonymization**: All telemetry sent to the AI must use Virtual Paths; Physical Paths must only exist in the encrypted local forensic logs.

## 2. Workspace Perimeter Expansion
*   **Peer Discovery**: Implement a central workspace registry to allow cross-kernel polling (Unity <-> Blender).
*   **Mutation Authority**: Ensure only one Zone has "Write" permission at any given time to prevent cross-project accidental deletion.

---
**Status**: Pending final stabilization of the Async Kernel.
