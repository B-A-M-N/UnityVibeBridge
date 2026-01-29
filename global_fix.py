#!/usr/bin/env python3
import requests
import json
import sys

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {
    "X-Vibe-Token": "VIBE_777_SECURE",
    "X-Vibe-Capability": "Admin"
}

def get_all_nodes_recursive(root_id=None):
    url = f"{BASE_URL}/hierarchy"
    if root_id:
        url += f"?root={root_id}"
    
    resp = requests.get(url, headers=HEADERS)
    if resp.status_code != 200:
        return []
    
    nodes = resp.json().get("nodes", [])
    all_nodes = list(nodes)
    if root_id is None:
        for node in nodes:
            all_nodes.extend(get_all_nodes_recursive(node["instanceID"]))
    return all_nodes

def brute_force_color(target_color):
    color_map = {
        "red": "1,0,0,1",
        "green": "0,1,0,1",
        "blue": "0,0,1,1",
        "teal": "0,1,1,1",
        "purple": "0.5,0,1,1",
        "black": "0,0,0,1"
    }
    
    unity_color = color_map.get(target_color.lower(), target_color)
    all_objects = get_all_nodes_recursive()
    unique_objects = {obj["instanceID"]: obj for obj in all_objects}.values()
    
    print(f"[*] SYNCING ALL ACCENTS TO: {target_color} ({unity_color})")
    
    target_keywords = ["metal", "pawpad", "accent", "collar", "chains", "lofi", "piercing", "garter", "boot", "pant", "black"]
    
    for obj in unique_objects:
        inst_id = obj["instanceID"]
        m_resp = requests.get(f"{BASE_URL}/material/list?path={inst_id}", headers=HEADERS)
        if m_resp.status_code == 200:
            mats = m_resp.json().get("materials", [])
            for m in mats:
                mat_name = m["name"].lower()
                if any(k in mat_name for k in target_keywords):
                    requests.get(f"{BASE_URL}/material/set-color?path={inst_id}&index={m['index']}&color={unity_color}", headers=HEADERS)
                    requests.get(f"{BASE_URL}/material/clear-block?path={inst_id}", headers=HEADERS)

if __name__ == "__main__":
    color = sys.argv[1] if len(sys.argv) > 1 else "purple"
    brute_force_color(color)