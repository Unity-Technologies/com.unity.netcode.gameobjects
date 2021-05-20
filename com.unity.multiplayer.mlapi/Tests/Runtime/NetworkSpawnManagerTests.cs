using System;
using System.Collections;
using MLAPI.Exceptions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests
{
    public class NetworkSpawnManagerTests
    {
        private NetworkManager m_ServerNetworkManager;
        private NetworkManager[] m_ClientNetworkManagers;
        private GameObject m_PlayerPrefab;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            LogAssert.ignoreFailingMessages = true;

            // Create multiple NetworkManager instances
            if (!MultiInstanceHelpers.Create(2, out NetworkManager server, out NetworkManager[] clients))
            {
                Debug.LogError("Failed to create instances");
                Assert.Fail("Failed to create instances");
            }

            m_ServerNetworkManager = server;
            m_ClientNetworkManagers = clients;

            // Create playerPrefab
            m_PlayerPrefab = new GameObject("Player");
            NetworkObject networkObject = m_PlayerPrefab.AddComponent<NetworkObject>();

            // Make it a prefab
            MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(networkObject);

            // Set the player prefab
            server.NetworkConfig.PlayerPrefab = m_PlayerPrefab;

            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            }

            // Start the instances
            if (!MultiInstanceHelpers.Start(true, server, clients))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            // Wait for connection on client side
            for (int i = 0; i < clients.Length; i++)
            {
                yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnected(clients[i]));
            }

            // Wait for connection on server side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnectedToServer(server, nbClients:2));
        }

        [UnityTest]
        public IEnumerator TestClientCanOnlySeeItselfInClientList()
        {
            var serverSideClientId = m_ServerNetworkManager.ServerClientId;
            var clientSideClientId = m_ClientNetworkManagers[0].LocalClientId;
            var otherClientSideClientId = m_ClientNetworkManagers[1].LocalClientId;

            // test 1 server can access its own player
            var serverSideServerPlayerObject = m_ServerNetworkManager.SpawnManager.GetPlayerNetworkObject(serverSideClientId);
            Assert.NotNull(serverSideServerPlayerObject);
            Assert.AreEqual(serverSideClientId, serverSideServerPlayerObject.OwnerClientId);

            // test 2 server can access other players
            var serverSideClientPlayerObject = m_ServerNetworkManager.SpawnManager.GetPlayerNetworkObject(clientSideClientId);
            Assert.NotNull(serverSideClientPlayerObject);
            Assert.AreEqual(clientSideClientId, serverSideClientPlayerObject.OwnerClientId);

            var serverSideOtherClientPlayerObject = m_ServerNetworkManager.SpawnManager.GetPlayerNetworkObject(otherClientSideClientId);
            Assert.NotNull(serverSideOtherClientPlayerObject);
            Assert.AreEqual(otherClientSideClientId, serverSideOtherClientPlayerObject.OwnerClientId);

            // test 3 client can't access server player
            Assert.Throws<NotServerException>(() =>
            {
                m_ClientNetworkManagers[0].SpawnManager.GetPlayerNetworkObject(serverSideClientId);
            });

            // test 4 client can access own player
            var clientSideClientPlayerObject = m_ClientNetworkManagers[0].SpawnManager.GetPlayerNetworkObject(clientSideClientId);
            Assert.NotNull(clientSideClientPlayerObject);
            Assert.AreEqual(clientSideClientId, clientSideClientPlayerObject.OwnerClientId);

            // test 5 client can't access other player
            Assert.Throws<NotServerException>(() =>
            {
                m_ClientNetworkManagers[0].SpawnManager.GetPlayerNetworkObject(otherClientSideClientId);
            });

            // test 6 server gets null value if invalid id
            var nullPlayer = m_ServerNetworkManager.SpawnManager.GetPlayerNetworkObject(9999);
            Assert.Null(nullPlayer);

            // test server can use GetLocalPlayerObject
            serverSideServerPlayerObject = m_ServerNetworkManager.SpawnManager.GetLocalPlayerObject();
            Assert.NotNull(serverSideServerPlayerObject);
            Assert.AreEqual(serverSideClientId, serverSideServerPlayerObject.OwnerClientId);

            // test client can use GetLocalPlayerObject
            clientSideClientPlayerObject = m_ClientNetworkManagers[0].SpawnManager.GetLocalPlayerObject();
            Assert.NotNull(clientSideClientPlayerObject);
            Assert.AreEqual(clientSideClientId, clientSideClientPlayerObject.OwnerClientId);

            // test when client connects, player object is now available
            // Create multiple NetworkManager instances
            if (!MultiInstanceHelpers.Create(1, out NetworkManager server, out NetworkManager[] clients))
            {
                Debug.LogError("Failed to create instances");
                Assert.Fail("Failed to create instances");
            }

            var newClientNetworkManager = clients[0];
            newClientNetworkManager.NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            newClientNetworkManager.StartClient();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnected(newClientNetworkManager));
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnectedToServer(m_ServerNetworkManager, nbClients:3)); // todo use wait for specific client
            var newClientLocalClientId = newClientNetworkManager.LocalClientId;
            var newPlayerObject = newClientNetworkManager.SpawnManager.GetLocalPlayerObject();
            Assert.NotNull(newPlayerObject);
            Assert.AreEqual(newClientLocalClientId, newPlayerObject.OwnerClientId);
            var serverSideNewClientPlayer = m_ServerNetworkManager.SpawnManager.GetPlayerNetworkObject(newClientLocalClientId);
            Assert.NotNull(serverSideNewClientPlayer);
            Assert.AreEqual(newClientLocalClientId, serverSideNewClientPlayer.OwnerClientId);

            // test when client disconnects, player object no longer available.
            newClientNetworkManager.StopClient();
            var nbConnectedClients = m_ServerNetworkManager.ConnectedClients.Count;
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => m_ServerNetworkManager.ConnectedClients.Count == nbConnectedClients - 1));
            serverSideNewClientPlayer = m_ServerNetworkManager.SpawnManager.GetPlayerNetworkObject(newClientLocalClientId);
            Assert.Null(serverSideNewClientPlayer);
        }
    }
}
