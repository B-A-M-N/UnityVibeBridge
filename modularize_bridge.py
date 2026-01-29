import os
import re

SOURCE_PATH = "/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/VibeBridge/VibeBridgeServer.cs"
SRC_DIR = "src"

def modularize():
    if not os.path.exists(SRC_DIR):
        os.makedirs(SRC_DIR)

    with open(SOURCE_PATH, "r") as f:
        content = f.read()

    usings = "\n".join(re.findall(r"^using\s+.*;", content, re.MULTILINE))
    
    # 1. Thry
    thry_match = re.search(r"namespace Thry \{(.*?)\n}", content, re.DOTALL)
    if thry_match:
        with open(os.path.join(SRC_DIR, "ThryCompatibility.cs"), "w") as f:
            f.write(usings + "\n\nnamespace Thry {\n" + thry_match.group(1) + "\n}\n")

    # 2. RegistryTypes (The pure types)
    reg_types_match = re.search(r"// --- FROM RegistryTypes.cs ---\n(.*?)// ---", content, re.DOTALL)
    if reg_types_match:
        with open(os.path.join(SRC_DIR, "RegistryTypes.cs"), "w") as f:
            f.write(usings + "\n\nnamespace VibeBridge {\n" + reg_types_match.group(1).strip() + "\n}\n")

    # 3. Security (Including Enum)
    sec_match = re.search(r"public enum EditorCapability \{(.*?)\n    }", content, re.DOTALL)
    sec_body_match = re.search(r"// --- FROM SecurityModule.cs ---\n(.*?)// ---", content, re.DOTALL)
    if sec_body_match:
        body = usings + "\n\nnamespace VibeBridge {\n"
        if sec_match:
            body += "    public enum EditorCapability {" + sec_match.group(1) + "    }\n\n"
        body += "    public static partial class VibeBridgeServer {\n"
        body += "        " + sec_body_match.group(1).strip().replace("\n", "\n        ") + "\n"
        body += "    }\n}\n"
        with open(os.path.join(SRC_DIR, "SecurityModule.cs"), "w") as f:
            f.write(body)

    # 4. Process all other modules
    segments = re.split(r"// --- FROM (.*?) ---", content)
    for i in range(1, len(segments), 2):
        filename = segments[i].strip()
        if filename in ["SecurityModule.cs", "RegistryTypes.cs", "ThryCompatibility.cs"]:
            continue
            
        body = segments[i+1].strip()
        # Remove trailing close braces if this was the end of the monolith
        body = re.sub(r"\s*}\s*}\s*$", "", body)
        
        module_content = usings + "\n\nnamespace VibeBridge {\n"
        module_content += "    public static partial class VibeBridgeServer {\n"
        module_content += "        " + body.replace("\n", "\n        ") + "\n"
        module_content += "    }\n}\n"
        
        with open(os.path.join(SRC_DIR, filename), "w") as f:
            f.write(module_content)
        print(f"Modularized: {filename}")

if __name__ == "__main__":
    modularize()