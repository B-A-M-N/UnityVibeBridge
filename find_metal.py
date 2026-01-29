#!/usr/bin/env python3
import requests
import json

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {"X-Vibe-Token": "VIBE_777_SECURE", "X-Vibe-Capability": "Admin"}

def find_metal_decoupled():
    resp = requests.get(f"{BASE_URL}/hierarchy", headers=HEADERS)
    roots = resp.json().get("nodes", [])
    
    print("[*] Searching for 'metal' or 'decoupled' materials...")
    
    for r in roots:
        d_resp = requests.get(f"{BASE_URL}/hierarchy?root={r['instanceID']}", headers=HEADERS)
        nodes = [r]
        if d_resp.status_code == 200:
            nodes.extend(d_resp.json().get("nodes", []))
            
        for node in nodes:
            m_resp = requests.get(f"{BASE_URL}/material/list?path={node['instanceID']}", headers=HEADERS)
            if m_resp.status_code == 200:
                mats = m_resp.json().get("materials", [])
                for m in mats:
                    if m["name"] and ("metal" in m["name"].lower() or "decoupled" in m["name"].lower()):
                        print(f"FOUND: '{node['name']}' ({node['instanceID']}) Slot {m['index']} -> '{m['name']}'")

if __name__ == "__main__":
    find_metal_decoupled()
