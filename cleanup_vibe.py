import os
import sys

def cleanup_vibe(project_path):
    print(f"Cleaning VibeBridge duplicates in: {project_path}")
    
    # The list of canonical script names that make up the bridge
    core_scripts = [
        "VibeBridgeKernel.cs",
        "VibeBridge_StandardPayload.cs", 
        "VibeBridge_MaterialPayload.cs",
        "VibeBridge_RegistryPayload.cs",
        "VibeBridge_VisionPayload.cs",
        "VibeBridge_VRChatPayload.cs",
        "VibeBridge_AuditingPayload.cs",
        "VibeBridge_ExtrasPayload.cs",
        "VibeBridge_Stubs.cs",
        "VibeDevHelper.cs"
    ]

    deleted_count = 0
    kept_files = {}

    # Pass 1: Scan and identify all locations
    print("  [SCAN] Mapping file locations...")
    file_map = {} # Filename -> [List of full paths]
    
    for root, dirs, files in os.walk(project_path):
        for file in files:
            if file in core_scripts:
                if file not in file_map: file_map[file] = []
                file_map[file].append(os.path.join(root, file))

    # Pass 2: Decide who lives and who dies
    print("  [DECIDE] Enforcing 'Single Source of Truth'...")
    for filename, paths in file_map.items():
        if len(paths) > 1:
            print(f"    Conflict found for {filename}:")
            # Preference Rule: 
            # 1. Prefer path containing 'unity-package/Scripts'
            # 2. Prefer path containing 'VibeBridge/'
            # 3. Else, pick the longest path (deepest nesting usually implies structure)
            
            best_path = None
            for p in paths:
                if "unity-package/Scripts" in p:
                    best_path = p
                    break
            
            if not best_path:
                for p in paths:
                    if "Assets/VibeBridge" in p:
                        best_path = p
                        break
            
            if not best_path:
                # Fallback: Just pick the first one
                best_path = paths[0]

            print(f"      [KEEP] {best_path}")
            
            # Delete the losers
            for p in paths:
                if p != best_path:
                    print(f"      [DELETE] {p}")
                    try:
                        os.remove(p)
                        if os.path.exists(p + ".meta"):
                            os.remove(p + ".meta")
                        deleted_count += 1
                    except Exception as e:
                        print(f"        Error deleting: {e}")
        else:
             print(f"    OK: {filename} (Unique)")

    print(f"Cleanup complete. Deleted {deleted_count} duplicate files.")
    print("Please re-focus Unity to trigger a recompile.")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python3 cleanup_vibe.py <path_to_unity_project_assets>")
    else:
        cleanup_vibe(sys.argv[1])