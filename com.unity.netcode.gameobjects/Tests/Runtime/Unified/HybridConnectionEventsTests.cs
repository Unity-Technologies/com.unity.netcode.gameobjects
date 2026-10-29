#if UNIFIED_NETCODE
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Validates <see cref="NetworkManager.OnNetCodeConnect"/> and <see cref="NetworkManager.OnNetCodeDisconnect"/> fire
    /// for each connection, and that a client only goes in game once NGO has synchronized it.
    /// </summary>
    [TestFixture(HostOrServer.UnifiedHost)]
    [TestFixture(HostOrServer.UnifiedServer)]
    internal class HybridConnectionEventsTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private readonly List<int> m_ServerConnects = new List<int>();
        private readonly List<int> m_ServerDisconnects = new List<int>();
        private int m_ClientConnects;
        private bool m_ClientSynchronizedOnConnect;

        public HybridConnectionEventsTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override bool UseUnifiedTests()
        {
            return true;
        }

        protected override void OnServerAndClientsCreated()
        {
            // No world is created for a session without ghost prefabs.
            CreateHybridPrefab("ConnectionEventsPrefab");
            m_ServerConnects.Clear();
            m_ServerDisconnects.Clear();
            m_ClientConnects = 0;
            m_ClientSynchronizedOnConnect = false;
            NetworkManager.OnNetCodeConnect += OnNetCodeConnect;
            NetworkManager.OnNetCodeDisconnect += OnNetCodeDisconnect;
            base.OnServerAndClientsCreated();
        }

        protected override IEnumerator OnTearDown()
        {
            NetworkManager.OnNetCodeConnect -= OnNetCodeConnect;
            NetworkManager.OnNetCodeDisconnect -= OnNetCodeDisconnect;
            return base.OnTearDown();
        }

        private void OnNetCodeConnect(NetcodeConnection connection)
        {
            if (connection.IsServer)
            {
                m_ServerConnects.Add(connection.NetworkId);
                return;
            }
            m_ClientConnects++;
            m_ClientSynchronizedOnConnect = m_ClientNetworkManagers[0].IsConnectedClient;
        }

        private void OnNetCodeDisconnect(NetcodeConnection connection)
        {
            if (connection.IsServer)
            {
                m_ServerDisconnects.Add(connection.NetworkId);
            }
        }

        [UnityTest]
        public IEnumerator ConnectFiresOnceSynchronized()
        {
            var clientNetworkId = m_ClientNetworkManagers[0].NetcodeWorld.LocalConnection.NetworkId.Value;
            yield return WaitForConditionOrTimeOut(() => m_ClientConnects > 0 && m_ServerConnects.Contains(clientNetworkId));
            AssertOnTimeout($"Connect events missing! Client connects: {m_ClientConnects}, server saw the client: {m_ServerConnects.Contains(clientNetworkId)}");

            Assert.AreEqual(1, m_ClientConnects, "The client connection should only be reported once.");
            Assert.IsTrue(m_ClientSynchronizedOnConnect, "The client went in game prior to NGO synchronizing it.");
        }

        [UnityTest]
        public IEnumerator ServerReportsClientDisconnect()
        {
            var clientNetworkId = m_ClientNetworkManagers[0].NetcodeWorld.LocalConnection.NetworkId.Value;
            yield return WaitForConditionOrTimeOut(() => m_ServerConnects.Contains(clientNetworkId));
            AssertOnTimeout("The server never reported the client connection!");

            yield return StopOneClient(m_ClientNetworkManagers[0]);
            yield return WaitForConditionOrTimeOut(() => m_ServerDisconnects.Contains(clientNetworkId));
            AssertOnTimeout("The server never reported the client disconnect!");
            Assert.IsTrue(m_ServerNetworkManager.IsListening, "The disconnect should be reported while the session is still running.");
        }
    }
}
#endif
