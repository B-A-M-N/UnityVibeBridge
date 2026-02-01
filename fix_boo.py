import os
import sys

def fix_boo_error(project_path):
    target_file = "MarkerEditor.cs"
    found = False
    
    print(f"Searching for {target_file} in {project_path}...")
    
    for root, dirs, files in os.walk(project_path):
        if target_file in files:
            full_path = os.path.join(root, target_file)
            print(f"Found: {full_path}")
            found = True
            
            try:
                with open(full_path, 'r') as f:
                    lines = f.readlines()
                
                new_lines = []
                changed = False
                for line in lines:
                    if "using Boo.Lang" in line:
                        print(f"  - Removing: {line.strip()}")
                        changed = True
                    else:
                        new_lines.append(line)
                
                if changed:
                    with open(full_path, 'w') as f:
                        f.writelines(new_lines)
                    print("  [SUCCESS] File fixed!")
                else:
                    print("  [INFO] No 'Boo.Lang' found. File is already clean.")
                    
            except Exception as e:
                print(f"  [ERROR] Could not read/write file: {e}")

    if not found:
        print("[ERROR] Could not find MarkerEditor.cs. Are you pointing to the right folder?")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        # Try current directory if no arg provided
        fix_boo_error(".")
    else:
        fix_boo_error(sys.argv[1])
