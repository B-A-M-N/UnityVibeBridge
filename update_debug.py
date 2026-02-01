import os

path = "/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/VibeBridge/VibeBridgeKernel.Debug.cs"
with open(path, "w") as f:
    f.write("""#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        public static string VibeTool_debug_find_optimizer(Dictionary<string, string> q) {
            var results = new List<string>();
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies()) {
                try {
                    var type = a.GetType("Thry.ShaderOptimizer");
                    if (type != null) results.Add(a.GetName().Name);
                } catch {}
            }
            return JsonUtility.ToJson(new ToolListRes { tools = results.ToArray() });
        }
    }
}
#endif
""")
