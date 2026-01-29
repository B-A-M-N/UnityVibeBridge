#!/usr/bin/env python3
import json

# Semantic map for the Blender MCP
# This tells the Blender logic exactly how to color the model
# regardless of what Unity's raw slot names are.

SEMANTIC_RESTORE_PLAN = {
    "AccentAll": {
        "hex": "#8000FF", # Purple
        "roughness": 0.2,
        "metallic": 0.8
    },
    "Hair": {
        "hex": "#FFFFFF", # White
        "roughness": 0.5,
        "metallic": 0.0
    },
    "Base": {
        "hex": "#000000", # Black
        "roughness": 0.4,
        "metallic": 0.1
    }
}

def generate_blender_mcp_config():
    print("[*] Generating semantic config for Blender MCP...")
    with open("metadata/blender_mcp_config.json", "w") as f:
        json.dump(SEMANTIC_RESTORE_PLAN, f, indent=2)
    print("[*] Config saved: metadata/blender_mcp_config.json")

if __name__ == "__main__":
    generate_blender_mcp_config()
