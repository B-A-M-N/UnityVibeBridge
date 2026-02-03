#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityVibeBridge.Kernel.Core;

namespace UnityVibeBridge.Kernel {
    [InitializeOnLoad]
    public static partial class VibeBridgeServer {
        private enum BridgeState { Stopped, Starting, Running, Stopping }
        private static BridgeState _currentState = BridgeState.Stopped;
        private static string _inboxPath, _outboxPath;
        private static bool _isProcessing = false;
        private static bool _isVetoed = false;
        private static string _persistentNonce = null;
        private static string _lastAuditHash = "GENESIS";
        private static List<string> _errors = new List<string>();
        private static KernelSettings _settings = new KernelSettings();

        // Server State
        private static HttpListener _httpListener;
        private static Thread _serverThread;
        private static readonly object _queueLock = new object();
        private static Queue<HttpListenerContext> _requestQueue = new Queue<HttpListenerContext>();

        static VibeBridgeServer() {
            Debug.Log("[VibeBridge] Initializing Kernel...");
            LoadSettings();
            AssemblyReloadEvents.beforeAssemblyReload += () => Teardown();
            EditorApplication.quitting += () => Teardown();
            Startup();
        }

        private static void LoadSettings() {
            string path = "metadata/vibe_settings.json";
            if (File.Exists(path)) try { _settings = JsonUtility.FromJson<KernelSettings>(File.ReadAllText(path)); } catch { }
        }

        private static void Startup() {
            _inboxPath = Path.Combine(Directory.GetCurrentDirectory(), "vibe_queue/inbox");
            _outboxPath = Path.Combine(Directory.GetCurrentDirectory(), "vibe_queue/outbox");
            if (!Directory.Exists(_inboxPath)) Directory.CreateDirectory(_inboxPath);
            if (!Directory.Exists(_outboxPath)) Directory.CreateDirectory(_outboxPath);
            
            VibeMetadataProvider.Initialize();
            LoadOrCreateSession();
            InitializeTelemetry();
            StartHttpServer();
            StartVisionBroadcaster();
            
            EditorApplication.update -= PollAirlock;
            EditorApplication.update += PollAirlock;
            _currentState = BridgeState.Running;
            SetStatus("Ready");
            Debug.Log("[VibeBridge] Kernel Active on Port " + _settings.ports.control);
        }

        public static void Reinitialize() {
            Teardown();
            Startup();
        }

        private static void Teardown() {
            _currentState = BridgeState.Stopping;
            StopHttpServer();
            StopVisionBroadcaster();
            EditorApplication.update -= PollAirlock;
            SetStatus("Stopped");
            _currentState = BridgeState.Stopped;
        }

        private static void PollAirlock() {
            UpdateHeartbeat();
            UpdateVisionCapture();
            
            string currentStatus = GetEngineStatus();
            SetStatus(currentStatus);

            if (_currentState != BridgeState.Running || _isProcessing || _isVetoed) return;
            
            HandleHttpRequests(); 
            UpdateColorSync(); 

            if (currentStatus != "Ready") return;

            string[] pending = Directory.GetFiles(_inboxPath, "*.json");
            if (pending.Length == 0) return;

            _isProcessing = true;
            try { 
                AssetDatabase.StartAssetEditing();
                foreach (var f in pending) { 
                    ProcessAirlockFile(f); 
                } 
            } finally { 
                AssetDatabase.StopAssetEditing();
                _isProcessing = false; 
            } 
        }

        private static void ProcessAirlockFile(string f) {
            string responsePath = Path.Combine(_outboxPath, "res_" + Path.GetFileName(f));
            try {
                var command = JsonUtility.FromJson<AirlockCommand>(File.ReadAllText(f));
                string payload = ExecuteAirlockCommand(command);
                
                _monotonicTick++;
                var wrapper = new ResponseWrapper {
                    payload = payload,
                    monotonicTick = _monotonicTick,
                    state = _lastAuditHash,
                    mainThreadBudgetUsed = 0, // Not measured for file-system yet
                    overBudget = false
                };
                
                File.WriteAllText(responsePath, JsonUtility.ToJson(wrapper));
            } catch (Exception e) { 
                var errObj = new BasicRes { 
                    error = e.Message.Replace("\"", "'"),
                    conclusion = "FILE_SYSTEM_IPC_ERROR",
                    message = e.StackTrace.Replace("\"", "'").Replace("\n", "\\n")
                };
                File.WriteAllText(responsePath, JsonUtility.ToJson(errObj)); 
            } finally { 
                try { if (File.Exists(f)) File.Delete(f); } catch {}
            }
        }

