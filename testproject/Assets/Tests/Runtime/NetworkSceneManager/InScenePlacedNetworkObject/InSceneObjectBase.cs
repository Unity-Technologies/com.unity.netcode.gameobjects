using System.Collections;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.SceneManagement;

namespace TestProject.RuntimeTests
{
    public class InSceneObjectBase : IntegrationTestWithApproximation
    {
        protected override int NumberOfClients => 2;

        internal const string SceneToLoad = "InSceneNetworkObject";
        private Scene m_AuthoritySideSceneLoaded;
        private Scene m_PreviousSceneLoaded;

        protected InSceneObjectBase(NetworkTopologyTypes networkTopologyType, HostOrServer hostOrServer) : base(networkTopologyType, hostOrServer) { }

        // Constructor that is used by InScenePlacedNetworkObjectDestroyTests
        protected InSceneObjectBase(NetworkTopologyTypes networkTopologyType) : base(networkTopologyType) { }

        protected override IEnumerator OnSetup()
        {
            NetworkObjectTestComponent.Reset();
            NetworkObjectTestComponent.VerboseDebug = m_EnableVerboseDebug;
            m_AuthoritySideSceneLoaded = default;
            return base.OnSetup();
        }

        /// <summary>
        /// Very important to always have a backup "unloading" catch
        /// in the event your test fails it could not potentially unload
        /// a scene and the proceeding tests could be impacted by this!
        /// </summary>
        /// <returns></returns>
        protected override IEnumerator OnTearDown()
        {
            GetAuthorityNetworkManager().SceneManager.OnSceneEvent -= OnSceneEvent;
            NetworkObjectTestComponent.Reset();
            yield return CleanUpLoadedScene();
        }

        protected override IEnumerator OnStartedServerAndClients()
        {
            GetAuthorityNetworkManager().SceneManager.OnSceneEvent += OnSceneEvent;
            return base.OnStartedServerAndClients();
        }

        /// <summary>
        /// Gets the currently loaded scene.
        /// If multiple scenes are loaded this will get the most recently loaded scene
        /// </summary>
        internal Scene GetLoadedScene()
        {
            return m_AuthoritySideSceneLoaded;
        }

        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.Load:
                    // Reset the loaded scene when the load starts to avoid data leaking
                    // m_AuthoritySideSceneLoaded tracks the most recently loaded scene.
                    // Any existing scene is no longer valid when a new scene begins loading.
                    m_AuthoritySideSceneLoaded = default;
                    break;
                case SceneEventType.LoadComplete:
                    if (sceneEvent.ClientId == GetAuthorityNetworkManager().LocalClientId && sceneEvent.Scene.IsValid() && sceneEvent.Scene.isLoaded)
                    {
                        m_AuthoritySideSceneLoaded = sceneEvent.Scene;
                    }
                    break;
                case SceneEventType.Unload:
                    m_PreviousSceneLoaded = m_AuthoritySideSceneLoaded;
                    m_AuthoritySideSceneLoaded = default;
                    break;
            }
        }

        private IEnumerator CleanUpLoadedScene()
        {
            if (m_AuthoritySideSceneLoaded.IsValid() && m_AuthoritySideSceneLoaded.isLoaded)
            {
                VerboseDebug($"Cleaning up loaded scene [{m_AuthoritySideSceneLoaded.name}-{m_AuthoritySideSceneLoaded.handle}]");
                var authority = GetAuthorityNetworkManager();
                authority.SceneManager.UnloadScene(m_AuthoritySideSceneLoaded);
                yield return WaitForConditionOrTimeOut(() => m_ClientNetworkManagers.Any(c => c.IsListening));
                AssertOnTimeout($"[CleanUpLoadedScene] Timed out waiting for all in-scene instances to be despawned!  Current spawned count: {m_ClientNetworkManagers.Count(c => !c.IsListening)}");
            }
        }

        /// <summary>
        /// Checks if all clients have loaded the most recently loaded scene.
        /// </summary>
        internal bool HaveAllClientsLoadedScene()
        {
            if (!(m_AuthoritySideSceneLoaded.IsValid() && m_AuthoritySideSceneLoaded.isLoaded))
            {
                return false;
            }

            foreach (var manager in m_NetworkManagers)
            {
                // default will have isLoaded as false so we can get the scene or default and test on isLoaded
                var loadedScene = manager.SceneManager.ScenesLoaded.Values.FirstOrDefault(scene => scene.name == m_AuthoritySideSceneLoaded.name);
                if (!loadedScene.isLoaded)
                {
                    return false;
                }

                if (manager.SceneManager.SceneEventProgressTracking.Count > 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if all clients have unloaded the most recently loaded scene
        /// </summary>
        internal bool HaveAllClientsUnloadedScene()
        {
            if (m_AuthoritySideSceneLoaded.IsValid() || m_AuthoritySideSceneLoaded.isLoaded)
            {
                return false;
            }

            foreach (var manager in m_NetworkManagers)
            {
                if (manager.SceneManager.ScenesLoaded.Values.Any(scene => scene.name == m_PreviousSceneLoaded.name))
                {
                    return false;
                }

                if (manager.SceneManager.SceneEventProgressTracking.Count > 0)
                {
                    return false;
                }
            }
            return true;
        }


        internal bool HaveAllClientsDespawnedInSceneObject()
        {
            // Make sure we despawned all instances
            if (NetworkObjectTestComponent.DespawnedInstances.Count < TotalClients)
            {
                return false;
            }

            foreach (var despawnedInstance in NetworkObjectTestComponent.DespawnedInstances)
            {
                if (despawnedInstance && despawnedInstance.gameObject && despawnedInstance.gameObject.activeInHierarchy)
                {
                    return false;
                }
            }

            return true;
        }

        internal bool HaveAllClientsSpawnedInSceneObject()
        {
            // Make sure we despawned all instances
            if (NetworkObjectTestComponent.SpawnedInstances.Count < TotalClients)
            {
                return false;
            }

            foreach (var despawnedInstance in NetworkObjectTestComponent.SpawnedInstances)
            {
                if (!despawnedInstance.gameObject.activeInHierarchy)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
