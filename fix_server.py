import os

csharp_code = r"""using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VibeBridge
{
    [InitializeOnLoad]
    public static class VibeBridgeServer
    {
        private static HttpListener _listener;
        private static Thread _serverThread;
        private static bool _isRunning = true;
        private static readonly Queue<HttpListenerContext> _requestQueue = new Queue<HttpListenerContext>();
        private const string PORT = "8085";

        static VibeBridgeServer()
        {
            EditorApplication.update -= HandlePendingRequests;
            EditorApplication.quitting -= StopServer;
            AssemblyReloadEvents.beforeAssemblyReload -= StopServer;
            StartServer();
            EditorApplication.update += HandlePendingRequests;
            EditorApplication.quitting += StopServer;
            AssemblyReloadEvents.beforeAssemblyReload += StopServer;
        }

        private static void StartServer()
        {
            if (_serverThread != null && _serverThread.IsAlive) return;
            _serverThread = new Thread(Listen);
            _serverThread.IsBackground = true;
            _serverThread.Start();
            Debug.Log("[VibeBridge] Server started on port " + PORT);
        }

        private static void StopServer()
        {
            _isRunning = false;
            if (_listener != null) { _listener.Stop(); _listener.Close(); }
        }

        private static void Listen()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:" + PORT + "/");
            _listener.Start();
            while (_isRunning)
            {
                try { var context = _listener.GetContext(); lock (_requestQueue) { _requestQueue.Enqueue(context); } } 
                catch { break; }
            }
        }

        private static void HandlePendingRequests()
        {
            lock (_requestQueue) { while (_requestQueue.Count > 0) ProcessRequest(_requestQueue.Dequeue()); }
        }

        private static void ProcessRequest(HttpListenerContext context)
        {
            try
            {
                string path = context.Request.Url.AbsolutePath;
                var query = context.Request.QueryString;
                string responseString = "";

                if (path == "/status") responseString = "{\"status\":\"connected\"}";
                else if (path == "/ping2") responseString = "{\"pong\":true}";
                else if (path == "/asset/find") responseString = FindAssets(query["filter"]);
                else if (path == "/asset/inspect") responseString = InspectAsset(query["path"]);
                else if (path == "/vrc/menu/inspect") responseString = InspectVRCMenu(query["path"]);
                else if (path == "/vrc/menu/clear") responseString = ClearVRCMenu(query["path"]);
                else if (path == "/vrc/menu/add") responseString = AddVRCMenuControl(query);
                else if (path == "/vrc/param/inspect") responseString = InspectVRCParams(query["path"]);
                else if (path == "/vrc/param/add") responseString = AddVRCParameter(query);
                else { context.Response.StatusCode = 404; responseString = "{\"error\":\"Unknown\"}"; }

                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                context.Response.ContentType = "application/json";
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                context.Response.StatusCode = 500;
                byte[] buffer = Encoding.UTF8.GetBytes("{\"error\":\"" + e.Message.Replace("\"", "\\\"") + "\"}");
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            finally { context.Response.OutputStream.Close(); }
        }

        private static string FindAssets(string filter)
        {
            var results = AssetDatabase.FindAssets(filter).Take(20).Select(guid => {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                return "{\"name\":\"" + Path.GetFileNameWithoutExtension(p) + "\",\"path\":\"" + p + "\"}";
            });
            return "{\"results\":[" + string.Join(",", results) + "]}";
        }

        private static string InspectAsset(string path)
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset == null) return "{\"error\":\"Not found\"}";
            return "{\"name\":\"" + asset.name + "\",\"type\":\"" + asset.GetType().Name + "\"}";
        }

        private static string InspectVRCMenu(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset == null) return "{\"error\":\"Not found\"}";
            var so = new SerializedObject(asset);
            var controlsProp = so.FindProperty("controls");
            var controls = new List<string>();
            for (int i = 0; i < (controlsProp?.arraySize ?? 0); i++)
            {
                var control = controlsProp.GetArrayElementAtIndex(i);
                var subMenu = control.FindPropertyRelative("subMenu").objectReferenceValue;
                string subPath = subMenu != null ? AssetDatabase.GetAssetPath(subMenu) : "";
                string name = control.FindPropertyRelative("name").stringValue.Replace("\"", "\\\"");
                string param = control.FindPropertyRelative("parameter").FindPropertyRelative("name").stringValue.Replace("\"", "\\\"");
                int type = control.FindPropertyRelative("type").enumValueIndex;
                controls.Add("{\"name\":\"" + name + "\",\"subMenu\":\"" + subPath + "\",\"type\":" + type + ",\"parameter\":\"" + param + "\"}");
            }
            return "{\"name\":\"" + asset.name + "\",\"controls\":[" + string.Join(",", controls) + "]}";
        }

        private static string ClearVRCMenu(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset == null) return "{\"error\":\"Not found\"}";
            var so = new SerializedObject(asset);
            so.FindProperty("controls").ClearArray();
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            return "{\"message\":\"Cleared\"}";
        }

        private static string AddVRCMenuControl(System.Collections.Specialized.NameValueCollection q)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(q["path"]);
            if (asset == null) return "{\"error\":\"Not found\"}";
            var so = new SerializedObject(asset);
            var controlsProp = so.FindProperty("controls");
            controlsProp.InsertArrayElementAtIndex(controlsProp.arraySize);
            var control = controlsProp.GetArrayElementAtIndex(controlsProp.arraySize - 1);
            
            control.FindPropertyRelative("name").stringValue = q["name"] ?? "";
            control.FindPropertyRelative("type").enumValueIndex = string.IsNullOrEmpty(q["type"]) ? 0 : int.Parse(q["type"]);
            control.FindPropertyRelative("parameter").FindPropertyRelative("name").stringValue = q["parameter"] ?? "";
            control.FindPropertyRelative("subMenu").objectReferenceValue = null;

            if (!string.IsNullOrEmpty(q["subMenu"])) {
                control.FindPropertyRelative("subMenu").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(q["subMenu"]);
            }
            
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            return "{\"message\":\"Added\"}";
        }

        private static string InspectVRCParams(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset == null) return "{\"error\":\"Not found\"}";
            var so = new SerializedObject(asset);
            var paramsProp = so.FindProperty("parameters");
            var list = new List<string>();
            for (int i = 0; i < (paramsProp?.arraySize ?? 0); i++)
            {
                list.Add("\"" + paramsProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue + "\"");
            }
            return "{\"name\":\"" + asset.name + "\",\"parameters\":[" + string.Join(",", list) + "]}";
        }

        private static string AddVRCParameter(System.Collections.Specialized.NameValueCollection q)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(q["path"]);
            if (asset == null) return "{\"error\":\"Not found\"}";
            var so = new SerializedObject(asset);
            var paramsProp = so.FindProperty("parameters");
            paramsProp.InsertArrayElementAtIndex(paramsProp.arraySize);
            var param = paramsProp.GetArrayElementAtIndex(paramsProp.arraySize - 1);
            param.FindPropertyRelative("name").stringValue = q["name"];
            param.FindPropertyRelative("valueType").enumValueIndex = int.Parse(q["type"]);
            param.FindPropertyRelative("saved").boolValue = q["saved"] == "true";
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            return "{\"message\":\"Added\"}";
        }
    }
}
"""

OUTPUT_PATH = "unity-package/Scripts/VibeBridgeServer.cs"

if __name__ == "__main__":
    if not os.path.exists(os.path.dirname(OUTPUT_PATH)):
        os.makedirs(os.path.dirname(OUTPUT_PATH))
    with open(OUTPUT_PATH, "w") as f:
        f.write(csharp_code)
    print(f"Success: Server script restored to {OUTPUT_PATH}")

