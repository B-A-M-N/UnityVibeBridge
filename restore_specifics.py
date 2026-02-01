import os
import re
import sys

# Materials explicitly confirmed as "Fine" by user. 
# We will NOT touch these.
EXCLUDE_FILES = [
    "face.mat",
    "hair 1.mat",
    "hair 2.mat",
    "pants.mat",
    "skele hoodie.mat",
    "eyes.mat"
]

def get_guid_from_meta(meta_path):
    try:
        with open(meta_path, 'r') as f:
            for line in f:
                if "guid:" in line:
                    return line.split("guid:")[1].strip()
    except Exception as e:
        print(f"  Error reading meta {meta_path}: {e}")
    return None

def find_optimized_shader_guid(mat_path):
    # Heuristic: Look for 'OptimizedShaders/<mat_name>/' in the same directory or parents
    # Actually, based on file structure: 
    # mat_path: .../Folder/MatName.mat
    # opt_path: .../Folder/OptimizedShaders/MatName/xxx.shader
    
    folder = os.path.dirname(mat_path)
    mat_filename = os.path.basename(mat_path)
    mat_name = os.path.splitext(mat_filename)[0]
    
    # Common path pattern seen in logs:
    # .../TextureFolder/MatName.mat
    # .../TextureFolder/OptimizedShaders/MatName/S_Poiyomi_Toon.shader
    
    opt_folder = os.path.join(folder, "OptimizedShaders", mat_name)
    
    if os.path.exists(opt_folder):
        for f in os.listdir(opt_folder):
            if f.endswith(".shader"):
                meta_path = os.path.join(opt_folder, f + ".meta")
                if os.path.exists(meta_path):
                    return get_guid_from_meta(meta_path)
    
    # Fallback: Sometimes optimized shaders are one level up? 
    # Or in a 'Textures' folder? 
    # Let's rely on the standard pattern first.
    return None

def restore_specifics(root_path):
    print(f"Starting Granular Restore in: {root_path}")
    print("Strategy: Keep 'Fine' materials on Master. Re-link 'Wrong' materials to their Optimized Shader.")
    
    count = 0
    skipped = 0
    
    for root, dirs, files in os.walk(root_path):
        for file in files:
            if file.endswith(".mat"):
                if file.lower() in EXCLUDE_FILES:
                    # print(f"[Skipping Fine] {file}")
                    continue
                
                mat_path = os.path.join(root, file)
                
                # Find Optimized GUID
                opt_guid = find_optimized_shader_guid(mat_path)
                
                if opt_guid:
                    # Apply it
                    try:
                        with open(mat_path, 'r') as f:
                            lines = f.readlines()
                        
                        changed = False
                        new_lines = []
                        for line in lines:
                            if "m_Shader:" in line and "fileID:" in line:
                                if opt_guid in line:
                                    # Already correct
                                    pass
                                else:
                                    prefix = line.split("m_Shader:")[0]
                                    new_lines.append(f"{prefix}m_Shader: {{fileID: 4800000, guid: {opt_guid}, type: 3}}\n")
                                    changed = True
                                    continue
                            new_lines.append(line)
                        
                        if changed:
                            with open(mat_path, 'w') as f:
                                f.writelines(new_lines)
                            print(f"[Restored to Opt] {file} -> {opt_guid}")
                            count += 1
                        else:
                            # print(f"[Already Opt] {file}")
                            pass
                            
                    except Exception as e:
                        print(f"[Error] {file}: {e}")
                else:
                    # print(f"[No Opt Found] {file}")
                    skipped += 1

    print("------------------------------------------------")
    print(f"Granular Restore Complete.")
    print(f"Restored to Optimized: {count}")
    print(f"Skipped (No Opt Found): {skipped}")

if __name__ == "__main__":
    target_path = "/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/✮exto"
    if len(sys.argv) > 1:
        target_path = sys.argv[1]
    
    restore_specifics(target_path)
