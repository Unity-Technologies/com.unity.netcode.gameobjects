using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    public class NetworkManagerTransportTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private bool m_CanStartServerAndClients = false;

        public NetworkManagerTransportTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override IEnumerator OnSetup()
        {
            m_CanStartServerAndClients = false;
            return base.OnSetup();
        }

        protected override bool CanStartServerAndClients()
        {
            return m_CanStartServerAndClients;
        }

        protected override void OnServerAndClientsCreated()
        {

        }

        /// <summary>
        /// Validate that if the NetworkTransport fails to start the NetworkManager
        /// will not continue the startup process and will shut itself down.
        /// </summary>
        /// <param name="testClient">if true it will test the client side</param>
        [UnityTest]
        public IEnumerator DoesNotStartWhenTransportFails([Values] bool testClient)
        {
            // The error message we should expect
            var messageToCheck = "";
            if (!testClient)
            {
                Object.DestroyImmediate(m_ServerNetworkManager.NetworkConfig.NetworkTransport);
                m_ServerNetworkManager.NetworkConfig.NetworkTransport = m_ServerNetworkManager.gameObject.AddComponent<FailedTransport>();
                m_ServerNetworkManager.NetworkConfig.NetworkTransport.Initialize(m_ServerNetworkManager);
                // The error message we should expect
                messageToCheck = $"Server is shutting down due to network transport start failure of {m_ServerNetworkManager.NetworkConfig.NetworkTransport.GetType().Name}!";
            }
            else
            {
                foreach (var client in m_ClientNetworkManagers)
                {
                    Object.DestroyImmediate(client.NetworkConfig.NetworkTransport);
                    client.NetworkConfig.NetworkTransport = client.gameObject.AddComponent<FailedTransport>();
                    client.NetworkConfig.NetworkTransport.Initialize(m_ServerNetworkManager);
                }
                // The error message we should expect
                messageToCheck = $"Client is shutting down due to network transport start failure of {m_ClientNetworkManagers[0].NetworkConfig.NetworkTransport.GetType().Name}!";
            }

            // Trap for the nested NetworkManager exception
            LogAssert.Expect(LogType.Error, messageToCheck);
            m_CanStartServerAndClients = true;
            // Due to other errors, we must not send clients if testing the server-host side
            // We can test both server and client(s) when testing client-side only
            if (testClient)
            {
                NetcodeIntegrationTestHelpers.Start(m_UseHost, m_ServerNetworkManager, m_ClientNetworkManagers);
                yield return s_DefaultWaitForTick;
                foreach(var client in m_ClientNetworkManagers)
                {
                    Assert.False(client.IsListening);
                    Assert.False(client.IsConnectedClient);
                }
            }
            else
            {
                NetcodeIntegrationTestHelpers.Start(m_UseHost, m_ServerNetworkManager, new NetworkManager[] { });
                yield return s_DefaultWaitForTick;
                Assert.False(m_ServerNetworkManager.IsListening);
            }
        }
    }

    /// <summary>
    /// Does nothing but simulate a transport that failed to start
    /// </summary>
    public class FailedTransport : TestingNetworkTransport
    {
        public override void Shutdown()
        {
        }

        public override ulong ServerClientId => 0;

        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            clientId = 0;
            payload = null;
            receiveTime = 0;
            return NetworkEvent.Nothing;
        }
        public override bool StartClient()
        {
            // Simulate failure, always return false
            return false;
        }
        public override bool StartServer()
        {
            // Simulate failure, always return false
            return false;
        }
        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
        {
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
        }

        public override void Initialize(NetworkManager networkManager = null)
        {
        }
        public override ulong GetCurrentRtt(ulong clientId)
        {
            return 0;
        }
        public override void DisconnectLocalClient()
        {
        }
    }
}
