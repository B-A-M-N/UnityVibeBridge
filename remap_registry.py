import json
import os

REGISTRY_PATH = "metadata/vibe_registry.json"
CURRENT_HIERARCHY = {
    "body": "32788",
    "Collar and body chains": "32158",
    "warmers and glove": "32410",
    "skeleton hoodie": "32760",
    "Cyberpants_by_Grey": "32132",
    "Belt": "32352",
    "garter": "32472",
    "Boots": "33156",
    "Head": "33518"
}

def remap():
    if not os.path.exists(REGISTRY_PATH):
        print("Registry not found.")
        return

    with open(REGISTRY_PATH, "r") as f:
        registry = json.load(f)

    new_registry = {}
    # We map old keys (stale_id/slot) to new keys (current_id/slot)
    # based on the name contained in the role/observations or just by matching the name.
    
    # Actually, the user's registry has names in the 'role' field.
    # "76624/Slot1": { "role": "body !!METAL", ... }
    
    for old_key, data in registry.items():
        role = data.get("role", "").lower()
        old_id, slot = old_key.split("/")
        
        found_new_id = None
        for name, new_id in CURRENT_HIERARCHY.items():
            if name.lower() in role:
                found_new_id = new_id
                break
        
        if found_new_id:
            new_key = f"{found_new_id}/{slot}"
            new_registry[new_key] = data
        else:
            # Keep as is if not found, maybe it's still valid or we don't have the mapping
            new_registry[old_key] = data

    with open(REGISTRY_PATH, "w") as f:
        json.dump(new_registry, f, indent=2)
    print("Registry remapped with current InstanceIDs.")

if __name__ == "__main__":
    remap()
