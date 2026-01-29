import os

def find_matching_brace(content, start_index):
    brace_count = 0
    in_string = False
    in_char = False
    in_line_comment = False
    in_block_comment = False
    i = start_index
    length = len(content)
    while i < length:
        c = content[i]
        
        if (in_string or in_char) and c == '\\':
            i += 2
            continue
            
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
            if c == '\n': in_line_comment = False
            i += 1
            continue
            
        if in_block_comment:
            if c == '*' and i + 1 < length and content[i+1] == '/':
                in_block_comment = False
                i += 2
            else: i += 1
            continue
            
        if not (in_line_comment or in_block_comment):
            if c == '"' and not in_char: in_string = not in_string
            elif c == "'" and not in_string: in_char = not in_char
            
        if not (in_string or in_char or in_line_comment or in_block_comment):
            if c == '{':
                brace_count += 1
            elif c == '}':
                brace_count -= 1
                if brace_count == 0: return i
        i += 1
    return -1

for fname in ["src/AuditModule.cs", "src/VRChatModule.cs", "src/WorldModule.cs"]:
    print(f"--- {fname} ---")
    content = open(fname).read()
    start = content.find("namespace VibeBridge {")
    if start == -1:
        print("Namespace declaration not found")
        continue
    start += len("namespace VibeBridge {") - 1
    print(f"Start: {start}, char: {content[start]}")
    end = find_matching_brace(content, start)
    print(f"End: {end}")
    if end == -1:
        # Debug why it failed
        print("Debugging failure...")
        # ... could add more here
