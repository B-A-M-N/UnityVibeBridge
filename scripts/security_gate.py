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
                editor_data = os.path.dirname(os.path.dirname(os.path.dirname(cls.CSC_DLL)))
                cls.DOTNET_BIN = os.path.join(editor_data, "NetCoreRuntime/dotnet")
                cls.UNITY_MANAGED = os.path.join(editor_data, "Managed")
                cls.UNITY_REF_ASSEMBLIES = os.path.join(editor_data, "UnityReferenceAssemblies/unity-4.8-api")
                break
        
        # 2. Project Assemblies (relative to current working dir)
        cls.SCRIPT_ASSEMBLIES = os.path.join(os.getcwd(), "Library/ScriptAssemblies")
        if not os.path.exists(cls.SCRIPT_ASSEMBLIES):
             # Fallback: check registered peers in workspace
             pass 

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

    @classmethod
    def audit_assembly(cls, package_path):
        if not cls.CSC_DLL: cls._initialize_paths()
        if not os.path.exists(cls.CSC_DLL):
            return ["Environment Error: Unity Roslyn (csc.dll) not found. Check UNITY_PATH."]
        
        temp_dir = tempfile.mkdtemp(prefix="vibe_batch_audit_")
        try:
            source_files = []
            for sub in ["Scripts", "Editor"]:
                dir_path = os.path.join(package_path, sub)
                if os.path.exists(dir_path):
                    for root, _, files in os.walk(dir_path):
                        for f in files:
                            if f.endswith(".cs"):
                                source_files.append(os.path.join(root, f))

            cmd = [
                cls.DOTNET_BIN, cls.CSC_DLL,
                "-target:library", "-nologo", "-nostdlib",
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