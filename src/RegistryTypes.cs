using System;
using System.Collections.Generic;
using UnityEngine;

namespace VibeBridge {
    [Serializable] public class RegistryData { public List<RegistryEntry> entries = new List<RegistryEntry>(); }
    [Serializable] public class RegistryEntry { 
        public string uuid, role, group; 
        public int lastKnownID; 
        public int slotIndex = -1; // -1 means all slots
        public Fingerprint fingerprint; 
    }
    [Serializable] public class Fingerprint { public string meshName; public int triangles, vertices; public string[] shaders, components; }
    [Serializable] public class SessionData { public string sessionNonce; public List<int> createdObjectIds = new List<int>(); }
    [Serializable] public class AirlockCommand { public string action, id, capability; public string[] keys, values; }

    [Serializable]
    public class MaterialSnapshot {
        public string avatarName;
        public List<RendererSnapshot> renderers = new List<RendererSnapshot>();
    }

    [Serializable]
    public class RendererSnapshot {
        public string path;
        public List<string> materialGuids = new List<string>();
    }
}