using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace VibeBridge {
    public static partial class VibeBridgeServer {
        public static string ExecuteAirlockCommand(AirlockCommand cmd) {
            try {
                string path = cmd.action.TrimStart('/');
                // Default mapping: replace / and - with _
                string methodName = "VibeTool_" + path.Replace("/", "_").Replace("-", "_");
                
                // Specific manual overrides for tools that might fail the auto-map
                if (path == "asset/set-internal-name") methodName = "VibeTool_asset_set_internal_name";
                if (path == "material/inspect-properties") methodName = "VibeTool_material_inspect_properties";
                if (path == "material/inspect-slot") methodName = "VibeTool_material_inspect_slot";
                if (path == "material/clear-block") methodName = "VibeTool_material_clear_block";
                if (path == "material/set-color") methodName = "VibeTool_material_set_color";
                if (path == "material/remove-slot") methodName = "VibeTool_material_remove_slot";
                if (path == "material/insert-slot") methodName = "VibeTool_material_insert_slot";
                if (path == "material/set-slot-texture") methodName = "VibeTool_material_set_slot_texture";
                if (path == "unity/mesh-info") methodName = "VibeTool_unity_mesh_info";
                if (path == "material/snapshot") methodName = "VibeTool_material_snapshot";
                if (path == "material/restore") methodName = "VibeTool_material_restore";
                if (path == "system/focus") methodName = "VibeTool_system_focus";
                if (path == "opt/fork") methodName = "VibeTool_opt_fork";
                if (path == "opt/shader/quest") methodName = "VibeTool_shader_swap_quest";
                if (path == "opt/texture/crush") methodName = "VibeTool_texture_crush";
                if (path == "opt/mesh/simplify") methodName = "VibeTool_opt_mesh_simplify";
                if (path == "vrc/param/get") methodName = "VibeTool_vrc_param_get";
                if (path == "vrc/param/set") methodName = "VibeTool_vrc_param_set";

                var method = typeof(VibeBridgeServer).GetMethod(methodName, 
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);

                if (method == null) return "{\"error\":\"Tool not found: " + path + " (looked for " + methodName + ")\"}";

                var query = new Dictionary<string, string>();
                if (cmd.keys != null && cmd.values != null) {
                    for (int i = 0; i < Math.Min(cmd.keys.Length, cmd.values.Length); i++) {
                        query[cmd.keys[i]] = cmd.values[i];
                    }
                }
                return (string)method.Invoke(null, new object[] { query });
            } catch (Exception e) {
                return "{\"error\":\"" + e.Message.Replace("\"", "\\\"") + "\"}";
            }
        }

        public static string VibeTool_status(Dictionary<string, string> query) {
            return "{\"status\":\"connected\",\"mode\":\"modular-v16-router-robust\"}";
        }
    }
}
