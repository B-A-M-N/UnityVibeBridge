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

print("Listing materials for body (28866)...")
res_body = send_and_receive({"action":"material/list","capability":"read","keys":["path"],"values":["28866"]}, "list_body_mats")
print("Body Materials:", res_body)

print("\nListing materials for bodysuits (28830)...")
res_suit = send_and_receive({"action":"material/list","capability":"read","keys":["path"],"values":["28830"]}, "list_suit_mats")
print("Bodysuit Materials:", res_suit)
