using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkObjectSpawnManyObjectsTests
    {
        // "many" in this case means enough to exceed a ushort_max message size written in the header
        // 1500 is not a magic number except that it's big enough to trigger a failure
        private const int k_SpawnedObjects = 1500;

        [UnityTest]
        // When this test fails it does so without an exception and will wait the default ~6 minutes
        [Timeout(10000)]
        public IEnumerator WhenManyObjectsAreSpawnedAtOnce_AllAreReceived()
        {
            MultiInstanceHelpers.Create(1, out NetworkManager server, out NetworkManager[] clients);

            // create prefab
            var gameObject = new GameObject("TestObject");
            var networkObject = gameObject.AddComponent<NetworkObject>();
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkObject);

            server.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab()
            {
                Prefab = gameObject
            });

            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab()
                {
                    Prefab = gameObject
                });
            }

            MultiInstanceHelpers.Start(false, server, clients);

            for (int i = 0; i < k_SpawnedObjects; i++)
            {
                NetworkObject serverObject = Object.Instantiate(gameObject).GetComponent<NetworkObject>();
                serverObject.NetworkManagerOwner = server;
                serverObject.Spawn();
            }

            // wait for connection on client side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients, null, 10));

            // wait for connection on server side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnectedToServer(server, null, 10));

            // ensure all objects are replicated
            Assert.AreEqual(k_SpawnedObjects, clients[0].SpawnManager.SpawnedObjectsList.Count);
            MultiInstanceHelpers.Destroy();
        }
    }
}
