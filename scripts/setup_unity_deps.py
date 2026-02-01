#!/usr/bin/env python3
import json
import os
import sys

# UnityVibeBridge: Dependency Automator
# Automates the injection of UniTask, MemoryPack, and EditorCoroutines.

DEPS = {
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
    "com.cysharp.memorypack": "https://github.com/Cysharp/MemoryPack.git?path=src/MemoryPack.Unity/Assets/Plugins/MemoryPack",
    "com.unity.editorcoroutines": "1.0.0"
}

def setup_dependencies(project_path):
    manifest_path = os.path.join(project_path, "Packages", "manifest.json")
    
    if not os.path.exists(manifest_path):
        print(f"Error: manifest.json not found at {manifest_path}")
        print("Make sure you provide the root path of your Unity project.")
        return False

    try:
        with open(manifest_path, 'r') as f:
            manifest = json.load(f)
        
        if "dependencies" not in manifest:
            manifest["dependencies"] = {}

        changed = False
        for pkg, version in DEPS.items():
            if pkg not in manifest["dependencies"]:
                print(f"Adding {pkg} -> {version}")
                manifest["dependencies"][pkg] = version
                changed = True
            else:
                print(f"Package {pkg} already exists. Skipping.")

        if changed:
            with open(manifest_path, 'w') as f:
                json.dump(manifest, f, indent=2)
            print("\nSuccess! manifest.json updated.")
            print("Return to Unity and wait for the 'Importing' progress bar to finish.")
        else:
            print("\nNo changes needed. All dependencies are already present.")
        
        return True
    except Exception as e:
        print(f"Failed to update manifest: {e}")
        return False

if __name__ == "__main__":
    path = sys.argv[1] if len(sys.argv) > 1 else os.getcwd()
    print(f"Targeting Unity Project: {path}")
    if setup_dependencies(path):
        sys.exit(0)
    else:
        sys.exit(1)
