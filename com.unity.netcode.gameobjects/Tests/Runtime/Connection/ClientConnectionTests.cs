using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
    internal class ClientConnectionTests : IntegrationTestWithApproximation
    {
        protected override int NumberOfClients => 3;
        private readonly bool m_SceneManagementEnabled;
        private HashSet<ulong> m_ServerCallbackCalled = new HashSet<ulong>();
        private HashSet<ulong> m_ClientCallbackCalled = new HashSet<ulong>();

        // TODO: [CmbServiceTests] Enable once the cmb service sceneManagementEnabled OnClientConnected flow is fixed
        protected override bool UseCMBService()
        {
            return false;
        }

        public ClientConnectionTests(SceneManagementState sceneManagementState, NetworkTopologyTypes networkTopologyType) : base(networkTopologyType)
        {
            m_SceneManagementEnabled = sceneManagementState == SceneManagementState.SceneManagementEnabled;
        }

        protected override IEnumerator OnSetup()
        {
            m_ServerCallbackCalled.Clear();
            m_ClientCallbackCalled.Clear();
            return base.OnSetup();
        }
        protected override void OnServerAndClientsCreated()
        {
            m_ServerNetworkManager.NetworkConfig.EnableSceneManagement = m_SceneManagementEnabled;
            m_ServerNetworkManager.OnClientConnectedCallback += Server_OnClientConnectedCallback;

            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.EnableSceneManagement = m_SceneManagementEnabled;
                client.OnClientConnectedCallback += Client_OnClientConnectedCallback;
            }

            base.OnServerAndClientsCreated();
        }

        [UnityTest]
        public IEnumerator VerifyOnClientConnectedCallback()
        {
            yield return WaitForConditionOrTimeOut(AllCallbacksCalled);
            AssertOnTimeout("Timed out waiting for all clients to be connected!");

            // The client callbacks should have been called once per client (called once on self)
            Assert.True(m_ClientCallbackCalled.Count == NumberOfClients);

            // The server callback should be called for self, and then once per client
            Assert.True(m_ServerCallbackCalled.Count == 1 + NumberOfClients);
        }

        /// <summary>
        /// Validates that no warnings are logged upon a client disconnecting and the
        /// log level is set to developer.
        /// </summary>
        [UnityTest]
        public IEnumerator VerifyNoWarningOnClientDisconnect()
        {
            yield return WaitForConditionOrTimeOut(AllCallbacksCalled);
            AssertOnTimeout("Timed out waiting for all clients to be connected!");

            var authority = GetAuthorityNetworkManager();
            var clientToDisconnect = GetNonAuthorityNetworkManager();
            clientToDisconnect.LogLevel = LogLevel.Developer;
            authority.LogLevel = LogLevel.Developer;

            yield return StopOneClient(clientToDisconnect);

            NetcodeLogAssert.LogWasNotReceived(LogType.Warning, new Regex(".*"));
        }


        private void Server_OnClientConnectedCallback(ulong clientId)
        {
            if (!m_ServerCallbackCalled.Add(clientId))
            {
                Assert.Fail($"Client already connected: {clientId}");
            }
        }

        private void Client_OnClientConnectedCallback(ulong clientId)
        {
            if (!m_ClientCallbackCalled.Add(clientId))
            {
                Assert.Fail($"Client already connected: {clientId}");
            }
        }

        private bool AllCallbacksCalled()
        {
            foreach (var client in m_ClientNetworkManagers)
            {
                if (!m_ClientCallbackCalled.Contains(client.LocalClientId) || !m_ServerCallbackCalled.Contains(client.LocalClientId))
                {
                    return false;
                }
            }

            return m_ServerCallbackCalled.Contains(m_ServerNetworkManager.LocalClientId);
        }
    }
}

