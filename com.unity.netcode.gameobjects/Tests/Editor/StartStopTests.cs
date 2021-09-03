using NUnit.Framework;
using UnityEngine;

namespace Unity.Netcode.EditorTests
{
    public class StartStopTests
    {
        private NetworkManager m_NetworkManager;

        [SetUp]
        public void Setup()
        {
            // Create the reusable NetworkManager
            m_NetworkManager = new GameObject(nameof(NetworkManager)).AddComponent<NetworkManager>();
            var transport = m_NetworkManager.gameObject.AddComponent<DummyTransport>();

            m_NetworkManager.NetworkConfig = new NetworkConfig()
            {
                NetworkTransport = transport
            };
        }

        [Test]
        public void TestStopAndRestartForExceptions()
        {
            m_NetworkManager.StartServer();
            m_NetworkManager.Shutdown();
            m_NetworkManager.StartServer();
            m_NetworkManager.Shutdown();
        }

        [Test]
        public void TestStartupServerState()
        {
            m_NetworkManager.StartServer();

            Assert.True(m_NetworkManager.IsServer);
            Assert.False(m_NetworkManager.IsClient);
            Assert.False(m_NetworkManager.IsHost);

            m_NetworkManager.Shutdown();
        }

        [Test]
        public void TestFlagShutdown()
        {
            m_NetworkManager.StartServer();
            m_NetworkManager.Shutdown();

            Assert.False(m_NetworkManager.IsServer);
            Assert.False(m_NetworkManager.IsClient);
            Assert.False(m_NetworkManager.IsHost);
        }

        [TearDown]
        public void Teardown()
        {
            // Cleanup
            Object.DestroyImmediate(m_NetworkManager.gameObject);
        }
    }
}
