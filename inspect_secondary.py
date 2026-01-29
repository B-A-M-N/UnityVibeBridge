import json
import uuid
import os
import time

INBOX_PATH = "/home/bamn/ALCOM/Projects/BAMN-EXTO/vibe_queue/inbox"
OUTBOX_PATH = "/home/bamn/ALCOM/Projects/BAMN-EXTO/vibe_queue/outbox"

def airlock(action, params):
    cmd_id = str(uuid.uuid4())
    payload = {
        "action": action, 
        "id": cmd_id, 
        "capability": "Admin", 
        "keys": list(params.keys()), 
        "values": [str(v) for v in params.values()]
    }
    with open(os.path.join(INBOX_PATH, f"{cmd_id}.json"), "w") as f:
        json.dump(payload, f)
    
    start = time.time()
    while time.time() - start < 10:
        res_path = os.path.join(OUTBOX_PATH, f"res_{cmd_id}.json")
        if os.path.exists(res_path):
            with open(res_path, "r") as f:
                data = f.read()
            print(f"--- {action} ({params.get('path')}) ---")
            print(data)
            os.remove(res_path)
            return
        time.sleep(0.1)

if __name__ == "__main__":
    airlock("/material/list", {"path": "33072"}) # Hair
    airlock("/material/list", {"path": "32752"}) # Bodysuits
    airlock("/material/list", {"path": "32336"}) # Fishnets
