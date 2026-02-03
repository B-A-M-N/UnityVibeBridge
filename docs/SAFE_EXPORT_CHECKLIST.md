# 🛡️ UNITY → BLENDER SAFE EXPORT CHECKLIST

Follow this checklist for **EVERY** avatar export to ensure no rig collapse ("crumpled ball") and 100% material integrity.

## 1. Unity Pre-Export (Safety First)
- [ ] **Bridge Status**: Check `status`. Ensure `vibe_status.json` is `Ready`.
- [ ] **Normalization**: Run `system/reset-transforms`. Verify the message: `Transforms reset (MAX_SAFETY_ENFORCED)`.
- [ ] **Integrity Audit**: Run `export/validate`. Confirm no bone scales are `!= 1.0`.
- [ ] **Scene Cleanup**: Remove all temporary debug objects (spheres, lines).

## 2. Triggering the Export
- [ ] **Command**: `export/fbx`
- [ ] **Targets**: Select the Avatar Root only.
- [ ] **Output**: Verify files appear in `Export_Blender/`:
    - `[AvatarName].fbx`
    - `[AvatarName]_materials.json`
    - All texture dependencies (PNG/JPG).

## 3. Blender Import
- [ ] **Import Settings**:
    - Scale: `1.0`
    - Manual Orientation: `Off`
    - Animation: `On` (if needed)
- [ ] **Rig Check**: Switch to `Viewport Display -> In Front`. Verify bones align with the mesh.
- [ ] **Safety Verification**: Confirm `Hips` position is NOT `(0,0,0)` if it wasn't originally.

## 4. Material Reassignment (Automatic)
- [ ] **Automation**: Open the Text Editor in Blender.
- [ ] **Run Script**: Open and run `vibe_import_fix.py` (found in `Export_Blender/`).
- [ ] **Result**: Verify that textures appear on the avatar in `Material Preview` mode.

---

### 🚫 THE GOLDEN RULES
1. **NEVER** use `transform.Reset()` or `localScale = Vector3.zero` on any object with an Animator.
2. **NEVER** parent bones to non-bone objects during export prep.
3. **FAIL FAST**: If `RigSafetyGate` throws a `RIG_SAFETY_VIOLATION`, **STOP**. Do not attempt to fix it manually; use `system/undo`.
