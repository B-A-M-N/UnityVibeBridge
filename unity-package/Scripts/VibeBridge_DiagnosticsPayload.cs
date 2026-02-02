#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityVibeBridge.Kernel {
    public static partial class VibeBridgeServer {

        [VibeTool("system/sdk_report", "Detects installed SDKs like VRCSDK and Poiyomi.")]
        public static string VibeTool_system_sdk_report(Dictionary<string, string> q) {
            var report = new Dictionary<string, bool>();
            
            // Check for VRCSDK
            report["VRCSDK_Base"] = TypeExists("VRC.SDKBase.VRC_AvatarDescriptor");
            report["VRCSDK_3A"] = TypeExists("VRC.SDK3.Avatars.Components.VRCAvatarDescriptor");
            
            // Check for Poiyomi
            report["Poiyomi_Shader"] = ShaderExists("Poiyomi");
            
            // Check for Modular Avatar
            report["ModularAvatar"] = TypeExists("nadena.dev.modular_avatar.core.ModularAvatarMenu");

            return JsonUtility.ToJson(new BasicRes { 
                conclusion = "SDK_REPORT_COMPLETE",
                message = string.Join(", ", report.Select(x => $"{x.Key}: {x.Value}"))
            });
        }

        private static bool TypeExists(string typeName) {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                if (assembly.GetType(typeName) != null) return true;
            }
            return false;
        }

        private static bool ShaderExists(string partName) {
            var shaders = ShaderUtil.GetAllShaderInfo();
            return shaders.Any(s => s.name.Contains(partName));
        }
    }
}
#endif
