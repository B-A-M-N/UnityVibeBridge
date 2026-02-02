#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace UnityVibeBridge.Kernel {
    public static partial class VibeBridgeServer {
        private static HttpListener _visionListener;
        private static Thread _visionThread;
        private static byte[] _lastFrameData;
        private static readonly object _frameLock = new object();

        private static void StartVisionBroadcaster() {
            try {
                if (_visionListener != null && _visionListener.IsListening) return;
                _visionListener = new HttpListener();
                _visionListener.Prefixes.Add($"http://localhost:{_settings.ports.vision}/");
                _visionListener.Start();

                _visionThread = new Thread(() => {
                    while (_visionListener != null && _visionListener.IsListening) {
                        try {
                            var context = _visionListener.GetContext();
                            ProcessVisionRequest(context);
                        } catch (Exception e) {
                            _errors.Add($"[VisionThread] request error: {e.Message}");
                        }
                    }
                });
                _visionThread.IsBackground = true;
                _visionThread.Start();
                Debug.Log($"[Vibe] Vision Broadcaster active on Port {_settings.ports.vision}");
            } catch (HttpListenerException ex) when (ex.ErrorCode == 183 || ex.ErrorCode == 48 || ex.ErrorCode == 10048) {
                Debug.LogError($"[Vibe] Vision PORT BUSY: Port {_settings.ports.vision} is already in use.");
                File.WriteAllText("metadata/vibe_vision_error.json", "{\"error\":\"PORT_BUSY\", \"port\":" + _settings.ports.vision + "}");
            } catch (Exception e) {
                Debug.LogError($"[Vibe] Vision failed: {e.Message}");
                File.WriteAllText("metadata/vibe_vision_error.json", "{\"error\":\"VISION_CRASH\", \"message\":\"" + e.Message.Replace("\"", "'") + "\"}");
            }
        }

        private static void StopVisionBroadcaster() {
            if (_visionListener != null) {
                _visionListener.Stop();
                _visionListener.Close();
                _visionListener = null;
            }
            if (_visionThread != null) {
                _visionThread.Abort();
                _visionThread = null;
            }
        }

        private static void ProcessVisionRequest(HttpListenerContext context) {
            var response = context.Response;
            byte[] data = null;
            lock (_frameLock) { data = _lastFrameData; }

            if (data == null) {
                response.StatusCode = 404;
                response.Close();
                return;
            }

            try {
                response.ContentType = "image/jpeg";
                response.ContentLength64 = data.Length;
                // Add correlation headers
                response.Headers.Add("X-Vibe-Tick", _monotonicTick.ToString());
                response.Headers.Add("X-Vibe-State", _lastAuditHash);
                
                response.OutputStream.Write(data, 0, data.Length);
                response.OutputStream.Close();
            } catch (Exception e) {
                _errors.Add($"[Vision] Write error: {e.Message}");
            }
        }

        // Called from PollAirlock to capture frames safely on main thread
        private static void UpdateVisionCapture() {
            if (_visionListener == null || !_visionListener.IsListening) return;
            
            // Limit capture rate to ~15 FPS
            if (Time.realtimeSinceStartup % 0.06f > 0.02f) return;

            if (SceneView.lastActiveSceneView == null || SceneView.lastActiveSceneView.camera == null) return;

            Camera cam = SceneView.lastActiveSceneView.camera;
            int w = 512, h = 512;
            RenderTexture rt = RenderTexture.GetTemporary(w, h, 24);
            var prevRT = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            
            RenderTexture.active = rt;
            Texture2D ss = new Texture2D(w, h, TextureFormat.RGB24, false);
            ss.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            ss.Apply();
            
            byte[] jpg = ss.EncodeToJPG(75);
            
            lock (_frameLock) { _lastFrameData = jpg; }

            cam.targetTexture = prevRT;
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(ss);
        }
    }
}
#endif
