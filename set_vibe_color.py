import requests
import json
import sys

BASE_URL = "http://localhost:8086"
REGISTRY_PATH = "metadata/vibe_registry.json"

COLORS = {
    "red": "1,0,0,1",
    "green": "0,1,0,1",
    "blue": "0,0,1,1",
    "white": "1,1,1,1",
    "black": "0,0,0,1"
}

def set_color(group_name, color_name):
    color_val = COLORS.get(color_name.lower(), "1,1,1,1")
    
    with open(REGISTRY_PATH, "r") as f:
        registry = json.load(f)
    
    entries = registry.get("entries", [])
    updated = 0
    
    for entry in entries:
        if entry.get("group") == group_name:
            iid = entry.get("lastKnownID")
            slot = entry.get("slotIndex", 0)
            role = entry.get("role", "Unknown")
            
            print(f"[*] Setting {role} ({iid}/Slot{slot}) to {color_name}...")
            url = f"{BASE_URL}/material/set-color?path={iid}&index={slot}&color={color_val}"
            try:
                resp = requests.get(url, timeout=2)
                if resp.status_code == 200:
                    updated += 1
            except Exception as e:
                print(f"  [!] Failed: {e}")
                
    print(f"[*] Successfully updated {updated} items in group {group_name}.")

if __name__ == "__main__":
    color = sys.argv[1] if len(sys.argv) > 1 else "red"
    set_color("AccentAll", color)
