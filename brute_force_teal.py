#!/usr/bin/env python3
import requests
import json

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {
    "X-Vibe-Token": "VIBE_777_SECURE",
    "X-Vibe-Capability": "Admin"
}

def brute_force_teal():
    # 1. Get ALL nodes in the scene
    resp = requests.get(f"{BASE_URL}/hierarchy", headers=HEADERS)
    roots = resp.json().get("nodes", [])
    
    all_objects = []
    for root in roots:
        all_objects.append(root)
        # Get children
        child_resp = requests.get(f"{BASE_URL}/hierarchy?root={root['instanceID']}", headers=HEADERS)
        if child_resp.status_code == 200:
            all_objects.extend(child_resp.json().get("nodes", []))

    print(f"[*] Scanning {len(all_objects)} objects for target materials...")
    
    target_keywords = ["metal", "pawpad", "accent", "collar", "chains", "lofi"]
    update_count = 0
    
    for obj in all_objects:
        inst_id = obj["instanceID"]
        # List materials
        m_resp = requests.get(f"{BASE_URL}/material/list?path={inst_id}", headers=HEADERS)
        if m_resp.status_code == 200:
            mats = m_resp.json().get("materials", [])
            for m in mats:
                mat_name = m["name"].lower()
                idx = m["index"]
                
                # If name matches any keyword, force teal
                if any(k in mat_name for k in target_keywords):
                    print(f"[*] Found '{m['name']}' on '{obj['name']}'. Applying Red...")
                    # Set both _Color and _EmissionColor via /material/set-color
                    requests.get(f"{BASE_URL}/material/set-color?path={inst_id}&index={idx}&color=1,0,0,1", headers=HEADERS)
                    # Also try specific slot fields to be sure
                    requests.get(f"{BASE_URL}/material/set-slot-color?path={inst_id}&index={idx}&field=_Color&value=1,0,0,1", headers=HEADERS)
                    requests.get(f"{BASE_URL}/material/set-slot-color?path={inst_id}&index={idx}&field=_EmissionColor&value=1,0,0,1", headers=HEADERS)
                    update_count += 1

    print(f"[*] Brute-force complete. Updated {update_count} material slots.")

if __name__ == "__main__":
    brute_force_teal()
