# 🛠️ UnityVibeBridge: Technical Specifications

This document defines the mathematical and technical invariants of the Bridge for developers and technical artists.

---

## 📐 Coordinate Systems & Units

### 1. Unity Standard
The Bridge operates in **Unity World Space** by default. 
- **Scale**: 1 Unit = 1 Meter.
- **Handedness**: Left-Handed, Y-Up.
- **Rotation**: Euler angles in degrees (Serialized as Vector3).

### 2. Blender Translation (VibeSync)
When syncing with Blender (via VibeSync), the following translation is applied:
- **Scale Conversion**: Blender (1.0) -> Unity (1.0).
- **Axis Remapping**: 
  - Blender X -> Unity X
  - Blender Y -> Unity -Z
  - Blender Z -> Unity Y
- **Euler Order**: Unity (ZXY) vs Blender (XYZ). The Kernel handles the quaternion conversion during `set-value` if a `Rotation` type is detected.

---

## 📋 JSON Schema: Core Types

### Vector3 / Color
Strings passed to the AI must follow the comma-separated format:
- `pos`: `"x,y,z"` (e.g., `"0.5,1.0,0.0"`)
- `color`: `"r,g,b,a"` (0.0 to 1.0 range, e.g., `"1,0,0,1"` for opaque red)

### Semantic Identifiers (`sem:`)
The Registry Payload uses a prefix-based resolution system:
- `sem:MainBody` -> Resolves to InstanceID via `metadata/vibe_registry.json`.
- `id:12345` -> Raw InstanceID (Volatile, use only for session-lifetime).
- `/root/path/obj` -> Hierarchy path (Fallback).

---

## ⚡ Kernel Performance Budget
- **Main Thread Slice**: 5ms.
- **Request Cycle**: 
  1. AI -> Python (AST Check: ~2ms)
  2. Python -> Unity (Airlock: ~1-5ms)
  3. Unity -> Execute (5ms max)
  4. Response -> AI.
- **Total Latency**: Target < 50ms for synchronous tools.

---

## 🧪 Testing Guidelines

### Unit Tests (Editor)
- Located in `unity-package/Tests/Editor/`.
- Run via the **Unity Test Runner** (`Window -> General -> Test Runner`).
- Mandatory for any changes to the `VibeBridgeKernel` core serialization logic.

### Integration Tests (Python)
- Located in `security_tests/`.
- Run using `pytest`.
- Use these to verify that `security_gate.py` correctly blocks new attack vectors.

---
**Copyright (C) 2026 B-A-M-N**
