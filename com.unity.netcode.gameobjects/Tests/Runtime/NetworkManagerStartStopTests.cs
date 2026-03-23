using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(NetworkTopologyTypes.ClientServer)]
    internal class NetworkManagerStartStopTests : NetcodeIntegrationTest
    {
        private const int k_NumberOfSessions = 5;
        protected override int NumberOfClients => 2;
        private OnClientStoppedHandler m_StoppedHandler;
        private int m_ExpectedNumberOfClients = 0;
        public NetworkManagerStartStopTests(NetworkTopologyTypes networkTopologyType) : base(networkTopologyType, HostOrServer.Host) { }

        /// <summary>
        /// This test will not work with the CMB service since it requires the service
        /// to remain active after all clients have disconnected.
        /// </summary>
        protected override bool UseCMBService()
        {
            return false;
        }

        private void ShutdownIfListening()
        {
            var networkManager = m_StoppedHandler.NetworkManager;
            if (networkManager.IsListening)
            {
                m_StoppedHandler.NetworkManager.Shutdown();
            }
        }

        private bool NetworkManagerCompletedSessionCount(StringBuilder errorLog)
        {
            // Once the session count is decremented to zero the condition has been met.
            if (m_StoppedHandler.SessionCount != 0)
            {
                // If we are a host, then only shutdown once all clients have reconnected
                if (m_StoppedHandler.IsSessionAuthority && m_StoppedHandler.NetworkManager.ConnectedClientsIds.Count != m_ExpectedNumberOfClients)
                {
                    errorLog.Append($"[{m_StoppedHandler.NetworkManager.name}] Waiting for {m_ExpectedNumberOfClients} clients to connect but there are only {m_StoppedHandler.NetworkManager.ConnectedClientsIds.Count} connected!");
                    return false;
                }
                ShutdownIfListening();
                errorLog.Append($"[{m_StoppedHandler.NetworkManager.name}] Still has a session count of {m_StoppedHandler.SessionCount}!");
            }
            return errorLog.Length == 0;
        }

        [UnityTest]
        public IEnumerator StartFromWithinOnClientStopped()
        {
            var authority = GetAuthorityNetworkManager();
            m_ExpectedNumberOfClients = authority.ConnectedClientsIds.Count;

            // Validate a client can disconnect and immediately reconnect from within OnClientStopped
            m_StoppedHandler = new OnClientStoppedHandler(k_NumberOfSessions, GetNonAuthorityNetworkManager());
            ShutdownIfListening();
            yield return WaitForConditionOrTimeOut(NetworkManagerCompletedSessionCount);
            AssertOnTimeout($"Not all {nameof(NetworkManager)} instances finished their sessions!");

            // Validate a host can disconnect and immediately reconnect from within OnClientStopped
            m_StoppedHandler = new OnHostStoppedHandler(k_NumberOfSessions, authority, m_NetworkManagers.ToList());
            ShutdownIfListening();
            yield return WaitForConditionOrTimeOut(NetworkManagerCompletedSessionCount);
            AssertOnTimeout($"Not all {nameof(NetworkManager)} instances finished their sessions!");

            // Verify OnServerStopped is not invoked if NetworkManager is started again within OnClientStopped (it should not invoke if it is listening).
            Assert.False((m_StoppedHandler as OnHostStoppedHandler).OnServerStoppedInvoked, $"{nameof(NetworkManager.OnServerStopped)} was invoked when it should not have been invoked!");
        }
    }

    internal class OnHostStoppedHandler : OnClientStoppedHandler
    {
        public bool OnServerStoppedInvoked = false;

        private List<NetworkManager> m_Clients = new List<NetworkManager>();

        private Networking.Transport.NetworkEndpoint m_Endpoint;

        protected override void OnClientStopped(bool wasHost)
        {
            m_Endpoint.Port++;
            var unityTransport = (Transports.UTP.UnityTransport)NetworkManager.NetworkConfig.NetworkTransport;
            unityTransport.SetConnectionData(m_Endpoint);
            // Make sure all clients are shutdown or shutting down
            foreach (var networkManager in m_Clients)
            {
                if (networkManager.IsListening && !networkManager.ShutdownInProgress)
                {
                    networkManager.Shutdown();
                }
            }

            base.OnClientStopped(wasHost);
            if (SessionCount != 0)
            {
                NetworkManager.StartCoroutine(StartClients());
            }

        }

        private IEnumerator StartClients()
        {
            var nextPhase = false;
            var timeout = UnityEngine.Time.realtimeSinceStartup + 5.0f;
            while (!nextPhase)
            {
                if (!nextPhase && timeout < UnityEngine.Time.realtimeSinceStartup)
                {
                    Assert.Fail($"Timed out waiting for all {nameof(NetworkManager)} instances to shutdown!");
                    yield break;
                }

                nextPhase = true;
                foreach (var networkManager in m_Clients)
                {
                    if (networkManager.ShutdownInProgress || networkManager.IsListening)
                    {
                        nextPhase = false;
                    }
                }
                yield return null;
            }

            // Now, start all of the clients and have them connect again
            foreach (var networkManager in m_Clients)
            {
                var unityTransport = (Transports.UTP.UnityTransport)networkManager.NetworkConfig.NetworkTransport;
                unityTransport.SetConnectionData(m_Endpoint);
                networkManager.StartClient();
            }
        }

        public OnHostStoppedHandler(int numberOfSessions, NetworkManager authority, List<NetworkManager> networkManagers) : base(numberOfSessions, authority)
        {
            m_Endpoint = ((Transports.UTP.UnityTransport)authority.NetworkConfig.NetworkTransport).GetLocalEndpoint();
            networkManagers.Remove(authority);
            m_Clients = networkManagers;
            authority.OnServerStopped += OnServerStopped;
        }

        private void OnServerStopped(bool wasHost)
        {
            OnServerStoppedInvoked = SessionCount != 0;
        }
    }

    internal class OnClientStoppedHandler
    {
        public NetworkManager NetworkManager { get; private set; }

        public int SessionCount { get; private set; }
        public bool IsSessionAuthority { get; private set; }

        protected virtual void OnClientStopped(bool wasHost)
        {
            SessionCount--;
            if (SessionCount <= 0)
            {
                NetworkManager.OnClientStopped -= OnClientStopped;
                return;
            }

            if (wasHost)
            {
                NetworkManager.StartHost();
            }
            else
            {
                NetworkManager.StartClient();
            }
        }

        public OnClientStoppedHandler(int sessionCount, NetworkManager networkManager)
        {
            NetworkManager = networkManager;
            NetworkManager.OnClientStopped += OnClientStopped;
            SessionCount = sessionCount;
            IsSessionAuthority = networkManager.IsServer || networkManager.LocalClient.IsSessionOwner;
        }

        public OnClientStoppedHandler() { }

    }
}
