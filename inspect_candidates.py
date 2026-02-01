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

print("Inspecting candidates...")

# Inspect ExtoPc (Potential Root)
cmd = {
    "action": "inspect",
    "capability": "read",
    "keys": ["path"],
    "values": ["28338"]
}
res = send_and_receive(cmd, "inspect_root")
print("Root Inspection:", json.dumps(res, indent=2))

# Inspect Cyberpants_by_Grey (Potential Body Suit)
cmd["values"] = ["28214"]
res = send_and_receive(cmd, "inspect_pants")
print("Pants Inspection:", json.dumps(res, indent=2))

# Check for a 'Body' mesh specifically if it exists under root
cmd = {
    "action": "system/search",
    "capability": "read",
    "keys": ["term", "recursive"],
    "values": ["Body", "true"]
}
res = send_and_receive(cmd, "deep_search_body")
print("Deep Search Body:", json.dumps(res, indent=2))
