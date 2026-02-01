# UnityVibeBridge Modular Kernel (v1.5.0)
# Copyright (C) 2026 B-A-M-N

import sys
import os

# Ensure local modules are importable
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

from core.engine import VibeEngine
from tools.management import register_management_tools
from tools.unity_mvc import register_mvc_tools
from tools.payloads import register_payload_tools
from tools.telemetry import register_telemetry_tools

def create_kernel():
    engine = VibeEngine()
    
    # Register tool groups
    register_management_tools(engine)
    register_mvc_tools(engine)
    register_payload_tools(engine)
    register_telemetry_tools(engine)
    
    return engine

if __name__ == "__main__":
    kernel = create_kernel()
    kernel.run()
