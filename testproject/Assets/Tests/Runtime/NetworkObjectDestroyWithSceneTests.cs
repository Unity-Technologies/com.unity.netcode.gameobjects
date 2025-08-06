using System.Collections;
using System.Text;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Host)]
    internal class NetworkObjectDestroyWithSceneTests : NetcodeIntegrationTest
    {
        private const string k_SceneToLoad = "EmptyScene";
        protected override int NumberOfClients => 2;

        private bool m_SceneHasLoaded;
        private Scene m_SceneLoaded;
        private Scene m_NotSessionOwnerScene;
        private NetworkManager m_SessionOwner;
        private NetworkManager m_NotSessionOwner;
        private NetworkObject m_SpawnedInstance;
        private StringBuilder m_ErrorLog = new StringBuilder();
        private NetworkObject m_TestPrefab;

        public NetworkObjectDestroyWithSceneTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override void OnServerAndClientsCreated()
        {
            m_TestPrefab = CreateNetworkObjectPrefab("TestObject").GetComponent<NetworkObject>();
            m_TestPrefab.DestroyWithScene = true;
            m_TestPrefab.SceneMigrationSynchronization = true;
            base.OnServerAndClientsCreated();
        }

        /// <summary>
        /// Conditional that determines if all <see cref="NetworkManager"/> instances have
        /// spawned or despawned the instance.
        /// </summary>
        private bool ObjectSpawnedOnAllNetworkManagers(bool isSpawned)
        {
            m_ErrorLog.Clear();
            var networkObjectId = m_SpawnedInstance.NetworkObjectId;
            foreach (var networkManager in m_NetworkManagers)
            {
                var containsKey = networkManager.SpawnManager.SpawnedObjects.ContainsKey(networkObjectId);
                if ((isSpawned && !containsKey) || (!isSpawned && containsKey))
                {
                    m_ErrorLog.AppendLine($"[{networkManager.name}] NetworkObjectId-{networkObjectId} should be spawned ({isSpawned}) but its key status is ({containsKey})!");
                    continue;
                }
            }
            return m_ErrorLog.Length == 0;
        }

        /// <summary>
        /// Condition that determines if all instances of the spawned object were migrated
        /// to the loaded scene.
        /// </summary>
        private bool AllInstancesMovedToScene()
        {
            var networkObjectId = m_SpawnedInstance.NetworkObjectId;
            foreach (var networkManager in m_NetworkManagers)
            {
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(networkObjectId))
                {
                    return false;
                }
                if (networkManager.SpawnManager.SpawnedObjects[networkObjectId].gameObject.scene.name != m_SceneLoaded.name)
                {
                    return false;
                }
            }
            return true;
        }

        [UnityTest]
        public IEnumerator DestroyWithScene()
        {
            // Get the session owner
            m_SessionOwner = GetAuthorityNetworkManager();
            // Get a non-session owner client
            m_NotSessionOwner = GetNonAuthorityNetworkManager();

            // Register for scene events on the session owner side.
            m_SessionOwner.SceneManager.OnSceneEvent += OnSessionOwnerSceneEvent;

            // Register for scene events on the non-session owner side.
            // (this is the scene we want to migrate the spawned object into when running a distributed authority session)
            m_NotSessionOwner.SceneManager.OnSceneEvent += OnNonSessionOwnerSceneEvent;

            // Have the session owner load the scene
            Assert.True(m_SessionOwner.SceneManager.LoadScene(k_SceneToLoad, LoadSceneMode.Additive) == SceneEventProgressStatus.Started, $"[{m_SessionOwner.name}] Failed to begin loading scene {k_SceneToLoad}!");
            yield return WaitForConditionOrTimeOut(() => m_SceneHasLoaded && m_NotSessionOwnerScene != null
            && m_NotSessionOwnerScene.IsValid() && m_NotSessionOwnerScene.isLoaded);
            AssertOnTimeout($"Scene loading event for {k_SceneToLoad} failed to complete!");

            // Depending on network topology, spawn the object with the appropriate owner.
            var owner = m_DistributedAuthority ? m_NotSessionOwner : m_SessionOwner;
            m_SpawnedInstance = SpawnObject(m_TestPrefab.gameObject, m_NotSessionOwner, true).GetComponent<NetworkObject>();

            var instanceName = m_SpawnedInstance.name;
            yield return WaitForConditionOrTimeOut(() => ObjectSpawnedOnAllNetworkManagers(true));
            AssertOnTimeout($"Not all clients spawned an instance of {instanceName}!");

            var sceneToMigrateTo = m_DistributedAuthority ? m_NotSessionOwnerScene : m_SceneLoaded;
            // Migrate the newly spawned object into the recently loaded scene so we can unload the scene
            // and make sure the spawned object is destroyed when that happens.
            SceneManager.MoveGameObjectToScene(m_SpawnedInstance.gameObject, sceneToMigrateTo);

            yield return WaitForConditionOrTimeOut(AllInstancesMovedToScene);
            AssertOnTimeout($"Not all instances of {instanceName} moved to scene {k_SceneToLoad}!");

            // Have the session owner unload the scene containing the spawned object
            VerboseDebug($"Unloading scene {k_SceneToLoad}");
            Assert.True(m_SessionOwner.SceneManager.UnloadScene(m_SceneLoaded) == SceneEventProgressStatus.Started, $"[{m_SessionOwner.name}] Failed to begin unloading scene {k_SceneToLoad}!");
            yield return WaitForConditionOrTimeOut(() => !m_SceneHasLoaded);
            AssertOnTimeout($"Scene unloading event for {k_SceneToLoad} failed to complete!");

            // Assure the spawned object was destroyed when the scene was unloaded
            yield return WaitForConditionOrTimeOut(() => ObjectSpawnedOnAllNetworkManagers(false));
            AssertOnTimeout($"Not all despawned an instance of {instanceName}!\n {m_ErrorLog}");
        }

        /// <summary>
        /// Handles scene events for the session owner
        /// </summary>
        private void OnSessionOwnerSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.ClientId != m_SessionOwner.LocalClientId)
            {
                return;
            }

            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.LoadComplete:
                    {
                        VerboseDebug($"Scene loaded: {sceneEvent.Scene.name}");
                        m_SceneLoaded = sceneEvent.Scene;
                        break;
                    }
                case SceneEventType.LoadEventCompleted:
                    {
                        m_SceneHasLoaded = true;
                        break;
                    }
                case SceneEventType.UnloadEventCompleted:
                    {
                        m_SessionOwner.SceneManager.OnSceneEvent -= OnSessionOwnerSceneEvent;
                        m_SceneHasLoaded = false;
                        break;
                    }
            }
        }

        /// <summary>
        /// Handles scene events for the non-session owner
        /// </summary>
        private void OnNonSessionOwnerSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.ClientId != m_NotSessionOwner.LocalClientId)
            {
                return;
            }

            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.LoadComplete:
                    {
                        VerboseDebug($"Scene loaded: {sceneEvent.Scene.name}");
                        m_NotSessionOwnerScene = sceneEvent.Scene;
                        m_NotSessionOwner.SceneManager.OnSceneEvent -= OnNonSessionOwnerSceneEvent;
                        break;
                    }
            }
        }


        protected override IEnumerator OnTearDown()
        {
            // In case of an error, assure the scene is unloaded
            if (m_SceneLoaded.IsValid() && m_SceneLoaded.isLoaded)
            {
                SceneManager.UnloadSceneAsync(m_SceneLoaded);
            }

            if (m_NotSessionOwnerScene.IsValid() && m_NotSessionOwnerScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(m_NotSessionOwnerScene);
            }
            return base.OnTearDown();
        }
    }
}
