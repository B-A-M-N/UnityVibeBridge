import os
import json
import time

def list_blendshapes(project_path, obj_name):
    print(f"Listing BlendShapes for {obj_name}...")
    inbox = os.path.join(project_path, "vibe_queue/inbox")
    outbox = os.path.join(project_path, "vibe_queue/outbox")
    
    # We use a custom 'object/get-value' if available, or just inspect.
    # Since we don't have a direct tool, I'll send a search for 'paws' properties.
    cmd = {"action":"inspect","capability":"read","keys":["path"],"values":[obj_name]}
    with open(os.path.join(inbox, "inspect_body.json"), 'w') as f:
        json.dump(cmd, f)
    
    time.sleep(2)
    with open(os.path.join(outbox, "res_inspect_body.json"), 'r') as f:
        res = json.load(f)
        print(f"Object: {res['name']}")
        print(f"Components: {', '.join(res['components'])}")

if __name__ == "__main__":
    list_blendshapes("/home/bamn/ALCOM/Projects/BAMN-EXTO", "body")
