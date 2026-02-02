import os
import json
import uuid
import time

def wake():
    inbox = "vibe_queue/inbox"
    outbox = "vibe_queue/outbox"
    os.makedirs(inbox, exist_ok=True)
    os.makedirs(outbox, exist_ok=True)

    cmd_id = str(uuid.uuid4())
    payload = {
        "action": "status",
        "id": cmd_id,
        "capability": "Read",
        "keys": [],
        "values": []
    }

    print(f"Sending wake-up command {cmd_id}...")
    with open(os.path.join(inbox, f"{cmd_id}.json"), "w") as f:
        json.dump(payload, f)

    start = time.time()
    while time.time() - start < 10:
        res_file = os.path.join(outbox, f"res_{cmd_id}.json")
        if os.path.exists(res_file):
            with open(res_file, "r") as f:
                print("Received response:")
                print(f.read())
            return
        time.sleep(0.5)
    print("Wake-up timed out.")

if __name__ == "__main__":
    wake()
