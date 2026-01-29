#!/usr/bin/env python3
import requests
import json

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {"X-Vibe-Token": "VIBE_777_SECURE", "X-Vibe-Capability": "Admin"}
ACTIVE_AVATAR_ROOT = "52224"

def restore_quest_look():
    print("[*] Restoring all customizations to Quest Shaders...")
    
    # 1. Load Registry for AccentAll and Hair
    with open("metadata/vibe_registry.json", "r") as f:
        registry = json.load(f)

    # 2. Get all objects under avatar
    resp = requests.get(f"{BASE_URL}/hierarchy?root={ACTIVE_AVATAR_ROOT}", headers=HEADERS)
    nodes = resp.json().get("nodes", [])
    
    update_count = 0
    for node in nodes:
        inst_id = node["instanceID"]
        m_resp = requests.get(f"{BASE_URL}/material/list?path={inst_id}", headers=HEADERS)
        if m_resp.status_code == 200:
            mats = m_resp.json().get("materials", [])
            for m in mats:
                idx = m["index"]
                mat_name = m["name"].lower()
                reg_key = f"{inst_id}/Slot{idx}"
                
                target_color = None
                
                # Logic: Determine what color this slot SHOULD be
                if reg_key in registry:
                    group = registry[reg_key].get("group")
                    if group == "AccentAll":
                        target_color = "1,0,0,1" # Red
                    elif "Hair" in group:
                        target_color = "1,1,1,1" # White
                
                # If not a linked accent/hair, check if it's a base black part
                if not target_color:
                    base_keywords = ["black", "pants", "sweater", "boxers", "fishnets", "body", "ears", "face", "pawpad", "tail", "hoodie", "harness"]
                    if any(k in mat_name for k in base_keywords):
                        target_color = "0,0,0,1" # Black
                
                if target_color:
                    requests.get(f"{BASE_URL}/material/set-color?path={inst_id}&index={idx}&color={target_color}", headers=HEADERS)
                    update_count += 1

    print(f"[*] Done. Surgically restored {update_count} material colors on Quest avatar.")

if __name__ == "__main__":
    restore_quest_look()
