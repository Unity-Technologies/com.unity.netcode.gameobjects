#if UNIFIED_NETCODE
using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Validates that hiding and then showing a spawned hybrid instance respawns it on the client.
    /// </summary>
    [TestFixture(HostOrServer.UnifiedHost)]
    [TestFixture(HostOrServer.UnifiedServer)]
    internal class HybridNetworkShowHideTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private GameObject m_HybridPrefab;
        private ulong m_NetworkObjectId;

        public HybridNetworkShowHideTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override bool UseUnifiedTests()
        {
            return true;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_HybridPrefab = CreateNetworkObjectPrefab("HybridShowHide");
            base.OnServerAndClientsCreated();
        }

        private bool IsSpawnedOnClient()
        {
            return GetNonAuthorityNetworkManager().SpawnManager.SpawnedObjects.ContainsKey(m_NetworkObjectId);
        }

        [UnityTest]
        public IEnumerator HideThenShowRespawnsOnClient()
        {
            var nonAuthority = GetNonAuthorityNetworkManager();
            var authorityInstance = SpawnObject(m_HybridPrefab, GetAuthorityNetworkManager()).GetComponent<NetworkObject>();
            m_NetworkObjectId = authorityInstance.NetworkObjectId;
            yield return WaitForSpawnedOnAllOrTimeOut(authorityInstance);
            AssertOnTimeout($"[Setup] {authorityInstance.name} was not spawned on all instances!");
            var clientInstance = nonAuthority.SpawnManager.SpawnedObjects[m_NetworkObjectId];

            // A second cycle shows the respawned instance can be hidden and shown again.
            for (int i = 0; i < 2; i++)
            {
                authorityInstance.NetworkHide(nonAuthority.LocalClientId);
                yield return WaitForConditionOrTimeOut(() => !IsSpawnedOnClient());
                AssertOnTimeout($"[Cycle {i}] {authorityInstance.name} was not despawned on Client-{nonAuthority.LocalClientId}!");

                authorityInstance.NetworkShow(nonAuthority.LocalClientId);
                yield return WaitForConditionOrTimeOut(IsSpawnedOnClient);
                AssertOnTimeout($"[Cycle {i}] {authorityInstance.name} was not respawned on Client-{nonAuthority.LocalClientId}!");
                Assert.AreSame(clientInstance, nonAuthority.SpawnManager.SpawnedObjects[m_NetworkObjectId], $"[Cycle {i}] Client-{nonAuthority.LocalClientId} did not respawn the instance its ghost kept alive!");
            }
        }

        [UnityTest]
        public IEnumerator DespawnWhileHiddenReleasesClientInstance()
        {
            var nonAuthority = GetNonAuthorityNetworkManager();
            var authorityInstance = SpawnObject(m_HybridPrefab, GetAuthorityNetworkManager()).GetComponent<NetworkObject>();
            m_NetworkObjectId = authorityInstance.NetworkObjectId;
            yield return WaitForSpawnedOnAllOrTimeOut(authorityInstance);
            AssertOnTimeout($"[Setup] {authorityInstance.name} was not spawned on all instances!");
            var clientInstance = nonAuthority.SpawnManager.SpawnedObjects[m_NetworkObjectId];

            authorityInstance.NetworkHide(nonAuthority.LocalClientId);
            yield return WaitForConditionOrTimeOut(() => !IsSpawnedOnClient());
            AssertOnTimeout($"[Setup] {authorityInstance.name} was not despawned on Client-{nonAuthority.LocalClientId}!");
            Assert.IsTrue(nonAuthority.SpawnManager.GhostSpawnManager.IsGhostPendingSpawn(m_NetworkObjectId), $"Client-{nonAuthority.LocalClientId} is not holding the hidden instance for a later show!");

            authorityInstance.Despawn();
            yield return WaitForConditionOrTimeOut(() => clientInstance == null);
            AssertOnTimeout($"N4E did not destroy the hidden instance on Client-{nonAuthority.LocalClientId}!");
            Assert.IsFalse(nonAuthority.SpawnManager.GhostSpawnManager.IsGhostPendingSpawn(m_NetworkObjectId), $"Client-{nonAuthority.LocalClientId} still holds the destroyed instance as pending!");
        }
    }
}
#endif
