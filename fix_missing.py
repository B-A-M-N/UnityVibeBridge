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

# IDs to enable
enable_ids = [28826, 29260]

for obj_id in enable_ids:
    print(f"Enabling ID: {obj_id}")
    cmd = {"action":"object/set-active","capability":"write","keys":["path", "active"],"values":[str(obj_id), "true"]}
    res = send_and_receive(cmd, f"enable_{obj_id}")
    if res:
        print(json.dumps(res, indent=2))
    else:
        print("  No response.")

