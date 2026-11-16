#if UNIFIED_NETCODE
using System;
using System.Collections;
using NUnit.Framework;
using Unity.Netcode.Components;
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
        private GameObject m_AttachablePrefab;
        private GameObject m_AttachableNodePrefab;
        private ulong m_NetworkObjectId;

        public HybridNetworkShowHideTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override bool UseUnifiedTests()
        {
            return true;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_HybridPrefab = CreateNetworkObjectPrefab("HybridShowHide");
            m_AttachablePrefab = CreateNetworkObjectPrefab("HybridAttachable");
            var attachableChild = new GameObject("AttachableChild");
            attachableChild.transform.parent = m_AttachablePrefab.transform;
            attachableChild.AddComponent<AttachableBehaviour>();
            m_AttachableNodePrefab = CreateNetworkObjectPrefab("HybridAttachableNode");
            var nodeChild = new GameObject("NodeChild");
            nodeChild.transform.parent = m_AttachableNodePrefab.transform;
            nodeChild.AddComponent<AttachableNode>();
            base.OnServerAndClientsCreated();
        }

        private bool IsSpawnedOnClient()
        {
            return GetNonAuthorityNetworkManager().SpawnManager.SpawnedObjects.ContainsKey(m_NetworkObjectId);
        }

        private IEnumerator SpawnOnAll(GameObject prefab, Action<NetworkObject> onSpawned)
        {
            var authorityInstance = SpawnObject(prefab, GetAuthorityNetworkManager()).GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(authorityInstance);
            AssertOnTimeout($"[Setup] {authorityInstance.name} was not spawned on all instances!");
            onSpawned(authorityInstance);
        }

        private IEnumerator HideThenShow(NetworkObject authorityInstance, string context)
        {
            var nonAuthority = GetNonAuthorityNetworkManager();
            m_NetworkObjectId = authorityInstance.NetworkObjectId;
            authorityInstance.NetworkHide(nonAuthority.LocalClientId);
            yield return WaitForConditionOrTimeOut(() => !IsSpawnedOnClient());
            AssertOnTimeout($"[{context}] {authorityInstance.name} was not despawned on Client-{nonAuthority.LocalClientId}!");

            authorityInstance.NetworkShow(nonAuthority.LocalClientId);
            yield return WaitForConditionOrTimeOut(IsSpawnedOnClient);
            AssertOnTimeout($"[{context}] {authorityInstance.name} was not respawned on Client-{nonAuthority.LocalClientId}!");
        }

        [UnityTest]
        public IEnumerator HideThenShowRespawnsOnClient()
        {
            var nonAuthority = GetNonAuthorityNetworkManager();
            NetworkObject authorityInstance = null;
            yield return SpawnOnAll(m_HybridPrefab, spawned => authorityInstance = spawned);
            var clientInstance = nonAuthority.SpawnManager.SpawnedObjects[authorityInstance.NetworkObjectId];

            // A second cycle shows the respawned instance can be hidden and shown again.
            for (int i = 0; i < 2; i++)
            {
                yield return HideThenShow(authorityInstance, $"Cycle {i}");
                Assert.AreSame(clientInstance, nonAuthority.SpawnManager.SpawnedObjects[m_NetworkObjectId], $"[Cycle {i}] Client-{nonAuthority.LocalClientId} did not respawn the instance its ghost kept alive!");
            }
        }

        [UnityTest]
        public IEnumerator HideThenShowParentedChild()
        {
            var nonAuthority = GetNonAuthorityNetworkManager();
            NetworkObject parent = null;
            NetworkObject child = null;
            yield return SpawnOnAll(m_HybridPrefab, spawned => parent = spawned);
            yield return SpawnOnAll(m_HybridPrefab, spawned => child = spawned);
            var clientParent = nonAuthority.SpawnManager.SpawnedObjects[parent.NetworkObjectId].transform;
            var clientChild = nonAuthority.SpawnManager.SpawnedObjects[child.NetworkObjectId].transform;
            Assert.IsTrue(child.TrySetParent(parent, false), $"{nameof(NetworkObject.TrySetParent)} failed!");
            yield return WaitForConditionOrTimeOut(() => clientChild.parent == clientParent);
            AssertOnTimeout($"[Setup] {child.name} was not parented on Client-{nonAuthority.LocalClientId}!");

            yield return HideThenShow(child, "Parented");
            Assert.AreSame(clientParent, clientChild.parent, $"Client-{nonAuthority.LocalClientId} did not keep {child.name} parented!");
        }

        [UnityTest]
        public IEnumerator HideThenShowThenAttachAndDetach()
        {
            var nonAuthority = GetNonAuthorityNetworkManager();
            NetworkObject source = null;
            NetworkObject node = null;
            yield return SpawnOnAll(m_AttachablePrefab, spawned => source = spawned);
            yield return SpawnOnAll(m_AttachableNodePrefab, spawned => node = spawned);
            yield return HideThenShow(source, "Attachable");

            var clientSource = nonAuthority.SpawnManager.SpawnedObjects[source.NetworkObjectId].transform;
            var clientAttachable = clientSource.GetComponentInChildren<AttachableBehaviour>().transform;
            var clientNode = nonAuthority.SpawnManager.SpawnedObjects[node.NetworkObjectId].GetComponentInChildren<AttachableNode>().transform;
            var attachable = source.GetComponentInChildren<AttachableBehaviour>();

            attachable.Attach(node.GetComponentInChildren<AttachableNode>());
            yield return WaitForConditionOrTimeOut(() => clientAttachable.parent == clientNode);
            AssertOnTimeout($"Client-{nonAuthority.LocalClientId} did not attach {clientAttachable.name}!");

            attachable.Detach();
            yield return WaitForConditionOrTimeOut(() => clientAttachable.parent == clientSource);
            AssertOnTimeout($"Client-{nonAuthority.LocalClientId} did not detach {clientAttachable.name}!");
        }

        [UnityTest]
        public IEnumerator DespawnWhileHiddenReleasesClientInstance()
        {
            var nonAuthority = GetNonAuthorityNetworkManager();
            NetworkObject authorityInstance = null;
            yield return SpawnOnAll(m_HybridPrefab, spawned => authorityInstance = spawned);
            m_NetworkObjectId = authorityInstance.NetworkObjectId;
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
