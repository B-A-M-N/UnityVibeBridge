import ast
import sys
import os
import hashlib
import json

class SecurityGate:
    """
    A robust security auditor with AST analysis and a Trusted Signature whitelist.
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

        errors = []
        node_count = 0
        
        for node in ast.walk(tree):
            node_count += 1
            if node_count > 5000:
                return ["Security Violation: File too complex to safely audit."]

            # 0. DoS Protection (Loops & Memory)
            if isinstance(node, (ast.While, ast.For)):
                if cls._is_infinite_loop(node):
                    errors.append("Security Violation: Potential infinite loop detected (DoS risk).")
            
            if isinstance(node, ast.Constant):
                if isinstance(node.value, (int, float)) and node.value > 10**12:
                    errors.append("Security Violation: Extremely large numeric constant (DoS risk).")
                if isinstance(node.value, (bytes, str)) and len(node.value) > 10**6:
                    errors.append("Security Violation: Extremely large data literal (DoS risk).")

            if isinstance(node, ast.BinOp):
                if isinstance(node.op, ast.Mult):
                    # Detect 'A' * 10**8 or similar
                    if isinstance(node.right, ast.Constant) and isinstance(node.right.value, int) and node.right.value > 10**6:
                        errors.append("Security Violation: Potential large memory allocation detected (DoS risk).")
                if isinstance(node.op, ast.Pow):
                    # Detect 10**8 or similar
                    if isinstance(node.right, ast.Constant) and isinstance(node.right.value, int) and node.right.value > 6:
                        errors.append("Security Violation: Potential large numeric computation detected (DoS risk).")

            # 1. Check Imports
            if isinstance(node, (ast.Import, ast.ImportFrom)):
                for alias in (node.names if isinstance(node, ast.Import) else [ast.alias(name=node.module, asname=None)]):
                    mod_base = alias.name.split('.')[0]
                    if mod_base in cls.PYTHON_FORBIDDEN_MODULES:
                        errors.append(f"Security Violation: Forbidden module import '{alias.name}'")

            # 2. Check Function Calls
            if isinstance(node, ast.Call):
                func_name = cls._resolve_func_name(node.func)
                if func_name in cls.PYTHON_FORBIDDEN_FUNCTIONS:
                    errors.append(f"Security Violation: Use of forbidden function '{func_name}'")
                
                # Network & Bridge Check
                if func_name in ('get', 'post', 'request'):
                    url = cls._get_url_from_call(node)
                    if url:
                        if "8085" in url or "localhost" in url or "127.0.0.1" in url:
                            # Mandatory Token Check for Local Unity Bridge
                            if not cls._has_vibe_token(node):
                                errors.append("Security Violation: Local Unity requests MUST include 'X-Vibe-Token' header.")
                        elif not any(host in url for host in cls.ALLOWED_HOSTS):
                            errors.append(f"Security Violation: External network request to '{url}' blocked.")
                
                # File System Safety Check
                if func_name in ('open', 'write', 'Path', 'mkdir', 'remove', 'rmdir'):
                    for arg in node.args:
                        if isinstance(arg, ast.Constant) and isinstance(arg.value, str):
                            if not cls._is_path_safe(arg.value):
                                errors.append(f"Security Violation: Access to forbidden path '{arg.value}' blocked.")

            # 3. Check for Internal Access
            if isinstance(node, ast.Attribute):
                if node.attr in cls.PYTHON_FORBIDDEN_ATTRIBUTES:
                    errors.append(f"Security Violation: Access to internal attribute '{node.attr}' forbidden.")

            # 4. AST-Based Secret Detection (More robust than Regex)
            if isinstance(node, ast.Assign):
                for target in node.targets:
                    if isinstance(target, ast.Name):
                        var_name = target.id.upper()
                        if any(k in var_name for k in ['KEY', 'SECRET', 'TOKEN', 'PASSWORD', 'AUTH', 'CREDENTIAL']):
                            # Block if a long string literal (or concat) is assigned to a sensitive variable name
                            if cls._is_sensitive_value(node.value):
                                errors.append(f"Security Violation: Potential hardcoded secret in variable '{target.id}'")

            # 5. Block access to forbidden names (e.g. __builtins__)
            if isinstance(node, ast.Name):
                if node.id in cls.PYTHON_FORBIDDEN_FUNCTIONS:
                    errors.append(f"Security Violation: Access to forbidden name '{node.id}'")

        return errors

    @classmethod
    def _check_secrets(cls, content):
        """Scans for potential hardcoded secrets/credentials."""
        import re
        # Common patterns for keys/tokens
        SECRET_PATTERNS = [
            r'(?i)(api_key|secret|token|password|auth|credential|pk_live|sk_live)\s*[:=]\s*["\'][a-z0-9_\-\.]{10,}["\']',
            r'(?i)ya29\.[a-z0-9_\-]{50,}', # Google OAuth
            r'(?i)AKIA[A-Z0-9]{16}',       # AWS Access Key
        ]
        
        for pattern in SECRET_PATTERNS:
            if re.search(pattern, content):
                return ["Security Violation: Potential hardcoded secret or credential detected."]
        return []

    @classmethod
    def _is_sensitive_value(cls, node, in_collection=False):
        """Recursively checks if an expression resolves to a sensitive string literal."""
        threshold = 4 if in_collection else 8
        if isinstance(node, ast.Constant) and isinstance(node.value, str):
            return len(node.value) >= threshold
        if isinstance(node, ast.BinOp) and isinstance(node.op, ast.Add):
            return cls._is_sensitive_value(node.left, in_collection) or cls._is_sensitive_value(node.right, in_collection)
        if isinstance(node, ast.JoinedStr): # f-strings
            return True
        if isinstance(node, ast.List):
            return any(cls._is_sensitive_value(elt, True) for elt in node.elts)
        if isinstance(node, ast.Call):
            return any(cls._is_sensitive_value(arg, True) for arg in node.args) or \
                   (isinstance(node.func, ast.Attribute) and cls._is_sensitive_value(node.func.value, True))
        if isinstance(node, ast.Name):
            return True
        return False

    @classmethod
    def _resolve_func_name(cls, node):
        if isinstance(node, ast.Name): return node.id
        if isinstance(node, ast.Attribute): return node.attr
        if isinstance(node, ast.Subscript):
            # Handle __builtins__['eval'](...)
            if isinstance(node.slice, ast.Constant) and isinstance(node.slice.value, str):
                return node.slice.value
        return ""

    @classmethod
    def _get_url_from_call(cls, node):
        # Try to find a string constant in args or keywords
        for arg in node.args:
            if isinstance(arg, ast.Constant) and isinstance(arg.value, str) and "http" in arg.value:
                return arg.value
        for kw in node.keywords:
            if kw.arg == 'url' and isinstance(kw.value, ast.Constant) and isinstance(kw.value.value, str):
                return kw.value.value
        return None

    @staticmethod
    def _is_path_safe(path):
        """Checks if a path is inside allowed subdirs, not a security file, and not a symlink/meta."""
        forbidden = ("security_gate.py", "trusted_signatures.json", "metadata/", ".gemini_security/", ".meta")
        if any(f in path for f in forbidden):
            return False

        # --- NEW: Symlink Hijack Protection ---
        if os.path.islink(path):
            return False

        allowed_subdirs = ("captures", "optimizations", "logs", "unity-package")
        if os.path.isabs(path):
            return path.startswith(os.getcwd())
        
        # Allow relative paths that start with an allowed subdir
        if any(path.startswith(subdir) for subdir in allowed_subdirs):
            return ".." not in path
            
        if ".." in path:
            return False
        return True

    @classmethod
    def _is_infinite_loop(cls, node):
        """Detects trivial infinite loops like 'while True'."""
        if isinstance(node, ast.While):
            if isinstance(node.test, ast.Constant) and node.test.value is True:
                # If there is no 'break' in the body, it's definitely infinite
                has_break = any(isinstance(n, ast.Break) for n in ast.walk(node))
                return not has_break
        return False

    # --- C# Security (Lexical/Semantic Based) ---
    # We avoid Regex. We strip strings/comments first, then scan tokens.

    CS_FORBIDDEN_NAMESPACES = {
        'System.Diagnostics', 'System.IO.Compression', 'System.Net.Sockets', 
        'System.Reflection', 'System.Type', 'System.Environment', 'System.Threading',
        'System.Reflection.Emit', 'System.Runtime.InteropServices',
        'UnityEditor.Build', 'UnityEditor.Compilation', 'UnityEditor.Callbacks'
    }
    CS_FORBIDDEN_METHODS = {
        'Process.Start', 'File.Delete', 'Directory.Delete', 'UnityWebRequest.Get', 'UnityWebRequest.Post', 
        'GetType', 'GetMethod', 'Invoke', 'DefineDynamicAssembly', 'Assembly.Load',
        'AssetDatabase.DeleteAsset', 'AssetDatabase.MoveAsset', 'AssetDatabase.ImportAsset',
        'EditorPrefs.Set', 'PlayerSettings.Set', 'BuildPipeline.BuildPlayer', 'SessionState.Set',
        'EditorApplication.update'
    }
    CS_FORBIDDEN_KEYWORDS = {
        'DllImport', 'unsafe', 'IntPtr', 'ModuleInitializer', 'dynamic', 'fixed'
    }
    CS_FORBIDDEN_ATTRIBUTES = {
        'InitializeOnLoad', 'DidReloadScripts', 'OnOpenAsset', 'PostProcessBuild', 'PostProcessScene',
        'MenuItem', 'ContextMenu', 'ExecuteInEditMode'
    }
    CS_FORBIDDEN_LIFECYCLE_METHODS = {
        'OnEnable', 'OnValidate', 'OnAfterDeserialize', 'Awake', 'OnDisable', 'OnDestroy', 'Start', 'Update'
    }

    # --- NEW: Compiler & Entropy Protections ---
    CS_FORBIDDEN_COMPILER_DIRECTIVES = {'#if', '#define', '#undef', '#pragma'}

    @classmethod
    def _check_entropy(cls, name):
        """Detects unusually high entropy in identifiers (potential covert channels)."""
        if len(name) > 64: return True
        # Simple heuristic: excessive numbers/hex-like patterns in long names
        import re
        if len(name) > 20 and re.search(r'[0-9a-f]{8,}', name.lower()):
            return True
        return False

    @classmethod
    def check_csharp(cls, code):
        # 0. Check Whitelist first
        if cls.is_trusted(code):
            return []

        clean_code = cls._strip_cs_noise(code)
        errors = []

        # 1. Namespace Check
        for ns in cls.CS_FORBIDDEN_NAMESPACES:
            if ns in clean_code:
                # Use word boundaries to reduce false positives
                import re
                if re.search(r'\b' + re.escape(ns) + r'\b', clean_code):
                    errors.append(f"Security Violation: Forbidden namespace '{ns}' detected.")

        # 2. Method/API Check
        for method in cls.CS_FORBIDDEN_METHODS:
            if method in clean_code:
                import re
                if re.search(r'\b' + re.escape(method) + r'\b', clean_code):
                    errors.append(f"Security Violation: Forbidden API call '{method}' detected.")

        # 3. Attribute/Keyword/Lifecycle Check
        for category, forbidden_set in [
            ("Attribute", cls.CS_FORBIDDEN_ATTRIBUTES),
            ("Keyword", cls.CS_FORBIDDEN_KEYWORDS),
            ("Lifecycle", cls.CS_FORBIDDEN_LIFECYCLE_METHODS)
        ]:
            for item in forbidden_set:
                if item in clean_code:
                    import re
                    if re.search(r'\b' + re.escape(item) + r'\b', clean_code):
                        errors.append(f"Security Violation: Forbidden {category} '{item}' detected.")
            

        # 4. Compiler Boundary Protection
        for directive in cls.CS_FORBIDDEN_COMPILER_DIRECTIVES:
            if directive in clean_code:
                errors.append(f"Security Violation: Conditional compilation directive '{directive}' is forbidden to prevent boundary abuse.")

        # 5. Identifier Entropy Check
        import re
        identifiers = re.findall(r'\b[a-zA-Z_][a-zA-Z0-9_]*\b', clean_code)
        for id_name in identifiers:
            if cls._check_entropy(id_name):
                errors.append(f"Security Violation: High-entropy identifier '{id_name}' detected (Potential covert channel).")

        return errors

    

    @staticmethod
    def _strip_cs_noise(code):
        """Removes strings and comments from C# code and decodes escapes."""
        # Step 0: Handle escaped newlines (e.g. System.\nDiagnostics)
        code = code.replace("\\n", "\n").replace("\\r", "\r")
        
        # Step 0.5: Decode Unicode escapes (e.g. \u0061)
        import re
        code = re.sub(r'\\u([0-9a-fA-F]{4})', lambda m: chr(int(m.group(1), 16)), code)

        result = []
        i = 0
        in_string = False
        in_block_comment = False
        in_line_comment = False
        
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
                if char == '\\': i += 2 # Skip escaped chars
                elif char == '"': in_string = False; i += 1
                else: i += 1
                continue
            
            if char == '/' and next_char == '/': in_line_comment = True; i += 2; continue
            if char == '/' and next_char == '*': in_block_comment = True; i += 2; continue
            if char == '"': in_string = True; i += 1; continue
            
            result.append(char)
            i += 1
        return "".join(result)

    # --- Shell Security (Whitelist Based) ---

    SHELL_WHITELIST = {
        'git', 'python', 'python3', 'ls', 'cat', 'mkdir', 'rm', 'cp', 'mv', 
        'grep', 'find', 'pip', 'pip3', 'cargo', 'rustc', 'docker'
    }
    FORBIDDEN_SHELL_PATTERNS = {
        'curl', 'wget', 'ssh', 'nc', 'bash -i', 'sh -i', '>', '>>', '|', '&&', ';', '`', '$(',
        'API_KEY', 'TOKEN', 'gcloud', 'env', 'printenv', '.config',
        'core.pager', 'alias.', 'ext::', '--open', '-m pty', '-m http', 'install .',
        'LD_', 'PYTHONPATH', 'PERL5LIB', 'RUBYLIB', '*', '?', '[', ']', '{', '}'
    }

    @classmethod
    def _has_vibe_token(cls, node):
        """Checks if a call node includes the X-Vibe-Token header."""
        for kw in node.keywords:
            if kw.arg == 'headers':
                # Check for dict: {'X-Vibe-Token': ...}
                if isinstance(kw.value, ast.Dict):
                    for k in kw.value.keys:
                        if isinstance(k, ast.Constant) and k.value == "X-Vibe-Token":
                            return True
        return False

    @classmethod
    def check_shell(cls, cmd):
        parts = cmd.strip().split()
        if not parts: return []
        
        errors = []
        base_cmd = parts[0]
        
        if base_cmd not in cls.SHELL_WHITELIST:
            errors.append(f"Security Violation: Shell command '{base_cmd}' is not in the whitelist.")
        
        # --- Mandatory Token Check for Local Bridge (curl/wget) ---
        if "8085" in cmd or "localhost" in cmd:
            if "X-Vibe-Token" not in cmd:
                errors.append("Security Violation: Local Unity requests via shell MUST include 'X-Vibe-Token' header.")

        # --- NEW: Recursive Bridge Auditing ---
        if base_cmd in ('python', 'python3') and '-c' in parts:
            try:
                idx = parts.index('-c')
                if idx + 1 < len(parts):
                    python_code = parts[idx + 1]
                    # Strip quotes if present
                    if (python_code.startswith("'") and python_code.endswith("'")) or \
                       (python_code.startswith('"') and python_code.endswith('"')):
                        python_code = python_code[1:-1]
                    
                    bridge_errors = cls.check_python(python_code)
                    for err in bridge_errors:
                        errors.append(f"Bridge Violation (Python): {err}")
            except: pass

        # --- PROTECTION: Prevent Credential Exfiltration ---
        for pattern in cls.FORBIDDEN_SHELL_PATTERNS:
            if pattern in cmd:
                errors.append(f"Security Violation: Forbidden keyword or pattern '{pattern}' detected.")

        # --- PROTECTION: Prevent Agent from Trusting its own code ---
        if "--trust" in cmd or "trusted_signatures.json" in cmd:
            errors.append("Security Violation: AI Agent is forbidden from modifying security trust settings.")

        # Check for redirection or piping
        for pattern in cls.FORBIDDEN_SHELL_PATTERNS:
            if pattern in cmd:
                # Special case for legitimate redirection if needed, but safer to block by default
                errors.append(f"Security Violation: Use of forbidden shell pattern '{pattern}'")

        # Path traversal check
        if ".." in cmd:
            errors.append("Security Violation: Path traversal (..) detected in shell command.")
            
        return errors

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
        print(f"python3 security_gate.py {args.file} --trust")
        sys.exit(1)
    else:
        print("✅ Security Audit Passed.")
        sys.exit(0)
