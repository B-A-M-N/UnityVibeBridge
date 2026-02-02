#!/usr/bin/env python3
import hashlib
import os
import json
import sys

# UnityVibeBridge: Integrity Map Generator
# Generates a mapping between Source Code (AST) and Compiled Binary (DLL).

def calculate_folder_hash(directory):
    sha256 = hashlib.sha256()
    for root, dirs, files in os.walk(directory):
        for names in sorted(files):
            if names.endswith(".cs"):
                filepath = os.path.join(root, names)
                with open(filepath, 'rb') as f:
                    while True:
                        data = f.read(65536)
                        if not data:
                            break
                        sha256.update(data)
    return sha256.hexdigest()

def calculate_file_hash(filepath):
    if not os.path.exists(filepath):
        return None
    sha256 = hashlib.sha256()
    with open(filepath, 'rb') as f:
        while True:
            data = f.read(65536)
            if not data:
                break
            sha256.update(data)
    return sha256.hexdigest()

def generate_map(project_root):
    scripts_dir = os.path.join(project_root, "unity-package/Scripts")
    
    # Heuristic for finding the compiled DLL
    # In development, it's in Library/ScriptAssemblies/
    # In release, it would be in unity-package/Plugins/
    dll_candidates = [
        "/home/bamn/ALCOM/Projects/BAMN-EXTO/Library/ScriptAssemblies/UnityVibeBridge.Kernel.dll",
        os.path.join(project_root, "unity-package/Plugins/UnityVibeBridge.Kernel.dll")
    ]
    
    source_hash = calculate_folder_hash(scripts_dir)
    dll_hash = None
    active_dll_path = None

    for candidate in dll_candidates:
        hash_val = calculate_file_hash(candidate)
        if hash_val:
            dll_hash = hash_val
            active_dll_path = candidate
            break

    if not dll_hash:
        print(f"Warning: No compiled DLL found. Run Unity first.")
        # We can still save the source hash
    
    integrity_data = {
        "source_hash": source_hash,
        "binary_hash": dll_hash,
        "path": active_dll_path,
        "timestamp": os.path.getmtime(active_dll_path) if active_dll_path else None
    }

    output_path = os.path.join(project_root, "metadata/vibe_integrity.json")
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    
    with open(output_path, 'w') as f:
        json.dump(integrity_data, f, indent=4)
    
    print(f"Integrity Map Generated:")
    print(f"  Source: {source_hash}")
    print(f"  Binary: {dll_hash}")
    print(f"  Saved to: {output_path}")

if __name__ == "__main__":
    root = os.getcwd()
    generate_map(root)
