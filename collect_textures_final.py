import json
import os
import shutil

def collect():
    # Authoritative path discovered via find
    PROJECT_ROOT = "/home/bamn/ALCOM/Projects/BAMN-EXTO"
    MANIFEST_PATH = "blender_reconstruction.json"
    EXPORT_DIR = "Export_Blender"

    if not os.path.exists(EXPORT_DIR): os.makedirs(EXPORT_DIR)
    
    with open(MANIFEST_PATH, "r") as f:
        data = json.load(f)

    seen = set()
    count = 0
    
    for item in data.get("shade_info", {}).get("list", []):
        for tex in item.get("textures", []):
            unity_path = tex.get("path")
            if not unity_path or unity_path.startswith("Resources/") or unity_path.startswith("Packages/"):
                continue
                
            abs_source = os.path.join(PROJECT_ROOT, unity_path)
            if abs_source in seen: continue
            
            if os.path.exists(abs_source):
                filename = os.path.basename(abs_source)
                dest = os.path.join(EXPORT_DIR, filename)
                
                # Collision check
                if os.path.exists(dest):
                    base, ext = os.path.splitext(filename)
                    dest = os.path.join(EXPORT_DIR, f"{base}_{len(seen)}{ext}")
                
                shutil.copy2(abs_source, dest)
                print(f"Copied: {filename}")
                seen.add(abs_source)
                count += 1
            else:
                print(f"Warning: File missing: {abs_source}")

    print(f"\nTotal textures collected: {count}")

if __name__ == "__main__":
    collect()
