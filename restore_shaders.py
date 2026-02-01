import os
import re

def get_guid(meta_path):
    try:
        with open(meta_path, 'r') as f:
            for line in f:
                if "guid:" in line:
                    return line.split("guid:")[1].strip()
    except Exception as e:
        print(f"Error reading meta {meta_path}: {e}")
    return None

def restore_shaders(root_path):
    print(f"Restoring shaders in {root_path}")
    count = 0
    skipped = 0
    for root, dirs, files in os.walk(root_path):
        for file in files:
            if file.endswith(".mat"):
                mat_path = os.path.join(root, file)
                mat_name = os.path.splitext(file)[0]
                
                # Check for Optimized Shader
                # Pattern: same_dir/OptimizedShaders/mat_name/*.shader
                opt_folder = os.path.join(root, "OptimizedShaders", mat_name)
                found_shader_guid = None
                
                if os.path.exists(opt_folder):
                    for s_file in os.listdir(opt_folder):
                        if s_file.endswith(".shader"):
                            meta_path = os.path.join(opt_folder, s_file + ".meta")
                            if os.path.exists(meta_path):
                                found_shader_guid = get_guid(meta_path)
                                # print(f"Found optimized shader for {file}: {s_file} ({found_shader_guid})")
                                break
                
                if found_shader_guid:
                    # Apply fix
                    try:
                        with open(mat_path, 'r') as f:
                            lines = f.readlines()
                        
                        new_lines = []
                        changed = False
                        for line in lines:
                            # Match the nuke line loosely or strictly
                            if "m_Shader:" in line and "fileID: 46" in line and "type: 0" in line:
                                # Replace with custom shader format
                                new_lines.append(f"  m_Shader: {{fileID: 4800000, guid: {found_shader_guid}, type: 3}}\n")
                                changed = True
                            else:
                                new_lines.append(line)
                        
                        if changed:
                            with open(mat_path, 'w') as f:
                                f.writelines(new_lines)
                            print(f"Restored: {file}")
                            count += 1
                        else:
                            # print(f"No change needed for {file}")
                            pass
                            
                    except Exception as e:
                        print(f"Error processing {file}: {e}")
                else:
                    print(f"Skipped (no opt shader): {file}")
                    skipped += 1

    print(f"Total restored: {count}")
    print(f"Total skipped (no opt shader): {skipped}")

if __name__ == "__main__":
    restore_shaders("/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/✮exto")
