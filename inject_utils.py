import os

path = "/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/VibeBridge/VibeBridgeKernel.Utils.cs"
with open(path, "r") as f:
    content = f.read()

insertion_point = "public static class ShaderUtils {"
code_to_insert = """
        private static GameObject Resolve(string path) {
            if (string.IsNullOrEmpty(path)) return null;
            if (int.TryParse(path, out int id)) return EditorUtility.InstanceIDToObject(id) as GameObject;
            return GameObject.Find(path);
        }

        private static string ResolveAssetPath(string query, string filter = "") {
            if (File.Exists(query)) return query;
            string[] guids = AssetDatabase.FindAssets(query + " " + filter);
            if (guids.Length > 0) return AssetDatabase.GUIDToAssetPath(guids[0]);
            return null;
        }

"""

if insertion_point in content and "private static GameObject Resolve" not in content:
    new_content = content.replace(insertion_point, code_to_insert + insertion_point)
    with open(path, "w") as f:
        f.write(new_content)
    print("Injected Resolve methods.")
else:
    print("Injection point not found or methods already exist.")
