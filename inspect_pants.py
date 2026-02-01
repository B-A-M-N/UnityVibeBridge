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

print("Inspecting Cyberpants (28216)...")
res = send_and_receive({"action":"inspect","capability":"read","keys":["path"],"values":["28216"]}, "inspect_pants_28216")
print(json.dumps(res, indent=2))

print("\nInspecting other potential pants (29150)...")
res2 = send_and_receive({"action":"inspect","capability":"read","keys":["path"],"values":["29150"]}, "inspect_pants_29150")
print(json.dumps(res2, indent=2))
