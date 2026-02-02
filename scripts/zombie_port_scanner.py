import os
import json
import psutil
import socket

def check_port(port):
    """Checks if a port is in use and returns the process holding it."""
    for conn in psutil.net_connections(kind='inet'):
        if conn.laddr.port == port:
            try:
                proc = psutil.Process(conn.pid)
                return {
                    "pid": conn.pid,
                    "name": proc.name(),
                    "status": conn.status,
                    "command": " ".join(proc.cmdline())
                }
            except:
                return {"pid": conn.pid, "name": "Unknown (Access Denied)"}
    return None

def scan_vibe_ports():
    project_path = os.getcwd()
    settings_file = os.path.join(project_path, "metadata", "vibe_settings.json")
    diag_file = os.path.join(project_path, "metadata", "zombie_diag.json")
    
    control_port = 8085
    vision_port = 8086
    
    if os.path.exists(settings_file):
        try:
            with open(settings_file, "r") as f:
                settings = json.load(f)
                control_port = settings.get("ports", {}).get("control", 8085)
                vision_port = settings.get("ports", {}).get("vision", 8086)
        except: pass

    report = {
        "timestamp": psutil.boot_time(),
        "ports": {}
    }

    control_holder = check_port(control_port)
    if control_holder: report["ports"][str(control_port)] = control_holder
    
    vision_holder = check_port(vision_port)
    if vision_holder: report["ports"][str(vision_port)] = vision_holder

    if report["ports"]:
        print(f"⚠️ [Vibe] PORT CONFLICT DETECTED: {list(report['ports'].keys())}")
        with open(diag_file, "w") as f:
            json.dump(report, f, indent=2)
    else:
        if os.path.exists(diag_file): os.remove(diag_file)

if __name__ == "__main__":
    scan_vibe_ports()
