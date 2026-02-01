import os
import json
import time
import random

project_path = "/home/bamn/ALCOM/Projects/BAMN-EXTO"
inbox = os.path.join(project_path, "vibe_queue/inbox")
outbox = os.path.join(project_path, "vibe_queue/outbox")

def send_and_receive(cmd, filename):
    filename = f"{filename}_{random.randint(1000,9999)}"
    with open(os.path.join(inbox, filename + ".json"), 'w') as f:
        json.dump(cmd, f)
    for _ in range(60): 
        time.sleep(1.0)
        res_file = os.path.join(outbox, "res_" + filename + ".json")
        if os.path.exists(res_file):
            with open(res_file, 'r') as f:
                return json.load(f)
    return None

targets = ["28866", "28830"]

for tid in targets:
    print(f"Triggering Poiyomi Lock/Bake for target: {tid}...")
    # Using action name that directly maps to method name with underscores
    cmd = {"action":"material_poiyomi_lock","capability":"write","keys":["path"],"values":[tid]}
    res = send_and_receive(cmd, f"bake_v5_{tid}")
    print(f"  Result: {json.dumps(res, indent=2) if res else 'Timeout'}")

