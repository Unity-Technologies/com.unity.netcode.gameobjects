using System.Collections.Generic;
using MLAPI.Data;
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
        internal static Dictionary<uint, uint> clientsLatestConfirmedLoadedScene = new Dictionary<uint, uint>();

        /// <summary>
        /// Are all clients done loading the latest scene that the server requested, only valid on the server.
        /// </summary>
        public static bool HasAllClientsLoadedLatestScene()
        {
            for(int i=0; i< NetworkingManager.singleton.ConnectedClientsList.Count; i++) 
            {
                if (!clientsLatestConfirmedLoadedScene.ContainsKey(NetworkingManager.singleton.ConnectedClientsList[i].ClientId))
                {
                    return false;
                }
                if (clientsLatestConfirmedLoadedScene[NetworkingManager.singleton.ConnectedClientsList[i].ClientId] != CurrentSceneIndex)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Get the number of clients that are done loading the latest scene that the server requested, only valid on the server.
        /// </summary>
        /// <returns></returns>
        public static int GetNumberOfClientsDoneLoadingLatestScene() 
        {
            int doneLoadingCount = 0;
            for (int i = 0; i < NetworkingManager.singleton.ConnectedClientsList.Count; i++) 
            {
                if (clientsLatestConfirmedLoadedScene.ContainsKey(NetworkingManager.singleton.ConnectedClientsList[i].ClientId)
                    && clientsLatestConfirmedLoadedScene[NetworkingManager.singleton.ConnectedClientsList[i].ClientId] == CurrentSceneIndex)
                {
                    doneLoadingCount++;
                }
            }
            return doneLoadingCount;
        }

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
            nextScene = SceneManager.GetSceneByName(sceneName);
            sceneLoad.completed += OnSceneLoaded;

            clientsLatestConfirmedLoadedScene.Clear();

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt32Packed(sceneNameToIndex[sceneName]);

                    InternalMessageHandler.Send(MLAPIConstants.MLAPI_SWITCH_SCENE, "MLAPI_INTERNAL", stream);
                }
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

            string sceneName = sceneIndexToString[sceneIndex];
            AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            nextScene = SceneManager.GetSceneByName(sceneName);
            sceneLoad.completed += OnSceneLoaded;
        }

        private static void OnSceneLoaded(AsyncOperation operation)
        {
            SceneManager.SetActiveScene(nextScene);
            
            List<NetworkedObject> objectsToKeep = SpawnManager.SpawnedObjectsList;
            for (int i = 0; i < objectsToKeep.Count; i++)
            {
                SceneManager.MoveGameObjectToScene(objectsToKeep[i].gameObject, nextScene);
            }
            AsyncOperation sceneLoad = SceneManager.UnloadSceneAsync(lastScene);
            sceneLoad.completed += OnSceneUnload;

            if (NetworkingManager.singleton.isHost) 
            {
                OnClientSwitchSceneCompleted(NetworkingManager.singleton.LocalClientId, sceneNameToIndex[nextScene.name]);
            }
            else if (NetworkingManager.singleton.isClient) 
            { 
                using (PooledBitStream stream = PooledBitStream.Get()) 
                {
                    using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                    {
                        writer.WriteUInt32Packed(sceneNameToIndex[nextScene.name]);
                        InternalMessageHandler.Send(MLAPIConstants.MLAPI_CLIENT_SWITCH_SCENE_COMPLETED, "MLAPI_INTERNAL", stream);
                    }
                }
            }
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



        /// <summary>
        /// Called on server
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="sceneIndex"></param>
        internal static void OnClientSwitchSceneCompleted(uint clientId, uint sceneIndex) 
        {
            if (!NetworkingManager.singleton.NetworkConfig.EnableSceneSwitching)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Scene switching is not enabled but was confirmed done by a client");
                return;
            }
            else if (!sceneIndexToString.ContainsKey(sceneIndex) || !registeredSceneNames.Contains(sceneIndexToString[sceneIndex])) 
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Client requested a scene switch as done to a non registered scene");
                return;
            }

            if (!clientsLatestConfirmedLoadedScene.ContainsKey(clientId)) 
                clientsLatestConfirmedLoadedScene.Add(clientId, sceneIndex);
            else 
                clientsLatestConfirmedLoadedScene[clientId] = sceneIndex;
        }

    }
}
