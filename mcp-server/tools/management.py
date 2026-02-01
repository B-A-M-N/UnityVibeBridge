import os
import json
import requests

def register_management_tools(engine):
    mcp = engine.mcp

    @mcp.tool()
    def bootstrap_vibe_bridge(project_path: str) -> str:
        """Injects the VibeBridgeKernel and ALL Payloads into a new Unity project."""
        try:
            if ".." in project_path: return "Error: Path traversal blocked."
            base_path = os.path.dirname(os.path.abspath(__file__))
            root_path = os.path.abspath(os.path.join(base_path, "..", ".."))
            dest_dir = os.path.join(project_path, "Assets", "VibeBridge")
            if not os.path.exists(dest_dir): os.makedirs(dest_dir)
            
            payloads = ["VibeBridgeKernel.cs", "VibeBridge_StandardPayload.cs", "VibeBridge_ExtrasPayload.cs",
                        "VibeBridge_VisionPayload.cs", "VibeBridge_MaterialPayload.cs", "VibeBridge_RegistryPayload.cs",
                        "VibeBridge_VRChatPayload.cs", "VibeBridge_AuditingPayload.cs", "VibeBridge_HeartbeatManager.cs"]
            
            for f in payloads:
                src = os.path.join(root_path, f)
                if os.path.exists(src):
                    with open(src, "r") as s, open(os.path.join(dest_dir, f), "w") as d:
                        d.write(s.read())
            return f"Successfully bootstrapped into {project_path}."
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
