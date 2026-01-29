using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        private static HttpListener _listener;
        private static Thread _serverThread;
        private static readonly Queue<HttpListenerContext> _requestQueue = new Queue<HttpListenerContext>();
        private const string PORT = "8085";

        // Extend Startup to include HTTP
        private static void StartHttpServer() {
            if (_serverThread != null && _serverThread.IsAlive) return;
            _serverThread = new Thread(Listen);
            _serverThread.IsBackground = true;
            _serverThread.Start();
            Debug.Log("[VibeBridge] HTTP Server started on port " + PORT);
        }

        private static void StopHttpServer() {
            if (_listener != null) { 
                try { _listener.Stop(); _listener.Close(); } catch {}
            }
        }

        private static void Listen() {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://127.0.0.1:" + PORT + "/");
            _listener.Prefixes.Add("http://[::1]:" + PORT + "/");
            _listener.Start();
            while (_currentState == BridgeState.Running) {
                try { 
                    var context = _listener.GetContext(); 
                    lock (_requestQueue) { _requestQueue.Enqueue(context); } 
                } catch { break; }
            }
        }

        private static void HandleHttpRequests() {
            lock (_requestQueue) {
                while (_requestQueue.Count > 0) {
                    ProcessHttpRequest(_requestQueue.Dequeue());
                }
            }
        }

        private static void ProcessHttpRequest(HttpListenerContext context) {
            try {
                string action = context.Request.Url.AbsolutePath.TrimStart('/');
                var query = context.Request.QueryString;
                
                var cmd = new AirlockCommand {
                    action = action,
                    keys = query.AllKeys,
                    values = Array.ConvertAll(query.AllKeys, k => query[k])
                };

                string result = ExecuteAirlockCommand(cmd);
                byte[] buffer = Encoding.UTF8.GetBytes(result);
                context.Response.ContentType = "application/json";
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            } catch (Exception e) {
                context.Response.StatusCode = 500;
                byte[] buffer = Encoding.UTF8.GetBytes("{\"error\":\"" + e.Message + "\"}");
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            } finally {
                context.Response.OutputStream.Close();
            }
        }
    }
}
