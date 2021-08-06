using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Netcode
{
    /// <summary>
    /// Used for local notifications of various scene events.
    /// The <see cref="NetworkSceneManager.OnSceneEvent"/> of delegate type <see cref="NetworkSceneManager.SceneEventDelegate"/> uses this class to provide
    /// scene event status/state.
    /// </summary>
    public class SceneEvent
    {
        /// <summary>
        /// If applicable, this will be set to the <see cref="UnityEngine.AsyncOperation"/> returned by <see cref="SceneManager"/>
        /// load scene and unload scene asynchronous methods.
        /// </summary>
        public AsyncOperation AsyncOperation;

        /// <summary>
        /// Will always be set to the current scene event type (<see cref="SceneEventData.SceneEventTypes"/>) this scene event notification pertains to
        /// </summary>
        public SceneEventData.SceneEventTypes SceneEventType;

        /// <summary>
        /// If applicable, this reflects the type of scene loading or unloading that is occurring.
        /// Unlike <see cref="SceneManager"/>, scene unload events will have the original <see cref="LoadSceneMode"/> applied when the scene was loaded.
        /// </summary>
        public LoadSceneMode LoadSceneMode;

        /// <summary>
        /// Excluding <see cref="SceneEventData.SceneEventTypes.S2C_Event_Sync"/> and <see cref="SceneEventData.SceneEventTypes.C2S_Event_Sync_Complete"/>
        /// This will be set to the scene name that the event pertains to.
        /// </summary>
        public string SceneName;

        /// <summary>
        /// Events that always set <see cref="ClientId"/> to the local client identifier
        /// and only triggered locally:
        /// <see cref="SceneEventData.SceneEventTypes.S2C_Load"/>
        /// <see cref="SceneEventData.SceneEventTypes.S2C_Unload"/>
        /// <see cref="SceneEventData.SceneEventTypes.S2C_Sync"/>
        /// <see cref="SceneEventData.SceneEventTypes.S2C_ReSync"/>
        ///
        /// Events that always set <see cref="ClientId"/> to the local client identifier,
        /// are triggered locally, and a host or server will trigger externally generated
        /// scene event message types (i.e. sent by a client):
        /// <see cref="SceneEventData.SceneEventTypes.C2S_UnloadComplete"/>
        /// <see cref="SceneEventData.SceneEventTypes.C2S_LoadComplete"/>
        /// <see cref="SceneEventData.SceneEventTypes.C2S_SyncComplete"/>
        ///
        /// Events that always set <see cref="ClientId"/> to the ServerId:
        /// <see cref="SceneEventData.SceneEventTypes.S2C_LoadComplete"/>
        /// <see cref="SceneEventData.SceneEventTypes.S2C_UnLoadComplete"/>
        /// </summary>
        public ulong ClientId;

        /// <summary>
        /// List of clients that completed a loading or unloading event
        /// Applies only to:
        /// <see cref="SceneEventData.SceneEventTypes.S2C_LoadComplete"/>
        /// <see cref="SceneEventData.SceneEventTypes.S2C_UnLoadComplete"/>
        /// </summary>
        public List<ulong> ClientsThatCompleted;

        /// <summary>
        /// List of clients that timed out during a loading or unloading event
        /// Applies only to:
        /// <see cref="SceneEventData.SceneEventTypes.S2C_LoadComplete"/>
        /// <see cref="SceneEventData.SceneEventTypes.S2C_UnLoadComplete"/>
        /// </summary>
        public List<ulong> ClientsThatTimedOut;
    }

    /// <summary>
    /// Main class for managing network scenes when <see cref="NetworkConfig.EnableSceneManagement"/> is enabled.
    /// Uses the <see cref="MessageQueueContainer.MessageType.SceneEvent"/> message to communicate <see cref="SceneEventData"/> between the server and client(s)
    /// </summary>
    public class NetworkSceneManager
    {
        internal static bool DisableReSynchronization;
        internal static bool IsUnitTesting;

        /// <summary>
        /// The delegate callback definition for scene event notifications
        /// For more details review over <see cref="SceneEvent"/> and <see cref="SceneEventData"/>
        /// </summary>
        /// <param name="sceneEvent"></param>
        public delegate void SceneEventDelegate(SceneEvent sceneEvent);

        /// <summary>
        /// Event that will notify the local client or server of all scene events that take place
        /// For more details review over <see cref="SceneEvent"/>, <see cref="SceneEventData"/>, and <see cref="SceneEventData.SceneEventTypes"/>
        /// </summary>
        public event SceneEventDelegate OnSceneEvent;

        internal readonly HashSet<string> RegisteredSceneNames = new HashSet<string>();
        internal readonly Dictionary<string, uint> SceneNameToIndex = new Dictionary<string, uint>();
        internal readonly Dictionary<uint, string> SceneIndexToString = new Dictionary<uint, string>();
        internal readonly Dictionary<Guid, SceneEventProgress> SceneEventProgressTracking = new Dictionary<Guid, SceneEventProgress>();
        internal readonly Dictionary<uint, NetworkObject> ScenePlacedObjects = new Dictionary<uint, NetworkObject>();

        // Used for observed object synchronization
        private readonly List<NetworkObject> m_ObservedObjects = new List<NetworkObject>();

        // Used to track which scenes are currently loaded (outside of SceneManager)
        private List<string> m_ScenesLoaded = new List<string>();

        // Used to detect if we are in the middle of a single mode scene transition
        private static bool s_IsSceneEventActive = false;

        // The Condition: While a scene is asynchronously loaded in single loading scene mode, if any new NetworkObjects are spawned
        // they need to be moved into the do not destroy temporary scene
        // When it is set: Just before starting the asynchronous loading call
        // When it is unset: After the scene has loaded, the PopulateScenePlacedObjects is called, and all NetworkObjects in the do
        // not destroy temporary scene are moved into the active scene
        internal static bool IsSpawnedObjectsPendingInDontDestroyOnLoad = false;

        //Client and Server: used for all scene event processing except for ClientSynchEventData specific events
        internal SceneEventData SceneEventData;

        //Server Side: Used specifically for scene synchronization and scene event progress related events.
        internal SceneEventData ClientSynchEventData;

        private NetworkManager m_NetworkManager { get; }

        private const MessageQueueContainer.MessageType k_MessageType = MessageQueueContainer.MessageType.SceneEvent;
        private const NetworkChannel k_ChannelType = NetworkChannel.Internal;
        private const NetworkUpdateStage k_NetworkUpdateStage = NetworkUpdateStage.PreUpdate;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="networkManager"></param>
        internal NetworkSceneManager(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            SceneEventData = new SceneEventData(networkManager);
            ClientSynchEventData = new SceneEventData(networkManager);
        }

        /// <summary>
        /// Generic sending of scene event data
        /// </summary>
        /// <param name="targetClientIds">array of client identifiers to receive the scene event message</param>
        internal void SendSceneEventData(ulong[] targetClientIds)
        {
            if (targetClientIds.Length == 0)
            {
                // This would be the server with no clients connected
                // Silently return as there is nothing to be done
                return;
            }

            var context = m_NetworkManager.MessageQueueContainer.EnterInternalCommandContext(k_MessageType, k_ChannelType, targetClientIds, k_NetworkUpdateStage);

            if (context != null)
            {
                using (var nonNullContext = (InternalCommandContext)context)
                {
                    SceneEventData.OnWrite(nonNullContext.NetworkWriter);
                }
                return;
            }

            // This should never happen, but if it does something very bad has happened and we should throw an exception
            throw new Exception($"{nameof(InternalCommandContext)} is null! {nameof(NetworkSceneManager)} failed to send event notification {SceneEventData.SceneEventType} to target clientIds {targetClientIds}!");
        }

        /// <summary>
        /// Returns the Netcode scene index from a scene
        /// </summary>
        /// <param name="scene"></param>
        /// <returns>Netcode Scene Index</returns>
        internal uint GetNetcodeSceneIndexFromScene(Scene scene)
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

        /// <summary>
        /// Returns the scene name from the Netcode scene index
        /// Note: This is not the same as the Build Settings Scenes in Build index
        /// </summary>
        /// <param name="sceneIndex">Netcode Scene Index</param>
        /// <returns>scene name</returns>
        internal string GetSceneNameFromNetcodeSceneIndex(uint sceneIndex)
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
        /// This also initializes some commonly shared values as well as SceneEventProgress
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns><see cref="SceneEventProgress"/> that should have a <see cref="SceneEventProgress.Status"/> of <see cref="SceneEventProgressStatus.Started"/> otherwise it failed.</returns>
        private SceneEventProgress ValidateServerSceneEvent(string sceneName, bool isUnloading = false)
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

            // Return scene event already in progress if one is already in progress... :)
            if (s_IsSceneEventActive)
            {
                return new SceneEventProgress(null, SceneEventProgressStatus.SceneEventInProgress);
            }

            // Return invalid scene name status if the scene name is invalid... :)
            if (!RegisteredSceneNames.Contains(sceneName))
            {
                return new SceneEventProgress(null, SceneEventProgressStatus.InvalidSceneName);
            }

            var sceneEventProgress = new SceneEventProgress(m_NetworkManager);
            sceneEventProgress.SceneName = sceneName;
            SceneEventProgressTracking.Add(sceneEventProgress.Guid, sceneEventProgress);

            if (!isUnloading)
            {
                // The Condition: While a scene is asynchronously loaded in single loading scene mode, if any new NetworkObjects are spawned
                // they need to be moved into the do not destroy temporary scene
                // When it is set: Just before starting the asynchronous loading call
                // When it is unset: After the scene has loaded, the PopulateScenePlacedObjects is called, and all NetworkObjects in the do
                // not destroy temporary scene are moved into the active scene
                IsSpawnedObjectsPendingInDontDestroyOnLoad = true;
            }

            s_IsSceneEventActive = true;

            // Set our callback delegate handler for completion
            sceneEventProgress.OnComplete = OnSceneEventProgressCompleted;

            return sceneEventProgress;
        }

        /// <summary>
        /// Callback for the <see cref="SceneEventProgress.OnComplete"/> <see cref="SceneEventProgress.OnCompletedDelegate"/> handler
        /// </summary>
        /// <param name="sceneEventProgress"></param>
        /// <returns></returns>
        internal bool OnSceneEventProgressCompleted(SceneEventProgress sceneEventProgress)
        {
            // Send a message to all clients that all clients are done loading or unloading
            var context = m_NetworkManager.MessageQueueContainer.EnterInternalCommandContext(k_MessageType, k_ChannelType, m_NetworkManager.ConnectedClientsIds, k_NetworkUpdateStage);
            if (context != null)
            {
                using (var nonNullContext = (InternalCommandContext)context)
                {
                    ClientSynchEventData.SceneEventGuid = sceneEventProgress.Guid;
                    ClientSynchEventData.SceneIndex = SceneNameToIndex[sceneEventProgress.SceneName];
                    ClientSynchEventData.SceneEventType = sceneEventProgress.SceneEventType;
                    ClientSynchEventData.ClientsCompleted = sceneEventProgress.DoneClients;
                    ClientSynchEventData.ClientsTimedOut = m_NetworkManager.ConnectedClients.Keys.Except(sceneEventProgress.DoneClients).ToList();
                    ClientSynchEventData.OnWrite(nonNullContext.NetworkWriter);
                }
            }

            // Send a local notification to the server that all clients are done loading or unloading
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                SceneEventType = sceneEventProgress.SceneEventType,
                SceneName = sceneEventProgress.SceneName,
                ClientId = m_NetworkManager.ServerClientId,
                LoadSceneMode = sceneEventProgress.LoadSceneMode,
                ClientsThatCompleted = sceneEventProgress.DoneClients,
                ClientsThatTimedOut = m_NetworkManager.ConnectedClients.Keys.Except(sceneEventProgress.DoneClients).ToList(),
            });

            SceneEventProgressTracking.Remove(sceneEventProgress.Guid);

            return false;
        }

        /// <summary>
        /// Unloads an additively loaded scene
        /// When applicable, the <see cref="AsyncOperation"/> is delivered within the <see cref="SceneEvent"/> via the <see cref="OnSceneEvent"/>
        /// </summary>
        /// <param name="sceneName">scene name to unload</param>
        /// <returns><see cref="SceneEventProgressStatus"/> (<see cref="SceneEventProgressStatus.Started"/> means it was successful)</returns>
        public SceneEventProgressStatus UnloadScene(string sceneName)
        {
            // Make sure the scene is actually loaded
            var sceneToUnload = SceneManager.GetSceneByName(sceneName);
            if (sceneToUnload == null)
            {
                Debug.LogWarning($"{nameof(UnloadScene)} was called, but the scene {sceneName} is not currently loaded!");
                return SceneEventProgressStatus.SceneNotLoaded;
            }

            var sceneEventProgress = ValidateServerSceneEvent(sceneName, true);
            if (sceneEventProgress.Status != SceneEventProgressStatus.Started)
            {
                return sceneEventProgress.Status;
            }

            SceneEventData.SceneEventGuid = sceneEventProgress.Guid;
            SceneEventData.SceneEventType = SceneEventData.SceneEventTypes.S2C_Unload;
            SceneEventData.SceneIndex = SceneNameToIndex[sceneName];
            sceneEventProgress.SceneEventType = SceneEventData.SceneEventTypes.S2C_UnLoadComplete;

            // Sends the unload scene notification
            SendSceneEventData(m_NetworkManager.ConnectedClientsIds.Where(c => c != m_NetworkManager.ServerClientId).ToArray());

            AsyncOperation sceneUnload = SceneManager.UnloadSceneAsync(sceneToUnload);
            sceneUnload.completed += (AsyncOperation asyncOp2) => { OnSceneUnloaded(); };
            sceneEventProgress.SetSceneLoadOperation(sceneUnload);
            if (m_ScenesLoaded.Contains(sceneName))
            {
                m_ScenesLoaded.Remove(sceneName);
            }

            // Notify local server that a scene is going to be unloaded
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                AsyncOperation = sceneUnload,
                SceneEventType = SceneEventData.SceneEventType,
                LoadSceneMode = SceneEventData.LoadSceneMode,
                SceneName = sceneName,
                ClientId = m_NetworkManager.ServerClientId  // Server can only invoke this
            });

            //Return the status
            return sceneEventProgress.Status;
        }

        /// <summary>
        /// Client Side:
        /// SceneManager.UnloadSceneAsync handler for clients
        /// </summary>
        private void OnClientUnloadScene()
        {

            var sceneName = GetSceneNameFromNetcodeSceneIndex(SceneEventData.SceneIndex);
            if (sceneName == string.Empty)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning("Server requested a scene switch to a non-registered scene");
                }

                return;
            }
            s_IsSceneEventActive = true;

            var sceneUnload = (AsyncOperation)null;

            // Don't unload anything while unit testing (i.e. multi-instance *not* multi-process)
            if (!IsUnitTesting)
            {
                sceneUnload = SceneManager.UnloadSceneAsync(sceneName);
                sceneUnload.completed += asyncOp2 => OnSceneUnloaded();
            }

            if (m_ScenesLoaded.Contains(sceneName))
            {
                m_ScenesLoaded.Remove(sceneName);
            }

            // Notify the local client that a scene is going to be unloaded
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                AsyncOperation = IsUnitTesting ? new AsyncOperation() : sceneUnload,
                SceneEventType = SceneEventData.SceneEventType,
                LoadSceneMode = SceneEventData.LoadSceneMode,
                SceneName = sceneName,
                ClientId = m_NetworkManager.LocalClientId   // Server sent this message to the client, but client is executing it
            });

            // Pass through for unit test (i.e. multi-instance *not* multi-process)
            if (IsUnitTesting)
            {
                OnSceneUnloaded();
            }
        }

        /// <summary>
        /// Server and Client:
        /// Invoked when the additively loaded scene is unloaded
        /// </summary>
        private void OnSceneUnloaded()
        {
            SceneEventData.SceneEventType = SceneEventData.SceneEventTypes.C2S_UnloadComplete;

            //First, notify the client or server that a scene was unloaded
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                SceneEventType = SceneEventData.SceneEventType,
                LoadSceneMode = SceneEventData.LoadSceneMode,
                SceneName = GetSceneNameFromNetcodeSceneIndex(SceneEventData.SceneIndex),
                ClientId = m_NetworkManager.IsServer ? m_NetworkManager.ServerClientId : m_NetworkManager.LocalClientId
            });

            if (!m_NetworkManager.IsServer)
            {
                SendSceneEventData(new ulong[] { m_NetworkManager.ServerClientId });
            }
            else //Second, server sets itself as having finished loading
            {
                if (SceneEventProgressTracking.ContainsKey(SceneEventData.SceneEventGuid))
                {
                    SceneEventProgressTracking[SceneEventData.SceneEventGuid].AddClientAsDone(m_NetworkManager.ServerClientId);
                }
            }

            s_IsSceneEventActive = false;
        }

        /// <summary>
        /// Server side: Loads the scene name in either additive or single loading mode.
        /// When applicable, the <see cref="AsyncOperation"/> is delivered within the <see cref="SceneEvent"/> via the <see cref="OnSceneEvent"/>
        /// </summary>
        /// <param name="sceneName">the name of the scene to be loaded</param>
        /// <returns><see cref="SceneEventProgressStatus"/> (<see cref="SceneEventProgressStatus.Started"/> means it was successful)</returns>
        public SceneEventProgressStatus LoadScene(string sceneName, LoadSceneMode loadSceneMode)
        {
            var sceneEventProgress = ValidateServerSceneEvent(sceneName);
            if (sceneEventProgress.Status != SceneEventProgressStatus.Started)
            {
                return sceneEventProgress.Status;
            }

            sceneEventProgress.SceneEventType = SceneEventData.SceneEventTypes.S2C_LoadComplete;
            sceneEventProgress.LoadSceneMode = loadSceneMode;
            SceneEventData.SceneEventGuid = sceneEventProgress.Guid;
            SceneEventData.SceneEventType = SceneEventData.SceneEventTypes.S2C_Load;
            SceneEventData.SceneIndex = SceneNameToIndex[sceneName];
            SceneEventData.LoadSceneMode = loadSceneMode;

            if (SceneEventData.LoadSceneMode == LoadSceneMode.Single)
            {
                // Destroy current scene objects before switching.
                m_NetworkManager.SpawnManager.ServerDestroySpawnedSceneObjects();

                // Preserve the objects that should not be destroyed during the scene event
                MoveObjectsToDontDestroyOnLoad();
            }

            var currentActiveScene = SceneManager.GetActiveScene();
            // Unload all additive scenes while making sure we don't try to unload the base scene ( loaded in single mode ).
            if (SceneEventData.LoadSceneMode == LoadSceneMode.Single)
            {
                foreach (var additiveSceneName in m_ScenesLoaded)
                {
                    if (currentActiveScene.name != additiveSceneName)
                    {
                        OnSceneEvent?.Invoke(new SceneEvent()
                        {
                            SceneEventType = SceneEventData.SceneEventTypes.S2C_Unload,
                            LoadSceneMode = LoadSceneMode.Additive,
                            SceneName = additiveSceneName,
                            ClientId = m_NetworkManager.ServerClientId
                        });
                    }
                }
                m_ScenesLoaded.Clear();
            }

            // Now start loading the scene
            AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
            sceneLoad.completed += (AsyncOperation asyncOp2) => { OnSceneLoaded(sceneName); };
            sceneEventProgress.SetSceneLoadOperation(sceneLoad);

            // Notify the local server that a scene loading event has begun
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                AsyncOperation = sceneLoad,
                SceneEventType = SceneEventData.SceneEventType,
                LoadSceneMode = SceneEventData.LoadSceneMode,
                SceneName = sceneName,
                ClientId = m_NetworkManager.ServerClientId
            });

            //Return our scene progress instance
            return sceneEventProgress.Status;
        }

        /// <summary>
        /// Client Side:
        /// Handles both forms of scene loading
        /// </summary>
        /// <param name="objectStream">Stream data associated with the event</param>
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

            // Unload all additive scenes while making sure we don't try to unload the base scene ( loaded in single mode ).
            var currentActiveScene = SceneManager.GetActiveScene();
            if (SceneEventData.LoadSceneMode == LoadSceneMode.Single)
            {
                foreach (var loadedSceneName in m_ScenesLoaded)
                {
                    if (currentActiveScene.name != loadedSceneName)
                    {
                        Debug.Log($"Invoking unload scene event for {loadedSceneName}");
                        OnSceneEvent?.Invoke(new SceneEvent()
                        {
                            AsyncOperation = SceneManager.UnloadSceneAsync(loadedSceneName),
                            SceneEventType = SceneEventData.SceneEventTypes.S2C_Unload,
                            LoadSceneMode = LoadSceneMode.Additive,
                            SceneName = loadedSceneName,
                            ClientId = m_NetworkManager.LocalClientId
                        });
                    }
                }
                m_ScenesLoaded.Clear();

                // Move ALL NetworkObjects to the temp scene
                MoveObjectsToDontDestroyOnLoad();
            }

            // The Condition: While a scene is asynchronously loaded in single loading scene mode, if any new NetworkObjects are spawned
            // they need to be moved into the do not destroy temporary scene
            // When it is set: Just before starting the asynchronous loading call
            // When it is unset: After the scene has loaded, the PopulateScenePlacedObjects is called, and all NetworkObjects in the do
            // not destroy temporary scene are moved into the active scene
            if (SceneEventData.LoadSceneMode == LoadSceneMode.Single)
            {
                IsSpawnedObjectsPendingInDontDestroyOnLoad = true;
            }

            var sceneLoad = (AsyncOperation)null;
            // Don't load anything while unit testing (i.e. multi-instance *not* multi-process)
            if (!IsUnitTesting)
            {
                sceneLoad = SceneManager.LoadSceneAsync(sceneName, SceneEventData.LoadSceneMode);
                sceneLoad.completed += asyncOp2 => OnSceneLoaded(sceneName);
            }

            OnSceneEvent?.Invoke(new SceneEvent()
            {
                AsyncOperation = IsUnitTesting ? new AsyncOperation() : sceneLoad,
                SceneEventType = SceneEventData.SceneEventType,
                LoadSceneMode = SceneEventData.LoadSceneMode,
                SceneName = sceneName,
                ClientId = m_NetworkManager.LocalClientId
            });

            // Pass through for unit test (i.e. multi-instance *not* multi-process)
            if (IsUnitTesting)
            {
                OnSceneLoaded(sceneName);
            }
        }

        /// <summary>
        /// Client and Server:
        /// Generic on scene loaded callback method to be called upon a scene loading
        /// </summary>
        private void OnSceneLoaded(string sceneName)
        {
            var nextScene = SceneManager.GetSceneByName(sceneName);
            if (SceneEventData.LoadSceneMode == LoadSceneMode.Single)
            {
                SceneManager.SetActiveScene(nextScene);
            }

            if (!m_ScenesLoaded.Contains(sceneName))
            {
                m_ScenesLoaded.Add(sceneName);
            }

            //Get all NetworkObjects loaded by the scene
            PopulateScenePlacedObjects(nextScene);

            if (SceneEventData.LoadSceneMode == LoadSceneMode.Single)
            {
                // Move all objects to the new scene
                MoveObjectsToScene(nextScene);
            }

            // The Condition: While a scene is asynchronously loaded in single loading scene mode, if any new NetworkObjects are spawned
            // they need to be moved into the do not destroy temporary scene
            // When it is set: Just before starting the asynchronous loading call
            // When it is unset: After the scene has loaded, the PopulateScenePlacedObjects is called, and all NetworkObjects in the do
            // not destroy temporary scene are moved into the active scene
            IsSpawnedObjectsPendingInDontDestroyOnLoad = false;

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
        /// Server side:
        /// On scene loaded callback method invoked by OnSceneLoading only
        /// </summary>
        private void OnServerLoadedScene()
        {
            // Register in-scene placed NetworkObjects with the netcode
            foreach (var keyValuePair in ScenePlacedObjects)
            {
                if (!keyValuePair.Value.IsPlayerObject)
                {
                    m_NetworkManager.SpawnManager.SpawnNetworkObjectLocally(keyValuePair.Value, m_NetworkManager.SpawnManager.GetNetworkObjectId(), true, false, null, null, false, true);
                }
            }

            // Send all clients the scene load event
            for (int j = 0; j < m_NetworkManager.ConnectedClientsList.Count; j++)
            {
                var clientId = m_NetworkManager.ConnectedClientsList[j].ClientId;
                if (clientId != m_NetworkManager.ServerClientId)
                {

                    uint sceneObjectsToSpawn = 0;

                    foreach (var keyValuePair in ScenePlacedObjects)
                    {
                        if (keyValuePair.Value.Observers.Contains(clientId))
                        {
                            sceneObjectsToSpawn++;
                        }
                    }

                    var context = m_NetworkManager.MessageQueueContainer.EnterInternalCommandContext(k_MessageType, k_ChannelType, new ulong[] { clientId }, k_NetworkUpdateStage);
                    if (context != null)
                    {
                        using (var nonNullContext = (InternalCommandContext)context)
                        {
                            SceneEventData.OnWrite(nonNullContext.NetworkWriter);
                            // Write number of scene objects to spawn
                            nonNullContext.NetworkWriter.WriteUInt32Packed(sceneObjectsToSpawn);
                            foreach (var keyValuePair in ScenePlacedObjects)
                            {
                                if (keyValuePair.Value.Observers.Contains(clientId))
                                {
                                    keyValuePair.Value.SerializeSceneObject(nonNullContext.NetworkWriter, clientId);
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new Exception($"{nameof(NetworkSceneManager)} failed to send event notification {SceneEventData.SceneEventType} to target clientId {clientId}!");
                    }
                }
            }

            s_IsSceneEventActive = false;

            //First, notify local server that the scene was loaded
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                SceneEventType = SceneEventData.SceneEventTypes.C2S_LoadComplete,
                LoadSceneMode = SceneEventData.LoadSceneMode,
                SceneName = GetSceneNameFromNetcodeSceneIndex(SceneEventData.SceneIndex),
                ClientId = m_NetworkManager.ServerClientId
            });

            //Second, set the server as having loaded for the associated SceneEventProgress
            if (SceneEventProgressTracking.ContainsKey(SceneEventData.SceneEventGuid))
            {
                SceneEventProgressTracking[SceneEventData.SceneEventGuid].AddClientAsDone(m_NetworkManager.ServerClientId);
            }
        }

        /// <summary>
        /// Client side:
        /// On scene loaded callback method invoked by OnSceneLoading only
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

            SceneEventData.SceneEventType = SceneEventData.SceneEventTypes.C2S_LoadComplete;
            SendSceneEventData(new ulong[] { m_NetworkManager.ServerClientId });
            s_IsSceneEventActive = false;

            // Notify local client that the scene was loaded
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                SceneEventType = SceneEventData.SceneEventTypes.C2S_LoadComplete,
                LoadSceneMode = SceneEventData.LoadSceneMode,
                SceneName = GetSceneNameFromNetcodeSceneIndex(SceneEventData.SceneIndex),
                ClientId = m_NetworkManager.LocalClientId
            });
        }

        /// <summary>
        /// Server Side:
        /// This is used for players that have just had their connection approved and will assure they are synchronized
        /// properly if they are late joining
        /// </summary>
        /// <param name="ownerClientId">newly joined client identifier</param>
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
            ClientSynchEventData.SceneEventType = SceneEventData.SceneEventTypes.S2C_Sync;

            // Organize how (and when) we serialize our NetworkObjects
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);

                var malpiSceneIndex = GetNetcodeSceneIndexFromScene(scene);

                if (malpiSceneIndex == uint.MaxValue)
                {
                    continue;
                }
                // This would depend upon whether we are additive or note
                if (activeScene == scene)
                {
                    ClientSynchEventData.SceneIndex = malpiSceneIndex;
                }

                // Separate NetworkObjects by scene
                foreach (var networkObject in m_ObservedObjects)
                {
                    // If the current scene we are matching NetworkObjects to does not match and this NetworkObject has no dependent scene, then continue.
                    if (networkObject.gameObject.scene != scene && (networkObject.DependentSceneName == null || networkObject.DependentSceneName == string.Empty))
                    {
                        continue;
                    }
                    else // If this NetworkObject has a dependent scene and the current scene is not the dependent scene, then continue
                    if (networkObject.DependentSceneName != null && networkObject.DependentSceneName != string.Empty && networkObject.DependentSceneName != scene.name)
                    {
                        continue;
                    }
                    ClientSynchEventData.AddNetworkObjectForSynch(malpiSceneIndex, networkObject);
                }
            }

            var context = m_NetworkManager.MessageQueueContainer.EnterInternalCommandContext(k_MessageType, k_ChannelType, new ulong[] { ownerClientId }, k_NetworkUpdateStage);
            if (context != null)
            {
                using (var nonNullContext = (InternalCommandContext)context)
                {
                    ClientSynchEventData.OnWrite(nonNullContext.NetworkWriter);
                }
            }

            // Notify the local server that the client has been sent the SceneEventData.SceneEventTypes.S2C_Event_Sync event
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                SceneEventType = SceneEventData.SceneEventType,
                ClientId = ownerClientId
            });
        }

        /// <summary>
        /// This is called when the client receives the SCENE_EVENT of type SceneEventData.SceneEventTypes.SYNC
        /// Note: This can recurse one additional time by the client if the current scene loaded by the client
        /// is already loaded.
        /// </summary>
        /// <param name="sceneIndex">Netcode sceneIndex to load</param>
        private void OnClientBeginSync(uint sceneIndex)
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
            var loadSceneMode = sceneIndex == SceneEventData.SceneIndex ? SceneEventData.LoadSceneMode : LoadSceneMode.Additive;

            // If this is the beginning of the synchronization event, then send client a notification that synchronization has begun
            if (sceneIndex == SceneEventData.SceneIndex)
            {
                OnSceneEvent?.Invoke(new SceneEvent()
                {
                    SceneEventType = SceneEventData.SceneEventTypes.S2C_Sync,
                    ClientId = m_NetworkManager.LocalClientId,
                });
            }

            // Check to see if the client already has loaded the scene to be loaded
            if (sceneName != activeScene.name)
            {
                // If not, then load the scene
                var sceneLoad = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);

                // Notify local client that a scene load has begun
                OnSceneEvent?.Invoke(new SceneEvent()
                {
                    AsyncOperation = sceneLoad,
                    SceneEventType = SceneEventData.SceneEventTypes.S2C_Load,
                    LoadSceneMode = loadSceneMode,
                    SceneName = sceneName,
                    ClientId = m_NetworkManager.LocalClientId,
                });

                sceneLoad.completed += asyncOp2 => ClientLoadedSynchronization(sceneIndex);
            }
            else
            {
                // If so, then pass through
                ClientLoadedSynchronization(sceneIndex);
            }
        }

        /// <summary>
        /// Once a scene is loaded ( or if it was already loaded) this gets called.
        /// This handles all of the in-scene and dynamically spawned NetworkObject synchronization
        /// </summary>
        /// <param name="sceneIndex">Netcode scene index that was loaded</param>
        private void ClientLoadedSynchronization(uint sceneIndex)
        {
            var sceneName = GetSceneNameFromNetcodeSceneIndex(sceneIndex);
            var nextScene = SceneManager.GetSceneByName(sceneName);
            if (nextScene == null)
            {
                Debug.LogError($"Client was trying to load {sceneIndex} which does not appear to be a valid registered scene index!");
                return;
            }

            var loadSceneMode = (sceneIndex == SceneEventData.SceneIndex ? SceneEventData.LoadSceneMode : LoadSceneMode.Additive);

            // For now, during a synchronization event, we will make the first scene the "base/master" scene that denotes a "complete scene switch"
            if (loadSceneMode == LoadSceneMode.Single)
            {
                SceneManager.SetActiveScene(nextScene);
            }

            if (!m_ScenesLoaded.Contains(sceneName))
            {
                m_ScenesLoaded.Add(sceneName);
            }

            // Get all NetworkObjects loaded by the scene  (in-scene NetworkObjects)
            PopulateScenePlacedObjects(nextScene);

            // Synchronize the NetworkObjects for this scene
            SceneEventData.SynchronizeSceneNetworkObjects(sceneIndex, m_NetworkManager);

            // Send notification back to server that we finished loading this scene
            ClientSynchEventData.LoadSceneMode = loadSceneMode;
            ClientSynchEventData.SceneEventType = SceneEventData.SceneEventTypes.C2S_LoadComplete;
            ClientSynchEventData.SceneIndex = sceneIndex;

            var context = m_NetworkManager.MessageQueueContainer.EnterInternalCommandContext(k_MessageType, k_ChannelType,
                new ulong[] { m_NetworkManager.ServerClientId }, k_NetworkUpdateStage);
            if (context != null)
            {
                using (var nonNullContext = (InternalCommandContext)context)
                {
                    ClientSynchEventData.OnWrite(nonNullContext.NetworkWriter);
                }
            }

            // Send notification to local client that the scene has finished loading
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                SceneEventType = SceneEventData.SceneEventTypes.C2S_LoadComplete,
                LoadSceneMode = loadSceneMode,
                SceneName = sceneName,
                ClientId = m_NetworkManager.LocalClientId,
            });

            // Check to see if we still have scenes to load and synchronize with
            HandleClientSceneEvent(null);
        }

        /// <summary>
        /// Client Side: Handles incoming Scene_Event messages
        /// It is "understood" that the server is the sender
        /// </summary>
        /// <param name="stream">data associated with the event</param>
        private void HandleClientSceneEvent(Stream stream)
        {
            switch (SceneEventData.SceneEventType)
            {
                case SceneEventData.SceneEventTypes.S2C_Load:
                    {
                        OnClientSceneLoadingEvent(stream);
                        break;
                    }
                case SceneEventData.SceneEventTypes.S2C_Unload:
                    {
                        OnClientUnloadScene();
                        break;
                    }
                case SceneEventData.SceneEventTypes.S2C_Sync:
                    {
                        if (!SceneEventData.IsDoneWithSynchronization())
                        {
                            OnClientBeginSync(SceneEventData.GetNextSceneSynchronizationIndex());
                        }
                        else
                        {
                            SceneEventData.SceneEventType = SceneEventData.SceneEventTypes.C2S_SyncComplete;
                            SendSceneEventData(new ulong[] { m_NetworkManager.ServerClientId });

                            // All scenes are synchronized, let the server know we are done synchronizing
                            m_NetworkManager.IsConnectedClient = true;
                            m_NetworkManager.InvokeOnClientConnectedCallback(m_NetworkManager.LocalClientId);

                            // Notify the client that they have finished synchronizing
                            OnSceneEvent?.Invoke(new SceneEvent()
                            {
                                SceneEventType = SceneEventData.SceneEventType,
                                ClientId = m_NetworkManager.LocalClientId, // Client sent this to the server
                            });
                        }
                        break;
                    }
                case SceneEventData.SceneEventTypes.S2C_ReSync:
                    {
                        // Notify the client that they have been re-synchronized after being synchronized with an in progress game session
                        OnSceneEvent?.Invoke(new SceneEvent()
                        {
                            SceneEventType = SceneEventData.SceneEventType,
                            ClientId = m_NetworkManager.ServerClientId,  // Server sent this to client
                        });

                        break;
                    }
                case SceneEventData.SceneEventTypes.S2C_LoadComplete:
                case SceneEventData.SceneEventTypes.S2C_UnLoadComplete:
                    {
                        // Notify client that all clients have finished loading or unloading
                        OnSceneEvent?.Invoke(new SceneEvent()
                        {
                            SceneEventType = SceneEventData.SceneEventType,
                            SceneName = m_NetworkManager.SceneManager.GetSceneNameFromNetcodeSceneIndex(SceneEventData.SceneIndex),
                            ClientId = m_NetworkManager.ServerClientId,
                            LoadSceneMode = SceneEventData.LoadSceneMode,
                            ClientsThatCompleted = SceneEventData.ClientsCompleted,
                            ClientsThatTimedOut = SceneEventData.ClientsTimedOut,
                        });
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
        /// Server Side: Handles incoming Scene_Event messages
        /// </summary>
        /// <param name="clientId">client who sent the event</param>
        /// <param name="stream">data associated with the event</param>
        private void HandleServerSceneEvent(ulong clientId, Stream stream)
        {
            switch (SceneEventData.SceneEventType)
            {
                case SceneEventData.SceneEventTypes.C2S_LoadComplete:
                    {
                        // Notify the local server that the client has finished loading a scene
                        OnSceneEvent?.Invoke(new SceneEvent()
                        {
                            SceneEventType = SceneEventData.SceneEventType,
                            LoadSceneMode = SceneEventData.LoadSceneMode,
                            SceneName = GetSceneNameFromNetcodeSceneIndex(SceneEventData.SceneIndex),
                            ClientId = clientId
                        });

                        if (SceneEventProgressTracking.ContainsKey(SceneEventData.SceneEventGuid))
                        {
                            SceneEventProgressTracking[SceneEventData.SceneEventGuid].AddClientAsDone(clientId);
                        }

                        break;
                    }
                case SceneEventData.SceneEventTypes.C2S_UnloadComplete:
                    {
                        if (SceneEventProgressTracking.ContainsKey(SceneEventData.SceneEventGuid))
                        {
                            SceneEventProgressTracking[SceneEventData.SceneEventGuid].AddClientAsDone(clientId);
                        }
                        // Notify the local server that the client has finished unloading a scene
                        OnSceneEvent?.Invoke(new SceneEvent()
                        {
                            SceneEventType = SceneEventData.SceneEventType,
                            LoadSceneMode = SceneEventData.LoadSceneMode,
                            SceneName = GetSceneNameFromNetcodeSceneIndex(SceneEventData.SceneIndex),
                            ClientId = clientId
                        });

                        break;
                    }
                case SceneEventData.SceneEventTypes.C2S_SyncComplete:
                    {
                        // Notify the local server that a client has finished synchronizing
                        OnSceneEvent?.Invoke(new SceneEvent()
                        {
                            SceneEventType = SceneEventData.SceneEventType,
                            SceneName = string.Empty,
                            ClientId = clientId
                        });

                        if (SceneEventData.ClientNeedsReSynchronization() && !DisableReSynchronization)
                        {
                            SceneEventData.SceneEventType = SceneEventData.SceneEventTypes.S2C_ReSync;
                            SendSceneEventData(new ulong[] { clientId });

                            OnSceneEvent?.Invoke(new SceneEvent()
                            {
                                SceneEventType = SceneEventData.SceneEventType,
                                SceneName = string.Empty,
                                ClientId = clientId
                            });
                        }

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
                    var reader = NetworkReaderPool.GetReader(stream);
                    SceneEventData.OnRead(reader);
                    NetworkReaderPool.PutBackInPool(reader);
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

        /// <summary>
        /// Moves all NetworkObjects that don't have the <see cref="NetworkObject.DestroyWithScene"/> set to
        /// the "Do not destroy on load" scene.
        /// </summary>
        private void MoveObjectsToDontDestroyOnLoad()
        {
            // Move ALL NetworkObjects to the temp scene
            var objectsToKeep = new HashSet<NetworkObject>(m_NetworkManager.SpawnManager.SpawnedObjectsList);

            foreach (var sobj in objectsToKeep)
            {
                //In case an object has been set as a child of another object it has to be removed from the parent in order to be moved from one scene to another.
                if (sobj.gameObject.transform.parent != null)
                {
                    sobj.gameObject.transform.parent = null;
                }

                if (!sobj.DestroyWithScene)
                {
                    UnityEngine.Object.DontDestroyOnLoad(sobj.gameObject);
                }
                else if (m_NetworkManager.IsServer)
                {
                    sobj.Despawn(true);
                }
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

        /// <summary>
        /// Moves all spawned NetworkObjects (from do not destroy on load) to the scene specified
        /// </summary>
        /// <param name="scene">scene to move the NetworkObjects to</param>
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
    }
}
