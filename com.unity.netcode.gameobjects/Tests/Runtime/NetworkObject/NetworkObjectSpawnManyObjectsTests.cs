using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace Unity.Netcode.RuntimeTests
{

    [TestFixture(NetworkTopologyTypes.ClientServer)]
    [TestFixture(NetworkTopologyTypes.DistributedAuthority)]
    internal class NetworkObjectSpawnManyObjectsTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;
        private const int k_SpawnedObjects = 1500;

        private NetworkPrefab m_PrefabToSpawn;

        public NetworkObjectSpawnManyObjectsTests(NetworkTopologyTypes topology) : base(topology)
        {
        }

        private static void ResetTracking()
        {
            SpawnObjectTrackingComponent.SpawnedObjects = 0;
            SpawnObjectTrackingComponent.DespawnedObjects = 0;
        }

        // Using this component assures we will know precisely how many prefabs were spawned on the client
        internal class SpawnObjectTrackingComponent : NetworkBehaviour
        {
            public static int SpawnedObjects;
            public static int DespawnedObjects;

            public override void OnNetworkSpawn()
            {
                if (!IsOwner)
                {
                    SpawnedObjects++;
                }
            }

            public override void OnNetworkDespawn()
            {
                if (!IsOwner)
                {
                    DespawnedObjects++;
                }
            }
        }

        protected override void OnServerAndClientsCreated()
        {
            ResetTracking();
            // create prefab
            var gameObject = new GameObject("TestObject");
            var networkObject = gameObject.AddComponent<NetworkObject>();
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);
            networkObject.IsSceneObject = false;
            gameObject.AddComponent<SpawnObjectTrackingComponent>();

            m_PrefabToSpawn = new NetworkPrefab() { Prefab = gameObject };

            foreach (var client in m_NetworkManagers)
            {
                client.NetworkConfig.RecycleNetworkIds = true;
                client.NetworkConfig.NetworkIdRecycleDelay = 0.01f;
                client.NetworkConfig.Prefabs.Add(m_PrefabToSpawn);
            }
        }

        [UnityTest]
        public IEnumerator WhenManyObjectsAreSpawnedAtOnce_AllAreReceived()
        {
            var authority = GetAuthorityNetworkManager();
            for (int x = 0; x < k_SpawnedObjects; x++)
            {
                NetworkObject serverObject = Object.Instantiate(m_PrefabToSpawn.Prefab).GetComponent<NetworkObject>();
                serverObject.NetworkManagerOwner = authority;
                serverObject.SpawnWithObservers = true;
                serverObject.Spawn();
            }

            var timeoutHelper = new TimeoutHelper(30);
            // ensure all objects are replicated
            yield return WaitForConditionOrTimeOut(() => SpawnObjectTrackingComponent.SpawnedObjects == k_SpawnedObjects, timeoutHelper);
            AssertOnTimeout($"Timed out waiting for the client to spawn {k_SpawnedObjects} objects! expected: {k_SpawnedObjects} | have: {SpawnObjectTrackingComponent.SpawnedObjects}", timeoutHelper);

            // Provide one full tick for all messages to finish being processed.
            // DANGO-TODO: Determine if this is only when testing against Rust server (i.e. messages still pending and clients shutting down before they are dequeued)
            // yield return s_DefaultWaitForTick;
        }

        [UnityTest, Performance]
        public IEnumerator PerformanceWhenManyObjectsAreSpawnedAndDespawned()
        {
            var authority = GetAuthorityNetworkManager();
            SampleGroup sampleGroup = new SampleGroup($"ManyObjectsSpawnedAndDespawned");
            for (var i = 0; i < 100; i++)
            {
                ResetTracking();
                var objects = new NetworkObject[k_SpawnedObjects];

                Stopwatch stopwatch = Stopwatch.StartNew();
                for (int x = 0; x < k_SpawnedObjects; x++)
                {
                    NetworkObject serverObject = Object.Instantiate(m_PrefabToSpawn.Prefab).GetComponent<NetworkObject>();
                    serverObject.NetworkManagerOwner = authority;
                    serverObject.SpawnWithObservers = true;
                    serverObject.Spawn();
                    objects[x] = serverObject;

                    if (x % 100 == 0)
                    {
                        yield return null;
                    }
                }

                for (int x = 0; x < k_SpawnedObjects; x++)
                {
                    var serverObject = objects[x];
                    serverObject.Despawn();

                    if (x % 100 == 0)
                    {
                        yield return null;
                    }
                }

                // Provide plenty of time to spawn all objects in case the CI VM is running slow
                var timeoutHelper = new TimeoutHelper(30);
                // ensure all objects are replicated
                yield return WaitForConditionOrTimeOut(() => SpawnObjectTrackingComponent.SpawnedObjects == k_SpawnedObjects, timeoutHelper);
                AssertOnTimeout($"Timed out waiting for the client to spawn {k_SpawnedObjects} objects! expected: {k_SpawnedObjects} | have: {SpawnObjectTrackingComponent.SpawnedObjects}", timeoutHelper);

                yield return WaitForConditionOrTimeOut(() => SpawnObjectTrackingComponent.DespawnedObjects == k_SpawnedObjects, timeoutHelper);
                AssertOnTimeout($"Timed out waiting for the client to despawn {k_SpawnedObjects} objects! expected: {k_SpawnedObjects} | have: {SpawnObjectTrackingComponent.DespawnedObjects}", timeoutHelper);
                stopwatch.Stop();
                Measure.Custom(sampleGroup, stopwatch.ElapsedMilliseconds);
                yield return null;
            }

            yield return s_DefaultWaitForTick;
        }
    }
}
