import os

real_file = "/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/VibeBridge/VibeBridge_MaterialPayload.cs"
with open(real_file, 'r') as f:
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
    # Insert before the last closing brace of the class
    last_brace = content.rfind("}")
    # We need to find the brace of the namespace or class. 
    # Let's just find the last method and insert after it.
    insert_pos = content.rfind("public static string VibeTool_")
    if insert_pos != -1:
        # Find end of that method
        end_of_method = content.find("}", insert_pos)
        if end_of_method != -1:
            new_content = content[:end_of_method+1] + new_tool + content[end_of_method+1:]
            with open(real_file, 'w') as f:
                f.write(new_content)
            print("Successfully updated real payload.")
else:
    print("Tool already exists in real payload.")
