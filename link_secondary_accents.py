#!/usr/bin/env python3
import requests
import json

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {"X-Vibe-Token": "VIBE_777_SECURE", "X-Vibe-Capability": "Admin"}
ACTIVE_AVATAR_ROOT = "52224"

# User's likely Secondary candidates (Inner details/Secondary accents)
TARGET_MAPPINGS = [
    {"name": "body", "mat": "black 6"},
    {"name": "body", "mat": "black 3"},
    {"name": "Belt", "mat": "black 2"},
    {"name": "Centiped tail", "mat": "black 6"},
    {"name": "Head", "mat": "black 6"},
    {"name": "Head", "mat": "Lofi Water"}
]

def rebuild_secondary_registry():
    print(f"[*] Linking Secondary Accent slots...")
    
    resp = requests.get(f"{BASE_URL}/hierarchy?root={ACTIVE_AVATAR_ROOT}", headers=HEADERS)
    nodes = resp.json().get("nodes", [])
    name_to_id = {n["name"]: n["instanceID"] for n in nodes}
    
    with open("metadata/vibe_registry.json", "r") as f:
        registry = json.load(f)
    
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
                        # Link to Secondary group
                        registry[key] = {
                            "role": f"{obj_name} {m['name']}",
                            "group": "Secondary",
                            "observations": "Linked to Secondary Color Wheel."
                        }
                        print(f"  [+] Linked Secondary: {obj_name} (Slot {m['index']}) -> {m['name']}")

    with open("metadata/vibe_registry.json", "w") as f:
        json.dump(registry, f, indent=2)
    
    print(f"[*] Successfully linked Secondary accents.")

if __name__ == "__main__":
    rebuild_secondary_registry()