        // --- HTTP SERVER IMPLEMENTATION ---

        private static void StartHttpServer() {
            try {
                if (_httpListener != null && _httpListener.IsListening) return;
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"http://localhost:{_settings.ports.control}/");
                _httpListener.Start();

                _serverThread = new Thread(() => {
                    while (_httpListener != null && _httpListener.IsListening) {
                        try {
                            var context = _httpListener.GetContext();
                            lock (_queueLock) { _requestQueue.Enqueue(context); }
                        } catch (Exception e) {
                            // Non-fatal loop error, log to internal buffer
                            _errors.Add($"[ServerThread] context error: {e.Message}");
                        } 
                    }
                });
                _serverThread.IsBackground = true;
                _serverThread.Start();
            } catch (HttpListenerException ex) when (ex.ErrorCode == 183 || ex.ErrorCode == 48 || ex.ErrorCode == 10048) {
                Debug.LogError($"[VibeBridge] PORT BUSY: Port {_settings.ports.control} is already in use.");
                File.WriteAllText("metadata/vibe_server_error.json", "{\"error\":\"PORT_BUSY\", \"port\":" + _settings.ports.control + "}");
            } catch (Exception e) {
                Debug.LogError($"[VibeBridge] Failed to start HTTP Server: {e.Message}");
                File.WriteAllText("metadata/vibe_server_error.json", "{\"error\":\"SERVER_CRASH\", \"message\":\"" + e.Message.Replace("\"", "'") + "\"}");
            }
        }

        private static void StopHttpServer() {
            if (_httpListener != null) {
                try { _httpListener.Stop(); _httpListener.Close(); } catch {}
                _httpListener = null;
            }
            if (_serverThread != null) {
                try { _serverThread.Abort(); } catch {}
                _serverThread = null;
            }
        }

        private static void HandleHttpRequests() {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int processed = 0;
            
            // --- UNDO GROUPING START ---
            int undoGroup = -1;
            bool mutationFound = false;

            while (processed < 5 && stopwatch.ElapsedMilliseconds < 10) { 
                HttpListenerContext context;
                lock (_queueLock) {
                    if (_requestQueue.Count == 0) break;
                    context = _requestQueue.Dequeue();
                }
                
                // Only start a group if we have pending requests that might mutate
                if (!mutationFound && context.Request.Url.AbsolutePath == "/vibe") {
                    undoGroup = Undo.GetCurrentGroup();
                    Undo.IncrementCurrentGroup();
                    Undo.SetCurrentGroupName("VibeBridge Batch");
                    mutationFound = true;
                }

                var toolStart = stopwatch.ElapsedMilliseconds;
                ProcessHttpContext(context);
                var toolEnd = stopwatch.ElapsedMilliseconds;
                
                if (toolEnd - toolStart > 5) {
                    Debug.LogWarning($"[Vibe Watchdog] Main Thread Over-Budget: {context.Request.Url.AbsolutePath} took {toolEnd - toolStart}ms");
                }
                
                processed++;
            }

            if (mutationFound && undoGroup != -1) {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        private static long _monotonicTick = 0;
        private static void ProcessHttpContext(HttpListenerContext context) {
            var request = context.Request;
            var response = context.Response;
            string responseString = "";
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try {
                if (request.Url.AbsolutePath == "/health") {
                    responseString = "{\"status\":\"Ready\",\"kernel\":\"v1.3.7\"}";
                } else {
                    string payload = "";
                    if (request.Url.AbsolutePath == "/vibe") {
                        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding)) {
                            string json = reader.ReadToEnd();
                            var cmd = JsonUtility.FromJson<AirlockCommand>(json);
                            payload = ExecuteAirlockCommand(cmd);
                        }
                    } else {
                        // Handle direct GET requests for tools (e.g. /inspect?path=...)
                        string toolName = request.Url.AbsolutePath.TrimStart('/');
                        var query = request.QueryString;
                        var cmd = new AirlockCommand { action = toolName, capability = "read" };
                        var keys = new List<string>();
                        var values = new List<string>();
                        foreach (string key in query.AllKeys) {
                            keys.Add(key);
                            values.Add(query[key]);
                        }
                        cmd.keys = keys.ToArray();
                        cmd.values = values.ToArray();
                        payload = ExecuteAirlockCommand(cmd);
                    }

                    _monotonicTick++;
                    var wrapper = new ResponseWrapper {
                        payload = payload,
                        monotonicTick = _monotonicTick,
                        state = _lastAuditHash,
                        mainThreadBudgetUsed = stopwatch.ElapsedMilliseconds,
                        overBudget = stopwatch.ElapsedMilliseconds > 5
                    };
                    responseString = JsonUtility.ToJson(wrapper);
                }
            } catch (Exception e) {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                var errObj = new BasicRes { 
                    error = e.Message.Replace("\"", "'"),
                    conclusion = "INTERNAL_SERVER_ERROR",
                    message = e.StackTrace.Replace("\"", "'").Replace("\n", "\\n").Replace("\r", "")
                };
                responseString = JsonUtility.ToJson(errObj);
            }

            try {
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.ContentType = "application/json";
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            } catch {}
        }

