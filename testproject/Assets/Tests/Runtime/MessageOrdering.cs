using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TestProject.RuntimeTests.Support;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    public class MessageOrderingTests
    {
        private GameObject m_Prefab;

        private NetworkManager m_ServerNetworkManager;
        private NetworkManager[] m_ClientNetworkManagers;


        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Make sure these static values are reset
            Support.SpawnRpcDespawn.ClientUpdateCount = 0;
            Support.SpawnRpcDespawn.ServerUpdateCount = 0;
            Support.SpawnRpcDespawn.ClientNetworkSpawnRpcCalled = false;
            Support.SpawnRpcDespawn.ExecuteClientRpc = false;
            yield break;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            // Shutdown and clean up both of our NetworkManager instances
            if (m_Prefab)
            {
                Object.Destroy(m_Prefab);
                m_Prefab = null;
                Support.SpawnRpcDespawn.ClientUpdateCount = 0;
                Support.SpawnRpcDespawn.ServerUpdateCount = 0;
                Support.SpawnRpcDespawn.ClientNetworkSpawnRpcCalled = false;
                Support.SpawnRpcDespawn.ExecuteClientRpc = false;
            }

            NetcodeIntegrationTestHelpers.Destroy();
            yield break;
        }

        [UnityTest]
        public IEnumerator SpawnChangeOwnership()
        {
            const int numClients = 1;
            Assert.True(NetcodeIntegrationTestHelpers.Create(numClients, out NetworkManager server, out NetworkManager[] clients));
            m_Prefab = new GameObject("Object");
            var networkObject = m_Prefab.AddComponent<NetworkObject>();
            m_Prefab.AddComponent<NetworkObjectTestComponent>();

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
            if (!NetcodeIntegrationTestHelpers.Start(true, server, clients))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            // [Client-Side] Wait for a connection to the server
            yield return NetcodeIntegrationTestHelpers.WaitForClientsConnected(clients, null, 512);

            // [Host-Side] Check to make sure all clients are connected
            yield return NetcodeIntegrationTestHelpers.WaitForClientsConnectedToServer(server, clients.Length + 1, null, 512);

            var serverObject = Object.Instantiate(m_Prefab, Vector3.zero, Quaternion.identity);
            NetworkObject serverNetworkObject = serverObject.GetComponent<NetworkObject>();
            serverNetworkObject.NetworkManagerOwner = server;
            serverNetworkObject.Spawn();
            serverNetworkObject.ChangeOwnership(clients[0].LocalClientId);

            // Wait until all objects have spawned.
            var timeoutHelper = new TimeoutHelper();
            yield return NetcodeIntegrationTest.WaitForConditionOrTimeOut(() => NetworkObjectTestComponent.SpawnedInstances.Count == numClients + 1);
            Assert.False(timeoutHelper.TimedOut, "Did not successfully spawn all expected NetworkObjects");
        }

        [UnityTest]
        public IEnumerator SpawnRpcDespawn()
        {
            var frameCountStart = Time.frameCount;
            // Must be 1 for this test.
            const int numClients = 1;
            Assert.True(NetcodeIntegrationTestHelpers.Create(numClients, out m_ServerNetworkManager, out m_ClientNetworkManagers));
            m_Prefab = new GameObject("Object");
            m_Prefab.AddComponent<SpawnRpcDespawn>();
            Support.SpawnRpcDespawn.TestStage = NetworkUpdateStage.EarlyUpdate;
            var networkObject = m_Prefab.AddComponent<NetworkObject>();

            // Make it a prefab
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);
            var validNetworkPrefab = new NetworkPrefab
            {
                Prefab = m_Prefab
            };
            m_ServerNetworkManager.NetworkConfig.Prefabs.Add(validNetworkPrefab);
            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.Prefabs.Add(validNetworkPrefab);
            }

            var clientHandlers = new List<SpawnRpcDespawnInstanceHandler>();
            //var handler = new SpawnRpcDespawnInstanceHandler(networkObject.GlobalObjectIdHash);
            //server.PrefabHandler.AddHandler(networkObject.GlobalObjectIdHash, handler);
            foreach (var client in m_ClientNetworkManagers)
            {
                var clientHandler = new SpawnRpcDespawnInstanceHandler(networkObject.GlobalObjectIdHash);
                client.PrefabHandler.AddHandler(networkObject, clientHandler);
                clientHandlers.Add(clientHandler);
            }

            // Start the instances
            if (!NetcodeIntegrationTestHelpers.Start(true, m_ServerNetworkManager, m_ClientNetworkManagers))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            // [Client-Side] Wait for a connection to the server
            yield return NetcodeIntegrationTestHelpers.WaitForClientsConnected(m_ClientNetworkManagers, null, 512);

            // [Host-Side] Check to make sure all clients are connected
            yield return NetcodeIntegrationTestHelpers.WaitForClientsConnectedToServer(m_ServerNetworkManager, m_ClientNetworkManagers.Length + 1, null, 512);

            var serverObject = Object.Instantiate(m_Prefab, Vector3.zero, Quaternion.identity);
            NetworkObject serverNetworkObject = serverObject.GetComponent<NetworkObject>();
            serverNetworkObject.NetworkManagerOwner = m_ServerNetworkManager;
            serverNetworkObject.Spawn();

            SpawnRpcDespawn srdComponent = serverObject.GetComponent<SpawnRpcDespawn>();
            srdComponent.Activate();

            // Wait until all objects have spawned.
            int expectedCount = Support.SpawnRpcDespawn.ClientUpdateCount + numClients + 1; // Clients plus host
            int maxFrames = 240 + Time.frameCount;
            var doubleCheckTime = Time.realtimeSinceStartup + 5.0f;
            var clientCountReached = false;
            var allHandlersSpawned = false;
            var allHandlersDestroyed = false;
            var waitForTick = new WaitForSeconds(1.0f / m_ServerNetworkManager.NetworkConfig.TickRate);

            while (!(allHandlersSpawned && clientCountReached && allHandlersDestroyed))
            {
                clientCountReached = (Support.SpawnRpcDespawn.ClientUpdateCount == expectedCount);
                foreach (var clientHandler in clientHandlers)
                {
                    allHandlersSpawned = clientHandler.WasSpawned;
                    allHandlersDestroyed = clientHandler.WasDestroyed;
                    if (!allHandlersSpawned || !allHandlersDestroyed)
                    {
                        break;
                    }
                }

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

                yield return waitForTick;
            }

            Assert.True(allHandlersSpawned, $"Not all client-side handlers were spawned!");
            Assert.True(allHandlersDestroyed, $"Not all client-side handlers were destroyed!");
            Assert.True(clientCountReached, $"Client count ({Support.SpawnRpcDespawn.ClientUpdateCount}) did not match the expected count ({expectedCount})");

            Debug.Log($"It took {Time.frameCount - frameCountStart} frames to process the MessageOrdering.SpawnRpcDespawn integration test.");
        }

        [UnityTest]
        public IEnumerator RpcOnNetworkSpawn()
        {
            Support.SpawnRpcDespawn.ExecuteClientRpc = true;
            // Must be 1 for this test.
            const int numClients = 1;
            Assert.True(NetcodeIntegrationTestHelpers.Create(numClients, out NetworkManager server, out NetworkManager[] clients));
            m_Prefab = new GameObject("Object");
            m_Prefab.AddComponent<SpawnRpcDespawn>();
            Support.SpawnRpcDespawn.TestStage = NetworkUpdateStage.EarlyUpdate;
            var networkObject = m_Prefab.AddComponent<NetworkObject>();

            // Make it a prefab
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);
            var handlers = new List<SpawnRpcDespawnInstanceHandler>();
            var handler = new SpawnRpcDespawnInstanceHandler(networkObject.GlobalObjectIdHash);

            // We *must* always add a unique handler to both the server and the clients
            server.PrefabHandler.AddHandler(networkObject, handler);
            handlers.Add(handler);
            foreach (var client in clients)
            {
                // Create a unique SpawnRpcDespawnInstanceHandler per client
                handler = new SpawnRpcDespawnInstanceHandler(networkObject.GlobalObjectIdHash);
                handlers.Add(handler);
                client.PrefabHandler.AddHandler(networkObject, handler);
            }

            var validNetworkPrefab = new NetworkPrefab
            {
                Prefab = m_Prefab
            };
            server.NetworkConfig.Prefabs.Add(validNetworkPrefab);
            foreach (var client in clients)
            {
                client.NetworkConfig.Prefabs.Add(validNetworkPrefab);
            }

            var waitForTickInterval = new WaitForSeconds(1.0f / server.NetworkConfig.TickRate);

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
            while (!Support.SpawnRpcDespawn.ClientNetworkSpawnRpcCalled)
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

            Assert.True(handler.WasSpawned);
            Assert.True(Support.SpawnRpcDespawn.ClientNetworkSpawnRpcCalled);

            // Despawning the server-side NetworkObject will invoke the handler's OnDestroy method
            serverNetworkObject.Despawn();
            yield return waitForTickInterval;

            var hasTimedOut = false;
            var timeOutPeriod = Time.realtimeSinceStartup + 2.0f;
            var allHandlersDestroyed = false;
            while (!allHandlersDestroyed && !hasTimedOut)
            {
                allHandlersDestroyed = true;
                foreach (var handlerInstance in handlers)
                {
                    if (!handlerInstance.WasDestroyed)
                    {
                        allHandlersDestroyed = false;
                        break;
                    }
                }
                hasTimedOut = timeOutPeriod < Time.realtimeSinceStartup;
                yield return waitForTickInterval;
            }

            Assert.False(hasTimedOut, "Timed out waiting for handlers to be destroyed");
        }
    }
}
