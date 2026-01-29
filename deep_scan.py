import requests
import json

BASE_URL = "http://localhost:8085"
AVATAR_ROOT = 38590

def scan_recursive(node_id, depth=0):
    try:
        # Get immediate children
        resp = requests.get(f"{BASE_URL}/hierarchy?root={node_id}", timeout=5).json()
        nodes = resp.get("nodes", [])
        
        for node in nodes:
            iid = node["instanceID"]
            name = node["name"]
            
            # Check materials
            try:
                m_resp = requests.get(f"{BASE_URL}/material/list?path={iid}", timeout=2).json()
                materials = m_resp.get("materials", [])
                for i, m in enumerate(materials):
                    if "InternalError" in m.get("shader", "") or "MISSING" in m.get("shader", "") or "PINK" in m.get("shader", ""):
                        print(f"!!! PINK MATERIAL FOUND !!!")
                        print(f"  Object: {name} (ID: {iid})")
                        print(f"  Slot: {i}")
                        print(f"  Material: {m.get('name')}")
                        print(f"  Shader: {m.get('shader')}")
                        print("-" * 30)
            except: pass
            
            # Recurse
            scan_recursive(iid, depth + 1)
    except: pass

print(f"Starting deep scan of Avatar {AVATAR_ROOT}...")
scan_recursive(AVATAR_ROOT)
print("Scan complete.")