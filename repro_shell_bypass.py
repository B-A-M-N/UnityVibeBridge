
import sys
import os

# Add project root to path to import SecurityGate
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), '.')))
from security_gate import SecurityGate

cmd = "ls && whoami"

print(f"[*] Testing Shell Injection: {cmd}")
errors = SecurityGate.check_shell(cmd)
if not errors:
    print("[!] BYPASS DETECTED: SecurityGate failed to block command chaining with '&&'.")
    sys.exit(0)
else:
    print(f"[*] Blocked: {errors[0]}")
    sys.exit(1)
