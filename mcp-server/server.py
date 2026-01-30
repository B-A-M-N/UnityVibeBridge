# UnityVibeBridge: The Governed Creation Kernel for Unity
# Copyright (C) 2026 B-A-M-N
#
# This software is dual-licensed under the GNU AGPLv3 and a 
# Commercial "Work-or-Pay" Maintenance Agreement.
#
# You may use this file under the terms of the AGPLv3, provided 
# you meet all requirements (including source disclosure).
#
# For commercial use, or to keep your modifications private, 
# you must satisfy the requirements of the Commercial Path 
# as defined in the LICENSE file at the project root.

import sys
import os
import requests
import base64
import time
import datetime
import json
import uuid
from mcp.server.fastmcp import FastMCP
from mcp.types import ImageContent

# --- KERNEL INITIALIZATION ---
mcp = FastMCP("UnityVibeBridge")
UNITY_PROJECT_PATH = os.getcwd()
INBOX_PATH = os.path.join(UNITY_PROJECT_PATH, "vibe_queue", "inbox")
OUTBOX_PATH = os.path.join(UNITY_PROJECT_PATH, "vibe_queue", "outbox")

def ensure_infrastructure():
    DIRS = [INBOX_PATH, OUTBOX_PATH, 
            os.path.join(UNITY_PROJECT_PATH, "metadata"),
            os.path.join(UNITY_PROJECT_PATH, "captures"),
            os.path.join(UNITY_PROJECT_PATH, "logs"),
            os.path.join(UNITY_PROJECT_PATH, "optimizations")]
    for d in DIRS:
        if not os.path.exists(d): os.makedirs(d)

ensure_infrastructure()

def unity_request(path, params=None, is_mutation=False):
    """Secure AIRLOCK IPC with Token-Auth support."""
    
    # --- GET SESSION TOKEN ---
    token = None
    status_path = os.path.join(UNITY_PROJECT_PATH, "metadata", "vibe_status.json")
    if os.path.exists(status_path):
        try:
            with open(status_path, "r") as f:
                s = json.load(f)
                token = s.get("nonce")
        except: pass

    # Try HTTP first for low-latency tools
    try:
        headers = {"X-Vibe-Token": token} if token else {}
        resp = requests.get(f"http://127.0.0.1:8085/{path}", params=params, headers=headers, timeout=2)
        if resp.status_code == 200:
            return resp.json()
    except: pass # Fallback to Airlock

    cmd_id = str(uuid.uuid4())
    payload = {
        "action": path,
        "id": cmd_id,
        "capability": "Admin" if is_mutation else "Read",
        "keys": list(params.keys()) if params else [],
        "values": [str(v) for v in params.values()] if params else []
    }

    with open(os.path.join(INBOX_PATH, f"{cmd_id}.json"), "w") as f: json.dump(payload, f)

    outbox_file = os.path.join(OUTBOX_PATH, f"res_{cmd_id}.json")
    start = time.time()
    while time.time() - start < 15:
        if os.path.exists(outbox_file):
            with open(outbox_file, "r") as f: res_content = f.read()
            os.remove(outbox_file)
            
            try:
                data = json.loads(res_content)
            except:
                data = {"raw_response": res_content}
            
            # --- TRUTH LOOP ---
            health_path = os.path.join(UNITY_PROJECT_PATH, "metadata", "vibe_health.json")
            if os.path.exists(health_path):
                try:
                    with open(health_path, "r") as f: h = json.load(f)
                    if h.get("errorCount", 0) > 0:
                        data["_vibe_warning"] = f"Unity has {h.get('errorCount')} console errors."
                except: pass
            
            return data
        time.sleep(0.1)
    return {"error": "Timeout"}

# --- TOOL GROUP: MANAGEMENT & SYSTEM ---

