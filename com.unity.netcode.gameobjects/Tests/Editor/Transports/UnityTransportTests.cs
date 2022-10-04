using NUnit.Framework;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.EditorTests
{
    public class UnityTransportTests
    {
        // Check that starting an IPv4 server succeeds.
        [Test]
        public void UnityTransport_BasicInitServer_IPv4()
        {
            UnityTransport transport = new GameObject().AddComponent<UnityTransport>();
            transport.Initialize();

            Assert.True(transport.StartServer());

            transport.Shutdown();
        }

        // Check that starting an IPv4 client succeeds.
        [Test]
        public void UnityTransport_BasicInitClient_IPv4()
        {
            UnityTransport transport = new GameObject().AddComponent<UnityTransport>();
            transport.Initialize();

            Assert.True(transport.StartClient());

            transport.Shutdown();
        }

        // Check that starting an IPv6 server succeeds.
        [Test]
        public void UnityTransport_BasicInitServer_IPv6()
        {
            UnityTransport transport = new GameObject().AddComponent<UnityTransport>();
            transport.Initialize();
            transport.SetConnectionData("::1", 7777);

            Assert.True(transport.StartServer());

            transport.Shutdown();
        }

        // Check that starting an IPv6 client succeeds.
        [Test]
        public void UnityTransport_BasicInitClient_IPv6()
        {
            UnityTransport transport = new GameObject().AddComponent<UnityTransport>();
            transport.Initialize();
            transport.SetConnectionData("::1", 7777);

            Assert.True(transport.StartClient());

            transport.Shutdown();
        }

        // Check that we can't restart a server.
        [Test]
        public void UnityTransport_NoRestartServer()
        {
            UnityTransport transport = new GameObject().AddComponent<UnityTransport>();
            transport.Initialize();

            transport.StartServer();
            Assert.False(transport.StartServer());

            transport.Shutdown();
        }

        // Check that we can't restart a client.
        [Test]
        public void UnityTransport_NoRestartClient()
        {
            UnityTransport transport = new GameObject().AddComponent<UnityTransport>();
            transport.Initialize();

            transport.StartClient();
            Assert.False(transport.StartClient());

            transport.Shutdown();
        }

        // Check that we can't start both a server and client on the same transport.
        [Test]
        public void UnityTransport_NotBothServerAndClient()
        {
            UnityTransport transport;

            // Start server then client.
            transport = new GameObject().AddComponent<UnityTransport>();
            transport.Initialize();

            transport.StartServer();
            Assert.False(transport.StartClient());

            transport.Shutdown();

            // Start client then server.
            transport = new GameObject().AddComponent<UnityTransport>();
            transport.Initialize();

            transport.StartClient();
            Assert.False(transport.StartServer());

            transport.Shutdown();
        }

        // Check that restarting after failure succeeds.
        [Test]
        public void UnityTransport_RestartSucceedsAfterFailure()
        {
            UnityTransport transport = new GameObject().AddComponent<UnityTransport>();
            transport.Initialize();

            transport.SetConnectionData("127.0.0.", 4242);
            Assert.False(transport.StartServer());

            LogAssert.Expect(LogType.Error, "Invalid network endpoint: 127.0.0.:4242.");
            LogAssert.Expect(LogType.Error, "Server failed to bind");

            transport.SetConnectionData("127.0.0.1", 4242);
            Assert.True(transport.StartServer());

            transport.Shutdown();
        }
    }
}
