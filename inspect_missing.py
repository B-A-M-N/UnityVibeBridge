import os
import json
import time

project_path = "/home/bamn/ALCOM/Projects/BAMN-EXTO"
inbox = os.path.join(project_path, "vibe_queue/inbox")
outbox = os.path.join(project_path, "vibe_queue/outbox")

def send_and_receive(cmd, filename):
    with open(os.path.join(inbox, filename + ".json"), 'w') as f:
        json.dump(cmd, f)
    
    # Wait for response
    for _ in range(20):
        time.sleep(0.5)
        res_file = os.path.join(outbox, "res_" + filename + ".json")
        if os.path.exists(res_file):
            with open(res_file, 'r') as f:
                return json.load(f)
    return None

ids_to_check = {
    "Pentagram Body Harness": 28826,
    "Centiped tail": 29260,
    "Belt": 28428
}

for name, obj_id in ids_to_check.items():
    print(f"\nInspecting {name} (ID: {obj_id})")
    cmd = {"action":"inspect","capability":"read","keys":["path"],"values":[str(obj_id)]}
    res = send_and_receive(cmd, f"inspect_{obj_id}")
    if res:
        print(json.dumps(res, indent=2))
    else:
        print("  No response.")

