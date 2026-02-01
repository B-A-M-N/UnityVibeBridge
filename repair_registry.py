import json
import os

def repair_registry():
    path = "metadata/vibe_registry.json"
    if not os.path.exists(path):
        print("Registry not found.")
        return

    with open(path, "r") as f:
        data = json.load(f)

    for entry in data["entries"]:
        if entry["role"] == "Body Horns Slot 3":
            entry["lastKnownID"] = 28866
            entry["slotIndex"] = 1
            entry["fingerprint"]["meshName"] = "body"
            # We don't have exact verts/tris from inspect tool, but updating ID and slot should fix it for now
            print(f"Repaired: {entry['role']}")
        
        if entry["role"] == "body !!METAL (Blackout)":
            entry["lastKnownID"] = 28866
            entry["slotIndex"] = 1 # Also Horns?
            print(f"Repaired: {entry['role']}")

    with open(path, "w") as f:
        json.dump(data, f, indent=2)
    print("Registry repair complete.")

if __name__ == "__main__":
    repair_registry()
