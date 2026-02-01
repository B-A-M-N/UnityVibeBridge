import os
import json
import time

def find_eyepatch(project_path):
    print("Finding eyepatch in scene...")
    inbox = os.path.join(project_path, "vibe_queue/inbox")
    outbox = os.path.join(project_path, "vibe_queue/outbox")
    
    # 1. Get all objects with renderers
    cmd = {"action":"system/find/by/component","capability":"read","keys":["type"],"values":["Renderer"]}
    with open(os.path.join(inbox, "get_all_renderers.json"), 'w') as f:
        json.dump(cmd, f)
    
    time.sleep(2)
    with open(os.path.join(outbox, "res_get_all_renderers.json"), 'r') as f:
        res = json.load(f)
    
    # 2. List materials for each and look for 'eyepatch'
    for obj in res["results"]:
        name = obj["message"]
        id = obj["id"]
        list_cmd = {"action":"material/list","capability":"read","keys":["path"],"values":[str(id)]}
        with open(os.path.join(inbox, f"list_{id}.json"), 'w') as f:
            json.dump(list_cmd, f)
        
        time.sleep(0.1)
        try:
            with open(os.path.join(outbox, f"res_list_{id}.json"), 'r') as f:
                mats = json.load(f)
                for m in mats["materials"]:
                    if "patch" in m["name"].lower():
                        print(f"FOUND: Object '{name}' (ID: {id}) has material '{m['name']}' at index {m['index']}")
        except:
            pass

if __name__ == "__main__":
    find_eyepatch("/home/bamn/ALCOM/Projects/BAMN-EXTO")