@mcp.tool()
def bootstrap_vibe_bridge(project_path: str) -> str:
    """Injects the VibeBridgeKernel.cs and Payloads into a new Unity project."""
    try:
        if ".." in project_path: return "Error: Path traversal blocked."
        
        base_path = os.path.dirname(os.path.abspath(__file__))
        root_path = os.path.abspath(os.path.join(base_path, ".."))
        
        dest_dir = os.path.join(project_path, "Assets", "VibeBridge")
        editor_dir = os.path.join(dest_dir, "Editor")
        if not os.path.exists(dest_dir): os.makedirs(dest_dir)
        if not os.path.exists(editor_dir): os.makedirs(editor_dir)
        
        # Copy Kernel and Payloads
        payloads = [
            "VibeBridgeKernel.cs", 
            "VibeBridge_StandardPayload.cs", 
            "VibeBridge_ExtrasPayload.cs",
            "VibeBridge_VisionPayload.cs"
        ]
        
        for f in payloads:
            src_path = os.path.join(root_path, f)
            if not os.path.exists(src_path): return f"Error: Source file {f} not found at {src_path}"
            with open(src_path, "r") as src, open(os.path.join(dest_dir, f), "w") as dest:
                dest.write(src.read())
                
        # Copy Editor Tools
        dev_helper_src = os.path.join(root_path, "unity-package", "Editor", "VibeDevHelper.cs")
        if os.path.exists(dev_helper_src):
            with open(dev_helper_src, "r") as src, open(os.path.join(editor_dir, "VibeDevHelper.cs"), "w") as dest:
                dest.write(src.read())

        return f"Successfully bootstrapped into {project_path}."
    except Exception as e: return f"Bootstrap failed: {str(e)}"

@mcp.tool()
def run_vibe_check(project_path: str) -> str:
    """Performs a 'Pre-flight' audit of the project infrastructure."""
    report = []
    bridge_path = os.path.join(project_path, "Assets", "VibeBridge", "VibeBridgeKernel.cs")
    report.append(f"✅ Kernel: {'FOUND' if os.path.exists(bridge_path) else 'MISSING'}")
    return "\n".join(report)

@mcp.tool()
def check_heartbeat() -> str:
    """Reads the local heartbeat file. Use this if Unity is unresponsive via HTTP."""
    path = os.path.join(UNITY_PROJECT_PATH, "metadata", "vibe_health.json")
    if os.path.exists(path):
        with open(path, "r") as f: return f.read()
    return "Heartbeat missing."

# --- KERNEL TOOLS (MVC) ---

@mcp.tool()
def get_hierarchy() -> str:
    """Returns the Unity scene hierarchy."""
    return str(unity_request("hierarchy"))

@mcp.tool()
def search_objects(name_pattern: str = None, layer: int = -1) -> str:
    """Finds objects by Regex name pattern or Layer (limit 100)."""
    p = {}
    if name_pattern: p["name"] = name_pattern
    if layer != -1: p["layer"] = layer
    return str(unity_request("system/search", p))

@mcp.tool()
def inspect_object(path: str) -> str:
    """Returns components and state of a GameObject."""
    return str(unity_request("inspect", {"path": path}))

@mcp.tool()
def select_object(path: str) -> str:
    """Selects and frames a GameObject in the Editor."""
    return str(unity_request("system/select", {"path": path}))

@mcp.tool()
def rename_object(path: str, new_name: str) -> str:
    """Renames a GameObject."""
    return str(unity_request("object/rename", {"path": path, "newName": new_name}, is_mutation=True))

@mcp.tool()
def set_value(path: str, component: str, field: str, value: str) -> str:
    """Sets a field or property value on a component."""
    return str(unity_request("object/set-value", {"path": path, "component": component, "field": field, "value": value}, is_mutation=True))

@mcp.tool()
def clone_object(path: str) -> str:
    """Duplicates a GameObject."""
    return str(unity_request("object/clone", {"path": path}, is_mutation=True))

@mcp.tool()
def delete_object(path: str) -> str:
    """Deletes a GameObject safely."""
    return str(unity_request("object/delete", {"path": path}, is_mutation=True))

@mcp.tool()
def reparent_object(path: str, new_parent: str = None) -> str:
    """Changes the parent of a GameObject."""
    return str(unity_request("object/reparent", {"path": path, "newParent": new_parent}, is_mutation=True))

@mcp.tool()
def batch_rename_objects(paths: str, names: str) -> str:
    """Renames multiple objects (comma-separated)."""
    return str(unity_request("object/batch-rename", {"paths": paths, "names": names}, is_mutation=True))

@mcp.tool()
def begin_transaction(name: str = "AI Op") -> str:
    """Starts an atomic Undo Group."""
    return str(unity_request("transaction/begin", {"name": name}, is_mutation=True))

