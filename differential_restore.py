import os
import re
import sys

# GUIDs derived from user input and project scanning
POI_TOON_GUID = "fcc39a2fdda47d84cb32cb3c226657dc"
POI_PRO_GUID = "4681c5e75c9738f41bc55faf877212a4"

def differential_restore(root_path):
    print(f"Starting Differential Recovery in: {root_path}")
    print(f"Target Toon GUID: {POI_TOON_GUID}")
    print(f"Target Pro GUID:  {POI_PRO_GUID}")
    
    count = 0
    scanned = 0
    
    for root, dirs, files in os.walk(root_path):
        for file in files:
            if file.endswith(".mat"):
                scanned += 1
                mat_path = os.path.join(root, file)
                try:
                    with open(mat_path, 'r') as f:
                        lines = f.readlines()
                    
                    found_tag = None
                    shader_line_index = -1
                    
                    # 1. Scan for OriginalShader tag and m_Shader line
                    for i, line in enumerate(lines):
                        if "m_Shader:" in line and "fileID:" in line:
                            shader_line_index = i
                        
                        if "OriginalShader" in line:
                            # Extract the value part (everything after :)
                            parts = line.split(":", 1)
                            if len(parts) > 1:
                                found_tag = parts[1].strip().strip('"').strip("'")
                    
                    if found_tag and shader_line_index != -1:
                        # Determine target GUID
                        target_guid = None
                        tag_lower = found_tag.lower()
                        
                        # logic to distinguish Toon vs Pro
                        # Tag example: ".poiyomi/Poiyomi 7.3/• Poiyomi Toon •"
                        # We look for "toon" or "pro"
                        
                        if "toon" in tag_lower:
                            target_guid = POI_TOON_GUID
                        elif "pro" in tag_lower and "prop" not in tag_lower: # simple safety
                            target_guid = POI_PRO_GUID
                        
                        if target_guid:
                            current_line = lines[shader_line_index]
                            
                            # Check if already correct
                            if target_guid in current_line:
                                # print(f"Skipping {file} (already correct)")
                                continue
                            
                            # Apply change
                            prefix = current_line.split("m_Shader:")[0]
                            # Use fileID 4800000 for custom shaders
                            new_line = f"{prefix}m_Shader: {{fileID: 4800000, guid: {target_guid}, type: 3}}\n"
                            lines[shader_line_index] = new_line
                            
                            with open(mat_path, 'w') as f:
                                f.writelines(lines)
                            
                            print(f"[Restored] {file}")
                            print(f"  Tag: {found_tag}")
                            print(f"  New GUID: {target_guid}")
                            count += 1
                        else:
                            # print(f"[Skipped] {file} - Unknown tag: {found_tag}")
                            pass

                except Exception as e:
                    print(f"[Error] {file}: {e}")

    print("------------------------------------------------")
    print(f"Differential Recovery Complete.")
    print(f"Scanned: {scanned}")
    print(f"Restored: {count}")

if __name__ == "__main__":
    target_path = "/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/✮exto"
    if len(sys.argv) > 1:
        target_path = sys.argv[1]
    
    if not os.path.exists(target_path):
        print(f"Error: Path does not exist: {target_path}")
        sys.exit(1)
        
    differential_restore(target_path)