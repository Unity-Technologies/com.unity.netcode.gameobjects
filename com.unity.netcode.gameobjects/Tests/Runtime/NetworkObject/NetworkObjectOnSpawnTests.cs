using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkObjectOnSpawnTests : NetcodeIntegrationTest
    {
        private GameObject m_TestNetworkObjectPrefab;
        private GameObject m_TestNetworkObjectInstance;

        protected override int NumberOfClients => 2;

        /// <summary>
        /// Tests that instantiating a <see cref="NetworkObject"/> and destroying without spawning it
        /// does not run <see cref="NetworkBehaviour.OnNetworkSpawn"/> or <see cref="NetworkBehaviour.OnNetworkSpawn"/>.
        /// </summary>
        [UnityTest]
        public IEnumerator InstantiateDestroySpawnNotCalled()
        {
            m_TestNetworkObjectPrefab = new GameObject("InstantiateDestroySpawnNotCalled_Object");
            var networkObject = m_TestNetworkObjectPrefab.AddComponent<NetworkObject>();
            var fail = m_TestNetworkObjectPrefab.AddComponent<FailWhenSpawned>();

            // instantiate
            m_TestNetworkObjectInstance = Object.Instantiate(m_TestNetworkObjectPrefab);
            yield return null;
            Object.Destroy(m_TestNetworkObjectInstance);

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

        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.AddComponent<TrackOnSpawnFunctions>();
        }

        protected override IEnumerator OnTearDown()
        {
            if (m_TestNetworkObjectPrefab != null)
            {
                Object.Destroy(m_TestNetworkObjectPrefab);
            }

            if (m_TestNetworkObjectInstance != null)
            {
                Object.Destroy(m_TestNetworkObjectInstance);
            }
            yield return base.OnTearDown();
        }

        private List<TrackOnSpawnFunctions> m_ClientTrackOnSpawnInstances = new List<TrackOnSpawnFunctions>();

        /// <summary>
        /// Test that callbacks are run for playerobject spawn, despawn, regular spawn, destroy on server.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TestOnNetworkSpawnCallbacks()
        {
            // [Host-Side] Get the Host owned instance
            var serverInstance = m_PlayerNetworkObjects[m_ServerNetworkManager.LocalClientId][m_ServerNetworkManager.LocalClientId].GetComponent<TrackOnSpawnFunctions>();

            foreach (var client in m_ClientNetworkManagers)
            {
                var clientRpcTests = m_PlayerNetworkObjects[client.LocalClientId][m_ServerNetworkManager.LocalClientId].gameObject.GetComponent<TrackOnSpawnFunctions>();
                Assert.IsNotNull(clientRpcTests);
                m_ClientTrackOnSpawnInstances.Add(clientRpcTests);
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
                foreach (var clientInstance in m_ClientTrackOnSpawnInstances)
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
                return clientsCompleted >= NumberOfClients;
            }

            // safety check that all clients have not been despawned yet
            Assert.True(HasConditionBeenMet(), "Failed condition that all clients not despawned yet!");

            // now verify that all clients have been spawned
            checkSpawnCondition = true;
            yield return WaitForConditionOrTimeOut(HasConditionBeenMet);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Timed out while waiting for client side spawns!");

            // despawn on server.  However, since we'll be using this object later in the test, don't delete it
            serverInstance.GetComponent<NetworkObject>().Despawn(false);

            // check despawned on server
            Assert.AreEqual(1, serverInstance.OnNetworkDespawnCalledCount);
            // we now expect the clients to each have despawned once
            expectedDespawnCount = 1;

            yield return s_DefaultWaitForTick;
            // verify that all client-side instances are despawned
            checkSpawnCondition = false;
            yield return WaitForConditionOrTimeOut(HasConditionBeenMet);

            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Timed out while waiting for client side despawns!");

            //----------- step 2 check spawn and destroy again
            serverInstance.GetComponent<NetworkObject>().Spawn();
            // wait a tick
            yield return s_DefaultWaitForTick;
            // check spawned again on server this is 2 because we are reusing the object which was already spawned once.
            Assert.AreEqual(2, serverInstance.OnNetworkSpawnCalledCount);

            checkSpawnCondition = true;
            yield return WaitForConditionOrTimeOut(HasConditionBeenMet);

            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Timed out while waiting for client side spawns! (2nd pass)");

            // destroy the server object
            Object.Destroy(serverInstance.gameObject);

            yield return s_DefaultWaitForTick;

            // check whether despawned was called again on server instance
            Assert.AreEqual(2, serverInstance.OnNetworkDespawnCalledCount);

            checkSpawnCondition = false;
            yield return WaitForConditionOrTimeOut(HasConditionBeenMet);

            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Timed out while waiting for client side despawns! (2nd pass)");
        }

        [Test]
        public void DynamicallySpawnedNoSceneOriginException()
        {
            var gameObject = new GameObject();
            var networkObject = gameObject.AddComponent<NetworkObject>();
            networkObject.IsSpawned = true;
            networkObject.SceneOriginHandle = 0;
            networkObject.IsSceneObject = false;
            // This validates invoking GetSceneOriginHandle will not throw an exception for a dynamically spawned NetworkObject
            // when the scene of origin handle is zero
            var sceneOriginHandle = networkObject.GetSceneOriginHandle();

            // This validates that GetSceneOriginHandle will return the GameObject's scene handle that should be the currently active scene
            var activeSceneHandle = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
            Assert.IsTrue(sceneOriginHandle == activeSceneHandle, $"{nameof(NetworkObject)} should have returned the active scene handle of {activeSceneHandle} but returned {sceneOriginHandle}");
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
