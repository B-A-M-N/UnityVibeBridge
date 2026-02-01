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
    
    for _ in range(50):
        time.sleep(0.1)
        res_file = os.path.join(outbox, "res_" + filename + ".json")
        if os.path.exists(res_file):
            with open(res_file, 'r') as f:
                return json.load(f)
    return None

print("Inspecting Body...")
cmd = {
    "action": "inspect",
    "capability": "read",
    "keys": ["path"],
    "values": ["28834"]
}
res_body = send_and_receive(cmd, "inspect_body_mesh")
print("Body Inspection:", json.dumps(res_body, indent=2))

print("Inspecting Pants...")
cmd["values"] = ["28214"]
res_pants = send_and_receive(cmd, "inspect_pants_mesh")
print("Pants Inspection:", json.dumps(res_pants, indent=2))

# Check materials for Body
cmd = {
    "action": "material/list",
    "capability": "read",
    "keys": ["path"],
    "values": ["28834"]
}
res_mat_body = send_and_receive(cmd, "list_mats_body")
print("Body Materials:", json.dumps(res_mat_body, indent=2))

# Check materials for Pants
cmd["values"] = ["28214"]
res_mat_pants = send_and_receive(cmd, "list_mats_pants")
print("Pants Materials:", json.dumps(res_mat_pants, indent=2))
