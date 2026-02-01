import os
import re

def nuke_shaders(folder_path):
    print(f"Nuking shaders in: {folder_path}")
    
    # Standard Shader (Built-in)
    # m_Shader: {fileID: 46, guid: 0000000000000000f000000000000000, type: 0}
    standard_line = "  m_Shader: {fileID: 46, guid: 0000000000000000f000000000000000, type: 0}\n"
    
    count = 0
    for root, dirs, files in os.walk(folder_path):
        for file in files:
            if file.endswith(".mat"):
                path = os.path.join(root, file)
                try:
                    with open(path, 'r') as f:
                        lines = f.readlines()
                    
                    new_lines = []
                    changed = False
                    for line in lines:
                        # Match any m_Shader line that is NOT already Standard
                        if "m_Shader:" in line and "0000000000000000f000000000000000" not in line:
                            new_lines.append(standard_line)
                            changed = True
                        else:
                            new_lines.append(line)
                    
                    if changed:
                        with open(path, 'w') as f:
                            f.writelines(new_lines)
                        print(f"  Fixed: {file}")
                        count += 1
                        
                except Exception as e:
                    print(f"  Error reading {file}: {e}")

    print(f"Total materials fixed: {count}")

if __name__ == "__main__":
    nuke_shaders("/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/✮exto")
