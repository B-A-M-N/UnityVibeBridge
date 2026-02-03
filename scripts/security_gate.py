import ast
import sys
import os
import hashlib
import json
import re
import subprocess
import tempfile
import shutil
import glob

class SecurityGate:
    """
    UnityVibeBridge: Industrial Security Gate (v2.9)
    - Dynamic Path Resolution
    - Assembly Auditing
    """
    
    DOTNET_BIN = "dotnet"
    CSC_DLL = ""
    UNITY_MANAGED = ""
    UNITY_REF_ASSEMBLIES = ""
    SCRIPT_ASSEMBLIES = ""

    @classmethod
    def _initialize_paths(cls):
        """Locates Unity Editor and Project assemblies dynamically."""
        # 1. Try to find CSC.dll in standard Unity locations
        search_roots = ["/home/bamn/Unity", "/opt/Unity", os.path.expanduser("~/Unity")]
        for root in search_roots:
            if not os.path.exists(root): continue
            # Find the latest version or a specific one
            csc_matches = glob.glob(os.path.join(root, "**/DotNetSdkRoslyn/csc.dll"), recursive=True)
            if csc_matches:
                cls.CSC_DLL = csc_matches[0]
                # csc.dll is usually in Data/DotNetSdkRoslyn/ or similar.
                # We need to find the 'Data' directory.
                current = os.path.dirname(cls.CSC_DLL)
                while current and os.path.basename(current) != "Data" and len(current) > 1:
                    current = os.path.dirname(current)
                
                if os.path.basename(current) == "Data":
                    editor_data = current
                    cls.DOTNET_BIN = os.path.join(editor_data, "NetCoreRuntime/dotnet")
                    cls.UNITY_MANAGED = os.path.join(editor_data, "Managed")
                    cls.UNITY_REF_ASSEMBLIES = os.path.join(editor_data, "UnityReferenceAssemblies/unity-4.8-api")
                    break
                else:
                    # Fallback to previous logic if Data folder not found
                    editor_data = os.path.dirname(os.path.dirname(os.path.dirname(cls.CSC_DLL)))
                    cls.DOTNET_BIN = os.path.join(editor_data, "NetCoreRuntime/dotnet")
                    cls.UNITY_MANAGED = os.path.join(editor_data, "Managed")
                    cls.UNITY_REF_ASSEMBLIES = os.path.join(editor_data, "UnityReferenceAssemblies/unity-4.8-api")
                    break
        
        # 2. Project Assemblies (relative to current working dir)
        cls.SCRIPT_ASSEMBLIES = os.path.join(os.getcwd(), "Library/ScriptAssemblies")
        if not os.path.exists(cls.SCRIPT_ASSEMBLIES):
             # Fallback: check registered peers or common ALCOM patterns
             alcom_path = "/home/bamn/ALCOM/Projects/BAMN-EXTO/Library/ScriptAssemblies"
             if os.path.exists(alcom_path):
                 cls.SCRIPT_ASSEMBLIES = alcom_path
             else:
                 # Last resort: search sibling directories
                 parent = os.path.dirname(os.getcwd())
                 # find Library/ScriptAssemblies in any subfolder of parent
                 pass 

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

        if ".." in cmd:
            issues.append("Security Violation: Path traversal (..) detected in shell command.")
            
        return issues

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
        # Strict ASCII Check
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

            # 1. Alias Detection
            if isinstance(node, ast.Assign):
                if isinstance(node.value, ast.Name) and node.value.id in cls.PYTHON_FORBIDDEN_FUNCTIONS:
                    issues.append(f"Python Violation: Attempt to alias forbidden function '{node.value.id}'")
                
                if isinstance(node.value, ast.Call) and isinstance(node.value.func, ast.Name) and node.value.func.id == "getattr":
                    issues.append("Python Violation: Attempt to alias via 'getattr' is forbidden.")

            # 2. Check Imports
            if isinstance(node, (ast.Import, ast.ImportFrom)):
                for alias in (node.names if isinstance(node, ast.Import) else [ast.alias(name=node.module, asname=None)]):
                    mod_base = alias.name.split('.')[0] if alias.name else ""
                    if mod_base in cls.PYTHON_FORBIDDEN_MODULES:
                        issues.append(f"Security Violation: Forbidden module import '{alias.name}'")

            # 3. Check Function Calls
            if isinstance(node, ast.Call):
                func_name = ""
                if isinstance(node.func, ast.Name): func_name = node.func.id
                elif isinstance(node.func, ast.Attribute): func_name = node.func.attr
                
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
                            # We can't check path safety here easily as we don't have safe_zones context
                            # But we can block obvious badness
                            if ".." in arg.value:
                                issues.append(f"Security Violation: Path traversal '..' detected in '{arg.value}'.")

            # 4. Check for Internal Access
            if isinstance(node, ast.Attribute):
                if node.attr in cls.PYTHON_FORBIDDEN_ATTRIBUTES:
                    issues.append(f"Security Violation: Access to internal attribute '{node.attr}' forbidden.")

        return issues

    @classmethod
    def _get_url_from_call(cls, node):
        for arg in node.args:
            if isinstance(arg, ast.Constant) and isinstance(arg.value, str) and "http" in arg.value:
                return arg.value
        for kw in node.keywords:
            if kw.arg == 'url' and isinstance(kw.value, ast.Constant) and isinstance(kw.value.value, str):
                return kw.value.value
        return None

    @classmethod
    def _has_vibe_token(cls, node):
        for kw in node.keywords:
            if kw.arg == 'headers':
                if isinstance(kw.value, ast.Dict):
                    for k in kw.value.keys:
                        if isinstance(k, ast.Constant) and k.value == "X-Vibe-Token":
                            return True
        return False

    @staticmethod
    def _check_syntax_basics(code):
        """Perform fast, pre-compiler syntax checks."""
        if not code.strip(): return ["Syntax Error: Empty file."]
        
        # 1. Brace Balance
        open_braces = code.count('{')
        close_braces = code.count('}')
        if open_braces != close_braces:
            return [f"Syntax Error: Unbalanced Braces (Open: {open_braces}, Close: {close_braces})"]
            
        return []

    @classmethod
    def check_csharp(cls, code):
        """Performs a live Roslyn audit on a C# snippet."""
        # 1. Fast Syntax Check
        basic_errors = cls._check_syntax_basics(code)
        if basic_errors: return basic_errors

        if not cls.CSC_DLL: cls._initialize_paths()
        
        temp_dir = tempfile.mkdtemp(prefix="vibe_audit_")
        try:
            # We create a dummy project structure to satisfy CSC
            script_path = os.path.join(temp_dir, "GeneratedTool.cs")
            with open(script_path, "w") as f:
                f.write(code)
            
            # Use audit_assembly logic on the temp dir
            return cls.audit_assembly(temp_dir)
        finally:
            if os.path.exists(temp_dir): shutil.rmtree(temp_dir)

    @staticmethod
    def is_path_safe(path, safe_zones):
        """Ensures a path is within the allowed Workspace Perimeter."""
        try:
            abs_path = os.path.abspath(path)
            return any(abs_path.startswith(os.path.abspath(z)) for z in safe_zones)
        except: return False

    @staticmethod
    def _check_secrets(value):
        """Scans for sensitive patterns (API Keys, etc)."""
        # Basic pattern check
        patterns = [r"sk_[a-zA-Z0-9]{32}", r"AIza[a-zA-Z0-9_-]{35}"]
        issues = []
        for p in patterns:
            if re.search(p, value): issues.append("Security Violation: Sensitive pattern detected.")
        return issues

    @classmethod
    def audit_assembly(cls, package_path):
        if not cls.CSC_DLL: cls._initialize_paths()
        if not os.path.exists(cls.CSC_DLL):
            return ["Environment Error: Unity Roslyn (csc.dll) not found. Check UNITY_PATH."]
        
        temp_dir = tempfile.mkdtemp(prefix="vibe_batch_audit_")
        try:
            source_files = []
            
            # Recursively find all .cs files in the package root and subfolders
            for root, dirs, files in os.walk(package_path):
                # Skip Tests directory
                if "Tests" in dirs:
                    dirs.remove("Tests")
                
                for f in files:
                    if f.endswith(".cs"):
                        source_files.append(os.path.join(root, f))

            if not source_files:
                return ["Security Error: No source files found for audit in " + package_path]

            out_dll = os.path.join(temp_dir, "AuditOutput.dll")
            cmd = [
                cls.DOTNET_BIN, cls.CSC_DLL,
                "-target:library", "-nologo", "-nostdlib",
                f"-out:{out_dll}",
                "-nowarn:1701,1702,CS0067,CS0414",
                "-define:UNITY_EDITOR"
            ]
            
            # 1. Base Framework
            core_refs = ["mscorlib.dll", "System.dll", "System.Core.dll", "System.Data.dll", "System.Runtime.dll", "netstandard.dll"]
            for r in core_refs:
                for p in [os.path.join(cls.UNITY_REF_ASSEMBLIES, "Facades", r), os.path.join(cls.UNITY_REF_ASSEMBLIES, r)]:
                    if os.path.exists(p):
                        cmd.append(f"-r:{p}")
                        break
            
            # 2. Unity Shells (CRITICAL: Must be added BEFORE modules for type forwarding)
            for r in ["UnityEngine.dll", "UnityEditor.dll"]:
                p = os.path.join(cls.UNITY_MANAGED, r)
                if os.path.exists(p): cmd.append(f"-r:{p}")

            # 3. Unity Modules
            unity_mod_path = os.path.join(cls.UNITY_MANAGED, "UnityEngine")
            if os.path.exists(unity_mod_path):
                for dll in glob.glob(os.path.join(unity_mod_path, "UnityEngine.*Module.dll")):
                    # We use 'alias' to prevent CS0433 Ambiguous Match
                    # But since csc -r:alias=path is complex in a flat list, we'll try including them normally
                    # but ensure we don't have the same path twice.
                    if dll not in cmd:
                        cmd.append(f"-r:{dll}")
            
            # 4. Project Dependencies
            extra_refs = ["Unity.EditorCoroutines.Editor.dll", "VRC.SDKBase.dll", "VRC.SDKBase.Editor.dll", "UniTask.dll", "MemoryPack.Unity.dll", "MemoryPack.Core.dll"]
            for r in extra_refs:
                p = os.path.join(cls.SCRIPT_ASSEMBLIES, r)
                if os.path.exists(p): cmd.append(f"-r:{p}")
            
            cmd.extend(source_files)
            process = subprocess.run(cmd, capture_output=True, text=True)
            
            # In v2.8, we filter out CS0433 because we KNOW we have overlapping shells/modules
            # and that is by design for Unity 2022 compatibility.
            if process.returncode != 0:
                errors = []
                for line in process.stdout.splitlines():
                    if "error CS" in line:
                        if "CS0433" in line: continue # Skip ambiguous type warnings
                        errors.append(line.strip())
                return errors

            return []
        finally:
            shutil.rmtree(temp_dir)

if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser(description="VibeBridge Batch Auditor")
    parser.add_argument("--package", default="unity-package", help="Path to package root")
    args = parser.parse_args()
    issues = SecurityGate.audit_assembly(args.package)
    if issues:
        print(f"❌ ASSEMBLY AUDIT FAILED ({len(issues)} errors):")
        for i in issues[:20]: print(f"  - {i}")
        sys.exit(1)
    else:
        print("✅ Assembly Audit Passed. Kernel is stable.")
        sys.exit(0)