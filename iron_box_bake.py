import os
import json
import time
import subprocess

project_path = "/home/bamn/ALCOM/Projects/BAMN-EXTO"
inbox = os.path.join(project_path, "vibe_queue/inbox")
outbox = os.path.join(project_path, "vibe_queue/outbox")

def run_git_safety_snapshot(name):
    print(f"Creating Iron Box Snapshot: {name}...")
    # git --git-dir=.git_safety --work-tree=. add .
    # git --git-dir=.git_safety --work-tree=. commit -m "[Snapshot Name]"
    try:
        subprocess.run(["git", "--git-dir=.git_safety", "--work-tree=.", "add", "."], cwd=project_path, check=True)
        subprocess.run(["git", "--git-dir=.git_safety", "--work-tree=.", "commit", "-m", name], cwd=project_path, check=True)
        print("Snapshot successful.")
    except subprocess.CalledProcessError as e:
        print(f"Snapshot failed or nothing to commit: {e}")

def send_and_receive(cmd, filename):
    cmd_id = filename
    cmd["id"] = cmd_id
    with open(os.path.join(inbox, filename + ".json"), 'w') as f:
        json.dump(cmd, f)
    
    print(f"Sent {cmd['action']} for {filename}, waiting for result...")
    # Poiyomi Lock can take 30-60 seconds
    for _ in range(600):
        time.sleep(0.1)
        res_file = os.path.join(outbox, "res_" + filename + ".json")
        if os.path.exists(res_file):
            with open(res_file, 'r') as f:
                return json.load(f)
    return None

# 1. Mandatory Snapshot
run_git_safety_snapshot("Pre-Bake: Skin and Body Suit")

# 2. Bake Body (Skin)
print("Triggering Bake for Body (Skin)...")
# We use the name 'body' which we verified works
cmd_body = {
    "action": "material/poiyomi/lock",
    "capability": "write",
    "keys": ["path"],
    "values": ["body"]
}
res_body = send_and_receive(cmd_body, "bake_body")
print("Body Bake Result:", json.dumps(res_body, indent=2))

# 3. Bake Body Suit (Cyberpants_by_Grey)
print("Triggering Bake for Body Suit (Pants)...")
cmd_pants = {
    "action": "material/poiyomi/lock",
    "capability": "write",
    "keys": ["path"],
    "values": ["Cyberpants_by_Grey"]
}
res_pants = send_and_receive(cmd_pants, "bake_pants")
print("Pants Bake Result:", json.dumps(res_pants, indent=2))
