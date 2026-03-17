using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    internal class SessionVersionConnectionRequest : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 0;

        // Use a specific version for the CMB tests
        // The CMB service has more detailed versioning logic. Not all lower versions are invalid to connect with the higher version.
        // This version will not connect with the version lower.
        private const int k_ValidCMBVersion = 5;

        public SessionVersionConnectionRequest() : base(NetworkTopologyTypes.DistributedAuthority, HostOrServer.DAHost) { }

        private bool m_UseValidSessionVersion;
        private bool m_ClientWasDisconnected;
        private bool m_CanStartClients;

        // Don't start automatically when using the CMB Service
        // We want to customize the SessionVersion of the session owner before they connect
        protected override bool CanStartServerAndClients() => !m_UseCmbService || m_CanStartClients;

        /// <summary>
        /// Callback used to mock the scenario where a client has an invalid session version
        /// </summary>
        private SessionConfig GetInvalidSessionConfig()
        {
            var authority = GetAuthorityNetworkManager();
            return new SessionConfig(authority.SessionConfig.SessionVersion - 1);
        }

        /// <summary>
        /// Tracks if the client was disconnected or not
        /// </summary>
        private void OnClientDisconnectCallback(ulong clientId)
        {
            m_ClientWasDisconnected = true;
        }

        /// <summary>
        /// This handles disabling the internal integration test logic that waits for
        /// clients to be connected. When we know the client will be disconnected,
        /// we want to have the NetcodeIntegrationTest not wait for the client to
        /// connect (otherwise it will timeout there and fail the test).
        /// </summary>
        /// <param name="networkManager"></param>
        /// <returns>true = wait | false = don't wait</returns>
        protected override bool ShouldWaitForNewClientToConnect(NetworkManager networkManager)
        {
            return m_UseValidSessionVersion;
        }
        /// <summary>
        /// Validates that when the client's session config version is valid a client will be
        /// allowed to connect and when it is not valid the client will be disconnected.
        /// </summary>
        [UnityTest]
        public IEnumerator ValidateSessionVersion()
        {
            if (m_UseCmbService)
            {
                var authority = GetAuthorityNetworkManager();
                authority.OnGetSessionConfig = () => new SessionConfig(k_ValidCMBVersion);
                m_CanStartClients = true;
                yield return StartServerAndClients();
            }

            /*
             * Test client being disconnected due to invalid session version
             */
            m_UseValidSessionVersion = false;

            // Create and setup client to use invalid session config
            var invalidClient = CreateNewClient();
            invalidClient.OnClientDisconnectCallback += OnClientDisconnectCallback;
            invalidClient.OnGetSessionConfig = GetInvalidSessionConfig;

            // Start client and wait for disconnect callback
            m_ClientWasDisconnected = false;
            yield return StartClient(invalidClient);
            Assert.True(invalidClient.IsListening);
            yield return s_DefaultWaitForTick;

            var timeoutHelper = new TimeoutHelper(30f);
            yield return WaitForConditionOrTimeOut(() => !invalidClient.IsListening, timeoutHelper);
            AssertOnTimeout("Client is still listening when it should have been disconnected!", timeoutHelper);

            yield return WaitForConditionOrTimeOut(() => m_ClientWasDisconnected);
            AssertOnTimeout("Client was not disconnected when it should have been!");

            var expectedReason = m_UseCmbService ? "incompatible ngo c# package versions for feature" : ConnectionRequestMessage.InvalidSessionVersionMessage;
            Assert.That(invalidClient.DisconnectReason, Does.Contain(expectedReason), $"Client did not receive the correct invalid session version message! Received: {invalidClient.DisconnectReason}");

            // Clean up invalid client
            invalidClient.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            yield return StopOneClient(invalidClient, true);

            /*
             * Test a later client with a valid version
             * They should connect as normal
             */
            m_UseValidSessionVersion = true;

            // Create and setup client to use invalid session config
            var lateJoin = CreateNewClient();
            lateJoin.OnClientDisconnectCallback += OnClientDisconnectCallback;
            if (m_UseCmbService)
            {
                lateJoin.OnGetSessionConfig = () => new SessionConfig(k_ValidCMBVersion);
            }

            // Start client and wait for disconnect callback
            m_ClientWasDisconnected = false;
            yield return StartClient(lateJoin);
            yield return s_DefaultWaitForTick;

            Assert.False(m_ClientWasDisconnected, "Client was disconnected when it was expected to connect!");
            Assert.True(lateJoin.IsConnectedClient, "Client did not connect properly using the correct session version!");
            Assert.That(GetAuthorityNetworkManager().ConnectedClientsIds, Has.Member(lateJoin.LocalClientId), "Newly joined client should be in connected list!");

            // Clean up
            lateJoin.OnClientDisconnectCallback -= OnClientDisconnectCallback;
        }
    }
}
