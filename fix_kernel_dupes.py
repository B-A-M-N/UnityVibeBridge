import os

path = "/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/VibeBridge/VibeBridgeKernel.cs"
with open(path, "r") as f:
    lines = f.readlines()

new_lines = []
skip = False
found_namespace = False

for line in lines:
    if "namespace VibeBridge {" in line:
        found_namespace = True
        new_lines.append(line)
        skip = True # Start skipping types
        continue
    
    if "[InitializeOnLoad]" in line:
        skip = False # Stop skipping, we reached the class
        
    if not skip:
        new_lines.append(line)
    elif skip and ("using" in line or "#if" in line): # Safety check
        skip = False
        new_lines.append(line)

with open(path, "w") as f:
    f.writelines(new_lines)

print(f"Cleaned {path}")