@mcp.tool()
def commit_transaction() -> str:
    """Commits the current Undo Group."""
    return str(unity_request("transaction/commit", is_mutation=True))

@mcp.tool()
def get_telemetry_errors() -> str:
    """Returns recent Unity console errors."""
    return str(unity_request("telemetry/get-errors"))

@mcp.tool()
def list_available_tools() -> str:
    """Returns a list of all installed VibeTools."""
    return str(unity_request("system/list-tools"))

# --- PAYLOAD TOOLS ---

@mcp.tool()
def calculate_vram_footprint(path: str) -> str:
    """[Payload] Estimates VRAM usage for an object."""
    return str(unity_request("system/vram-footprint", {"path": path}))

@mcp.tool()
def crush_textures(path: str, max_size: int = 512) -> str:
    """[Payload] Downscales textures."""
    return str(unity_request("texture/crush", {"path": path, "maxSize": max_size}, is_mutation=True))

@mcp.tool()
def audit_avatar(path: str) -> str:
    """[Payload] Returns a report on meshes and materials."""
    return str(unity_request("audit/avatar", {"path": path}))

@mcp.tool()
def run_physics_audit() -> str:
    """[Payload] Identifies Rigidbodies and Colliders."""
    return str(unity_request("physics/audit"))

@mcp.tool()
def spawn_prefab(asset_path: str) -> str:
    """[Payload] Instantiates a prefab."""
    return str(unity_request("world/spawn", {"asset": asset_path}, is_mutation=True))

@mcp.tool()
def rename_asset(path: str, new_name: str) -> str:
    """[Payload] Renames an asset file (e.g. .mat, .prefab)."""
    return str(unity_request("asset/rename", {"path": path, "newName": new_name}, is_mutation=True))

@mcp.tool()
def move_asset(path: str, new_path: str) -> str:
    """[Payload] Moves an asset to a new folder path."""
    return str(unity_request("asset/move", {"path": path, "newPath": new_path}, is_mutation=True))

@mcp.tool()
def apply_prefab_changes(path: str) -> str:
    """[Payload] Commits overrides on a prefab instance back to the master asset."""
    return str(unity_request("prefab/apply", {"path": path}, is_mutation=True))

@mcp.tool()
def take_screenshot() -> ImageContent:
    """Captures the scene view via Port 8085."""
    try:
        resp = requests.get("http://127.0.0.1:8085/view/screenshot", timeout=5)
        if resp.status_code == 200:
            return ImageContent(data=resp.json().get('base64'), mimeType="image/png")
    except: pass
    return ImageContent(data="", mimeType="text/plain")

# --- EXTRAS TOOLS ---

@mcp.tool()
def visual_point(target: str = None, pos: str = None, label: str = "Attention") -> str:
    """Spawns a visual debug sphere."""
    p = {"label": label}
    if target: p["path"] = target
    if pos: p["pos"] = pos
    return str(unity_request("visual/point", p, is_mutation=True))

@mcp.tool()
def visual_line(start_target: str = None, start_pos: str = None, end_target: str = None, end_pos: str = None) -> str:
    """Spawns a visual debug line."""
    p = {}
    if start_target: p["startPath"] = start_target
    if start_pos: p["startPos"] = start_pos
    if end_target: p["endPath"] = end_target
    if end_pos: p["endPos"] = end_pos
    return str(unity_request("visual/line", p, is_mutation=True))

@mcp.tool()
def visual_clear() -> str:
    """Clears all visual debug markers."""
    return str(unity_request("visual/clear", {}, is_mutation=True))

@mcp.tool()
def set_animator_param(target: str, name: str, value: str) -> str:
    """Sets a parameter on an Animator (float, int, bool)."""
    return str(unity_request("animator/set-param", {"path": target, "name": name, "value": value}, is_mutation=True))

@mcp.tool()
def create_optimization_fork(target: str) -> str:
    """Creates a safe copy of an avatar with isolated materials for optimization."""
    return str(unity_request("opt/fork", {"path": target}, is_mutation=True))

@mcp.tool()
def validate_export(target: str) -> str:
    """Checks if an object is safe to export (Scale 1, Rot 0, No missing scripts)."""
    return str(unity_request("export/validate", {"path": target}))

if __name__ == "__main__":
    mcp.run()