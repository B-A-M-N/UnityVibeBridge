#!/usr/bin/env python3
import json
import os
import sys

# UnityVibeBridge: Smart Dependency Automator (v2.1)
# Detects environment (VRChat vs Generic) via both manifest and physical folder check.

def setup_dependencies(project_path):
    packages_dir = os.path.join(project_path, "Packages")
    manifest_path = os.path.join(packages_dir, "manifest.json")
    
    if not os.path.exists(manifest_path):
        print(f"Error: manifest.json not found at {manifest_path}")
        return False

    try:
        with open(manifest_path, 'r') as f:
            manifest = json.load(f)
        
        if "dependencies" not in manifest:
            manifest["dependencies"] = {}
        if "scopedRegistries" not in manifest:
            manifest["scopedRegistries"] = []

        # 1. Add OpenUPM Registry
        openupm = {
            "name": "package.openupm.com",
            "url": "https://package.openupm.com",
            "scopes": ["com.cysharp"]
        }
        if not any(r["url"] == openupm["url"] for r in manifest["scopedRegistries"]):
            manifest["scopedRegistries"].append(openupm)

        # 2. PROBE: Is this a VRChat project?
        # Check manifest keys
        has_vrchat_manifest = any(k.startswith("com.vrchat") for k in manifest["dependencies"].keys())
        
        # Check physical folders (for embedded/VCC packages)
        has_vrchat_physical = False
        if os.path.exists(packages_dir):
            has_vrchat_physical = any(d.startswith("com.vrchat") for d in os.listdir(packages_dir))

        has_vrchat = has_vrchat_manifest or has_vrchat_physical
        changed = False

        if has_vrchat:
            print(f">> VRChat SDK detected (Manifest:{has_vrchat_manifest}, Physical:{has_vrchat_physical}).")
            print(">> Using bundled UniTask to prevent GUID conflicts.")
            # FORCE REMOVE the conflicting package if it accidentally got in there
            if "com.cysharp.unitask" in manifest["dependencies"]:
                print(">> Removing conflicting com.cysharp.unitask...")
                del manifest["dependencies"]["com.cysharp.unitask"]
                changed = True
        else:
            print(">> Generic Unity project detected. Installing standard UniTask.")
            if manifest["dependencies"].get("com.cysharp.unitask") != "2.5.3":
                manifest["dependencies"]["com.cysharp.unitask"] = "2.5.3"
                changed = True

        # 3. MemoryPack and EditorCoroutines (Always needed)
        if manifest["dependencies"].get("com.cysharp.memorypack") != "1.10.0":
            manifest["dependencies"]["com.cysharp.memorypack"] = "1.10.0"
            changed = True
        
        if "com.unity.editorcoroutines" not in manifest["dependencies"]:
            manifest["dependencies"]["com.unity.editorcoroutines"] = "1.0.0"
            changed = True

        if changed:
            with open(manifest_path, 'w') as f:
                json.dump(manifest, f, indent=2)
            print("\nSuccess! manifest.json updated and optimized.")
        else:
            print("\nNo changes needed. Environment already correctly configured.")
        
        return True
    except Exception as e:
        print(f"Failed to update manifest: {e}")
        return False

if __name__ == "__main__":
    path = sys.argv[1] if len(sys.argv) > 1 else os.getcwd()
    if setup_dependencies(path):
        sys.exit(0)
    else:
        sys.exit(1)
