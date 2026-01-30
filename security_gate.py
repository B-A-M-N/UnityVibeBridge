import ast
import re
import os

class SecurityGate:
    """Hardened Security Gate v1.2 with Path-Safety and Alias Detection."""

    PYTHON_FORBIDDEN_MODULES = {'os', 'subprocess', 'shlex', 'shutil', 'socket', 'requests', 'urllib', 'http', 'pickle', 'marshal'}
    PYTHON_FORBIDDEN_FUNCTIONS = {'eval', 'exec', 'getattr', 'setattr', '__import__', 'compile', 'input', 'globals', 'locals'}
    PYTHON_FORBIDDEN_ATTRIBUTES = {'__subclasses__', '__dict__', '__globals__', '__mro__', '__base__', '__class__', 'modules', 'environ'}

    @staticmethod
    def _is_path_safe(path):
        """Blocks access to sensitive infra, .meta files, and path traversal."""
        forbidden = {'.meta', 'security_gate.py', 'vibe_status.json', 'vibe_settings.json', 'HUMAN_ONLY'}
        if any(f in path for f in forbidden): return False
        if '..' in path or path.startswith('/'): return False
        return True

    @staticmethod
    def check_python(content):
        issues = []
        try:
            content.encode('ascii')
        except UnicodeEncodeError:
            issues.append("Python Violation: Non-ASCII characters detected.")

        try:
            tree = ast.parse(content)
            for node in ast.walk(tree):
                # 1. Alias Detection (e.g. f = __import__)
                if isinstance(node, ast.Assign):
                    if isinstance(node.value, ast.Name) and node.value.id in SecurityGate.PYTHON_FORBIDDEN_FUNCTIONS:
                        issues.append(f"Python Violation: Attempt to alias forbidden function '{node.value.id}'")

                # 2. Function Call & Path Check
                if isinstance(node, ast.Call):
                    func_name = ""
                    if isinstance(node.func, ast.Name): func_name = node.func.id
                    elif isinstance(node.func, ast.Attribute): func_name = node.func.attr
                    
                    if func_name in SecurityGate.PYTHON_FORBIDDEN_FUNCTIONS:
                        issues.append(f"Python Violation: Forbidden function '{func_name}'")
                    
                    # Check path arguments in common sinks
                    if func_name in {'open', 'write', 'Path'}:
                        for arg in node.args:
                            if isinstance(arg, ast.Constant) and isinstance(arg.value, str):
                                if not SecurityGate._is_path_safe(arg.value):
                                    issues.append(f"Python Violation: Unsafe path '{arg.value}'")

                # 3. Attribute & Loop Checks
                if isinstance(node, ast.Attribute) and node.attr in SecurityGate.PYTHON_FORBIDDEN_ATTRIBUTES:
                    issues.append(f"Python Violation: Forbidden attribute '{node.attr}'")
                
                if isinstance(node, ast.While) and isinstance(node.test, ast.Constant) and node.test.value is True:
                    if not any(isinstance(n, ast.Break) for n in ast.walk(node)):
                        issues.append("Python Violation: Infinite loop detected.")

        except SyntaxError:
            issues.append("Python Violation: Syntax Error.")
        
        issues.extend(SecurityGate._check_secrets(content))
        return issues

    @staticmethod
    def check_csharp(content):
        issues = []
        BANNED = [
            (r'System\.Reflection', "Reflection"), (r'Process\.Start', "Spawning"),
            (r'File\.(Write|Delete|Append|Move|Copy)', "File Mutation"),
            (r'Directory\.', "Directory Mutation"), (r'System\.Net', "Networking"),
            (r'InitializeOnLoad', "Constructor Side-effects"), (r'DllImport', "Native Code"),
            (r'dynamic\b', "Dynamic Typing"), (r'typeof\(.*\)\.GetMethod', "Type-based Reflection")
        ]
        clean = re.sub(r'//.*|/\*.*?\*/', '', content, flags=re.DOTALL)
        clean = re.sub(r'"(\\.|[^"\\])*"', '""', clean)
        for pattern, reason in BANNED:
            if re.search(pattern, clean, re.IGNORECASE): issues.append(f"C# Violation: {reason} is forbidden.")
        return issues

    @staticmethod
    def _check_secrets(content):
        issues = []
        if re.search(r'(api[_-]?key|secret|password|token|auth)\s*[:=]\s*["\'][a-zA-Z0-9_\-\.] {10,}', content, re.I):
            issues.append("Security: Possible hardcoded secret.")
        return issues
