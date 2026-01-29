
import sys
import os

# Add project root to path to import SecurityGate
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), '.')))
from security_gate import SecurityGate

code = """
MY_OAUTH_TOKEN = "long-string-that-is-secret"
"""

print("[*] Testing AST Secret Detection...")
errors = SecurityGate.check_python(code)
if errors:
    print(f"[*] Blocked: {errors[0]}")
    sys.exit(0)
else:
    print("[!] FAILURE: AST failed to detect secret assignment.")
    sys.exit(1)
