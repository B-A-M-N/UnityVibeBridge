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

ids = ["29118", "29084"]
for i in ids:
    print(f"Inspecting {i}...")
    cmd = {
        "action": "inspect",
        "capability": "read",
        "keys": ["path"],
        "values": [str(i)]
    }
    res = send_and_receive(cmd, f"inspect_{i}")
    print(json.dumps(res, indent=2))
    
    # Also list materials
    cmd_mat = {
        "action": "material/list",
        "capability": "read",
        "keys": ["path"],
        "values": [str(i)]
    }
    res_mat = send_and_receive(cmd_mat, f"mat_list_{i}")
    print(json.dumps(res_mat, indent=2))