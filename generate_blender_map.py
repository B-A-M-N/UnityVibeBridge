#!/usr/bin/env python3
import requests
import json

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {"X-Vibe-Token": "VIBE_777_SECURE", "X-Vibe-Capability": "Admin"}
ACTIVE_AVATAR_ROOT = "76104"

def generate_blender_map():
    print(f"[*] Generating Blender Texture Map for avatar {ACTIVE_AVATAR_ROOT}...")
    
    resp = requests.get(f"{BASE_URL}/hierarchy?root={ACTIVE_AVATAR_ROOT}", headers=HEADERS)
    nodes = resp.json().get("nodes", [])
    
    blender_map = {}
    
    for node in nodes:
        inst_id = node["instanceID"]
        # Audit gives us material and texture info
        m_resp = requests.get(f"{BASE_URL}/material/list?path={inst_id}", headers=HEADERS)
        if m_resp.status_code == 200:
            mats = m_resp.json().get("materials", [])
            obj_mats = []
            for m in mats:
                idx = m["index"]
                # Get the texture path for _MainTex
                # We need a way to get texture path from the bridge
                # I'll use a placeholder for now since the bridge needs a 'get-texture-path' endpoint
                obj_mats.append({
                    "slot": idx,
                    "material": m["name"],
                    "note": "Re-apply original PC textures in Blender."
                })
            if obj_mats:
                blender_map[node["name"]] = obj_mats

    with open("metadata/blender_import_map.json", "w") as f:
        json.dump(blender_map, f, indent=2)
    
    print("[*] Map generated: metadata/blender_import_map.json")

if __name__ == "__main__":
    generate_blender_map()
