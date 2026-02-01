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

print("Checking MainTex for body...")
# Use material/inspect/properties again but I need a tool that returns the texture path.
# Currently material/inspect/properties only returns the property names.
# I'll check if there is a tool for that.
# VibeBridge_MaterialPayload.cs might have it.
