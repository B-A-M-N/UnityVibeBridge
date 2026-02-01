import os
import json
import time

def find_singular_eye(project_path):
    print("Searching for object with material 'eye'...")
    inbox = os.path.join(project_path, "vibe_queue/inbox")
    outbox = os.path.join(project_path, "vibe_queue/outbox")
    
    cmd = {"action":"system/find-by-component","capability":"read","keys":["type"],"values":["Renderer"]}
    with open(os.path.join(inbox, "find_all.json"), 'w') as f:
        json.dump(cmd, f)
    
    time.sleep(2)
    with open(os.path.join(outbox, "res_find_all.json"), 'r') as f:
        res = json.load(f)
    
    for obj in res["results"]:
        id = obj["id"]
        list_cmd = {"action":"material/list","capability":"read","keys":["path"],"values":[str(id)]}
        with open(os.path.join(inbox, f"l_{id}.json"), 'w') as f:
            json.dump(list_cmd, f)
        
        time.sleep(0.3)
        try:
            with open(os.path.join(outbox, f"res_l_{id}.json"), 'r') as f:
                mats = json.load(f)
                for m in mats["materials"]:
                    if m["name"] == "eye":
                        print(f"BINGO! Object '{obj['message']}' (ID: {id}) has material 'eye' at index {m['index']}")
        except:
            pass

if __name__ == "__main__":
    find_singular_eye("/home/bamn/ALCOM/Projects/BAMN-EXTO")
