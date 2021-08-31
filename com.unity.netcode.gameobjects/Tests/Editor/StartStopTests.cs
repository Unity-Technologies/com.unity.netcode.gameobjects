using NUnit.Framework;
using UnityEngine;

namespace Unity.Netcode.EditorTests
{
    public class StartStopTests
    {
        [Test]
        public void TestStopAndRestartForExceptions()
        {
            // Create the reusable NetworkManager
            var singletonNetworkManager = new GameObject(nameof(NetworkManager)).AddComponent<NetworkManager>();
            var transport = singletonNetworkManager.gameObject.AddComponent<DummyTransport>();

            singletonNetworkManager.NetworkConfig = new NetworkConfig()
            {
                NetworkTransport = transport
            };

            // Start a normal server
            singletonNetworkManager.StartServer();

            // Ensure proper start
            Assert.True(singletonNetworkManager.IsServer);
            Assert.False(singletonNetworkManager.IsClient);
            Assert.False(singletonNetworkManager.IsHost);

            // Shut it down.
            singletonNetworkManager.Shutdown();

            Assert.False(singletonNetworkManager.IsServer);
            Assert.False(singletonNetworkManager.IsClient);
            Assert.False(singletonNetworkManager.IsHost);

            // Restart
            singletonNetworkManager.StartServer();

            // Ensure everything is still normal after restart
            Assert.True(singletonNetworkManager.IsServer);
            Assert.False(singletonNetworkManager.IsClient);
            Assert.False(singletonNetworkManager.IsHost);

            // Final shutdown
            singletonNetworkManager.Shutdown();

            // Ensure everything is shut down
            Assert.False(singletonNetworkManager.IsServer);
            Assert.False(singletonNetworkManager.IsClient);
            Assert.False(singletonNetworkManager.IsHost);

            // Cleanup
            Object.DestroyImmediate(singletonNetworkManager.gameObject);
        }
    }
}
