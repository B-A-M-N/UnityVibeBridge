#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

#if VIBE_MEMORYPACK
using MemoryPack;
#endif

namespace UnityVibeBridge.Kernel {
    public enum ToolID {
        MaterialList, MaterialInspectProperties, MaterialSetColor, MaterialSetTexture, MaterialSetFloat, MaterialToggleKeyword,
        MaterialSyncSlots, MaterialAssign, MaterialSnapshot, MaterialRestore, MaterialFixBroken, MaterialHideSlot, MaterialPoiyomiLock,
        ObjectSetActive, ObjectSetBlendshape, ObjectSetValue, ObjectRename, ObjectReparent, ObjectClone, ObjectDelete,
        SystemVramFootprint, TextureCrush, WorldSpawn, WorldSpawnPrimitive,
        WorldStaticList, WorldStaticSet, AssetMove, PrefabApply, ViewScreenshot, MaterialBatchReplace,
        SystemFindByComponent, SystemSearch, SystemSelect, OptFork, SystemGitCheckpoint, Inspect, Hierarchy,
        AuditAvatar, PhysicsAudit, AnimationAudit, PhysboneRankImportance,
        VisualPoint, VisualLine, VisualClear, AnimatorSetParam, ExportValidate,
        SystemUndo, SystemRedo, SystemListTools, TransactionBegin, TransactionCommit, TransactionAbort, Status,
        SystemVeto, SystemUnveto, SystemExecuteRecipe, MaterialSwapToQuestShaders
    }

#if VIBE_MEMORYPACK
    [MemoryPackable]
#endif
    [Serializable] public partial class SessionData { public string sessionNonce; public string sceneGuid; public List<int> createdObjectIds = new List<int>(); }

#if VIBE_MEMORYPACK
    [MemoryPackable]
#endif
    [Serializable] public partial class RegistryData {
        public string sceneGuid;
        public List<RegistryEntry> entries = new List<RegistryEntry>();
        public string lastUpdate;
    }

#if VIBE_MEMORYPACK
    [MemoryPackable]
#endif
    [Serializable] public partial class RegistryEntry {
        public string uuid;
        public string role; 
        public string group; 
        public string path;
        public int lastKnownID;
        public int slotIndex = -1;
    }

#if VIBE_MEMORYPACK
    [MemoryPackable]
#endif
    [Serializable] public partial class AirlockCommand { 
        public string action, id, capability; 
        public string[] keys, values; 
    }

    [Serializable] public partial class RecipeCommand { public AirlockCommand[] tools; }

    [Serializable] public partial class KernelSettings {
        public PortSettings ports = new PortSettings();
        [Serializable] public partial class PortSettings { public int control = 8085, vision = 8086; }
    }

    [Serializable] public partial class ResponseWrapper {
        public string payload;
        public long monotonicTick;
        public string state;
        public float mainThreadBudgetUsed;
        public bool overBudget;
    }

    // --- RESPONSE WRAPPERS ---
    [Serializable] public partial class BasicRes { 
        public string message, error; 
        public int id;
        public string conclusion; 
        public List<VibeBelief> derived_beliefs = new List<VibeBelief>(); 
    }

    [Serializable] public partial class InspectRes {
        public string name, tag, error;
        public bool active;
        public int layer;
        public Vector3 pos, rot, scale;
        public string[] components;
        public string[] blendshapes;
    }

    [Serializable] public partial class HierarchyRes { 
        public ObjectNode[] objects; 
        [Serializable] public partial struct ObjectNode { public string name; public int id; } 
    }

    [Serializable] public partial class ToolListRes { public string[] tools; }

    [Serializable] public partial class ErrorRes { public string[] errors; }
    
    // --- PAYLOAD SHARED TYPES ---
    [Serializable] public partial class MatListRes { 
        public MatNode[] materials; 
        [Serializable] public partial struct MatNode { public int index; public string name; } 
    }

    [Serializable] public partial class MatPropRes { public string name, shader; public string[] properties; }

    [Serializable] public partial class MatSnapshot { public string avatarName; public List<RendererSnapshot> renderers = new List<RendererSnapshot>(); }

