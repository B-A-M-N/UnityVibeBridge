#!/usr/bin/env python3
import requests

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {"X-Vibe-Token": "VIBE_777_SECURE", "X-Vibe-Capability": "Admin"}

def fix_all_blacks():
    resp = requests.get(f"{BASE_URL}/hierarchy?root=52224", headers=HEADERS)
    nodes = resp.json().get("nodes", [])
    
    print("[*] Forcing ALL 'black' named materials to BLACK...")
    
    for node in nodes:
        inst_id = node["instanceID"]
        m_resp = requests.get(f"{BASE_URL}/material/list?path={inst_id}", headers=HEADERS)
        if m_resp.status_code == 200:
            mats = m_resp.json().get("materials", [])
            for m in mats:
                if "black" in m["name"].lower():
                    print(f"[*] Force Black: {node['name']} (Slot {m['index']}) -> {m['name']}")
                    requests.get(f"{BASE_URL}/material/set-color?path={inst_id}&index={m['index']}&color=0,0,0,1", headers=HEADERS)

if __name__ == "__main__":
    fix_all_blacks()
