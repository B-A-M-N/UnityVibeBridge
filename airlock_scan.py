import os
import shutil
import json
import hashlib
from datetime import datetime
from security_gate import SecurityGate

# Configuration
STAGING_DIR = "airlock/staging"
QUARANTINE_DIR = "airlock/quarantine"
REPORTS_DIR = "airlock/reports"

def get_file_hash(path):
    """Generates SHA-256 hash for a file."""
    sha256_hash = hashlib.sha256()
    with open(path, "rb") as f:
        for byte_block in iter(lambda: f.read(4096), b""):
            sha256_hash.update(byte_block)
    return sha256_hash.hexdigest()

def scan_staging():
    """Scans all files in the staging directory and takes action based on security audit."""
    print(f"[*] Starting Airlock Scan at {datetime.now().isoformat()}")
    
    # Ensure directories exist (redundant but safe)
    for d in [STAGING_DIR, QUARANTINE_DIR, REPORTS_DIR]:
        os.makedirs(d, exist_ok=True)

    files_processed = 0
    violations_found = 0

    for root, dirs, files in os.walk(STAGING_DIR):
        for file in files:
            file_path = os.path.join(root, file)
            rel_path = os.path.relpath(file_path, STAGING_DIR)
            
            # Skip hidden files
            if file.startswith('.'):
                continue

            print(f"[*] Auditing: {rel_path}")
            files_processed += 1
            
            ext = os.path.splitext(file)[1].lower()
            issues = []
            
            try:
                with open(file_path, 'r', encoding='utf-8', errors='replace') as f:
                    content = f.read()
                
                if ext == '.py':
                    issues = SecurityGate.check_python(content)
                elif ext == '.cs':
                    issues = SecurityGate.check_csharp(content)
                else:
                    # Generic secret scan for other text-based files
                    issues = SecurityGate._check_secrets(content)
                
                # Generate Report
                timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
                file_hash = get_file_hash(file_path)
                report_name = f"scan_{file}_{timestamp}.json"
                report_path = os.path.join(REPORTS_DIR, report_name)
                
                status = "PASS" if not issues else "FAIL"
                
                report_data = {
                    "filename": file,
                    "rel_path": rel_path,
                    "hash": file_hash,
                    "timestamp": datetime.now().isoformat(),
                    "status": status,
                    "issues": issues
                }
                
                with open(report_path, 'w') as rf:
                    json.dump(report_data, rf, indent=2)
                
                if issues:
                    print(f"[!] SECURITY VIOLATION detected in {rel_path}")
                    violations_found += 1
                    # Quarantine the file
                    q_path = os.path.join(QUARANTINE_DIR, f"{timestamp}_{file}")
                    shutil.move(file_path, q_path)
                    print(f"[!] Moved to quarantine: {q_path}")
                    print(f"[!] Report: {report_path}")
                else:
                    print(f"[+] {rel_path} PASSED audit.")
                    # In a real airlock, we might move it to a 'trusted' or 'approved' folder.
                    # For now, we leave it or move it to a 'cleared' folder.
                    # Let's create 'airlock/cleared' if it passes.
                    cleared_dir = "airlock/cleared"
                    os.makedirs(cleared_dir, exist_ok=True)
                    shutil.move(file_path, os.path.join(cleared_dir, file))
                    print(f"[+] Moved to: {cleared_dir}/{file}")

            except Exception as e:
                print(f"[X] Error processing {rel_path}: {e}")

    print(f"\n[*] Scan Complete.")
    print(f"[*] Files Processed: {files_processed}")
    print(f"[*] Violations Found: {violations_found}")

if __name__ == "__main__":
    scan_staging()
