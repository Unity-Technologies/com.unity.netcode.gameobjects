using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using static Unity.Netcode.RuntimeTests.AttachableBehaviourTests;

namespace TestProject.RuntimeTests
{
    /// <summary>
    /// Validates that attachables do not log errors or throw exceptions
    /// during a scene transition (or unload) where either the attachable
    /// or the node persists and the other does not but they are attached
    /// to each other prior to the scene being unloaded.
    /// </summary>
    [TestFixture(HostOrServer.Host, Persists.AttachableBehaviour)]
    [TestFixture(HostOrServer.Server, Persists.AttachableBehaviour)]
    [TestFixture(HostOrServer.DAHost, Persists.AttachableBehaviour)]
    [TestFixture(HostOrServer.Host, Persists.AttachableNode)]
    [TestFixture(HostOrServer.Server, Persists.AttachableNode)]
    [TestFixture(HostOrServer.DAHost, Persists.AttachableNode)]
    internal class AttachableBehaviourSceneLoadTests : NetcodeIntegrationTest
    {
        public enum Persists
        {
            AttachableBehaviour,
            AttachableNode
        }

        protected override int NumberOfClients => 2;

        private const string k_SceneToLoad = "EmptyScene";

        private GameObject m_AttachablePrefab;
        private GameObject m_TargetNodePrefabA;
        private TestAttachable m_PrefabAttachableBehaviour;
        private TestNode m_PrefabAttachableNode;

        private NetworkObject m_SourceInstance;
        private NetworkObject m_TargetInstance;
        private TestAttachable m_AttachableBehaviourInstance;
        private AttachableNode m_AttachableNodeInstance;

        private List<ulong> m_DoesNotPersistNetworkObjectIds = new List<ulong>();

        private Scene m_AuthoritySceneLoaded;
        private Scene m_TestRunnerScene;

        private bool m_SceneLoadCompleted;

        private Persists m_Persists;
        public AttachableBehaviourSceneLoadTests(HostOrServer hostOrServer, Persists persists) : base(hostOrServer)
        {
            m_Persists = persists;
        }

        protected override IEnumerator OnSetup()
        {
            m_TestRunnerScene = SceneManager.GetActiveScene();
            m_DoesNotPersistNetworkObjectIds.Clear();
            return base.OnSetup();
        }

        protected override void OnCreatePlayerPrefab()
        {
            var networkObject = m_PlayerPrefab.GetComponent<NetworkObject>();
            networkObject.ActiveSceneSynchronization = false;
            base.OnCreatePlayerPrefab();
        }

        /// <summary>
        /// Create an attachable with a node that should be destroyed upon
        /// the scene it currently resides in being unloaded.
        /// </summary>
        protected override void OnServerAndClientsCreated()
        {
            // The source prefab contains the nested NetworkBehaviour that
            // will be parented under the target prefab.
            m_AttachablePrefab = CreateNetworkObjectPrefab("Source");
            m_AttachablePrefab.AddComponent<SceneSynchronizer>();
            var attachableNetworkObject = m_AttachablePrefab.GetComponent<NetworkObject>();
            attachableNetworkObject.DontDestroyWithOwner = false;
            attachableNetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);
            attachableNetworkObject.ActiveSceneSynchronization = false;
            attachableNetworkObject.SceneMigrationSynchronization = false;

            // The "attachable" that has a world item (source) as its original root parent.
            var sourceChild = new GameObject("SourceChild");
            sourceChild.transform.parent = m_AttachablePrefab.transform;
            m_PrefabAttachableBehaviour = sourceChild.AddComponent<TestAttachable>();
            m_PrefabAttachableBehaviour.AutoDetach = AttachableBehaviour.AutoDetachTypes.OnDespawn | AttachableBehaviour.AutoDetachTypes.OnAttachNodeDestroy;

            // This particular test validates that the attachable's world item is destroyed on a scene transition while the attachable
            // is attached to something that persists through scene loading.
            m_PrefabAttachableBehaviour.DestroyWithScene = m_Persists != Persists.AttachableBehaviour;

            // The target prefab that the source prefab will attach
            // to will be parented under the target prefab.
            m_TargetNodePrefabA = CreateNetworkObjectPrefab("TargetA");
            m_TargetNodePrefabA.AddComponent<SceneSynchronizer>();
            var targetNetworkObject = m_TargetNodePrefabA.GetComponent<NetworkObject>();
            targetNetworkObject.DontDestroyWithOwner = true;
            targetNetworkObject.SceneMigrationSynchronization = false;

