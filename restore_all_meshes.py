#!/usr/bin/env python3
import requests
import json
import re

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {
    "X-Vibe-Token": "VIBE_777_SECURE",
    "X-Vibe-Capability": "Admin"
}
ACTIVE_AVATAR_ROOT = "52224"

def restore_all():
    print(f"[*] Auditing meshes for active avatar: {ACTIVE_AVATAR_ROOT}")
    resp = requests.get(f"{BASE_URL}/opt/meshes?path={ACTIVE_AVATAR_ROOT}", headers=HEADERS)
    if resp.status_code != 200:
        print("[!] Failed to get meshes.")
        return

    meshes = resp.json().get("meshes", [])
    
    h_resp = requests.get(f"{BASE_URL}/hierarchy?root={ACTIVE_AVATAR_ROOT}", headers=HEADERS)
    nodes = h_resp.json().get("nodes", [])
    name_to_id = {n["name"]: n["instanceID"] for n in nodes}

    success_count = 0
    for m in meshes:
        obj_name = m["name"]
        mesh_name = m["meshName"]
        
        # Suffix stripping logic: body_Simplified_19b5 -> body
        clean_mesh_name = re.sub(r"_Simplified_[a-f0-9]+$", "", mesh_name)
        
        inst_id = name_to_id.get(obj_name)
        if inst_id:
            print(f"[*] Restoring {obj_name} (Original Name: {clean_mesh_name})...")
            r_resp = requests.get(f"{BASE_URL}/opt/mesh/restore?path={inst_id}&meshName={clean_mesh_name}", headers=HEADERS)
            if r_resp.status_code == 200:
                res = r_resp.json()
                if "error" not in res:
                    print(f"  [+] Success: {res.get('message')}")
                    success_count += 1
                else:
                    print(f"  [-] Failed: {res.get('error')}")
            else:
                print(f"  [-] HTTP Error: {r_resp.status_code}")

    print(f"[*] Mesh restoration complete. {success_count}/{len(meshes)} processed.")

if __name__ == "__main__":
    restore_all()