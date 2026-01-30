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
        print(f"Char: '{c}' i={i} bc={brace_count} s={in_string}")
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
            if c == '{': brace_count += 1
            elif c == '}':
                brace_count -= 1
                if brace_count == 0: return i
        i += 1
    return -1

with open("src/VisualModule.cs", "r") as f:
    content = f.read()

import re
regex = r"namespace\s+(\w+)"
matches = list(re.finditer(regex, content))
print(f"Matches (simple): {len(matches)}")
for m in matches:
    print(f"  Found '{m.group(0)}' at {m.start()}")

regex = r"namespace\s+([\w\.]+)\s*{"
matches = list(re.finditer(regex, content, re.MULTILINE | re.DOTALL))
print(f"Matches: {len(matches)}")
for m in matches:
    print(f"Match: {m.group(1)} at {m.start()}")
    end = find_matching_brace(content, m.end() - 1)
    print(f"End Pos: {end}")
