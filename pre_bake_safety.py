import os
import json
import time

project_path = "/home/bamn/ALCOM/Projects/BAMN-EXTO"
inbox = os.path.join(project_path, "vibe_queue/inbox")
outbox = os.path.join(project_path, "vibe_queue/outbox")

def send_and_receive(cmd, filename):
    with open(os.path.join(inbox, filename + ".json"), 'w') as f:
        json.dump(cmd, f)
    for _ in range(30):
        time.sleep(0.5)
        res_file = os.path.join(outbox, "res_" + filename + ".json")
        if os.path.exists(res_file):
            with open(res_file, 'r') as f:
                return json.load(f)
    return None

# 1. Git Checkpoint
print("Creating Git checkpoint...")
checkpoint_cmd = {
    "action": "system/git/checkpoint",
    "capability": "write",
    "keys": ["message"],
    "values": ["Pre-Bake Safety Checkpoint"]
}
res_git = send_and_receive(checkpoint_cmd, "git_checkpoint")
print("Git Result:", res_git)

# 2. Material Snapshot (ExtoPc ID: 28350)
print("\nTaking material snapshot...")
snapshot_cmd = {
    "action": "material/snapshot",
    "capability": "write",
    "keys": ["path"],
    "values": ["28350"]
}
res_snap = send_and_receive(snapshot_cmd, "mat_snapshot")
print("Snapshot Result:", res_snap)

