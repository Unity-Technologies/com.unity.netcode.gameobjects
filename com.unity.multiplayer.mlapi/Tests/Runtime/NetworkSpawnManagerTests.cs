using System;
using System.Collections;
using MLAPI.Exceptions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = System.Object;

namespace MLAPI.RuntimeTests
{
    public class NetworkSpawnManagerTests
    {
        private NetworkManager m_ServerNetworkManager;
        private NetworkManager[] m_ClientNetworkManagers;
        private GameObject m_PlayerPrefab;
        private int m_OriginalTargetFrameRate;

        private ulong serverSideClientId => m_ServerNetworkManager.ServerClientId;
        private ulong clientSideClientId => m_ClientNetworkManagers[0].LocalClientId;
        private ulong otherClientSideClientId => m_ClientNetworkManagers[1].LocalClientId;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            // Just always track the current target frame rate (will be re-applied upon TearDown)
            m_OriginalTargetFrameRate = Application.targetFrameRate;

            // Since we use frame count as a metric, we need to assure it runs at a "common update rate"
            // between platforms (i.e. Ubuntu seems to run at much higher FPS when set to -1)
            if (Application.targetFrameRate < 0 || Application.targetFrameRate > 120)
            {
                Application.targetFrameRate = 120;
            }

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
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnectedToServer(server, clientCount: 3));
        }

        [Test]
        public void TestServerCanAccessItsOwnPlayer()
        {
            // server can access its own player
            var serverSideServerPlayerObject = m_ServerNetworkManager.SpawnManager.GetPlayerNetworkObject(serverSideClientId);
            Assert.NotNull(serverSideServerPlayerObject);
            Assert.AreEqual(serverSideClientId, serverSideServerPlayerObject.OwnerClientId);
        }

        [Test]
        public void TestServerCanAccessOtherPlayers()
        {
            // server can access other players
            var serverSideClientPlayerObject = m_ServerNetworkManager.SpawnManager.GetPlayerNetworkObject(clientSideClientId);
            Assert.NotNull(serverSideClientPlayerObject);
            Assert.AreEqual(clientSideClientId, serverSideClientPlayerObject.OwnerClientId);

            var serverSideOtherClientPlayerObject = m_ServerNetworkManager.SpawnManager.GetPlayerNetworkObject(otherClientSideClientId);
            Assert.NotNull(serverSideOtherClientPlayerObject);
            Assert.AreEqual(otherClientSideClientId, serverSideOtherClientPlayerObject.OwnerClientId);
        }

        [Test]
        public void TestClientCantAccessServerPlayer()
        {
            // client can't access server player
            Assert.Throws<NotServerException>(() =>
            {
                m_ClientNetworkManagers[0].SpawnManager.GetPlayerNetworkObject(serverSideClientId);
            });
        }

        [Test]
        public void TestClientCanAccessOwnPlayer()
        {
            // client can access own player
            var clientSideClientPlayerObject = m_ClientNetworkManagers[0].SpawnManager.GetPlayerNetworkObject(clientSideClientId);
            Assert.NotNull(clientSideClientPlayerObject);
            Assert.AreEqual(clientSideClientId, clientSideClientPlayerObject.OwnerClientId);
        }

        [Test]
        public void TestClientCantAccessOtherPlayer()
        {
            // client can't access other player
            Assert.Throws<NotServerException>(() =>
            {
                m_ClientNetworkManagers[0].SpawnManager.GetPlayerNetworkObject(otherClientSideClientId);
            });
        }

        [Test]
        public void TestServerGetsNullValueIfInvalidId()
        {
            // server gets null value if invalid id
            var nullPlayer = m_ServerNetworkManager.SpawnManager.GetPlayerNetworkObject(9999);
            Assert.Null(nullPlayer);
        }

        [Test]
        public void TestServerCanUseGetLocalPlayerObject()
        {
            // test server can use GetLocalPlayerObject
            var serverSideServerPlayerObject = m_ServerNetworkManager.SpawnManager.GetLocalPlayerObject();
            Assert.NotNull(serverSideServerPlayerObject);
            Assert.AreEqual(serverSideClientId, serverSideServerPlayerObject.OwnerClientId);
        }

        [Test]
        public void TestClientCanUseGetLocalPlayerObject()
        {
            // test client can use GetLocalPlayerObject
            var clientSideClientPlayerObject = m_ClientNetworkManagers[0].SpawnManager.GetLocalPlayerObject();
            Assert.NotNull(clientSideClientPlayerObject);
            Assert.AreEqual(clientSideClientId, clientSideClientPlayerObject.OwnerClientId);
        }

        [UnityTest]
        public IEnumerator TestConnectAndDisconnect()
        {
            // test when client connects, player object is now available

            // connect new client
            if (!MultiInstanceHelpers.CreateNewClients(1, out NetworkManager[] clients))
            {
                Debug.LogError("Failed to create instances");
                Assert.Fail("Failed to create instances");
            }
            var newClientNetworkManager = clients[0];
            newClientNetworkManager.NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            newClientNetworkManager.StartClient();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnected(newClientNetworkManager));
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => m_ServerNetworkManager.ConnectedClients.ContainsKey(newClientNetworkManager.LocalClientId)));
            var newClientLocalClientId = newClientNetworkManager.LocalClientId;

            // test new client can get that itself locally
            var newPlayerObject = newClientNetworkManager.SpawnManager.GetLocalPlayerObject();
            Assert.NotNull(newPlayerObject);
            Assert.AreEqual(newClientLocalClientId, newPlayerObject.OwnerClientId);
            // test server can get that new client locally
            var serverSideNewClientPlayer = m_ServerNetworkManager.SpawnManager.GetPlayerNetworkObject(newClientLocalClientId);
            Assert.NotNull(serverSideNewClientPlayer);
            Assert.AreEqual(newClientLocalClientId, serverSideNewClientPlayer.OwnerClientId);

            // test when client disconnects, player object no longer available.
            var nbConnectedClients = m_ServerNetworkManager.ConnectedClients.Count;
            MultiInstanceHelpers.StopOneClient(newClientNetworkManager);
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => m_ServerNetworkManager.ConnectedClients.Count == nbConnectedClients - 1));
            serverSideNewClientPlayer = m_ServerNetworkManager.SpawnManager.GetPlayerNetworkObject(newClientLocalClientId);
            Assert.Null(serverSideNewClientPlayer);
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            // Shutdown and clean up both of our NetworkManager instances
            MultiInstanceHelpers.Destroy();
            UnityEngine.Object.Destroy(m_PlayerPrefab);

            // Set the application's target frame rate back to its original value
            Application.targetFrameRate = m_OriginalTargetFrameRate;
            yield return new WaitForSeconds(0); // wait for next frame so everything is destroyed, so following tests can execute from clean environment
        }
    }
}
