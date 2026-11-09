#if UNIFIED_NETCODE && COM_UNITY_MODULES_PHYSICS
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Validates that an <see cref="INetworkPrefabInstanceHandler"/> pools hybrid instances on the server and on clients.
    /// </summary>
    [TestFixture(HostOrServer.UnifiedHost)]
    [TestFixture(HostOrServer.UnifiedServer)]
    internal class HybridPrefabHandlerPoolingTests : NetcodeIntegrationTest
    {
        private const int k_InstanceCount = 3;
        private const int k_SpawnCycles = 2;

        protected override int NumberOfClients => 1;

        private GameObject m_HybridPrefab;
        private PoolHandler m_AuthorityPool;
        private PoolHandler m_NonAuthorityPool;

        public HybridPrefabHandlerPoolingTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override bool UseUnifiedTests()
        {
            return true;
        }

        private class PoolHandler : INetworkPrefabInstanceHandler
        {
            public readonly List<NetworkObject> Created = new List<NetworkObject>();
            public int Reused;
            public int Returned;

            private readonly GameObject m_Prefab;
            private readonly Stack<NetworkObject> m_Available = new Stack<NetworkObject>();

            public PoolHandler(GameObject prefab)
            {
                m_Prefab = prefab;
            }

            public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
            {
                if (m_Available.Count > 0)
                {
                    var instance = m_Available.Pop();
                    instance.transform.SetPositionAndRotation(position, rotation);
                    instance.gameObject.SetActive(true);
                    Reused++;
                    return instance;
                }
                var created = Object.Instantiate(m_Prefab, position, rotation).GetComponent<NetworkObject>();
                Created.Add(created);
                return created;
            }

            public void Destroy(NetworkObject networkObject)
            {
                networkObject.gameObject.SetActive(false);
                m_Available.Push(networkObject);
                Returned++;
            }

            public void DestroyPooled()
            {
                while (m_Available.Count > 0)
                {
                    var instance = m_Available.Pop();
                    if (instance)
                    {
                        Object.Destroy(instance.gameObject);
                    }
                }
            }
        }

        protected override void OnServerAndClientsCreated()
        {
            m_HybridPrefab = CreateNetworkObjectPrefab("HybridPooled");
            m_HybridPrefab.AddComponent<NetworkTransform>();
            m_HybridPrefab.AddComponent<Rigidbody>().isKinematic = false;
            m_HybridPrefab.AddComponent<NetworkRigidbody>();

            m_AuthorityPool = new PoolHandler(m_HybridPrefab);
            m_NonAuthorityPool = new PoolHandler(m_HybridPrefab);
            m_ServerNetworkManager.PrefabHandler.AddHandler(m_HybridPrefab, m_AuthorityPool);
            m_ClientNetworkManagers[0].PrefabHandler.AddHandler(m_HybridPrefab, m_NonAuthorityPool);
            base.OnServerAndClientsCreated();
        }

        protected override IEnumerator OnTearDown()
        {
            // Without a handler, shutdown destroys what is still spawned instead of pooling it where nothing cleans it up.
            m_ServerNetworkManager.PrefabHandler.RemoveHandler(m_HybridPrefab);
            m_ClientNetworkManagers[0].PrefabHandler.RemoveHandler(m_HybridPrefab);
            m_AuthorityPool.DestroyPooled();
            m_NonAuthorityPool.DestroyPooled();
            yield return base.OnTearDown();
        }

        [UnityTest]
        public IEnumerator SpawnDespawnRespawnUsesPools()
        {
            var authority = GetAuthorityNetworkManager();
            var nonAuthority = GetNonAuthorityNetworkManager();
            var authorityInstances = new NetworkObject[k_InstanceCount];

            for (int cycle = 0; cycle < k_SpawnCycles; cycle++)
            {
                for (int i = 0; i < k_InstanceCount; i++)
                {
                    var instance = m_AuthorityPool.Instantiate(authority.LocalClientId, new Vector3(i * 2.0f, 0.0f, 0.0f), Quaternion.identity);
                    instance.NetworkManagerOwner = authority;
                    instance.Spawn();
                    authorityInstances[i] = instance;
                }
                foreach (var instance in authorityInstances)
                {
                    yield return WaitForSpawnedOnAllOrTimeOut(instance);
                    AssertOnTimeout($"[Cycle {cycle}] {instance.name} was not spawned on all instances!");
                }

                Assert.AreEqual(k_InstanceCount, m_NonAuthorityPool.Created.Count, $"[Cycle {cycle}] Client-{nonAuthority.LocalClientId} did not take its instances from its handler!");
                Assert.AreEqual(cycle * k_InstanceCount, m_NonAuthorityPool.Reused, $"[Cycle {cycle}] Client-{nonAuthority.LocalClientId} did not reuse its pooled instances!");
                foreach (var instance in authorityInstances)
                {
                    var clientInstance = nonAuthority.SpawnManager.SpawnedObjects[instance.NetworkObjectId];
                    Assert.IsTrue(m_NonAuthorityPool.Created.Contains(clientInstance), $"[Cycle {cycle}] Client-{nonAuthority.LocalClientId} spawned {clientInstance.name}, which its handler did not provide!");
                    Assert.IsFalse(instance.GetComponent<Rigidbody>().isKinematic, $"[Cycle {cycle}] {instance.name} is kinematic on the server!");
                    Assert.IsTrue(clientInstance.GetComponent<Rigidbody>().isKinematic, $"[Cycle {cycle}] {clientInstance.name} is not kinematic on Client-{nonAuthority.LocalClientId}!");
                }

                foreach (var instance in authorityInstances)
                {
                    instance.Despawn();
                }
                Assert.AreEqual((cycle + 1) * k_InstanceCount, m_AuthorityPool.Returned, $"[Cycle {cycle}] The server did not return its instances to its handler!");
                yield return WaitForConditionOrTimeOut(() => m_NonAuthorityPool.Returned == (cycle + 1) * k_InstanceCount);
                AssertOnTimeout($"[Cycle {cycle}] Client-{nonAuthority.LocalClientId} returned {m_NonAuthorityPool.Returned} instances to its handler!");
            }

            Assert.AreEqual(k_InstanceCount, m_AuthorityPool.Created.Count, "The server created instances instead of reusing its pooled ones!");
        }
    }
}
#endif
