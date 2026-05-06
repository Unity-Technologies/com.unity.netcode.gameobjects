using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;


namespace TestProject.RuntimeTests
{

    [TestFixture(NetworkTopologyTypes.DistributedAuthority, HostOrServer.DAHost)]
    [TestFixture(NetworkTopologyTypes.ClientServer, HostOrServer.Host)]
#if UNIFIED_NETCODE
    [TestFixture(NetworkTopologyTypes.ClientServer, HostOrServer.UnifiedHost)]
#endif
    public class ServerDisconnectsClientTest : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        // TODO: [CmbServiceTests] Adapt to run with the service
        protected override bool UseCMBService()
        {
            return false;
        }

        public ServerDisconnectsClientTest(NetworkTopologyTypes networkTopologyType, HostOrServer hostOrServer) : base(networkTopologyType, hostOrServer) { }

        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.AddComponent<ClientSendRpcUponDisconnect>();
            base.OnCreatePlayerPrefab();
        }

        [UnityTest]
        public IEnumerator ServerDisconnectsClient()
        {
            m_ServerNetworkManager.DisconnectClient(m_ClientNetworkManagers[0].LocalClientId);
            yield return WaitForConditionOrTimeOut(() => !m_ClientNetworkManagers[0].IsConnectedClient && !m_ClientNetworkManagers[0].IsListening);
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, "Timed out waiting for client to disconnect!");
        }
        public class ClientSendRpcUponDisconnect : NetworkBehaviour
        {
            public override void OnNetworkSpawn()
            {
                if (!IsServer)
                {
                    NetworkManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
                }
                base.OnNetworkSpawn();
            }

            [Rpc(SendTo.Server)]
            public void ClientToServerRpc()
            {
                Debug.Log($"Received {nameof(ClientToServerRpc)}");
            }

            private void NetworkManager_OnClientDisconnectCallback(ulong obj)
            {
                NetworkManager.OnClientDisconnectCallback -= NetworkManager_OnClientDisconnectCallback;
                // To simulate that there were already messages to be sent this frame in the send queue,
                // we just send a few packets to the server even though we are considered disconnected
                // at this point.
                for (int i = 0; i < 5; i++)
                {
                    ClientToServerRpc();
                }
            }
        }
    }
}
