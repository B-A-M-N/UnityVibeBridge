import requests
import json
import time

BASE_URL = "http://127.0.0.1:8085"

def check():
    try:
        resp = requests.get(f"{BASE_URL}/material/list?path=32788", timeout=2)
        if resp.status_code == 200:
            data = resp.json()
            mats = data.get("materials", [])
            print(f"Materials on Body (32788):")
            for m in mats:
                print(f"  Slot {m['index']}: {m['name']}")
        else:
            print(f"Error: {resp.text}")
    except Exception as e:
        print(f"Failed: {e}")

if __name__ == "__main__":
    check()
