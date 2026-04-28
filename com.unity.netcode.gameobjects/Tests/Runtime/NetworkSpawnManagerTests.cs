using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Host)]
    internal class NetworkSpawnManagerTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        public NetworkSpawnManagerTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        [Test]
        public void TestGetPlayerNetworkObject()
        {
            foreach (var toTest in m_NetworkManagers)
            {
                foreach (var toGet in m_NetworkManagers)
                {
                    // GetPlayerNetworkObject should only be able to get the object
                    // - When using DA
                    // - When testing the Server NetworkManager
                    // - And when the client is getting their own PlayerObject.
                    var canFetch = m_DistributedAuthority || toTest.IsServer || toTest == toGet;

                    if (!canFetch)
                    {
                        LogAssert.Expect(UnityEngine.LogType.Error, new Regex("Only the server can find player objects from other clients."));
                    }

                    var playerObject = toTest.SpawnManager.GetPlayerNetworkObject(toGet.LocalClientId);

                    if (canFetch)
                    {
                        Assert.That(playerObject, Is.Not.Null);
                        Assert.That(toGet.LocalClientId, Is.EqualTo(playerObject.OwnerClientId));
                    }
                    else
                    {
                        Assert.That(playerObject, Is.Null);
                    }
                }
            }

            // finally, test that an invalid clientId returns a null object
            var invalid = GetAuthorityNetworkManager().SpawnManager.GetPlayerNetworkObject(9999);
            Assert.That(invalid, Is.Null);
        }

        [Test]
        public void TestGetLocalPlayerObject()
        {
            foreach (var manager in m_NetworkManagers)
            {
                var playerObject = manager.SpawnManager.GetLocalPlayerObject();
                Assert.That(playerObject, Is.Not.Null);
                Assert.That(manager.LocalClientId, Is.EqualTo(playerObject.OwnerClientId));
                Assert.That(manager.LocalClient.PlayerObject, Is.EqualTo(playerObject));
            }
        }

        public enum DestroyWithOwner
        {
            DestroyWithOwner,
            DontDestroyWithOwner
        }

        [UnityTest]
        public IEnumerator TestPlayerPrefabConnectAndDisconnect([Values] DestroyWithOwner destroySetting)
        {
            var destroyWithOwner = destroySetting == DestroyWithOwner.DestroyWithOwner;
            var authority = GetAuthorityNetworkManager();
            // Regression test: Ensure the authority's player object is set
            Assert.That(authority.LocalClient.PlayerObject != null, Is.True, "The server should have a player object!");

            // Mark PlayerPrefab as DontDestroyWithOwner
            m_PlayerPrefab.GetComponent<NetworkObject>().DontDestroyWithOwner = !destroyWithOwner;

            // test when client connects, player object is now available
            var newClient = CreateNewClient();
            yield return StartClient(newClient);
            var newClientId = newClient.LocalClientId;

            // test new client can get that itself locally
            var newPlayerObject = newClient.SpawnManager.GetLocalPlayerObject();
            Assert.NotNull(newPlayerObject);
            Assert.AreEqual(newClientId, newPlayerObject.OwnerClientId);

            // test server can get that new client locally
            var serverSideNewClientPlayer = authority.SpawnManager.GetPlayerNetworkObject(newClientId);
            Assert.NotNull(serverSideNewClientPlayer);
            Assert.AreEqual(newClientId, serverSideNewClientPlayer.OwnerClientId);

            // test when client disconnects, player object no longer available.
            var nbConnectedClients = authority.ConnectedClients.Count;

            if (!m_DistributedAuthority)
            {
                authority.DisconnectClient(newClientId);

                yield return WaitForConditionOrTimeOut(() => !newClient.IsConnectedClient);
                AssertOnTimeout("Timed out waiting for client to disconnect");
            }

            // Call this to clean up NetcodeIntegrationTestHelpers
            yield return StopOneClient(newClient);

            Assert.AreEqual(authority.ConnectedClients.Count, nbConnectedClients - 1);
            Assert.True(newPlayerObject == null, "The client's player object should have been destroyed!");

            if (destroyWithOwner)
            {
                Assert.That(serverSideNewClientPlayer == null, Is.True, "The server's version of the client's player object should have been destroyed");
            }
            else
            {
                Assert.That(serverSideNewClientPlayer == null, Is.False, "The server's version of the client's player object shouldn't have been destroyed!");
                var newOwner = authority;
                if (m_UseCmbService)
                {
                    // The CMB service will transfer ownership to another connected client
                    Assert.That(serverSideNewClientPlayer.OwnerClientId, Is.Not.EqualTo(newClientId), "Ownership should have been removed!");
                    newOwner = GetOwningNetworkManager(serverSideNewClientPlayer);
                }
                Assert.That(serverSideNewClientPlayer.OwnerClientId, Is.EqualTo(newOwner.LocalClientId), "Ownership should have transferred to the authority!");
                Assert.That(serverSideNewClientPlayer.IsPlayerObject, Is.True, $"{nameof(NetworkObject.IsPlayerObject)} should still be set!");

                // Requesting the player's object after they've left should still while the object hasn't been destroyed
                var playerObject = newOwner.SpawnManager.GetPlayerNetworkObject(newClientId);
                Assert.That(playerObject != null, Is.True, "The authority should still be able to get the player object after the client has disconnected");

                // Despawn and destroy the player object
                playerObject.Despawn();

                // Check that now the player object is null
                yield return WaitForConditionOrTimeOut(() => playerObject == null);
                AssertOnTimeout("Timed out waiting for the object to be destroyed.");

                // Regression test:
                // check that the authority's player object isn't destroyed
                Assert.That(newOwner.LocalClient.PlayerObject != null, Is.True, "The server's player object should not have been destroyed!");
            }

            // sanity check that requesting the object from the client who left after the object was destroyed is now null
            var sanity = authority.SpawnManager.GetPlayerNetworkObject(newClientId);
            Assert.Null(sanity, $"{nameof(NetworkSpawnManager.GetPlayerNetworkObject)} shouldn't be able to get the player object after the client has disconnected!");
        }

        private NetworkManager GetOwningNetworkManager(NetworkObject networkObject)
        {
            foreach (var manager in m_NetworkManagers)
            {
                if (manager.LocalClientId == networkObject.OwnerClientId)
                {
                    return manager;
                }
            }
            Assert.Fail($"Failed to find network manager who owns object {networkObject.name}. OwnerClientId: {networkObject.OwnerClientId}");
            return null;
        }
    }

}
