import bpy
import struct
import mathutils

def read_string(f):
    # Read 7-bit encoded integer
    count = 0
    shift = 0
    while True:
        b = struct.unpack('B', f.read(1))[0]
        count |= (b & 0x7F) << shift
        if (b & 0x80) == 0:
            break
        shift += 7
    return f.read(count).decode('utf-8')

def import_raw_vibe(filepath):
    print(f"Importing from {filepath}")
    with open(filepath, 'rb') as f:
        # 1. Renderer Count
        renderer_count = struct.unpack('i', f.read(4))[0]
        meshes = []
        
        for i in range(renderer_count):
            name = read_string(f)
            if name == "null_mesh":
                f.read(4) # skip 0 vertex count
                continue
                
            vert_count = struct.unpack('i', f.read(4))[0]
            verts = []
            for _ in range(vert_count):
                verts.append(struct.unpack('fff', f.read(12)))
            
            index_count = struct.unpack('i', f.read(4))[0]
            indices = []
            for _ in range(index_count):
                indices.append(struct.unpack('i', f.read(4))[0])
            
            # Group indices into triangles
            faces = [indices[j:j+3] for j in range(0, len(indices), 3)]
            
            bone_count = struct.unpack('i', f.read(4))[0]
            bones_list = []
            for _ in range(bone_count):
                bones_list.append(read_string(f))
                
            weight_count = struct.unpack('i', f.read(4))[0]
            weights = []
            for _ in range(weight_count):
                # i0, w0, i1, w1, i2, w2, i3, w3
                w_data = struct.unpack('ifififif', f.read(32))
                weights.append(w_data)
                
            meshes.append({
                'name': name,
                'verts': verts,
                'faces': faces,
                'bones': bones_list,
                'weights': weights
            })
            
        # 2. Hierarchy Count
        hierarchy_count = struct.unpack('i', f.read(4))[0]
        nodes = []
        for _ in range(hierarchy_count):
            node_name = read_string(f)
            parent_name = read_string(f)
            pos = struct.unpack('fff', f.read(12))
            rot = struct.unpack('ffff', f.read(16))
            scale = struct.unpack('fff', f.read(12))
            nodes.append({
                'name': node_name,
                'parent': parent_name,
                'pos': pos,
                'rot': rot,
                'scale': scale
            })

    # Reconstruction
    # Create Armature
    bpy.ops.object.armature_add(enter_editmode=True)
    arm_obj = bpy.context.object
    arm_obj.name = "VibeArmature"
    amt = arm_obj.data
    amt.name = "VibeArmatureData"
    
    # Remove default bone
    amt.edit_bones.remove(amt.edit_bones[0])
    
    # Build Bone Hierarchy
    # We need to compute world matrices from local
    bone_map = {}
    for node in nodes:
        eb = amt.edit_bones.new(node['name'])
        eb.head = (0, 0, 0)
        eb.tail = (0, 0.1, 0) # Placeholder length
        bone_map[node['name']] = eb

    for node in nodes:
        if node['parent'] != "null" and node['parent'] in bone_map:
            bone_map[node['name']].parent = bone_map[node['parent']]

    # Position Bones
    # Since we have local pos/rot/scale, we can apply them.
    # But edit_bones use head/tail/roll.
    # It's easier to set pose bones but we need edit mode for structure.
    # Let's use world matrices if we had them. Since we only have local, we'll walk the tree.
    
    def conv_pos(p):
        return (p[0], -p[2], p[1])
    
    def conv_quat(q):
        # Unity (x, y, z, w) -> Blender (w, x, -z, y)
        return mathutils.Quaternion((q[3], q[0], -q[2], q[1]))

    def apply_transform(node_name, parent_matrix):
        node = next(n for n in nodes if n['name'] == node_name)
        # Create local matrix in Unity space
        local_pos = conv_pos(node['pos'])
        local_rot = conv_quat(node['rot'])
        
        # This is a bit tricky because we are converting individual components
        # instead of the whole matrix. 
        # For a raw dump, it's often safer to just reconstruct in Unity space 
        # and rotate the root object once.
        # Let's do that instead to avoid complex transform math errors.
        
        unity_mat = mathutils.Matrix.Translation(node['pos']) @ \
                    mathutils.Quaternion((node['rot'][3], node['rot'][0], node['rot'][1], node['rot'][2])).to_matrix().to_4x4()
        
        world_matrix = parent_matrix @ unity_mat
        
        eb = bone_map[node_name]
        eb.matrix = world_matrix
        
        # Children
        children = [n for n in nodes if n['parent'] == node_name]
        for child in children:
            apply_transform(child['name'], world_matrix)

    # Find roots (nodes where parent is null or not in our list)
    roots = [n for n in nodes if n['parent'] == "null" or not any(p['name'] == n['parent'] for p in nodes)]
    for root in roots:
        apply_transform(root['name'], mathutils.Matrix.Identity(4))

    bpy.ops.object.mode_set(mode='OBJECT')

    # Create Meshes
    for m_data in meshes:
        mesh = bpy.data.meshes.new(m_data['name'])
        obj = bpy.data.objects.new(m_data['name'], mesh)
        bpy.context.collection.objects.link(obj)
        
        mesh.from_pydata(m_data['verts'], [], m_data['faces'])
        mesh.update()
        
        # Add Armature Modifier
        mod = obj.modifiers.new(name="Armature", type='ARMATURE')
        mod.object = arm_obj
        
        # Create Vertex Groups and assign weights
        for bone_name in m_data['bones']:
            obj.vertex_groups.new(name=bone_name)
            
        for v_idx, w_data in enumerate(m_data['weights']):
            # w_data: i0, w0, i1, w1, i2, w2, i3, w3
            for i in range(0, 8, 2):
                b_idx = w_data[i]
                weight = w_data[i+1]
                if weight > 0 and b_idx < len(m_data['bones']):
                    b_name = m_data['bones'][b_idx]
                    obj.vertex_groups[b_name].add([v_idx], weight, 'REPLACE')

    print("Import Complete")

# Example usage:
# import_raw_vibe('/home/bamn/UnityVibeBridge/avatar_dump.vibe')
