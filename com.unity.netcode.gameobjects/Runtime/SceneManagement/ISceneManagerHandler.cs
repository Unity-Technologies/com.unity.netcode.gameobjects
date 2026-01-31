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
