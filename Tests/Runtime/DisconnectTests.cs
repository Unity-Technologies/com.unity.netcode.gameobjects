using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    public class DisconnectTests
    {

        private bool m_ClientDisconnected;
        [UnityTest]
        public IEnumerator RemoteDisconnectPlayerObjectCleanup()
        {
            // create server and client instances
            NetcodeIntegrationTestHelpers.Create(1, out NetworkManager server, out NetworkManager[] clients);

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

            // disconnect the remote client
            m_ClientDisconnected = false;
            server.DisconnectClient(clients[0].LocalClientId);
            clients[0].OnClientDisconnectCallback += OnClientDisconnectCallback;
            var timeoutHelper = new TimeoutHelper();
            yield return NetcodeIntegrationTest.WaitForConditionOrTimeOut(() => m_ClientDisconnected, timeoutHelper);

            // We need to do this to remove other associated client properties/values from NetcodeIntegrationTestHelpers
            NetcodeIntegrationTestHelpers.StopOneClient(clients[0]);

            // ensure the object was destroyed
            Assert.False(server.SpawnManager.SpawnedObjects.Any(x => x.Value.IsPlayerObject && x.Value.OwnerClientId == clients[0].LocalClientId));

            // cleanup
            NetcodeIntegrationTestHelpers.Destroy();
        }

        private void OnClientDisconnectCallback(ulong obj)
        {
            m_ClientDisconnected = true;
        }

        [UnityTest]
        public IEnumerator ClientDisconnectPlayerObjectCleanup()
        {
            // create server and client instances
            NetcodeIntegrationTestHelpers.Create(1, out NetworkManager server, out NetworkManager[] clients);

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

            // disconnect the remote client
            m_ClientDisconnected = false;

            server.OnClientDisconnectCallback += OnClientDisconnectCallback;

            var serverSideClientPlayer = server.ConnectedClients[clients[0].LocalClientId].PlayerObject;

            // Stopping the client is the same as the client disconnecting
            NetcodeIntegrationTestHelpers.StopOneClient(clients[0]);

            var timeoutHelper = new TimeoutHelper();
            yield return NetcodeIntegrationTest.WaitForConditionOrTimeOut(() => m_ClientDisconnected, timeoutHelper);

            // ensure the object was destroyed
            Assert.True(serverSideClientPlayer.IsOwnedByServer, $"The client's player object's ownership was not transferred back to the server!");

            // cleanup
            NetcodeIntegrationTestHelpers.Destroy();
        }
    }
}
