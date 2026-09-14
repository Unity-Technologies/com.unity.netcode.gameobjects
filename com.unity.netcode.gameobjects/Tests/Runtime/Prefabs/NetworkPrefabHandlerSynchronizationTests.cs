using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.DAHost)]
    internal class NetworkPrefabHandlerSynchronizationTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        public NetworkPrefabHandlerSynchronizationTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        private GameObject m_ValidPrefab;
        private GameObject m_ClientSideValidPrefab;
        private GameObject m_ClientSideExceptionPrefab;

        public class VerifyLastClientSentRpcToServer : NetworkBehaviour
        {
            public bool RpcReceived { get; private set; }

            protected override void OnNetworkPreSpawn(ref NetworkManager networkManager)
            {
                RpcReceived = false;
                base.OnNetworkPreSpawn(ref networkManager);
            }

            public void DelayUntilOneMessageReceivedRpc(RpcParams rpcParams = default)
            {
                RpcReceived = true;
            }
        }

        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.AddComponent<VerifyLastClientSentRpcToServer>();
            base.OnCreatePlayerPrefab();
        }

        protected override void OnServerAndClientsCreated()
        {
            m_ValidPrefab = CreateNetworkObjectPrefab("ValidPrefab");
            m_ClientSideValidPrefab = CreateNetworkObjectPrefab("ClientSideValidPrefab");
            m_ClientSideExceptionPrefab = CreateNetworkObjectPrefab("ClientSideExceptionPrefab");
            base.OnServerAndClientsCreated();
        }

        [UnityTest]
        public IEnumerator NetworkPrefabHandlerSpawnAndSynchronizeTests()
        {
            var nonAuthority = GetNonAuthorityNetworkManager();

            var networkObjectToSpawnOnClient = m_ClientSideValidPrefab.GetComponent<NetworkObject>();

            var clientSideHandler = new GameObject();
            var clientPrefabHandler = clientSideHandler.AddComponent<NetworkPrefabInstanceHandler>();
            clientPrefabHandler.Initialize(nonAuthority, m_ClientSideValidPrefab.GetComponent<NetworkObject>());

            nonAuthority.PrefabHandler.AddHandler(m_ValidPrefab, clientPrefabHandler);

            var clientSideExceptionHandler = new GameObject();
            var clientSideExceptionPrefabHandler = clientSideExceptionHandler.AddComponent<NetworkPrefabExceptionThrower>();

            nonAuthority.PrefabHandler.AddHandler(m_ClientSideExceptionPrefab, clientSideExceptionPrefabHandler);

            var authority = GetAuthorityNetworkManager();

            // Spawn the invalid object first.
            var exceptionObject = SpawnObject(m_ClientSideExceptionPrefab, authority).GetComponent<NetworkObject>();

            // Check the invalid object spawns on the authority, expect an error from non-authority.
            LogAssert.Expect(LogType.Exception, "Exception: exception while instantiating");
            LogAssert.Expect(LogType.Error, new Regex("Failed to spawn NetworkObject!"));
            // Authority should receive an error from non-authority and should use the globalObjectIdHash to find the failing object
            LogAssert.Expect(LogType.Error, new Regex($@"SenderId:{nonAuthority.LocalClientId}\]\[{Regex.Escape(exceptionObject.name)}"));

            yield return WaitForConditionOrTimeOut(() => exceptionObject.IsSpawned);
            AssertOnTimeout("Failed to spawn object on authority!");

            // Now spawn a valid object
            var validObject = SpawnObject(m_ValidPrefab, authority).GetComponent<NetworkObject>();

            // The valid object should spawn as expected
            yield return WaitForSpawnedOnAllOrTimeOut(validObject);
            AssertOnTimeout("Failed to spawn valid prefab on all clients!");

            // Create a new client and register the same PrefabHandlers on the client
            var newClient = CreateNewClient();

            var lateJoinClientSideHandler = new GameObject();
            var lateJoinClientPrefabHandler = clientSideHandler.AddComponent<NetworkPrefabInstanceHandler>();
            var lateJoinExceptionHandler = new GameObject();
            var lateJoinClientExceptionHandler = lateJoinExceptionHandler.AddComponent<NetworkPrefabExceptionThrower>();

            lateJoinClientPrefabHandler.Initialize(newClient, m_ClientSideValidPrefab.GetComponent<NetworkObject>());

            newClient.PrefabHandler.AddHandler(m_ClientSideExceptionPrefab, lateJoinClientExceptionHandler);
            newClient.PrefabHandler.AddHandler(m_ValidPrefab, lateJoinClientPrefabHandler);

            // Expect assertions from the new client
            LogAssert.Expect(LogType.Exception, "Exception: exception while instantiating");
            LogAssert.Expect(LogType.Error, new Regex("Failed to spawn NetworkObject!"));

            // Authority will receive an error from new client and should use the globalObjectIdHash to find the failing object
            var expectedNewClientId = nonAuthority.LocalClientId + 1;
            LogAssert.Expect(LogType.Error, new Regex($@"SenderId:{expectedNewClientId}\]\[{Regex.Escape(exceptionObject.name)}"));

            // Start and synchronize the new client
            yield return StartClient(newClient);
            AssertOnTimeout($"Timed out waiting for the late joining client, {newClient.name}, to connect!");


            // Validate the valid prefab spawned on all clients without issue
            var expectedAuthorityHash = m_ValidPrefab.GetComponent<NetworkObject>().GlobalObjectIdHash;
            var expectedNonAuthorityHash = m_ClientSideValidPrefab.GetComponent<NetworkObject>().GlobalObjectIdHash;
            foreach (var networkManager in m_NetworkManagers)
            {
                Assert.True(networkManager.SpawnManager.SpawnedObjects.TryGetValue(validObject.NetworkObjectId, out NetworkObject spawnedObject), $"Client-{networkManager.LocalClientId} failed to spawn version of valid object!");

                if (spawnedObject.HasAuthority)
                {
                    Assert.That(spawnedObject.GlobalObjectIdHash, Is.EqualTo(expectedAuthorityHash), "NetworkObject spawned with unexpected GlobalObjectIdHash!");
                    Assert.That(networkManager.SpawnManager.SpawnedObjects.ContainsKey(exceptionObject.NetworkObjectId), Is.True, "Authority missing spawned NetworkObject!");
                }
                else
                {
                    Assert.That(spawnedObject.GlobalObjectIdHash, Is.EqualTo(expectedNonAuthorityHash), "NetworkObject spawned with unexpected GlobalObjectIdHash!");
                    Assert.That(networkManager.SpawnManager.SpawnedObjects.ContainsKey(exceptionObject.NetworkObjectId), Is.False, "Non authority should not have spawned exception object!");
                }
            }

            // Assure this test continues to run until we verify the late joining client has sent 1 message to the server
            // This should be the fix for MTT-15473 where the test finishes/exits before the message from the client has been received and processed by the server.
            Assert.IsTrue(authority.SpawnManager.SpawnedObjects.ContainsKey(newClient.LocalClient.PlayerObject.NetworkObjectId), $"Server does not have a player for Client-{newClient.LocalClientId}!");

            // Get server and late joining client's VerifyLastClientSentRpcToServer NetworkBehaviour
            var serverLateClientInstance = authority.SpawnManager.SpawnedObjects[newClient.LocalClient.PlayerObject.NetworkObjectId].GetComponent<VerifyLastClientSentRpcToServer>();
            var sendRpc = newClient.LocalClient.PlayerObject.GetComponent<VerifyLastClientSentRpcToServer>();

            // Send a message from the late joining client to the server
            sendRpc.DelayUntilOneMessageReceivedRpc();

            // Wait for the server to have received this message before exiting the test.
            // If the log message has not been received by the server at this point, then there is some other type of bug specific to iOS and Mac.
            yield return WaitForConditionOrTimeOut(() => serverLateClientInstance.RpcReceived);
        }
    }
}
