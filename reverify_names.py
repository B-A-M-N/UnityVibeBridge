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
    
    for _ in range(30):
        time.sleep(0.1)
        res_file = os.path.join(outbox, "res_" + filename + ".json")
        if os.path.exists(res_file):
            with open(res_file, 'r') as f:
                return json.load(f)
    return None

print("Re-verifying Body and Pants...")
res_body = send_and_receive({"action":"inspect","capability":"read","keys":["path"],"values":["body"]}, "reinspect_body")
res_pants = send_and_receive({"action":"inspect","capability":"read","keys":["path"],"values":["Cyberpants_by_Grey"]}, "reinspect_pants")

print("Body:", json.dumps(res_body, indent=2))
print("Pants:", json.dumps(res_pants, indent=2))