    [Serializable] public partial class RendererSnapshot { public string path; public List<string> materialGuids = new List<string>(); }

    // --- PAYLOAD RESPONSE WRAPPERS ---
    [Serializable] public partial class VramRes { public float vramMB; public int textures; }

    [Serializable] public partial class MissingScriptsRes { public int missing; public BasicRes[] details; }

    [Serializable] public partial class AvatarAuditRes { public string name; public RendererAudit[] renderers; }

    [Serializable] public partial class RendererAudit { public string path; public int verts, mats; }

    [Serializable] public partial class StaticFlagRes { 
        public StaticFlagNode[] flags; 
        [Serializable] public partial struct StaticFlagNode { public string name; public int value; } 
    }

    [Serializable] public partial class PhysicsAuditRes { 
        public PhysicsNode[] physicsObjects; 
        [Serializable] public partial struct PhysicsNode { public string name, type; public bool isKinematic, isTrigger; } 
    }

    [Serializable] public partial class AnimationAuditRes { 
        public AnimatorNode[] animators; 
        [Serializable] public partial struct AnimatorNode { public string name; public int missingClips; } 
    }

    [Serializable] public partial class FindRes { public BasicRes[] results; }

    [Serializable] public partial class AssemblyStateRes {
        public AssemblyInfo[] assemblies_loaded;
        public bool asmdef_cycles_detected;
        [Serializable] public partial struct AssemblyInfo { public string name; public bool compiled; public int error_count; }
    }

    [Serializable] public partial class ErrorStateRes {
        public UnityError[] errors;
        public string error_hash;
        [Serializable] public partial struct UnityError { public string message, type; }
    }

    [Serializable] public partial class PhysBoneRankRes { 
        public BoneRankNode[] bones; 
        [Serializable] public partial struct BoneRankNode { public string name; public float weight; public int childCount; } 
    }

    [Serializable] public partial class HeartbeatRes {
        public int unity_pid;
        public bool editor_responsive, domain_reload_in_progress, compilation_in_progress, last_compile_success;
        public int script_error_count;
    }

    [Serializable] public partial class ExecutionModeRes {
        public string mode;
        public bool entering_playmode, exiting_playmode;
        public float time_scale;
    }

    [Serializable] public partial class AssetStateRes {
        public string asset_db_state;
        public bool refresh_in_progress;
        public FileHash[] file_hashes;
        [Serializable] public partial struct FileHash { public string path, hash; }
    }

    [Serializable] public partial class AuditEntry {
        public string prevHash, timestamp, capability, action, details, entryHash;
    }

    // --- CORE AGENT TYPES ---
    [Serializable] public partial class VibeBelief {
        public string conclusion;
        public string[] provenance;
        public float confidence;
        public long expiry;
    }

    [Serializable] public partial class WorkOrder {
        public string id;
        public string intent;
        public string action;
        public string target_uuid;
        public string rationale;
        public string[] opcodes;
    }

    // --- INDUSTRIAL INTELLIGENCE (SITREP & AFFORDANCES) ---
    [Serializable] public partial class SitrepRes {
        public string target_uuid;
        public string state_summary;
        public List<AffordanceNode> affordances = new List<AffordanceNode>();
        public List<string> active_constraints = new List<string>();
    }

    [Serializable] public partial struct AffordanceNode {
        public string capability; 
        public bool available;
        public string reason; 
    }

    [Serializable] public partial class StrategicPlan {
        public string id;
        public string goal;
        public List<PlanStep> steps = new List<StrategicPlan.PlanStep>();
        [Serializable] public partial struct PlanStep { public int order; public string intent; public string description; }
    }

    [Serializable] public partial class IntegrityReport {
        public bool passed;
        public string[] issues;
        public string state_hash_delta;
    }

    [Serializable] public partial class ApiDumpRes {
        public ApiNode[] tools;
        [Serializable] public partial struct ApiNode {
            public string name, description;
            public string[] parameters;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public partial class VibeToolAttribute : Attribute {
        public string Name { get; }
        public string Description { get; }
        public string[] Params { get; }
        public VibeToolAttribute(string name, string description, params string[] parameters) {
            Name = name;
            Description = description;
            Params = parameters;
        }
    }
}
#endif