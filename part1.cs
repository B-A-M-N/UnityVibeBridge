#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        public static string VibeTool_status(Dictionary<string, string> q) { 
            return "{\"status\":\"connected\",\"kernel\":\"v1.2\",\"vetoed\":