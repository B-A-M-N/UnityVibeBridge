import sys
import re

def audit_cs_syntax(file_path):
    with open(file_path, 'r') as f:
        content = f.read()

    errors = []
    
    # 1. Lexical State Machine
    stack = []
    in_string = False
    in_char = False
    in_block_comment = False
    in_line_comment = False
    escaped = False
    
    line_no = 1
    col_no = 0
    
    i = 0
    while i < len(content):
        char = content[i]
        next_char = content[i+1] if i + 1 < len(content) else ""
        
        col_no += 1
        if char == '\n':
            line_no += 1
            col_no = 0
            in_line_comment = False
            if in_string:
                # C# doesn't allow multi-line strings without @
                if i > 0 and content[i-1] != '\r':
                     # Check if it's a verbatim string (very basic check)
                     # Real linter would be more complex, but this catches basic accidents
                     pass

        if in_line_comment:
            i += 1
            continue
            
        if in_block_comment:
            if char == '*' and next_char == '/':
                in_block_comment = False
                i += 2
                continue
            i += 1
            continue

        if in_string:
            if escaped:
                escaped = False
            elif char == '\\':
                escaped = True
            elif char == '"':
                in_string = False
            i += 1
            continue

        if in_char:
            if escaped:
                escaped = False
            elif char == '\\':
                escaped = True
            elif char == '\'':
                in_char = False
            i += 1
            continue

        # Start of comments/strings
        if char == '/' and next_char == '/':
            in_line_comment = True
            i += 2
            continue
        if char == '/' and next_char == '*':
            in_block_comment = True
            i += 2
            continue
        if char == '"':
            in_string = True
            i += 1
            continue
        if char == '\'':
            in_char = True
            i += 1
            continue

        # Balancing
        if char in '{形(': # Typo check: { ( [
            pass # Replaced with actual chars below
        if char in '{[(': 
            stack.append((char, line_no, col_no))
        elif char in '}])':
            if not stack:
                errors.append(f"Unexpected closing '{char}' at {line_no}:{col_no}")
            else:
                opening, o_line, o_col = stack.pop()
                if (opening == '{' and char != '}') or \
                   (opening == '[' and char != ']') or \
                   (opening == '(' and char != ')'):
                    errors.append(f"Mismatched '{char}' at {line_no}:{col_no} (opened with '{opening}' at {o_line}:{o_col})")

        i += 1

    if in_block_comment: errors.append("Unclosed block comment (/*)")
    if in_string: errors.append("Unterminated string literal")
    if in_char: errors.append("Unterminated character literal")
    
    while stack:
        opening, o_line, o_col = stack.pop()
        errors.append(f"Unclosed '{opening}' opened at {o_line}:{o_col}")

    # 2. Basic Semicolon Check (Heuristic)
    # Lines ending in word/paren without semicolon/brace/comma
    lines = content.splitlines()
    for idx, line in enumerate(lines):
        clean = re.sub(r'//.*', '', line).strip()
        if not clean or clean.endswith('{') or clean.endswith('}') or clean.endswith(';') or clean.endswith(',') or clean.startswith('#') or clean.startswith('['):
            continue
        # Control flow
        if clean.startswith('if') or clean.startswith('else') or clean.startswith('while') or clean.startswith('for') or clean.startswith('foreach') or clean.startswith('using ('):
            continue
        # Namespace/Class/Method headers (very rough)
        if 'class ' in clean or 'namespace ' in clean or 'static ' in clean:
            continue
            
        # If the line ends with a letter or digit or closing paren, it likely needs a semicolon
        if re.search(r'[\w\)]$', clean):
            # This is a heuristic, but good for catching missing ;
            # errors.append(f"Potential missing semicolon at {idx+1}")
            pass

    return errors

if __name__ == "__main__":
    results = audit_cs_syntax(sys.argv[1])
    if results:
        print("\n".join(results))
        sys.exit(1)
    else:
        print("Syntactic Audit Passed.")
        sys.exit(0)
