import sys
import os
import json

# Add mcp-server to path so we can import engine and tools
sys.path.append(os.path.join(os.getcwd(), "mcp-server"))

from core.engine import VibeEngine

def main():
    if len(sys.argv) < 2:
        print("Usage: python3 vibe_cmd.py <action> [key=value ...]")
        return

    action = sys.argv[1]
    params = {}
    for arg in sys.argv[2:]:
        if "=" in arg:
            k, v = arg.split("=", 1)
            params[k] = v

    engine = VibeEngine()
    
    # We don't necessarily need to register all tools if we are just calling the airlock
    # but let's follow the engine's way.
    
    is_mutation = False
    if action in ["transaction/begin", "transaction/commit", "object/set-value", "system/reset-transforms"]:
        is_mutation = True
        
    res = engine.unity_request(action, params, is_mutation=is_mutation, intent="CLI_EXEC")
    print(json.dumps(res, indent=2))

if __name__ == "__main__":
    main()
