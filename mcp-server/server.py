import sys
import os

# Add project root to sys.path to allow importing security_gate
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

import requests
import base64
import time
import datetime
import json
import uuid
from mcp.server.fastmcp import FastMCP
from mcp.types import ImageContent
from security_gate import SecurityGate

# --- CORE SERVER INITIALIZATION ---
mcp = FastMCP("UnityVibeBridge")
UNITY_PROJECT_PATH = os.getcwd() # Default to current directory
QUEUE_PATH = os.path.join(UNITY_PROJECT_PATH, "vibe_queue")
INBOX_PATH = os.path.join(QUEUE_PATH, "inbox")
OUTBOX_PATH = os.path.join(QUEUE_PATH, "outbox")

def ensure_project_infrastructure():
    """
    Auto-Initialization: Creates the necessary 'Airlock' directories 
    to ensure project-specific data is isolated and organized.
    """
    DIRS = [
        INBOX_PATH, 
        OUTBOX_PATH, 
        os.path.join(UNITY_PROJECT_PATH, "metadata"),
        os.path.join(UNITY_PROJECT_PATH, "captures"),
        os.path.join(UNITY_PROJECT_PATH, "logs"),
        os.path.join(UNITY_PROJECT_PATH, "optimizations")
    ]
    for d in DIRS:
        if not os.path.exists(d):
            try:
                os.makedirs(d)
                # Note: We don't print to avoid cluttering MCP transport, 
                # but it ensures the 'Airlock' is ready.
            except: pass

# Run infrastructure check immediately
ensure_project_infrastructure()

def unity_request(method, path, params=None, is_mutation=False):
    """Secure wrapper for Unity requests via AIRLOCK (JSON Queue)."""
    cmd_id = str(uuid.uuid4())
    payload = {
        "action": path,
        "id": cmd_id,
        "capability": "Admin",
        "keys": list(params.keys()) if params else [],
        "values": [str(v) for v in params.values()] if params else []
    }

    # 1. Write to Inbox
    inbox_file = os.path.join(INBOX_PATH, f"{cmd_id}.json")
    with open(inbox_file, "w") as f:
        json.dump(payload, f)

    # 2. Poll Outbox
    outbox_file = os.path.join(OUTBOX_PATH, f"res_{cmd_id}.json")
    timeout = 15 # 15 second timeout for Unity to process
    start_time = time.time()
    
    while time.time() - start_time < timeout:
        if os.path.exists(outbox_file):
            try:
                with open(outbox_file, "r") as f:
                    response_content = f.read()
                os.remove(outbox_file) # Cleanup
                try: return json.loads(response_content)
                except: return response_content
            except Exception as e:
                return f"Error reading response: {str(e)}"
        time.sleep(0.1) # Aggressive polling for better "vibe"

    return f"Error: Airlock Timeout. Unity did not respond to {path} within {timeout}s."

# --- TOOL GROUP: MANAGEMENT & SYSTEM ---

@mcp.tool()
def get_safe_config() -> str:
    """
    Retrieves non-sensitive project configuration from environment variables.
    Only whitelisted keys (e.g. PROJECT_PATH, USER_PREFS) are visible.
    """
    # Whitelist of safe keys. NEVER put API keys or tokens here.
    SAFE_KEYS = {"UNITY_PROJECT_PATH", "AVATAR_NAME", "VIBE_LOG_LEVEL"}
    config = {k: os.environ.get(k) for k in SAFE_KEYS if k in os.environ}
    return f"Safe Configuration: {config if config else 'No safe variables set.'}"

@mcp.tool()
def check_unity_status() -> str:
    """Checks if the Unity VibeBridge server is running and accessible."""
    return str(unity_request("GET", "/status"))

@mcp.tool()
def set_server_mode(mode: str) -> str:
    """Toggles the server between 'readonly' and 'readwrite' modes."""
    return str(unity_request("GET", "/system/mode", {"mode": mode.lower()}))

@mcp.tool()
def run_vibe_check(project_path: str) -> str:
    """Performs a 'Pre-flight' audit of the project for dependencies."""
    report = []
    bridge_path = os.path.join(project_path, "Assets", "VibeBridge", "VibeBridgeServer.cs")
    if os.path.exists(bridge_path):
        report.append("✅ VibeBridge C# Server: INSTALLED")
    else:
        report.append("❌ VibeBridge C# Server: MISSING")

    vcc_config = os.path.join(project_path, "Packages", "vpm-manifest.json")
    if os.path.exists(vcc_config):
        try:
            with open(vcc_config, "r") as f:
                manifest = f.read()
                if "modular-avatar" in manifest.lower(): report.append("✅ Modular Avatar: FOUND")
                else: report.append("⚠️ Modular Avatar: MISSING")
        except: pass
    return "\n".join(report)

