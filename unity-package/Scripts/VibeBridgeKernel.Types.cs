#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using MemoryPack;

namespace VibeBridge {
    [MemoryPackable] [Serializable] public partial class SessionData { public string sessionNonce; public List<int> createdObjectIds = new List<int>(); }
    [MemoryPackable] [Serializable] public partial class AirlockCommand { public string action, id, capability; public string[] keys, values; }
    [MemoryPackable] [Serializable] public partial class RecipeCommand { public AirlockCommand[] tools; }
    [MemoryPackable] [Serializable] public partial class KernelSettings {
        public PortSettings ports = new PortSettings();
        [MemoryPackable] [Serializable] public partial class PortSettings { public int control = 8085, vision = 8086; }
    }

    // --- RESPONSE WRAPPERS ---
    [MemoryPackable] [Serializable] public partial class BasicRes { public string message, error; public int id; }
    [MemoryPackable] [Serializable] public partial class InspectRes {
        public string name, tag, error;
        public bool active;
        public int layer;
        public Vector3 pos, rot, scale;
        public string[] components;
        public string[] blendshapes;
    }
    [MemoryPackable] [Serializable] public partial class HierarchyRes { public ObjectNode[] objects; [MemoryPackable] [Serializable] public partial struct ObjectNode { public string name; public int id; } }
    [MemoryPackable] [Serializable] public partial class ToolListRes { public string[] tools; }
    [MemoryPackable] [Serializable] public partial class ErrorRes { public string[] errors; }
    
    // --- PAYLOAD SHARED TYPES ---
    [MemoryPackable] [Serializable] public partial class MatListRes { public MatNode[] materials; [MemoryPackable] [Serializable] public partial struct MatNode { public int index; public string name; } }
    [MemoryPackable] [Serializable] public partial class MatPropRes { public string name, shader; public string[] properties; }
    [MemoryPackable] [Serializable] public partial class MatSnapshot { public string avatarName; public List<RendererSnapshot> renderers = new List<RendererSnapshot>(); }
    [MemoryPackable] [Serializable] public partial class RendererSnapshot { public string path; public List<string> materialGuids = new List<string>(); }

    // --- PAYLOAD RESPONSE WRAPPERS ---
    [MemoryPackable] [Serializable] public partial class VramRes { public float vramMB; public int textures; }
    [MemoryPackable] [Serializable] public partial class MissingScriptsRes { public int missing; public BasicRes[] details; }
    [MemoryPackable] [Serializable] public partial class AvatarAuditRes { public string name; public RendererAudit[] renderers; }
    [MemoryPackable] [Serializable] public partial class RendererAudit { public string path; public int verts, mats; }
    [MemoryPackable] [Serializable] public partial class StaticFlagRes { public StaticFlagNode[] flags; [MemoryPackable] [Serializable] public partial struct StaticFlagNode { public string name; public int value; } }
    [MemoryPackable] [Serializable] public partial class PhysicsAuditRes { public PhysicsNode[] physicsObjects; [MemoryPackable] [Serializable] public partial struct PhysicsNode { public string name, type; public bool isKinematic, isTrigger; } }
    [MemoryPackable] [Serializable] public partial class AnimationAuditRes { public AnimatorNode[] animators; [MemoryPackable] [Serializable] public partial struct AnimatorNode { public string name; public int missingClips; } }
    [MemoryPackable] [Serializable] public partial class FindRes { public BasicRes[] results; }
    [MemoryPackable] [Serializable] public partial class PhysBoneRankRes { public BoneRankNode[] bones; [MemoryPackable] [Serializable] public partial struct BoneRankNode { public string name; public float weight; public int childCount; } }

    [Serializable] public class AuditEntry {
        public string prevHash;
        public string timestamp;
        public string capability;
        public string action;
        public string details;
        public string entryHash;
    }

    // --- INVARIANT POLL SET TYPES ---
    [Serializable] public class HeartbeatRes {
        public int unity_pid;
        public bool editor_responsive;
        public bool domain_reload_in_progress;
        public bool compilation_in_progress;
        public bool last_compile_success;
        public string last_compile_hash;
        public int script_error_count;
    }

    [Serializable] public class AssemblyStateRes {
        public AssemblyInfo[] assemblies_loaded;
        public string[] missing_types;
        public bool asmdef_cycles_detected;
        [Serializable] public struct AssemblyInfo { public string name; public bool compiled; public int error_count; public string hash; }
    }

    [Serializable] public class ExecutionModeRes {
        public string mode;
        public bool entering_playmode;
        public bool exiting_playmode;
        public float time_scale;
    }

    [Serializable] public class ErrorStateRes {
        public UnityError[] errors;
        public string error_hash;
        [Serializable] public struct UnityError { public string type; public string file; public int line; public string message; }
    }

    [Serializable] public class AssetStateRes {
        public string asset_db_state;
        public bool refresh_in_progress;
        public FileHash[] file_hashes;
        [Serializable] public struct FileHash { public string path; public string hash; }
    }
}
#endif
