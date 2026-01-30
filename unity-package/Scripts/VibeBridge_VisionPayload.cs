// UnityVibeBridge: The Governed Creation Kernel for Unity
// Copyright (C) 2026 B-A-M-N
//
// This software is dual-licensed under the GNU AGPLv3 and a 
// Commercial "Work-or-Pay" Maintenance Agreement.
//
// You may use this file under the terms of the AGPLv3, provided 
// you meet all requirements (including source disclosure).
//
// For commercial use, or to keep your modifications private, 
// you must satisfy the requirements of the Commercial Path 
// as defined in the LICENSE file at the project root.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        private static HttpListener _visionListener;
        private static bool _visionRunning = false;
        private static byte[] _latestFrame;
        private static readonly object _frameLock = new object();

        private static void StartVisionBroadcaster() {
            if (_visionRunning) return;
            try {
                _visionListener = new HttpListener();
                _visionListener.Prefixes.Add($"http://127.0.0.1:{_settings.ports.vision}/");
                _visionListener.Start();
                _visionRunning = true;
                ThreadPool.QueueUserWorkItem(o => {
                    while (_visionRunning) {
                        try {
                            var context = _visionListener.GetContext();
                            ThreadPool.QueueUserWorkItem(c => ServeVisionStream((HttpListenerContext)c), context);
                        } catch { } 
                    }
                });
                EditorApplication.update += CaptureVisionFrame;
                Debug.Log($"[VibeBridge] Vision Broadcaster Active on Port {_settings.ports.vision}");
            } catch (Exception e) { Debug.LogError("Vision Start Failed: " + e.Message); }
        }

        private static void StopVisionBroadcaster() {
            _visionRunning = false;
            if (_visionListener != null) {
                try { _visionListener.Close(); } catch {}
                _visionListener = null;
            }
            EditorApplication.update -= CaptureVisionFrame;
            if (_visionTex != null) { UnityEngine.Object.DestroyImmediate(_visionTex); _visionTex = null; }
            if (_visionRT != null) { RenderTexture.ReleaseTemporary(_visionRT); _visionRT = null; }
        }

        private static Texture2D _visionTex;
        private static RenderTexture _visionRT;
        private static float _lastVisionCapture = 0;

        private static void CaptureVisionFrame() {
            if (SceneView.lastActiveSceneView == null || SceneView.lastActiveSceneView.camera == null) return;
            if (Time.realtimeSinceStartup - _lastVisionCapture < 0.066f) return; // ~15 FPS
            _lastVisionCapture = Time.realtimeSinceStartup;

            Camera cam = SceneView.lastActiveSceneView.camera;
            int w = 640, h = 360;
            
            if (_visionRT == null) _visionRT = RenderTexture.GetTemporary(w, h, 24);
            if (_visionTex == null) _visionTex = new Texture2D(w, h, TextureFormat.RGB24, false);

            cam.targetTexture = _visionRT;
            cam.Render();
            RenderTexture.active = _visionRT;
            _visionTex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            cam.targetTexture = null;
            RenderTexture.active = null;
            
            byte[] bytes = _visionTex.EncodeToJPG(75);
            lock (_frameLock) { _latestFrame = bytes; }
        }

        private static void ServeVisionStream(HttpListenerContext context) {
            try {
                context.Response.ContentType = "multipart/x-mixed-replace; boundary=--frame";
                context.Response.StatusCode = 200;

                while (_visionRunning && context.Response.OutputStream.CanWrite) {
                    byte[] frame;
                    lock (_frameLock) { frame = _latestFrame; }

                    if (frame != null) {
                        string header = "--frame\r\nContent-Type: image/jpeg\r\nContent-Length: " + frame.Length + "\r\n\r\n";
                        byte[] headerBytes = System.Text.Encoding.ASCII.GetBytes(header);
                        context.Response.OutputStream.Write(headerBytes, 0, headerBytes.Length);
                        context.Response.OutputStream.Write(frame, 0, frame.Length);
                        byte[] footer = System.Text.Encoding.ASCII.GetBytes("\r\n");
                        context.Response.OutputStream.Write(footer, 0, footer.Length);
                    }
                    Thread.Sleep(66);
                }
            } catch { } finally { try { context.Response.Close(); } catch { } }
        }
    }
}
#endif
