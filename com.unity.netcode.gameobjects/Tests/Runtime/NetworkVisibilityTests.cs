using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(SceneManagementState.SceneManagementEnabled)]
    [TestFixture(SceneManagementState.SceneManagementDisabled)]
    public class NetworkVisibilityTests : NetcodeIntegrationTest
    {
        public enum SceneManagementState
        {
            SceneManagementEnabled,
            SceneManagementDisabled
        }
        protected override int NumberOfClients => 1;
        private GameObject m_TestNetworkPrefab;
        private bool m_SceneManagementEnabled;

        private GameObject m_SpawnedObject;

        public NetworkVisibilityTests(SceneManagementState sceneManagementState)
        {
            m_SceneManagementEnabled = sceneManagementState == SceneManagementState.SceneManagementEnabled;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_TestNetworkPrefab = CreateNetworkObjectPrefab("Object");
            m_TestNetworkPrefab.AddComponent<NetworkVisibilityComponent>();
            m_ServerNetworkManager.NetworkConfig.EnableSceneManagement = m_SceneManagementEnabled;
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                clientNetworkManager.NetworkConfig.EnableSceneManagement = m_SceneManagementEnabled;
            }
            base.OnServerAndClientsCreated();
        }


        protected override IEnumerator OnServerAndClientsConnected()
        {
            m_SpawnedObject = SpawnObject(m_TestNetworkPrefab, m_ServerNetworkManager);

            yield return base.OnServerAndClientsConnected();
        }

        [UnityTest]
        public IEnumerator HiddenObjectsTest()
        {
#if UNITY_2023_1_OR_NEWER
            yield return WaitForConditionOrTimeOut(() => Object.FindObjectsByType<NetworkVisibilityComponent>(FindObjectsSortMode.None).Where((c) => c.IsSpawned).Count() == 2);
#else
            yield return WaitForConditionOrTimeOut(() => Object.FindObjectsOfType<NetworkVisibilityComponent>().Where((c) => c.IsSpawned).Count() == 2);
#endif

            AssertOnTimeout("Timed out waiting for the visible object count to equal 2!");
        }


        [UnityTest]
        public IEnumerator HideShowAndDeleteTest()
        {
#if UNITY_2023_1_OR_NEWER
            yield return WaitForConditionOrTimeOut(() => Object.FindObjectsByType<NetworkVisibilityComponent>(FindObjectsSortMode.None).Where((c) => c.IsSpawned).Count() == 2);
#else
            yield return WaitForConditionOrTimeOut(() => Object.FindObjectsOfType<NetworkVisibilityComponent>().Where((c) => c.IsSpawned).Count() == 2);
#endif
            AssertOnTimeout("Timed out waiting for the visible object count to equal 2!");

            var serverNetworkObject = m_SpawnedObject.GetComponent<NetworkObject>();

            serverNetworkObject.NetworkHide(m_ClientNetworkManagers[0].LocalClientId);

#if UNITY_2023_1_OR_NEWER
            yield return WaitForConditionOrTimeOut(() => Object.FindObjectsByType<NetworkVisibilityComponent>(FindObjectsSortMode.None).Where((c) => c.IsSpawned).Count() == 1);
#else
            yield return WaitForConditionOrTimeOut(() => Object.FindObjectsOfType<NetworkVisibilityComponent>().Where((c) => c.IsSpawned).Count() == 1);
#endif
            AssertOnTimeout($"Timed out waiting for {m_SpawnedObject.name} to be hidden from client!");
            var networkObjectId = serverNetworkObject.NetworkObjectId;
            serverNetworkObject.NetworkShow(m_ClientNetworkManagers[0].LocalClientId);
            serverNetworkObject.Despawn(true);

            // Expect no exceptions
            yield return s_DefaultWaitForTick;

            // Now force a scenario where it normally would have caused an exception
            m_ServerNetworkManager.SpawnManager.ObjectsToShowToClient.Add(m_ClientNetworkManagers[0].LocalClientId, new System.Collections.Generic.List<NetworkObject>());
            m_ServerNetworkManager.SpawnManager.ObjectsToShowToClient[m_ClientNetworkManagers[0].LocalClientId].Add(null);

            // Expect no exceptions
            yield return s_DefaultWaitForTick;
        }
    }
}
