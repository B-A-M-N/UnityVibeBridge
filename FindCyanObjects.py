import requests
import json

BASE_URL = "http://localhost:8085"
HEADERS = {
    "X-Vibe-Token": "VIBE_777_SECURE",
    "X-Vibe-Capability": "Admin"
}

def get_nodes(root_id=None):
    url = f"{BASE_URL}/hierarchy"
    if root_id: url += f"?root={root_id}"
    try:
        resp = requests.get(url, headers=HEADERS, timeout=5)
        return resp.json().get("nodes", [])
    except: return []

def find_cyan(node):
    iid = node["instanceID"]
    name = node["name"]
    
    # Get materials
    try:
        mats_resp = requests.get(f"{BASE_URL}/material/list?path={iid}", headers=HEADERS, timeout=2).json()
        materials = mats_resp.get("materials", [])
        for m in materials:
            print(f"Object: {name} ({iid}) Slot {m['index']}: {m['name']}")
    except: pass

nodes = get_nodes("-29498")
for n in nodes:
    find_cyan(n)
