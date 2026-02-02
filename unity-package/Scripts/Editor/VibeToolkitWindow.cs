#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityVibeBridge.Kernel;

namespace UnityVibeBridge.Kernel.Editor {
    public class VibeToolkitWindow : EditorWindow {
        [MenuItem("VibeBridge/Toolkit Window")]
        public static void ShowWindow() {
            GetWindow<VibeToolkitWindow>("Vibe Toolkit");
        }

        private string idToSelect = "";

        void OnGUI() {
            GUILayout.Label("Selection Tools", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Copy Selected ID", GUILayout.Height(30))) {
                if (Selection.activeGameObject != null) {
                    GUIUtility.systemCopyBuffer = Selection.activeGameObject.GetInstanceID().ToString();
                    Debug.Log("ID Copied: " + GUIUtility.systemCopyBuffer);
                }
            }

            if (GUILayout.Button("Copy Selected Path", GUILayout.Height(30))) {
                if (Selection.activeGameObject != null) {
                    GUIUtility.systemCopyBuffer = GetPath(Selection.activeGameObject);
                    Debug.Log("Path Copied: " + GUIUtility.systemCopyBuffer);
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("Action Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("FIX PINK SHADERS (On Selected)", GUILayout.Height(40))) {
                if (Selection.activeGameObject != null) {
                    VibeBridgeServer.ExecuteAirlockCommand(new AirlockCommand {
                        action = "material/fix-broken-mat",
                        keys = new string[] { "path" },
                        values = new string[] { GetPath(Selection.activeGameObject) }
                    });
                } else {
                    Debug.LogWarning("Select an object first!");
                }
            }

            GUILayout.Space(10);
            idToSelect = EditorGUILayout.TextField("Select by ID:", idToSelect);
            if (GUILayout.Button("Select Object")) {
                if (int.TryParse(idToSelect, out int id)) {
                    GameObject go = EditorUtility.InstanceIDToObject(id) as GameObject;
                    if (go != null) Selection.activeGameObject = go;
                }
            }
        }

        private string GetPath(GameObject go) {
            string path = go.name;
            while (go.transform.parent != null) {
                go = go.transform.parent.gameObject;
                path = go.name + "/" + path;
            }
            return path;
        }
    }
}
#endif
