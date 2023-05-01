using System.Collections;
using NUnit.Framework;
using TestProject.RuntimeTests.Support;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    public class NoMemoryLeakOnNetworkManagerShutdownTest
    {
        private GameObject m_Prefab;

        [SetUp]
        public void Setup()
        {
            ShutdownDuringOnNetworkSpawnBehaviour.SpawnCount = 0;
            ShutdownDuringOnNetworkSpawnBehaviour.ClientRpcsCalled = 0;
            ShutdownDuringOnNetworkSpawnBehaviour.ServerRpcsCalled = 0;
            ShutdownDuringOnNetworkSpawnBehaviour.ShutdownImmediately = false;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            NetcodeIntegrationTestHelpers.Destroy();
            // Shutdown and clean up both of our NetworkManager instances
            if (m_Prefab)
            {
                Object.Destroy(m_Prefab);
            }
            yield break;
        }

        public IEnumerator RunTest()
        {
            // Must be 1 for this test.
            const int numClients = 1;
            Assert.True(NetcodeIntegrationTestHelpers.Create(numClients, out NetworkManager server, out NetworkManager[] clients));
            m_Prefab = new GameObject("Object");
            m_Prefab.AddComponent<ShutdownDuringOnNetworkSpawnBehaviour>();
            var networkObject = m_Prefab.AddComponent<NetworkObject>();

            // Make it a prefab
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);

            var validNetworkPrefab = new NetworkPrefab
            {
                Prefab = m_Prefab
            };
            server.NetworkConfig.Prefabs.Add(validNetworkPrefab);
            foreach (var client in clients)
            {
                client.NetworkConfig.Prefabs.Add(validNetworkPrefab);
            }

            // Start the instances
            if (!NetcodeIntegrationTestHelpers.Start(false, server, clients))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            // [Client-Side] Wait for a connection to the server
            yield return NetcodeIntegrationTestHelpers.WaitForClientsConnected(clients, null, 512);

            // [Host-Side] Check to make sure all clients are connected
            yield return NetcodeIntegrationTestHelpers.WaitForClientsConnectedToServer(server, clients.Length, null, 512);

            var serverObject = Object.Instantiate(m_Prefab, Vector3.zero, Quaternion.identity);
            NetworkObject serverNetworkObject = serverObject.GetComponent<NetworkObject>();
            serverNetworkObject.NetworkManagerOwner = server;
            serverNetworkObject.Spawn();

            // Wait until all objects have spawned.
            const int maxFrames = 240;
            var doubleCheckTime = Time.realtimeSinceStartup + 5.0f;
            while (ShutdownDuringOnNetworkSpawnBehaviour.SpawnCount < clients.Length + 1)
            {
                if (Time.frameCount > maxFrames)
                {
                    // This is here in the event a platform is running at a higher
                    // frame rate than expected
                    if (doubleCheckTime < Time.realtimeSinceStartup)
                    {
                        Assert.Fail("Did not successfully call all expected client RPCs");
                        break;
                    }
                }
                var nextFrameNumber = Time.frameCount + 1;
                yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);
            }

            Assert.AreEqual(ShutdownDuringOnNetworkSpawnBehaviour.SpawnCount, clients.Length + 1);
            // Extra frames to catch Native Container memory leak log message
            var lastFrameNumber = Time.frameCount + 10;
            yield return new WaitUntil(() => Time.frameCount >= lastFrameNumber);
            Object.Destroy(serverObject);
        }

        [UnityTest]
        public IEnumerator WhenNetworkManagerShutsDownWhileTriggeredMessagesArePending_MemoryDoesNotLeak()
        {
            yield return RunTest();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WhenNetworkManagerShutsDownWhileTriggeredMessagesArePending_MessagesAreStillProcessed()
        {
            yield return RunTest();
            Assert.AreEqual(1, ShutdownDuringOnNetworkSpawnBehaviour.ClientRpcsCalled);
            Assert.AreEqual(1, ShutdownDuringOnNetworkSpawnBehaviour.ServerRpcsCalled);
        }

        [UnityTest]
        public IEnumerator WhenNetworkManagerShutsDownImmediatelyWhileTriggeredMessagesArePending_MessagesAreNotProcessed()
        {
            ShutdownDuringOnNetworkSpawnBehaviour.ShutdownImmediately = true;
            yield return RunTest();
            Assert.AreEqual(0, ShutdownDuringOnNetworkSpawnBehaviour.ClientRpcsCalled);
            Assert.AreEqual(0, ShutdownDuringOnNetworkSpawnBehaviour.ServerRpcsCalled);
        }
    }
}
