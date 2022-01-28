using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkObjectOnSpawnTests : BaseMultiInstanceTest
    {
        private GameObject m_TestNetworkObjectPrefab;
        private GameObject m_TestNetworkObjectInstance;

        protected override int NbClients => 2;

        private const float k_TimeOutWaitPeriod = 2.0f;
        private static float s_TimeOutPeriod;

        /// <summary>
        /// This will simply advance the timeout period
        /// </summary>
        public static void AdvanceTimeOutPeriod()
        {
            s_TimeOutPeriod = Time.realtimeSinceStartup + k_TimeOutWaitPeriod;
        }

        /// <summary>
        /// Checks if the timeout period has elapsed
        /// </summary>
        private static bool HasTimedOut()
        {
            return s_TimeOutPeriod <= Time.realtimeSinceStartup;
        }

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

            // safety check despawned
            Assert.AreEqual(0, serverInstance.OnNetworkDespawnCalledCount);

            AdvanceTimeOutPeriod();
            var timedOut = false;
            while (!HasTimedOut())
            {
                var spawnedCount = 0;
                // check spawned on client
                foreach (var clientInstance in clientInstances)
                {
                    if (clientInstance.OnNetworkSpawnCalledCount == 1)
                    {
                        spawnedCount++;
                    }

                    // safety check despawned
                    Assert.AreEqual(0, clientInstance.OnNetworkDespawnCalledCount);
                }

                timedOut = HasTimedOut();
                if (spawnedCount >= NbClients)
                {
                    break;
                }

                yield return new WaitForSeconds(1.0f / m_ServerNetworkManager.NetworkConfig.TickRate);
            }

            Assert.False(timedOut, "Timed out while waiting for client side spawns!");

            // despawn on server.  However, since we'll be using this object later in the test, don't delete it (false)
            serverInstance.GetComponent<NetworkObject>().Despawn(false);

            // check despawned on server
            Assert.AreEqual(1, serverInstance.OnNetworkDespawnCalledCount);

            AdvanceTimeOutPeriod();
            timedOut = false;
            while (!HasTimedOut())
            {
                var deSpawnedCount = 0;
                foreach (var clientInstance in clientInstances)
                {
                    if (clientInstance.OnNetworkDespawnCalledCount == 1)
                    {
                        deSpawnedCount++;
                    }
                }

                timedOut = HasTimedOut();

                if (deSpawnedCount >= NbClients)
                {
                    break;
                }

                yield return new WaitForSeconds(1.0f / m_ServerNetworkManager.NetworkConfig.TickRate);
            }

            Assert.False(timedOut, "Timed out while waiting for client side despawns!");

            //----------- step 2 check spawn again and destroy
            serverInstance.GetComponent<NetworkObject>().Spawn();

            yield return new WaitForSeconds(1.0f / m_ServerNetworkManager.NetworkConfig.TickRate);
            // check spawned again on server this is 2 because we are reusing the object which was already spawned once.
            Assert.AreEqual(2, serverInstance.OnNetworkSpawnCalledCount);

            AdvanceTimeOutPeriod();
            timedOut = false;
            while (!HasTimedOut())
            {
                var spawnedCount = 0;
                // check spawned on client
                foreach (var clientInstance in clientInstances)
                {
                    if (clientInstance.OnNetworkSpawnCalledCount == 1)
                    {
                        spawnedCount++;
                    }
                }

                timedOut = HasTimedOut();
                if (spawnedCount >= NbClients)
                {
                    break;
                }

                yield return new WaitForSeconds(1.0f / m_ServerNetworkManager.NetworkConfig.TickRate);
            }

            Assert.False(timedOut, "Timed out while waiting for client side spawns! (2nd pass)");

            // destroy the server object
            Object.Destroy(serverInstance.gameObject);

            yield return new WaitForSeconds(1.0f / m_ServerNetworkManager.NetworkConfig.TickRate);

            // check whether despawned was called again on server instance
            Assert.AreEqual(2, serverInstance.OnNetworkDespawnCalledCount);

            AdvanceTimeOutPeriod();
            timedOut = false;
            while (!HasTimedOut())
            {
                var deSpawnedCount = 0;
                foreach (var clientInstance in clientInstances)
                {
                    if (clientInstance.OnNetworkDespawnCalledCount == 1)
                    {
                        deSpawnedCount++;
                    }
                }

                timedOut = HasTimedOut();

                if (deSpawnedCount >= NbClients)
                {
                    break;
                }

                yield return new WaitForSeconds(1.0f / m_ServerNetworkManager.NetworkConfig.TickRate);
            }

            Assert.False(timedOut, "Timed out while waiting for client side despawns! (2nd pass)");
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
