import requests

BASE_URL = "http://localhost:8085"

# Current IDs and Slots for Red Accents
TARGETS = [
    {"path": "28566", "index": 2}, # Head: Face details
    {"path": "28566", "index": 6}, # Head: Black 6 (Accents)
    {"path": "27190", "index": 0}, # Chains: Metal
    {"path": "27190", "index": 1}, # Chains: Pawpad
    {"path": "27442", "index": 2}, # Warmers: Metal
    {"path": "27162", "index": 1}, # Pants: Metal
    {"path": "27384", "index": 1}, # Belt: Pawpad
    {"path": "28202", "index": 3}, # Boots: Metal
    {"path": "27804", "index": 0}, # Skele Hoodie
    {"path": "27792", "index": 0}, # Harness: Metal
]

def apply_red():
    color = "1,0,0,1" # Red
    emission = "1,0,0,1" # Bright Red Emission
    
    for t in TARGETS:
        # Set main color
        url = f"{BASE_URL}/material/set-slot-color?path={t['path']}&index={t['index']}&field=_Color&value={color}"
        requests.get(url)
        # Set emission color
        url_em = f"{BASE_URL}/material/set-slot-color?path={t['path']}&index={t['index']}&field=_EmissionColor&value={emission}"
        requests.get(url_em)

    # Ensure Horns/Nails are Black
    requests.get(f"{BASE_URL}/material/set-slot-color?path=27832&index=1&field=_Color&value=0,0,0,1")
    requests.get(f"{BASE_URL}/material/set-slot-color?path=28566&index=3&field=_Color&value=0,0,0,1")

if __name__ == "__main__":
    apply_red()
    print("Accents set to Red. Horns/Nails set to Black.")
