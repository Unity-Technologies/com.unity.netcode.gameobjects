using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

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
            clients[0].OnClientDisconnectCallback += OnClientDisconnectCallback;

            server.DisconnectClient(clients[0].LocalClientId);

            var timeoutHelper = new TimeoutHelper();
            yield return NetcodeIntegrationTest.WaitForConditionOrTimeOut(() => m_ClientDisconnected, timeoutHelper);
            if (timeoutHelper.TimedOut)
            {
                // If we fail, clean up or all other tests proceeding this test could fail!
                CleanupTest(clients[0]);
                // Force the assert
                Assert.IsFalse(timeoutHelper.TimedOut, "Timed out waiting for client to disconnect!");
            }

            // ensure the object was destroyed by looking for any player objects with the client's assigned id
            NetcodeIntegrationTest.WaitForConditionOrTimeOut(() => !server.SpawnManager.SpawnedObjects.Any(x => x.Value.IsPlayerObject && x.Value.OwnerClientId == clients[0].LocalClientId), timeoutHelper);
            if (timeoutHelper.TimedOut)
            {
                // If we fail, clean up or all other tests proceeding this test could fail!
                CleanupTest(clients[0]);
                // Force the assert
                Assert.IsFalse(timeoutHelper.TimedOut, "Timed out waiting for client's player object to be destroyed!");
            }

            CleanupTest(clients[0]);
        }

        private void CleanupTest(NetworkManager networkManager = null)
        {
            if (networkManager != null)
            {
                // We need to do this to remove other associated client properties/values from NetcodeIntegrationTestHelpers
                NetcodeIntegrationTestHelpers.StopOneClient(networkManager);
            }
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
            var timeoutHelper = new TimeoutHelper();

            // Stopping the client is the same as the client disconnecting
            NetcodeIntegrationTestHelpers.StopOneClient(clients[0]);

            yield return NetcodeIntegrationTest.WaitForConditionOrTimeOut(() => m_ClientDisconnected, timeoutHelper);

            if (timeoutHelper.TimedOut)
            {
                // If we fail, clean up or all other tests proceeding this test could fail!
                CleanupTest();
                // Force the assert
                Assert.IsFalse(timeoutHelper.TimedOut, "Timed out waiting for client to disconnect!");
            }

            // ensure the player object's ownership was transferred back to the server
            NetcodeIntegrationTest.WaitForConditionOrTimeOut(() => serverSideClientPlayer.IsOwnedByServer, timeoutHelper);
            if (timeoutHelper.TimedOut)
            {
                // If we fail, clean up or all other tests proceeding this test could fail!
                CleanupTest();
                // Force the assert
                Assert.IsFalse(timeoutHelper.TimedOut, "The client's player object's ownership was not transferred back to the server!");
            }

            // cleanup
            CleanupTest();
        }
    }
}