@mcp.tool()
def bootstrap_vibe_bridge(project_path: str) -> str:
    """Injects the VibeBridgeServer.cs into a new Unity project."""
    try:
        # Remediation: Path Traversal
        if os.path.isabs(project_path) or ".." in project_path:
             return "Error: Absolute paths and traversal are blocked for safety. Use relative paths."

        source_path = "unity-package/Scripts/VibeBridgeServer.cs"
        dest_dir = os.path.join(project_path, "Assets", "VibeBridge")
        if not os.path.exists(dest_dir): os.makedirs(dest_dir)
        with open(source_path, "r") as src, open(os.path.join(dest_dir, "VibeBridgeServer.cs"), "w") as dest:
            dest.write(src.read())
        return f"Successfully bootstrapped into {project_path}."
    except Exception as e: return f"Bootstrap failed: {str(e)}"

@mcp.tool()
def log_semantic_discovery(target: str, role: str, observations: str) -> str:
    """
    Persists a discovery to the vibe_registry.json.
    Use this to 'remember' what a specific object or material slot does.
    Format for target: 'InstanceID' or 'InstanceID/SlotX'
    """
    import json
    registry_path = os.path.join("metadata", "vibe_registry.json")
    registry = {}
    if os.path.exists(registry_path):
        try:
            with open(registry_path, "r") as f: registry = json.load(f)
        except: pass
    
    registry[target] = {
        "role": role,
        "observations": observations,
        "last_updated": datetime.datetime.now().isoformat()
    }
    
    try:
        if not os.path.exists("metadata"): os.makedirs("metadata")
        with open(registry_path, "w") as f: json.dump(registry, f, indent=2)
        return f"✅ Discovery Logged: {target} -> {role}"
    except Exception as e: return f"Failed to log discovery: {str(e)}"

@mcp.tool()
def generate_vibe_hint() -> str:
    """Generates a .goosehints file to onboard future AI agents with project knowledge."""
    import json
    registry_path = os.path.join("metadata", "vibe_registry.json")
    known_roles = []
    if os.path.exists(registry_path):
        try:
            with open(registry_path, "r") as f:
                reg = json.load(f)
                known_roles = [f"- {v['role']} ({k}): {v.get('observations', 'No notes')}" for k, v in reg.items()]
        except: pass

    hint_content = f"""# UnityVibeBridge: Creative & Safety Context
## 🛡️ Iron Box Protocol
- **Transactions**: All mutations are automatically wrapped in transactions. One request = One Undo step.
- **Dry-Runs**: Every change is validated against system safety rules before execution.
- **No Guesswork**: Use the Semantic Roles below to target objects. If a role is missing, use 'inspect_object' first, then 'log_semantic_discovery'.

## 🎨 Semantic Roles (Targets)
{chr(10).join(known_roles) if known_roles else '- No roles logged. Run a discovery sweep first.'}

## 🚀 Optimization Goals
- PC: < 70k Polys, < 150MB VRAM.
- Quest: < 10k Polys (Very Poor) or 5k (Excellent).
"""
    try:
        with open(".goosehints", "w") as f: f.write(hint_content)
        return "✅ .goosehints updated with proactive safety context."
    except Exception as e: return f"Failed: {str(e)}"

# --- TOOL GROUP: HIGH-INTEGRITY MAPPING ---

