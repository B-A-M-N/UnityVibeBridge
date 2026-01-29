#!/usr/bin/env python3
import requests
import json

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {
    "X-Vibe-Token": "VIBE_777_SECURE",
    "X-Vibe-Capability": "Admin"
}

def find_black3():
    # Get everything
    resp = requests.get(f"{BASE_URL}/hierarchy", headers=HEADERS)
    roots = resp.json().get("nodes", [])
    
    all_nodes = []
    for root in roots:
        all_nodes.append(root)
        c_resp = requests.get(f"{BASE_URL}/hierarchy?root={root['instanceID']}", headers=HEADERS)
        if c_resp.status_code == 200:
            all_nodes.extend(c_resp.json().get("nodes", []))

    print(f"[*] Searching for 'black 3' materials...")
    
    for node in all_nodes:
        inst_id = node["instanceID"]
        # material/list shows names
        m_resp = requests.get(f"{BASE_URL}/material/list?path={inst_id}", headers=HEADERS)
        if m_resp.status_code == 200:
            data = m_resp.json()
            if "materials" in data:
                for ml in data["materials"]:
                    if "black 3" in ml["name"]:
                        # Now audit shader
                        a_resp = requests.get(f"{BASE_URL}/audit/materials?path={inst_id}", headers=HEADERS)
                        shader = "Unknown"
                        if a_resp.status_code == 200:
                            audit = a_resp.json()
                            for am in audit.get("materials", []):
                                if am["name"] == ml["name"]:
                                    shader = am["shader"]
                                    break
                        print(f"[!] FOUND: '{node['name']}' ({inst_id}), Slot {ml['index']}, Material: {ml['name']}, Shader: {shader}")

if __name__ == "__main__":
    find_black3()