            // The "target node" to attach the source child to
            var targetChildA = new GameObject("TargetChildA");
            targetChildA.transform.parent = m_TargetNodePrefabA.transform;
            m_PrefabAttachableNode = targetChildA.AddComponent<TestNode>();
            m_PrefabAttachableNode.DetachOnDespawn = true;

            // This particular test validates that the attachable's world item is destroyed on a scene transition while the attachable
            // is attached to something that persists through scene loading.
            m_PrefabAttachableNode.DestroyWithScene = m_Persists != Persists.AttachableNode;

            // For this test & only when using a distributed authority topology, we want to switch the default synchronization
            // mode back to single (even though it still is effectively loaded additively).
            if (m_DistributedAuthority)
            {
                foreach (var networkManager in m_NetworkManagers)
                {
                    m_ApplyClientSynchronizationModeInstances.Add(new ApplyClientSynchronizationMode(networkManager, LoadSceneMode.Single));
                }
            }
            base.OnServerAndClientsCreated();
        }

        #region Silly helper class to handle setting client synchronization mode for all distributed authority clients
        // TODO: We should be able to apply NetworkSceneManager properties like this within the NetworkConfig.
        private List<ApplyClientSynchronizationMode> m_ApplyClientSynchronizationModeInstances = new List<ApplyClientSynchronizationMode>();
        internal class ApplyClientSynchronizationMode
        {
            private NetworkManager m_NetworkManager;
            private LoadSceneMode m_LoadSceneMode;
            public ApplyClientSynchronizationMode(NetworkManager networkManager, LoadSceneMode loadSceneMode)
            {
                m_NetworkManager = networkManager;
                m_LoadSceneMode = loadSceneMode;
                m_NetworkManager.OnClientStarted += OnClientStarted;
            }

            private void OnClientStarted()
            {
                m_NetworkManager.OnClientStarted -= OnClientStarted;
                m_NetworkManager.SceneManager.SetClientSynchronizationMode(m_LoadSceneMode);
            }
        }
        #endregion

        protected override IEnumerator OnServerAndClientsConnected()
        {
            m_ApplyClientSynchronizationModeInstances.Clear();
            foreach (var networkManager in m_NetworkManagers)
            {
                networkManager.SceneManager.ActiveSceneSynchronizationEnabled = true;
            }
            return base.OnServerAndClientsConnected();
        }

        /// <summary>
        /// Conditional that validates a specific spawned attachable instance
        /// has been attached for the original and all cloned instances.
        /// </summary>
        private bool AllInstancesAttachedStateChanged(StringBuilder errorLog)
        {
            var target = m_TargetInstance;
            var targetId = target.NetworkObjectId;
            // The attachable can move between the two spawned instances so we have to use the appropriate one depending upon the authority's current state.
            var currentAttachableRoot = m_AttachableBehaviourInstance.State == AttachableBehaviour.AttachState.Attached ? target : m_SourceInstance;
            var attachable = (TestAttachable)null;
            var node = (TestNode)null;
            foreach (var networkManager in m_NetworkManagers)
            {
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(currentAttachableRoot.NetworkObjectId))
                {
                    errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] Has no spawned instance of {currentAttachableRoot.name}!");
                    continue;
                }
                else
                {
                    attachable = networkManager.SpawnManager.SpawnedObjects[currentAttachableRoot.NetworkObjectId].GetComponentInChildren<TestAttachable>();
                }

