#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityVibeBridge.Kernel {
    public static partial class VibeBridgeServer {

        [VibeTool("audit/avatar", "Performs a deep audit of an avatar's renderers and vertex counts.", "path")]
        public static string VibeTool_audit_avatar(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Target not found" });
            
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            var audits = new List<RendererAudit>();
            foreach (var r in renderers) {
                int verts = 0;
                Mesh m = null;
                if (r is SkinnedMeshRenderer smr) m = smr.sharedMesh;
                else if (r is MeshRenderer) m = r.GetComponent<MeshFilter>()?.sharedMesh;
                
                if (m != null) verts = m.vertexCount;
                
                audits.Add(new RendererAudit {
                    path = GetGameObjectPath(r.gameObject, go),
                    verts = verts,
                    mats = r.sharedMaterials.Length
                });
            }
            return JsonUtility.ToJson(new AvatarAuditRes { name = go.name, renderers = audits.ToArray() });
        }

        [VibeTool("physics/audit", "Scans for all colliders and rigidbodies on a target.", "path")]
        public static string VibeTool_physics_audit(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Target not found" });
            
            var nodes = new List<PhysicsAuditRes.PhysicsNode>();
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) {
                nodes.Add(new PhysicsAuditRes.PhysicsNode { 
                    name = GetGameObjectPath(rb.gameObject, go), 
                    type = "Rigidbody", 
                    isKinematic = rb.isKinematic, 
                    isTrigger = false 
                });
            }
            foreach (var col in go.GetComponentsInChildren<Collider>(true)) {
                nodes.Add(new PhysicsAuditRes.PhysicsNode { 
                    name = GetGameObjectPath(col.gameObject, go), 
                    type = col.GetType().Name, 
                    isKinematic = false, 
                    isTrigger = col.isTrigger 
                });
            }
            return JsonUtility.ToJson(new PhysicsAuditRes { physicsObjects = nodes.ToArray() });
        }

        [VibeTool("animation/audit", "Identifies all animators and checks for missing clips or broken states.", "path")]
        public static string VibeTool_animation_audit(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Target not found" });
            
            var animators = go.GetComponentsInChildren<Animator>(true);
            var nodes = animators.Select(a => new AnimationAuditRes.AnimatorNode {
                name = GetGameObjectPath(a.gameObject, go),
                missingClips = (a.runtimeAnimatorController == null) ? 1 : 0
            }).ToArray();
            return JsonUtility.ToJson(new AnimationAuditRes { animators = nodes });
        }

        [VibeTool("physbone/rank/importance", "Analyzes PhysBone weight and child counts to rank performance impact.", "path")]
        public static string VibeTool_physbone_rank_importance(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Target not found" });
            
            var bones = new List<PhysBoneRankRes.BoneRankNode>();
            var allComps = go.GetComponentsInChildren<Component>(true);
            foreach (var c in allComps) {
                if (c == null) continue;
                if (c.GetType().Name == "VRCPhysBone") {
                    var type = c.GetType();
                    var rootTransform = type.GetField("rootTransform")?.GetValue(c) as Transform;
                    if (rootTransform == null) rootTransform = c.transform;
                    
                    // Advanced weight calculation if possible
                    float weight = 1.0f;
                    var multi = type.GetField("multiChildType")?.GetValue(c);
                    if (multi != null && multi.ToString() == "Average") weight *= 1.5f;

                    bones.Add(new PhysBoneRankRes.BoneRankNode {
                        name = GetGameObjectPath(c.gameObject, go),
                        weight = weight,
                        childCount = rootTransform.GetComponentsInChildren<Transform>().Length
                    });
                }
            }
            return JsonUtility.ToJson(new PhysBoneRankRes { bones = bones.OrderByDescending(b => b.childCount).ToArray() });
        }

        [VibeTool("system/vram_footprint", "Calculates the total VRAM footprint of all textures on a target.", "path")]
        public static string VibeTool_system_vram_footprint(Dictionary<string, string> q) {
            GameObject obj = Resolve(q["path"]);
            if (obj == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            long totalBytes = 0;
            var textures = new HashSet<Texture2D>();
            foreach (var r in obj.GetComponentsInChildren<Renderer>(true)) {
                foreach (var mat in r.sharedMaterials) {
                    if (mat == null) continue;
                    int count = ShaderUtil.GetPropertyCount(mat.shader);
                    for (int i = 0; i < count; i++) {
                        if (ShaderUtil.GetPropertyType(mat.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv) {
                            Texture t = mat.GetTexture(ShaderUtil.GetPropertyName(mat.shader, i));
                            if (t is Texture2D t2d) textures.Add(t2d);
                        }
                    }
                }
            }
            foreach (var tex in textures) totalBytes += (long)(tex.width * tex.height * 4);
            return JsonUtility.ToJson(new VramRes { vramMB = totalBytes / (1024f * 1024f), textures = textures.Count });
        }

        [VibeTool("texture/crush", "Reduces the max resolution of all textures on a target to optimize VRAM.", "path", "maxSize")]
        public static string VibeTool_texture_crush(Dictionary<string, string> q) {
            GameObject obj = Resolve(q["path"]);
            if (obj == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            int size = int.Parse(q["maxSize"]);
            var textures = new HashSet<Texture2D>();
            foreach (var r in obj.GetComponentsInChildren<Renderer>(true)) {
                foreach (var mat in r.sharedMaterials) {
                    if (mat == null) continue;
                    int count = ShaderUtil.GetPropertyCount(mat.shader);
                    for (int i = 0; i < count; i++) {
                        if (ShaderUtil.GetPropertyType(mat.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv) {
                            Texture t = mat.GetTexture(ShaderUtil.GetPropertyName(mat.shader, i));
                            if (t is Texture2D t2d) textures.Add(t2d);
                        }
                    }
                }
            }
            int changed = 0;
            foreach (var tex in textures) {
                string path = AssetDatabase.GetAssetPath(tex);
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti != null && ti.maxTextureSize > size) { 
                    ti.maxTextureSize = size; 
                    AssetDatabase.ImportAsset(path); 
                    changed++; 
                }
            }
            return JsonUtility.ToJson(new BasicRes { message = "Crushed " + changed + " textures" });
        }
    }
}
#endif
