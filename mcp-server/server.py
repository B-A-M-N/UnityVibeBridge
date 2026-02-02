# UnityVibeBridge Modular Wrapper (v1.5.0)
import sys
import os

def check_dependencies():
    """Verifies all required packages for VibeBridge Hardening are present."""
    required = ["mcp", "requests", "psutil"]
    missing = []
    for pkg in required:
        try:
            __import__(pkg)
        except ImportError:
            missing.append(pkg)
    
    if missing:
        print(f"❌ [VibeBridge] CRITICAL: Missing Python dependencies: {', '.join(missing)}")
        print(f"👉 Run: pip install -r {os.path.join(os.path.dirname(__file__), 'requirements.txt')}")
        sys.exit(1)

if __name__ == "__main__":
    check_dependencies()
    sys.path.append(os.path.dirname(os.path.abspath(__file__)))
    
    # Run Zombie Port Detection
    try:
        from scripts.zombie_port_scanner import scan_vibe_ports
        scan_vibe_ports()
    except Exception as e:
        print(f"⚠️ [Vibe] Diagnostic scan failed: {e}")

    from __init__ import create_kernel
    kernel = create_kernel()
    kernel.run()
