import os
import json
import requests

def get_fingerprint():
    project_path = os.path.dirname(os.path.abspath(__file__))
    status_file = os.path.join(project_path, "metadata", "vibe_status.json")
    settings_file = os.path.join(project_path, "metadata", "vibe_settings.json")
    
    nonce = None
    if os.path.exists(status_file):
        with open(status_file, "r") as f:
            nonce = json.load(f).get("nonce")
            
    port = 8087
    if os.path.exists(settings_file):
        with open(settings_file, "r") as f:
            port = json.load(f).get("ports", {}).get("control", 8087)

    headers = {"X-Vibe-Token": nonce, "X-Vibe-Capability": "Read"}
    
    # Inspect body (ID: 28866) to get components and blendshapes
    try:
        resp = requests.get(f"http://127.0.0.1:{port}/inspect", params={"path": "28866"}, headers=headers)
        if resp.status_code == 200:
            print(f"Body Inspection:\n{json.dumps(resp.json(), indent=2)}")
        else:
            print(f"Error {resp.status_code}: {resp.text}")
    except Exception as e:
        print(f"Failed: {e}")

if __name__ == "__main__":
    get_fingerprint()
