import os
import json
import time

project_path = "/home/bamn/ALCOM/Projects/BAMN-EXTO"
inbox = os.path.join(project_path, "vibe_queue/inbox")
outbox = os.path.join(project_path, "vibe_queue/outbox")

def send_and_receive(cmd, filename):
    with open(os.path.join(inbox, filename + ".json"), 'w') as f:
        json.dump(cmd, f)
    for _ in range(10):
        time.sleep(0.5)
        res_file = os.path.join(outbox, "res_" + filename + ".json")
        if os.path.exists(res_file):
            with open(res_file, 'r') as f:
                return json.load(f)
    return None

cmd = {"action":"telemetry_get_errors","capability":"read","keys":[],"values":[]}
res = send_and_receive(cmd, "get_errors_flat")
print(res)
