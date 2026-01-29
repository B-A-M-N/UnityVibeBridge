#!/usr/bin/env python3
import requests

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {"X-Vibe-Token": "VIBE_777_SECURE", "X-Vibe-Capability": "Admin"}

def surgical_audit():
    path = "52754"
    index = "4"
    print(f"[*] Surgical property audit for {path} Slot {index}...")
    
    # Common color properties in various shaders
    props = [
        "_Color", "_BaseColor", "_MainColor", "_EmissionColor", 
        "_ColorWheel", "_AccentColor", "_GlowColor", "_RimColor",
        "_UvAnimColor", "_LayerColor", "_OverlayColor"
    ]
    
    for p in props:
        # We use inspect-slot which I just added to the bridge
        r = requests.get(f"{BASE_URL}/material/get-property?path={path}&index={index}&field={p}", headers=HEADERS)
        if r.status_code == 200:
            print(f"  {p}: {r.text}")

if __name__ == "__main__":
    surgical_audit()
