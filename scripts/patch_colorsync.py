import os

path = "/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/VibeBridge/VibeBridgeKernel.ColorSync.cs"
with open(path, "r") as f:
    content = f.read()

old_block = """                foreach (var entry in _registry.entries.Where(e => e.group == "AccentAll")) {
                    var go = EditorUtility.InstanceIDToObject(entry.lastKnownID) as GameObject;
                    var r = go?.GetComponent<Renderer>();
                    if (r != null) {
                        foreach (var m in r.sharedMaterials) {
                            if (m == null) continue;
                            if (m.HasProperty("_Color")) m.SetColor("_Color", col);
                            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", col);
                        }
                    }
                }"""

new_block = """                foreach (var entry in _registry.entries.Where(e => e.group == "AccentAll")) {
                    var go = EditorUtility.InstanceIDToObject(entry.lastKnownID) as GameObject;
                    var r = go?.GetComponent<Renderer>();
                    if (r != null) {
                        if (entry.slotIndex >= 0 && entry.slotIndex < r.sharedMaterials.Length) {
                            var m = r.sharedMaterials[entry.slotIndex];
                            if (m != null) {
                                if (m.HasProperty("_Color")) m.SetColor("_Color", col);
                                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", col);
                            }
                        } else {
                            foreach (var m in r.sharedMaterials) {
                                if (m == null) continue;
                                if (m.HasProperty("_Color")) m.SetColor("_Color", col);
                                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", col);
                            }
                        }
                    }
                }"""

if old_block in content:
    content = content.replace(old_block, new_block)
    with open(path, "w") as f:
        f.write(content)
    print("Success")
else:
    # Try with different indentation or slightly different content
    print("Old block not found. Check indentation.")
