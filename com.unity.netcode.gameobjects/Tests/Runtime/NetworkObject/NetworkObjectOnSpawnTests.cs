using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkObjectOnSpawnTests : BaseMultiInstanceTest
    {
        private GameObject m_TestNetworkObjectPrefab;
        private GameObject m_TestNetworkObjectInstance;

        protected override int NbClients => 2;

        /// <summary>
        /// Tests that instantiating a <see cref="NetworkObject"/> and destroying without spawning it
        /// does not run <see cref="NetworkBehaviour.OnNetworkSpawn"/> or <see cref="NetworkBehaviour.OnNetworkSpawn"/>.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator InstantiateDestroySpawnNotCalled()
        {
            m_TestNetworkObjectPrefab = new GameObject("InstantiateDestroySpawnNotCalled_Object");
            var networkObject = m_TestNetworkObjectPrefab.AddComponent<NetworkObject>();
            var fail = m_TestNetworkObjectPrefab.AddComponent<FailWhenSpawned>();

            // instantiate
            m_TestNetworkObjectInstance = Object.Instantiate(m_TestNetworkObjectPrefab);
            yield return null;
        }

        private class FailWhenSpawned : NetworkBehaviour
        {
            public override void OnNetworkSpawn()
            {
                Assert.Fail("Spawn should not be called on not spawned object");
            }

            public override void OnNetworkDespawn()
            {
                Assert.Fail("Depawn should not be called on not spawned object");
            }
        }

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return StartSomeClientsAndServerWithPlayers(true, NbClients, playerPrefab =>
            {
                // add test component
                playerPrefab.AddComponent<TrackOnSpawnFunctions>();
            });
        }

        [UnityTearDown]
        public override IEnumerator Teardown()
        {
            if (m_TestNetworkObjectPrefab != null)
            {
                Object.Destroy(m_TestNetworkObjectPrefab);
            }

            if (m_TestNetworkObjectInstance != null)
            {
                Object.Destroy(m_TestNetworkObjectInstance);
            }
            yield return base.Teardown();
        }

        /// <summary>
        /// Test that callbacks are run for playerobject spawn, despawn, regular spawn, destroy on server.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TestOnNetworkSpawnCallbacks()
        {
            // [Host-Side] Get the Host owned instance
            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ServerNetworkManager, serverClientPlayerResult));

            var serverInstance = serverClientPlayerResult.Result.GetComponent<TrackOnSpawnFunctions>();

            var clientInstances = new List<TrackOnSpawnFunctions>();
            foreach (var client in m_ClientNetworkManagers)
            {
                var clientClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
                yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), client, clientClientPlayerResult));
                var clientRpcTests = clientClientPlayerResult.Result.GetComponent<TrackOnSpawnFunctions>();
                Assert.IsNotNull(clientRpcTests);
                clientInstances.Add(clientRpcTests);
            }

            // -------------- step 1 check player spawn despawn

            // check spawned on server
            Assert.AreEqual(1, serverInstance.OnNetworkSpawnCalledCount);

            // safety check server despawned
            Assert.AreEqual(0, serverInstance.OnNetworkDespawnCalledCount);

            // Conditional check for clients spawning or despawning
            var checkSpawnCondition = false;
            var expectedSpawnCount = 1;
            var expectedDespawnCount = 0;
            bool HasConditionBeenMet()
            {
                var clientsCompleted = 0;
                // check spawned on client
                foreach (var clientInstance in clientInstances)
                {
                    if (checkSpawnCondition)
                    {
                        if (clientInstance.OnNetworkSpawnCalledCount == expectedSpawnCount)
                        {
                            clientsCompleted++;
                        }
                    }
                    else
                    {
                        if (clientInstance.OnNetworkDespawnCalledCount == expectedDespawnCount)
                        {
                            clientsCompleted++;
                        }
                    }
                }
                return clientsCompleted >= NbClients;
            }

            // safety check that all clients have not been despawned yet
            Assert.True(HasConditionBeenMet(), "Failed condition that all clients not despawned yet!");

            // now verify that all clients have been spawned
            checkSpawnCondition = true;
            yield return WaitForConditionOrTimeOut(HasConditionBeenMet);
            Assert.False(s_GloabalTimeOutHelper.TimedOut, "Timed out while waiting for client side spawns!");

            // despawn on server.  However, since we'll be using this object later in the test, don't delete it
            serverInstance.GetComponent<NetworkObject>().Despawn(false);

            // check despawned on server
            Assert.AreEqual(1, serverInstance.OnNetworkDespawnCalledCount);
            // we now expect the clients to each have despawned once
            expectedDespawnCount = 1;

            // verify that all client-side instances are despawned
            checkSpawnCondition = false;
            yield return WaitForConditionOrTimeOut(HasConditionBeenMet);

            Assert.False(s_GloabalTimeOutHelper.TimedOut, "Timed out while waiting for client side despawns!");

            //----------- step 2 check spawn and destroy again
            serverInstance.GetComponent<NetworkObject>().Spawn();
            // wait a tick
            yield return m_DefaultWaitForTick;
            // check spawned again on server this is 2 because we are reusing the object which was already spawned once.
            Assert.AreEqual(2, serverInstance.OnNetworkSpawnCalledCount);

            checkSpawnCondition = true;
            yield return WaitForConditionOrTimeOut(HasConditionBeenMet);

            Assert.False(s_GloabalTimeOutHelper.TimedOut, "Timed out while waiting for client side spawns! (2nd pass)");

            // destroy the server object
            Object.Destroy(serverInstance.gameObject);

            yield return m_DefaultWaitForTick;

            // check whether despawned was called again on server instance
            Assert.AreEqual(2, serverInstance.OnNetworkDespawnCalledCount);

            checkSpawnCondition = false;
            yield return WaitForConditionOrTimeOut(HasConditionBeenMet);

            Assert.False(s_GloabalTimeOutHelper.TimedOut, "Timed out while waiting for client side despawns! (2nd pass)");
        }

        private class TrackOnSpawnFunctions : NetworkBehaviour
        {
            public int OnNetworkSpawnCalledCount { get; private set; }
            public int OnNetworkDespawnCalledCount { get; private set; }

            public override void OnNetworkSpawn()
            {
                OnNetworkSpawnCalledCount++;
            }

            public override void OnNetworkDespawn()
            {
                OnNetworkDespawnCalledCount++;
            }
        }
    }
}
