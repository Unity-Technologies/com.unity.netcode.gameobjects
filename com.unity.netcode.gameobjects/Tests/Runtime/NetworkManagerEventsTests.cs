using System;
using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkManagerEventsTests
    {
        private NetworkManager m_ClientManager;
        private NetworkManager m_ServerManager;

        [UnityTest]
        public IEnumerator OnServerStoppedCalledWhenServerStops()
        {
            bool callbackInvoked = false;
            var gameObject = new GameObject(nameof(OnServerStoppedCalledWhenServerStops));
            m_ServerManager = gameObject.AddComponent<NetworkManager>();

            // Set dummy transport that does nothing
            var transport = gameObject.AddComponent<DummyTransport>();
            m_ServerManager.NetworkConfig = new NetworkConfig() { NetworkTransport = transport };

            Action<bool> onServerStopped = (bool wasAlsoClient) =>
            {
                callbackInvoked = true;
                Assert.IsFalse(wasAlsoClient);
            };

            // Start server to cause initialization process
            Assert.True(m_ServerManager.StartServer());
            Assert.True(m_ServerManager.IsListening);

            m_ServerManager.OnServerStopped += onServerStopped;
            m_ServerManager.Shutdown();
            UnityEngine.Object.DestroyImmediate(gameObject);

            yield return WaitUntilManagerShutsdown();

            Assert.False(m_ServerManager.IsListening);
            Assert.True(callbackInvoked, "OnServerStopped wasn't invoked");
        }

        [UnityTest]
        public IEnumerator OnClientStoppedCalledWhenClientStops()
        {
            yield return InitializeServerAndAClient();

            bool callbackInvoked = false;
            Action<bool> onClientStopped = (bool wasAlsoServer) =>
            {
                callbackInvoked = true;
                Assert.IsFalse(wasAlsoServer);
            };

            m_ClientManager.OnClientStopped += onClientStopped;
            m_ClientManager.Shutdown();
            yield return WaitUntilManagerShutsdown();

            Assert.True(callbackInvoked, "OnClientStopped wasn't invoked");
        }

        [UnityTest]
        public IEnumerator OnClientAndServerStoppedCalledWhenHostStops()
        {
            var gameObject = new GameObject(nameof(OnClientAndServerStoppedCalledWhenHostStops));
            m_ServerManager = gameObject.AddComponent<NetworkManager>();

            // Set dummy transport that does nothing
            var transport = gameObject.AddComponent<DummyTransport>();
            m_ServerManager.NetworkConfig = new NetworkConfig() { NetworkTransport = transport };

            int callbacksInvoked = 0;
            Action<bool> onClientStopped = (bool wasAlsoServer) =>
            {
                callbacksInvoked++;
                Assert.IsTrue(wasAlsoServer);
            };

            Action<bool> onServerStopped = (bool wasAlsoClient) =>
            {
                callbacksInvoked++;
                Assert.IsTrue(wasAlsoClient);
            };

            // Start server to cause initialization process
            Assert.True(m_ServerManager.StartHost());
            Assert.True(m_ServerManager.IsListening);

            m_ServerManager.OnServerStopped += onServerStopped;
            m_ServerManager.OnClientStopped += onClientStopped;
            m_ServerManager.Shutdown();
            UnityEngine.Object.DestroyImmediate(gameObject);

            yield return WaitUntilManagerShutsdown();

            Assert.False(m_ServerManager.IsListening);
            Assert.AreEqual(2, callbacksInvoked, "either OnServerStopped or OnClientStopped wasn't invoked");
        }

        [UnityTest]
        public IEnumerator OnServerStartedCalledWhenServerStarts()
        {
            var gameObject = new GameObject(nameof(OnServerStartedCalledWhenServerStarts));
            m_ServerManager = gameObject.AddComponent<NetworkManager>();

            // Set dummy transport that does nothing
            var transport = gameObject.AddComponent<DummyTransport>();
            m_ServerManager.NetworkConfig = new NetworkConfig() { NetworkTransport = transport };

            bool callbackInvoked = false;
            Action onServerStarted = () =>
            {
                callbackInvoked = true;
                if (!m_ServerManager.IsServer)
                {
                    Assert.Fail("OnServerStarted called when the server is not active yet");
                }
            };

            // Start server to cause initialization process
            m_ServerManager.OnServerStarted += onServerStarted;

            Assert.True(m_ServerManager.StartServer());
            Assert.True(m_ServerManager.IsListening);

            yield return WaitUntilServerBufferingIsReady();

            Assert.True(callbackInvoked, "OnServerStarted wasn't invoked");
        }

        [UnityTest]
        public IEnumerator OnClientStartedCalledWhenClientStarts()
        {
            bool callbackInvoked = false;
            Action onClientStarted = () =>
            {
                callbackInvoked = true;
                if (!m_ClientManager.IsClient)
                {
                    Assert.Fail("onClientStarted called when the client is not active yet");
                }
            };

            yield return InitializeServerAndAClient(onClientStarted);

            Assert.True(callbackInvoked, "OnClientStarted wasn't invoked");
        }

        [UnityTest]
        public IEnumerator OnClientAndServerStartedCalledWhenHostStarts()
        {
            var gameObject = new GameObject(nameof(OnClientAndServerStartedCalledWhenHostStarts));
            m_ServerManager = gameObject.AddComponent<NetworkManager>();

            // Set dummy transport that does nothing
            var transport = gameObject.AddComponent<DummyTransport>();
            m_ServerManager.NetworkConfig = new NetworkConfig() { NetworkTransport = transport };

            int callbacksInvoked = 0;
            Action onClientStarted = () =>
            {
                callbacksInvoked++;
            };

            Action onServerStarted = () =>
            {
                callbacksInvoked++;
            };

            m_ServerManager.OnServerStarted += onServerStarted;
            m_ServerManager.OnClientStarted += onClientStarted;

            // Start server to cause initialization process
            Assert.True(m_ServerManager.StartHost());
            Assert.True(m_ServerManager.IsListening);

            yield return WaitUntilServerBufferingIsReady();
            Assert.AreEqual(2, callbacksInvoked, "either OnServerStarted or OnClientStarted wasn't invoked");
        }

        private IEnumerator WaitUntilManagerShutsdown()
        {
            /* Need two updates to actually shut down. First one to see the transport failing, which
            marks the NetworkManager as shutting down. Second one where actual shutdown occurs. */
            yield return null;
            yield return null;
        }

        private IEnumerator InitializeServerAndAClient(Action onClientStarted = null)
        {
            // Create multiple NetworkManager instances
            if (!NetcodeIntegrationTestHelpers.Create(1, out m_ServerManager, out NetworkManager[] clients, 30))
            {
                Debug.LogError("Failed to create instances");
                Assert.Fail("Failed to create instances");
            }

            // passing no clients on purpose to start them manually later
            NetcodeIntegrationTestHelpers.Start(false, m_ServerManager, new NetworkManager[] { });

            yield return WaitUntilServerBufferingIsReady();
            m_ClientManager = clients[0];

            if (onClientStarted != null)
            {
                m_ClientManager.OnClientStarted += onClientStarted;
            }

            Assert.True(m_ClientManager.StartClient());
            NetcodeIntegrationTestHelpers.RegisterHandlers(clients[0]);
            // Wait for connection on client side
            yield return NetcodeIntegrationTestHelpers.WaitForClientsConnected(clients);
        }

        private IEnumerator WaitUntilServerBufferingIsReady()
        {
            /* wait until at least more than 2 server ticks have passed
            Note: Waiting for more than 2 ticks on the server is due
            to the time system applying buffering to the received time
            in NetworkTimeSystem.Sync */
            yield return new WaitUntil(() => m_ServerManager.NetworkTickSystem.ServerTime.Tick > 2);
        }

        [UnityTearDown]
        public virtual IEnumerator Teardown()
        {
            NetcodeIntegrationTestHelpers.Destroy();
            yield return null;
        }
    }
}
