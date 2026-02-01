import os
import json
import time

project_path = "/home/bamn/ALCOM/Projects/BAMN-EXTO"
inbox = os.path.join(project_path, "vibe_queue/inbox")
outbox = os.path.join(project_path, "vibe_queue/outbox")

def send_and_receive(cmd, filename):
    with open(os.path.join(inbox, filename + ".json"), 'w') as f:
        json.dump(cmd, f)
    for _ in range(20):
        time.sleep(0.5)
        res_file = os.path.join(outbox, "res_" + filename + ".json")
        if os.path.exists(res_file):
            with open(res_file, 'r') as f:
                return json.load(f)
    return None

# 1. Fix Belt
print("Adjusting Belt fittings...")
for bs in ["pants", "shorts"]:
    send_and_receive({"action":"object/set/blendshape","capability":"write","keys":["path", "name", "value"],"values":["28428", bs, "100"]}, f"belt_{bs}_100")

# 2. Fix Harness
print("Adjusting Harness fittings...")
for bs in ["bodysuits", "pants", "skele hoodie", "boxers"]:
    send_and_receive({"action":"object/set/blendshape","capability":"write","keys":["path", "name", "value"],"values":["28826", bs, "100"]}, f"harness_{bs}_100")

# 3. Ensure Tail is active and check scale
print("Ensuring Centiped tail is visible...")
send_and_receive({"action":"object/set/active","capability":"write","keys":["path", "active"],"values":["29260", "true"]}, "tail_active_final")

