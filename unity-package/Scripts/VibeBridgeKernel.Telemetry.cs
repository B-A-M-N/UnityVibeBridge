#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        public static string VibeTool_telemetry_get_errors(Dictionary<string, string> q) { 
            return JsonUtility.ToJson(new ErrorRes { errors = _errors.ToArray() }); 
        } 
    }
}
#endif
