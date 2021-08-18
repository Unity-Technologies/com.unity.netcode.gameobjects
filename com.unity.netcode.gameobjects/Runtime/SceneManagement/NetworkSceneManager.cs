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
        /// When a scene is loaded, the Scene structure is returned.
        /// </summary>
        public Scene Scene;

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
        // Used to be able to turn re-synchronization off for future snapshot development purposes.
        internal static bool DisableReSynchronization;

        /// <summary>
        /// Used to detect if a scene event is underway
        /// Only 1 scene event can occur on the server at a time for now.
        /// </summary>
        private static bool s_IsSceneEventActive = false;

        /// <summary>
        /// For multi-instance unit tests, set this to true if you are use the <see cref="NetworkSceneManager"/>
        /// </summary>
        internal static bool IsTesting;

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

        /// <summary>
        /// Used to track in-scene placed NetworkObjects
        /// We store them by:
        /// [GlobalObjectIdHash][Scene.Handle][NetworkObject]
        /// The Scene.Handle aspect allows us to distinguish duplicated in-scene placed NetworkObjects created by the loading
        /// of the same additive scene multiple times.
        /// </summary>
        internal readonly Dictionary<uint, Dictionary<int, NetworkObject>> ScenePlacedObjects = new Dictionary<uint, Dictionary<int, NetworkObject>>();

        /// <summary>
        /// This is used for the deserialization of in-scene placed NetworkObjects in order to distinguish duplicated in-scene
        /// placed NetworkObjects created by the loading of the same additive scene multiple times.
        /// </summary>
        internal Scene SceneBeingSynchronized;

        // Used to track which scenes are currently loaded
        // We store scenes as follows: [SceneName][SceneHandle][Scene]
        private Dictionary<string, Dictionary<int, Scene>> m_ScenesLoaded = new Dictionary<string, Dictionary<int, Scene>>();

        /// <summary>
        /// Since Scene.handle is unique per client, we create a look-up table between the client and server
        /// </summary>
        internal Dictionary<int, int> ServerSceneHandleToClientSceneHandle = new Dictionary<int, int>();

        /// <summary>
        /// The Condition: While a scene is asynchronously loaded in single loading scene mode, if any new NetworkObjects are spawned
        /// they need to be moved into the do not destroy temporary scene
        /// When it is set: Just before starting the asynchronous loading call
        /// When it is unset: After the scene has loaded, the PopulateScenePlacedObjects is called, and all NetworkObjects in the do
        /// not destroy temporary scene are moved into the active scene
        /// </summary>
        internal static bool IsSpawnedObjectsPendingInDontDestroyOnLoad = false;

        /// <summary>
        /// Client and Server:
        /// Used for all scene event processing except for ClientSynchEventData specific events
        /// </summary>
        internal SceneEventData SceneEventData;

        /// <summary>
        /// Server Side:
        /// Used specifically for scene synchronization and scene event progress related events.
        /// </summary>
        internal SceneEventData ClientSynchEventData;

        private NetworkManager m_NetworkManager { get; }

        private const MessageQueueContainer.MessageType k_MessageType = MessageQueueContainer.MessageType.SceneEvent;
        private const NetworkChannel k_ChannelType = NetworkChannel.Internal;
        private const NetworkUpdateStage k_NetworkUpdateStage = NetworkUpdateStage.EarlyUpdate;

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
        /// Since SceneManager.GetSceneByName only returns the first scene that matches the name
        /// we must "find" a newly added scene by looking through all loaded scenes and determining
        /// which scene with the same name has not yet been loaded.
        /// In order to support loading the same additive scene within in-scene placed NetworkObjects,
        /// we must do this to be able to soft synchronize the "right version" of the NetworkObject.
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns></returns>
        internal Scene GetAndAddNewlyLoadedSceneByName(string sceneName)
        {

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var sceneLoaded = SceneManager.GetSceneAt(i);
                if (sceneLoaded.name == sceneName)
                {
                    if (m_ScenesLoaded.ContainsKey(sceneName))
                    {
                        if (!m_ScenesLoaded[sceneName].ContainsKey(sceneLoaded.handle))
                        {
                            m_ScenesLoaded[sceneName].Add(sceneLoaded.handle, sceneLoaded);
                            return sceneLoaded;
                        }
                    }
                    else
                    {
                        // On a new entry we add the entry and scene then we are done.
                        m_ScenesLoaded.Add(sceneName, new Dictionary<int, Scene>());
                        m_ScenesLoaded[sceneName].Add(sceneLoaded.handle, sceneLoaded);
                        return sceneLoaded;
                    }
                }
            }

            throw new Exception("Failed to find scene that was loaded!");
        }

        /// <summary>
        /// Client Side Only:
        /// This takes a server scene handle that is written by the server before the scene relative
        /// NetworkObject is serialized and converts the server scene handle to a local client handle
        /// so it can set the appropriate SceneBeingSynchronized.
        /// Note: This is now part of the soft synchronization process and is needed for the scenario
        /// where a user loads the same scene additively that has an in-scene placed NetworkObject
        /// which means each scene relative in-scene placed NetworkObject will have the identical GlobalObjectIdHash
        /// value.  Scene handles are used to distinguish between in-scene placed NetworkObjects under this situation.
        /// </summary>
        /// <param name="serverSceneHandle"></param>
        internal void SetTheSceneBeingSynchronized(int serverSceneHandle)
        {
            if (IsTesting)
            {
                // If we were already set, then ignore
                if (SceneBeingSynchronized.IsValid() && SceneBeingSynchronized.isLoaded)
                {
                    return;
                }
                SceneBeingSynchronized = SceneManager.GetActiveScene();
                return;
            }

            var clientSceneHandle = serverSceneHandle;
            if (m_NetworkManager.SceneManager.ServerSceneHandleToClientSceneHandle.ContainsKey(serverSceneHandle))
            {
                clientSceneHandle = m_NetworkManager.SceneManager.ServerSceneHandleToClientSceneHandle[serverSceneHandle];
                // If we were already set, then ignore
                if (SceneBeingSynchronized.IsValid() && SceneBeingSynchronized.isLoaded && SceneBeingSynchronized.handle == clientSceneHandle)
                {
                    return;
                }

                // Find and set the scene currently being synchronized
                SceneBeingSynchronized = new Scene();
                foreach (var keyValuePairBySceneName in m_ScenesLoaded)
                {
                    if (keyValuePairBySceneName.Value.ContainsKey(clientSceneHandle))
                    {
                        SceneBeingSynchronized = keyValuePairBySceneName.Value[clientSceneHandle];
                    }
                }

                if (!SceneBeingSynchronized.IsValid() || !SceneBeingSynchronized.isLoaded)
                {
                    throw new Exception($"[{nameof(NetworkSceneManager)}- {nameof(m_ScenesLoaded)}] Could not find the appropriate scene to set as being synchronized!");
                }
            }
            else
            {
                // This should never happen, but in the event it does...
                throw new Exception($"[{nameof(SceneEventData)}- Scene Handle Mismatch] {nameof(serverSceneHandle)} could not be found in {nameof(ServerSceneHandleToClientSceneHandle)}!");
            }
        }

        /// <summary>
        /// During soft synchronization of in-scene placed NetworkObjects, this is now used by NetworkSpawnManager.CreateLocalNetworkObject
        /// </summary>
        /// <param name="globalObjectIdHash"></param>
        /// <returns></returns>
        internal NetworkObject GetSceneRelativeInSceneNetworkObject(uint globalObjectIdHash)
        {
            if (ScenePlacedObjects.ContainsKey(globalObjectIdHash))
            {
                if (ScenePlacedObjects[globalObjectIdHash].ContainsKey(SceneBeingSynchronized.handle))
                {
                    var inScenePlacedNetworkObject = ScenePlacedObjects[globalObjectIdHash][SceneBeingSynchronized.handle];

                    // We can only have 1 duplicated globalObjectIdHash per scene instance, so remove it once it has been returned
                    ScenePlacedObjects[globalObjectIdHash].Remove(SceneBeingSynchronized.handle);

                    return inScenePlacedNetworkObject;
                }
            }
            return null;
        }

        /// <summary>
        /// Generic sending of scene event data
        /// </summary>
        /// <param name="targetClientIds">array of client identifiers to receive the scene event message</param>
        private void SendSceneEventData(ulong[] targetClientIds)
        {
            if (targetClientIds.Length == 0)
            {
                // This would be the Host/Server with no clients connected
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
        private uint GetNetcodeSceneIndexFromScene(Scene scene)
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
        private string GetSceneNameFromNetcodeSceneIndex(uint sceneIndex)
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
                throw new NotServerException("Only server can start a scene event!");
            }

            if (!m_NetworkManager.NetworkConfig.EnableSceneManagement)
            {
                //Log message about enabling SceneManagement
                throw new Exception($"{nameof(NetworkConfig.EnableSceneManagement)} flag is not enabled in the {nameof(NetworkManager)}'s {nameof(NetworkConfig)}. " +
                    $"Please set {nameof(NetworkConfig.EnableSceneManagement)} flag to true before calling " +
                    $"{nameof(NetworkSceneManager.LoadScene)} or {nameof(NetworkSceneManager.UnloadScene)}.");
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
        private bool OnSceneEventProgressCompleted(SceneEventProgress sceneEventProgress)
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
        /// Server Side:
        /// Unloads an additively loaded scene.  If you want to unload a <see cref="LoadSceneMode.Single"/> mode loaded scene load another <see cref="LoadSceneMode.Single"/> scene.
        /// When applicable, the <see cref="AsyncOperation"/> is delivered within the <see cref="SceneEvent"/> via the <see cref="OnSceneEvent"/>
        /// </summary>
        /// <param name="sceneName">scene name to unload</param>
        /// <returns><see cref="SceneEventProgressStatus"/> (<see cref="SceneEventProgressStatus.Started"/> means it was successful)</returns>
        public SceneEventProgressStatus UnloadScene(Scene scene)
        {
            var sceneName = scene.name;
            if (!scene.isLoaded)
            {
                Debug.LogWarning($"{nameof(UnloadScene)} was called, but the scene {scene.name} is not currently loaded!");
                return SceneEventProgressStatus.SceneNotLoaded;
            }

            var sceneEventProgress = ValidateServerSceneEvent(sceneName, true);
            if (sceneEventProgress.Status != SceneEventProgressStatus.Started)
            {
                return sceneEventProgress.Status;
            }

            if (!m_ScenesLoaded.ContainsKey(sceneName) || !m_ScenesLoaded[sceneName].ContainsKey(scene.handle))
            {
                Debug.LogError($"{nameof(UnloadScene)} internal error! {sceneName} with handle {scene.handle} is not within the internal scenes loaded dictionary!");
                return SceneEventProgressStatus.InternalNetcodeError;
            }

            SceneEventData.SceneEventGuid = sceneEventProgress.Guid;
            SceneEventData.SceneEventType = SceneEventData.SceneEventTypes.S2C_Unload;
            SceneEventData.SceneIndex = SceneNameToIndex[sceneName];
            SceneEventData.SceneHandle = m_ScenesLoaded[sceneName][scene.handle].handle;

            // This will be the message we send to everyone when this scene event sceneEventProgress is complete
            sceneEventProgress.SceneEventType = SceneEventData.SceneEventTypes.S2C_UnLoadComplete;

            // Sends the unload scene notification
            SendSceneEventData(m_NetworkManager.ConnectedClientsIds.Where(c => c != m_NetworkManager.ServerClientId).ToArray());

            m_ScenesLoaded[sceneName].Remove(scene.handle);

            if (m_ScenesLoaded[sceneName].Count == 0)
            {
                m_ScenesLoaded.Remove(sceneName);
            }

            AsyncOperation sceneUnload = SceneManager.UnloadSceneAsync(scene);
            sceneUnload.completed += (AsyncOperation asyncOp2) => { OnSceneUnloaded(); };
            sceneEventProgress.SetSceneLoadOperation(sceneUnload);

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
        /// Handles <see cref="SceneEventData.SceneEventTypes.S2C_Unload"/> scene events.
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

            if (m_ScenesLoaded.ContainsKey(sceneName))
            {
                if (!ServerSceneHandleToClientSceneHandle.ContainsKey(SceneEventData.SceneHandle))
                {
                    throw new Exception("No server to scene handle exist!");
                }
                var sceneHandle = ServerSceneHandleToClientSceneHandle[SceneEventData.SceneHandle];
                if (m_ScenesLoaded[sceneName].ContainsKey(sceneHandle))
                {
                    var sceneUnload = SceneManager.UnloadSceneAsync(m_ScenesLoaded[sceneName][sceneHandle]);

                    sceneUnload.completed += asyncOp2 => OnSceneUnloaded();

                    m_ScenesLoaded[sceneName].Remove(sceneHandle);

                    // Remove our server to scene handle lookup
                    ServerSceneHandleToClientSceneHandle.Remove(SceneEventData.SceneHandle);
                    if (m_ScenesLoaded[sceneName].Count == 0)
                    {
                        m_ScenesLoaded.Remove(sceneName);
                    }

                    // Notify the local client that a scene is going to be unloaded
                    OnSceneEvent?.Invoke(new SceneEvent()
                    {
                        AsyncOperation = sceneUnload,
                        SceneEventType = SceneEventData.SceneEventType,
                        LoadSceneMode = SceneEventData.LoadSceneMode,
                        SceneName = sceneName,
                        ClientId = m_NetworkManager.LocalClientId   // Server sent this message to the client, but client is executing it
                    });
                }
                else
                {
                    // Error scene handle not found!
                    Debug.LogError("Server Scene Handle Not Found!");
                }
            }
            else
            {

                // Error scene not loaded!
                Debug.LogError("Server Scene Handle Not Loaded!");
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
        /// Clears all scenes when loading in single mode
        /// Since we assume a single mode loaded scene will be considered the "currently active scene",
        /// we only unload any additively loaded scenes.
        /// </summary>
        internal void UnloadAdditivelyLoadedScenes()
        {
            // Unload all additive scenes while making sure we don't try to unload the base scene ( loaded in single mode ).
            var currentActiveScene = SceneManager.GetActiveScene();
            foreach (var keyRootSceneEntry in m_ScenesLoaded)
            {
                foreach (var keyHandleEntry in keyRootSceneEntry.Value)
                {
                    if (currentActiveScene.name != keyHandleEntry.Value.name)
                    {
                        OnSceneEvent?.Invoke(new SceneEvent()
                        {
                            AsyncOperation = SceneManager.UnloadSceneAsync(keyHandleEntry.Value),
                            SceneEventType = SceneEventData.SceneEventTypes.S2C_Unload,
                            LoadSceneMode = LoadSceneMode.Additive,
                            SceneName = keyHandleEntry.Value.name,
                            ClientId = m_NetworkManager.ServerClientId
                        });
                    }
                }
            }
            // clear out our scenes loaded list
            m_ScenesLoaded.Clear();
        }

        /// <summary>
        /// Server side:
        /// Loads the scene name in either additive or single loading mode.
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

            // This will be the message we send to everyone when this scene event sceneEventProgress is complete
            sceneEventProgress.SceneEventType = SceneEventData.SceneEventTypes.S2C_LoadComplete;
            sceneEventProgress.LoadSceneMode = loadSceneMode;

            // Now set up the current scene event
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

                // Now Unload all currently additively loaded scenes
                UnloadAdditivelyLoadedScenes();
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
        private void OnClientSceneLoadingEvent(Stream objectStream)
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

                // Now Unload all currently additively loaded scenes
                UnloadAdditivelyLoadedScenes();
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

            var sceneLoad = SceneManager.LoadSceneAsync(sceneName, SceneEventData.LoadSceneMode);
            sceneLoad.completed += asyncOp2 => OnSceneLoaded(sceneName);

            OnSceneEvent?.Invoke(new SceneEvent()
            {
                AsyncOperation = sceneLoad,
                SceneEventType = SceneEventData.SceneEventType,
                LoadSceneMode = SceneEventData.LoadSceneMode,
                SceneName = sceneName,
                ClientId = m_NetworkManager.LocalClientId
            });
        }


        /// <summary>
        /// Client and Server:
        /// Generic on scene loaded callback method to be called upon a scene loading
        /// </summary>
        private void OnSceneLoaded(string sceneName)
        {
            var nextScene = GetAndAddNewlyLoadedSceneByName(sceneName);
            if (!nextScene.isLoaded || !nextScene.IsValid())
            {
                throw new Exception($"Failed to find valid scene internal Unity.Netcode for {nameof(GameObject)}s error!");
            }

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

            // The Condition: While a scene is asynchronously loaded in single loading scene mode, if any new NetworkObjects are spawned
            // they need to be moved into the do not destroy temporary scene
            // When it is set: Just before starting the asynchronous loading call
            // When it is unset: After the scene has loaded, the PopulateScenePlacedObjects is called, and all NetworkObjects in the do
            // not destroy temporary scene are moved into the active scene
            IsSpawnedObjectsPendingInDontDestroyOnLoad = false;

            if (m_NetworkManager.IsServer)
            {
                OnServerLoadedScene(nextScene);
            }
            else
            {
                // For the client, we make a server scene handle to client scene handle look up table
                if (!ServerSceneHandleToClientSceneHandle.ContainsKey(SceneEventData.SceneHandle))
                {
                    ServerSceneHandleToClientSceneHandle.Add(SceneEventData.SceneHandle, nextScene.handle);
                }
                else
                {
                    // If the exact same handle exists then there are problems with using handles
                    throw new Exception($"Server Scene Handle ({SceneEventData.SceneHandle}) already exist!  Happened during scene load of {nextScene.name} with Client Handle ({nextScene.handle})");
                }

                OnClientLoadedScene(nextScene);
            }
        }

        /// <summary>
        /// Server side:
        /// On scene loaded callback method invoked by OnSceneLoading only
        /// </summary>
        private void OnServerLoadedScene(Scene scene)
        {
            // Register in-scene placed NetworkObjects with the netcode
            foreach (var keyValuePairByGlobalObjectIdHash in ScenePlacedObjects)
            {
                foreach (var keyValuePairBySceneHandle in keyValuePairByGlobalObjectIdHash.Value)
                {
                    if (!keyValuePairBySceneHandle.Value.IsPlayerObject)
                    {
                        m_NetworkManager.SpawnManager.SpawnNetworkObjectLocally(keyValuePairBySceneHandle.Value, m_NetworkManager.SpawnManager.GetNetworkObjectId(), true, false, null, null, false, true);
                    }
                }
            }

            // Set the server's scene's handle so the client can build a look up table
            SceneEventData.SceneHandle = scene.handle;

            // Send all clients the scene load event
            for (int j = 0; j < m_NetworkManager.ConnectedClientsList.Count; j++)
            {
                var clientId = m_NetworkManager.ConnectedClientsList[j].ClientId;
                if (clientId != m_NetworkManager.ServerClientId)
                {

                    uint sceneObjectsToSpawn = 0;

                    foreach (var keyValuePairByGlobalObjectIdHash in ScenePlacedObjects)
                    {
                        foreach (var keyValuePairBySceneHandle in keyValuePairByGlobalObjectIdHash.Value)
                        {
                            if (keyValuePairBySceneHandle.Value.Observers.Contains(clientId))
                            {
                                sceneObjectsToSpawn++;
                            }
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

                            foreach (var keyValuePairByGlobalObjectIdHash in ScenePlacedObjects)
                            {
                                foreach (var keyValuePairBySceneHandle in keyValuePairByGlobalObjectIdHash.Value)
                                {
                                    if (keyValuePairBySceneHandle.Value.Observers.Contains(clientId))
                                    {
                                        // Write our server relative scene handle for the NetworkObject being serialized
                                        nonNullContext.NetworkWriter.WriteInt32Packed(keyValuePairBySceneHandle.Key);
                                        // Serialize the NetworkObject
                                        keyValuePairBySceneHandle.Value.SerializeSceneObject(nonNullContext.NetworkWriter, clientId);
                                    }
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
                ClientId = m_NetworkManager.ServerClientId,
                Scene = scene,
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
        private void OnClientLoadedScene(Scene scene)
        {
            using (var reader = PooledNetworkReader.Get(SceneEventData.InternalBuffer))
            {
                var newObjectsCount = reader.ReadUInt32Packed();

                for (int i = 0; i < newObjectsCount; i++)
                {
                    // Set our relative scene to the NetworkObject
                    SetTheSceneBeingSynchronized(reader.ReadInt32Packed());

                    // Deserialize the NetworkObject
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
                ClientId = m_NetworkManager.LocalClientId,
                Scene = scene,
            });
        }

        /// <summary>
        /// Server Side:
        /// This is used for players that have just had their connection approved and will assure they are synchronized
        /// properly if they are late joining
        /// Note: We write out all of the scenes to be loaded first and then all of the NetworkObjects that need to be
        /// synchronized.
        /// </summary>
        /// <param name="ownerClientId">newly joined client identifier</param>
        internal void SynchronizeNetworkObjects(ulong ownerClientId)
        {
            foreach (var sobj in m_NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (sobj.CheckObjectVisibility == null || sobj.CheckObjectVisibility(ownerClientId))
                {
                    sobj.Observers.Add(ownerClientId);
                }
            }

            ClientSynchEventData.InitializeForSynch();
            ClientSynchEventData.TargetClientId = ownerClientId;
            ClientSynchEventData.LoadSceneMode = LoadSceneMode.Single;
            var activeScene = SceneManager.GetActiveScene();
            ClientSynchEventData.SceneEventType = SceneEventData.SceneEventTypes.S2C_Sync;

            // Organize how (and when) we serialize our NetworkObjects
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);

                var sceneIndex = GetNetcodeSceneIndexFromScene(scene);

                if (sceneIndex == uint.MaxValue)
                {
                    continue;
                }
                // This would depend upon whether we are additive or not
                // If we are the base scene, then we set the root scene index;
                if (activeScene == scene)
                {
                    ClientSynchEventData.SceneIndex = sceneIndex;
                    ClientSynchEventData.SceneHandle = scene.handle;
                }

                ClientSynchEventData.AddSceneToSynchronize(sceneIndex, scene.handle);
            }

            ClientSynchEventData.AddSpawnedNetworkObjects();

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
        private void OnClientBeginSync()
        {
            var sceneIndex = SceneEventData.GetNextSceneSynchronizationIndex();
            var sceneHandle = SceneEventData.GetNextSceneSynchronizationHandle();
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

                // Clear the in-scene placed NetworkObjects when we load the first scene in our synchronization process
                ScenePlacedObjects.Clear();
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

                sceneLoad.completed += asyncOp2 => ClientLoadedSynchronization(sceneIndex, sceneHandle);
            }
            else
            {
                // If so, then pass through
                ClientLoadedSynchronization(sceneIndex, sceneHandle);
            }
        }

        /// <summary>
        /// Once a scene is loaded ( or if it was already loaded) this gets called.
        /// This handles all of the in-scene and dynamically spawned NetworkObject synchronization
        /// </summary>
        /// <param name="sceneIndex">Netcode scene index that was loaded</param>
        private void ClientLoadedSynchronization(uint sceneIndex, int sceneHandle)
        {
            var sceneName = GetSceneNameFromNetcodeSceneIndex(sceneIndex);
            var nextScene = GetAndAddNewlyLoadedSceneByName(sceneName);

            if (!nextScene.isLoaded || !nextScene.IsValid())
            {
                throw new Exception($"Failed to find valid scene internal Unity.Netcode for {nameof(GameObject)}s error!");
            }

            var loadSceneMode = (sceneIndex == SceneEventData.SceneIndex ? SceneEventData.LoadSceneMode : LoadSceneMode.Additive);

            // For now, during a synchronization event, we will make the first scene the "base/master" scene that denotes a "complete scene switch"
            if (loadSceneMode == LoadSceneMode.Single)
            {
                SceneManager.SetActiveScene(nextScene);
            }

            if (!ServerSceneHandleToClientSceneHandle.ContainsKey(sceneHandle))
            {
                ServerSceneHandleToClientSceneHandle.Add(sceneHandle, nextScene.handle);
            }
            else
            {
                // If the exact same handle exists then there are problems with using handles
                throw new Exception($"Server Scene Handle ({SceneEventData.SceneHandle}) already exist!  Happened during scene load of {nextScene.name} with Client Handle ({nextScene.handle})");
            }

            // Apply all in-scene placed NetworkObjects loaded by the scene
            PopulateScenePlacedObjects(nextScene, false);

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
                Scene = nextScene,
                ClientId = m_NetworkManager.LocalClientId,
            });

            // Check to see if we still have scenes to load and synchronize with
            HandleClientSceneEvent(null);
        }

        /// <summary>
        /// Client Side:
        /// Handles incoming Scene_Event messages for clients
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
                            OnClientBeginSync();
                        }
                        else
                        {
                            // Synchronize the NetworkObjects for this scene
                            SceneEventData.SynchronizeSceneNetworkObjects(m_NetworkManager);

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
        /// Server Side:
        /// Handles incoming Scene_Event messages for host or server
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
        internal void HandleSceneEvent(ulong clientId, Stream stream)
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
        /// We organize our ScenePlacedObjects by:
        /// [GlobalObjectIdHash][SceneHandle][NetworkObject]
        /// Using the local scene relative Scene.handle as a sub-key to the root dictionary allows us to
        /// distinguish between duplicate in-scene placed NetworkObjects
        /// </summary>
        private void PopulateScenePlacedObjects(Scene sceneToFilterBy, bool clearScenePlacedObjects = true)
        {
            if (clearScenePlacedObjects)
            {
                ScenePlacedObjects.Clear();
            }

            var networkObjects = UnityEngine.Object.FindObjectsOfType<NetworkObject>();

            // Just add every NetworkObject found that isn't already in the list
            // With additive scenes, we can have multiple in-scene placed NetworkObjects with the same GlobalObjectIdHash value
            // During Client Side Synchronization: We add them on a FIFO basis, for each scene loaded without clearing, and then
            // at the end of scene loading we use this list to soft synchronize all in-scene placed NetworkObjects
            foreach (var networkObjectInstance in networkObjects)
            {
                // We check to make sure the NetworkManager instance is the same one to be "MultiInstanceHelpers" compatible and filter the list on a per scene basis (additive scenes)
                if (networkObjectInstance.IsSceneObject == null && networkObjectInstance.NetworkManager == m_NetworkManager && networkObjectInstance.gameObject.scene == sceneToFilterBy &&
                    networkObjectInstance.gameObject.scene.handle == sceneToFilterBy.handle)
                {
                    if (!ScenePlacedObjects.ContainsKey(networkObjectInstance.GlobalObjectIdHash))
                    {
                        ScenePlacedObjects.Add(networkObjectInstance.GlobalObjectIdHash, new Dictionary<int, NetworkObject>());
                    }

                    if (!ScenePlacedObjects[networkObjectInstance.GlobalObjectIdHash].ContainsKey(networkObjectInstance.gameObject.scene.handle))
                    {
                        ScenePlacedObjects[networkObjectInstance.GlobalObjectIdHash].Add(networkObjectInstance.gameObject.scene.handle, networkObjectInstance);
                    }
                    else
                    {
                        var exitingEntryName = ScenePlacedObjects[networkObjectInstance.GlobalObjectIdHash][networkObjectInstance.gameObject.scene.handle] != null ?
                            ScenePlacedObjects[networkObjectInstance.GlobalObjectIdHash][networkObjectInstance.gameObject.scene.handle].name : "Null Entry";
                        throw new Exception($"{networkObjectInstance.name} tried to registered with {nameof(ScenePlacedObjects)} which already contains " +
                            $"the same {nameof(NetworkObject.GlobalObjectIdHash)} value {networkObjectInstance.GlobalObjectIdHash} for {exitingEntryName}!");
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
