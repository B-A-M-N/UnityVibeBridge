import bpy

# BLENDER RESTORE SCRIPT
# Run this inside Blender (Scripting tab) after importing your FBX

CUSTOM_COLORS = {
    "AccentAll": (0.5, 0.0, 1.0, 1.0), # Purple
    "Hair": (1.0, 1.0, 1.0, 1.0),      # White
    "Base": (0.0, 0.0, 0.0, 1.0)       # Black
}

# Material mapping keywords
MAPPINGS = {
    "metal": "AccentAll",
    "pawpad": "AccentAll",
    "collar": "AccentAll",
    "chains": "AccentAll",
    "harness": "AccentAll",
    "skele": "AccentAll",
    "hair": "Hair",
    "black": "Base",
    "body": "Base",
    "tail": "Base"
}

def restore_colors():
    print("--- Restoring Customizations ---")
    for mat in bpy.data.materials:
        mat_name = mat.name.lower()
        target_group = None
        
        for key, group in MAPPINGS.items():
            if key in mat_name:
                target_group = group
                break
        
        if target_group:
            color = CUSTOM_COLORS[target_group]
            print(f"Applying {target_group} color to {mat.name}")
            
            # 1. Update Viewport Color
            mat.diffuse_color = color
            
            # 2. Update Shader Nodes (if using Nodes)
            if mat.use_nodes:
                bsdf = mat.node_tree.nodes.get("Principled BSDF")
                if bsdf:
                    bsdf.inputs['Base Color'].default_value = color
                    if target_group == "AccentAll":
                        bsdf.inputs['Metallic'].default_value = 0.8
                        bsdf.inputs['Roughness'].default_value = 0.2
                    else:
                        bsdf.inputs['Metallic'].default_value = 0.0
                        bsdf.inputs['Roughness'].default_value = 0.5

if __name__ == "__main__":
    restore_colors()
