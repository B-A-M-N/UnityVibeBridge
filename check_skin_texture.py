import os
import json
import time

project_path = "/home/bamn/ALCOM/Projects/BAMN-EXTO"
inbox = os.path.join(project_path, "vibe_queue/inbox")
outbox = os.path.join(project_path, "vibe_queue/outbox")

def send_and_receive(cmd, filename):
    cmd_id = filename
    cmd["id"] = cmd_id
    with open(os.path.join(inbox, filename + ".json"), 'w') as f:
        json.dump(cmd, f)
    
    for _ in range(50):
        time.sleep(0.1)
        res_file = os.path.join(outbox, "res_" + filename + ".json")
        if os.path.exists(res_file):
            with open(res_file, 'r') as f:
                return json.load(f)
    return None

print("Checking Body Material Properties...")
cmd = {
    "action": "material/inspect/properties",
    "capability": "read",
    "keys": ["path", "index"],
    "values": ["28834", "2"]
}
res = send_and_receive(cmd, "inspect_mat_prop")
print("Material Properties:", json.dumps(res, indent=2))