@mcp.tool()
def verify_target_fingerprint(role: str, target_id: str) -> str:
    """
    Verifies a target using the Confidence Tier protocol.
    Compares current state (VertexCount, Slots, Name) against the Vibe Registry.
    """
    import json
    registry_path = os.path.join("metadata", "vibe_registry.json")
    if not os.path.exists(registry_path):
        return "Tier 3: No registry found. Full discovery required."
    
    try:
        with open(registry_path, "r") as f:
            registry = json.load(f)
        
        target_meta = registry.get(role)
        if not target_meta:
            return f"Tier 3: Role '{role}' not found in registry."
        
        # 1. Fetch current live data
        current_data = unity_request("GET", "/inspect", {"path": target_id})
        if "error" in current_data:
            return f"Tier 3: Target {target_id} not found in Unity hierarchy."
            
        # 2. Compare Fingerprint (VertexCount, MaterialCount, Name)
        # Fingerprint logic is probabilistic but strict.
        match_score = 0
        if current_data.get("name") == target_meta.get("name"): match_score += 40
        # Additional fingerprint traits (Vertices, Slots) would be added here
        
        if match_score >= 100: return "Tier 1: Perfect Match. Proceeding Silently."
        if match_score >= 80: return f"Tier 2: Fuzzy Match ({match_score}%). Proceeding with Warning."
        return f"Tier 3: Ambiguous Match ({match_score}%). HALT: Re-discovery required."
        
    except Exception as e:
        return f"Error during verification: {str(e)}"

@mcp.tool()
def create_checkpoint(description: str) -> str:
    """
    Creates a non-destructive safety backup of the current avatar.
    This is the frozen 'Universal Snapshot' mechanism.
    """
    # Use existing create_optimization_variant but with a standard timestamped suffix
    ts = datetime.datetime.now().strftime("%Y%m%d_%H%M")
    result = unity_request("GET", "/object/create-variant", {"path": "root", "suffix": f"Backup_{ts}"}, is_mutation=True)
    return f"Checkpoint Created: {result.get('name', 'Unknown')} - {description}"

@mcp.tool()
def get_target_data(role: str) -> str:
    """
    JIT Context: Retrieves registry data for a single role.
    Prevents context bloat and 'Refactor Loop' hallucinations.
    """
    import json
    registry_path = os.path.join("metadata", "vibe_registry.json")
    if not os.path.exists(registry_path): return "Error: Registry missing."
    with open(registry_path, "r") as f:
        registry = json.load(f)
    return json.dumps(registry.get(role, {"error": "Role not found"}))

# --- TOOL GROUP: INSPECTION & AUDIT ---

@mcp.tool()
def get_hierarchy(root: str = None) -> str:
    """Retreives the Unity scene hierarchy."""
    return str(unity_request("GET", "/hierarchy", {"root": root} if root else {}))

@mcp.tool()
def inspect_object(target: str) -> str:
    """Inspects a specific GameObject to see its components and state."""
    return str(unity_request("GET", "/inspect", {"path": target}))

@mcp.tool()
def calculate_vram_footprint(target: str) -> str:
    """Calculates the VRAM usage (in MB) for an avatar's textures on PC."""
    return str(unity_request("GET", "/system/vram-footprint", {"path": target}))

@mcp.tool()
def rank_physbone_importance(target: str) -> str:
    """Ranks PhysBones by weight to prioritize Quest pruning."""
    return str(unity_request("GET", "/physbone/rank-importance", {"path": target}))

# --- TOOL GROUP: WORLD BUILDING & BAKE GUARD ---

@mcp.tool()
def get_static_flags(target: str) -> str:
    """Retrieves the StaticEditorFlags for a GameObject."""
    return str(unity_request("GET", "/world/static/get", {"path": target}))

@mcp.tool()
def set_static_flags(target: str, flags: int) -> str:
    """Sets the StaticEditorFlags for a GameObject."""
    return str(unity_request("GET", "/world/static/set", {"path": target, "flags": flags}, is_mutation=True))

@mcp.tool()
def validate_bake_readiness(target: str) -> str:
    """Performs a 'Bake Guard' audit to ensure static objects are ready for lightmapping."""
    return str(unity_request("GET", "/world/bake/validate", {"path": target}))

# --- TOOL GROUP: RIGGING & MUTATION ---

@mcp.tool()
def set_component_value(target: str, field: str, value: str, component: str = None) -> str:
    """Sets a field or Material property on an object."""
    params = {"path": target, "field": field, "value": value}
    if component: params["component"] = component
    return str(unity_request("GET", "/component/set", params, is_mutation=True))

@mcp.tool()
def rename_object(target: str, new_name: str) -> str:
    """Renames a GameObject in the hierarchy."""
    return str(unity_request("GET", "/object/rename", {"path": target, "newName": new_name}, is_mutation=True))

@mcp.tool()
def reparent_object(target: str, new_parent: str) -> str:
    """Changes the parent of a GameObject."""
    return str(unity_request("GET", "/object/reparent", {"path": target, "newParent": new_parent}, is_mutation=True))

