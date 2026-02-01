#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        public static string VibeTool_engine_assembly_state(Dictionary<string, string> q) {
            var assemblies = CompilationPipeline.GetAssemblies();
            var bridgeAssembly = assemblies.FirstOrDefault(a => a.name == "VibeBridge");
            
            var res = new AssemblyStateRes {
                assemblies_loaded = bridgeAssembly != null ? new AssemblyStateRes.AssemblyInfo[] {
                    new AssemblyStateRes.AssemblyInfo {
                        name = bridgeAssembly.name,
                        compiled = true,
                        error_count = 0 // CompilationPipeline doesn't expose current error count easily here
                    }
                } : new AssemblyStateRes.AssemblyInfo[0],
                asmdef_cycles_detected = false // Placeholder for complex logic
            };

            return JsonUtility.ToJson(res);
        }

        public static string VibeTool_engine_error_state(Dictionary<string, string> q) {
            // Converts the internal _errors list into the structured invariant format
            var res = new ErrorStateRes {
                errors = _errors.Select(e => new ErrorStateRes.UnityError {
                    message = e,
                    type = e.Contains("error CS") ? "ScriptError" : "RuntimeError"
                }).ToArray()
            };
            
            // Generate deterministic hash of the current error surface
            res.error_hash = ComputeHash(string.Join("|", _errors));
            
            return JsonUtility.ToJson(res);
        }
    }
}
#endif
