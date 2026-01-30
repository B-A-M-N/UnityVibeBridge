import ast
import re
import os

class SecurityGate:
    """Security Gate for validating AI-generated payloads using AST analysis."""

    PYTHON_FORBIDDEN_MODULES = {'os', 'subprocess', 'shlex', 'shutil', 'socket', 'requests', 'urllib', 'http'}
    PYTHON_FORBIDDEN_FUNCTIONS = {'eval', 'exec', 'getattr', 'setattr', '__import__', 'compile'}

    @staticmethod
    def check_python(content):
        issues = []
        try:
            tree = ast.parse(content)
            for node in ast.walk(tree):
                # Check Imports
                if isinstance(node, (ast.Import, ast.ImportFrom)):
                    modules = [n.name.split('.')[0] for n in (node.names if isinstance(node, ast.Import) else [ast.alias(name=node.module, asname=None)])]
                    for m in modules:
                        if m in SecurityGate.PYTHON_FORBIDDEN_MODULES:
                            issues.append(f"Python Violation: Forbidden module '{m}'")
                
                # Check Function Calls
                if isinstance(node, ast.Call):
                    func_name = ""
                    if isinstance(node.func, ast.Name): func_name = node.func.id
                    elif isinstance(node.func, ast.Attribute): func_name = node.func.attr
                    
                    if func_name in SecurityGate.PYTHON_FORBIDDEN_FUNCTIONS:
                        issues.append(f"Python Violation: Forbidden function '{func_name}'")
        except SyntaxError:
            issues.append("Python Violation: Syntax Error in generated code.")
        return issues

    @staticmethod
    def check_csharp(content):
        issues = []
        # C# remains pattern-based for now as we don't have a C# AST parser in Python environment easily
        # but we harden the patterns.
        BANNED = [
            (r'System\.Reflection', "Reflection is forbidden."),
            (r'Process\.Start', "Process spawning is forbidden."),
            (r'File\.(Write|Delete|Append|Move|Copy)', "Direct file mutation is forbidden."),
            (r'Directory\.(Delete|Create|Move)', "Direct directory mutation is forbidden."),
            (r'System\.Net', "Direct networking is forbidden."),
            (r'InitializeOnLoad', "Side effects in constructors are forbidden."),
            (r'DllImport', "Native code execution is forbidden.")
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
        if re.search(r'(api[_-]?key|secret|password|token)\s*[:=]\s*["\'][a-zA-Z0-9]{10,}', content, re.I):
            issues.append("Security: Possible hardcoded secret detected.")
        return issues
