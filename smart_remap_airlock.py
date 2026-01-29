import json
import os
import time
import uuid

INBOX = "vibe_queue/inbox"
OUTBOX = "vibe_queue/outbox"
REGISTRY_PATH = "metadata/vibe_registry.json"

import json
import os
import time
import uuid
import requests

INBOX = "vibe_queue/inbox"
OUTBOX = "vibe_queue/outbox"
REGISTRY_PATH = "metadata/vibe_registry.json"
BASE_URL = "http://127.0.0.1:8085"

def send_command(action, keys=[], values=[]):
    # Try HTTP first
    try:
        query_params = {keys[i]: values[i] for i in range(len(keys))}
        resp = requests.get(f"{BASE_URL}/{action}", params=query_params, timeout=2)
        if resp.status_code == 200:
            return resp.json()
    except Exception as e:
        print(f"HTTP failed ({action}): {e}. Falling back to Airlock...")

    # Airlock fallback
    cmd_id = str(uuid.uuid4())
    filename = f"{cmd_id}.json"
    cmd = {
        "action": action,
        "id": cmd_id,
        "keys": keys,
        "values": values
    }
    
    with open(os.path.join(INBOX, filename), "w") as f:
        json.dump(cmd, f)
        
    # Poll for response
    res_path = os.path.join(OUTBOX, f"res_{filename}")
    for _ in range(50): # 5 seconds timeout
        if os.path.exists(res_path):
            try:
                with open(res_path, "r") as f:
                    return json.load(f)
            except:
                time.sleep(0.1)
                continue
            finally:
                try: os.remove(res_path) 
                except: pass
        time.sleep(0.1)
    return None

def find_avatar_root():
    print("Scanning for Avatar Root...")
    resp = send_command("hierarchy")
    if not resp or "nodes" not in resp:
        print("Failed to get hierarchy roots.")
        return None

    for node in resp["nodes"]:
        # Heuristic: Check if this root has children like "Head" or "Body"
        # We need to fetch children first
        root_id = str(node["instanceID"])
        children_resp = send_command("hierarchy", ["root"], [root_id])
        if children_resp and "nodes" in children_resp:
            child_names = [c["name"].lower() for c in children_resp["nodes"]]
            if "head" in child_names or "body" in child_names or "armature" in child_names:
                print(f"Found Avatar Root: {node['name']} ({root_id})")
                return root_id
    print("Could not identify Avatar Root.")
    return None

def get_full_hierarchy_map(root_id):
    # Returns a dict of Name -> InstanceID
    # Uses a stack to traverse
    mapping = {}
    stack = [root_id]
    
    print("Traversing hierarchy...")
    while stack:
        current_id = stack.pop()
        resp = send_command("hierarchy", ["root"], [str(current_id)])
        if resp and "nodes" in resp:
            for node in resp["nodes"]:
                mapping[node["name"]] = node["instanceID"]
                # We assume unique names for simplicity, or last-write-wins
                # For deeper traversal, we add to stack
                # To avoid infinite loops or too deep, maybe limit? Unity hierarchy is a tree.
                # Optimization: Only traverse if we need to find something? 
                # But we need a map for the registry.
                # Only traverse if the node might contain parts? 
                # Let's traverse everything, it shouldn't be too huge.
                stack.append(node["instanceID"])
    return mapping

def remap():
    if not os.path.exists(REGISTRY_PATH):
        print("Registry not found.")
        return

    root_id = find_avatar_root()
    if not root_id:
        return

    # To be efficient, let's just do a shallow scan of the root's children first
    # As most items are direct children of the avatar (like Body, Head, etc.)
    # If items are deeper (like bones), we need full traversal.
    
    # Let's try to map based on the existing registry keys
    with open(REGISTRY_PATH, "r") as f:
        registry_data = json.load(f)
    
    # registry_data is { "entries": [ ... ] } in the new format?
    # Wait, existing 'remap_registry.py' assumed a dict: { "id/slot": ... }
    # But 'unity-package/Scripts/VibeBridgeServer.cs' uses RegistryData class with a list of entries.
    # We need to check the ACTUAL format of the registry file.
    
    is_list_format = isinstance(registry_data, dict) and "entries" in registry_data
    
    mapping = {}
    
    # We really need to know the names of the objects we are looking for.
    # Let's collect target names from the registry.
    target_names = set()
    
    # Check registry format
    if is_list_format:
        print("Registry is in NEW list format.")
        entries_to_process = registry_data["entries"]
    else:
        print("Registry is in OLD dictionary format. Migrating...")
        entries_to_process = []
        for key, val in registry_data.items():
            # Old Key: "InstanceID/SlotIndex"
            # We don't care about old InstanceID much, but SlotIndex is important.
            try:
                parts = key.split('/')
                old_id = int(parts[0])
                slot_index = int(parts[1].replace("Slot", "")) if "Slot" in parts[1] else -1
            except:
                continue

            entry = {
                "uuid": str(uuid.uuid4()),
                "role": val.get("role", ""),
                "group": val.get("group", ""),
                "lastKnownID": old_id,
                "slotIndex": slot_index,
                "fingerprint": {
                    "meshName": val.get("role", "").split(" ")[0], # Guess mesh name from role
                    "triangles": 0,
                    "vertices": 0,
                    "shaders": [],
                    "components": []
                }
            }
            entries_to_process.append(entry)

    full_map = get_full_hierarchy_map(root_id)
    
    # Aliases for better matching
    ALIASES = {
        "collar": "Collar and body chains",
        "hair": "Hair",
        "bodysuit": "body"
    }
    
    updated_count = 0
    new_entries = []

    for entry in entries_to_process:
        role = entry.get("role", "")
        # Heuristic: The role often starts with the object name, e.g. "Body metal" -> "Body"
        # Let's try to match the start of the role against hierarchy names.
        
        found_id = None
        best_match_name = None
        
        # Normalize role start
        role_start = role.split(" ")[0].lower()
        if role_start in ALIASES:
            role_start = ALIASES[role_start].lower()
        
        # 1. Strict Search
        for name, iid in full_map.items():
            n_lower = name.lower()
            r_lower = role.lower()
            
            # Exact Match of Name in Role or Role Start in Name
            if role_start == n_lower:
                best_match_name = name
                found_id = iid
                break # Exact match found
            
            # Starts with (e.g. "Body" matches "Body.001")
            if n_lower.startswith(role_start):
                # Prefer shortest match that starts with it (closest to root/simple name)
                if best_match_name is None or len(name) < len(best_match_name):
                    best_match_name = name
                    found_id = iid
        
        # 2. Loose Search (only if strict failed)
        if not found_id:
             for name, iid in full_map.items():
                n_lower = name.lower()
                # Containment check, but be careful
                if role_start in n_lower:
                    # Avoid "body" matching "Collar and body chains"
                    # The match must essentially "be" the object.
                    pass
        
        if found_id:
            print(f"Remapped '{role}' -> {best_match_name} ({found_id})")
            entry["lastKnownID"] = found_id
            updated_count += 1
        else:
            print(f"Could not find target for '{role}'")
            
        new_entries.append(entry)
            
    final_data = { "entries": new_entries }
    
    with open(REGISTRY_PATH, "w") as f:
        json.dump(final_data, f, indent=2)
        
    print(f"Remapped {updated_count} entries. Registry migrated to new format.")
    
    # Trigger reload
    send_command("registry/load")

if __name__ == "__main__":
    remap()
