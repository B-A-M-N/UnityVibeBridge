using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        
        // --- VIEW MODULE ---
        // Handles visual feedback and viewport captures.

        public static string VibeTool_view_screenshot(Dictionary<string, string> q) {
            string filename = q.ContainsKey("filename") ? q["filename"] : "screenshot_latest.png";
            string path = Path.Combine("captures", filename);
            if (!Directory.Exists("captures")) Directory.CreateDirectory("captures");

            // Capture the active Scene View
            Camera cam = SceneView.lastActiveSceneView.camera;
            if (cam == null) return "{\"error\":\"No active SceneView found\"}";

            int width = q.ContainsKey("width") ? int.Parse(q["width"]) : 1280;
            int height = q.ContainsKey("height") ? int.Parse(q["height"]) : 720;

            RenderTexture rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
            cam.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            cam.targetTexture = null;
            RenderTexture.active = null;
            GameObject.DestroyImmediate(rt);

            byte[] bytes = screenShot.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            
            // ALWAYS update the monitor's latest file
            if (filename != "screenshot_latest.png") {
                File.WriteAllBytes(Path.Combine("captures", "screenshot_latest.png"), bytes);
            }

            // Log mutation for audit trail
            LogMutation("VIEW", "global", "screenshot", path);

            return "{\"message\":\"Screenshot saved\",\"path\":\"" + path + ",\"base64\":\"" + Convert.ToBase64String(bytes) + "\"}";
        }
    }
}
