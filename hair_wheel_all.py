#!/usr/bin/env python3
import argparse
import requests
import json
import os

BASE_URL = "http://localhost:8085"
REGISTRY_PATH = "metadata/vibe_registry.json"
HEADERS = {"X-Vibe-Token": "VIBE_777_SECURE", "X-Vibe-Capability": "Admin"}

COLORS = {
    "red": "1,0,0,1", "green": "0,1,0,1", "blue": "0,0,1,1",
    "teal": "0,1,1,1", "cyan": "0,1,1,1", "magenta": "1,0,1,1",
    "yellow": "1,1,0,1", "purple": "0.5,0,1,1", "black": "0,0,0,1", "white": "1,1,1,1"
}

def hex_to_unity(hex_color):
    hex_color = hex_color.lstrip('#')
    if len(hex_color) == 3: hex_color = ''.join([c*2 for c in hex_color])
    if len(hex_color) == 6:
        try:
            r = int(hex_color[0:2], 16) / 255.0
            g = int(hex_color[2:4], 16) / 255.0
            b = int(hex_color[4:6], 16) / 255.0
            return f"{r:.3f},{g:.3f},{b:.3f},1.000"
        except: return None
    return None

def set_group_color(group_name, color_input):
    unity_color = COLORS.get(color_input.lower())
    if not unity_color: unity_color = hex_to_unity(color_input)
    if not unity_color:
        print(f"[!] Invalid color: {color_input}")
        return

    if not os.path.exists(REGISTRY_PATH): return
    with open(REGISTRY_PATH, "r") as f:
        registry = json.load(f)
        for key, data in registry.items():
            if data.get("group") == group_name:
                parts = key.split("/")
                requests.get(f"{BASE_URL}/material/set-color?path={parts[0]}&index={parts[1].replace('Slot', '')}&color={unity_color}", headers=HEADERS)

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Hair All Wheel - Changes entire hair color")
    parser.add_argument("color", help="Color to apply")
    args = parser.parse_args()

    print(f"[*] Updating ALL hair to {args.color}...")
    set_group_color("Hair1", args.color)
    set_group_color("Hair2", args.color)
    print("[*] Done.")
