#!/usr/bin/env python3
import requests

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {
    "X-Vibe-Token": "VIBE_777_SECURE",
    "X-Vibe-Capability": "Admin"
}

def aggressive_fix_collar():
    # Target both the active and backup collar just in case
    collar_ids = ["-31130", "-55804"]
    
    for cid in collar_ids:
        print(f"[*] Aggressively fixing Collar/Chains: {cid}")
        # Standard Shader properties
        for slot in [0, 1, 2]:
            # Set Color (Albedo)
            requests.get(f"{BASE_URL}/material/set-slot-color?path={cid}&index={slot}&field=_Color&value=0,1,1,1", headers=HEADERS)
            # Set Emission Color
            requests.get(f"{BASE_URL}/material/set-slot-color?path={cid}&index={slot}&field=_EmissionColor&value=0,1,1,1", headers=HEADERS)
            # Set Base Color (URP/HDRP/Poiyomi variant)
            requests.get(f"{BASE_URL}/material/set-slot-color?path={cid}&index={slot}&field=_BaseColor&value=0,1,1,1", headers=HEADERS)
            
            # Since we can't directly enable keywords via the current API,
            # we try to set common 'Enable' floats if they exist
            requests.get(f"{BASE_URL}/material/set-slot-color?path={cid}&index={slot}&field=_EmissionEnabled&value=1", headers=HEADERS)
            requests.get(f"{BASE_URL}/material/set-slot-color?path={cid}&index={slot}&field=_EnableEmission&value=1", headers=HEADERS)

    print("[*] Aggressive fix complete.")

if __name__ == "__main__":
    aggressive_fix_collar()