        // --- GOVERNANCE & SECURITY ---
        
        private static string GetEngineStatus() {
            if (_panicMode) return "PANIC";
            if (_isVetoed) return "VETOED";
            if (EditorApplication.isCompiling) return "COMPILING";
            if (EditorApplication.isUpdating) return "UPDATING";
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "PLAYMODE_TRANSITION";
            // AssetDatabase.IsImportingAssets() is 2021+, isUpdating covers it in 2019
            if (EditorApplication.isUpdating) return "IMPORTING";
            return "Ready";
        }

        private static void SetStatus(string s) {
            try { File.WriteAllText("metadata/vibe_status.json", "{\"status\":\"" + s + "\",\"time\":\"" + DateTime.UtcNow.ToString("o") + "\"}"); } catch {}
        }
        
        private static int _violationCount = 0;
        private static bool _panicMode = false;

        private static bool IsSafeToMutate() {
            if (_panicMode) return false;
            if (EditorApplication.isCompiling) return false;
            if (EditorApplication.isUpdating) return false;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return false;
            // 2019.4 Compat: isUpdating covers import state
            return true;
        }

        public static void ReportViolation(string reason) {
            _violationCount++;
            Debug.LogError($"[Vibe] SECURITY VIOLATION ({_violationCount}/3): {reason}");
            if (_violationCount >= 3) {
                _panicMode = true;
                SetStatus("PANIC");
                Debug.LogError("[Vibe] PANIC MODE ACTIVATED. Mutations disabled.");
            }
        }

        public static void ResetSecurity() {
            _violationCount = 0;
            _panicMode = false;
            SetStatus("Ready");
            Debug.Log("[Vibe] Security state reset.");
        }

        public static string GetStateHash() {
            // Returns a deterministic hash of the current error state and hierarchy
            return _lastAuditHash;
        }

        private static string _approvedIntent = null;
        public static void ApproveMutation(string intent) {
            _approvedIntent = intent;
            Debug.Log($"[Vibe] Human Approved: {intent}");
        }

        public static void CommitCheckpoint(string message) {
            try {
                AssetDatabase.SaveAssets();
                var proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = "python3";
                proc.StartInfo.Arguments = $"scripts/snap_commit.py \"{message}\"";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                proc.WaitForExit();
                Debug.Log($"[Vibe] Ghost Audit Complete: {message}");
            } catch (Exception e) {
                Debug.LogWarning($"[Vibe] Checkpoint failed: {e.Message}");
            }
        }

