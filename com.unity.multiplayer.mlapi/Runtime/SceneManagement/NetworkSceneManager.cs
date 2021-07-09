using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using MLAPI.Configuration;
using MLAPI.Exceptions;
using MLAPI.Logging;
using MLAPI.Messaging;
using MLAPI.Serialization.Pooled;
using MLAPI.Spawning;
using UnityEngine;
using UnityEngine.SceneManagement;
using MLAPI.Messaging.Buffering;
using MLAPI.Serialization;
using MLAPI.Transports;

namespace MLAPI.SceneManagement
{
    /// <summary>
    /// Main class for managing network scenes
    /// </summary>
    public class NetworkSceneManager
    {
        /// <summary>
        /// Delegate for when the scene has been switched
        /// </summary>
        public delegate void SceneSwitchedDelegate();

        /// <summary>
        /// Delegate for when a scene switch has been initiated
        /// </summary>
        public delegate void SceneSwitchStartedDelegate(AsyncOperation operation);

        /// <summary>
        /// Delegate for when a client has reported to the server that it has completed scene transition
        /// <see cref='OnNotifyServerClientLoadedScene'/>
        /// </summary>
        public delegate void NotifyServerClientLoadedSceneDelegate(SceneSwitchProgress progress, ulong clientId);

        /// <summary>
        /// Delegate for when all clients have reported to the server that they have completed scene transition or timed out
        /// <see cref='OnNotifyServerAllClientsLoadedScene'/>
        /// </summary>
        public delegate void NotifyServerAllClientsLoadedSceneDelegate(SceneSwitchProgress progress, bool timedOut);

        /// <summary>
        /// Delegate for when the clients get notified by the server that all clients have completed their scene transitions.
        /// <see cref='OnNotifyClientAllClientsLoadedScene'/>
        /// </summary>
        public delegate void NotifyClientAllClientsLoadedSceneDelegate(ulong[] clientIds, ulong[] timedOutClientIds);

        /// <summary>
        /// Event that is invoked when the scene is switched
        /// </summary>
        public event SceneSwitchedDelegate OnSceneSwitched;

        /// <summary>
        /// Event that is invoked when a local scene switch has started
        /// </summary>
        public event SceneSwitchStartedDelegate OnSceneSwitchStarted;

        /// <summary>
        /// Event that is invoked on the server when a client completes scene transition
        /// </summary>
        public event NotifyServerClientLoadedSceneDelegate OnNotifyServerClientLoadedScene;

        /// <summary>
        /// Event that is invoked on the server when all clients have reported that they have completed scene transition
        /// </summary>
        public event NotifyServerAllClientsLoadedSceneDelegate OnNotifyServerAllClientsLoadedScene;

        /// <summary>
        /// Event that is invoked on the clients after all clients have successfully completed scene transition or timed out.
        /// <remarks>This event happens after <see cref="OnNotifyServerAllClientsLoadedScene"/> fires on the server and the <see cref="NetworkConstants.ALL_CLIENTS_LOADED_SCENE"/> message is sent to the clients.
        /// It relies on MessageSender, which doesn't send events from the server to itself (which is the case for a Host client).</remarks>
        /// </summary>
        public event NotifyClientAllClientsLoadedSceneDelegate OnNotifyClientAllClientsLoadedScene;

        internal readonly HashSet<string> RegisteredSceneNames = new HashSet<string>();
        internal readonly Dictionary<string, uint> SceneNameToIndex = new Dictionary<string, uint>();
        internal readonly Dictionary<uint, string> SceneIndexToString = new Dictionary<uint, string>();
        internal readonly Dictionary<Guid, SceneSwitchProgress> SceneSwitchProgresses = new Dictionary<Guid, SceneSwitchProgress>();
        internal readonly Dictionary<uint, NetworkObject> ScenePlacedObjects = new Dictionary<uint, NetworkObject>();

        private static string s_NextSceneName;
        private static bool s_IsSwitching = false;
        internal static uint CurrentSceneIndex = 0;
        internal static Guid CurrentSceneSwitchProgressGuid = new Guid();
        internal static bool IsSpawnedObjectsPendingInDontDestroyOnLoad = false;

        internal SceneEventData SceneEventData;

        internal SceneEventData ClientSynchEventData; // For approval/late joining purposes

        private NetworkManager m_NetworkManager { get; }

        internal NetworkSceneManager(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            SceneEventData = new SceneEventData();
            ClientSynchEventData = new SceneEventData();
        }

        internal void SetCurrentSceneIndex()
        {
            var sceneIndex = GetMLAPISceneIndex(SceneManager.GetActiveScene());
            if (sceneIndex != uint.MaxValue)
            {
                CurrentActiveSceneIndex = CurrentSceneIndex = sceneIndex;
            }
        }


