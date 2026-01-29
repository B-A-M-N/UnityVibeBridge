#!/usr/bin/env python3
import argparse
import requests
import json
import os

# BASE_URL = "http://localhost:8085"
REGISTRY_PATH = "metadata/vibe_registry.json"
UNITY_PROJECT_PATH = "/home/bamn/ALCOM/Projects/BAMN-EXTO"
QUEUE_PATH = os.path.join(UNITY_PROJECT_PATH, "vibe_queue")
INBOX_PATH = os.path.join(QUEUE_PATH, "inbox")

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

def unity_request_airlock(action, params):
    import uuid
    if not os.path.exists(INBOX_PATH): os.makedirs(INBOX_PATH)
    cmd_id = str(uuid.uuid4())
    payload = {"action": action, "id": cmd_id, "capability": "Admin", "keys": list(params.keys()), "values": [str(v) for v in params.values()]}
    with open(os.path.join(INBOX_PATH, f"{cmd_id}.json"), "w") as f: json.dump(payload, f)
    return True

def set_secondary_color(color_input):
    unity_color = COLORS.get(color_input.lower())
    if not unity_color: unity_color = hex_to_unity(color_input)
    if not unity_color:
        print(f"[!] Invalid color: {color_input}")
        return

    if not os.path.exists(REGISTRY_PATH): return
    
    count = 0
    with open(REGISTRY_PATH, "r") as f:
        registry = json.load(f)
        for key, data in registry.items():
            if data.get("group") == "Secondary":
                parts = key.split("/")
                unity_request_airlock("/material/set-color", {"path": parts[0], "index": parts[1].replace('Slot', ''), "color": unity_color})
                unity_request_airlock("/material/clear-block", {"path": parts[0]})
                count += 1
    
    print(f"[*] Secondary Accent updated to {color_input} ({count} slots).")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Secondary Color Wheel")
    parser.add_argument("color", help="Color to apply")
    args = parser.parse_args()
    set_secondary_color(args.color)
