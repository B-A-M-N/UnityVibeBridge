#!/usr/bin/env python3
import requests
import json

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {
    "X-Vibe-Token": "VIBE_777_SECURE",
    "X-Vibe-Capability": "Admin"
}
ACTIVE_AVATAR_ROOT = "52224"
TARGET_KEYWORDS = ["metal", "pawpad", "accent", "collar", "chains", "lofi", "piercing", "garter", "boot", "pant", "black"]

def generate_frozen_registry():
    print(f"[*] Scanning active avatar (ID: {ACTIVE_AVATAR_ROOT}) to generate frozen registry...")
    
    resp = requests.get(f"{BASE_URL}/hierarchy?root={ACTIVE_AVATAR_ROOT}", headers=HEADERS)
    nodes = resp.json().get("nodes", [])
    
    frozen_registry = {}
    
    for node in nodes:
        inst_id = node["instanceID"]
        m_resp = requests.get(f"{BASE_URL}/material/list?path={inst_id}", headers=HEADERS)
        if m_resp.status_code == 200:
            mats = m_resp.json().get("materials", [])
            for m in mats:
                mat_name = m["name"].lower()
                idx = m["index"]
                
                if any(k in mat_name for k in TARGET_KEYWORDS):
                    # Key format: InstanceID/SlotX
                    key = f"{inst_id}/Slot{idx}"
                    frozen_registry[key] = {
                        "role": f"Frozen {node['name']} {m['name']}",
                        "group": "Static", # Change from "AccentAll" to "Static" to sever ties
                        "observations": f"Auto-captured from active setup. Locked at current state."
                    }

    with open("metadata/vibe_registry.json", "w") as f:
        json.dump(frozen_registry, f, indent=2)
    
    print(f"[*] Successfully frozen {len(frozen_registry)} slots into metadata/vibe_registry.json.")
    print("[*] Ties to 'AccentAll' group have been severed.")

if __name__ == "__main__":
    generate_frozen_registry()
