import os
import glob
import re
import subprocess
import sys

SRC_DIR = "src"
OUTPUT_FILE = "unity-package/Scripts/VibeBridgeServer_v16.cs"
UNITY_PROJECT_FILE = "/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/VibeBridge/VibeBridgeServer_v16.cs"

def find_matching_brace(content, start_index):
    """
    Finds the index of the closing brace '}' matching the opening brace '{' at start_index.
    Correctly ignores braces inside strings, chars, and comments.
    """
    brace_count = 0
    in_string = False
    in_char = False
    in_line_comment = False
    in_block_comment = False
    i = start_index
    length = len(content)

    while i < length:
        c = content[i]
        
        # Handle escapes first
        if (in_string or in_char) and c == '\\':
            i += 2
            continue

        # Handle comments
        if not (in_string or in_char or in_line_comment or in_block_comment):
            if c == '/' and i + 1 < length:
                if content[i+1] == '/':
                    in_line_comment = True
                    i += 2
                    continue
                elif content[i+1] == '*':
                    in_block_comment = True
                    i += 2
                    continue
        
        if in_line_comment:
            if c == '\n':
                in_line_comment = False
            i += 1
            continue
            
        if in_block_comment:
            if c == '*' and i + 1 < length and content[i+1] == '/':
                in_block_comment = False
                i += 2
            else:
                i += 1
            continue

        # Handle strings and chars
        if not (in_line_comment or in_block_comment):
            if c == '"' and not in_char:
                in_string = not in_string
            elif c == "'" and not in_string:
                in_char = not in_char

        # Handle braces
        if not (in_string or in_char or in_line_comment or in_block_comment):
            if c == '{':
                brace_count += 1
            elif c == '}':
                brace_count -= 1
                if brace_count == 0:
                    return i
        
        i += 1
    return -1

def extract_all_namespaces(content):
    results = [] # list of (namespace, type_content, class_content)
    
    # 1. Strip usings from content for easier matching (optional, but keeps logic similar)
    # clean_content = re.sub(r"^using\s+.*;", "", content, flags=re.MULTILINE).strip()
    # Actually, let's keep the content intact to preserve positions, just scan carefully.
    
    # Find namespace declarations
    ns_matches = list(re.finditer(r"namespace\s+([\w\.]+)\s*\{", content))
    
    for match in ns_matches:
        ns_name = match.group(1)
        start_pos = match.end() - 1 # Point to the '{'
        end_pos = find_matching_brace(content, start_pos)
        
        if end_pos != -1:
            inner_block = content[start_pos+1:end_pos].strip()
            
            # Now search for VibeBridgeServer class inside this namespace block
            # We need to be careful again about braces inside the class
            
            vbs_content = ""
            # Regex to find the class start. 
            # Note: We assume the class declaration isn't inside a string/comment within the namespace block.
            # This is a safe assumption for valid C# files usually.
            class_decl_pattern = r"((?:\\[\w\\.]+\\]\s*)*)public\s+static\s+partial\s+class\s+VibeBridgeServer\s*\{" # Corrected escape for backslash in attribute list
            class_match = re.search(class_decl_pattern, inner_block)
            
            types_content = inner_block
            
            if class_match:
                c_start_rel = class_match.end() - 1
                # We need to find the matching brace for the class. 
                # We can reuse find_matching_brace on the inner_block
                c_end_rel = find_matching_brace(inner_block, c_start_rel)
                
                if c_end_rel != -1:
                    vbs_content = inner_block[c_start_rel+1:c_end_rel].strip()
                    # Remove the class body from the types content
                    # We also remove the declaration preceding it
                    types_content = inner_block[:class_match.start()] + inner_block[c_end_rel+1:]
            
            results.append((ns_name, types_content.strip(), vbs_content))
            
    return results

def bundle():
    print(f"Scanning {SRC_DIR}...")
    modules = sorted(glob.glob(os.path.join(SRC_DIR, "*.cs")))
    # Ensure SecurityModule and RegistryTypes are early
    priority = ["SecurityModule.cs", "RegistryTypes.cs", "Core.cs"]
    modules.sort(key=lambda x: (0, priority.index(os.path.basename(x))) if os.path.basename(x) in priority else (1, x))
    
    all_usings = set()
    ns_types = {} # ns_name -> list of type strings
    vbs_bodies = []
    
    processed_count = 0
    
    for m_path in modules:
        print(f"Processing {m_path}...")
        with open(m_path, "r") as f:
            content = f.read()
            
            # Extract usings
            usings = re.findall(r"^using\s+.*;", content, re.MULTILINE)
            for u in usings: all_usings.add(u.strip())
            
            ns_results = extract_all_namespaces(content)
            print(f"  -> Extracted {len(ns_results)} namespaces")
            for ns_name, types, vbs_body in ns_results:
                if ns_name not in ns_types: ns_types[ns_name] = []
                if types: ns_types[ns_name].append(types)
                if vbs_body:
                    vbs_bodies.append(f"// --- FROM {os.path.basename(m_path)} ---\n{vbs_body}")
                    print(f"  -> Found VibeBridgeServer body in {os.path.basename(m_path)}")
                    processed_count += 1
                else:
                    print(f"  -> NO VibeBridgeServer body found in namespace {ns_name} of {os.path.basename(m_path)}")
    
    final_code = "// BUNDLED VIBEBRIDGE MODULAR SERVER\n"
    final_code += "\n".join(sorted(list(all_usings))) + "\n\n"
    
    for ns_name in sorted(ns_types.keys()):
        final_code += f"namespace {ns_name} {{\n"
        # Combine unique types
        unique_types = []
        for t in ns_types[ns_name]:
            if t.strip() and t.strip() not in unique_types:
                unique_types.append(t.strip())
        
        for t in unique_types:
            final_code += t + "\n\n"
            
        if ns_name == "VibeBridge" and vbs_bodies:
            final_code += "    [UnityEditor.InitializeOnLoad]\n"
            final_code += "    public static partial class VibeBridgeServer {\n\n"
            for b in vbs_bodies:
                final_code += b + "\n\n"
            final_code += "    }\n"
        final_code += "}\n\n"
    
    os.makedirs(os.path.dirname(OUTPUT_FILE), exist_ok=True)
    with open(OUTPUT_FILE, "w") as f: f.write(final_code)
    print(f"Successfully bundled {processed_count} class modules.")
    
    # Audit
    # audit_res = subprocess.run(["python3", "cs_audit.py", OUTPUT_FILE], capture_output=True, text=True)
    # if audit_res.returncode != 0:
    #     print("❌ STAGING SYNTAX AUDIT FAILED:")
    #     print(audit_res.stdout)
    #     return

    # print("✅ Audit Passed.")

    if os.path.exists(os.path.dirname(UNITY_PROJECT_FILE)):
        with open(UNITY_PROJECT_FILE, "w") as f: f.write(final_code)
        print("🚀 COMMITTED to Unity Assets: " + UNITY_PROJECT_FILE)

if __name__ == "__main__":
    bundle()