using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TestProject.RuntimeTests
{
    [TestFixture(UseSceneManagement.SceneManagementDisabled)]
    [TestFixture(UseSceneManagement.SceneManagementEnabled)]
    public class NetworkManagerTests : NetcodeIntegrationTest
    {
        private const string k_SceneToLoad = "InSceneNetworkObject";
        protected override int NumberOfClients => 0;

        public enum UseSceneManagement
        {
            SceneManagementEnabled,
            SceneManagementDisabled
        }

        private bool m_EnableSceneManagement;
        private NetworkObject m_NetworkObject;
        private bool m_NetworkObjectWasSpawned;
        private bool m_NetworkBehaviourIsHostWasSet;
        private bool m_NetworkBehaviourIsClientWasSet;
        private bool m_NetworkBehaviourIsServerWasSet;
        private int m_NumberOfTimesInvoked;
        private AsyncOperation m_AsyncOperation;
        private NetworkObjectTestComponent m_NetworkObjectTestComponent;

        private bool m_UseSceneManagement;

        public NetworkManagerTests(UseSceneManagement useSceneManagement)
        {
            m_UseSceneManagement = useSceneManagement == UseSceneManagement.SceneManagementEnabled;
        }

        private void OnClientConnectedCallback(NetworkObject networkObject, int numberOfTimesInvoked, bool isHost, bool isClient, bool isServer)
        {
            m_NetworkObject = networkObject;
            m_NetworkObjectWasSpawned = networkObject.IsSpawned;
            m_NetworkBehaviourIsHostWasSet = isHost;
            m_NetworkBehaviourIsClientWasSet = isClient;
            m_NetworkBehaviourIsServerWasSet = isServer;
            m_NumberOfTimesInvoked = numberOfTimesInvoked;
        }

        private bool TestComponentFound()
        {
            if (!m_AsyncOperation.isDone)
            {
                return false;
            }
#if UNITY_2023_1_OR_NEWER
            m_NetworkObjectTestComponent = Object.FindFirstObjectByType<NetworkObjectTestComponent>();
#else
            m_NetworkObjectTestComponent = Object.FindObjectOfType<NetworkObjectTestComponent>();
#endif
            if (m_NetworkObjectTestComponent == null)
            {
                return false;
            }
            return true;
        }

        protected override IEnumerator OnSetup()
        {
            m_AsyncOperation = SceneManager.LoadSceneAsync(k_SceneToLoad, LoadSceneMode.Additive);
            yield return WaitForConditionOrTimeOut(TestComponentFound);
            AssertOnTimeout($"Failed to find {nameof(NetworkObjectTestComponent)} after loading test scene {k_SceneToLoad}");
        }

        protected override IEnumerator OnTearDown()
        {
            SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName(k_SceneToLoad));
            yield return s_DefaultWaitForTick;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_ServerNetworkManager.NetworkConfig.EnableSceneManagement = m_EnableSceneManagement;
            m_NetworkObjectTestComponent.ConfigureClientConnected(m_ServerNetworkManager, OnClientConnectedCallback);
        }

        [Test]
        public void ValidateHostSettings()
        {
            Assert.IsTrue(m_ServerNetworkManager.LocalClient != null);
            Assert.IsTrue(m_NetworkObjectWasSpawned, $"{m_NetworkObject.name} was not spawned when OnClientConnectedCallback was invoked!");
            Assert.IsTrue(m_NetworkBehaviourIsHostWasSet, $"IsHost was not true when OnClientConnectedCallback was invoked!");
            Assert.IsTrue(m_NetworkBehaviourIsClientWasSet, $"IsClient was not true when OnClientConnectedCallback was invoked!");
            Assert.IsTrue(m_NumberOfTimesInvoked == 1, $"OnClientConnectedCallback was invoked {m_NumberOfTimesInvoked} as opposed to just once!");
            Assert.IsTrue(m_NetworkBehaviourIsServerWasSet, $"IsServer was not true when OnClientConnectedCallback was invoked!");
        }
    }
}
