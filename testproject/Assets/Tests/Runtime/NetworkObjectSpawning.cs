using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    [TestFixture(NetworkTopologyTypes.ClientServer)]
    [TestFixture(NetworkTopologyTypes.DistributedAuthority)]
    internal class NetworkObjectSpawning : NetcodeIntegrationTest
    {
        private const string k_SceneToLoad = "NetworkObjectSpawnerTest";

        private GameObject m_ObjectToSpawn;

        private bool m_CanStartServerAndClients;

        protected override int NumberOfClients => 1;

        public NetworkObjectSpawning(NetworkTopologyTypes networkTopology) : base(networkTopology) { }


        protected override IEnumerator OnSetup()
        {
            NetworkObjectSpawnerForTests.Reset();
            m_CanStartServerAndClients = false;
            return base.OnSetup();
        }

        protected override IEnumerator OnTearDown()
        {
            Object.DestroyImmediate(m_ObjectToSpawn);
            m_ObjectToSpawn = null;
            return base.OnTearDown();
        }

        protected override bool CanStartServerAndClients()
        {
            return m_CanStartServerAndClients;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_ObjectToSpawn = NetworkObjectSpawnerForTests.ObjectToSpawn = CreateNetworkObjectPrefab("TestObject");
            base.OnServerAndClientsCreated();
        }

        private void OnClientStarted()
        {
            if (m_NetworkTopologyType == NetworkTopologyTypes.DistributedAuthority)
            {
                LogAssert.Expect(LogType.Error, new Regex($"Cannot spawn {m_ObjectToSpawn.name} until the client is fully connected to the session!"));
                var networkObject = m_ObjectToSpawn.GetComponent<NetworkObject>();
                networkObject.NetworkManagerOwner = m_ClientNetworkManagers[0];
                networkObject.Spawn();
                m_ClientNetworkManagers[0].OnClientStarted -= OnClientStarted;
            }
        }

        /// <summary>
        /// Validates that a NetworkObject cannot be spawned before being approved when
        /// using a distributed authority network topology.
        /// </summary>
        [UnityTest]
        public IEnumerator CannotSpawnBeforeApproved()
        {
            m_CanStartServerAndClients = true;
            m_ClientNetworkManagers[0].OnClientStarted += OnClientStarted;
            yield return StartServerAndClients();
        }

        private Scene m_SceneLoaded;
        private bool m_SceneLoadCompleted;
        private NetworkSceneManager.VerifySceneBeforeLoadingDelegateHandler m_ServerVerificationAction;

        public enum SynchronizeNotificationTypes
        {
            InSceneObjects,
            Synchronized,
            Both
        }

        /// <summary>
        /// Validates that you can spawn during <see cref="NetworkBehaviour.OnInSceneObjectsSpawned"/> and <see cref="NetworkBehaviour.OnNetworkSessionSynchronized"/> (or spawn during both)
        /// </summary>
        [UnityTest]
        public IEnumerator SpawnOnSynchronizedNotification([Values] SynchronizeNotificationTypes synchronizeNotificationTypes)
        {
            m_SceneLoadCompleted = false;
            m_CanStartServerAndClients = true;
            NetworkObjectSpawnerForTests.SpawnAfterInSceneSynchronized = synchronizeNotificationTypes == SynchronizeNotificationTypes.InSceneObjects || synchronizeNotificationTypes == SynchronizeNotificationTypes.Both;
            NetworkObjectSpawnerForTests.SpawnAfterSynchronized = synchronizeNotificationTypes == SynchronizeNotificationTypes.Synchronized || synchronizeNotificationTypes == SynchronizeNotificationTypes.Both;
            NetworkObjectSpawnerForTests.OnlyAuthoritySpawns = !m_DistributedAuthority;

            if (synchronizeNotificationTypes == SynchronizeNotificationTypes.Both)
            {
                SceneManager.sceneLoaded += SceneLoaded;
                var asyncTask = SceneManager.LoadSceneAsync(k_SceneToLoad, LoadSceneMode.Additive);
                yield return WaitForConditionOrTimeOut(() => asyncTask.isDone);
                AssertOnTimeout($"Failed to load scene {k_SceneToLoad}!");
            }

            yield return StartServerAndClients();

            if (synchronizeNotificationTypes != SynchronizeNotificationTypes.Both)
            {
                m_ServerNetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
                m_ServerNetworkManager.SceneManager.LoadScene(k_SceneToLoad, LoadSceneMode.Additive);

                yield return WaitForConditionOrTimeOut(() => m_SceneLoaded.name == k_SceneToLoad && m_SceneLoaded.IsValid() && m_SceneLoaded.isLoaded && m_SceneLoadCompleted);
                AssertOnTimeout($"Failed to load scene {k_SceneToLoad}!");

                m_ServerNetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
            }

            yield return s_DefaultWaitForTick;
            var hostOrSessionOwner = m_DistributedAuthority ? "Session Owner" : "Host";

            Assert.IsTrue(NetworkObjectSpawnerForTests.SpawnedInstances.ContainsKey(NetworkManager.ServerClientId), $"[{m_NetworkTopologyType}] {hostOrSessionOwner} did not spawn any objects during notification for in-scene objects synchronized!");
            var expectedSpawnCount = synchronizeNotificationTypes == SynchronizeNotificationTypes.Both ? 2 : 1;
            var authorityCount = NetworkObjectSpawnerForTests.SpawnedInstances[NetworkManager.ServerClientId].Count;
            Assert.IsTrue(authorityCount == expectedSpawnCount, $"[{m_NetworkTopologyType}] {hostOrSessionOwner} only spawned {authorityCount} but was expected to spawn {expectedSpawnCount}!");

            if (m_DistributedAuthority)
            {
                Assert.IsTrue(NetworkObjectSpawnerForTests.SpawnedInstances.ContainsKey(m_ClientNetworkManagers[0].LocalClientId), $"[{m_NetworkTopologyType}] Client did not spawn any objects during notification for in-scene objects synchronized!");
                var clientCount = NetworkObjectSpawnerForTests.SpawnedInstances[m_ClientNetworkManagers[0].LocalClientId].Count;
                Assert.IsTrue(clientCount == expectedSpawnCount, $"[{m_NetworkTopologyType}] Client-{m_ClientNetworkManagers[0].LocalClientId} only spawned {clientCount} but was expected to spawn {expectedSpawnCount}!");
            }

            if (synchronizeNotificationTypes == SynchronizeNotificationTypes.Both)
            {
                SceneManager.UnloadSceneAsync(m_SceneLoaded);
            }
        }

        private void SceneLoaded(Scene scene, LoadSceneMode arg1)
        {
            if (scene.name == k_SceneToLoad)
            {
                m_SceneLoaded = scene;
            }
        }

        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.SceneEventType == SceneEventType.LoadComplete && sceneEvent.ClientId == m_ServerNetworkManager.LocalClientId && sceneEvent.Scene.name == k_SceneToLoad)
            {
                m_SceneLoaded = sceneEvent.Scene;
            }
            else if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted && sceneEvent.SceneName == k_SceneToLoad)
            {
                m_SceneLoadCompleted = true;
            }
        }
    }
}
