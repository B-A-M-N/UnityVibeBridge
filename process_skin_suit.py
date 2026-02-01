import os
import json
import time
import random

project_path = "/home/bamn/ALCOM/Projects/BAMN-EXTO"
inbox = os.path.join(project_path, "vibe_queue/inbox")
outbox = os.path.join(project_path, "vibe_queue/outbox")

def send_and_receive(cmd, base_filename):
    uid = str(random.randint(10000, 99999))
    filename = f"{base_filename}_{uid}"
    
    req_path = os.path.join(inbox, filename + ".json")
    with open(req_path, 'w') as f:
        json.dump(cmd, f)
        
    print(f"Sent request: {cmd['action']} for {cmd['values'][0]}")
    
    for _ in range(40): # Wait 20s
        time.sleep(0.5)
        res_file = os.path.join(outbox, "res_" + filename + ".json")
        if os.path.exists(res_file):
            with open(res_file, 'r') as f:
                try:
                    return json.load(f)
                except:
                    return {"raw": f.read()}
    return {"error": "Timeout"}

targets = [
    {"name": "Body", "id": "28866"},
    {"name": "Bodysuits", "id": "28830"}
]

for t in targets:
    print(f"--- Processing {t['name']} ({t['id']}) ---")
    
    # 1. Snapshot
    cmd_snap = {"action":"material_snapshot","capability":"write","keys":["path"],"values":[t['id']]}
    res_snap = send_and_receive(cmd_snap, f"snap_{t['name']}")
    print(f"Snapshot Result: {res_snap}")

    # 2. Bake (Poiyomi Lock)
    cmd_bake = {"action":"material_poiyomi_lock","capability":"write","keys":["path"],"values":[t['id']]}
    res_bake = send_and_receive(cmd_bake, f"bake_{t['name']}")
    print(f"Bake Result: {res_bake}")
    print("\n")
