using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TestProject.RuntimeTests.Support;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    public class MessageOrderingTests : NetcodeIntegrationTest
    {
        // Must be 1 for these tests.
        protected override int NumberOfClients => 1;

        private GameObject m_OwnershipPrefab;
        private GameObject m_SpawnRpcDespawnPrefab;

        public MessageOrderingTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        private static void ResetStatics()
        {
            Support.SpawnRpcDespawn.ClientUpdateCount = 0;
            Support.SpawnRpcDespawn.ServerUpdateCount = 0;
            Support.SpawnRpcDespawn.ClientNetworkSpawnRpcCalled = false;
            Support.SpawnRpcDespawn.ExecuteClientRpc = false;
        }

        protected override IEnumerator OnSetup()
        {
            ResetStatics();
            return base.OnSetup();
        }

        protected override IEnumerator OnTearDown()
        {
            ResetStatics();
            return base.OnTearDown();
        }

        protected override void OnServerAndClientsCreated()
        {
            m_OwnershipPrefab = CreateNetworkObjectPrefab("OwnershipObject");

            m_SpawnRpcDespawnPrefab = CreateNetworkObjectPrefab("SpawnRpcDespawnObject");
            m_SpawnRpcDespawnPrefab.AddComponent<SpawnRpcDespawn>();
            Support.SpawnRpcDespawn.TestStage = NetworkUpdateStage.EarlyUpdate;
            base.OnServerAndClientsCreated();
        }

        /// <summary>
        /// Adds a unique <see cref="SpawnRpcDespawnInstanceHandler"/> to each given <see cref="NetworkManager"/>.
        /// </summary>
        private List<SpawnRpcDespawnInstanceHandler> AddSpawnRpcDespawnHandlers(IEnumerable<NetworkManager> networkManagers)
        {
            var networkObject = m_SpawnRpcDespawnPrefab.GetComponent<NetworkObject>();
            var handlers = new List<SpawnRpcDespawnInstanceHandler>();
            foreach (var networkManager in networkManagers)
            {
                var handler = new SpawnRpcDespawnInstanceHandler(networkObject.GlobalObjectIdHash, networkManager);
                networkManager.PrefabHandler.AddHandler(networkObject, handler);
                handlers.Add(handler);
            }
            return handlers;
        }

        private static bool AllHandlersSpawned(List<SpawnRpcDespawnInstanceHandler> handlers)
        {
            foreach (var handler in handlers)
            {
                if (!handler.WasSpawned)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool AllHandlersDestroyed(List<SpawnRpcDespawnInstanceHandler> handlers)
        {
            foreach (var handler in handlers)
            {
                if (!handler.WasDestroyed)
                {
                    return false;
                }
            }
            return true;
        }

        [UnityTest]
        public IEnumerator SpawnChangeOwnership()
        {
            var nonAuthority = GetNonAuthorityNetworkManager();
            var authorityInstance = SpawnObject(m_OwnershipPrefab, GetAuthorityNetworkManager()).GetComponent<NetworkObject>();
            authorityInstance.ChangeOwnership(nonAuthority.LocalClientId);

            var timeoutHelper = new TimeoutHelper(8.0f);
            yield return WaitForSpawnedOnAllOrTimeOut(authorityInstance, timeoutHelper);
            AssertOnTimeout("Did not successfully spawn all expected NetworkObjects", timeoutHelper);
            Assert.AreEqual(nonAuthority.LocalClientId, nonAuthority.SpawnManager.SpawnedObjects[authorityInstance.NetworkObjectId].OwnerClientId,
                $"[Client-{nonAuthority.LocalClientId}] Does not own {authorityInstance.name}!");
        }

        [UnityTest]
        public IEnumerator SpawnRpcDespawn()
        {
            var frameCountStart = Time.frameCount;
            var clientHandlers = AddSpawnRpcDespawnHandlers(m_ClientNetworkManagers);

            SpawnObject(m_SpawnRpcDespawnPrefab, GetAuthorityNetworkManager()).GetComponent<SpawnRpcDespawn>().Activate();

            // Every client receives the client RPC, including the host's own client.
            var expectedCount = Support.SpawnRpcDespawn.ClientUpdateCount + TotalClients;
            bool AllClientRpcsReceivedAndHandled(StringBuilder errorLog)
            {
                if (Support.SpawnRpcDespawn.ClientUpdateCount != expectedCount)
                {
                    errorLog.Append($"Client count ({Support.SpawnRpcDespawn.ClientUpdateCount}) did not match the expected count ({expectedCount})!");
                    return false;
                }
                if (!AllHandlersSpawned(clientHandlers))
                {
                    errorLog.Append("Not all client-side handlers were spawned!");
                    return false;
                }
                if (!AllHandlersDestroyed(clientHandlers))
                {
                    errorLog.Append("Not all client-side handlers were destroyed!");
                    return false;
                }
                return true;
            }
            var timeoutHelper = new TimeoutHelper(5.0f);
            yield return WaitForConditionOrTimeOut(AllClientRpcsReceivedAndHandled, timeoutHelper);
            AssertOnTimeout("Did not successfully call all expected client RPCs!", timeoutHelper);

            Debug.Log($"It took {Time.frameCount - frameCountStart} frames to process the MessageOrdering.SpawnRpcDespawn integration test.");
        }

        [UnityTest]
        public IEnumerator RpcOnNetworkSpawn()
        {
            Support.SpawnRpcDespawn.ExecuteClientRpc = true;
            var authority = GetAuthorityNetworkManager();

            // We *must* always add a unique handler to both the server and the clients
            var handlers = AddSpawnRpcDespawnHandlers(new[] { authority });
            handlers.AddRange(AddSpawnRpcDespawnHandlers(m_ClientNetworkManagers));

            var serverNetworkObject = NetworkObject.InstantiateAndSpawn(m_SpawnRpcDespawnPrefab, authority);

            // Make sure everyone spawns the object
            var timeoutHelper = new TimeoutHelper(4.0f);
            yield return WaitForSpawnedOnAllOrTimeOut(serverNetworkObject, timeoutHelper);
            AssertOnTimeout($"Timed out waiting for all clients to spawn {serverNetworkObject.name}!", timeoutHelper);

            timeoutHelper = new TimeoutHelper(5.0f);
            yield return WaitForConditionOrTimeOut(() => Support.SpawnRpcDespawn.ClientNetworkSpawnRpcCalled, timeoutHelper);
            AssertOnTimeout("Did not successfully call all expected client RPCs", timeoutHelper);
            Assert.True(AllHandlersSpawned(handlers), "Not all handlers were spawned!");

            // Despawning the server-side NetworkObject will invoke the handler's OnDestroy method
            serverNetworkObject.Despawn();
            timeoutHelper = new TimeoutHelper(2.0f);
            yield return WaitForConditionOrTimeOut(() => AllHandlersDestroyed(handlers), timeoutHelper);
            AssertOnTimeout("Timed out waiting for handlers to be destroyed", timeoutHelper);
        }
    }
}
