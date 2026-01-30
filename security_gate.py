import ast
import re
import os

class SecurityGate:
    """Hardened Security Gate for validating AI-generated payloads using AST analysis."""

    PYTHON_FORBIDDEN_MODULES = {'os', 'subprocess', 'shlex', 'shutil', 'socket', 'requests', 'urllib', 'http', 'pickle', 'marshal'}
    PYTHON_FORBIDDEN_FUNCTIONS = {'eval', 'exec', 'getattr', 'setattr', '__import__', 'compile', 'input', 'globals', 'locals'}
    PYTHON_FORBIDDEN_ATTRIBUTES = {'__subclasses__', '__dict__', '__globals__', '__mro__', '__base__', '__class__', 'modules', 'environ'}

    @staticmethod
    def check_python(content):
        issues = []
        try:
            # Homoglyph / Non-ASCII check
            content.encode('ascii')
        except UnicodeEncodeError:
            issues.append("Python Violation: Non-ASCII characters detected (Homoglyph risk).")

        try:
            tree = ast.parse(content)
            for node in ast.walk(tree):
                # 1. Check Imports
                if isinstance(node, (ast.Import, ast.ImportFrom)):
                    modules = [n.name.split('.')[0] for n in (node.names if isinstance(node, ast.Import) else [ast.alias(name=node.module, asname=None)])]
                    for m in modules:
                        if m in SecurityGate.PYTHON_FORBIDDEN_MODULES:
                            issues.append(f"Python Violation: Forbidden module '{m}'")
                
                # 2. Check Function Calls
                if isinstance(node, ast.Call):
                    func_name = ""
                    if isinstance(node.func, ast.Name): func_name = node.func.id
                    elif isinstance(node.func, ast.Attribute): func_name = node.func.attr
                    
                    if func_name in SecurityGate.PYTHON_FORBIDDEN_FUNCTIONS:
                        issues.append(f"Python Violation: Forbidden function '{func_name}'")
                
                # 3. Check Attribute Access (e.g. obj.__subclasses__)
                if isinstance(node, ast.Attribute):
                    if node.attr in SecurityGate.PYTHON_FORBIDDEN_ATTRIBUTES:
                        issues.append(f"Python Violation: Forbidden attribute access '{node.attr}'")

                # 4. Check for Infinite Loops (DoS)
                if isinstance(node, ast.While):
                    if isinstance(node.test, ast.Constant) and node.test.value is True:
                        # Check if it has a break
                        has_break = any(isinstance(n, ast.Break) for n in ast.walk(node))
                        if not has_break:
                            issues.append("Python Violation: Potential infinite loop detected.")

        except SyntaxError:
            issues.append("Python Violation: Syntax Error in generated code.")
        
        # 5. Generic Secret Check
        issues.extend(SecurityGate._check_secrets(content))
        return issues

    @staticmethod
    def check_csharp(content):
        issues = []
        BANNED = [
            (r'System\.Reflection', "Reflection is forbidden."),
            (r'Process\.Start', "Process spawning is forbidden."),
            (r'File\.(Write|Delete|Append|Move|Copy)', "Direct file mutation is forbidden."),
            (r'Directory\.(Delete|Create|Move)', "Direct directory mutation is forbidden."),
            (r'System\.Net', "Direct networking is forbidden."),
            (r'InitializeOnLoad', "Side effects in constructors are forbidden."),
            (r'DllImport', "Native code execution is forbidden."),
            (r'dynamic\b', "Dynamic typing is forbidden."),
            (r'AppDomain', "AppDomain manipulation is forbidden."),
            (r'typeof\(.*?\)\.GetMethod', "Reflection via typeof is forbidden."),
            (r'unsafe\b', "Unsafe code blocks are forbidden."),
            (r'#if\b', "Conditional compilation is forbidden.")
        ]
        
        # Remove comments and strings before check
        clean = re.sub(r'//.*', '', content)
        clean = re.sub(r'/\*.*?\*/', '', clean, flags=re.DOTALL)
        clean = re.sub(r'"(\\.|[^"\\])*"', '""', clean)

        for pattern, reason in BANNED:
            if re.search(pattern, clean, re.IGNORECASE):
                issues.append(f"C# Violation: {reason}")
        return issues

    @staticmethod
    def _check_secrets(content):
        issues = []
        # Match long strings assigned to sensitive-looking variable names
        if re.search(r'(api[_-]?key|secret|password|token|auth)\s*[:=]\s*["\'][a-zA-Z0-9_\-\.] {10,}', content, re.I):
            issues.append("Security: Possible hardcoded secret detected.")
        # Match Google / AWS key patterns
        if re.search(r'ya29\.[a-zA-Z0-9_\-]{50,}', content) or re.search(r'AKIA[A-Z0-9]{16}', content):
            issues.append("Security: High-entropy secret pattern detected.")
        return issues