#!/usr/bin/env python3
import requests
import json

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {"X-Vibe-Token": "VIBE_777_SECURE", "X-Vibe-Capability": "Admin"}
ACTIVE_AVATAR_ROOT = "76104"

# The core list of objects/materials provided by user
TARGET_MAPPINGS = [
    {"name": "body", "mat": "metal"},
    {"name": "Collar and body chains", "mat": "pawpad"},
    {"name": "Collar and body chains", "mat": "metal"},
    {"name": "warmers and glove", "mat": "metal"},
    {"name": "skeleton hoodie", "mat": "skele hoodie"},
    {"name": "Cyberpants_by_Grey", "mat": "pants"},
    {"name": "Cyberpants_by_Grey", "mat": "metal"},
    {"name": "Belt", "mat": "pawpad"},
    {"name": "Pentagram Body Harness", "mat": "metal"},
    {"name": "garter", "mat": "metal"},
    {"name": "Boots", "mat": "metal"},
    {"name": "Head", "mat": "metal"},
    {"name": "jacket", "mat": "metal"}
]

def rebuild_targeted_registry():
    print(f"[*] Rebuilding registry for all requested links (13 components)...")
    
    resp = requests.get(f"{BASE_URL}/hierarchy?root={ACTIVE_AVATAR_ROOT}", headers=HEADERS)
    nodes = resp.json().get("nodes", [])
    name_to_id = {n["name"]: n["instanceID"] for n in nodes}
    
    new_registry = {}
    
    for target in TARGET_MAPPINGS:
        obj_name = target["name"]
        mat_search = target["mat"].lower()
        inst_id = name_to_id.get(obj_name)
        
        if inst_id:
            m_resp = requests.get(f"{BASE_URL}/material/list?path={inst_id}", headers=HEADERS)
            if m_resp.status_code == 200:
                mats = m_resp.json().get("materials", [])
                for m in mats:
                    if mat_search in m["name"].lower():
                        key = f"{inst_id}/Slot{m['index']}"
                        new_registry[key] = {
                            "role": f"{obj_name} {m['name']}",
                            "group": "AccentAll",
                            "observations": "Linked by user request."
                        }
                        print(f"  [+] Linked: {obj_name} (Slot {m['index']}) -> {m['name']}")

    with open("metadata/vibe_registry.json", "w") as f:
        json.dump(new_registry, f, indent=2)
    
    print(f"[*] Successfully linked {len(new_registry)} specific slots to AccentAll.")

if __name__ == "__main__":
    rebuild_targeted_registry()