#!/usr/bin/env python3
import requests

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {"X-Vibe-Token": "VIBE_777_SECURE", "X-Vibe-Capability": "Admin"}

def deep_fix_black3():
    # Get all nodes under avatar
    resp = requests.get(f"{BASE_URL}/hierarchy?root=52224", headers=HEADERS)
    nodes = resp.json().get("nodes", [])
    
    print(f"[*] Deep scanning {len(nodes)} objects for 'black 3'...")
    
    for node in nodes:
        inst_id = node["instanceID"]
        # Audit gives us the actual material names and shaders
        a_resp = requests.get(f"{BASE_URL}/audit/materials?path={inst_id}", headers=HEADERS)
        if a_resp.status_code == 200:
            mats = a_resp.json().get("materials", [])
            for m in mats:
                if "black 3" in m["name"].lower():
                    # We found it. Now we need the slot index.
                    # material/list gives indices.
                    l_resp = requests.get(f"{BASE_URL}/material/list?path={inst_id}", headers=HEADERS)
                    if l_resp.status_code == 200:
                        slots = l_resp.json().get("materials", [])
                        for s in slots:
                            if s["name"] == m["name"]:
                                print(f"[!] FIXING: {node['name']} (Slot {s['index']}) -> {m['name']} to BLACK")
                                requests.get(f"{BASE_URL}/material/set-color?path={inst_id}&index={s['index']}&color=0,0,0,1", headers=HEADERS)

if __name__ == "__main__":
    deep_fix_black3()
