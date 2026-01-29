#!/usr/bin/env python3
import requests
import json

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {"X-Vibe-Token": "VIBE_777_SECURE", "X-Vibe-Capability": "Admin"}
ACTIVE_AVATAR_ROOT = "32270"

# Mappings of mesh objects to their likely textures
TEXTURE_MAPPINGS = {
    "body": {"slot": 2, "tex": "Assets/✮exto/text/BnWTexture.png"},
    "Head": {"slot": 4, "tex": "Assets/✮exto/text/EyeTexv3_Cateye_Kri.png"}, # eyes
    "Centiped tail": {"slot": 0, "tex": "Assets/✮exto/centiped tail_centiped tail_Albedo.png"}
}

def restore_textures():
    print("[*] Restoring textures to Quest materials...")
    
    resp = requests.get(f"{BASE_URL}/hierarchy?root={ACTIVE_AVATAR_ROOT}", headers=HEADERS)
    if resp.status_code != 200:
        print("[!] Failed to get hierarchy.")
        return
        
    nodes = resp.json().get("nodes", [])
    name_to_id = {n["name"]: n["instanceID"] for n in nodes}

    for obj_name, config in TEXTURE_MAPPINGS.items():
        inst_id = name_to_id.get(obj_name)
        if inst_id:
            print(f"[*] Assigning {config['tex']} to {obj_name} (Slot {config['slot']})...")
            url = f"{BASE_URL}/material/set-slot-texture?path={inst_id}&index={config['slot']}&field=_MainTex&value={config['tex']}"
            r = requests.get(url, headers=HEADERS)
            if r.status_code == 200:
                print(f"  [+] Success: {r.json().get('message')}")
            else:
                print(f"  [-] Failed: {r.status_code} - {r.text}")

if __name__ == "__main__":
    restore_textures()