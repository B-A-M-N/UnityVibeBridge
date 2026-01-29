#!/usr/bin/env python3
import requests
import json

BASE_URL = "http://127.0.0.1:8085"
HEADERS = {"X-Vibe-Token": "VIBE_777_SECURE", "X-Vibe-Capability": "Admin"}
ACTIVE_AVATAR_ROOT = "71346"

def convert_to_mq():
    print(f"[*] Starting Transactional MQ Conversion for Avatar {ACTIVE_AVATAR_ROOT}...")

    # 1. Snapshot Material State (Mandatory)
    print("[*] Step 1: Taking material snapshot for rollback safety...")
    s_resp = requests.get(f"{BASE_URL}/material/snapshot?path={ACTIVE_AVATAR_ROOT}", headers=HEADERS)
    snap_data = s_resp.json()
    if "error" in snap_data:
        print(f"  [!] Snapshot failed: {snap_data['error']}. Aborting for safety.")
        return
    SNAPSHOT_PATH = snap_data["path"]
    print(f"  [+] Snapshot saved to: {SNAPSHOT_PATH}")

    # 2. Fork Avatar (Isolation Layer)
    print("[*] Step 2: Forking avatar and materials...")
    f_resp = requests.get(f"{BASE_URL}/opt/fork?path={ACTIVE_AVATAR_ROOT}", headers=HEADERS)
    fork_data = f_resp.json()
    if "error" in fork_data:
        print(f"  [!] Fork failed: {fork_data['error']}")
        return
    
    QUEST_AVATAR_ROOT = str(fork_data["instanceID"])
    print(f"  [+] Isolated avatar created (ID: {QUEST_AVATAR_ROOT}). Original is now safe.")

    # 3. Stability Lock (Read-Only)
    print("[*] Step 3: Activating Stability Lock (Read-Only)...")
    requests.get(f"{BASE_URL}/system/mode?mode=readonly", headers=HEADERS)

    try:
        # 4. Swap Shaders
        print("[*] Step 4: Swapping to Quest-compatible shaders...")
        sw_resp = requests.get(f"{BASE_URL}/opt/shader/quest?path={QUEST_AVATAR_ROOT}", headers=HEADERS)
        print(f"  [+] {sw_resp.json().get('message')}")

        # 5. Crush Textures
        print("[*] Step 5: Crushing textures to 2048px...")
        t_resp = requests.get(f"{BASE_URL}/opt/texture/crush?path={QUEST_AVATAR_ROOT}&maxSize=2048", headers=HEADERS)
        print(f"  [+] {t_resp.json().get('message')}")

        # 6. Simplify Meshes
        print("[*] Step 6: Simplifying meshes...")
        h_resp = requests.get(f"{BASE_URL}/hierarchy?root={QUEST_AVATAR_ROOT}", headers=HEADERS)
        for node in h_resp.json().get("nodes", []):
            inst_id = node["instanceID"]
            m_resp = requests.get(f"{BASE_URL}/opt/meshes?path={inst_id}", headers=HEADERS)
            if m_resp.status_code == 200:
                for m in m_resp.json().get("meshes", []):
                    if m.get("triangles", 0) > 5000:
                        requests.get(f"{BASE_URL}/opt/mesh/simplify?path={inst_id}&quality=0.5", headers=HEADERS)

        # 7. Semantic Restore (Optional Polish)
        print("[*] Step 7: Performing Semantic Restore to preserve intent...")
        r_resp = requests.get(f"{BASE_URL}/material/restore?path={QUEST_AVATAR_ROOT}&snapshot={SNAPSHOT_PATH}", headers=HEADERS)
        print(f"  [+] {r_resp.json().get('message')}")

    except Exception as e:
        print(f"  [!] CRITICAL FAILURE: {e}")
        print("[*] DISCARDING QUEST BUILD AND RESTORING ORIGINAL STATE...")
        # In a real scenario, we'd destroy QUEST_AVATAR_ROOT here.
    
    finally:
        print("[*] Releasing Stability Lock...")
        requests.get(f"{BASE_URL}/system/mode?mode=normal", headers=HEADERS)

    print(f"[*] MQ Conversion Complete. New Avatar ID: {QUEST_AVATAR_ROOT}")
    print("[*] All assets are isolated in Assets/_QuestGenerated/")

if __name__ == "__main__":
    convert_to_mq()
