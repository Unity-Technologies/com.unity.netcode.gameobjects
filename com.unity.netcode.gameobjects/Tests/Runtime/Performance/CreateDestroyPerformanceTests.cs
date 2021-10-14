using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;

namespace Unity.Netcode.RuntimeTests.Performance
{
    public class CreateDestroyPerformanceTests
    {
        [Test, Performance]
        public void SpawnNetworkObjectPerformance()
        {
            // Create multiple NetworkManager instances
            if (!MultiInstanceHelpers.Create(4, out NetworkManager server, out NetworkManager[] clients, 60))
            {
                Debug.LogError("Failed to create instances");
                Assert.Fail("Failed to create instances");
            }

            Assert.That(MultiInstanceHelpers.Start(true, server, clients));

            var prefab = new GameObject("Object");
            var networkedPrefab = prefab.AddComponent<NetworkObject>();

            // Make it a prefab
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkedPrefab);

            NetworkObject no = null;

            Measure.Method(() =>
                {
                    no.Spawn();
                })
                .SetUp(() =>
                {
                    var go = Object.Instantiate(prefab);
                    no = go.GetComponent<NetworkObject>();
                    no.NetworkManagerOwner = server;
                })
                .CleanUp(() =>
                {
                    Object.Destroy(no.gameObject);
                })
                .GC()
                .WarmupCount(20)
                .MeasurementCount(50)
                .Run();

            MultiInstanceHelpers.Destroy();
        }
    }
}
