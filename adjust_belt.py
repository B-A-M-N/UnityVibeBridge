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

# Belt ID: 28428. Blendshapes: pants, shorts
# Let's try setting pants to 100 first to see if it moves it out
cmd = {"action":"object/set/blendshape","capability":"write","keys":["path", "name", "value"],"values":["28428", "pants", "100"]}
res = send_and_receive(cmd, "belt_pants_100")
if res:
    print(json.dumps(res, indent=2))
