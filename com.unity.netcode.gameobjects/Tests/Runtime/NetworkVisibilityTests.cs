using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(SceneManagementState.SceneManagementEnabled, NetworkTopologyTypes.DistributedAuthority)]
    [TestFixture(SceneManagementState.SceneManagementDisabled, NetworkTopologyTypes.DistributedAuthority)]
    [TestFixture(SceneManagementState.SceneManagementEnabled, NetworkTopologyTypes.ClientServer)]
    [TestFixture(SceneManagementState.SceneManagementDisabled, NetworkTopologyTypes.ClientServer)]
    internal class NetworkVisibilityTests : NetcodeIntegrationTest
    {

        protected override int NumberOfClients => 2;
        private GameObject m_TestNetworkPrefab;
        private bool m_SceneManagementEnabled;
        private GameObject m_SpawnedObject;
        private NetworkManager m_SessionOwner;

        public NetworkVisibilityTests(SceneManagementState sceneManagementState, NetworkTopologyTypes networkTopologyType) : base(networkTopologyType)
        {
            m_SceneManagementEnabled = sceneManagementState == SceneManagementState.SceneManagementEnabled;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_TestNetworkPrefab = CreateNetworkObjectPrefab("Object");
            m_TestNetworkPrefab.AddComponent<NetworkVisibilityComponent>();

            foreach (var manager in m_NetworkManagers)
            {
                manager.NetworkConfig.EnableSceneManagement = m_SceneManagementEnabled;
            }
            base.OnServerAndClientsCreated();
        }


        protected override IEnumerator OnServerAndClientsConnected()
        {
            m_SessionOwner = GetAuthorityNetworkManager();
            m_SpawnedObject = SpawnObject(m_TestNetworkPrefab, m_SessionOwner);

            yield return base.OnServerAndClientsConnected();
        }

        [UnityTest]
        public IEnumerator HiddenObjectsTest()
        {
            yield return WaitForConditionOrTimeOut(() => Object.FindObjectsByType<NetworkVisibilityComponent>(FindObjectsSortMode.None).Where((c) => c.IsSpawned).Count() == TotalClients);
            AssertOnTimeout($"Timed out waiting for the visible object count to equal {TotalClients}!Actual count {Object.FindObjectsByType<NetworkVisibilityComponent>(FindObjectsSortMode.None).Count(c => c.IsSpawned)}");
        }

        [UnityTest]
        public IEnumerator HideShowAndDeleteTest()
        {
            yield return WaitForConditionOrTimeOut(() => Object.FindObjectsByType<NetworkVisibilityComponent>(FindObjectsSortMode.None).Count(c => c.IsSpawned) == TotalClients);

            AssertOnTimeout($"Timed out waiting for the visible object count to equal {TotalClients}! Actual count {Object.FindObjectsByType<NetworkVisibilityComponent>(FindObjectsSortMode.None).Count(c => c.IsSpawned)}");

            var sessionOwnerNetworkObject = m_SpawnedObject.GetComponent<NetworkObject>();
            var nonAuthority = GetNonAuthorityNetworkManager();
            sessionOwnerNetworkObject.NetworkHide(nonAuthority.LocalClientId);
            yield return WaitForConditionOrTimeOut(() => Object.FindObjectsByType<NetworkVisibilityComponent>(FindObjectsSortMode.None).Where((c) => c.IsSpawned).Count() == TotalClients - 1);
            AssertOnTimeout($"Timed out waiting for {m_SpawnedObject.name} to be hidden from client!");
            var networkObjectId = sessionOwnerNetworkObject.NetworkObjectId;
            sessionOwnerNetworkObject.NetworkShow(nonAuthority.LocalClientId);
            sessionOwnerNetworkObject.Despawn(true);

            // Expect no exceptions while waiting to show the object and wait for the client id to be removed
            yield return WaitForConditionOrTimeOut(() => !m_SessionOwner.SpawnManager.ObjectsToShowToClient.ContainsKey(nonAuthority.LocalClientId));
            AssertOnTimeout($"Timed out waiting for client-{nonAuthority.LocalClientId} to be removed from the {nameof(NetworkSpawnManager.ObjectsToShowToClient)} table!");

            // Now force a scenario where it normally would have caused an exception
            m_SessionOwner.SpawnManager.ObjectsToShowToClient.Add(nonAuthority.LocalClientId, new System.Collections.Generic.List<NetworkObject>());
            m_SessionOwner.SpawnManager.ObjectsToShowToClient[nonAuthority.LocalClientId].Add(null);

            // Expect no exceptions
            yield return s_DefaultWaitForTick;
        }
    }
}
