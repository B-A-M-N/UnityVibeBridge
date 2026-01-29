#!/usr/bin/env python3
import requests

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {"X-Vibe-Token": "VIBE_777_SECURE", "X-Vibe-Capability": "Admin"}

def scan_recursive(root_id):
    resp = requests.get(f"{BASE_URL}/hierarchy?root={root_id}", headers=HEADERS)
    if resp.status_code != 200: return
    nodes = resp.json().get("nodes", [])
    for node in nodes:
        inst_id = node["instanceID"]
        # Check for renderer
        m_resp = requests.get(f"{BASE_URL}/material/list?path={inst_id}", headers=HEADERS)
        if m_resp.status_code == 200:
            data = m_resp.json()
            if "materials" in data:
                print(f"RENDERER FOUND: {node['name']} ({inst_id}) -> {[m['name'] for m in data['materials']]}")
        # Recurse
        scan_recursive(inst_id)

if __name__ == "__main__":
    print("[*] Scanning Tail hierarchy for renderers...")
    scan_recursive("60918")
