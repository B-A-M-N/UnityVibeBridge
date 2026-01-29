import requests
import json

BASE_URL = "http://localhost:8085"

def full_scan():
    # Use AuditMaterials on 'root' (null path) which usually scans everything
    try:
        resp = requests.get(f"{BASE_URL}/audit/materials").json()
        materials = resp.get("materials", [])
        for i, m in enumerate(materials):
            if "InternalError" in m.get("shader", "") or "MISSING" in m.get("shader", ""):
                print(f"Broken Material Found: {m.get('name')} (Shader: {m.get('shader')})")
    except Exception as e:
        print(f"Scan failed: {e}")

print("Performing full scene material audit...")
full_scan()
