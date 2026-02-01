import os
import json
import time
import random

project_path = "/home/bamn/ALCOM/Projects/BAMN-EXTO"
inbox = os.path.join(project_path, "vibe_queue/inbox")
outbox = os.path.join(project_path, "vibe_queue/outbox")

def send_and_receive(cmd, base_filename):
    # Use unique ID to avoid stale reads
    uid = str(random.randint(10000, 99999))
    filename = f"{base_filename}_{uid}"
    
    req_path = os.path.join(inbox, filename + ".json")
    with open(req_path, 'w') as f:
        json.dump(cmd, f)
        
    print(f"Sent request: {filename}")
    
    for _ in range(20): # Wait 10s
        time.sleep(0.5)
        res_file = os.path.join(outbox, "res_" + filename + ".json")
        if os.path.exists(res_file):
            with open(res_file, 'r') as f:
                try:
                    return json.load(f)
                except:
                    return {"raw": f.read()}
    return None

cmd = {"action":"debug_find_optimizer","capability":"read","keys":[],"values":[]}
res = send_and_receive(cmd, "find_opt")
print("Optimizer Assemblies:", json.dumps(res, indent=2))