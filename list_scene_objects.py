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

# Get all objects
cmd = {"action":"system/find-by-component","capability":"read","keys":["type"],"values":["Transform"]}
res = send_and_receive(cmd, "list_all_transforms")

if res and res.get("results"):
    for obj in res["results"]:
        name = obj["message"]
        if any(x in name for x in ["Harness", "tail", "Belt"]):
            print(f"Match: {name} (ID: {obj['id']})")
else:
    print("Failed to list objects.")
