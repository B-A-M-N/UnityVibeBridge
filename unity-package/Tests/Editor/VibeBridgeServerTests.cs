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

using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.Networking;
using System.Collections;
using System.Threading;

namespace VibeBridge.Tests
{
    public class VibeBridgeServerTests
    {
        private const string BASE_URL = "http://localhost:8085";

        [UnityTest]
        public IEnumerator Server_IsReachable_And_Healthy()
        {
            // 1. Sanity Check (Bypasses Main Thread)
            string sanityUrl = BASE_URL + "/sanity";
            using (UnityWebRequest www = UnityWebRequest.Get(sanityUrl))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Assert.Fail($"Sanity check failed: {www.error}");
                }
                
                string json = www.downloadHandler.text;
                Assert.IsTrue(json.Contains("sanity") || json.Contains("alive"), "Sanity response invalid: " + json);
                Assert.IsTrue(json.Contains("thread"), "Sanity response missing thread info");
            }
        }

        [UnityTest]
        public IEnumerator Server_CanAccess_MainThread()
        {
            // 2. Status Check (Requires Main Thread)
            string statusUrl = BASE_URL + "/status";
            using (UnityWebRequest www = UnityWebRequest.Get(statusUrl))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Assert.Fail($"Status check failed: {www.error}");
                }

                string json = www.downloadHandler.text;
                Assert.IsTrue(json.Contains("connected"), "Status response missing 'connected': " + json);
                Assert.IsTrue(json.Contains("isReadOnly"), "Status response missing isReadOnly");
            }
        }
    }
}
