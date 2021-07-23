using System;
using System.Collections;
using MLAPI;
using MLAPI.Configuration;
using MLAPI.RuntimeTests;
using NUnit.Framework;
using TestProject.RuntimeTests.Support;
using UnityEngine;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    public class FixedUpdateMessagesAreOnlyProcessedOnceTest
    {
        private GameObject m_Prefab;
        private int m_originalFrameRate;


        [UnityTearDown]
        public IEnumerator Teardown()
        {
            // Shutdown and clean up both of our NetworkManager instances
            if (m_Prefab)
            {
                MultiInstanceHelpers.Destroy();
                UnityEngine.Object.Destroy(m_Prefab);
                m_Prefab = null;
                Support.SpawnRpcDespawn.ClientUpdateCount = 0;
                Support.SpawnRpcDespawn.ServerUpdateCount = 0;
                Application.targetFrameRate = m_originalFrameRate;
            }
            yield break;
        }
        
        [UnitySetUp]
        public IEnumerator Setup()
        {
            m_originalFrameRate = Application.targetFrameRate;
            yield break;
        }

        [UnityTest]
        public IEnumerator TestFixedUpdateMessagesAreOnlyProcessedOnce()
        {
            NetworkUpdateStage testStage = NetworkUpdateStage.FixedUpdate;
            
            // Must be 1 for this test.
            const int numClients = 1;
            Assert.True(MultiInstanceHelpers.Create(numClients, out NetworkManager server, out NetworkManager[] clients));
            m_Prefab = new GameObject("Object");
            m_Prefab.AddComponent<SpawnRpcDespawn>();
            Support.SpawnRpcDespawn.TestStage = testStage;
            var networkObject = m_Prefab.AddComponent<NetworkObject>();

            // Make it a prefab
            MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(networkObject, 1);
            var handler = new SpawnRpcDespawnInstanceHandler(1);
            foreach (var client in clients)
            {
                client.PrefabHandler.AddHandler(networkObject, handler);
            }

            var validNetworkPrefab = new NetworkPrefab();
            validNetworkPrefab.Prefab = m_Prefab;
            server.NetworkConfig.NetworkPrefabs.Add(validNetworkPrefab);
            foreach (var client in clients)
            {
                client.NetworkConfig.NetworkPrefabs.Add(validNetworkPrefab);
            }

            // Start the instances
            if (!MultiInstanceHelpers.Start(true, server, clients))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            // [Client-Side] Wait for a connection to the server
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients, null, 512));

            // [Host-Side] Check to make sure all clients are connected
            yield return MultiInstanceHelpers.Run(
                MultiInstanceHelpers.WaitForClientsConnectedToServer(server, clients.Length + 1, null, 512));

            // Setting targetFrameRate to 1 will cause FixedUpdate to get called multiple times.
            // We should only see ClientUpdateCount increment once because only one RPC is being sent.
            Application.targetFrameRate = 1;
            GameObject serverObject = GameObject.Instantiate(m_Prefab, Vector3.zero, Quaternion.identity);
            NetworkObject serverNetworkObject = serverObject.GetComponent<NetworkObject>();
            serverNetworkObject.NetworkManagerOwner = server;
            SpawnRpcDespawn srdComponent = serverObject.GetComponent<SpawnRpcDespawn>();
            srdComponent.Activate();

            // Wait until all objects have spawned.
            int expectedCount = Support.SpawnRpcDespawn.ClientUpdateCount + 1;
            const int maxFrames = 240;
            var doubleCheckTime = Time.realtimeSinceStartup + 1.0f;
            while (Support.SpawnRpcDespawn.ClientUpdateCount < expectedCount)
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

            Assert.AreEqual(testStage, Support.SpawnRpcDespawn.StageExecutedByReceiver);
            Assert.AreEqual(Support.SpawnRpcDespawn.ServerUpdateCount, Support.SpawnRpcDespawn.ClientUpdateCount);
            Assert.True(handler.WasSpawned);
            Assert.True(handler.WasDestroyed);
        }
    }
}
