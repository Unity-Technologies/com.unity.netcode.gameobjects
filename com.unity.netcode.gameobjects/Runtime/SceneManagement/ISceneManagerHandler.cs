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
        AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, SceneEventProgress sceneEventProgress);

        AsyncOperation UnloadSceneAsync(Scene scene, SceneEventProgress sceneEventProgress);

        void PopulateLoadedScenes(ref Dictionary<int, Scene> scenesLoaded, NetworkManager networkManager = null);
        Scene GetSceneFromLoadedScenes(string sceneName, NetworkManager networkManager = null);

        bool DoesSceneHaveUnassignedEntry(string sceneName, NetworkManager networkManager = null);

        void StopTrackingScene(int handle, string name, NetworkManager networkManager = null);

        void StartTrackingScene(Scene scene, bool assigned, NetworkManager networkManager = null);

        void ClearSceneTracking(NetworkManager networkManager = null);

        void UnloadUnassignedScenes(NetworkManager networkManager = null);

        void MoveObjectsFromSceneToDontDestroyOnLoad(ref NetworkManager networkManager, Scene scene);

        void SetClientSynchronizationMode(ref NetworkManager networkManager, LoadSceneMode mode);

        bool ClientShouldPassThrough(string sceneName, bool isPrimaryScene, LoadSceneMode clientSynchronizationMode, NetworkManager networkManager);
    }
}
