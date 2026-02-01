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

print("Searching for objects...")
# Search for objects containing "Body" or "Skin"
cmd = {
    "action": "system/search",
    "capability": "read",
    "keys": ["term"],
    "values": ["Body"]
}
res = send_and_receive(cmd, "search_body")
print("Search Body Results:", json.dumps(res, indent=2))

cmd["values"] = ["Skin"]
res = send_and_receive(cmd, "search_skin")
print("Search Skin Results:", json.dumps(res, indent=2))
