using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    public class StopStartRuntimeTests
    {
        [UnityTest]
        public IEnumerator WhenShuttingDownAndRestarting_SDKRestartsSuccessfullyAndStaysRunning()
        {            // create server and client instances
            NetcodeIntegrationTestHelpers.Create(1, out NetworkManager server, out NetworkManager[] clients);

            try
            {

                // create prefab
                var gameObject = new GameObject("PlayerObject");
                var networkObject = gameObject.AddComponent<NetworkObject>();
                networkObject.DontDestroyWithOwner = true;
                NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);

                server.NetworkConfig.PlayerPrefab = gameObject;

                for (int i = 0; i < clients.Length; i++)
                {
                    clients[i].NetworkConfig.PlayerPrefab = gameObject;
                }

                // start server and connect clients
                NetcodeIntegrationTestHelpers.Start(false, server, clients);

                // wait for connection on client side
                yield return NetcodeIntegrationTestHelpers.WaitForClientsConnected(clients);

                // wait for connection on server side
                yield return NetcodeIntegrationTestHelpers.WaitForClientConnectedToServer(server);

                // shutdown the server
                server.Shutdown();

                // wait 1 frame because shutdowns are delayed
                var nextFrameNumber = Time.frameCount + 1;
                yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);

                // Verify the shutdown occurred
                Assert.IsFalse(server.IsServer);
                Assert.IsFalse(server.IsListening);
                Assert.IsFalse(server.IsHost);
                Assert.IsFalse(server.IsClient);

                server.StartServer();
                // Verify the server started
                Assert.IsTrue(server.IsServer);
                Assert.IsTrue(server.IsListening);

                // Wait several frames
                nextFrameNumber = Time.frameCount + 10;
                yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);

                // Verify the server is still running
                Assert.IsTrue(server.IsServer);
                Assert.IsTrue(server.IsListening);
            }
            finally
            {
                // cleanup
                NetcodeIntegrationTestHelpers.Destroy();
            }
        }
    }
}
