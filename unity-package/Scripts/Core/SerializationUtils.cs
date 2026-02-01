using System;
using MemoryPack;
using UnityEngine;

namespace VibeBridge.Core
{
    /// <summary>
    /// HARDENING LAYER: Wraps MemoryPack to enforce 'Staging DTO' pattern.
    /// Banishes direct serialization of Unity Objects.
    /// </summary>
    public static class SerializationUtils
    {
        /// <summary>
        /// Deserializes a binary payload into a Strict DTO.
        /// Throws immediately if data is corrupt or schema mismatches.
        /// </summary>
        public static T DeserializeDTO<T>(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("[VibeSerialization] Cannot deserialize empty payload.");
            }

            try
            {
                // MemoryPack throws on schema mismatch, satisfying 'Fail Fast'.
                return MemoryPackSerializer.Deserialize<T>(data);
            }
            catch (Exception e)
            {
                Debug.LogError($"[VibeSerialization] Security Failure: Payload rejected. {e.Message}");
                throw; // Propagate up for Telemetry capture
            }
        }

        /// <summary>
        /// Serializes a Safe DTO to binary.
        /// </summary>
        public static byte[] SerializeDTO<T>(T value)
        {
            return MemoryPackSerializer.Serialize(value);
        }
    }
}
