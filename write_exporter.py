import os

file_path = "/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/Editor/FbxAirlockExporter.cs"
content = """using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;
using System.Linq;

public static class FbxAirlockExporter
{
    [MenuItem("Vibe/Force Export Avatar")]
    public static void ExportAvatar()
    {
        Debug.Log("[Airlock] STARTING PREFLIGHT...");
        GameObject target = GameObject.Find("ExtoPc");
        if (target == null) {
            Debug.LogError("[Airlock] FAILED: Could not find 'ExtoPc' in hierarchy.");
            return;
        }

        // 1. Sanitize Materials (Standard Swap)
        Debug.Log("[Airlock] SANITIZING MATERIALS...");
        Shader standardShader = Shader.Find("Standard");
        var renderers = target.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers) {
            Material[] sharedMats = r.sharedMaterials;
            for (int i = 0; i < sharedMats.Length; i++) {
                if (sharedMats[i] == null) continue;
                Material exportMat = new Material(standardShader);
                exportMat.name = sharedMats[i].name + "_EXPORT";
                if (sharedMats[i].HasProperty("_Color")) exportMat.color = sharedMats[i].color;
                sharedMats[i] = exportMat;
            }
            r.sharedMaterials = sharedMats;
        }

        // 2. Execute Export
        string filePath = "/home/bamn/ALCOM/Projects/BAMN-EXTO/ExtoPC_Airlock_Export.fbx";
        Debug.Log("[Airlock] EXPORTING TO: " + filePath);
        
        try {
            Assembly fbxAssembly = Assembly.Load("Unity.Formats.Fbx.Editor");
            System.Type exporterType = fbxAssembly.GetType("UnityEditor.Formats.Fbx.Exporter.ModelExporter");
            MethodInfo exportMethod = exporterType.GetMethod("ExportObject", new System.Type[] { typeof(string), typeof(UnityEngine.Object) });
            
            if (exportMethod != null) {
                exportMethod.Invoke(null, new object[] { filePath, target });
                Debug.Log("[Airlock] EXPORT COMMAND INVOKED.");
            }
            
            if (File.Exists(filePath)) {
                Debug.Log("[Airlock] SUCCESS: " + filePath + " (" + new FileInfo(filePath).Length + " bytes)");
            } else {
                Debug.LogError("[Airlock] FAILED: File not found after export.");
            }
        } catch (System.Exception e) {
            Debug.LogError("[Airlock] EXCEPTION: " + e.ToString());
        }
    }
}
"""

os.makedirs(os.path.dirname(file_path), exist_ok=True)
with open(file_path, "w") as f:
    f.write(content)
print(f"Successfully wrote {file_path}")