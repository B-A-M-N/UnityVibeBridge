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

# Try renaming "Belt" to "Belt_Test" and back
cmd = {"action":"object/rename","capability":"write","keys":["path", "newName"],"values":["28428", "Belt_Test"]}
res = send_and_receive(cmd, "test_rename")
if res:
    print(json.dumps(res, indent=2))
    # Rename back
    cmd2 = {"action":"object/rename","capability":"write","keys":["path", "newName"],"values":["28428", "Belt"]}
    send_and_receive(cmd2, "test_rename_back")
else:
    print("No response.")
