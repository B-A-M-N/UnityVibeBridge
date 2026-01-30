import ast
import re
import os

class SecurityGate:
    """Production-Hardened Security Gate v1.3 with Recursive AST Auditing."""

    PYTHON_FORBIDDEN_MODULES = {'os', 'subprocess', 'shlex', 'shutil', 'socket', 'requests', 'urllib', 'http', 'pickle', 'marshal', 'ctypes'}
    PYTHON_FORBIDDEN_FUNCTIONS = {'eval', 'exec', 'getattr', 'setattr', '__import__', 'compile', 'input', 'globals', 'locals', 'gethostbyname', 'create_connection'}
    PYTHON_FORBIDDEN_ATTRIBUTES = {'__subclasses__', '__dict__', '__globals__', '__mro__', '__base__', '__class__', 'modules', 'environ'}

    @staticmethod
    def _is_path_safe(path):
        forbidden = {'.meta', 'security_gate.py', 'vibe_status.json', 'vibe_settings.json', 'HUMAN_ONLY'}
        if any(f in path for f in forbidden): return False
        if '..' in path or path.startswith('/') or ':' in path: return False
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
                # 1. Alias Detection
                if isinstance(node, ast.Assign):
                    if isinstance(node.value, ast.Name) and node.value.id in SecurityGate.PYTHON_FORBIDDEN_FUNCTIONS:
                        issues.append(f"Python Violation: Attempt to alias forbidden function '{node.value.id}'")
                    # DoS: Large memory allocation
                    if isinstance(node.value, ast.BinOp) and isinstance(node.value.op, ast.Mult):
                        if isinstance(node.value.right, ast.Constant) and isinstance(node.value.right.value, int) and node.value.right.value > 1000000:
                            issues.append("Python Violation: Potential memory exhaustion (DoS).")

                # 2. Function Call & Sink Check
                if isinstance(node, ast.Call):
                    func_name = ""
                    if isinstance(node.func, ast.Name): func_name = node.func.id
                    elif isinstance(node.func, ast.Attribute): func_name = node.func.attr
                    
                    if func_name in SecurityGate.PYTHON_FORBIDDEN_FUNCTIONS:
                        issues.append(f"Python Violation: Forbidden function '{func_name}'")
                    
                    if func_name in {'open', 'write', 'Path'}:
                        for arg in node.args:
                            if isinstance(arg, ast.Constant) and isinstance(arg.value, str):
                                if not SecurityGate._is_path_safe(arg.value):
                                    issues.append(f"Python Violation: Unsafe path '{arg.value}'")

                # 3. Attribute & Metadata Checks
                if isinstance(node, ast.Attribute) and node.attr in SecurityGate.PYTHON_FORBIDDEN_ATTRIBUTES:
                    issues.append(f"Python Violation: Forbidden attribute '{node.attr}'")
                
                # 4. Imports
                if isinstance(node, (ast.Import, ast.ImportFrom)):
                    modules = [n.name.split('.')[0] for n in (node.names if isinstance(node, ast.Import) else [ast.alias(name=node.module, asname=None)])]
                    for m in modules:
                        if m in SecurityGate.PYTHON_FORBIDDEN_MODULES:
                            issues.append(f"Python Violation: Forbidden module '{m}'")

                # 5. Loop Verification
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
            (r'dynamic\b', "Dynamic Typing"), (r'typeof\(.*\)\.GetMethod', "Type-based Reflection"),
            (r'AppDomain', "AppDomain manipulation"), (r'unsafe\b', "Unsafe code"),
            (r'ModuleInitializer', "Module Initializer Hijack"), (r'Type\.GetType', "Reflection")
        ]
        clean = re.sub(r'//.*|/\*.*?\*/', '', content, flags=re.DOTALL)
        clean = re.sub(r'"(\\.|[^"\\])*"', '""', clean)
        for pattern, reason in BANNED:
            if re.search(pattern, clean, re.IGNORECASE): issues.append(f"C# Violation: {reason} is forbidden.")
        return issues

    @staticmethod
    def _check_secrets(content):
        issues = []
        # secret patterns (api keys, tokens)
        if re.search(r'(api[_-]?key|secret|password|token|auth)\s*[:=]\s*["\'][a-zA-Z0-9_\-\.] {10,}', content, re.I):
            issues.append("Security: Possible hardcoded secret.")
        # chain detection (ya29, AKIA)
        if re.search(r'ya29\.[a-zA-Z0-9_\-]{50,}', content) or re.search(r'AKIA[A-Z0-9]{16}', content):
            issues.append("Security: High-entropy secret pattern.")
        return issues