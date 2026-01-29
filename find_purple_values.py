#!/usr/bin/env python3
import requests
import json

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {
    "X-Vibe-Token": "VIBE_777_SECURE",
    "X-Vibe-Capability": "Admin"
}

def is_purple(color_str):
    try:
        # Format: "RGBA(1.000, 0.000, 1.000, 1.000)" or "1,0,1,1"
        clean = color_str.replace("RGBA(", "").replace(")", "")
        parts = [float(x.strip()) for x in clean.split(",")]
        r, g, b = parts[0], parts[1], parts[2]
        # Purple: High R, Low G, High B
        if r > 0.4 and g < 0.3 and b > 0.4:
            return True
    except:
        pass
    return False

def scan_for_purple():
    resp = requests.get(f"{BASE_URL}/hierarchy?root=-29498", headers=HEADERS)
    nodes = resp.json().get("nodes", [])
    
    print(f"[*] Scanning {len(nodes)} objects for purple values...")
    
    for node in nodes:
        inst_id = node["instanceID"]
        name = node["name"]
        
        # Check slot colors
        m_resp = requests.get(f"{BASE_URL}/material/list?path={inst_id}", headers=HEADERS)
        if m_resp.status_code == 200:
            mats = m_resp.json().get("materials", [])
            for m in mats:
                idx = m["index"]
                # Check _Color and _EmissionColor
                for field in ["_Color", "_EmissionColor"]:
                    c_resp = requests.get(f"{BASE_URL}/material/get-property?path={inst_id}&index={idx}&field={field}", headers=HEADERS)
                    # Note: get-property returns value if float, but what about color?
                    # The VibeBridge MaterialModule.cs says GetMaterialProperty returns mat.GetFloat(field).
                    # Wait, SetMaterialColor uses SetColor. 
                    # Let's check if we have a way to get color string.
                    pass

        # Since GetMaterialProperty only returns float, let's use a custom approach if possible.
        # However, looking at the code, there is no generic GetColor.
        # BUT, the user says "when I click on it... its saying its teal".
        # This means they are likely looking at the material in Unity.

    print("[*] Scan complete.")

if __name__ == "__main__":
    scan_for_purple()
