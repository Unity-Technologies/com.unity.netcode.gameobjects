using System.Collections.Generic;
using System;
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
        internal static Guid CurrentSceneSwitchProgressGuid = new Guid();
        internal static Dictionary<Guid, SwitchSceneProgress> switchSceneProgresses = new Dictionary<Guid, SwitchSceneProgress> ();

        internal static void SetCurrentSceneIndex ()
        {
            if(!sceneNameToIndex.ContainsKey(SceneManager.GetActiveScene().name))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Scene switching is enabled but the current scene (" + SceneManager.GetActiveScene().name + ") is not regisered as a network scene.");
                return;
            }
            CurrentSceneIndex = sceneNameToIndex[SceneManager.GetActiveScene().name];
            CurrentActiveSceneIndex = CurrentSceneIndex;
        }

        internal static uint CurrentActiveSceneIndex { get; private set; } = 0;

        /// <summary>
        /// Switches to a scene with a given name. Can only be called from Server
        /// </summary>
        /// <param name="sceneName">The name of the scene to switch to</param>
        public static SwitchSceneProgress SwitchScene(string sceneName)
        {
            if(!NetworkingManager.singleton.NetworkConfig.EnableSceneSwitching)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Scene switching is not enabled");
                return null;
            }
            else if (isSwitching)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Scene switch already in progress");
                return null;
            }
            else if(!registeredSceneNames.Contains(sceneName))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The scene " + sceneName + " is not registered as a switchable scene.");
                return null;
            }
            SpawnManager.DestroySceneObjects(); //Destroy current scene objects before switching.
            CurrentSceneIndex = sceneNameToIndex[sceneName];
            isSwitching = true;
            lastScene = SceneManager.GetActiveScene();

            SwitchSceneProgress switchSceneProgress = new SwitchSceneProgress();
            switchSceneProgresses.Add(switchSceneProgress.guid, switchSceneProgress);
            CurrentSceneSwitchProgressGuid = switchSceneProgress.guid;

            AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            nextScene = SceneManager.GetSceneByName(sceneName);
            sceneLoad.completed += (AsyncOperation operation) => { OnSceneLoaded(operation, switchSceneProgress.guid); };

            switchSceneProgress.setSceneLoadOperation(sceneLoad);

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt32Packed(sceneNameToIndex[sceneName]);
                    writer.WriteByteArray(switchSceneProgress.guid.ToByteArray());

                    InternalMessageHandler.Send(MLAPIConstants.MLAPI_SWITCH_SCENE, "MLAPI_INTERNAL", stream);
                }
            }
            return switchSceneProgress;
        }

        /// <summary>
        /// Called on client
        /// </summary>
        /// <param name="sceneIndex"></param>
        /// <param name="switchSceneGuid"></param>
        internal static void OnSceneSwitch(uint sceneIndex, Guid switchSceneGuid)
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
            //SpawnManager.DestroySceneObjects();
            lastScene = SceneManager.GetActiveScene();

            string sceneName = sceneIndexToString[sceneIndex];
            AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            nextScene = SceneManager.GetSceneByName(sceneName);
            sceneLoad.completed += (AsyncOperation operation) => { OnSceneLoaded(operation, switchSceneGuid); };
        }

        private static void OnSceneLoaded(AsyncOperation operation, Guid switchSceneGuid)
        {
            CurrentActiveSceneIndex = sceneNameToIndex[nextScene.name];
            SceneManager.SetActiveScene(nextScene);
            
            List<NetworkedObject> objectsToKeep = SpawnManager.SpawnedObjectsList;
            objectsToKeep.AddRange(SpawnManager.GetPendingSpawnObjectsList());
            for (int i = 0; i < objectsToKeep.Count; i++)
            {
                if(objectsToKeep[i].gameObject.transform.parent != null)
                {
                    objectsToKeep[i].gameObject.transform.parent = null;
                }
                SceneManager.MoveGameObjectToScene(objectsToKeep[i].gameObject, nextScene);
            }
            AsyncOperation sceneLoad = SceneManager.UnloadSceneAsync(lastScene);
            sceneLoad.completed += OnSceneUnload;

            if (NetworkingManager.singleton.isHost) 
            {
                OnClientSwitchSceneCompleted(NetworkingManager.singleton.LocalClientId, switchSceneGuid);
            }
            else if (NetworkingManager.singleton.isClient) 
            { 
                using (PooledBitStream stream = PooledBitStream.Get()) 
                {
                    using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                    {
                        writer.WriteByteArray(switchSceneGuid.ToByteArray());
                        InternalMessageHandler.Send(MLAPIConstants.MLAPI_CLIENT_SWITCH_SCENE_COMPLETED, "MLAPI_INTERNAL", stream);
                    }
                }
            }



            if (NetworkingManager.singleton.isServer)
            {
                SpawnManager.MarkSceneObjects();
                NetworkedObject[] networkedObjects = MonoBehaviour.FindObjectsOfType<NetworkedObject>();
                for (int i = 0; i < networkedObjects.Length; i++)
                {
                    if (!networkedObjects[i].isSpawned && networkedObjects[i].sceneObject == true)
                        networkedObjects[i].Spawn(null, true);
                }
            }
            else
            {
                SpawnManager.spawnPendingObjectsForScene(CurrentActiveSceneIndex);

                NetworkedObject[] netObjects = MonoBehaviour.FindObjectsOfType<NetworkedObject>();
                for (int i = 0; i < netObjects.Length; i++)
                {
                    if (netObjects[i].sceneObject == null)
                        MonoBehaviour.Destroy(netObjects[i].gameObject);
                }
            }

        }

        private static void OnSceneUnload(AsyncOperation operation)
        {
            isSwitching = false;
        }

        /* 
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
        */

        /// <summary>
        /// Called on server
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="switchSceneGuid"></param>
        internal static void OnClientSwitchSceneCompleted(uint clientId, Guid switchSceneGuid) 
        {
            if (!NetworkingManager.singleton.NetworkConfig.EnableSceneSwitching)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Scene switching is not enabled but was confirmed done by a client");
                return;
            }
            if(switchSceneGuid == Guid.Empty) 
            {
                //If Guid is empty it means the client has loaded the start scene of the server and the server would never have a switchSceneProgresses created for the start scene.
                return;
            }
            if (!switchSceneProgresses.ContainsKey(switchSceneGuid)) 
            {
                return;
            }

            switchSceneProgresses[switchSceneGuid].addClientAsDone(clientId);
        }


        internal static void removeClientFromSceneSwitchProgresses(uint clientId) 
        {
            foreach (SwitchSceneProgress switchSceneProgress in switchSceneProgresses.Values)
                switchSceneProgress.removeClientAsDone(clientId);
        }
    }

    /// <summary>
    /// Class for tracking scene switching progress by server and clients.
    /// </summary>
    public class SwitchSceneProgress 
    {

        internal SwitchSceneProgress()
        {
            timeOutCoroutine = NetworkingManager.singleton.StartCoroutine(NetworkingManager.singleton.TimeOutSwitchSceneProgress(this));
        }

        private Coroutine timeOutCoroutine;
        private Guid _guid = Guid.NewGuid();
        internal Guid guid => _guid;

        private AsyncOperation sceneLoadOperation;

        /// <summary>
        /// Array of clientIds of those clients that is done loading the scene.
        /// </summary>
        public uint[] doneClients => _doneClients.ToArray();
        private List<uint> _doneClients = new List<uint>();
        /// <summary>
        /// The NetworkTime time at the moment the scene switch was initiated by the server.
        /// </summary>
        public float timeAtInitiation => _timeAtInitiation;
        private float _timeAtInitiation = NetworkingManager.singleton.NetworkTime;
        /// <summary>
        /// Delegate type for when the switch scene progress is completed. Either by all clients done loading the scene or by time out.
        /// </summary>
        public delegate void OnCompletedDelegate(bool timedOut);
        /// <summary>
        /// The callback invoked when the switch scene progress is completed. Either by all clients done loading the scene or by time out.
        /// </summary>
        public event OnCompletedDelegate onCompleted;
        /// <summary>
        /// Is this scene switch progresses completed, all clients are done loading the scene or a timeout has occured.
        /// </summary>
        public bool isCompleted => completed;
        private bool completed = false;
        /// <summary>
        /// If all clients are done loading the scene, at the moment of completed.
        /// </summary>
        public bool isAllClientsDoneLoading => allClientsDoneLoading;
        private bool allClientsDoneLoading = false;
        /// <summary>
        /// Delegate type for when a client is done loading the scene.
        /// </summary>
        public delegate void OnClientLoadedSceneDelegate(uint clientId);
        /// <summary>
        /// The callback invoked when a client is done loading the scene.
        /// </summary>
        public event OnClientLoadedSceneDelegate OnClientLoadedScene;

        internal void addClientAsDone(uint clientId) 
        {
            _doneClients.Add(clientId);
            if (OnClientLoadedScene != null)
                OnClientLoadedScene.Invoke(clientId);
            checkCompletion();
        }
        internal void removeClientAsDone(uint clientId) 
        {
            _doneClients.Remove(clientId);
            checkCompletion();
        }
        internal void setSceneLoadOperation(AsyncOperation sceneLoadOperation)
        {
            this.sceneLoadOperation = sceneLoadOperation;
            this.sceneLoadOperation.completed += (AsyncOperation operation) => { checkCompletion(); };
        }
        internal void checkCompletion() 
        {
            if (!completed && _doneClients.Count == NetworkingManager.singleton.ConnectedClientsList.Count && sceneLoadOperation.isDone) 
            {
                completed = true;
                allClientsDoneLoading = true;
                NetworkSceneManager.switchSceneProgresses.Remove(_guid);
                if (onCompleted != null)
                    onCompleted.Invoke(false);

                NetworkingManager.singleton.StopCoroutine(timeOutCoroutine);
            }
        }
        internal void setTimedOut() 
        {
            if (!completed)
            { 
                completed = true;
                NetworkSceneManager.switchSceneProgresses.Remove(_guid);
                if (onCompleted != null)
                    onCompleted.Invoke(true);
            }
        }

    }
}
