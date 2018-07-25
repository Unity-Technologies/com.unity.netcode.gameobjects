using System.Collections.Generic;
using MLAPI.Internal;
using MLAPI.Logging;
using MLAPI.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MLAPI.Components
{
    /// <summary>
    /// Main class for managing network scenes
    /// </summary>
    public static class NetworkSceneManager
    {
        internal static readonly HashSet<string> registeredSceneNames = new HashSet<string>();
        internal static readonly Dictionary<string, uint> sceneNameToIndex = new Dictionary<string, uint>();
        internal static readonly Dictionary<uint, string> sceneIndexToString = new Dictionary<uint, string>();
        private static Scene lastScene;
        private static Scene nextScene;
        private static bool isSwitching = false;
        internal static uint CurrentSceneIndex = 0;

        internal static void SetCurrentSceneIndex ()
        {
            if(!sceneNameToIndex.ContainsKey(SceneManager.GetActiveScene().name))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Scene switching is enabled but the current scene (" + SceneManager.GetActiveScene().name + ") is not regisered as a network scene.");
                return;
            }
            CurrentSceneIndex = sceneNameToIndex[SceneManager.GetActiveScene().name];
        }

        /// <summary>
        /// Switches to a scene with a given name. Can only be called from Server
        /// </summary>
        /// <param name="sceneName">The name of the scene to switch to</param>
        public static void SwitchScene(string sceneName)
        {
            if(!NetworkingManager.singleton.NetworkConfig.EnableSceneSwitching)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Scene switching is not enabled");
                return;
            }
            else if (isSwitching)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Scene switch already in progress");
                return;
            }
            else if(!registeredSceneNames.Contains(sceneName))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The scene " + sceneName + " is not registered as a switchable scene.");
                return;
            }
            SpawnManager.DestroySceneObjects(); //Destroy current scene objects before switching.
            CurrentSceneIndex = sceneNameToIndex[sceneName];
            isSwitching = true;
            lastScene = SceneManager.GetActiveScene();
            AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            sceneLoad.completed += OnSceneLoaded;

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                BitWriter writer = new BitWriter(stream);
                writer.WriteUInt32Packed(sceneNameToIndex[sceneName]);

                InternalMessageHandler.Send("MLAPI_SWITCH_SCENE", "MLAPI_INTERNAL", stream);
            }
        }

        /// <summary>
        /// Called on client
        /// </summary>
        /// <param name="sceneIndex"></param>
        internal static void OnSceneSwitch(uint sceneIndex)
        {
            if (!NetworkingManager.singleton.NetworkConfig.EnableSceneSwitching)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Scene switching is not enabled but was requested by the server");
                return;
            }
            else if (!sceneIndexToString.ContainsKey(sceneIndex) || !registeredSceneNames.Contains(sceneIndexToString[sceneIndex]))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Server requested a scene switch to a non registered scene");
                return;
            }
            else if(SceneManager.GetActiveScene().name == sceneIndexToString[sceneIndex])
            {
                return; //This scene is already loaded. This usually happends at first load
            }
            SpawnManager.DestroySceneObjects();
            lastScene = SceneManager.GetActiveScene();
            AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(sceneIndexToString[sceneIndex], LoadSceneMode.Additive);
            sceneLoad.completed += OnSceneLoaded;
        }

        private static void OnSceneLoaded(AsyncOperation operation)
        {
            SceneManager.SetActiveScene(nextScene);
            List<NetworkedObject> objectsToKeep = SpawnManager.SpawnedObjectsList;
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
            if (NetworkingManager.singleton.isServer)
            {
                SpawnManager.MarkSceneObjects();

                NetworkedObject[] networkedObjects = MonoBehaviour.FindObjectsOfType<NetworkedObject>();
                for (int i = 0; i < networkedObjects.Length; i++)
                {
                    if (!networkedObjects[i].isSpawned && (networkedObjects[i].sceneObject == null || networkedObjects[i].sceneObject == true))
                        networkedObjects[i].Spawn();
                }

                //SpawnManager.FlushSceneObjects();
            }
            else
            {
                SpawnManager.DestroySceneObjects();
            }
        }
    }
}
