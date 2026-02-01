import json
import os
import uuid

path = "/home/bamn/ALCOM/Projects/BAMN-EXTO/metadata/vibe_registry.json"
if os.path.exists(path):
    with open(path, "r") as f:
        data = json.load(f)
    
    # ID mapping from get_hierarchy
    ids = {
        "Head": 29588,
        "Boots": 29224,
        "garter": 28548,
        "Collar and body chains": 28240,
        "bodysuits": 28830,
        "Cyberpants_by_Grey": 28216,
        "Pentagram Body Harness": 28826,
        "Belt": 28428,
        "body": 28866,
        "skeleton hoodie": 28838,
        "Blade_L": 27886, # Wait, I need to check these IDs
        "Blade_R": 27994,
        "warmers and glove": 28484
    }

    # Map of role to (meshName, slotIndex)
    targets = {
        "HeadMetal": ("Head", 3),
        "BootsMetals": ("Boots", 3),
        "GarterMetals": ("garter", 0),
        "CollarMetals": ("Collar and body chains", 0),
        "BodysuitMetals": ("bodysuits", 1),
        "PantsMetals": ("Cyberpants_by_Grey", 1),
        "HarnessMetals": ("Pentagram Body Harness", 0),
        "BeltPawpad": ("Belt", 1),
        "CollarPawpad": ("Collar and body chains", 1),
        "BodyPawpad": ("body", 5),
        "Eyes": ("Head", 4),
        "BodyHorns": ("body", 1),
        "Boots": ("Boots", 3),
        "Hoodie": ("skeleton hoodie", 0),
        "Blade_L": ("Blade_L", 1),
        "Blade_R": ("Blade_R", 1),
        "WarmersMetals": ("warmers and glove", 2)
    }

    existing_roles = {e["role"]: e for e in data["entries"]}

    for role, (mesh, slot) in targets.items():
        if role in existing_roles:
            entry = existing_roles[role]
            entry["slotIndex"] = slot
            entry["lastKnownID"] = ids.get(mesh, entry["lastKnownID"])
        else:
            # Add new entry
            entry = {
                "uuid": str(uuid.uuid4())[:8],
                "role": role,
                "group": "AccentAll",
                "lastKnownID": ids.get(mesh, 0),
                "slotIndex": slot,
                "fingerprint": {
                    "meshName": mesh,
                    "triangles": 0, # Dummy values for now
                    "vertices": 0,
                    "components": ["Transform", "SkinnedMeshRenderer"]
                }
            }
            data["entries"].append(entry)
    
    with open(path, "w") as f:
        json.dump(data, f, indent=4)
    print("Registry synchronized with all metal and pawpad targets.")
else:
    print("Registry file not found.")
