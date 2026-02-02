import os
import json
import requests

def register_management_tools(engine):
    mcp = engine.mcp

    @mcp.tool()
    def emergency_self_heal() -> str:
        """
        [EMERGENCY] Reads the direct Unity Editor.log to find compiler errors.
        If an error is found in a VibeBridge script, it offers to remove it to restore kernel stability.
        Use this if the Bridge HTTP server is offline.
        """
        from scripts.emergency_log_tailer import find_compiler_errors
        import re
        
        errors = find_compiler_errors()
        if not errors:
            return "✅ No compiler errors detected in the direct Unity logs. Kernel should be stable."
        
        report = ["❌ Kernel Instability Detected via Editor.log:"]
        report.extend(errors[:10])
        
        # Look for the most common culprit: a recently written script
        offending_files = []
        for e in errors:
            match = re.search(r"(Assets/[^\(]+.cs)", e)
            if match:
                f = match.group(1)
                if "VibeBridge" in f and f not in offending_files:
                    offending_files.append(f)
        
        if offending_files:
            report.append(f"\n⚠️ Proposed Healing Action: Remove offending scripts: {', '.join(offending_files)}")
            # We don't auto-delete without a second confirmation turn, but we provide the command
            report.append("👉 To fix, run: rm " + " ".join(offending_files))
            
        return "\n".join(report)

    @mcp.tool()
    def mutate_script(path: str, code: str) -> str:
        """
        [HARDENED] Writes a C# script to the project after performing a mandatory Roslyn audit.
        Triggers AssetDatabase.Refresh() automatically on success.
        """
        from scripts.security_gate import SecurityGate
        
        # 1. Mandatory Pre-Flight Roslyn Audit
        errors = SecurityGate.check_csharp(code)
        if errors:
            return json.dumps({
                "error": "ROSLYN_AUDIT_FAILED",
                "details": errors,
                "message": "Write blocked: C# code contains compilation errors."
            })

        # 2. Write to disk
        abs_path = engine.workspace.resolve_path(path)
        if not abs_path: return "Error: Path resolution failed."
        
        os.makedirs(os.path.dirname(abs_path), exist_ok=True)
        with open(abs_path, "w") as f:
            f.write(code)
            
        # 3. Automated Gated Refresh
        if path.endswith(".cs") or path.endswith(".meta"):
            engine.unity_request("system/refresh")
            
        return f"Script written successfully to {path}. Unity refresh triggered."

    @mcp.tool()
    def list_workspace_peers() -> str:
        """Returns all registered projects in the current Workspace Perimeter."""
        return json.dumps({
            "active": engine.workspace.active_project,
            "peers": engine.workspace.projects
        }, indent=2)

    @mcp.tool()
    def register_workspace_project(name: str, path: str, project_type: str = "unity") -> str:
        """Adds a project to the Workspace Perimeter, allowing for cross-project polling."""
        if ".." in path: return "Error: Path traversal blocked."
        abs_path = engine.workspace.register_project(name, path, project_type)
        return f"Project '{name}' registered at {abs_path}."

    @mcp.tool()
    def switch_active_project(name: str) -> str:
        """Switches the bridge's Write Authority to the specified project."""
        if engine.switch_project(name):
            return f"Active project switched to '{name}'. Write authority granted."
        return f"Error: Project '{name}' not found in registry."

    @mcp.tool()
    def peek_peer_file(peer_name: str, relative_path: str) -> str:
        """[PEERING] Reads a file from a peer project within the workspace perimeter (Read-Only)."""
        target_path = engine.workspace.resolve_path(relative_path, peer_name)
        if not target_path or not os.path.exists(target_path):
            return f"Error: File '{relative_path}' not found in peer '{peer_name}'."
        
        # Security Check: Ensure the target is actually within that peer's root
        peer_root = engine.workspace.projects.get(peer_name, {}).get("path")
        if not peer_root or not target_path.startswith(peer_root):
            return "Security Violation: Peer access out of bounds."

        with open(target_path, "r") as f:
            return f.read()

    @mcp.tool()
    def bootstrap_vibe_bridge(project_path: str) -> str:
        """Injects the VibeBridgeKernel and ALL Payloads into a new Unity project."""
        try:
            if ".." in project_path: return "Error: Path traversal blocked."
            base_path = os.path.dirname(os.path.abspath(__file__))
            root_path = os.path.abspath(os.path.join(base_path, "..", ".."))
            src_dir = os.path.join(root_path, "unity-package", "Scripts")
            dest_dir = os.path.join(project_path, "Assets", "VibeBridge")
            
            if not os.path.exists(dest_dir): os.makedirs(dest_dir)
            
            copied_count = 0
            for root, dirs, files in os.walk(src_dir):
                for file in files:
                    if file.endswith(".cs") or file.endswith(".asmdef"):
                        # Calculate relative path to maintain structure (e.g., Core/VibeISA.cs)
                        rel_path = os.path.relpath(root, src_dir)
                        target_subdir = os.path.join(dest_dir, rel_path)
                        
                        if not os.path.exists(target_subdir):
                            os.makedirs(target_subdir)
                            
                        src_file = os.path.join(root, file)
                        dst_file = os.path.join(target_subdir, file)
                        
                        with open(src_file, "r") as s, open(dst_file, "w") as d:
                            d.write(s.read())
                        copied_count += 1
                        
            return f"Successfully bootstrapped {copied_count} files into {project_path} (Recursive)."
        except Exception as e: return f"Bootstrap failed: {str(e)}"

    @mcp.tool()
    def check_heartbeat() -> str:
        """Reads the local heartbeat file. Use this if Unity is unresponsive via HTTP."""
        path = os.path.join(engine.project_path, "metadata", "vibe_health.json")
        if os.path.exists(path):
            with open(path, "r") as f: return f.read()
        return "Heartbeat missing."

    @mcp.tool()
    def get_wal_tail(count: int = 10) -> str:
        """[Forensics] Returns the last N entries from the Write-Ahead Log (Audit Log)."""
        return json.dumps(engine.logger.get_wal_tail(count))

    @mcp.tool()
    def get_bridge_pulse() -> str:
        """Returns a compact, 1-line status summary of the entire VibeBridge stack."""
        status = engine.unity_request("status")
        # Extract metadata from the invariance block
        invariance = status.get("_vibe_invariance", {})
        pulse = [
            f"KERNEL: {invariance.get('unity_status', 'OFFLINE')}",
            f"TICK: {invariance.get('monotonic_tick', 0)}",
            f"WAL: {invariance.get('wal_hash', 'N/A')[:8]}",
            f"ENTROPY: {invariance.get('entropy_remaining', 0)}/100",
            f"DRIFT: {invariance.get('drift_budget', 0)}"
        ]
        return " | ".join(pulse)

    @mcp.tool()
    def stabilize_and_start() -> str:
        """Automatically detects and resolves zombie processes or port conflicts, then initializes the bridge."""
        import subprocess
        report = []
        # 1. Check for zombie Unity processes on port 8085
        try:
            # Finding PIDs using port 8085
            pids = subprocess.check_output(["lsof", "-t", "-i:8085"]).decode().split()
            for pid in pids:
                if int(pid) != os.getpid():
                    subprocess.run(["kill", "-9", pid])
                    report.append(f"Killed zombie process {pid} on port 8085.")
        except: pass

        # 2. Re-trigger Unity Kernel
        engine.set_activity("STABILIZING_KERNEL")
        res = engine.unity_request("status")
        report.append(f"Kernel Status: {res.get('_vibe_invariance', {}).get('unity_status', 'Ready')}")
        
        return "\n".join(report) if report else "System already stable."
