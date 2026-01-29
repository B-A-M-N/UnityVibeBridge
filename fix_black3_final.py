#!/usr/bin/env python3
import requests
import json

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {
    "X-Vibe-Token": "VIBE_777_SECURE",
    "X-Vibe-Capability": "Admin"
}

def fix_black3():
    # Scanning active avatar hierarchy
    resp = requests.get(f"{BASE_URL}/hierarchy?root=52224", headers=HEADERS)
    nodes = resp.json().get("nodes", [])
    
    print("[*] Searching for any 'black 3' slots to restore to black...")
    
    for node in nodes:
        inst_id = node["instanceID"]
        m_resp = requests.get(f"{BASE_URL}/material/list?path={inst_id}", headers=HEADERS)
        if m_resp.status_code == 200:
            mats = m_resp.json().get("materials", [])
            for m in mats:
                # Target anything named 'black 3'
                if "black 3" in m["name"].lower():
                    print(f"[*] Restoring '{m['name']}' on '{node['name']}' (Slot {m['index']}) to BLACK.")
                    requests.get(f"{BASE_URL}/material/set-color?path={inst_id}&index={m['index']}&color=0,0,0,1", headers=HEADERS)

if __name__ == "__main__":
    fix_black3()
