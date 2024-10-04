using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class ObserversTestClass : NetworkBehaviour
    {
        public NetworkVariable<int> NetVariable = new NetworkVariable<int>();

        public bool ClientRPCCalled;

        public void ResetRPCState()
        {
            ClientRPCCalled = false;
        }

        [ClientRpc]
        public void TestClientRpc()
        {
            ClientRPCCalled = true;
        }
    }

    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    internal class PlayerObjectTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        protected GameObject m_NewPlayerToSpawn;

        public PlayerObjectTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override void OnServerAndClientsCreated()
        {
            m_NewPlayerToSpawn = CreateNetworkObjectPrefab("NewPlayerInstance");
            base.OnServerAndClientsCreated();
        }

        [UnityTest]
        public IEnumerator SpawnAndReplaceExistingPlayerObject()
        {
            yield return WaitForConditionOrTimeOut(() => m_PlayerNetworkObjects[m_ServerNetworkManager.LocalClientId].ContainsKey(m_ClientNetworkManagers[0].LocalClientId));
            AssertOnTimeout("Timed out waiting for client-side player object to spawn!");
            // Get the server-side player NetworkObject
            var originalPlayer = m_PlayerNetworkObjects[m_ServerNetworkManager.LocalClientId][m_ClientNetworkManagers[0].LocalClientId];
            // Get the client-side player NetworkObject
            var playerLocalClient = m_ClientNetworkManagers[0].LocalClient.PlayerObject;

            // Create a new player prefab instance
            var newPlayer = Object.Instantiate(m_NewPlayerToSpawn);
            var newPlayerNetworkObject = newPlayer.GetComponent<NetworkObject>();
            // In distributed authority mode, the client owner spawns its new player
            newPlayerNetworkObject.NetworkManagerOwner = m_DistributedAuthority ? m_ClientNetworkManagers[0] : m_ServerNetworkManager;
            // Spawn this instance as a new player object for the client who already has an assigned player object
            newPlayerNetworkObject.SpawnAsPlayerObject(m_ClientNetworkManagers[0].LocalClientId);

            // Make sure server-side changes are detected, the original player object should no longer be marked as a player
            // and the local new player object should.
            yield return WaitForConditionOrTimeOut(() => !originalPlayer.IsPlayerObject && newPlayerNetworkObject.IsPlayerObject);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Timed out waiting for server-side player object to change!");

            // Make sure the client-side changes are the same
            yield return WaitForConditionOrTimeOut(() => m_ClientNetworkManagers[0].LocalClient.PlayerObject != playerLocalClient && !playerLocalClient.IsPlayerObject
            && m_ClientNetworkManagers[0].LocalClient.PlayerObject.IsPlayerObject);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Timed out waiting for client-side player object to change!");
        }
    }

    /// <summary>
    /// Validate that when auto-player spawning but SpawnWithObservers is disabled,
    /// the player instantiated is only spawned on the authority side.
    /// </summary>
    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    internal class PlayerSpawnNoObserversTest : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        public PlayerSpawnNoObserversTest(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override bool ShouldCheckForSpawnedPlayers()
        {
            return false;
        }

        protected override void OnCreatePlayerPrefab()
        {
            var playerNetworkObject = m_PlayerPrefab.GetComponent<NetworkObject>();
            playerNetworkObject.SpawnWithObservers = false;
            base.OnCreatePlayerPrefab();
        }

        [UnityTest]
        public IEnumerator SpawnWithNoObservers()
        {
            yield return s_DefaultWaitForTick;

            if (!m_DistributedAuthority)
            {
                // Make sure clients did not spawn their player object on any of the clients including the owner.
                foreach (var client in m_ClientNetworkManagers)
                {
                    foreach (var playerObject in m_ServerNetworkManager.SpawnManager.PlayerObjects)
                    {
                        Assert.IsFalse(client.SpawnManager.SpawnedObjects.ContainsKey(playerObject.NetworkObjectId), $"Client-{client.LocalClientId} spawned player object for Client-{playerObject.NetworkObjectId}!");
                    }
                }
            }
            else
            {
                // For distributed authority, we want to make sure the player object is only spawned on the authority side and all non-authority instances did not spawn it.
                var playerObjectId = m_ServerNetworkManager.LocalClient.PlayerObject.NetworkObjectId;
                foreach (var client in m_ClientNetworkManagers)
                {
                    Assert.IsFalse(client.SpawnManager.SpawnedObjects.ContainsKey(playerObjectId), $"Client-{client.LocalClientId} spawned player object for Client-{m_ServerNetworkManager.LocalClientId}!");
                }

                foreach (var clientPlayer in m_ClientNetworkManagers)
                {
                    playerObjectId = clientPlayer.LocalClient.PlayerObject.NetworkObjectId;
                    Assert.IsFalse(m_ServerNetworkManager.SpawnManager.SpawnedObjects.ContainsKey(playerObjectId), $"Client-{m_ServerNetworkManager.LocalClientId} spawned player object for Client-{clientPlayer.LocalClientId}!");
                    foreach (var client in m_ClientNetworkManagers)
                    {
                        if (clientPlayer == client)
                        {
                            continue;
                        }
                        Assert.IsFalse(client.SpawnManager.SpawnedObjects.ContainsKey(playerObjectId), $"Client-{client.LocalClientId} spawned player object for Client-{clientPlayer.LocalClientId}!");
                    }
                }

            }
        }
    }

    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    internal class PlayerObjectSpawnTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        protected GameObject m_NewPlayerToSpawn;
        protected GameObject m_TestObserversObject;

        public PlayerObjectSpawnTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override void OnServerAndClientsCreated()
        {
            m_TestObserversObject = CreateNetworkObjectPrefab("ObserversTest");
            m_TestObserversObject.AddComponent<ObserversTestClass>();

            m_NewPlayerToSpawn = CreateNetworkObjectPrefab("NewPlayerInstance");
            base.OnServerAndClientsCreated();
        }

        private int GetObserverCount( System.Collections.Generic.HashSet<ulong>.Enumerator observerEnumerator )
        {
            int observerCount = 0;
            while (observerEnumerator.MoveNext())
            {
                observerCount++;
            }

            return observerCount;
        }
        private ObserversTestClass GetObserversTestClassObjectForClient(ulong clientId)
        {
#if UNITY_2023_1_OR_NEWER
            var emptyComponents = Object.FindObjectsByType<ObserversTestClass>(FindObjectsSortMode.InstanceID);
#else
            var emptyComponents = UnityEngine.Object.FindObjectsOfType<EmptyComponent>();
#endif
            foreach (var component in emptyComponents)
            {
                if (component.IsSpawned && component.NetworkManager.LocalClientId == clientId)
                {
                    return component;
                }
            }
            return null;
        }

        // https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues/3059 surfaced a regression from 1.11 to 2.0
        // where Destroying (not Disconnecting) a player object would remove it from all NetworkObjects observer arrays. Upon recreating
        // the player object they were no longer an observer for exising network objects causing them to miss NetworkVariable and RPC updates.
        // This test covers that case including testing RPCs and NetworkVariables still function after recreating the player object
        [UnityTest]
        public IEnumerator SpawnDestoryRespawnPlayerObjectMaintainsObservers()
        {
            yield return WaitForConditionOrTimeOut(() => m_PlayerNetworkObjects[m_ServerNetworkManager.LocalClientId].ContainsKey(m_ClientNetworkManagers[0].LocalClientId));
            AssertOnTimeout("Timed out waiting for client-side player object to spawn!");

            // So on the server we want to spawn the observer object and then check its observers
            var serverObserverObject = SpawnObject(m_TestObserversObject, m_ServerNetworkManager);
            var serverObserverComponent = serverObserverObject.GetComponent<ObserversTestClass>();
            yield return WaitForConditionOrTimeOut(() => GetObserversTestClassObjectForClient(m_ClientNetworkManagers[0].LocalClientId) != null);
            AssertOnTimeout("Timed out waiting for client-side observer object to spawn!");

            var clientObserverComponent = GetObserversTestClassObjectForClient(m_ClientNetworkManagers[0].LocalClientId);
            Assert.NotNull(clientObserverComponent);
            Assert.AreNotEqual(serverObserverComponent, clientObserverComponent, "Client and Server Observer Test components are equal, the test is wrong.");

            // The server object should be the owner
            Assert.IsTrue(serverObserverObject.GetComponent<ObserversTestClass>().IsOwner);
            Assert.IsFalse(clientObserverComponent.IsOwner);

            // Test Networkvariables and RPCs function as expected
            serverObserverComponent.NetVariable.Value = 123;
            yield return WaitForConditionOrTimeOut(() => clientObserverComponent.NetVariable.Value == 123);
            AssertOnTimeout("Timed out waiting for network variable to transmit!");

            serverObserverComponent.TestClientRpc();
            yield return WaitForConditionOrTimeOut(() => clientObserverComponent.ClientRPCCalled);
            AssertOnTimeout("Timed out waiting for RPC to be called!");

            serverObserverComponent.ResetRPCState();
            clientObserverComponent.ResetRPCState();

            // Destory the clients player object, this will remove the player object but not disconnect the client, it should leave the connection intact
            bool foundPlayer = false;
            ulong destroyedClientId = 0;
            foreach( var c in m_ServerNetworkManager.ConnectedClients)
            {
                if (!c.Value.PlayerObject.GetComponent<NetworkObject>().IsOwner)
                {
                    destroyedClientId = c.Key;
                    Object.Destroy(c.Value.PlayerObject);
                    foundPlayer = true;
                    break;
                }
            }
            Assert.True(foundPlayer);

            yield return WaitForConditionOrTimeOut(() => m_ServerNetworkManager.ConnectedClients[destroyedClientId].PlayerObject == null);
            AssertOnTimeout("Timed out waiting for player object to be destroyed!");


            // so lets respawn the player here
            var newPlayer = Object.Instantiate(m_NewPlayerToSpawn);
            newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(destroyedClientId);

            yield return WaitForConditionOrTimeOut(() => m_ServerNetworkManager.ConnectedClients[destroyedClientId].PlayerObject != null);
            AssertOnTimeout("Timed out waiting for player object to respawn!");

            // Test Networkvariables and RPCs function as expected after recreating the client player object
            serverObserverComponent.NetVariable.Value = 321;
            yield return WaitForConditionOrTimeOut(() => clientObserverComponent.NetVariable.Value == 321);
            AssertOnTimeout("Timed out waiting for network variable to transmit after respawn!");

            serverObserverComponent.TestClientRpc();
            yield return WaitForConditionOrTimeOut(() => clientObserverComponent.ClientRPCCalled);
            AssertOnTimeout("Timed out waiting for RPC to be called after respawn!");
        }
    }
}
