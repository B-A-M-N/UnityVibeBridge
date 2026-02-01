import os
import json
import time
import random

project_path = "/home/bamn/ALCOM/Projects/BAMN-EXTO"
inbox = os.path.join(project_path, "vibe_queue/inbox")
outbox = os.path.join(project_path, "vibe_queue/outbox")

def send_and_receive(cmd, filename):
    # Add random nonce to avoid cache
    filename = f"{filename}_{random.randint(1000,9999)}"
    with open(os.path.join(inbox, filename + ".json"), 'w') as f:
        json.dump(cmd, f)
    
    for _ in range(20):
        time.sleep(0.5)
        res_file = os.path.join(outbox, "res_" + filename + ".json")
        if os.path.exists(res_file):
            with open(res_file, 'r') as f:
                return json.load(f)
    return None

cmd = {"action":"system/list/tools","capability":"read","keys":[],"values":[]}
res = send_and_receive(cmd, "list_tools_final")
if res:
    print(json.dumps(res, indent=2))
else:
    print("No response.")
