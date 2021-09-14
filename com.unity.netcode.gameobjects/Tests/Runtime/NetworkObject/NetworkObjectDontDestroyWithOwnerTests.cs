using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkObjectDontDestroyWithOwnerTests
    {
        [UnityTest]
        public IEnumerator DontDestroyWithOwnerTest()
        {
            // create server and client instances
            MultiInstanceHelpers.Create(1, out NetworkManager server, out NetworkManager[] clients);

            // create prefab
            var gameObject = new GameObject("ClientOwnedObject");
            var networkObject = gameObject.AddComponent<NetworkObject>();
            networkObject.DontDestroyWithOwner = true;
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

            // start server and connect clients
            MultiInstanceHelpers.Start(false, server, clients);

            // wait for connection on client side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients));

            // wait for connection on server side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnectedToServer(server));

            // network objects
            var networkObjects = new List<NetworkObject>();

            // create instances
            for (int i = 0; i < 32; i++)
            {
                var no = Object.Instantiate(gameObject).GetComponent<NetworkObject>();
                no.NetworkManagerOwner = server;
                networkObjects.Add(no);
                no.SpawnWithOwnership(clients[0].LocalClientId);
            }

            // wait for object spawn on client
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => clients[0].SpawnManager.SpawnedObjects.Count == 32));

            // disconnect the client that owns all the clients
            MultiInstanceHelpers.StopOneClient(clients[0]);

            // wait for disconnect
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => server.ConnectedClients.Count == 0));

            for (int i = 0; i < networkObjects.Count; i++)
            {
                // ensure ownership was transferred back
                Assert.That(networkObjects[i].OwnerClientId == server.ServerClientId);
            }

            // cleanup
            MultiInstanceHelpers.Destroy();
        }
    }
}
