import re

content = """using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VibeBridge
{
    public static partial class VibeBridgeServer
    {
"""

regex = r"namespace\s+([\w\.]+)\s*\{"
matches = list(re.finditer(regex, content, re.MULTILINE | re.DOTALL))
print(f"Matches found: {len(matches)}")
for m in matches:
    print(f"  NS: {m.group(1)}")
