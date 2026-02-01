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

print("Inspecting 28866 and 28830...")
res1 = send_and_receive({"action":"inspect","capability":"read","keys":["path"],"values":["28866"]}, "inspect_28866")
res2 = send_and_receive({"action":"inspect","capability":"read","keys":["path"],"values":["28830"]}, "inspect_28830")

print("28866:", json.dumps(res1, indent=2))
print("28830:", json.dumps(res2, indent=2))
