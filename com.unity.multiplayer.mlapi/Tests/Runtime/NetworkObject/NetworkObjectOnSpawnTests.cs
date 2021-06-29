using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.RuntimeTests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests
{
    public class NetworkObjectOnSpawnTests
    {
        private GameObject m_Prefab;

        /// <summary>
        /// Tests that instantiating a <see cref="NetworkObject"/> and destroying without spawning it
        /// does not run <see cref="NetworkBehaviour.OnNetworkSpawn"/> or <see cref="NetworkBehaviour.OnNetworkSpawn"/>.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator InstantiateDestroySpawnNotCalled()
        {
            var gameObject = new GameObject("InstantiateDestroySpawnNotCalled_Object");
            var networkObject = gameObject.AddComponent<NetworkObject>();
            var fail = gameObject.AddComponent<FailWhenSpawned>();

            yield return null;

            // destroy
            Object.Destroy(gameObject);
            
            yield return null;
        }

        private class FailWhenSpawned : NetworkBehaviour
        {
            public override void OnNetworkSpawn()
            {
                Assert.Fail("Spawn should not be called on not spawned object");
            }

            public override void OnNetworkDespawn()
            {
                Assert.Fail("Depawn should not be called on not spawned object");
            }
        }

        /// <summary>
        /// Test that callbacks are run for playerobject spawn, despawn, regular spawn, destroy on server.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TestOnNetworkSpawnCallbacks()
        {
            // Create Host and (numClients) clients
            Assert.True(MultiInstanceHelpers.Create(2, out NetworkManager server, out NetworkManager[] clients));

            // Create a default player GameObject to use
            m_Prefab = new GameObject("TestObject");
            var networkObject = m_Prefab.AddComponent<NetworkObject>();

            // add test component
            m_Prefab.AddComponent<TrackOnSpawnFunctions>();

            // Make it a prefab
            MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(networkObject);

            // Set the player prefab
            server.NetworkConfig.PlayerPrefab = m_Prefab;

            // Set all of the client's player prefab
            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].NetworkConfig.PlayerPrefab = m_Prefab;
            }

            // Start the instances
            if (!MultiInstanceHelpers.Start(true, server, clients))
            {
                Assert.Fail("Failed to start instances");
            }

            // [Client-Side] Wait for a connection to the server
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients, null, 512));

            // [Host-Side] Check to make sure all clients are connected
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnectedToServer(server, clients.Length + 1, null, 512));

            // [Host-Side] Get the Host owned instance
            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == clients[0].LocalClientId), server, serverClientPlayerResult));

            var serverInstance = serverClientPlayerResult.Result.GetComponent<TrackOnSpawnFunctions>();

            var clientInstances = new List<TrackOnSpawnFunctions>();
            foreach (var client in clients)
            {
                var clientClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
                yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == clients[0].LocalClientId), client, clientClientPlayerResult));
                var clientRpcTests = clientClientPlayerResult.Result.GetComponent<TrackOnSpawnFunctions>();
                Assert.IsNotNull(clientRpcTests);
                clientInstances.Add(clientRpcTests);
            }

            // -------------- step 1 check player spawn despawn

            // check spawned on server
            Assert.AreEqual(1, serverInstance.OnNetworkSpawnCalledCount);

            // safety check despawned
            Assert.AreEqual(0, serverInstance.OnNetworkDespawnCalledCount);

            // check spawned on client
            foreach (var clientInstance in clientInstances)
            {
                Assert.AreEqual(1, clientInstance.OnNetworkSpawnCalledCount);

                // safety check despawned
                Assert.AreEqual(0, clientInstance.OnNetworkDespawnCalledCount);
            }

            // despawn on server
            serverInstance.GetComponent<NetworkObject>().Despawn();

            // check despawned on server
            Assert.AreEqual(1, serverInstance.OnNetworkDespawnCalledCount);

            // wait long enough for player object to be despawned
            int nextFrameNumber = Time.frameCount + 2;
            yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);

            // check despawned on clients
            foreach (var clientInstance in clientInstances)
            {
                Assert.AreEqual(1, clientInstance.OnNetworkDespawnCalledCount);
            }

            //----------- step 2 check spawn again and destroy

            serverInstance.GetComponent<NetworkObject>().Spawn();

            // wait long enough for player object to be spawned
            nextFrameNumber = Time.frameCount + 2;
            yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);


            // check spawned again on server this is 2 becaue we are reusing the object which was already spawned once.
            Assert.AreEqual(2, serverInstance.OnNetworkSpawnCalledCount);

            // check spawned on client
            foreach (var clientInstance in clientInstances)
            {
                Assert.AreEqual(1, clientInstance.OnNetworkSpawnCalledCount);
            }

            // destroy the server object
            Object.Destroy(serverInstance.gameObject);

            // wait one frame for destroy to kick in
            yield return null;

            // check whether despawned was called again on server instance
            Assert.AreEqual(2, serverInstance.OnNetworkDespawnCalledCount);

            // wait long enough for player object to be despawned on client
            nextFrameNumber = Time.frameCount + 2;
            yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);

            // check despawned on clients
            foreach (var clientInstance in clientInstances)
            {
                Assert.AreEqual(1, clientInstance.OnNetworkDespawnCalledCount);
            }

            // Shutdown and clean up both of our NetworkManager instances
            MultiInstanceHelpers.Destroy();
        }

        private class TrackOnSpawnFunctions : NetworkBehaviour
        {
            public int OnNetworkSpawnCalledCount { get; private set; }
            public int OnNetworkDespawnCalledCount { get; private set; }

            public override void OnNetworkSpawn()
            {
                OnNetworkSpawnCalledCount++;
            }

            public override void OnNetworkDespawn()
            {
                OnNetworkDespawnCalledCount++;
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Prefab != null)
            {
                Object.Destroy(m_Prefab);
                m_Prefab = null;
            }
        }
    }
}
