using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;

namespace Unity.Netcode.RuntimeTests
{
    internal class NetworkManagerCustomMessageManagerTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // This test does not need to run against the Rust server.
            NetcodeIntegrationTestHelpers.IgnoreIfServiceEnviromentVariableSet();
        }

        [Test]
        public void CustomMessageManagerAssigned()
        {
            var gameObject = new GameObject(nameof(CustomMessageManagerAssigned));
            var networkManager = gameObject.AddComponent<NetworkManager>();
            var transport = gameObject.AddComponent<DummyTransport>();

            networkManager.NetworkConfig = new NetworkConfig
            {
                // Set dummy transport that does nothing
                NetworkTransport = transport
            };

            CustomMessagingManager preManager = networkManager.CustomMessagingManager;

            // Start server to cause initialization
            networkManager.StartServer();

            Debug.Assert(preManager == null);
            Debug.Assert(networkManager.CustomMessagingManager != null);


            networkManager.Shutdown();
            Object.DestroyImmediate(gameObject);
        }
    }
}
