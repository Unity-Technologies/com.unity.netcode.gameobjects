using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using System.Collections;

namespace TestProject.RuntimeTests
{
    public class NetworkManagerTests : NetcodeIntegrationTest
    {
        private const string k_SceneToLoad = "InSceneNetworkObject";
        protected override int NumberOfClients => 0;

        private NetworkObject m_NetworkObject;
        private bool m_NetworkObjectWasSpawned;
        private bool m_NetworkBehaviourIsHostWasSet;
        private bool m_NetworkBehaviourIsClientWasSet;
        private bool m_NetworkBehaviourIsServerWasSet;
        private AsyncOperation m_AsyncOperation;
        private NetworkObjectTestComponent m_NetworkObjectTestComponent;

        private void OnClientConnectedCallback(NetworkObject networkObject, bool isHost, bool isClient, bool isServer)
        {
            m_NetworkObject = networkObject;
            m_NetworkObjectWasSpawned = networkObject.IsSpawned;
            m_NetworkBehaviourIsHostWasSet = isHost;
            m_NetworkBehaviourIsClientWasSet = isClient;
            m_NetworkBehaviourIsServerWasSet = isServer;
        }

        private bool TestComponentFound()
        {
            if (!m_AsyncOperation.isDone)
            {
                return false;
            }

            m_NetworkObjectTestComponent = Object.FindObjectOfType<NetworkObjectTestComponent>();
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

        protected override void OnServerAndClientsCreated()
        {
            m_NetworkObjectTestComponent.ConfigureClientConnected(m_ServerNetworkManager, OnClientConnectedCallback);
            base.OnServerAndClientsCreated();
        }

        [Test]
        public void ValidateHostSettings()
        {
            Assert.IsTrue(m_ServerNetworkManager.LocalClient != null);
            Assert.IsTrue(m_NetworkObjectWasSpawned, $"{m_NetworkObject.name} was not spawned when OnClientConnectedCallback was invoked!");
            Assert.IsTrue(m_NetworkBehaviourIsHostWasSet, $"IsHost was not true when OnClientConnectedCallback was invoked!");
            Assert.IsTrue(m_NetworkBehaviourIsClientWasSet, $"IsClient was not true when OnClientConnectedCallback was invoked!");
            Assert.IsTrue(m_NetworkBehaviourIsServerWasSet, $"IsServer was not true when OnClientConnectedCallback was invoked!");
        }
    }
}
