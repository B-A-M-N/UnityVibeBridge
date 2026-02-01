import os
import json
import time

def scan_materials(project_path):
    print("Scanning for 'eye' materials in scene...")
    inbox = os.path.join(project_path, "vibe_queue/inbox")
    outbox = os.path.join(project_path, "vibe_queue/outbox")
    
    # 1. Get all objects with renderers
    cmd = {"action":"system/find-by-component","capability":"read","keys":["type"],"values":["Renderer"]}
    with open(os.path.join(inbox, "scan_all.json"), 'w') as f:
        json.dump(cmd, f)
    
    time.sleep(2)
    with open(os.path.join(outbox, "res_scan_all.json"), 'r') as f:
        res = json.load(f)
    
    # 2. List materials for each
    for obj in res["results"]:
        name = obj["message"]
        list_cmd = {"action":"material/list","capability":"read","keys":["path"],"values":[name]}
        with open(os.path.join(inbox, f"list_{name}.json"), 'w') as f:
            json.dump(list_cmd, f)
        
        time.sleep(0.5)
        try:
            with open(os.path.join(outbox, f"res_list_{name}.json"), 'r') as f:
                mats = json.load(f)
                print(f"Object: {name}")
                for m in mats["materials"]:
                    print(f"  [{m['index']}] {m['name']}")
        except:
            pass

if __name__ == "__main__":
    scan_materials("/home/bamn/ALCOM/Projects/BAMN-EXTO")
