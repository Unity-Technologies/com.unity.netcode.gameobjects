using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;

namespace Unity.Netcode.RuntimeTests.Performance
{
    public class CreateDestroyPerformanceTests
    {
        [Test, Performance]
        public void Spawn8NetworkObjectPerformance()
        {
            var prefab = new GameObject("Object");
            var networkedPrefab = prefab.AddComponent<NetworkObject>();

            // Make it a prefab
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkedPrefab);

            // Create multiple NetworkManager instances
            if (!MultiInstanceHelpers.Create(8, out NetworkManager server, out NetworkManager[] clients, 60))
            {
                Assert.Fail("Failed to create instances");
            }

            server.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab()
            {
                Prefab = prefab
            });

            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab()
                {
                    Prefab = prefab
                });
            }

            Assert.That(MultiInstanceHelpers.Start(true, server, clients));

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
                .WarmupCount(5)
                .MeasurementCount(10)
                .Run();

            MultiInstanceHelpers.Destroy();
        }

        [Test, Performance]
        public void Spawn16NetworkObjectPerformance()
        {
            var prefab = new GameObject("Object");
            var networkedPrefab = prefab.AddComponent<NetworkObject>();

            // Make it a prefab
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkedPrefab);

            // Create multiple NetworkManager instances
            if (!MultiInstanceHelpers.Create(16, out NetworkManager server, out NetworkManager[] clients, 60))
            {
                Assert.Fail("Failed to create instances");
            }

            server.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab()
            {
                Prefab = prefab
            });

            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab()
                {
                    Prefab = prefab
                });
            }

            Assert.That(MultiInstanceHelpers.Start(true, server, clients));

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
                .WarmupCount(5)
                .MeasurementCount(10)
                .Run();

            MultiInstanceHelpers.Destroy();
        }

        [Test, Performance]
        public void Spawn32NetworkObjectPerformance()
        {
            var prefab = new GameObject("Object");
            var networkedPrefab = prefab.AddComponent<NetworkObject>();

            // Make it a prefab
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkedPrefab);

            // Create multiple NetworkManager instances
            if (!MultiInstanceHelpers.Create(32, out NetworkManager server, out NetworkManager[] clients, 60))
            {
                Assert.Fail("Failed to create instances");
            }

            server.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab()
            {
                Prefab = prefab
            });

            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab()
                {
                    Prefab = prefab
                });
            }

            Assert.That(MultiInstanceHelpers.Start(true, server, clients));

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
                .WarmupCount(5)
                .MeasurementCount(10)
                .Run();

            MultiInstanceHelpers.Destroy();
        }

        [Test, Performance]
        public void Spawn128NetworkObjectPerformance()
        {
            var prefab = new GameObject("Object");
            var networkedPrefab = prefab.AddComponent<NetworkObject>();

            // Make it a prefab
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkedPrefab);

            // Create multiple NetworkManager instances
            if (!MultiInstanceHelpers.Create(128, out NetworkManager server, out NetworkManager[] clients, 60))
            {
                Assert.Fail("Failed to create instances");
            }

            server.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab()
            {
                Prefab = prefab
            });

            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab()
                {
                    Prefab = prefab
                });
            }

            Assert.That(MultiInstanceHelpers.Start(true, server, clients));

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
                .WarmupCount(5)
                .MeasurementCount(10)
                .Run();

            MultiInstanceHelpers.Destroy();
        }
    }
}
