using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        public static string VibeTool_hierarchy(Dictionary<string, string> q) {
            string rootId = q.ContainsKey("root") ? q["root"] : null;
            GameObject root = null;
            if (string.IsNullOrEmpty(rootId)) {
                var nodes = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Select(go => "{\"name\":\"" + go.name + "\",\"instanceID\":" + go.GetInstanceID() + "}");
                return "{\"nodes\":[" + string.Join(",", nodes) + "]}";
            }
            if (int.TryParse(rootId, out int id)) root = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (root == null) return "{\"error\":\"Root not found\"}";
            var children = new List<string>();
            for (int i = 0; i < root.transform.childCount; i++) {
                var child = root.transform.GetChild(i).gameObject;
                children.Add("{\"name\":\"" + child.name + "\",\"instanceID\":" + child.GetInstanceID() + "}");
            }
            return "{\"nodes\":[" + string.Join(",", children) + "]}";
        }

        public static string VibeTool_inspect(Dictionary<string, string> query) {
            string path = query["path"];
            GameObject obj = null;
            if (int.TryParse(path, out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(path);
            if (obj == null) return "{\"error\":\"Not found\"}";
            var names = obj.GetComponents<Component>().Select(c => "\"" + (c != null ? c.GetType().Name : "null") + "\"");
            return "{\"name\":\"" + obj.name + "\",\"components\":[" + string.Join(",", names) + "]}";
        }

        public static string VibeTool_undo(Dictionary<string, string> query) {
            Undo.PerformUndo();
            return "{\"message\":\"Undone\"}";
        }

        public static string VibeTool_unity_mesh_info(Dictionary<string, string> q) {
            GameObject obj = null;
            if (int.TryParse(q["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(q["path"]);
            if (obj == null) return "{\"error\":\"Not found\"}";
            var smr = obj.GetComponent<SkinnedMeshRenderer>();
            var mf = obj.GetComponent<MeshFilter>();
            Mesh mesh = smr != null ? smr.sharedMesh : (mf != null ? mf.sharedMesh : null);
            if (mesh == null) return "{\"error\":\"No mesh\"}";
            return "{\"vertices\":" + mesh.vertexCount + ",\"triangles\":" + (mesh.triangles.Length/3) + "}";
        }
    }
}
