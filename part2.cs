
        public static string VibeTool_object_rename(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            Undo.RecordObject(go, "Rename"); go.name = q["newName"]; return JsonUtility.ToJson(new BasicRes { message = "Renamed" });
        }

        public static string VibeTool_object_reparent(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]), p = q.ContainsKey("newParent") ? Resolve(q["newParent"]) : null; 
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Target not found" });
            Undo.SetTransformParent(go.transform, p != null ? p.transform : null, "Reparent"); return JsonUtility.ToJson(new BasicRes { message = "Reparented" });
        }
        public static string VibeTool_object_clone(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            GameObject c = UnityEngine.Object.Instantiate(go); c.name = go.name + "_Clone"; Undo.RegisterCreatedObjectUndo(c, "Clone"); return JsonUtility.ToJson(new BasicRes { message = "Cloned", id = c.GetInstanceID() });
        }
        public static string VibeTool_object_delete(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            Undo.DestroyObjectImmediate(go); return JsonUtility.ToJson(new BasicRes { message = "Deleted" });
        }
        public static string VibeTool_system_select(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Not found" });
            Selection.activeGameObject = go;
            bool forceFrame = q.ContainsKey("frame") && q["frame"].ToLower() == "true";
            if (forceFrame || SceneView.lastActiveSceneView == null || !SceneView.lastActiveSceneView.hasFocus) SceneView.FrameLastActiveSceneView(); 
            return JsonUtility.ToJson(new BasicRes { message = "Selected" });
        }
    }
}
#endif
