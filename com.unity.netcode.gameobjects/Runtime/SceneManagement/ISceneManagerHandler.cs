using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Netcode
{
    /// <summary>
    /// Used to override the LoadSceneAsync and UnloadSceneAsync methods called
    /// within the NetworkSceneManager.
    /// </summary>
    internal interface ISceneManagerHandler
    {
        public AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, SceneEventProgress sceneEventProgress);

        public AsyncOperation UnloadSceneAsync(Scene scene, SceneEventProgress sceneEventProgress);

        /// <summary>
        /// Loads an Addressable scene by its address. Returns the <see cref="ISceneEventOperation"/>
        /// used to track completion (there is no <see cref="AsyncOperation"/> available for an
        /// Addressables load until after the underlying handle completes).
        /// </summary>
        public ISceneEventOperation LoadAddressableSceneAsync(string address, LoadSceneMode loadSceneMode, SceneEventProgress sceneEventProgress);

        /// <summary>
        /// Unloads a scene that was previously loaded via <see cref="LoadAddressableSceneAsync"/>.
        /// </summary>
        public ISceneEventOperation UnloadAddressableSceneAsync(Scene scene, SceneEventProgress sceneEventProgress);

        /// <summary>
        /// Returns true if the provided scene was loaded via Addressables (and therefore must be
        /// unloaded via <see cref="UnloadAddressableSceneAsync"/>).
        /// </summary>
        public bool IsAddressableSceneLoaded(Scene scene);

        /// <summary>
        /// Attempts to resolve the actual loaded <see cref="Scene.name"/> for a previously loaded
        /// Addressable scene, keyed by its address. The name is only known once the scene has finished
        /// loading (the address alone does not determine the scene's runtime name).
        /// </summary>
        public bool TryGetAddressableSceneName(string address, out string sceneName);

        /// <summary>
        /// Attempts to resolve the Addressable address a loaded scene was loaded from. Used to map a
        /// loaded <see cref="Scene"/> back to its wire hash during client synchronization.
        /// </summary>
        public bool TryGetAddressableSceneAddress(Scene scene, out string address);

        public void PopulateLoadedScenes(ref Dictionary<NetworkSceneHandle, Scene> scenesLoaded, NetworkManager networkManager = null);
        public Scene GetSceneFromLoadedScenes(string sceneName, NetworkManager networkManager = null);

        public bool DoesSceneHaveUnassignedEntry(string sceneName, NetworkManager networkManager = null);

        public void StopTrackingScene(NetworkSceneHandle handle, string name, NetworkManager networkManager = null);

        public void StartTrackingScene(Scene scene, bool assigned, NetworkManager networkManager = null);

        public void ClearSceneTracking(NetworkManager networkManager = null);

        public void UnloadUnassignedScenes(NetworkManager networkManager = null);

        public void MoveObjectsFromSceneToDontDestroyOnLoad(ref NetworkManager networkManager, Scene scene);

        public void SetClientSynchronizationMode(ref NetworkManager networkManager, LoadSceneMode mode);

        public bool ClientShouldPassThrough(string sceneName, bool isPrimaryScene, LoadSceneMode clientSynchronizationMode, NetworkManager networkManager);

        public bool IsIntegrationTest();
    }
}
