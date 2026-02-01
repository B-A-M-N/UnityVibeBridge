import os

core_file = "/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/VibeBridge/VibeBridgeKernel.Core.cs"
with open(core_file, 'r') as f:
    content = f.read()

new_tool = """
        public static string VibeTool_material_poiyomi_lock(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Target object not found" });
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            var matsToLock = new List<Material>();
            foreach (var r in renderers) {
                foreach (var m in r.sharedMaterials) {
                    if (m != null && m.shader != null && m.shader.name.Contains("Poiyomi")) {
                        if (!m.shader.name.Contains("Optimized")) matsToLock.Add(m);
                    }
                }
            }
            if (matsToLock.Count == 0) return JsonUtility.ToJson(new BasicRes { message = "No unlocked Poiyomi materials found on target." });
            try {
                System.Reflection.Assembly thryAssembly = System.AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "ThryEditor");
                if (thryAssembly == null) return JsonUtility.ToJson(new BasicRes { error = "ThryEditor not found." });
                System.Type optimizerType = thryAssembly.GetType("Thry.ShaderOptimizer");
                System.Reflection.MethodInfo optimizeMethod = optimizerType?.GetMethod("Optimize", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, new System.Type[] { typeof(Material[]) }, null);
                if (optimizeMethod == null) return JsonUtility.ToJson(new BasicRes { error = "Optimize method not found." });
                optimizeMethod.Invoke(null, new object[] { matsToLock.ToArray() });
                return JsonUtility.ToJson(new BasicRes { message = "Locked " + matsToLock.Count + " materials." });
            } catch (System.Exception e) { return JsonUtility.ToJson(new BasicRes { error = "Bake failed: " + e.Message }); }
        }
"""

if "VibeTool_material_poiyomi_lock" not in content:
    # Insert before the closing brace of the class
    # We find the last closing brace of the file which should be for the namespace,
    # and the one before it for the class.
    # Actually, let's just replace the last '    }' with tool + '    }'
    pos = content.rfind("    }")
    if pos != -1:
        new_content = content[:pos] + new_tool + content[pos:]
        with open(core_file, 'w') as f:
            f.write(new_content)
        print("Successfully updated VibeBridgeKernel.Core.cs")
else:
    print("Tool already exists in VibeBridgeKernel.Core.cs")
