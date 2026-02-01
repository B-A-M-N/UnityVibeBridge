import os

path = "/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/VibeBridge/VibeBridgeKernel.Core.cs"
with open(path, "r") as f:
    content = f.read()

old_line = 'string methodName = "VibeTool_" + path.Replace("/", "_");'
new_line = 'string methodName = "VibeTool_" + path.Replace("/", "_").Replace("-", "_");'

if old_line in content:
    new_content = content.replace(old_line, new_line)
    with open(path, "w") as f:
        f.write(new_content)
    print("Patched Core.cs for hyphens.")
else:
    print("Could not find line to patch.")
