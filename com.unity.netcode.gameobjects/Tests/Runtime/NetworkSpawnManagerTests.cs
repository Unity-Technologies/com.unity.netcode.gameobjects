using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkSpawnManagerTests : BaseMultiInstanceTest
    {
        private ulong serverSideClientId => m_ServerNetworkManager.ServerClientId;
        private ulong clientSideClientId => m_ClientNetworkManagers[0].LocalClientId;
        private ulong otherClientSideClientId => m_ClientNetworkManagers[1].LocalClientId;

        protected override int NbClients => 2;

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
    }
}
