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

            transport.SetConnectionData("127.0.0.", 4242, "127.0.0.");

            Assert.False(transport.StartServer());

            LogAssert.Expect(LogType.Error, "Invalid network endpoint: 127.0.0.:4242.");

#if HOSTNAME_RESOLUTION_AVAILABLE && UTP_TRANSPORT_2_4_ABOVE
            LogAssert.Expect(LogType.Error, $"Listen network address (127.0.0.) is not a valid {Networking.Transport.NetworkFamily.Ipv4} or {Networking.Transport.NetworkFamily.Ipv6} address!");
#else
            LogAssert.Expect(LogType.Error, "Network listen address (127.0.0.) is Invalid!");
#endif

            transport.SetConnectionData("127.0.0.1", 4242, "127.0.0.1");
            Assert.True(transport.StartServer());

            transport.Shutdown();
        }

        // Check that leaving all addresses empty is valid.
        [Test]
        public void UnityTransport_StartServerWithoutAddresses()
        {
            UnityTransport transport = new GameObject().AddComponent<UnityTransport>();
            transport.Initialize();

            transport.SetConnectionData(string.Empty, 4242);
            Assert.True(transport.StartServer());

            transport.Shutdown();
        }

        // Check that StartClient returns false with bad connection data.
        [Test]
        public void UnityTransport_StartClientFailsWithBadAddress()
        {
            UnityTransport transport = new GameObject().AddComponent<UnityTransport>();
            transport.Initialize();

            transport.SetConnectionData("foobar", 4242);
            Assert.False(transport.StartClient());
            LogAssert.Expect(LogType.Error, "Invalid network endpoint: foobar:4242.");
#if HOSTNAME_RESOLUTION_AVAILABLE && UTP_TRANSPORT_2_4_ABOVE
            LogAssert.Expect(LogType.Error, "Target server network address (foobar) is not a valid Fully Qualified Domain Name!");
#else
            LogAssert.Expect(LogType.Error, "Target server network address (foobar) is Invalid!");
#endif

            transport.Shutdown();
        }

#if UTP_TRANSPORT_2_0_ABOVE
        [Test]
        public void UnityTransport_EmptySecurityStringsShouldThrow([Values("", null)] string cert, [Values("", null)] string secret)
        {
            var supportingGO = new GameObject();
            try
            {
                var networkManager = supportingGO.AddComponent<NetworkManager>(); // NM is required for UTP to work with certificates.
                networkManager.NetworkConfig = new NetworkConfig();
                UnityTransport transport = supportingGO.AddComponent<UnityTransport>();
                networkManager.NetworkConfig.NetworkTransport = transport;
                transport.Initialize();
                transport.SetServerSecrets(serverCertificate: cert, serverPrivateKey: secret);

                // Use encryption, but don't set certificate and check for exception
                transport.UseEncryption = true;
                Assert.Throws<System.Exception>(() =>
                {
                    networkManager.StartServer();
                });
                // Make sure StartServer failed
                Assert.False(transport.NetworkDriver.IsCreated);
                Assert.False(networkManager.IsServer);
                Assert.False(networkManager.IsListening);
            }
            finally
            {
                if (supportingGO != null)
                {
                    Object.DestroyImmediate(supportingGO);
                }
            }
        }
#endif
    }
}
