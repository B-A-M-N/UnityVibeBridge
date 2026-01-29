#!/usr/bin/env python3
import argparse
import requests
import json
import sys
import colorsys
import os

# VibeBridge Server URL (Legacy HTTP - Swapping to Airlock Queue)
# BASE_URL = "http://localhost:8085"
REGISTRY_PATH = "/home/bamn/ALCOM/Projects/BAMN-EXTO/metadata/vibe_registry.json"
UNITY_PROJECT_PATH = "/home/bamn/ALCOM/Projects/BAMN-EXTO"
QUEUE_PATH = os.path.join(UNITY_PROJECT_PATH, "vibe_queue")
INBOX_PATH = os.path.join(QUEUE_PATH, "inbox")
OUTBOX_PATH = os.path.join(QUEUE_PATH, "outbox")

# Specific slot for body horns
HORN_SLOT = "32788/Slot1"

def unity_request_airlock(action, params):
    import uuid
    import time
    if not os.path.exists(INBOX_PATH): os.makedirs(INBOX_PATH)
    if not os.path.exists(OUTBOX_PATH): os.makedirs(OUTBOX_PATH)
    
    cmd_id = str(uuid.uuid4())
    payload = {
        "action": action,
        "id": cmd_id,
        "capability": "Admin",
        "keys": list(params.keys()),
        "values": [str(v) for v in params.values()]
    }
    
    with open(os.path.join(INBOX_PATH, f"{cmd_id}.json"), "w") as f:
        json.dump(payload, f)
    
    # We don't necessarily need to wait for the response for a "fire and forget" color set,
    # but for stability we'll give it a tiny poll or just return.
    return True

def set_avatar_color(color_str, skip_horns=False):
    if not os.path.exists(REGISTRY_PATH):
        print(f"[!] Registry not found.")
        return
    
    # 1. Load persistent settings
    settings_path = "metadata/vibe_settings.json"
    if os.path.exists(settings_path):
        with open(settings_path, "r") as f:
            settings = json.load(f)
            if not skip_horns:
                skip_horns = settings.get("skip_horns", False)

    targets = []
    try:
        with open(REGISTRY_PATH, "r") as f:
            data_raw = json.load(f)
            # Support both {"entries": []} list and legacy dict format
            entries = data_raw.get("entries", []) if isinstance(data_raw, dict) and "entries" in data_raw else []
            
            if not entries and isinstance(data_raw, dict):
                # Legacy Dict Logic
                for key, data in data_raw.items():
                    grp = data.get("group")
                    if grp == "AccentAll":
                        if skip_horns and key == HORN_SLOT: continue
                    elif grp == "Horns":
                        if skip_horns: continue
                    else: continue
                    
                    if "/" in key:
                        parts = key.split("/")
                        targets.append({"path": parts[0], "index": int(parts[1].replace("Slot", ""))})
            else:
                # Modern List Logic
                for entry in entries:
                    grp = entry.get("group")
                    if grp == "AccentAll" or grp == "Horns":
                        if skip_horns and grp == "Horns": continue
                        targets.append({
                            "path": str(entry.get("lastKnownID")), 
                            "index": entry.get("slotIndex", 0)
                        })
    except Exception as e:
        print(f"[!] Error reading registry: {e}")
        return

    print(f"[*] UPDATING: {len(targets)} requested accent slots to new color via Airlock.")
    if skip_horns: print("[*] Note: Surgical Skip applied to body metal (Horns).")
    
    for t in targets:
        # 1. Apply color
        unity_request_airlock("/material/set-color", {"path": t['path'], "index": t['index'], "color": color_str})
        # 2. Clear property blocks
        unity_request_airlock("/material/clear-block", {"path": t['path']})

    print(f"[*] Done.")

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

def hue_to_unity(hue_deg):
    r, g, b = colorsys.hls_to_rgb(hue_deg / 360.0, 0.5, 1.0)
    return f"{r:.3f},{g:.3f},{b:.3f},1.000"

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("color", help="Color name, hex, or hue")
    parser.add_argument("--skip-horns", action="store_true", help="Don't change body metal (horns)")
    args = parser.parse_args()

    COLORS = {
        "red": "1,0,0,1", "green": "0,1,0,1", "blue": "0,0,1,1",
        "teal": "0,1,1,1", "cyan": "0,1,1,1", "magenta": "1,0,1,1",
        "yellow": "1,1,0,1", "purple": "0.5,0,1,1", "black": "0,0,0,1", "white": "1,1,1,1",
        "orange": "1,0.5,0,1"
    }

    input_val = args.color.lower()
    unity_color = COLORS.get(input_val)
    if not unity_color: unity_color = hex_to_unity(input_val)
    if not unity_color:
        try: unity_color = hue_to_unity(float(input_val) % 360)
        except: pass

    if unity_color:
        set_avatar_color(unity_color, skip_horns=args.skip_horns)
    else:
        print(f"[!] Invalid color input: {args.color}")
