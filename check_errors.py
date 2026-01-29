import requests
import json

resp = requests.get("http://localhost:8085/system/logs")
if resp.status_code == 200:
    logs = resp.json().get("logs", [])
    for log in logs:
        if "error" in log["message"].lower() or "error" in log["type"].lower() or "warning" in log["type"].lower():
            print(f"[{log['type']}] {log['message']}")
else:
    print(f"Failed: {resp.status_code}")
