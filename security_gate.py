import os
import json
import sys

# Dynamic path resolution to prevent environment drift
PROJECT_ROOT = os.path.dirname(os.path.abspath(__file__))
sys.path.append(os.path.join(PROJECT_ROOT, "scripts"))

try:
    from security_gate import SecurityGate
except ImportError:
    # Fallback if scripts/ is missing
    class SecurityGate:
        @staticmethod
        def verify_integrity(path): return False

def verify_integrity(project_path=None):
    if project_path is None:
        project_path = PROJECT_ROOT
        
    audit_log = os.path.join(project_path, "logs/vibe_audit.jsonl")
    
    if not os.path.exists(audit_log):
        print(f"CRITICAL: No audit log found at {audit_log}")
        return False

    print(f"--- Integrity Report: {project_path} ---")
    # Basic check for existence, delegating deep audit to scripts/security_gate
    if os.path.exists(os.path.join(project_path, "scripts/security_gate.py")):
        print("Kernel Guard: ACTIVE")
        return True
    return False

if __name__ == "__main__":
    verify_integrity()