import os
import json
import time
import subprocess

project_path = "/home/bamn/ALCOM/Projects/BAMN-EXTO"
inbox = os.path.join(project_path, "vibe_queue/inbox")
outbox = os.path.join(project_path, "vibe_queue/outbox")

def run_git_snapshot(name):
    print(f"Creating Git Snapshot: {name}...")
    env = os.environ.copy()
    # Ensure we are in the project root
    cwd = project_path
    
    # Check if .git_safety exists, init if not (just in case, though it should exist)
    if not os.path.exists(os.path.join(cwd, ".git_safety")):
        subprocess.run(["git", "--git-dir=.git_safety", "--work-tree=.", "init"], cwd=cwd, check=True)

    subprocess.run(["git", "--git-dir=.git_safety", "--work-tree=.", "add", "."], cwd=cwd, check=True)
    try:
        subprocess.run(["git", "--git-dir=.git_safety", "--work-tree=.", "commit", "-m", name], cwd=cwd, check=True)
        print("Snapshot created successfully.")
    except subprocess.CalledProcessError:
        print("Nothing to commit or commit failed.")

def send_and_receive(cmd, filename):
    cmd_id = filename
    cmd["id"] = cmd_id
    with open(os.path.join(inbox, filename + ".json"), 'w') as f:
        json.dump(cmd, f)
    
    print(f"Sent command: {filename}. Waiting for result...")
    # Baking takes time, give it 60 seconds
    for _ in range(600):
        time.sleep(0.1)
        res_file = os.path.join(outbox, "res_" + filename + ".json")
        if os.path.exists(res_file):
            with open(res_file, 'r') as f:
                return json.load(f)
    return None

# 1. Snapshot
run_git_snapshot("Pre-Bake Snapshot: Body and Pants")

# 2. Bake Body (28834)
print("Triggering Bake for Body (Skin)...")
cmd_body = {
    "action": "material/poiyomi/lock",
    "capability": "write",
    "keys": ["path"],
    "values": ["28834"]
}
res_body = send_and_receive(cmd_body, "bake_body_final")
print("Body Bake Result:", json.dumps(res_body, indent=2))

# 3. Bake Pants (28214)
print("Triggering Bake for Body Suit (Pants)...")
cmd_pants = {
    "action": "material/poiyomi/lock",
    "capability": "write",
    "keys": ["path"],
    "values": ["28214"]
}
res_pants = send_and_receive(cmd_pants, "bake_pants_final")
print("Pants Bake Result:", json.dumps(res_pants, indent=2))
