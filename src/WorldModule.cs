using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        private static string VibeTool_world_static(Dictionary<string, string> query) {
            GameObject obj = null;
            if (int.TryParse(query["path"], out int id)) obj = EditorUtility.InstanceIDToObject(id) as GameObject;
            else obj = GameObject.Find(query["path"]);
            if (obj == null) return "{\"error\":\"Object not found\"}";

            StaticEditorFlags staticFlags = (StaticEditorFlags)Enum.Parse(typeof(StaticEditorFlags), query["flags"]);
            Undo.RecordObject(obj, "Set Static Flags");
            GameObjectUtility.SetStaticEditorFlags(obj, staticFlags);
            return "{\"message\":\"Success\",\"flags\":\"" + GameObjectUtility.GetStaticEditorFlags(obj).ToString() + "\"}";
        }

        private static string VibeTool_world_navmesh_bake(Dictionary<string, string> query) {
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            return "{\"message\":\"NavMesh Bake Triggered\"}";
        }

        private static string VibeTool_world_spawn(Dictionary<string, string> query) {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(query["asset"]);
            if (prefab == null) return "{\"error\":\"Prefab not found\"}";
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(go, "Spawn Object");
            if (query.ContainsKey("name")) go.name = query["name"];
            return "{\"message\":\"Spawned\",\"instanceID\":" + go.GetInstanceID() + "}";
        }
    }
}
