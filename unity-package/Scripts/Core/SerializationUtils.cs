#if UNITY_EDITOR
using System;
using UnityEngine;

#if VIBE_MEMORYPACK
using MemoryPack;
#endif

namespace UnityVibeBridge.Kernel.Core
{
    /// <summary>
    /// Authorized Bridge Serializer.
    /// Neutralized while waiting for UPM resolution.
    /// </summary>
    public static class SerializationUtils
    {
        public static T DeserializeDTO<T>(byte[] data)
        {
            if (data == null || data.Length == 0) throw new ArgumentException("Empty payload");
            string json = System.Text.Encoding.UTF8.GetString(data);
            return JsonUtility.FromJson<T>(json);
        }

        public static byte[] SerializeDTO<T>(T value)
        {
            if (value == null) return null;
            string json = JsonUtility.ToJson(value);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }
    }
}
#endif
