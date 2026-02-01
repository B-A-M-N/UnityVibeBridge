import os

content = """#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        public static string VibeTool_status(Dictionary<string, string> q) { 
            return "{\"status\":\"connected\",\"kernel\":\"v1.2\",\"vetoed\":" + _isVetoed.ToString().ToLower() + "}"; 
        } 
        
        public static string VibeTool_system_undo(Dictionary<string, string> q) { Undo.PerformUndo(); return JsonUtility.ToJson(new BasicRes { message = "Undo performed" }); } 
        public static string VibeTool_system_redo(Dictionary<string, string> q) { Undo.PerformRedo(); return JsonUtility.ToJson(new BasicRes { message = "Redo performed" }); }

        public static string VibeTool_system_list_tools(Dictionary<string, string> q) {
            var tools = typeof(VibeBridgeServer).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.Name.StartsWith("VibeTool_"))
                .Select(m => m.Name.Substring(9).Replace("_", "/")).ToArray();
            return JsonUtility.ToJson(new ToolListRes { tools = tools });
        }

        public static string VibeTool_transaction_begin(Dictionary<string, string> q) { Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName(q.ContainsKey("name") ? q["name"] : "AI Op"); return JsonUtility.ToJson(new BasicRes { message = "Started", id = Undo.GetCurrentGroup() }); } 
        public static string VibeTool_transaction_commit(Dictionary<string, string> q) { Undo.CollapseUndoOperations(Undo.GetCurrentGroup()); return JsonUtility.ToJson(new BasicRes { message = "Committed" }); } 
        public static string VibeTool_transaction_abort(Dictionary<string, string> q) { Undo.RevertAllDownToGroup(Undo.GetCurrentGroup()); return JsonUtility.ToJson(new BasicRes { message = "Aborted" }); } 
        
        public static string VibeTool_object_set_value(Dictionary<string, string> q) {
            GameObject go = Resolve(q["path"]);
            if (go == null) return JsonUtility.ToJson(new BasicRes { error = "Object not found" });
            string compName = q["component"], fieldName = q["field"], val = q["value"];
            Component c = go.GetComponent(compName);
            if (c == null) return JsonUtility.ToJson(new BasicRes { error = "Component not found" });
            Undo.RecordObject(c, "Set Value");
            var type = c.GetType();
            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            var prop = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
            try {
                object parsedVal = null;
                var targetType = field?.FieldType ?? prop?.PropertyType;
                if (targetType == typeof(Vector3)) {
                    var p = val.Split(',').Select(float.Parse).ToArray();
                    parsedVal = new Vector3(p[0], p[1], p[2]);
                } else if (targetType == typeof(Color)) {
                    var p = val.Split(',').Select(float.Parse).ToArray();
                    parsedVal = new Color(p[0], p[1], p[2], p.Length > 3 ? p[3] : 1f);
                } else { parsedVal = Convert.ChangeType(val, targetType); }
                if (field != null) field.SetValue(c, parsedVal); else prop.SetValue(c, parsedVal);
                return JsonUtility.ToJson(new BasicRes { message = "Value updated" });
            } catch (Exception e) { return JsonUtility.ToJson(new BasicRes { error = e.Message }); }
        }

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
""

path = "/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/VibeBridge/VibeBridgeKernel.Management.cs"
with open(path, "w") as f:
    f.write(content)
print(f"Wrote {path}")
