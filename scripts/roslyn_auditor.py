import os
import re

class RoslynAuditor:
    @staticmethod
    def detect_csharp_version():
        """Detects the C# version supported by the project based on Unity version."""
        version_file = "ProjectSettings/ProjectVersion.txt"
        if not os.path.exists(version_file):
            return 7.3 # Conservative default
        
        try:
            with open(version_file, "r") as f:
                content = f.read()
                # e.g., m_EditorVersion: 2022.3.10f1
                match = re.search(r"m_EditorVersion: (\d+)\.", content)
                if match:
                    year = int(match.group(1))
                    if year >= 2022: return 9.0
                    if year >= 2021: return 8.0
        except: pass
        return 7.3

    @staticmethod
    def audit_code(code):
        """Performs a multi-layered audit of generated C# code."""
        issues = []
        version = RoslynAuditor.detect_csharp_version()

        # 1. Syntax Balance (Phase 1 Fix)
        if code.count("{") != code.count("}"):
            issues.append("Syntax Error: Mismatched curly braces { }.")
        if code.count("(") != code.count(")"):
            issues.append("Syntax Error: Mismatched parentheses ( ).")

        # 2. Mandatory Patterns
        if "partial class VibeBridgeServer" not in code and "namespace UnityVibeBridge.Kernel" in code:
            issues.append("Structural Error: Missing 'partial' keyword for VibeBridgeServer extension.")

        # 3. Security Hardening
        if "System.Reflection" in code and "BindingFlags" not in code: # Allow our specific reflection pattern
             # Check for raw reflection bypass
             if re.search(r"\.GetMethod\(|\.GetField\(", code) and "[VibeTool" not in code:
                 issues.append("Security Risk: Potential raw reflection bypass detected.")

        # 4. Version Compliance
        if version < 9.0:
            if "new()" in code and not "new (" in code: # Detection for target-typed new
                issues.append(f"Version Mismatch: Target-typed new 'new()' requires C# 9.0+. Current project targeting ~C# {version}.")

        return issues

if __name__ == "__main__":
    # Test
    test_code = "public class Test { void Run() { var x = new(); } }"
    auditor = RoslynAuditor()
    print(f"Auditing for C# {auditor.detect_csharp_version()}...")
    errors = auditor.audit_code(test_code)
    for e in errors: print(f"❌ {e}")
