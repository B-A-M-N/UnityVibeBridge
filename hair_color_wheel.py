#!/usr/bin/env python3
import argparse
import requests
import json
import os

# BASE_URL = "http://localhost:8085"
REGISTRY_PATH = "/home/bamn/ALCOM/Projects/BAMN-EXTO/metadata/vibe_registry.json"
UNITY_PROJECT_PATH = "/home/bamn/ALCOM/Projects/BAMN-EXTO"
QUEUE_PATH = os.path.join(UNITY_PROJECT_PATH, "vibe_queue")
INBOX_PATH = os.path.join(QUEUE_PATH, "inbox")

COLORS = {
    "red": "1,0,0,1",
    "green": "0,1,0,1",
    "blue": "0,0,1,1",
    "teal": "0,1,1,1",
    "cyan": "0,1,1,1",
    "magenta": "1,0,1,1",
    "yellow": "1,1,0,1",
    "purple": "0.5,0,1,1",
    "black": "0,0,0,1",
    "white": "1,1,1,1"
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

def get_targets_for_group(group_name):
    if not os.path.exists(REGISTRY_PATH): return []
    targets = []
    with open(REGISTRY_PATH, "r") as f:
        data_raw = json.load(f)
        # Support both {"entries": []} list and legacy dict format
        entries = data_raw.get("entries", []) if isinstance(data_raw, dict) and "entries" in data_raw else []
        if not entries and isinstance(data_raw, dict):
            # Fallback to dict-style iteration
            for key, data in data_raw.items():
                if data.get("group") == group_name:
                    if "/" in key:
                        parts = key.split("/")
                        targets.append({"path": parts[0], "index": int(parts[1].replace("Slot", ""))})
        else:
            # List-style iteration
            for entry in entries:
                if entry.get("group") == group_name:
                    targets.append({
                        "path": str(entry.get("lastKnownID")), 
                        "index": entry.get("slotIndex", 0)
                    })
    return targets

def unity_request_airlock(action, params):
    import uuid
    if not os.path.exists(INBOX_PATH): os.makedirs(INBOX_PATH)
    cmd_id = str(uuid.uuid4())
    payload = {"action": action, "id": cmd_id, "capability": "Admin", "keys": list(params.keys()), "values": [str(v) for v in params.values()]}
    with open(os.path.join(INBOX_PATH, f"{cmd_id}.json"), "w") as f: json.dump(payload, f)
    return True

def set_color(group_name, color_input):
    unity_color = COLORS.get(color_input.lower())
    if not unity_color: unity_color = hex_to_unity(color_input)
    
    if not unity_color:
        print(f"[!] Invalid color: {color_input}")
        return

    targets = get_targets_for_group(group_name)
    if not targets:
        return

    for t in targets:
        unity_request_airlock("/material/set-color", {"path": t['path'], "index": t['index'], "color": unity_color})
        unity_request_airlock("/material/clear-block", {"path": t['path']})

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Hair Color Wheel System")
    parser.add_argument("wheel", choices=["all", "split1", "split2"], help="Which 'wheel' to use")
    parser.add_argument("color", help="Color to apply")
    args = parser.parse_args()

    if args.wheel == "all":
        print(f"[*] Updating ALL hair to {args.color}...")
        set_color("Hair1", args.color)
        set_color("Hair2", args.color)
    elif args.wheel == "split1":
        print(f"[*] Updating Hair Half 1 to {args.color}...")
        set_color("Hair1", args.color)
    elif args.wheel == "split2":
        print(f"[*] Updating Hair Half 2 to {args.color}...")
        set_color("Hair2", args.color)

    print("[*] Done.")