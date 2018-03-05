using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MLAPI.NetworkingManagerComponents
{
    public static class NetworkSceneManager
    {
        internal static HashSet<string> registeredSceneNames;
        internal static Dictionary<string, uint> sceneNameToIndex;
        internal static Dictionary<uint, string> sceneIndexToString;
        private static Scene lastScene;
        private static Scene nextScene;
        private static bool isSwitching = false;
        internal static uint CurrentSceneIndex = 0;

        internal static void SetCurrentSceneIndex ()
        {
            CurrentSceneIndex = sceneNameToIndex[SceneManager.GetActiveScene().name];
        }

        public static void SwitchScene(string sceneName)
        {
            if(!NetworkingManager.singleton.NetworkConfig.EnableSceneSwitching)
            {
                Debug.LogWarning("MLAPI: Scene switching is not enabled");
                return;
            }
            else if (isSwitching)
            {
                Debug.LogWarning("MLAPI: Scene switch already in progress");
                return;
            }
            else if(!registeredSceneNames.Contains(sceneName))
            {
                Debug.LogWarning("MLAPI: The scene " + sceneName + " is not registered as a switchable scene.");
                return;
            }
            CurrentSceneIndex = sceneNameToIndex[sceneName];
            isSwitching = true;
            lastScene = SceneManager.GetActiveScene();
            AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            sceneLoad.completed += OnSceneLoaded;
            using(MemoryStream stream = new MemoryStream(4))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(sceneNameToIndex[sceneName]);
                }
                NetworkingManager.singleton.Send("MLAPI_SWITCH_SCENE", "MLAPI_RELIABLE_FRAGMENTED_SEQUENCED", stream.GetBuffer());
            }
        }

        internal static void OnSceneSwitch(uint sceneIndex)
        {
            if (!NetworkingManager.singleton.NetworkConfig.EnableSceneSwitching)
            {
                Debug.LogWarning("MLAPI: Scene switching is not enabled but was requested by the server");
                return;
            }
            else if (!sceneIndexToString.ContainsKey(sceneIndex) || registeredSceneNames.Contains(sceneIndexToString[sceneIndex]))
            {
                Debug.LogWarning("MLAPI: Server requested a scene switch to a non registered scene");
                return;
            }
            lastScene = SceneManager.GetActiveScene();
            AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(sceneIndexToString[sceneIndex], LoadSceneMode.Additive);
            sceneLoad.completed += OnSceneLoaded;
        }

        private static void OnSceneLoaded(AsyncOperation operation)
        {
            List<NetworkedObject> objectsToKeep = SpawnManager.spawnedObjects.Values.ToList();
            //The last loaded scene
            nextScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            for (int i = 0; i < objectsToKeep.Count; i++)
            {
                SceneManager.MoveGameObjectToScene(objectsToKeep[i].gameObject, nextScene);
            }
            AsyncOperation sceneLoad = SceneManager.UnloadSceneAsync(lastScene);
            sceneLoad.completed += OnSceneUnload;
        }

        private static void OnSceneUnload(AsyncOperation operation)
        {
            isSwitching = false;
        }
    }
}