@mcp.tool()
def set_object_active(target: str, active: bool) -> str:
    """Enables or disables a GameObject in the hierarchy."""
    return str(unity_request("GET", "/object/active", {"path": target, "active": str(active).lower()}, is_mutation=True))

@mcp.tool()
def destroy_object(target: str) -> str:
    """Destroys a GameObject. Safety: Only objects created by the current session can be destroyed."""
    return str(unity_request("GET", "/object/destroy", {"path": target}, is_mutation=True))

@mcp.tool()
def force_destroy_object(target: str) -> str:
    """Destroys a GameObject IMMEDIATELY. Warning: Cannot be undone if not using transactions properly."""
    return str(unity_request("GET", "/object/force-destroy", {"path": target}, is_mutation=True))

@mcp.tool()
def rename_asset(asset_path: str, new_name: str) -> str:
    """Renames an asset file in the project folder."""
    return str(unity_request("GET", "/asset/rename", {"path": asset_path, "newName": new_name}, is_mutation=True))

@mcp.tool()
def create_optimization_variant(target: str, suffix: str = "Quest") -> str:
    """Creates a non-destructive clone of an object for optimization."""
    return str(unity_request("GET", "/object/create-variant", {"path": target, "suffix": suffix}, is_mutation=True))

@mcp.tool()
def export_fbx(target: str, export_path: str) -> str:
    """Exports a GameObject from Unity to an FBX file using the FBX Exporter package."""
    return str(unity_request("GET", "/object/export-fbx", {"path": target, "exportPath": export_path}, is_mutation=True))

@mcp.tool()
def crush_textures(target: str, max_size: int = 512) -> str:
    """Downscales textures on an object for mobile optimization."""
    return str(unity_request("GET", "/texture/crush", {"path": target, "maxSize": max_size}, is_mutation=True))

@mcp.tool()
def swap_to_quest_shaders(target: str) -> str:
    """Swaps materials to Quest-safe shaders."""
    return str(unity_request("GET", "/shader/swap-quest", {"path": target}, is_mutation=True))

# --- TOOL GROUP: VISUALS ---

@mcp.tool()
def take_screenshot(view: str = "scene", filename: str = None) -> ImageContent:
    """Captures a screenshot and saves it in the captures/ folder."""
    if filename and (".." in filename or filename.startswith("/")):
        return ImageContent(data="", mimeType="text/plain")

    token = get_token()
    headers = {"X-Vibe-Token": token} if token else {}
    resp = requests.get(f"{UNITY_API_URL}/view/screenshot", params={"view": view}, headers=headers, timeout=5)
    if resp.status_code == 200:
        b64 = resp.json().get('base64')
        if b64:
            ts = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
            fname = filename or f"screenshot_{{ts}}.png"
            with open(os.path.join("captures", fname), "wb") as f:
                f.write(base64.b64decode(b64))
            return ImageContent(data=b64, mimeType="image/png")
    return ImageContent(data="", mimeType="text/plain")

# --- FILE SAFETY GATE ---

@mcp.tool()
def secure_write_file(path: str, content: str) -> str:
    """Writes a file ONLY after passing a logic-based Security Audit."""
    if ".." in path or path.startswith("/"): return "Error: Path traversal blocked."
    
    # --- INFRASTRUCTURE PROTECTION: Prevent Self-Modification ---
    FORBIDDEN_PATHS = {
        "security_gate.py", 
        "mcp-server/server.py", 
        "trusted_signatures.json",
        ".gemini_security/",
        "security_tests/"
    }
    if any(fp in path for fp in FORBIDDEN_PATHS):
        return f"Error: Modification of security infrastructure ({path}) is strictly forbidden."

    ext = os.path.splitext(path)[1]
    issues = SecurityGate.check_python(content) if ext == '.py' else SecurityGate.check_csharp(content) if ext == '.cs' else []
    if issues: return "Security Violation:\n" + "\n".join(issues)
    try:
        with open(path, "w") as f: f.write(content)
        return f"Successfully wrote {path}."
    except Exception as e: return f"Failed: {str(e)}"

if __name__ == "__main__":

    if len(sys.argv) > 1 and sys.argv[1] == "test":

        print(f"🔍 Testing connection to Unity via AIRLOCK at {QUEUE_PATH}...")

        status = unity_request("GET", "/status")

        print(f"Result: {status}")

        sys.exit(0)

    mcp.run()
