using System.Collections;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.TestTools;


namespace Unity.Netcode.RuntimeTests
{
    public class ClientApprovalDenied : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 0;
        private bool m_ApproveConnection = true;
        private ulong m_PendingClientId = 0;
        private ulong m_DisconnectedClientId = 0;

        protected override void OnServerAndClientsCreated()
        {
            m_ServerNetworkManager.NetworkConfig.ConnectionApproval = true;
            m_ServerNetworkManager.ConnectionApprovalCallback = ConnectionApproval;
        }

        private void ConnectionApproval(NetworkManager.ConnectionApprovalRequest connectionApprovalRequest, NetworkManager.ConnectionApprovalResponse connectionApprovalResponse)
        {
            connectionApprovalResponse.Approved = m_ApproveConnection;
            connectionApprovalResponse.CreatePlayerObject = true;
            // When denied, store the client identifier to use for validating the client disconnected notification identifier matches
            if (!m_ApproveConnection)
            {
                m_PendingClientId = connectionApprovalRequest.ClientNetworkId;
            }
        }

        protected override void OnNewClientCreated(NetworkManager networkManager)
        {
            networkManager.NetworkConfig.ConnectionApproval = true;
            base.OnNewClientCreated(networkManager);
        }

        protected override bool WaitForNewClientToConnect(NetworkManager networkManager)
        {
            return false;
        }

        /// <summary>
        /// Validates that when a pending client is denied approval the server-host
        /// OnClientDisconnected method will return the valid pending client identifier.
        /// </summary>
        [UnityTest]
        public IEnumerator ClientApprovalDeniedNotificationTest()
        {
            m_ApproveConnection = false;
            m_ServerNetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
            yield return CreateAndStartNewClient();
            m_ServerNetworkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;

            yield return WaitForConditionOrTimeOut(() => m_PendingClientId == m_DisconnectedClientId);
            AssertOnTimeout($"Timed out waiting for disconnect notification for pending Client-{m_PendingClientId}!");
        }

        private void OnClientDisconnectCallback(ulong clientId)
        {
            m_DisconnectedClientId = clientId;
        }
    }
}
