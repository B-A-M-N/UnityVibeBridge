import os
import json
import time
import subprocess

project_path = "/home/bamn/ALCOM/Projects/BAMN-EXTO"
inbox = os.path.join(project_path, "vibe_queue/inbox")
outbox = os.path.join(project_path, "vibe_queue/outbox")

def send_and_receive(cmd, filename):
    cmd_id = filename
    cmd["id"] = cmd_id
    with open(os.path.join(inbox, filename + ".json"), 'w') as f:
        json.dump(cmd, f)
    
    print(f"Sent command: {filename}. Waiting for result...")
    # Baking takes time, give it 600 seconds (10 mins) just in case
    for _ in range(6000): 
        time.sleep(0.1)
        res_file = os.path.join(outbox, "res_" + filename + ".json")
        if os.path.exists(res_file):
            with open(res_file, 'r') as f:
                return json.load(f)
    return None

def run_git_snapshot(name):
    print(f"Creating Git Snapshot: {name}...")
    cwd = project_path
    
    # Check if .git_safety exists, init if not
    if not os.path.exists(os.path.join(cwd, ".git_safety")):
        subprocess.run(["git", "--git-dir=.git_safety", "--work-tree=.", "init"], cwd=cwd, check=True)

    subprocess.run(["git", "--git-dir=.git_safety", "--work-tree=.", "add", "."], cwd=cwd, check=True)
    try:
        subprocess.run(["git", "--git-dir=.git_safety", "--work-tree=.", "commit", "-m", name], cwd=cwd, check=True)
        print("Snapshot created successfully.")
    except subprocess.CalledProcessError:
        print("Nothing to commit or commit failed.")

# 1. Snapshot
run_git_snapshot("Pre-Bake Snapshot: BloodMoon Texture Update")

# 2. Assign Texture to Body (29118) Slot 2 (Body)
print("Assigning BloodMoon Texture to Body...")
cmd_tex = {
    "action": "material/set-texture",
    "capability": "write",
    "keys": ["path", "index", "field", "texture"],
    "values": ["29118", "2", "_MainTex", "Assets/✮exto/text/BloodMoonBySerpent/BloodMoonBase.png"]
}
res_tex = send_and_receive(cmd_tex, "assign_body_tex")
print("Texture Assignment Result:", json.dumps(res_tex, indent=2))

# 3. Bake Body (29118)
print("Triggering Bake (Poiyomi Lock) for Body...")
cmd_bake_body = {
    "action": "material/poiyomi/lock",
    "capability": "write",
    "keys": ["path"],
    "values": ["29118"]
}
res_bake_body = send_and_receive(cmd_bake_body, "bake_body_final")
print("Body Bake Result:", json.dumps(res_bake_body, indent=2))

# 4. Bake Body Suit (29084)
print("Triggering Bake (Poiyomi Lock) for Body Suit...")
cmd_bake_suit = {
    "action": "material/poiyomi/lock",
    "capability": "write",
    "keys": ["path"],
    "values": ["29084"]
}
res_bake_suit = send_and_receive(cmd_bake_suit, "bake_suit_final")
print("Body Suit Bake Result:", json.dumps(res_bake_suit, indent=2))
