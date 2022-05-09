using System.Collections;
using System.Linq;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;



namespace TestProject.RuntimeTests
{
    public class InScenePlacedNetworkObjects : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private const string k_SceneToLoad = "InSceneNetworkObject";
        private Scene m_ServerSideSceneLoaded;
        private bool m_CanStartServerAndClients;

        protected override bool OnSetVerboseDebug()
        {
            return true;
        }

        protected override IEnumerator OnSetup()
        {
            m_CanStartServerAndClients = false;
            return base.OnSetup();
        }

        protected override bool CanStartServerAndClients()
        {
            return m_CanStartServerAndClients;
        }

        /// <summary>
        /// This verifies that in-scene placed NetworkObjects will be properly
        /// synchronized if:
        /// 1.) Despawned prior to a client late-joining
        /// 2.) Re-spawned after having been despawned without registering the in-scene
        /// NetworkObject as a NetworkPrefab
        /// </summary>
        [UnityTest]
        public IEnumerator InSceneNetworkObjectSynchAndSpawn()
        {
            // Because despawning a client will cause it to shutdown and clean everything in the
            // scene hierarchy, we have to prevent one of the clients from spawning initially before
            // we test synchronizing late joining clients with despawned in-scene placed NetworkObjects.
            // So, we prevent the automatic starting of the server and clients, remove the client we
            // will be targeting to join late from the m_ClientNetworkManagers array, start the server
            // and the remaining client, despawn the in-scene NetworkObject, and then start and synchronize
            // the clientToTest.
            var clientToTest = m_ClientNetworkManagers[1];
            var clients = m_ClientNetworkManagers.ToList();
            clients.Remove(clientToTest);
            m_ClientNetworkManagers = clients.ToArray();
            m_CanStartServerAndClients = true;
            yield return StartServerAndClients();
            clients.Add(clientToTest);
            m_ClientNetworkManagers = clients.ToArray();

            NetworkObjectTestComponent.ServerNetworkObjectInstance = null;
            m_ServerNetworkManager.SceneManager.OnSceneEvent += Server_OnSceneEvent;
            var status = m_ServerNetworkManager.SceneManager.LoadScene(k_SceneToLoad, LoadSceneMode.Additive);
            Assert.IsTrue(status == SceneEventProgressStatus.Started, $"When attempting to load scene {k_SceneToLoad} was returned the following progress status: {status}");

            // This verifies the scene loaded and the in-scene placed NetworkObjects spawned.
            yield return WaitForConditionOrTimeOut(() => NetworkObjectTestComponent.SpawnedInstances.Count == TotalClients - 1);
            AssertOnTimeout($"Timed out waiting for total spawned in-scene placed NetworkObjects to reach a count of {TotalClients - 1} and is currently {NetworkObjectTestComponent.SpawnedInstances.Count}");

            // Get the server-side instance of the in-scene NetworkObject
            Assert.True(s_GlobalNetworkObjects.ContainsKey(m_ServerNetworkManager.LocalClientId), $"Could not find server instance of the test in-scene NetworkObject!");
            var serverObject = NetworkObjectTestComponent.ServerNetworkObjectInstance;
            Assert.IsNotNull(serverObject, $"Could not find server-side in-scene placed NetworkObject!");
            Assert.IsTrue(serverObject.IsSpawned, $"{serverObject.name} is not spawned!");

            // Despawn the in-scene placed NetworkObject
            Debug.Log("Despawning In-Scene placed NetworkObject");
            serverObject.Despawn(false);
            yield return WaitForConditionOrTimeOut(() => NetworkObjectTestComponent.SpawnedInstances.Count == 0);
            AssertOnTimeout($"Timed out waiting for all in-scene instances to be despawned!  Current spawned count: {NetworkObjectTestComponent.SpawnedInstances.Count()}");

            // Now late join a client
            NetworkObjectTestComponent.OnInSceneObjectDespawned += OnInSceneObjectDespawned;
            NetcodeIntegrationTestHelpers.StartOneClient(clientToTest);
            yield return WaitForConditionOrTimeOut(() => (clientToTest.IsConnectedClient && clientToTest.IsListening));
            AssertOnTimeout($"Timed out waiting for {clientToTest.name} to reconnect!");

            yield return s_DefaultWaitForTick;

            // Make sure the late-joining client's in-scene placed NetworkObject received the despawn notification during synchronization
            Assert.IsNotNull(m_JoinedClientDespawnedNetworkObject, $"{clientToTest.name} did not despawn the in-scene placed NetworkObject when connecting and synchronizing!");

            // Update the newly joined client information
            ClientNetworkManagerPostStartInit();

            // We should still have no spawned in-scene placed NetworkObjects at this point
            yield return WaitForConditionOrTimeOut(() => NetworkObjectTestComponent.SpawnedInstances.Count == 0);
            AssertOnTimeout($"{clientToTest.name} spawned in-scene placed NetworkObject!");

            // Now test that the despawned in-scene placed NetworkObject can be re-spawned (without having been registered as a NetworkPrefab)
            serverObject.Spawn();

            yield return WaitForConditionOrTimeOut(() => NetworkObjectTestComponent.SpawnedInstances.Count == TotalClients);
            AssertOnTimeout($"Timed out waiting for all in-scene instances to be spawned!  Current spawned count: {NetworkObjectTestComponent.SpawnedInstances.Count()} | Expected spawn count: {TotalClients}");
            CleanUpLoadedScene();
        }

        private NetworkObject m_JoinedClientDespawnedNetworkObject;

        private void OnInSceneObjectDespawned(NetworkObject networkObject)
        {
            m_JoinedClientDespawnedNetworkObject = networkObject;
            NetworkObjectTestComponent.OnInSceneObjectDespawned -= OnInSceneObjectDespawned;
        }

        private void Server_OnSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.ClientId == m_ServerNetworkManager.LocalClientId && sceneEvent.SceneEventType == SceneEventType.LoadComplete
                && sceneEvent.Scene.IsValid() && sceneEvent.Scene.isLoaded)
            {
                m_ServerSideSceneLoaded = sceneEvent.Scene;
                m_ServerNetworkManager.SceneManager.OnSceneEvent -= Server_OnSceneEvent;
            }
        }

        private IEnumerator CleanUpLoadedScene()
        {
            if (m_ServerSideSceneLoaded.IsValid() && m_ServerSideSceneLoaded.isLoaded)
            {
                Debug.Log("Unloading scene now");
                m_ServerNetworkManager.SceneManager.OnSceneEvent += Unload_OnSceneEvent;
                m_ServerNetworkManager.SceneManager.UnloadScene(m_ServerSideSceneLoaded);
                yield return WaitForConditionOrTimeOut(() => m_ClientNetworkManagers.Where((c) => !c.IsListening).Count() == 0);
                AssertOnTimeout($"[CleanUpLoadedScene] Timed out waiting for all in-scene instances to be despawned!  Current spawned count: {m_ClientNetworkManagers.Where((c) => !c.IsListening).Count()}");
            }
        }

        private void Unload_OnSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.ClientId == m_ServerNetworkManager.LocalClientId && sceneEvent.SceneEventType == SceneEventType.UnloadEventCompleted)
            {
                m_ServerNetworkManager.SceneManager.OnSceneEvent -= Unload_OnSceneEvent;
            }
        }

        /// <summary>
        /// Very important to always have a backup "unloading" catch
        /// in the event your test fails it could not potentially unload
        /// a scene and the proceeding tests could be impacted by this!
        /// </summary>
        /// <returns></returns>
        protected override IEnumerator OnTearDown()
        {
            yield return CleanUpLoadedScene();
        }
    }
}
