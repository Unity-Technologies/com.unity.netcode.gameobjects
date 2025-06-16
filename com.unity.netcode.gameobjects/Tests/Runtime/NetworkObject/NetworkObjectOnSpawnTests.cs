using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkObjectOnSpawnTests : NetcodeIntegrationTest
    {
        private GameObject m_TestNetworkObjectPrefab;
        private GameObject m_TestNetworkObjectInstance;

        protected override int NumberOfClients => 2;

        public enum ObserverTestTypes
        {
            WithObservers,
            WithoutObservers
        }
        private GameObject m_ObserverPrefab;
        private NetworkObject m_ObserverTestNetworkObject;
        private ObserverTestTypes m_ObserverTestType;

        private const string k_ObserverTestObjName = "ObsObj";
        private const string k_WithObserversError = "Not all clients spawned the";
        private const string k_WithoutObserversError = "A client spawned the";

        protected override void OnServerAndClientsCreated()
        {
            m_ObserverPrefab = CreateNetworkObjectPrefab(k_ObserverTestObjName);
            base.OnServerAndClientsCreated();
        }


        private bool CheckClientsSideObserverTestObj()
        {
            foreach (var client in m_ClientNetworkManagers)
            {
                if (!s_GlobalNetworkObjects.ContainsKey(client.LocalClientId))
                {
                    // When no observers there shouldn't be any client spawned NetworkObjects
                    // (players are held in a different list)
                    return !(m_ObserverTestType == ObserverTestTypes.WithObservers);
                }
                var clientObjects = s_GlobalNetworkObjects[client.LocalClientId];
                // Make sure they did spawn the object
                if (m_ObserverTestType == ObserverTestTypes.WithObservers)
                {
                    if (!clientObjects.ContainsKey(m_ObserverTestNetworkObject.NetworkObjectId))
                    {
                        return false;
                    }
                    if (!clientObjects[m_ObserverTestNetworkObject.NetworkObjectId].IsSpawned)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Set up late joining client
        /// </summary>
        protected override void OnNewClientCreated(NetworkManager networkManager)
        {
            networkManager.NetworkConfig.EnableSceneManagement = m_ServerNetworkManager.NetworkConfig.EnableSceneManagement;
            base.OnNewClientCreated(networkManager);
        }

        /// <summary>
        /// This test validates <see cref="NetworkObject.SpawnWithObservers"/> property
        /// </summary>
        /// <param name="observerTestTypes">whether to spawn with or without observers</param>
        [UnityTest]
        public IEnumerator ObserverSpawnTests([Values] ObserverTestTypes observerTestTypes, [Values] bool sceneManagement)
        {
            if (!sceneManagement)
            {
                // Disable prefabs to prevent them from being destroyed
                foreach (var networkPrefab in m_ServerNetworkManager.NetworkConfig.Prefabs.Prefabs)
                {
                    networkPrefab.Prefab.SetActive(false);
                }

                // Shutdown and clean up the current client NetworkManager instances
                foreach (var networkManager in m_ClientNetworkManagers)
                {
                    m_PlayerNetworkObjects[networkManager.LocalClientId].Clear();
                    m_PlayerNetworkObjects.Remove(networkManager.LocalClientId);
                    yield return StopOneClient(networkManager, true);
                }

                // Shutdown and clean up the server NetworkManager instance
                m_PlayerNetworkObjects[m_ServerNetworkManager.LocalClientId].Clear();
                yield return StopOneClient(m_ServerNetworkManager);

                // Set the prefabs to active again
                foreach (var networkPrefab in m_ServerNetworkManager.NetworkConfig.Prefabs.Prefabs)
                {
                    networkPrefab.Prefab.SetActive(true);
                }

                // Disable scene management and start the host
                m_ServerNetworkManager.NetworkConfig.EnableSceneManagement = false;
                m_ServerNetworkManager.StartHost();
                yield return s_DefaultWaitForTick;

                // Create 2 new clients and connect them
                for (int i = 0; i < NumberOfClients; i++)
                {
                    yield return CreateAndStartNewClient();
                }
            }

            m_ObserverTestType = observerTestTypes;
            var prefabNetworkObject = m_ObserverPrefab.GetComponent<NetworkObject>();
            prefabNetworkObject.SpawnWithObservers = observerTestTypes == ObserverTestTypes.WithObservers;
            var instance = SpawnObject(m_ObserverPrefab, m_ServerNetworkManager);
            m_ObserverTestNetworkObject = instance.GetComponent<NetworkObject>();
            var withoutObservers = m_ObserverTestType == ObserverTestTypes.WithoutObservers;
            if (withoutObservers)
            {
                // Just give a little time to make sure nothing spawned
                yield return s_DefaultWaitForTick;
            }
            yield return WaitForConditionOrTimeOut(CheckClientsSideObserverTestObj);
            AssertOnTimeout($"{(withoutObservers ? k_WithoutObserversError : k_WithObserversError)} {k_ObserverTestObjName} object!");
            // If we spawned without observers
            if (withoutObservers)
            {
                // Make each client an observer
                foreach (var client in m_ClientNetworkManagers)
                {
                    m_ObserverTestNetworkObject.NetworkShow(client.LocalClientId);
                }

                // Validate the clients spawned the NetworkObject
                m_ObserverTestType = ObserverTestTypes.WithObservers;
                yield return WaitForConditionOrTimeOut(CheckClientsSideObserverTestObj);
                AssertOnTimeout($"{k_WithObserversError} {k_ObserverTestObjName} object!");

                // Validate that a late joining client does not see the NetworkObject when it spawns
                yield return CreateAndStartNewClient();

                m_ObserverTestType = ObserverTestTypes.WithoutObservers;
                // Just give a little time to make sure nothing spawned
                yield return s_DefaultWaitForTick;
                yield return WaitForConditionOrTimeOut(CheckClientsSideObserverTestObj);
                AssertOnTimeout($"{(withoutObservers ? k_WithoutObserversError : k_WithObserversError)} {k_ObserverTestObjName} object!");

                // Now validate that we can make the NetworkObject visible to the newly joined client
                m_ObserverTestNetworkObject.NetworkShow(m_ClientNetworkManagers[NumberOfClients].LocalClientId);

                // Validate the NetworkObject is visible to all connected clients (including the recently joined client)
                m_ObserverTestType = ObserverTestTypes.WithObservers;
                yield return WaitForConditionOrTimeOut(CheckClientsSideObserverTestObj);
                AssertOnTimeout($"{k_WithObserversError} {k_ObserverTestObjName} object!");
            }
        }

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
            if (m_ObserverPrefab != null)
            {
                Object.Destroy(m_ObserverPrefab);
            }

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
        /// <returns>An IEnumerator for the UnityTest coroutine that validates callback execution.</returns>
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

        private bool AllClientsSpawnedObject()
        {
            foreach (var networkManager in m_ClientNetworkManagers)
            {
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_SpawnedInstanceId))
                {
                    return false;
                }
            }
            return true;
        }

        private bool AllClientsDespawnedObject()
        {
            foreach (var networkManager in m_ClientNetworkManagers)
            {
                if (networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_SpawnedInstanceId))
                {
                    return false;
                }
            }
            return true;
        }

        private ulong m_SpawnedInstanceId;
        /// <summary>
        /// Validates that NetworkObject is reset properly when despawned but not destroyed.
        /// </summary>
        /// <returns>IEnumerator</returns>
        [UnityTest]
        public IEnumerator NetworkObjectResetOnDespawn()
        {
            var authorityNetworkManager = m_ServerNetworkManager;
            var instance = SpawnObject(m_ObserverPrefab, authorityNetworkManager).GetComponent<NetworkObject>();
            m_SpawnedInstanceId = instance.NetworkObjectId;
            yield return WaitForConditionOrTimeOut(AllClientsSpawnedObject);
            AssertOnTimeout($"Not all clients spawned an instance of {instance.name}!");

            instance.Despawn(false);

            yield return WaitForConditionOrTimeOut(AllClientsDespawnedObject);
            AssertOnTimeout($"Not all clients de-spawned an instance of {instance.name}!");

            Assert.IsNull(instance.GetNetworkParenting(), "Last parent was not reset!");

            Object.Destroy(instance.gameObject);
        }
    }
}
