import requests
import json

BASE_URL = "http://localhost:8085"

def check_horn_status():
    inst_id = "32340" # ExtoPc ID
    param_name = "Horns"
    
    print(f"[*] Checking 'Horns' parameter on object {inst_id}...")
    try:
        url = f"{BASE_URL}/vrc/param/get?path={inst_id}&name={param_name}"
        print(f"    GET {url}")
        resp = requests.get(url, timeout=5)
        print(f"    Status: {resp.status_code}")
        print(f"    Response: {resp.text}")
        
        if resp.status_code == 200:
            val = resp.json().get("value")
            print(f"    Parsed Value: {val}")
        else:
            print("    Failed to get parameter.")
            
    except Exception as e:
        print(f"    Error: {e}")

if __name__ == "__main__":
    check_horn_status()
