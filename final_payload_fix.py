import os

file_path = "/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/VibeBridge/VibeBridge_MaterialPayload.cs"
with open(file_path, 'r') as f:
    lines = f.readlines()

# Find the last closing brace of the class (the one before the last closing brace of the namespace)
# We look for the last two '}'
braces = [i for i, line in enumerate(lines) if '}' in line]

if len(braces) >= 2:
    insert_pos = braces[-2] # The class closing brace
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
                System.Reflection.MethodInfo optimizeMethod = optimizerType?.GetMethod("Optimize", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, new System.Type[] { typeof(Material[]), typeof(bool) }, null);
                if (optimizeMethod == null) {
                    // Try without the bool parameter if the 2-arg version isn't found
                    optimizeMethod = optimizerType?.GetMethod("Optimize", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, new System.Type[] { typeof(Material[]) }, null);
                }
                if (optimizeMethod == null) return JsonUtility.ToJson(new BasicRes { error = "Optimize method not found." });
                
                object[] args = (optimizeMethod.GetParameters().Length == 2) ? new object[] { matsToLock.ToArray(), false } : new object[] { matsToLock.ToArray() };
                optimizeMethod.Invoke(null, args);
                return JsonUtility.ToJson(new BasicRes { message = "Locked " + matsToLock.Count + " materials." });
            } catch (System.Exception e) { return JsonUtility.ToJson(new BasicRes { error = "Bake failed: " + e.Message }); }
        }
"""
    lines.insert(insert_pos, new_tool)
    with open(file_path, 'w') as f:
        f.writelines(lines)
    print("Successfully injected Poiyomi Lock tool.")
else:
    print("Failed to find insertion point.")