                if (!attachable)
                {
                    attachable = networkManager.SpawnManager.SpawnedObjects[targetId].GetComponentInChildren<TestAttachable>();
                    if (!attachable)
                    {
                        errorLog.AppendLine($"[Client-{networkManager.LocalClientId}][Attachable] Attachable was not found!");
                    }
                    continue;
                }

                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(targetId))
                {
                    errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] Has no spawned instance of {target.name}!");
                    continue;
                }
                else
                {
                    node = networkManager.SpawnManager.SpawnedObjects[targetId].GetComponentInChildren<TestNode>();
                }

                if (!attachable.CheckForState(true, false))
                {
                    errorLog.AppendLine($"[Client-{networkManager.LocalClientId}][{attachable.name}] Did not have its override invoked!");
                }
                if (!attachable.CheckForState(true, true))
                {
                    errorLog.AppendLine($"[Client-{networkManager.LocalClientId}][{attachable.name}] Did not have its event invoked!");
                }
                if (!node.OnAttachedInvoked)
                {
                    errorLog.AppendLine($"[Client-{networkManager.LocalClientId}][{node.name}] Did not have its override invoked!");
                }
                if (attachable.transform.parent != node.transform)
                {
                    errorLog.AppendLine($"[Client-{networkManager.LocalClientId}][{attachable.name}] {node.name} is not the parent of {attachable.name}!");
                }
            }
            return errorLog.Length == 0;
        }

        /// <summary>
        /// Conditional that waits for the expected, scene load non-persistent, spawned objects
        /// to be despawned.
        /// </summary>
        private bool WaitForAllToDespawn(StringBuilder errorLog)
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                for (int i = 0; i < m_DoesNotPersistNetworkObjectIds.Count; i++)
                {
                    if (networkManager.SpawnManager == null)
                    {
                        continue;
                    }
                    var sourceId = m_DoesNotPersistNetworkObjectIds[i];
                    if (networkManager.SpawnManager.SpawnedObjects.ContainsKey(sourceId))
                    {
                        errorLog.AppendLine($"[{networkManager.name}] Still has NetworkObjectId-{sourceId} spawned!");
                    }
                }
            }
            return errorLog.Length == 0;
        }

        /// <summary>
        /// Since everything spawns in the active scene, we want to assure that these instances are in their
        /// NetworkManager relative scene so upon receiving a scene unload event each client will handle destroying
        /// anything still spawned that does not persist a scene loading event.
        /// This is required to replicate of this issue.
        /// </summary>
        private void SynchronizeScene()
        {
            var sourceId = m_SourceInstance.NetworkObjectId;
            var targetId = m_TargetInstance.NetworkObjectId;
            foreach (var networkManager in m_NetworkManagers)
            {
                var synchronizer = networkManager.SpawnManager.SpawnedObjects[sourceId].GetComponent<SceneSynchronizer>();
                Assert.IsTrue(synchronizer.SynchronizeScene(), $"[{synchronizer.name}] Failed to synchronize instance to local scene!");
                synchronizer = networkManager.SpawnManager.SpawnedObjects[targetId].GetComponent<SceneSynchronizer>();
                Assert.IsTrue(synchronizer.SynchronizeScene(), $"[{synchronizer.name}] Failed to synchronize instance to local scene!");
            }
        }

        [UnityTest]
        public IEnumerator AttachedUponSceneTransition([Values] bool detachOnDespawn)
        {
            m_SceneLoadCompleted = false;

            // Handle detach on despawn differently to validate both paths.
            m_PrefabAttachableNode.DetachOnDespawn = detachOnDespawn;

            var authority = GetAuthorityNetworkManager();

            // Load a scene so all clients load this scene.
            authority.SceneManager.OnSceneEvent += OnSceneEvent;
            var response = authority.SceneManager.LoadScene(k_SceneToLoad, LoadSceneMode.Additive);
            Assert.IsTrue(response == SceneEventProgressStatus.Started, $"Failed to begin scene loading event for {k_SceneToLoad} with a status of {response}!");
            yield return WaitForConditionOrTimeOut(() => m_AuthoritySceneLoaded.IsValid() && m_AuthoritySceneLoaded.isLoaded && m_SceneLoadCompleted);
            AssertOnTimeout($"Timed out waiting for all clients to load scene {k_SceneToLoad}!");
            var persistedObjects = new Dictionary<NetworkManager, ulong>();
            // Now, make the newly loaded scene the currently active scene so everything instantiates in the scene authority's newly loaded scene instance.
            SceneManager.SetActiveScene(m_AuthoritySceneLoaded);
            foreach (var networkManager in m_NetworkManagers)
            {
                // Spawn our instances for this client
                m_SourceInstance = SpawnObject(m_AttachablePrefab, networkManager).GetComponent<NetworkObject>();
                m_TargetInstance = SpawnObject(m_TargetNodePrefabA, networkManager).GetComponent<NetworkObject>();

                yield return WaitForSpawnedOnAllOrTimeOut(m_SourceInstance);
                AssertOnTimeout($"Timed out waiting for all clients to spawn {m_SourceInstance.name}!");

                yield return WaitForSpawnedOnAllOrTimeOut(m_TargetInstance);
                AssertOnTimeout($"Timed out waiting for all clients to spawn {m_TargetInstance.name}!");

                m_AttachableBehaviourInstance = m_SourceInstance.GetComponentInChildren<TestAttachable>();
                Assert.NotNull(m_AttachableBehaviourInstance, $"{m_SourceInstance.name} does not have a nested child {nameof(AttachableBehaviour)}!");

                m_AttachableNodeInstance = m_TargetInstance.GetComponentInChildren<TestNode>();
                Assert.NotNull(m_AttachableNodeInstance, $"{m_TargetInstance.name} does not have a nested child {nameof(AttachableNode)}!");

                m_AttachableBehaviourInstance.Attach(m_AttachableNodeInstance);

                yield return WaitForConditionOrTimeOut(AllInstancesAttachedStateChanged);
                AssertOnTimeout($"Timed out waiting for all clients to attach {m_AttachableBehaviourInstance.name} to {m_AttachableNodeInstance.name}!");

                // Now migrate all instances from the scene authority's scene instance to the NetworkManager relative scene.
                SynchronizeScene();

                // Keep track of the spawned instances we expect to not persist a scene load
                var doesNotPersist = m_Persists == Persists.AttachableNode ? m_SourceInstance.NetworkObjectId : m_TargetInstance.NetworkObjectId;
                var persists = m_Persists == Persists.AttachableNode ? m_TargetInstance.NetworkObjectId : m_SourceInstance.NetworkObjectId;
                m_DoesNotPersistNetworkObjectIds.Add(doesNotPersist);
                persistedObjects.Add(networkManager, persists);
                VerboseDebug($"[{networkManager.name}] Spawned attachable and attached it.");
            }

            // This is the actual validation point where the scene is unloaded and either the attachable or
            // the node will be destroyed while the other persists (and detects that the other has been despawned and destroyed).
            response = authority.SceneManager.UnloadScene(m_AuthoritySceneLoaded);
            Assert.IsTrue(response == SceneEventProgressStatus.Started, $"Failed to begin scene unloading event for {k_SceneToLoad} with a status of {response}!");

            yield return WaitForConditionOrTimeOut(WaitForAllToDespawn);
            AssertOnTimeout($"Timed out waiting for all clients to despawn NetworkObjects!");
        }

        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            if ((sceneEvent.SceneEventType != SceneEventType.LoadEventCompleted && sceneEvent.SceneEventType != SceneEventType.LoadComplete)
                || sceneEvent.ClientId != GetAuthorityNetworkManager().LocalClientId)
            {
                return;
            }

            if (sceneEvent.SceneName == k_SceneToLoad)
            {
                if (sceneEvent.SceneEventType == SceneEventType.LoadComplete)
                {
                    m_AuthoritySceneLoaded = sceneEvent.Scene;
                }

                if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
                {
                    m_SceneLoadCompleted = true;
                }
            }
        }

        protected override IEnumerator OnTearDown()
        {
            // Assure we set the active scene back to the integration test's scene if needed.
            var currentActiveScene = SceneManager.GetActiveScene();
            if (m_AuthoritySceneLoaded != null && currentActiveScene == m_AuthoritySceneLoaded && m_AuthoritySceneLoaded.isLoaded && m_AuthoritySceneLoaded.IsValid())
            {
                SceneManager.SetActiveScene(m_TestRunnerScene);
                SceneManager.UnloadSceneAsync(m_AuthoritySceneLoaded);
            }
            return base.OnTearDown();
        }
    }

    /// <summary>
    /// This helper class will assure that the spawned object clone
    /// (or original) instance will be migrated into its appropriate
    /// NetworkManager relative instance.
    /// </summary>
    /// <remarks>
    /// Since we share the same scene root, there is only 1 active scene
    /// and all new instances are always instantiated within that.
    /// If you load a new scene and make that the active scene prior to
    /// spawning instances, then all instances will reside in the scene
    /// authority's loaded scene instance.
    /// This helper assures all instances are migrated into their NetworkManager
    /// relative scene in order to replicate a more "real world" scenario
    /// where each spawned clone instance for each connected client will be
    /// destroyed during that client's processing of the unload scene
    /// event.
    /// Without this helper (or similar logic elsewhere), everything would be
    /// destroyed upon the authority's scene being unloaded while the rest of
    /// the clients would unload empty scenes.
    /// </remarks>
    internal class SceneSynchronizer : NetworkBehaviour
    {
        /// <summary>
        /// We are using the NetworkBehaviour to provide us with the relative
        /// NetworkManager instance (for this spawned instance) in order to
        /// then check against the client relative scenes loaded.
        /// </summary>
        /// <returns>Success if true. Failed to find the scene if false.</returns>
        public bool SynchronizeScene()
        {
            var scenes = NetworkManager.SceneManager.GetSynchronizedScenes();
            foreach (var scene in scenes)
            {
                // Find the matching scene name
                if (gameObject.scene.name == scene.name)
                {
                    // If the scene handles are different, then migrate to the
                    // one registered with this NetworkSceneManager instance.
                    if (scene.handle != gameObject.scene.handle)
                    {
                        SceneManager.MoveGameObjectToScene(gameObject, scene);
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
