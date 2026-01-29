import requests
import json

BASE_URL = "http://localhost:8085"

def get_nodes(root_id):
    try:
        resp = requests.get(f"{BASE_URL}/hierarchy?root={root_id}", timeout=5)
        return resp.json().get("nodes", [])
    except: return []

def find_renderers(root_id):
    nodes = get_nodes(root_id)
    for node in nodes:
        iid = node["instanceID"]
        name = node["name"]
        
        # Inspect for renderers
        try:
            insp = requests.get(f"{BASE_URL}/inspect?path={iid}").json()
            comps = [c["type"] for c in insp.get("components", [])]
            if "MeshRenderer" in comps or "SkinnedMeshRenderer" in comps:
                print(f"RENDERER FOUND: {name} (ID: {iid}, Path: {node['path']})")
                mats = requests.get(f"{BASE_URL}/material/list?path={iid}").json()
                print(f"  Materials: {json.dumps(mats.get('materials', []))}")
        except: pass
        
        if node["childCount"] > 0:
            find_renderers(iid)

print("Searching Armature for renderers...")
find_renderers(28400)
