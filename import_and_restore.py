import bpy
import os

# Configuration
FBX_PATH = "/home/bamn/ALCOM/Projects/BAMN-EXTO/ExportedFbx/ExtoPC_Restored.fbx"
BLEND_OUTPUT = "/home/bamn/UnityVibeBridge/Exto_Restored.blend"

CUSTOM_COLORS = {
    "AccentAll": (0.5, 0.0, 1.0, 1.0), # Purple
    "Hair": (1.0, 1.0, 1.0, 1.0),      # White
    "Base": (0.0, 0.0, 0.0, 1.0)       # Black
}

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

def clean_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()

def import_fbx(path):
    if os.path.exists(path):
        print(f"[*] Importing FBX from {path}")
        bpy.ops.import_scene.fbx(filepath=path)
    else:
        print(f"[!] FBX not found at {path}")

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
            mat.diffuse_color = color
            
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
    clean_scene()
    import_fbx(FBX_PATH)
    restore_colors()
    # Save the file so the user can open it
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUTPUT)
    print(f"[*] Blender file saved to {BLEND_OUTPUT}")
