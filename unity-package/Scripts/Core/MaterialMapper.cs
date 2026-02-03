#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityVibeBridge.Kernel.Core {
    [Serializable]
    public class MaterialManifest {
        public string avatarName;
        public List<MeshMap> meshes = new List<MeshMap>();
    }

    [Serializable]
    public class MeshMap {
        public string meshName;
        public List<SlotMap> slots = new List<SlotMap>();
    }

    [Serializable]
    public class SlotMap {
        public int index;
        public string materialName;
        public string shader;
        public List<TexMap> textures = new List<TexMap>();
        public List<ColorMap> colors = new List<ColorMap>();
    }

    [Serializable]
    public class TexMap {
        public string property;
        public string fileName;
        public string originalPath;
    }

    [Serializable]
    public class ColorMap {
        public string property;
        public Color value;
    }

    public static class MaterialMapper {
        public static MaterialManifest GenerateManifest(GameObject root, string exportDir) {
            var manifest = new MaterialManifest { avatarName = root.name };
            var renderers = root.GetComponentsInChildren<Renderer>(true);

            foreach (var r in renderers) {
                var meshMap = new MeshMap { meshName = r.name };
                var mats = r.sharedMaterials;

                for (int i = 0; i < mats.Length; i++) {
                    var m = mats[i];
                    if (m == null) continue;

                    var slot = new SlotMap {
                        index = i,
                        materialName = m.name,
                        shader = m.shader.name
                    };

                    // Extract Textures and copy them
                    var shader = m.shader;
                    int propCount = ShaderUtil.GetPropertyCount(shader);
                    for (int j = 0; j < propCount; j++) {
                        var propName = ShaderUtil.GetPropertyName(shader, j);
                        var propType = ShaderUtil.GetPropertyType(shader, j);

                        if (propType == ShaderUtil.ShaderPropertyType.TexEnv) {
                            var tex = m.GetTexture(propName);
                            if (tex != null) {
                                string path = AssetDatabase.GetAssetPath(tex);
                                if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets/")) {
                                    string fileName = Path.GetFileName(path);
                                    string dest = Path.Combine(exportDir, fileName);
                                    
                                    // Copy texture to export folder
                                    try {
                                        if (!File.Exists(dest)) {
                                            File.Copy(Path.Combine(Directory.GetCurrentDirectory(), path), dest);
                                        }
                                    } catch (Exception e) {
                                        Debug.LogWarning($"[VibeBridge] Failed to copy texture {path}: {e.Message}");
                                    }

                                    slot.textures.Add(new TexMap {
                                        property = propName,
                                        fileName = fileName,
                                        originalPath = path
                                    });
                                }
                            }
                        } else if (propType == ShaderUtil.ShaderPropertyType.Color) {
                            slot.colors.Add(new ColorMap {
                                property = propName,
                                value = m.GetColor(propName)
                            });
                        }
                    }
                    meshMap.slots.Add(slot);
                }
                manifest.meshes.Add(meshMap);
            }
            return manifest;
        }
    }
}
#endif
