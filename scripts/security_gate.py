import ast
import sys
import os
import hashlib
import json
import re

class SecurityGate:
    """
    A robust security auditor with AST analysis and a Trusted Signature whitelist.
    v1.5: Final Hardening for Kernel v1.2.1
    """
    
    TRUSTED_FILE = "trusted_signatures.json"

    @classmethod
    def _get_content_hash(cls, content):
        """Generates a stable SHA-256 hash for code content."""
        return hashlib.sha256(content.strip().encode('utf-8')).hexdigest()

    @classmethod
    def is_trusted(cls, content):
        """Checks if this exact code block has been previously approved."""
        if not os.path.exists(cls.TRUSTED_FILE):
            return False
        
        content_hash = cls._get_content_hash(content)
        try:
            with open(cls.TRUSTED_FILE, "r") as f:
                trusted = json.load(f)
                return content_hash in trusted
        except:
            return False

    @classmethod
    def trust_content(cls, content, reason="User Approved"):
        """Adds a content hash to the persistent whitelist."""
        content_hash = cls._get_content_hash(content)
        trusted = {}
        if os.path.exists(cls.TRUSTED_FILE):
            try:
                with open(cls.TRUSTED_FILE, "r") as f:
                    trusted = json.load(f)
            except: pass
        
        trusted[content_hash] = {
            "timestamp": __import__("datetime").datetime.now().isoformat(),
            "reason": reason
        }
        
        with open(cls.TRUSTED_FILE, "w") as f:
            json.dump(trusted, f, indent=2)
        return True

    PYTHON_FORBIDDEN_MODULES = {
        'os', 'subprocess', 'shlex', 'shutil', 'socket', 'posix', 'pty',
        'google.auth', 'google.oauth2', 'requests.auth', 'importlib', 'builtins',
        'ctypes', 'gc', 'marshal', 'pickle', 'types', 'inspect', 'shelve',
        'http', 'urllib', 'ftplib', 'telnetlib', 'smtplib', 'sys'
    }
    PYTHON_FORBIDDEN_FUNCTIONS = {
        'eval', 'exec', 'getattr', 'setattr', 'globals', 'locals', 'input', '__import__', '__builtins__', 'compile',
        'gethostbyname', 'getaddrinfo', 'create_connection'
    }
    PYTHON_FORBIDDEN_ATTRIBUTES = {
        'environ', 'getenv', '__globals__', '__subclasses__', '__mro__', '__base__', '__class__', '__code__',
        '__getattribute__', '__dict__', 'modules'
    }
    ALLOWED_HOSTS = {'localhost', '127.0.0.1', '0.0.0.0', '::1'}

    @classmethod
    def check_python(cls, code):
        # 0. Check Whitelist first
        if cls.is_trusted(code):
            return []

        # Strict ASCII Check to defeat ALL Homoglyph and non-visible character attacks
        try:
            code.encode('ascii')
        except UnicodeEncodeError:
            return ["Security Violation: Non-ASCII characters detected. Potential Homoglyph attack."]

        # Scanner DoS Protection
        if len(code) > 50000:
            return ["Security Violation: File too large to safely audit."]

        try:
            tree = ast.parse(code)
        except SyntaxError as e:
            return [f"Syntax Error: {str(e)}"]

        issues = []
        node_count = 0
        
        for node in ast.walk(tree):
            node_count += 1
            if node_count > 5000:
                return ["Security Violation: File too complex to safely audit."]

            # 0. DoS Protection (Loops & Memory)
            if isinstance(node, (ast.While, ast.For)):
                if cls._is_infinite_loop(node):
                    issues.append("Security Violation: Potential infinite loop detected (DoS risk).")
            
            if isinstance(node, ast.Constant):
                if isinstance(node.value, (int, float)) and node.value > 10**12:
                    issues.append("Security Violation: Extremely large numeric constant (DoS risk).")
                if isinstance(node.value, (bytes, str)) and len(node.value) > 10**6:
                    issues.append("Security Violation: Extremely large data literal (DoS risk).")

            if isinstance(node, ast.BinOp):
                if isinstance(node.op, ast.Mult):
                    if isinstance(node.right, ast.Constant) and isinstance(node.right.value, int) and node.right.value > 10**6:
                        issues.append("Security Violation: Potential large memory allocation detected (DoS risk).")
                if isinstance(node.op, ast.Pow):
                    if isinstance(node.right, ast.Constant) and isinstance(node.right.value, int) and node.right.value > 6:
                        issues.append("Security Violation: Potential large numeric computation detected (DoS risk).")

            # 1. Alias Detection
            if isinstance(node, ast.Assign):
                if isinstance(node.value, ast.Name) and node.value.id in cls.PYTHON_FORBIDDEN_FUNCTIONS:
                    issues.append(f"Python Violation: Attempt to alias forbidden function '{node.value.id}'")
                
                if isinstance(node.value, ast.Call) and cls._resolve_func_name(node.value.func) == "getattr":
                    issues.append("Python Violation: Attempt to alias via 'getattr' is forbidden.")

                for target in node.targets:
                    if isinstance(target, ast.Name):
                        var_name = target.id.upper()
                        if any(k in var_name for k in ['KEY', 'SECRET', 'TOKEN', 'PASSWORD', 'AUTH', 'CREDENTIAL']):
                            if cls._is_sensitive_value(node.value):
                                issues.append(f"Security Violation: Potential hardcoded secret in variable '{target.id}'")

            # 2. Check Imports
            if isinstance(node, (ast.Import, ast.ImportFrom)):
                for alias in (node.names if isinstance(node, ast.Import) else [ast.alias(name=node.module, asname=None)]):
                    mod_base = alias.name.split('.')[0]
                    if mod_base in cls.PYTHON_FORBIDDEN_MODULES:
                        issues.append(f"Security Violation: Forbidden module import '{alias.name}'")

            # 3. Check Function Calls
            if isinstance(node, ast.Call):
                func_name = cls._resolve_func_name(node.func)
                if func_name in cls.PYTHON_FORBIDDEN_FUNCTIONS:
                    issues.append(f"Security Violation: Use of forbidden function '{func_name}'")
                
                # Network & Bridge Check
                if func_name in ('get', 'post', 'request'):
                    url = cls._get_url_from_call(node)
                    if url:
                        if "8085" in url or "localhost" in url or "127.0.0.1" in url:
                            if not cls._has_vibe_token(node):
                                issues.append("Security Violation: Local Unity requests MUST include 'X-Vibe-Token' header.")
                        elif not any(host in url for host in cls.ALLOWED_HOSTS):
                            issues.append(f"Security Violation: External network request to '{url}' blocked.")
                
                # File System Safety Check
                if func_name in ('open', 'write', 'Path', 'mkdir', 'remove', 'rmdir'):
                    for arg in node.args:
                        if isinstance(arg, ast.Constant) and isinstance(arg.value, str):
                            if not cls._is_path_safe(arg.value):
                                issues.append(f"Security Violation: Access to forbidden path '{arg.value}' blocked.")

            # 4. Check for Internal Access
            if isinstance(node, ast.Attribute):
                if node.attr in cls.PYTHON_FORBIDDEN_ATTRIBUTES:
                    issues.append(f"Security Violation: Access to internal attribute '{node.attr}' forbidden.")

        issues.extend(cls._check_secrets(code))
        return issues

    @classmethod
    def _resolve_func_name(cls, node):
        if isinstance(node, ast.Name): return node.id
        if isinstance(node, ast.Attribute): return node.attr
        if isinstance(node, ast.Subscript):
            if isinstance(node.slice, ast.Constant) and isinstance(node.slice.value, str):
                return node.slice.value
        return ""

    @classmethod
    def _get_url_from_call(cls, node):
        for arg in node.args:
            if isinstance(arg, ast.Constant) and isinstance(arg.value, str) and "http" in arg.value:
                return arg.value
        for kw in node.keywords:
            if kw.arg == 'url' and isinstance(kw.value, ast.Constant) and isinstance(kw.value.value, str):
                return kw.value.value
        return None

    @staticmethod
    def _is_path_safe(path):
        # 1. Normalize path to prevent '..' or homoglyph bypass
        abs_path = os.path.normpath(path)
        if abs_path.startswith("..") or abs_path.startswith("/") or ":" in abs_path:
            return False

        # 2. Prevent access to core security/system files
        forbidden = ("security_gate.py", "trusted_signatures.json", "vibe_status.json", "vibe_settings.json", "HUMAN_ONLY", ".meta", ".git")
        if any(f in abs_path for f in forbidden): 
            return False
        
        # 3. Allowlisted Creation Perimeters
        allowed_subdirs = ("captures", "optimizations", "logs", "unity-package", "vibe_queue", "metadata")
        # Ensure the path is actually INSIDE one of the allowed subdirs
        return any(abs_path.startswith(subdir + os.sep) or abs_path == subdir for subdir in allowed_subdirs)

    @classmethod
    def _is_infinite_loop(cls, node):
        if isinstance(node, ast.While):
            if isinstance(node.test, ast.Constant) and node.test.value is True:
                has_break = any(isinstance(n, ast.Break) for n in ast.walk(node))
                return not has_break
        return False

    @classmethod
    def _is_sensitive_value(cls, node, in_collection=False):
        threshold = 4 if in_collection else 10
        if isinstance(node, ast.Constant) and isinstance(node.value, str):
            return len(node.value) >= threshold
        if isinstance(node, ast.BinOp) and isinstance(node.op, ast.Add):
            return cls._is_sensitive_value(node.left, in_collection) or cls._is_sensitive_value(node.right, in_collection)
        if isinstance(node, ast.JoinedStr): return True
        if isinstance(node, ast.List):
            return any(cls._is_sensitive_value(elt, True) for elt in node.elts)
        if isinstance(node, ast.Call):
            return any(cls._is_sensitive_value(arg, True) for arg in node.args)
        return False

    @staticmethod
    def check_csharp(content):
        issues = []
        clean = SecurityGate._strip_cs_noise(content)
        
        BANNED = [
            (r'System\.Reflection', "Reflection"), (r'Process\.Start', "Spawning"),
            (r'File\.(Write|Delete|Append|Move|Copy)', "File Mutation"),
            (r'Directory\.', "Directory Mutation"), (r'System\.Net', "Networking"),
            (r'InitializeOnLoad', "Constructor Side-effects"), (r'DllImport', "Native Code"),
            (r'dynamic\b', "Dynamic Typing"), (r'typeof\(.*\)\.GetMethod', "Type-based Reflection"),
            (r'AppDomain', "AppDomain manipulation"), (r'unsafe\b', "Unsafe code"),
            (r'ModuleInitializer', "Module Initializer Hijack"), (r'Type\.GetType', "Reflection")
        ]
        
        for pattern, reason in BANNED:
            if re.search(pattern, clean, re.IGNORECASE):
                issues.append(f"C# Violation: {reason} is forbidden.")
        
        # Identifier Entropy Check (Covert Channel Protection)
        identifiers = re.findall(r'\b[a-zA-Z_][a-zA-Z0-9_]*\b', clean)
        for id_name in identifiers:
            if SecurityGate._check_entropy(id_name):
                issues.append(f"Security Violation: High-entropy identifier '{id_name}' detected.")
                
        return issues

    @staticmethod
    def _strip_cs_noise(code):
        """Removes strings and comments from C# code."""
        code = code.replace("\\n", "\n").replace("\\r", "\r")
        code = re.sub(r'\\u([0-9a-fA-F]{4})', lambda m: chr(int(m.group(1), 16)), code)
        result = []
        i = 0
        in_string = in_block_comment = in_line_comment = False
        while i < len(code):
            char = code[i]
            next_char = code[i+1] if i + 1 < len(code) else ""
            if in_line_comment:
                if char == '\n': in_line_comment = False
                i += 1; continue
            if in_block_comment:
                if char == '*' and next_char == '/': in_block_comment = False; i += 2
                else: i += 1
                continue
            if in_string:
                if char == '\\': i += 2
                elif char == '"': in_string = False; i += 1
                else: i += 1
                continue
            if char == '/' and next_char == '/': in_line_comment = True; i += 2; continue
            if char == '/' and next_char == '*': in_block_comment = True; i += 2; continue
            if char == '"': in_string = True; i += 1; continue
            result.append(char)
            i += 1
        return "".join(result)

    @staticmethod
    def _check_entropy(name):
        """Detects unusually high entropy in identifiers."""
        if len(name) > 64: return True
        if len(name) > 20 and re.search(r'[0-9a-f]{8,}', name.lower()): return True
        return False

    @classmethod
    def _check_secrets(cls, content):
        issues = []
        SECRET_PATTERNS = [
            r'(?i)(api[_-]?key|secret|token|password|auth)\s*[:=]\s*["\'][a-zA-Z0-9_\-\.] {10,}["\']',
            r'(?i)ya29\.[a-zA-Z0-9_\-]{50,}',
            r'(?i)AKIA[A-Z0-9]{16}',
        ]
        for pattern in SECRET_PATTERNS:
            if re.search(pattern, content):
                issues.append("Security Violation: Possible hardcoded secret detected.")
        return issues

    @classmethod
    def _has_vibe_token(cls, node):
        for kw in node.keywords:
            if kw.arg == 'headers':
                if isinstance(kw.value, ast.Dict):
                    for k in kw.value.keys:
                        if isinstance(k, ast.Constant) and k.value == "X-Vibe-Token":
                            return True
        return False

    @classmethod
    def check_shell(cls, cmd):
        parts = cmd.strip().split()
        if not parts: return []
        
        issues = []
        SHELL_WHITELIST = {'git', 'python', 'python3', 'ls', 'cat', 'mkdir', 'rm', 'cp', 'mv', 'grep', 'find', 'pip', 'pip3', 'cargo', 'rustc', 'docker'}
        base_cmd = parts[0]
        
        if base_cmd not in SHELL_WHITELIST:
            issues.append(f"Security Violation: Shell command '{base_cmd}' is not in the whitelist.")
        
        if "8085" in cmd or "localhost" in cmd:
            if "X-Vibe-Token" not in cmd:
                issues.append("Security Violation: Local Unity requests via shell MUST include 'X-Vibe-Token' header.")

        FORBIDDEN_SHELL_PATTERNS = {'curl', 'wget', 'ssh', 'nc', 'bash -i', 'sh -i', '>', '>>', '|', '&&', ';', '`', '$(', 'ext::', '--open', '-m pty', 'core.pager', 'install .', '*', '?', '[', ']', '{', '}', 'LD_', 'PYTHONPATH'}
        for pattern in FORBIDDEN_SHELL_PATTERNS:
            if pattern in cmd:
                issues.append(f"Security Violation: Forbidden shell pattern '{pattern}' detected.")

        if "--trust" in cmd or "trusted_signatures.json" in cmd:
            issues.append("Security Violation: AI Agent is forbidden from modifying security trust settings.")

        if ".." in cmd:
            issues.append("Security Violation: Path traversal (..) detected in shell command.")
            
        return issues

if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser(description="UnityVibeBridge Security Gate")
    parser.add_argument("file", help="File to audit")
    parser.add_argument("--trust", action="store_true", help="Manually trust this file's content")
    
    args = parser.parse_args()
    
    if not os.path.exists(args.file):
        print(f"File not found: {args.file}")
        sys.exit(1)
        
    with open(args.file, 'r') as f:
        content = f.read()
    
    if args.trust:
        SecurityGate.trust_content(content, reason="Manual Human Approval")
        print(f"✅ Content hash for '{args.file}' is now TRUSTED.")
        sys.exit(0)
        
    ext = os.path.splitext(args.file)[1]
    issues = []
    
    if ext == '.py': issues = SecurityGate.check_python(content)
    elif ext == '.cs': issues = SecurityGate.check_csharp(content)
    
    if issues:
        print("❌ SECURITY AUDIT FAILED:")
        print("\n".join(issues))
        print("\nIf you are sure this is safe, run the following command manually:")
        print(f"python3 scripts/security_gate.py {args.file} --trust")
        sys.exit(1)
    else:
        print("✅ Security Audit Passed.")
        sys.exit(0)