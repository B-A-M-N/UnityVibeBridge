import os
import hashlib
import json
from datetime import datetime
from security_gate import SecurityGate

def get_content_hash(content):
    return hashlib.sha256(content.strip().encode('utf-8')).hexdigest()

def initialize_trust():
    print("[*] Initializing Trusted Signatures baseline...")
    trusted = {}
    
    # Files to trust initially (core logic)
    core_files = [
        "security_gate.py",
        "airlock_scan.py",
        "bundle.py",
        "robust_bundle.py",
        "modularize_bridge.py",
        "smart_remap_airlock.py",
        "fixed_server.cs",
        "full_server.cs"
    ]
    
    # Also scan all .py files in the root
    for f in os.listdir("."):
        if f.endswith(".py") or f.endswith(".cs"):
            if f not in core_files:
                core_files.append(f)

    for file_path in core_files:
        if os.path.exists(file_path):
            try:
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                content_hash = get_content_hash(content)
                trusted[content_hash] = {
                    "file": file_path,
                    "timestamp": datetime.now().isoformat(),
                    "reason": "Baseline Initialization"
                }
                print(f"[+] Trusted: {file_path}")
            except Exception as e:
                print(f"[!] Error trusting {file_path}: {e}")

    with open("trusted_signatures.json", "w") as f:
        json.dump(trusted, f, indent=2)
    
    print(f"\n[*] Baseline complete. {len(trusted)} files trusted.")

if __name__ == "__main__":
    initialize_trust()