        private uint GetMLAPISceneIndex(Scene scene)
        {
            uint index = 0;
            if (!SceneNameToIndex.TryGetValue(scene.name, out index))
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning($"The current scene ({scene.name}) is not registered as a network scene.");
                }
                //MaxValue denotes an error
                return uint.MaxValue;
            }
            return index;
        }


        private string GetMLAPISceneNameFromIndex(uint sceneIndex)
        {
            var sceneName = string.Empty;
            if (!SceneIndexToString.TryGetValue(sceneIndex, out sceneName))
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning($"The current scene index ({sceneIndex}) is not registered as a network scene.");
                }
            }
            return sceneName;
        }


        internal uint CurrentActiveSceneIndex { get; private set; } = 0;

        /// <summary>
        /// Adds a scene during runtime.
        /// The index is REQUIRED to be unique AND the same across all instances.
        /// </summary>
        /// <param name="sceneName">Scene name.</param>
        /// <param name="index">Index.</param>
        public void AddRuntimeSceneName(string sceneName, uint index)
        {
            if (!m_NetworkManager.NetworkConfig.AllowRuntimeSceneChanges)
            {
                throw new NetworkConfigurationException($"Cannot change the scene configuration when {nameof(NetworkConfig.AllowRuntimeSceneChanges)} is false");
            }

            RegisteredSceneNames.Add(sceneName);
            SceneIndexToString.Add(index, sceneName);
            SceneNameToIndex.Add(sceneName, index);
        }

        /// <summary>
        /// Validates the new scene event request by the server-side code.
        /// This also initializes some commonly shared values as well as switchSceneProgress
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns>SceneSwitchProgress (if null it failed)</returns>
        private SceneSwitchProgress ValidateServerSceneEvent(string sceneName, bool isUnloading = false)
        {
            if (!m_NetworkManager.IsServer)
            {
                throw new NotServerException("Only server can start a scene switch");
            }

            if (!m_NetworkManager.NetworkConfig.EnableSceneManagement)
            {
                //Log message about enabling SceneManagement
                throw new Exception($"{nameof(NetworkConfig.EnableSceneManagement)} flag is not enabled in the {nameof(NetworkManager)}'s {nameof(NetworkConfig)}. Please set {nameof(NetworkConfig.EnableSceneManagement)} flag to true before calling this method.");
            }

            if (s_IsSwitching)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning("Scene switch already in progress");
                }

                return null;
            }

            if (!RegisteredSceneNames.Contains(sceneName))
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning($"The scene {sceneName} is not registered as a switchable scene.");
                }

                return null;
            }

            var switchSceneProgress = new SceneSwitchProgress(m_NetworkManager);
            SceneSwitchProgresses.Add(switchSceneProgress.Guid, switchSceneProgress);

            // NSS TODO: remove any of these values that are no longer needed
            CurrentSceneSwitchProgressGuid = switchSceneProgress.Guid;
            if (!isUnloading)
            {
                CurrentActiveSceneIndex = SceneNameToIndex[sceneName];
                IsSpawnedObjectsPendingInDontDestroyOnLoad = true;
                s_NextSceneName = sceneName;
            }

            s_IsSwitching = true;


            switchSceneProgress.OnClientLoadedScene += clientId => { OnNotifyServerClientLoadedScene?.Invoke(switchSceneProgress, clientId); };
            switchSceneProgress.OnComplete += timedOut =>
            {
                OnNotifyServerAllClientsLoadedScene?.Invoke(switchSceneProgress, timedOut);

                using (var buffer = PooledNetworkBuffer.Get())
                using (var writer = PooledNetworkWriter.Get(buffer))
                {
                    var doneClientIds = switchSceneProgress.DoneClients.ToArray();
                    var timedOutClientIds = m_NetworkManager.ConnectedClients.Keys.Except(doneClientIds).ToArray();

                    writer.WriteULongArray(doneClientIds, doneClientIds.Length);
                    writer.WriteULongArray(timedOutClientIds, timedOutClientIds.Length);

                    // NSS TODO: This will need to be modified for loading and unloading
                    m_NetworkManager.MessageSender.Send(NetworkManager.Singleton.ServerClientId, NetworkConstants.ALL_CLIENTS_LOADED_SCENE, NetworkChannel.Internal, buffer);
                }
            };

            return switchSceneProgress;
        }

        public SceneSwitchProgress UnloadScene(string sceneName)
        {
            // Make sure the scene is actually loaded
            var sceneToUnload = SceneManager.GetSceneByName(sceneName);
            if (sceneToUnload == null)
            {
                Debug.LogWarning($"{nameof(UnloadScene)} was called, but the scene {sceneName} is not currently loaded!");
                return null;
            }

            var switchSceneProgress = ValidateServerSceneEvent(sceneName, true);
            if (switchSceneProgress == null)
            {
                return null;
            }

            SceneEventData.SwitchSceneGuid = switchSceneProgress.Guid;
            SceneEventData.SceneEventType = SceneEventData.SceneEventTypes.UNLOAD;
            SceneEventData.SceneIndex = SceneNameToIndex[sceneName];

            for (int j = 0; j < m_NetworkManager.ConnectedClientsList.Count; j++)
            {
                using (var buffer = PooledNetworkBuffer.Get())
                {
                    using (var writer = PooledNetworkWriter.Get(buffer))
                    {
                        writer.WriteObjectPacked(SceneEventData);

                        m_NetworkManager.MessageSender.Send(m_NetworkManager.ConnectedClientsList[j].ClientId, NetworkConstants.SCENE_EVENT, NetworkChannel.Internal, buffer);
                    }
                }
            }

            // start loading the scene
            AsyncOperation sceneUnload = SceneManager.UnloadSceneAsync(sceneToUnload);
            sceneUnload.completed += (AsyncOperation asyncOp2) => { OnSceneUnloaded(); };
            switchSceneProgress.SetSceneLoadOperation(sceneUnload);
            OnSceneSwitchStarted?.Invoke(sceneUnload);

            //Return our scene progress instance
            return switchSceneProgress;
        }

        private void OnClientUnloadScene()
        {
            if (!SceneIndexToString.TryGetValue(SceneEventData.SceneIndex, out string sceneName) || !RegisteredSceneNames.Contains(sceneName))
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning("Server requested a scene switch to a non-registered scene");
                }

                return;
            }
            s_IsSwitching = true;

            var sceneLoad = SceneManager.UnloadSceneAsync(sceneName);

            sceneLoad.completed += asyncOp2 => OnSceneUnloaded();

            // NSS TODO: Create the unloaded scene notification
            //OnSceneSwitchStarted?.Invoke(sceneLoad);
        }


        private void OnSceneUnloaded()
        {
            using (var buffer = PooledNetworkBuffer.Get())
            using (var writer = PooledNetworkWriter.Get(buffer))
            {
                SceneEventData.SceneEventType = SceneEventData.SceneEventTypes.UNLOAD_COMPLETE;
                writer.WriteObjectPacked(SceneEventData);
                m_NetworkManager.MessageSender.Send(m_NetworkManager.ServerClientId, NetworkConstants.SCENE_EVENT, NetworkChannel.Internal, buffer);
            }

            s_IsSwitching = false;
        }

        /// <summary>
        /// Additively loads the scene
        /// </summary>
        /// <param name="sceneName"></param>
        /// NSS TODO: This could probably stand to have some form of "scene event status" class/structure that will
        /// include any error types/messages and the SceneSwitchProgress class associated with the status (if success)
        /// <returns>SceneSwitchProgress  (if null this call failed)</returns>
        public SceneSwitchProgress LoadScene(string sceneName)
        {
            var switchSceneProgress = ValidateServerSceneEvent(sceneName);
            if (switchSceneProgress == null)
            {
                return null;
            }

            SceneEventData.SwitchSceneGuid = switchSceneProgress.Guid;
            SceneEventData.SceneEventType = SceneEventData.SceneEventTypes.LOAD;
            SceneEventData.SceneIndex = SceneNameToIndex[sceneName];

            // NSS TODO: remove this completely once done with the transition
            SceneEventData.LoadSceneMode = LoadSceneMode.Additive;

            // Begin the scene event
            OnBeginSceneEvent(sceneName, switchSceneProgress, LoadSceneMode.Additive);

            //Return our scene progress instance
            return switchSceneProgress;
        }

        /// <summary>
        /// Switches to a scene with a given name. Can only be called from Server
        /// </summary>
        /// <param name="sceneName">The name of the scene to switch to</param>
        /// <param name="loadSceneMode">The mode to load the scene (Additive vs Single)</param>
        /// NSS TODO: This could probably stand to have some form of "scene event status" class/structure that will
        /// include any error types/messages and the SceneSwitchProgress class associated with the status (if success)
        /// <returns>SceneSwitchProgress</returns>
        public SceneSwitchProgress SwitchScene(string sceneName, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            // NSS TODO: Remove this once the LoadScene method is completed and all areas in the code that use the loadSceneMode parameter are updated
            if (loadSceneMode == LoadSceneMode.Additive)
            {
                return LoadScene(sceneName);
            }

            var switchSceneProgress = ValidateServerSceneEvent(sceneName);
            if (switchSceneProgress == null)
            {
                return null;
            }

            SceneEventData.SwitchSceneGuid = switchSceneProgress.Guid;
            SceneEventData.SceneEventType = SceneEventData.SceneEventTypes.SWITCH;
            SceneEventData.SceneIndex = SceneNameToIndex[sceneName];

            // NSS TODO: remove this completely once done with the transition?
            SceneEventData.LoadSceneMode = LoadSceneMode.Single;

            // Destroy current scene objects before switching.
            m_NetworkManager.SpawnManager.ServerDestroySpawnedSceneObjects();

            // Preserve the objects that should not be destroyed during the scene event
            MoveObjectsToDontDestroyOnLoad();

            // Begin the scene event
            OnBeginSceneEvent(sceneName, switchSceneProgress, LoadSceneMode.Single);

            //Return our scene progress instance
            return switchSceneProgress;
        }

        /// <summary>
        /// Commonly shared code between switching and additively loading a scene
        /// </summary>
        /// <param name="sceneName">name of the scene to be loaded</param>
        /// <param name="switchSceneProgress">SceneSwitchProgress class instance</param>
        /// <param name="loadSceneMode">how the scene will be loaded</param>
        private void OnBeginSceneEvent(string sceneName, SceneSwitchProgress switchSceneProgress, LoadSceneMode loadSceneMode)
        {
            // start loading the scene
            AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
            sceneLoad.completed += (AsyncOperation asyncOp2) => { OnSceneLoaded(); };
            switchSceneProgress.SetSceneLoadOperation(sceneLoad);
            // NSS TODO: Make a single unified notification callback
            OnSceneSwitchStarted?.Invoke(sceneLoad);
        }

        /// <summary>
        /// Client Side: handles both forms of scene loading
        /// </summary>
        /// <param name="objectStream">Stream data associated with the event </param>
        internal void OnClientSceneLoadingEvent(Stream objectStream)
        {
            SceneEventData.CopyUnreadFromStream(objectStream);

            if (!SceneIndexToString.TryGetValue(SceneEventData.SceneIndex, out string sceneName) || !RegisteredSceneNames.Contains(sceneName))
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning("Server requested a scene switch to a non-registered scene");
                }

                return;
            }

            if (SceneEventData.LoadSceneMode == LoadSceneMode.Single)
            {
                // Move ALL NetworkObjects to the temp scene
                MoveObjectsToDontDestroyOnLoad();
            }

            IsSpawnedObjectsPendingInDontDestroyOnLoad = true;

            var sceneLoad = SceneManager.LoadSceneAsync(sceneName, SceneEventData.LoadSceneMode);

            s_NextSceneName = sceneName;

            sceneLoad.completed += asyncOp2 => OnSceneLoaded();
            OnSceneSwitchStarted?.Invoke(sceneLoad);
        }

        /// <summary>
        /// Client approval specific
        /// </summary>
        /// <param name="sceneIndex"></param>
        /// <param name="switchSceneGuid"></param>
        internal void OnFirstSceneSwitchSync(uint sceneIndex, Guid switchSceneGuid)
        {
            if (!SceneIndexToString.TryGetValue(sceneIndex, out string sceneName) || !RegisteredSceneNames.Contains(sceneName))
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning("Server requested a scene switch to a non-registered scene");
                }

                return;
            }

            if (SceneManager.GetActiveScene().name == sceneName)
            {
                return; //This scene is already loaded. This usually happens at first load
            }

            s_NextSceneName = sceneName;
            CurrentActiveSceneIndex = SceneNameToIndex[sceneName];

            IsSpawnedObjectsPendingInDontDestroyOnLoad = true;
            SceneManager.LoadScene(sceneName);

            using (var buffer = PooledNetworkBuffer.Get())
            using (var writer = PooledNetworkWriter.Get(buffer))
            {
                writer.WriteByteArray(switchSceneGuid.ToByteArray());
                m_NetworkManager.MessageSender.Send(m_NetworkManager.ServerClientId, NetworkConstants.CLIENT_SWITCH_SCENE_COMPLETED, NetworkChannel.Internal, buffer);
            }

            s_IsSwitching = false;
        }

        /// <summary>
        /// Client and Server: Generic on scene loaded callback method to be called upon a scene loading
        /// </summary>
        private void OnSceneLoaded()
        {
            var nextScene = SceneManager.GetSceneByName(s_NextSceneName);
            if (SceneEventData.LoadSceneMode == LoadSceneMode.Single)
            {
                SceneManager.SetActiveScene(nextScene);
            }

            //Get all NetworkObjects loaded by the scene
            PopulateScenePlacedObjects(nextScene);

            if (SceneEventData.LoadSceneMode == LoadSceneMode.Single)
            {
                // Move all objects to the new scene
                MoveObjectsToScene(nextScene);
            }

            // NSS TODO: I think this can be determined differently and removed completely
            IsSpawnedObjectsPendingInDontDestroyOnLoad = false;

            // NSS TODO: We might want to set this sooner, what happens if there is a connection during asynchronous scene loading?
            CurrentSceneIndex = CurrentActiveSceneIndex;

            if (m_NetworkManager.IsServer)
            {
                OnServerLoadedScene();
            }
            else
            {
                OnClientLoadedScene();
            }
        }


        /// <summary>
        /// Server specific on scene loaded callback method invoked by OnSceneLoading only
        /// </summary>
        private void OnServerLoadedScene()
        {
            // Register in-scene placed NetworkObjects with MLAPI
            foreach (var keyValuePair in ScenePlacedObjects)
            {
                if (!keyValuePair.Value.IsPlayerObject)
                {
                    m_NetworkManager.SpawnManager.SpawnNetworkObjectLocally(keyValuePair.Value, m_NetworkManager.SpawnManager.GetNetworkObjectId(), true, false, null, null, false, 0, false, true);
                }
            }

            for (int j = 0; j < m_NetworkManager.ConnectedClientsList.Count; j++)
            {
                if (m_NetworkManager.ConnectedClientsList[j].ClientId != m_NetworkManager.ServerClientId)
                {
                    using (var buffer = PooledNetworkBuffer.Get())
                    {
                        using (var writer = PooledNetworkWriter.Get(buffer))
                        {
                            SynchronizeInSceneObjects(m_NetworkManager.ConnectedClientsList[j].ClientId, writer);

                            m_NetworkManager.MessageSender.Send(m_NetworkManager.ConnectedClientsList[j].ClientId, NetworkConstants.SCENE_EVENT, NetworkChannel.Internal, buffer);
                        }
                    }
                }
            }

            // Tell server that scene load is completed
            if (m_NetworkManager.IsHost)
            {
                OnClientSceneLoadingEventCompleted(m_NetworkManager.LocalClientId, SceneEventData.SwitchSceneGuid);
            }

            s_IsSwitching = false;

            OnSceneSwitched?.Invoke();
        }

        /// <summary>
        /// NSS TODO: This might go back into the above method
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="writer"></param>
        internal void SynchronizeInSceneObjects(ulong clientId, NetworkWriter writer)
        {
            writer.WriteObjectPacked(SceneEventData);

            uint sceneObjectsToSpawn = 0;

            foreach (var keyValuePair in ScenePlacedObjects)
            {
                if (keyValuePair.Value.Observers.Contains(clientId))
                {
                    sceneObjectsToSpawn++;
                }
            }

            // Write number of scene objects to spawn
            writer.WriteUInt32Packed(sceneObjectsToSpawn);
            foreach (var keyValuePair in ScenePlacedObjects)
            {
                if (keyValuePair.Value.Observers.Contains(clientId))
                {
                    keyValuePair.Value.SerializeSceneObject(writer, clientId);
                }
            }
        }


        /// <summary>
        /// Client specific on scene loaded callback method invoked by OnSceneLoading only
        /// </summary>
        private void OnClientLoadedScene()
        {
            using (var reader = PooledNetworkReader.Get(SceneEventData.InternalBuffer))
            {
                var newObjectsCount = reader.ReadUInt32Packed();

                for (int i = 0; i < newObjectsCount; i++)
                {
                    NetworkObject.DeserializeSceneObject(SceneEventData.InternalBuffer as NetworkBuffer, reader, m_NetworkManager);
                }
            }

            using (var buffer = PooledNetworkBuffer.Get())
            using (var writer = PooledNetworkWriter.Get(buffer))
            {
                SceneEventData.SceneEventType = SceneEventData.SceneEventType == SceneEventData.SceneEventTypes.SWITCH ? SceneEventData.SceneEventTypes.SWITCH_COMPLETE : SceneEventData.SceneEventTypes.LOAD_COMPLETE;
                writer.WriteObjectPacked(SceneEventData);
                m_NetworkManager.MessageSender.Send(m_NetworkManager.ServerClientId, NetworkConstants.SCENE_EVENT, NetworkChannel.Internal, buffer);
            }

            s_IsSwitching = false;

            OnSceneSwitched?.Invoke();
        }





        private readonly List<NetworkObject> m_ObservedObjects = new List<NetworkObject>();
        internal void SynchronizeNetworkObjects(ulong ownerClientId)
        {
            m_ObservedObjects.Clear();

            foreach (var sobj in m_NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (sobj.CheckObjectVisibility == null || sobj.CheckObjectVisibility(ownerClientId))
                {
                    m_ObservedObjects.Add(sobj);
                    sobj.Observers.Add(ownerClientId);
                }
            }

            ClientSynchEventData.InitializeForSynch();
            ClientSynchEventData.LoadSceneMode = LoadSceneMode.Single;
            var activeScene = SceneManager.GetActiveScene();
            ClientSynchEventData.SceneEventType = SceneEventData.SceneEventTypes.SYNC;
            //var networkObjectsFound = UnityEngine.Object.FindObjectsOfType<NetworkObject>();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                uint malpiSceneIndex = GetMLAPISceneIndex(scene);

                // This would depend upon whether we are additive or note
                if (activeScene == scene)
                {
                    ClientSynchEventData.SceneIndex = malpiSceneIndex;
                }
                else
                {
                    if (!ClientSynchEventData.AdditiveScenes.Contains(malpiSceneIndex))
                    {
                        ClientSynchEventData.AdditiveScenes.Add(malpiSceneIndex);
                    }
                }

                foreach (var networkObject in m_ObservedObjects)
                {
                    if (networkObject.gameObject.scene != scene)
                    {
                        continue;
                    }
                    ClientSynchEventData.AddNetworkObjectForSynch(malpiSceneIndex, networkObject);
                }
            }

            // Send the scene event
            using (var buffer = PooledNetworkBuffer.Get())
            using (var writer = PooledNetworkWriter.Get(buffer))
            {
                writer.WriteObjectPacked(ClientSynchEventData);
                m_NetworkManager.MessageSender.Send(ownerClientId, NetworkConstants.SCENE_EVENT, NetworkChannel.Internal, buffer);
            }

        }

        private void OnClientBeginSynch(uint sceneIndex)
        {

            if (!SceneIndexToString.TryGetValue(sceneIndex, out string sceneName) || !RegisteredSceneNames.Contains(sceneName))
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning("Server requested a scene switch to a non-registered scene");
                }

                return;
            }


            var activeScene = SceneManager.GetActiveScene();

            IsSpawnedObjectsPendingInDontDestroyOnLoad = true;

            if (sceneName != activeScene.name)
            {
                //if (SceneEventData.LoadSceneMode == LoadSceneMode.Single)
                //{
                //    // Move ALL NetworkObjects to the temp scene
                //    MoveObjectsToDontDestroyOnLoad();
                //}
                var sceneLoad = SceneManager.LoadSceneAsync(sceneName, sceneIndex == SceneEventData.SceneIndex ? SceneEventData.LoadSceneMode:LoadSceneMode.Additive);
                s_NextSceneName = sceneName;
                sceneLoad.completed += asyncOp2 => ClientLoadedSynchronization(sceneIndex);
            }
            else
            {
                ClientLoadedSynchronization(sceneIndex);
            }
        }


        private void ClientLoadedSynchronization(uint sceneIndex)
        {
            var nextScene = SceneManager.GetSceneByName(GetMLAPISceneNameFromIndex(sceneIndex));
            if(nextScene == null)
            {
                return;
            }

            if ((sceneIndex == SceneEventData.SceneIndex ? SceneEventData.LoadSceneMode : LoadSceneMode.Additive) == LoadSceneMode.Single)
            {
                SceneManager.SetActiveScene(nextScene);
            }

            //Get all NetworkObjects loaded by the scene
            PopulateScenePlacedObjects(nextScene);

            SceneEventData.SynchronizeSceneNetworkObjects(sceneIndex, m_NetworkManager);

            if(!SceneEventData.IsDoneWithSynchronization())
            {
                HandleClientSceneEvent(null);
            }
            else
            {
                m_NetworkManager.IsConnectedClient = true;
                m_NetworkManager.InvokeOnClientConnectedCallback(m_NetworkManager.LocalClientId);
                using (var buffer = PooledNetworkBuffer.Get())
                using (var writer = PooledNetworkWriter.Get(buffer))
                {
                    SceneEventData.SceneEventType = SceneEventData.SceneEventTypes.SYNC_COMPLETE;
                    writer.WriteObjectPacked(SceneEventData);
                    m_NetworkManager.MessageSender.Send(m_NetworkManager.ServerClientId, NetworkConstants.SCENE_EVENT, NetworkChannel.Internal, buffer);
                }
            }
        }

        internal bool HasSceneMismatch(uint sceneIndex) => SceneManager.GetActiveScene().name != SceneIndexToString[sceneIndex];

        // Called on server
        internal void OnClientSceneLoadingEventCompleted(ulong clientId, Guid switchSceneGuid)
        {
            if (switchSceneGuid == Guid.Empty)
            {
                // If Guid is empty it means the client has loaded the start scene of the server and the server would never have a switchSceneProgresses created for the start scene.
                return;
            }

            if (SceneSwitchProgresses.TryGetValue(switchSceneGuid, out SceneSwitchProgress progress))
            {
                SceneSwitchProgresses[switchSceneGuid].AddClientAsDone(clientId);
            }
        }

        internal void RemoveClientFromSceneSwitchProgresses(ulong clientId)
        {
            foreach (var switchSceneProgress in SceneSwitchProgresses.Values)
            {
                switchSceneProgress.RemoveClientAsDone(clientId);
            }
        }

        private void MoveObjectsToDontDestroyOnLoad()
        {
            // Move ALL NetworkObjects to the temp scene
            var objectsToKeep = m_NetworkManager.SpawnManager.SpawnedObjectsList;

            foreach (var sobj in objectsToKeep)
            {
                //In case an object has been set as a child of another object it has to be removed from the parent in order to be moved from one scene to another.
                if (sobj.gameObject.transform.parent != null)
                {
                    sobj.gameObject.transform.parent = null;
                }

                UnityEngine.Object.DontDestroyOnLoad(sobj.gameObject);
            }
        }

        /// <summary>
        /// Should be invoked on both the client and server side after:
        /// -- A new scene has been loaded
        /// -- Before any "DontDestroyOnLoad" NetworkObjects have been added back into the scene.
        /// Added the ability to choose not to clear the scene placed objects for additive scene loading.
        /// </summary>
        internal void PopulateScenePlacedObjects(Scene sceneToFilterBy, bool clearScenePlacedObjects = true)
        {
            if (clearScenePlacedObjects)
            {
                ScenePlacedObjects.Clear();
            }

            var networkObjects = UnityEngine.Object.FindObjectsOfType<NetworkObject>();

            // Just add every NetworkObject found that isn't already in the list
            // If any "non-in-scene placed NetworkObjects" are added to this list it shouldn't matter
            // The only thing that matters is making sure each NetworkObject is keyed off of their GlobalObjectIdHash
            foreach (var networkObjectInstance in networkObjects)
            {
                if (!ScenePlacedObjects.ContainsKey(networkObjectInstance.GlobalObjectIdHash))
                {
                    // We check to make sure the NetworkManager instance is the same one to be "MultiInstanceHelpers" compatible and filter the list on a per scene basis (additive scenes)
                    if (networkObjectInstance.IsSceneObject == null && networkObjectInstance.NetworkManager == m_NetworkManager && networkObjectInstance.gameObject.scene == sceneToFilterBy)
                    {
                        ScenePlacedObjects.Add(networkObjectInstance.GlobalObjectIdHash, networkObjectInstance);
                    }
                }
            }
        }

        private void MoveObjectsToScene(Scene scene)
        {
            // Move ALL NetworkObjects to the temp scene
            var objectsToKeep = m_NetworkManager.SpawnManager.SpawnedObjectsList;

            foreach (var sobj in objectsToKeep)
            {
                //In case an object has been set as a child of another object it has to be removed from the parent in order to be moved from one scene to another.
                if (sobj.gameObject.transform.parent != null)
                {
                    sobj.gameObject.transform.parent = null;
                }

                SceneManager.MoveGameObjectToScene(sobj.gameObject, scene);
            }
        }

        internal void AllClientsReady(ulong[] clientIds, ulong[] timedOutClientIds)
        {
            OnNotifyClientAllClientsLoadedScene?.Invoke(clientIds, timedOutClientIds);
        }


        /// <summary>
        /// Client Side: Handles incoming SCENE_EVENT messages
        /// </summary>
        /// <param name="stream">data associated with the event</param>
        private void HandleClientSceneEvent(Stream stream)
        {
            switch (SceneEventData.SceneEventType)
            {
                // Both events are basically the same with some minor differences
                case SceneEventData.SceneEventTypes.SWITCH:
                case SceneEventData.SceneEventTypes.LOAD:
                    {
                        OnClientSceneLoadingEvent(stream);
                        break;
                    }
                case SceneEventData.SceneEventTypes.UNLOAD:
                    {
                        OnClientUnloadScene();
                        break;
                    }
                case SceneEventData.SceneEventTypes.SYNC:
                    {
                        OnClientBeginSynch(SceneEventData.GetNextSceneSynchronizationIndex());
                        break;
                    }
                default:
                    {
                        Debug.LogWarning($"{SceneEventData.SceneEventType} is not currently supported!");
                        break;
                    }
            }
        }

        /// <summary>
        /// Server Side: Handles incoming scene events
        /// </summary>
        /// <param name="clientId">client who sent the event</param>
        /// <param name="stream">data associated with the event</param>
        private void HandleServerSceneEvent(ulong clientId, Stream stream)
        {
            switch (SceneEventData.SceneEventType)
            {
                case SceneEventData.SceneEventTypes.SWITCH_COMPLETE:
                    {
                        OnClientSceneLoadingEventCompleted(clientId, SceneEventData.SwitchSceneGuid);
                        break;
                    }
                case SceneEventData.SceneEventTypes.LOAD_COMPLETE:
                    {
                        Debug.Log($"[{nameof(SceneEventData.SceneEventTypes.LOAD_COMPLETE)}] Client Id {clientId} finished loading additive scene.");
                        break;
                    }
                case SceneEventData.SceneEventTypes.UNLOAD_COMPLETE:
                    {
                        Debug.Log($"[{nameof(SceneEventData.SceneEventTypes.UNLOAD_COMPLETE)}] Client Id {clientId} finished unloading additive scene.");
                        break;
                    }
                case SceneEventData.SceneEventTypes.SYNC_COMPLETE:
                    {
                        //using (var buffer = PooledNetworkBuffer.Get())
                        //using (var writer = PooledNetworkWriter.Get(buffer))
                        //{
                        //    writer.WriteUInt64Packed(ownerClientId);

                        //    if (NetworkConfig.EnableSceneManagement)
                        //    {
                        //        writer.WriteUInt32Packed(NetworkSceneManager.CurrentSceneIndex);
                        //        writer.WriteByteArray(NetworkSceneManager.CurrentSceneSwitchProgressGuid.ToByteArray());
                        //    }

                        //    writer.WriteSinglePacked(Time.realtimeSinceStartup);
                        //    writer.WriteUInt32Packed((uint)m_ObservedObjects.Count);

                        //    for (int i = 0; i < m_ObservedObjects.Count; i++)
                        //    {
                        //        m_ObservedObjects[i].SerializeSceneObject(writer, ownerClientId);
                        //    }

                        //    MessageSender.Send(ownerClientId, NetworkConstants.CONNECTION_APPROVED, NetworkChannel.Internal, buffer);
                        //}

                        m_NetworkManager.NotifyPlayerConnected(clientId, m_NetworkManager.NetworkConfig.PlayerPrefab.GetComponent<NetworkObject>().GlobalObjectIdHash);
                        break;
                    }
                default:
                    {
                        Debug.LogWarning($"{SceneEventData.SceneEventType} is not currently supported!");
                        break;
                    }
            }
        }

        /// <summary>
        /// Both Client and Server: Incoming scene event entry point
        /// </summary>
        /// <param name="clientId">client who sent the scene event</param>
        /// <param name="stream">data associated with the scene event</param>
        public void HandleSceneEvent(ulong clientId, Stream stream)
        {
            if (m_NetworkManager != null)
            {
                if (stream != null)
                {
                    using (var reader = NetworkReaderPool.GetReader(stream))
                    {
                        SceneEventData = (SceneEventData)reader.ReadObjectPacked(typeof(SceneEventData));
                    }

                    if (SceneEventData.IsSceneEventClientSide())
                    {
                        HandleClientSceneEvent(stream);
                    }
                    else
                    {
                        HandleServerSceneEvent(clientId, stream);
                    }
                }
                else
                {
                    Debug.LogError($"Scene Event {nameof(OnClientSceneLoadingEvent)} was invoked with a null stream!");
                    return;
                }
            }
            else
            {
                Debug.LogError($"{nameof(NetworkSceneManager.HandleSceneEvent)} was invoked but {nameof(NetworkManager)} reference was null!");
            }
        }
    }

    [Serializable]
    public class SceneEventData : INetworkSerializable, IDisposable
    {
        public enum SceneEventTypes
        {
            SWITCH,             //Server to client full scene switch (i.e. single mode and destroy everything)
            LOAD,               //Server to client load additive scene
            UNLOAD,             //Server to client unload additive scene
            SYNC,               //Server to client late join approval synchronization
            SWITCH_COMPLETE,    //Client to server
            LOAD_COMPLETE,      //Client to server
            UNLOAD_COMPLETE,    //Client to server
            SYNC_COMPLETE,      //Client to server
        }

        public SceneEventTypes SceneEventType;
        public LoadSceneMode LoadSceneMode;
        public Guid SwitchSceneGuid;

        public List<uint> AdditiveScenes;

        public uint SceneIndex;
        public ulong TargetClientId;

        private Dictionary<uint, List<NetworkObject>> m_SceneNetworkObjects;
        private Dictionary<uint, long> m_SceneNetworkObjectDataOffsets;
        internal PooledNetworkBuffer InternalBuffer;

        public void InitializeForSynch()
        {
            if(m_SceneNetworkObjects == null)
            {
                m_SceneNetworkObjects = new Dictionary<uint, List<NetworkObject>>();
            }
            else
            {
                m_SceneNetworkObjects.Clear();
            }
        }

        public uint GetNextSceneSynchronizationIndex()
        {
            if(m_SceneNetworkObjectDataOffsets.ContainsKey(SceneIndex))
            {
                return SceneIndex;
            }
            return m_SceneNetworkObjectDataOffsets.First().Key;
        }

        public bool IsDoneWithSynchronization()
        {
            return (m_SceneNetworkObjectDataOffsets.Count == 0);
        }

        public void AddNetworkObjectForSynch(uint sceneIndex, NetworkObject networkObject)
        {
            if(!m_SceneNetworkObjects.ContainsKey(sceneIndex))
            {
                m_SceneNetworkObjects.Add(sceneIndex, new List<NetworkObject>());
            }

            m_SceneNetworkObjects[sceneIndex].Add(networkObject);
        }

        /// <summary>
        /// Determines if the scene event type was intended for the client ( or server )
        /// </summary>
        /// <returns>true (client should handle this message) false (server should handle this message)</returns>
        public bool IsSceneEventClientSide()
        {
            switch (SceneEventType)
            {
                case SceneEventTypes.LOAD:
                case SceneEventTypes.SWITCH:
                case SceneEventTypes.UNLOAD:
                case SceneEventTypes.SYNC:
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Serialize this class instance
        /// </summary>
        /// <param name="writer"></param>
        private void OnWrite(NetworkWriter writer)
        {
            writer.WriteByte((byte)SceneEventType);


            writer.WriteByte((byte)LoadSceneMode);
            if (SceneEventType != SceneEventTypes.SYNC)
            {
                writer.WriteByteArray(SwitchSceneGuid.ToByteArray());
            }

            writer.WriteUInt32Packed(SceneIndex);

            if (SceneEventType == SceneEventTypes.SYNC)
            {
                writer.WriteArrayPacked(AdditiveScenes.ToArray());
                Debug.Log($"Wrote:{AdditiveScenes.Count} additive scenes to be loaded.");
                writer.WriteInt32Packed(m_SceneNetworkObjects.Count());

                if (m_SceneNetworkObjects.Count() > 0)
                {
                    string msg = "Scene Associated NetworkObjects Write:\n";
                    foreach (var keypair in m_SceneNetworkObjects)
                    {
                        writer.WriteUInt32Packed(keypair.Key);
                        msg += $"Scene ID [{keypair.Key}] NumNetworkObjects:[{keypair.Value.Count}]\n";
                        writer.WriteUInt32Packed((uint)keypair.Value.Count);
                        var positionStart = writer.GetStream().Position;
                        // Size Place Holder
                        writer.WriteUInt64(0);
                        foreach (var networkObject in keypair.Value)
                        {
                            networkObject.SerializeSceneObject(writer, TargetClientId);
                        }
                        var positionEnd = writer.GetStream().Position;
                        var bytesWritten = (ulong)(positionEnd - positionStart);
                        writer.GetStream().Position = positionStart;
                        // Write the total size written to the stream by NetworkObjects being serialized
                        writer.WriteUInt64(bytesWritten);
                        writer.GetStream().Position = positionEnd;
                        msg += $"Wrote [{bytesWritten}] bytes of NetworkObject data.\n";
                    }

                    Debug.Log(msg);
                }
            }
        }

        /// <summary>
        /// Deserialize this class instance
        /// </summary>
        /// <param name="reader"></param>
        private void OnRead(NetworkReader reader)
        {
            var sceneEventTypeValue = reader.ReadByte();

            if (Enum.IsDefined(typeof(SceneEventTypes), sceneEventTypeValue))
            {
                SceneEventType = (SceneEventTypes)sceneEventTypeValue;
            }
            else
            {
                Debug.LogError($"Serialization Read Error: {nameof(SceneEventType)} vale {sceneEventTypeValue} is not within the range of the defined {nameof(SceneEventTypes)} enumerator!");
            }

            var loadSceneModeValue = reader.ReadByte();

            if (Enum.IsDefined(typeof(LoadSceneMode), loadSceneModeValue))
            {
                LoadSceneMode = (LoadSceneMode)loadSceneModeValue;
            }
            else
            {
                Debug.LogError($"Serialization Read Error: {nameof(LoadSceneMode)} vale {loadSceneModeValue} is not within the range of the defined {nameof(LoadSceneMode)} enumerator!");
            }

            if (SceneEventType != SceneEventTypes.SYNC)
            {
                SwitchSceneGuid = new Guid(reader.ReadByteArray());
            }

            SceneIndex = reader.ReadUInt32Packed();

            if (SceneEventType == SceneEventTypes.SYNC)
            {
                var array = reader.ReadUIntArrayPacked();
                AdditiveScenes = new List<uint>(array);
                var keyPairCount = reader.ReadInt32Packed();

                if (keyPairCount > 0)
                {
                    if (m_SceneNetworkObjectDataOffsets == null)
                    {
                        m_SceneNetworkObjectDataOffsets = new Dictionary<uint, long>();
                    }
                    else
                    {
                        m_SceneNetworkObjectDataOffsets.Clear();
                    }

                    InternalBuffer.Position = 0;

                    using (var writer = PooledNetworkWriter.Get(InternalBuffer))
                    {
                        for (int i = 0; i < keyPairCount; i++)
                        {
                            var key = reader.ReadUInt32Packed();
                            var count = reader.ReadUInt32Packed();
                            var bytesToRead = reader.ReadUInt64Packed();  // So we know how much to read
                            // We store off the current position of the stream as it pertains to the scene relative NetworkObjects
                            m_SceneNetworkObjectDataOffsets.Add(key, InternalBuffer.Position);
                            writer.WriteUInt32Packed(count);
                            var networkObjectsBuffer = reader.ReadByteArray(null, (long)bytesToRead);
                            writer.WriteByteArray(networkObjectsBuffer);
                        }
                    }
                }
            }
        }

        public void SynchronizeSceneNetworkObjects(uint sceneId, NetworkManager networkManager)
        {
            if (m_SceneNetworkObjectDataOffsets.ContainsKey(sceneId))
            {
                // Point to the appropriate offset
                InternalBuffer.Position = m_SceneNetworkObjectDataOffsets[sceneId];

                using (var reader = PooledNetworkReader.Get(InternalBuffer))
                {
                    // Process all NetworkObjects for this scene
                    var newObjectsCount = reader.ReadUInt32Packed();

                    for (int i = 0; i < newObjectsCount; i++)
                    {
                        NetworkObject.DeserializeSceneObject(InternalBuffer as NetworkBuffer, reader, networkManager);
                    }
                }

                // Remove this entry
                m_SceneNetworkObjectDataOffsets.Remove(sceneId);
            }
        }

        /// <summary>
        /// INetworkSerializable implementation method
        /// </summary>
        /// <param name="serializer">serializer passed in during serialization</param>
        public void NetworkSerialize(NetworkSerializer serializer)
        {
            if (serializer.IsReading)
            {
                OnRead(serializer.Reader);
            }
            else
            {
                OnWrite(serializer.Writer);
            }
        }

        /// <summary>
        /// Used to store data during an asynchronous scene loading event
        /// </summary>
        /// <param name="stream"></param>
        internal void CopyUnreadFromStream(Stream stream)
        {
            InternalBuffer.Position = 0;
            InternalBuffer.CopyUnreadFrom(stream);
            InternalBuffer.Position = 0;
        }

        /// <summary>
        /// Used to release the pooled network buffer
        /// </summary>
        public void Dispose()
        {
            if (InternalBuffer != null)
            {
                NetworkBufferPool.PutBackInPool(InternalBuffer);
                InternalBuffer = null;
            }
        }

        public SceneEventData()
        {
            InternalBuffer = NetworkBufferPool.GetBuffer();
            AdditiveScenes = new List<uint>();

        }
    }
}
