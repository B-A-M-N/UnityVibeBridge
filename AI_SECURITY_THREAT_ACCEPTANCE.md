# UnityVibeBridge: Threat Acceptance Document (TAD)

This document defines the residual risks and the boundary of the UnityVibeBridge security model. By operating this bridge, the user acknowledges that absolute safety in the Unity Editor is an asymptote, and the following threats are explicitly accepted.

## 1. Scope of Protection
The bridge implements **Capability-Scoped Isolation** and **Semantic Lifecycle Scanning**. It is designed to prevent:
*   Immediate RCE via shell or reflection.
*   Lifecycle hijacking via `[InitializeOnLoad]` or static constructors.
*   Structural project subversion via `.asmdef` or `ProjectSettings` manipulation.
*   Unauthorized data exfiltration via sockets.

## 2. Residual Risks (Accepted Threats)

### 2.1. The "Boiled Frog" Composition
While the bridge tracks **Subsystem Entropy**, it cannot mathematically prove that a series of 100 "safe" edits (e.g., default value tweaks, field renames) will not compose into a malicious behavior over time.
*   **Mitigation**: Mandatory human review for entropy spikes and periodic workspace finalization.
*   **Risk**: Low-to-Medium (Long-term semantic drift).

### 2.2. Covert Channel Exfiltration
The bridge uses entropy detection on identifiers, but an adversary can still exfiltrate small amounts of data through "legitimate" means:
*   Animation curve values, texture dimensions, or the specific ordering of GameObjects in the hierarchy.
*   **Risk**: Low (Low-bandwidth exfiltration).

### 2.3. UI-Driven "Trapdoor" Execution
Unity executes code when a user interacts with the Inspector (e.g., `OnInspectorGUI`, `PropertyDrawers`). The bridge cannot scan the *entire* existing project for pre-installed trapdoors that trigger when the AI modifies a specific "safe" field.
*   **Risk**: Medium (Dependency on project-wide integrity).

### 2.4. Semantic Shadowing
The bridge blocks known dangerous keywords, but it cannot prevent "Logic Inversion"—where a function named `Validate()` is modified to perform a side effect that is technically "safe" (like writing to a whitelisted log file) but semantically malicious.
*   **Risk**: Low-to-Medium (Requires sophisticated prompt engineering).

### 2.5. Human-in-the-Loop Failure
The bridge relies on the Human to verify "Structural Changes." If the human clicks "Approve" without understanding the delta, the technical protections are bypassed.
*   **Risk**: High (Human Factor).

## 3. Boundary of Responsibility
The UnityVibeBridge is **NOT** responsible for:
*   Compromise of the local machine via non-Unity vectors.
*   Zero-day vulnerabilities within the Unity Engine or VRCSDK itself.
*   Malicious actions taken by the local user or other non-bridge processes.

## 4. Final Declaration
This bridge is a **policy-enforced, capability-scoped, transactionally isolated augmentation layer**. It is not a sandbox. It is a suit of armor for a system never meant to be armored.

**Proceed with the understanding that Security is a Process, not a Product.**
