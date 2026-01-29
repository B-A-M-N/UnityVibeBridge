#!/usr/bin/env python3
import requests
import json

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {"X-Vibe-Token": "VIBE_777_SECURE", "X-Vibe-Capability": "Admin"}
ACTIVE_AVATAR_ROOT = "52224"

def setup_hair_links():
    print(f"[*] Setting up Hair Color Wheel groups...")
    
    # 1. Find the hair object
    resp = requests.get(f"{BASE_URL}/hierarchy?root={ACTIVE_AVATAR_ROOT}", headers=HEADERS)
    nodes = resp.json().get("nodes", [])
    hair_node = next((n for n in nodes if "Hair" in n["name"]), None)
    
    if not hair_node:
        print("[!] Could not find Hair object.")
        return

    inst_id = hair_node["instanceID"]
    
    # 2. Get the current registry
    with open("metadata/vibe_registry.json", "r") as f:
        registry = json.load(f)
    
    # 3. Define the two split groups
    # Slot 0 -> Hair1
    # Slot 1 -> Hair2
    registry[f"{inst_id}/Slot0"] = {
        "role": "Hair Half 1",
        "group": "Hair1",
        "observations": "Left/Top half of hair."
    }
    registry[f"{inst_id}/Slot1"] = {
        "role": "Hair Half 2",
        "group": "Hair2",
        "observations": "Right/Bottom half of hair."
    }

    with open("metadata/vibe_registry.json", "w") as f:
        json.dump(registry, f, indent=2)
    
    print(f"[*] Successfully linked {hair_node['name']} to 'Hair1' and 'Hair2' groups.")

if __name__ == "__main__":
    setup_hair_links()