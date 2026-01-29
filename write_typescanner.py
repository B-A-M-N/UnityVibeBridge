import os

file_path = "/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/Editor/TypeScanner.cs"
content = """using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Linq;

public static class TypeScanner
{
    public static void Scan()
    {
        Debug.Log("[TypeScan] SCANNING ModelExporter methods...");
        Assembly fbxAssembly = Assembly.Load("Unity.Formats.Fbx.Editor");
        System.Type exporterType = fbxAssembly.GetType("UnityEditor.Formats.Fbx.Exporter.ModelExporter");
        
        foreach (var m in exporterType.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (m.Name == "ExportObject" || m.Name == "ExportObjects")
            {
                string paramsStr = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name));
                Debug.Log("[TypeScan]   Method: " + m.Name + "(" + paramsStr + ")");
            }
        }
    }
}
"""

os.makedirs(os.path.dirname(file_path), exist_ok=True)
with open(file_path, "w") as f:
    f.write(content)
print(f"Successfully wrote {file_path}")