        private static void ValidateSanity(AirlockCommand cmd) {
            if (cmd.values == null) return;
            foreach (var val in cmd.values) {
                if (string.IsNullOrEmpty(val)) continue;
                if (float.TryParse(val, out float f)) {
                    if (float.IsNaN(f) || float.IsInfinity(f)) throw new VibeValidationException($"Geometric Insanity: {f}", "GEOMETRIC_REJECTION");
                    if (Math.Abs(f) > 10000000f) throw new VibeValidationException($"Magnitude Rejection: {f}", "MAGNITUDE_REJECTION");
                }
                if (val.Contains(",")) {
                    var parts = val.Split(',');
                    foreach (var p in parts) {
                        if (float.TryParse(p, out float pf)) {
                            if (Math.Abs(pf) > 10000000f) throw new VibeValidationException("Vector magnitude rejection.", "VECTOR_REJECTION");
                        }
                    }
                }
            }
        }

        public static string ExecuteAirlockCommand(AirlockCommand cmd) {
            // Check if this is actually a WorkOrder (Strategic Intent)
            if (cmd.action == "isa/execute") {
                try {
                    var order = new WorkOrder { action = cmd.action, id = cmd.id };
                    if (cmd.keys != null && cmd.values != null) {
                        for (int i = 0; i < Math.Min(cmd.keys.Length, cmd.values.Length); i++) {
                            if (cmd.keys[i] == "intent") order.intent = cmd.values[i];
                            if (cmd.keys[i] == "target_uuid") order.target_uuid = cmd.values[i];
                            if (cmd.keys[i] == "rationale") order.rationale = cmd.values[i];
                        }
                    }
                    return VibeISA.ExecuteIntent(order);
                } catch (Exception e) {
                    return JsonUtility.ToJson(new BasicRes { 
                        error = "ISA Execution Failed: " + e.Message,
                        conclusion = "ISA_FAILURE",
                        message = e.StackTrace
                    });
                }
            }

            try {
                if (!string.IsNullOrEmpty(cmd.capability) && !cmd.capability.Equals("read", StringComparison.OrdinalIgnoreCase)) {
                    if (!IsSafeToMutate()) return JsonUtility.ToJson(new BasicRes { error = "UNSAFE_STATE", conclusion = "LIFECYCLE_GUARD" });
                    ValidateSanity(cmd);
                }
                string path = cmd.action.TrimStart('/');
                string methodName = "VibeTool_" + path.Replace("/", "_").Replace("-", "_");
                if (path == "inspect") methodName = "VibeTool_inspect";
                
                // 1. Search by name convention
                var method = typeof(VibeBridgeServer).GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                
                // 2. Search by Attribute Name (more robust for tools with complex paths)
                if (method == null) {
                    var allMethods = typeof(VibeBridgeServer).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var m in allMethods) {
                        var attr = m.GetCustomAttribute<VibeToolAttribute>();
                        if (attr != null && attr.Name.Equals(path, StringComparison.OrdinalIgnoreCase)) {
                            method = m;
                            break;
                        }
                    }
                }

                if (method == null) return JsonUtility.ToJson(new BasicRes { error = "Tool not found: " + path });
                
                var query = new Dictionary<string, string>();
                if (cmd.keys != null && cmd.values != null) {
                    for (int i = 0; i < Math.Min(cmd.keys.Length, cmd.values.Length); i++) query[cmd.keys[i]] = cmd.values[i];
                }
                
                if (!string.IsNullOrEmpty(cmd.capability) && !cmd.capability.Equals("read", StringComparison.OrdinalIgnoreCase)) {
                    LogMutation(cmd.capability, cmd.action, JsonUtility.ToJson(cmd));
                }

                return (string)method.Invoke(null, new object[] { query });
            } catch (VibeValidationException ve) {
                return JsonUtility.ToJson(new BasicRes { error = ve.Message, conclusion = ve.Conclusion });
            } catch (TargetInvocationException tie) when (tie.InnerException != null) {
                return JsonUtility.ToJson(new BasicRes { 
                    error = tie.InnerException.Message, 
                    conclusion = "EXECUTION_ERROR",
                    message = tie.InnerException.StackTrace 
                });
            } catch (Exception e) { 
                return JsonUtility.ToJson(new BasicRes { 
                    error = e.Message, 
                    conclusion = "UNKNOWN_ERROR",
                    message = e.StackTrace 
                }); 
            }
        }
    }
}
#endif