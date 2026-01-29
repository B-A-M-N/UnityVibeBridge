import os
import glob
import re

# Configuration
SRC_DIR = "src"
UNITY_ASSETS_DIR = "/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/VibeBridge"
OUTPUT_FILENAME = "Bridge_v16.cs"
TARGET_NAMESPACE = "VibeBridge" 
TARGET_CLASS = "VibeBridgeServer"

def find_matching_brace(content, start_index):
    brace_count = 0
    in_string = False
    in_char = False
    i = start_index
    length = len(content)
    while i < length:
        c = content[i]
        if c == '\\': i += 2; continue
        if c == '"' and not in_char: in_string = not in_string
        if c == "'" and not in_string: in_char = not in_char
        if not in_string and not in_char:
            if c == '{': brace_count += 1
            elif c == '}':
                brace_count -= 1
                if brace_count == 0: return i
        i += 1
    return -1

def process_module(path):
    with open(path, 'r') as f:
        content = f.read()
    
    usings = re.findall(r"^using\s+.*;", content, re.MULTILINE)
    
    # Look for the class body
    class_match = re.search(r"public\s+static\s+partial\s+class\s+VibeBridgeServer\s*\{", content)
    if class_match:
        class_start_idx = class_match.end() - 1
        class_end_idx = find_matching_brace(content, class_start_idx)
        if class_end_idx != -1:
            body = content[class_start_idx+1:class_end_idx].strip()
            # RENAME CONSTRUCTOR: Replace VibeBridgeServer with TARGET_CLASS
            body = body.replace("static VibeBridgeServer()", f"static {TARGET_CLASS}()")
            # RENAME SELF-REFERENCES: Replace VibeBridgeServer. with TARGET_CLASS.
            body = body.replace("VibeBridgeServer.", f"{TARGET_CLASS}.")
            # RENAME TYPE REFERENCES: typeof(VibeBridgeServer)
            body = body.replace("typeof(VibeBridgeServer)", f"typeof({TARGET_CLASS})")
            return usings, "", body

    # Fallback: Entire namespace content (for types)
    ns_match = re.search(r"namespace\s+VibeBridge\s*\{", content)
    if ns_match:
        ns_start_idx = ns_match.end() - 1
        ns_end_idx = find_matching_brace(content, ns_start_idx)
        if ns_end_idx != -1:
            types = content[ns_start_idx+1:ns_end_idx].strip()
            return usings, types, ""
            
    return usings, "", ""

def bundle():
    print(f"[*] Building Robust Bundle v16.1...")
    all_usings = set()
    namespace_types = []
    class_bodies = []
    
    modules = sorted(glob.glob(os.path.join(SRC_DIR, "*.cs")))
    priority = ["SecurityModule.cs", "RegistryTypes.cs", "Core.cs"]
    modules.sort(key=lambda x: (0, priority.index(os.path.basename(x))) if os.path.basename(x) in priority else (1, x))

    for m in modules:
        print(f"  [>] Processing {os.path.basename(m)}...")
        u, types, body = process_module(m)
        print(f"      - Body len: {len(body)}, Types len: {len(types)}")
        for line in u: all_usings.add(line.strip())
        if types: namespace_types.append(f"// --- Types from {os.path.basename(m)} ---\n{types}")
        if body: class_bodies.append(f"// --- Logic from {os.path.basename(m)} ---\n{body}")

    output = []
    output.append("// AUTO-GENERATED VIBEBRIDGE BUNDLE v16.1")
    output.append(f"// Built on: {os.popen('date').read().strip()}")
    output.append("")
    for u in sorted(list(all_usings)): output.append(u)
    output.append("")
    output.append(f"namespace {TARGET_NAMESPACE} {{")
    output.append("")
    for t_block in namespace_types:
        output.append(t_block)
        output.append("")
    output.append("    [UnityEditor.InitializeOnLoad]")
    output.append(f"    public static partial class {TARGET_CLASS} {{")
    output.append("")
    for b_block in class_bodies:
        output.append(b_block)
        output.append("")
    output.append("    }")
    output.append("}")
    
    os.makedirs(UNITY_ASSETS_DIR, exist_ok=True)
    dest_path = os.path.join(UNITY_ASSETS_DIR, OUTPUT_FILENAME)
    with open(dest_path, 'w') as f: f.write("\n".join(output))
    print(f"[+] Bundle Written: {dest_path}")

if __name__ == "__main__":
    bundle()
