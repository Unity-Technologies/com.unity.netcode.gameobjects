using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkObjectNetworkClientOwnedObjectsTests
    {
        [UnityTest]
        public IEnumerator ChangeOwnershipOwnedObjectsAddTest()
        {
            // create server and client instances
            MultiInstanceHelpers.Create(1, out NetworkManager server, out NetworkManager[] clients);

            // create prefab
            var gameObject = new GameObject("ClientOwnedObject");
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

            // start server and connect clients
            MultiInstanceHelpers.Start(false, server, clients);

            // wait for connection on client side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients));

            // wait for connection on server side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnectedToServer(server));

            NetworkObject serverObject = Object.Instantiate(gameObject).GetComponent<NetworkObject>();
            serverObject.NetworkManagerOwner = server;
            serverObject.Spawn();

            // The object is owned by server
            Assert.False(server.ConnectedClients[clients[0].LocalClientId].OwnedObjects.Any(x => x.NetworkObjectId == serverObject.NetworkObjectId));

            // Change the ownership
            serverObject.ChangeOwnership(clients[0].LocalClientId);

            // Ensure it's now added to the list
            Assert.True(server.ConnectedClients[clients[0].LocalClientId].OwnedObjects.Any(x => x.NetworkObjectId == serverObject.NetworkObjectId));

            MultiInstanceHelpers.Destroy();
        }
    }
}
