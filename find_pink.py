#!/usr/bin/env python3
import requests
import json

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {
    "X-Vibe-Token": "VIBE_777_SECURE",
    "X-Vibe-Capability": "Admin"
}

def find_pink():
    # 1. Get all objects in hierarchy
    resp = requests.get(f"{BASE_URL}/hierarchy", headers=HEADERS)
    roots = resp.json().get("nodes", [])
    
    all_nodes = []
    for root in roots:
        all_nodes.append(root)
        c_resp = requests.get(f"{BASE_URL}/hierarchy?root={root['instanceID']}", headers=HEADERS)
        if c_resp.status_code == 200:
            all_nodes.extend(c_resp.json().get("nodes", []))

    print(f"[*] Scanning {len(all_nodes)} objects for pink (error) shaders...")
    
    found_any = False
    for node in all_nodes:
        inst_id = node["instanceID"]
        name = node["name"]
        
        # We need to check material/list which only works if the object HAS a renderer
        m_resp = requests.get(f"{BASE_URL}/material/list?path={inst_id}", headers=HEADERS)
        if m_resp.status_code == 200:
            data = m_resp.json()
            if "materials" in data:
                # This object has a renderer. Now check its shaders specifically.
                # Since material/list doesn't give shader, we use audit/materials on THIS SPECIFIC ID
                a_resp = requests.get(f"{BASE_URL}/audit/materials?path={inst_id}", headers=HEADERS)
                if a_resp.status_code == 200:
                    audit = a_resp.json()
                    for m in audit.get("materials", []):
                        shader = m.get("shader", "")
                        if "InternalErrorShader" in shader or "MISSING" in shader or "Error" in shader:
                            # We need to find the slot index. material/list has it.
                            mats_list = data["materials"]
                            slot_idx = -1
                            for ml in mats_list:
                                if ml["name"] == m["name"]:
                                    slot_idx = ml["index"]
                                    break
                            
                            print(f"[!] PINK DETECTED: Object '{name}' ({inst_id}), Slot {slot_idx}, Material: {m['name']}, Shader: {shader}")
                            found_any = True
    
    if not found_any:
        print("[*] No pink/error shaders detected.")

if __name__ == "__main__":
    find_pink()