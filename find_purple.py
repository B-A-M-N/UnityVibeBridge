#!/usr/bin/env python3
import requests
import json

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {
    "X-Vibe-Token": "VIBE_777_SECURE",
    "X-Vibe-Capability": "Admin"
}

def find_purple():
    # 1. Get all objects in hierarchy
    resp = requests.get(f"{BASE_URL}/hierarchy?root=-29498", headers=HEADERS)
    nodes = resp.json().get("nodes", [])
    
    print(f"[*] Checking {len(nodes)} sub-objects for error shaders...")
    
    for node in nodes:
        inst_id = node["instanceID"]
        name = node["name"]
        
        # 2. List materials for each
        m_resp = requests.get(f"{BASE_URL}/material/list?path={inst_id}", headers=HEADERS)
        if m_resp.status_code == 200:
            mats = m_resp.json().get("materials", [])
            for m in mats:
                # 3. Get property or audit to check shader?
                # Actually, audit/materials gives us the shader info for the whole object
                pass
        
        a_resp = requests.get(f"{BASE_URL}/audit/materials?path={inst_id}", headers=HEADERS)
        if a_resp.status_code == 200:
            audit = a_resp.json()
            for m in audit.get("materials", []):
                shader = m.get("shader", "")
                if "InternalErrorShader" in shader or "MISSING" in shader:
                    print(f"[!] FOUND PURPLE/ERROR: Object '{name}' ({inst_id}), Material: {m['name']}, Shader: {shader}")

if __name__ == "__main__":
    find_purple()
