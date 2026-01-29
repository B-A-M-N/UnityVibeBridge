#!/usr/bin/env python3
import argparse
import requests
import json
import sys
import colorsys
import os

# VibeBridge Server URL
BASE_URL = "http://localhost:8085"
HEADERS = {
    "X-Vibe-Token": "VIBE_777_SECURE",
    "X-Vibe-Capability": "Admin"
}

def set_color(input_val):
    print(f"[*] Connecting to VibeBridge at {BASE_URL}...")
    
    # 1. Start Transaction
    try:
        resp = requests.get(f"{BASE_URL}/transaction/begin?name=FixCollar_{input_val}", headers=HEADERS)
        if resp.status_code != 200:
            print(f"[!] Failed to start transaction: {resp.status_code}")
            return
        tx_id = resp.json().get("id")
    except Exception as e:
        print(f"[!] Connection failed: {e}")
        return

    # Collar IDs and Slots
    # Metal (0), Pawpad (1), Black 2 (2)
    collar_id = "-31130"
    slots = [0, 1, 2]
    unity_color_str = "0,1,1,1" # Cyan/Teal
    
    print(f"[*] Force-applying teal to Collar and Chains (all slots)...")
    
    for slot in slots:
        # We use set-slot-color which targets a specific field, 
        # but the MaterialModule also has SetMaterialColor which targets both _Color and _EmissionColor
        # Let's use the more aggressive one: /material/set-color?path={path}&index={index}&color={color}
        url = f"{BASE_URL}/material/set-color?path={collar_id}&index={slot}&color={unity_color_str}"
        try:
            r = requests.get(url, headers=HEADERS, timeout=2)
            if r.status_code == 200:
                print(f"[*] Updated Collar Slot {slot}")
            else:
                print(f"[!] Failed Collar Slot {slot}: {r.status_code}")
        except:
            print(f"[!] Timeout for Slot {slot}")

    # 5. Commit
    requests.get(f"{BASE_URL}/transaction/commit?id={tx_id}", headers=HEADERS)
    print(f"[*] Collar update complete.")

if __name__ == "__main__":
    set_color("cyan")
