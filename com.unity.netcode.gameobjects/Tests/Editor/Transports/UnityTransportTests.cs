using NUnit.Framework;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.EditorTests
{
    internal class UnityTransportTests
    {
        [SetUp]
        public void OnSetup()
        {
            ILPPMessageProvider.IntegrationTestNoMessages = true;
        }

        [TearDown]
        public void OnTearDown()
        {
            ILPPMessageProvider.IntegrationTestNoMessages = false;
        }

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

            transport.SetConnectionData("127.0.0.1", 4242, "foobar");

            Assert.False(transport.StartServer());
            LogAssert.Expect(LogType.Error, "Invalid listen endpoint: foobar:4242. Note that the listen endpoint MUST be an IP address (not a hostname).");

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
                Assert.False(transport.GetNetworkDriver().IsCreated);
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

#if HOSTNAME_RESOLUTION_AVAILABLE
        private static readonly (string, bool)[] k_HostnameChecks =
        {
            ("localhost", true),
            ("unity3d.com", true),
            ("unity3d.com.", true),
            (string.Empty, false),
            ("unity3d.com/test", false),
            ("test%123.com", false),
        };

        [Test]
        [TestCaseSource(nameof(k_HostnameChecks))]
        public void UnityTransport_HostnameValidation((string, bool) testCase)
        {
            var (hostname, isValid) = testCase;

            UnityTransport transport = new GameObject().AddComponent<UnityTransport>();
            transport.Initialize();

            if (!isValid)
            {
                LogAssert.Expect(LogType.Error, $"Provided connection address \"{hostname}\" is not a valid hostname.");
            }

            transport.SetConnectionData(hostname, 4242);
            Assert.AreEqual(isValid, transport.StartClient());

            transport.Shutdown();
        }
#endif
    }
}
