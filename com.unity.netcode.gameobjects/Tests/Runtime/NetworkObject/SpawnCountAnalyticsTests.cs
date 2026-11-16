#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Validates the spawn counts the editor analytics reads from <see cref="NetworkSpawnManager"/>:<br />
    /// - Every peer counts each instance it spawns locally, whether it is the authority or received the spawn.<br />
    /// - Hybrid instances are also counted on their own.<br />
    /// </summary>
    [TestFixture(HostOrServer.Host)]
#if UNIFIED_NETCODE
    [TestFixture(HostOrServer.UnifiedHost)]
#endif
    internal class SpawnCountAnalyticsTests : NetcodeIntegrationTest
    {
        private const int k_DefaultInstances = 3;
        private const int k_NgoInstances = 2;

        protected override int NumberOfClients => 2;

        // Hybrid during a unified test pass.
        private GameObject m_DefaultPrefab;
        private GameObject m_NgoPrefab;

        public SpawnCountAnalyticsTests(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

#if UNIFIED_NETCODE
        protected override bool UseUnifiedTests()
        {
            return true;
        }
#endif

        protected override void OnServerAndClientsCreated()
        {
            m_DefaultPrefab = CreateNetworkObjectPrefab("SpawnCountDefault");
#if UNIFIED_NETCODE
            m_NgoPrefab = CreateNetworkObjectPrefab("SpawnCountNgo", false);
#else
            m_NgoPrefab = CreateNetworkObjectPrefab("SpawnCountNgo");
#endif
            base.OnServerAndClientsCreated();
        }

        [UnityTest]
        public IEnumerator SpawnsAreCountedOnEveryPeer()
        {
            // Player objects are already spawned, so only the increase is checked.
            var spawnCounts = new int[m_NetworkManagers.Length];
            var hybridSpawnCounts = new int[m_NetworkManagers.Length];
            for (int i = 0; i < m_NetworkManagers.Length; i++)
            {
                spawnCounts[i] = m_NetworkManagers[i].SpawnManager.SpawnCount;
                hybridSpawnCounts[i] = m_NetworkManagers[i].SpawnManager.HybridSpawnCount;
            }

            var authority = GetAuthorityNetworkManager();
            var instances = SpawnObjects(m_DefaultPrefab, authority, k_DefaultInstances);
            instances.AddRange(SpawnObjects(m_NgoPrefab, authority, k_NgoInstances));
            foreach (var instance in instances)
            {
                yield return WaitForSpawnedOnAllOrTimeOut(instance.GetComponent<NetworkObject>());
                AssertOnTimeout($"Failed to spawn {instance.name} on all clients!");
            }

            var expectedHybridSpawns = 0;
#if UNIFIED_NETCODE
            if (m_UseUnifiedTests)
            {
                expectedHybridSpawns = k_DefaultInstances;
            }
#endif
            for (int i = 0; i < m_NetworkManagers.Length; i++)
            {
                var spawnManager = m_NetworkManagers[i].SpawnManager;
                var clientId = m_NetworkManagers[i].LocalClientId;
                Assert.AreEqual(k_DefaultInstances + k_NgoInstances, spawnManager.SpawnCount - spawnCounts[i], $"Client-{clientId} counted the wrong number of spawns!");
                Assert.AreEqual(expectedHybridSpawns, spawnManager.HybridSpawnCount - hybridSpawnCounts[i], $"Client-{clientId} counted the wrong number of hybrid spawns!");
            }
        }
    }
}
#endif
