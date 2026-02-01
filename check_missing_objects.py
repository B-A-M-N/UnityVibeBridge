import os
import json
import time

project_path = "/home/bamn/ALCOM/Projects/BAMN-EXTO"
inbox = os.path.join(project_path, "vibe_queue/inbox")
outbox = os.path.join(project_path, "vibe_queue/outbox")

def send_and_receive(cmd, filename):
    with open(os.path.join(inbox, filename + ".json"), 'w') as f:
        json.dump(cmd, f)
    print(f"Sent {filename}")
    
    # Wait for response
    for _ in range(20):
        time.sleep(0.5)
        res_file = os.path.join(outbox, "res_" + filename + ".json")
        if os.path.exists(res_file):
            with open(res_file, 'r') as f:
                return json.load(f)
    return None

objects_to_check = ["Pentagram Body Harness", "Centiped tail", "Belt"]

for obj_name in objects_to_check:
    print(f"\nChecking object: {obj_name}")
    # Find the object to get its path/ID
    find_cmd = {"action":"system/find-by-name","capability":"read","keys":["name"],"values":[obj_name]}
    res = send_and_receive(find_cmd, f"find_{obj_name.replace(' ', '_')}")
    
    if res and res.get("results"):
        obj_id = res["results"][0]["id"]
        obj_path = res["results"][0]["message"]
        print(f"  Found ID: {obj_id}, Path: {obj_path}")
        
        # Check active state
        # (Assuming system/get-property or similar, but let's try to get more info)
        # Actually, let's list materials first as that confirms renderer exists
        list_cmd = {"action":"material/list","capability":"read","keys":["path"],"values":[obj_path]}
        mats_res = send_and_receive(list_cmd, f"list_mats_{obj_id}")
        if mats_res:
            print(f"  Materials: {mats_res.get('materials')}")
        else:
            print("  Failed to get materials.")
    else:
        print(f"  Object '{obj_name}' not found in scene.")

