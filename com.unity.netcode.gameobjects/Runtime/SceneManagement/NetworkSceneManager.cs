using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Unity.Netcode
{
    /// <summary>
    /// Main class for managing network scenes when <see cref="NetworkConfig.EnableSceneManagement"/> is enabled.
    /// Uses the <see cref="SceneEventMessage"/> message to communicate <see cref="SceneEventData"/> between the server and client(s)
    /// </summary>
    public partial class NetworkSceneManager : IDisposable
    {
        private const NetworkDelivery k_DeliveryType = NetworkDelivery.ReliableFragmentedSequenced;

        /// <summary>
        /// Scene reference to whichever scene was requested as loaded in single mode
        /// </summary>
        public Scene SingleScene;

        /// <summary>
        /// Used to detect if a scene event is underway
        /// Only 1 scene event can occur on the server at a time for now.
        /// </summary>
        private bool m_IsSceneEventActive = false;

        /// <summary>
        /// The delegate callback definition for scene event notifications.<br/>
        /// See also: <br/>
        /// <seealso cref="SceneEvent"/><br/>
        /// <seealso cref="SceneEventData"/>
        /// </summary>
        /// <param name="sceneEvent"></param>
        public delegate void SceneEventDelegate(SceneEvent sceneEvent);

        /// <summary>
        /// Subscribe to this event to receive all <see cref="SceneEventType"/> notifications.<br/>
        /// For more details review over <see cref="SceneEvent"/> and <see cref="SceneEventType"/>.<br/>
        /// <b>Alternate Single Event Type Notification Registration Options</b><br/>
        /// To receive only a specific event type notification or a limited set of notifications you can alternately subscribe to
        /// each notification type individually via the following events:<br/>
        /// <list type="bullet">
        /// <item><term><see cref="OnLoad"/> Invoked only when a <see cref="SceneEventType.Load"/> event is being processed</term></item>
        /// <item><term><see cref="OnUnload"/> Invoked only when an <see cref="SceneEventType.Unload"/> event is being processed</term></item>
        /// <item><term><see cref="OnSynchronize"/> Invoked only when a <see cref="SceneEventType.Synchronize"/> event is being processed</term></item>
        /// <item><term><see cref="OnLoadEventCompleted"/> Invoked only when a <see cref="SceneEventType.LoadEventCompleted"/> event is being processed</term></item>
        /// <item><term><see cref="OnUnloadEventCompleted"/> Invoked only when an <see cref="SceneEventType.UnloadEventCompleted"/> event is being processed</term></item>
        /// <item><term><see cref="OnLoadComplete"/> Invoked only when a <see cref="SceneEventType.LoadComplete"/> event is being processed</term></item>
        /// <item><term><see cref="OnUnloadComplete"/> Invoked only when an <see cref="SceneEventType.UnloadComplete"/> event is being processed</term></item>
        /// <item><term><see cref="OnSynchronizeComplete"/> Invoked only when a <see cref="SceneEventType.SynchronizeComplete"/> event is being processed</term></item>
        /// </list>
        /// <em>Note: Do not start new scene events within NetworkSceneManager scene event notification callbacks.</em><br/>
        /// </summary>
        public event SceneEventDelegate OnSceneEvent;

        /// <summary>
        /// Delegate declaration for the OnLoad event.<br/>
        /// See also: <br/>
        /// <seealso cref="SceneEventType.Load"/>for more information
        /// </summary>
        /// <param name="clientId">the client that is processing this event (the server will receive all of these events for every client and itself)</param>
        /// <param name="sceneName">name of the scene being processed</param>
        /// <param name="loadSceneMode">the LoadSceneMode mode for the scene being loaded</param>
        /// <param name="asyncOperation">the associated <see cref="AsyncOperation"/> that can be used for scene loading progress</param>
        public delegate void OnLoadDelegateHandler(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation);

        /// <summary>
        /// Delegate declaration for the OnUnload event.<br/>
        /// See also: <br/>
        /// <seealso cref="SceneEventType.Unload"/> for more information
        /// </summary>
        /// <param name="clientId">the client that is processing this event (the server will receive all of these events for every client and itself)</param>
        /// <param name="sceneName">name of the scene being processed</param>
        /// <param name="asyncOperation">the associated <see cref="AsyncOperation"/> that can be used for scene unloading progress</param>
        public delegate void OnUnloadDelegateHandler(ulong clientId, string sceneName, AsyncOperation asyncOperation);

        /// <summary>
        /// Delegate declaration for the OnSynchronize event.<br/>
        /// See also: <br/>
        /// <seealso cref="SceneEventType.Synchronize"/> for more information
        /// </summary>
        /// <param name="clientId">the client that is processing this event (the server will receive all of these events for every client and itself)</param>
        public delegate void OnSynchronizeDelegateHandler(ulong clientId);

        /// <summary>
        /// Delegate declaration for the OnLoadEventCompleted and OnUnloadEventCompleted events.<br/>
        /// See also:<br/>
        /// <seealso cref="SceneEventType.LoadEventCompleted"/><br/>
        /// <seealso cref="SceneEventType.UnloadEventCompleted"/>
        /// </summary>
        /// <param name="sceneName">scene pertaining to this event</param>
        /// <param name="loadSceneMode"><see cref="LoadSceneMode"/> of the associated event completed</param>
        /// <param name="clientsCompleted">the clients that completed the loading event</param>
        /// <param name="clientsTimedOut">the clients (if any) that timed out during the loading event</param>
        public delegate void OnEventCompletedDelegateHandler(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut);

        /// <summary>
        /// Delegate declaration for the OnLoadComplete event.<br/>
        /// See also:<br/>
        /// <seealso cref="SceneEventType.LoadComplete"/> for more information
        /// </summary>
        /// <param name="clientId">the client that is processing this event (the server will receive all of these events for every client and itself)</param>
        /// <param name="sceneName">the scene name pertaining to this event</param>
        /// <param name="loadSceneMode">the mode the scene was loaded in</param>
        public delegate void OnLoadCompleteDelegateHandler(ulong clientId, string sceneName, LoadSceneMode loadSceneMode);

        /// <summary>
        /// Delegate declaration for the OnUnloadComplete event.<br/>
        /// See also:<br/>
        /// <seealso cref="SceneEventType.UnloadComplete"/> for more information
        /// </summary>
        /// <param name="clientId">the client that is processing this event (the server will receive all of these events for every client and itself)</param>
        /// <param name="sceneName">the scene name pertaining to this event</param>
        public delegate void OnUnloadCompleteDelegateHandler(ulong clientId, string sceneName);

        /// <summary>
        /// Delegate declaration for the OnSynchronizeComplete event.<br/>
        /// See also:<br/>
        /// <seealso cref="SceneEventType.SynchronizeComplete"/> for more information
        /// </summary>
        /// <param name="clientId">the client that completed this event</param>
        public delegate void OnSynchronizeCompleteDelegateHandler(ulong clientId);

        /// <summary>
        /// Invoked when a <see cref="SceneEventType.Load"/> event is started by the server.<br/>
        /// <em>Note: The server and connected client(s) will always receive this notification.</em><br/>
        /// <em>*** Do not start new scene events within scene event notification callbacks.</em><br/>
        /// </summary>
        public event OnLoadDelegateHandler OnLoad;

        /// <summary>
        /// Invoked when a <see cref="SceneEventType.Unload"/> event is started by the server.<br/>
        /// <em>Note: The server and connected client(s) will always receive this notification.</em><br/>
        /// <em>*** Do not start new scene events within scene event notification callbacks.</em><br/>
        /// </summary>
        public event OnUnloadDelegateHandler OnUnload;

        /// <summary>
        /// Invoked when a <see cref="SceneEventType.Synchronize"/> event is started by the server
        /// after a client is approved for connection in order to synchronize the client with the currently loaded
        /// scenes and NetworkObjects.  This event signifies the beginning of the synchronization event.<br/>
        /// <em>Note: The server and connected client(s) will always receive this notification.
        /// This event is generated on a per newly connected and approved client basis.</em><br/>
        /// <em>*** Do not start new scene events within scene event notification callbacks.</em><br/>
        /// </summary>
        public event OnSynchronizeDelegateHandler OnSynchronize;

        /// <summary>
        /// Invoked when a <see cref="SceneEventType.LoadEventCompleted"/> event is generated by the server.
        /// This event signifies the end of an existing <see cref="SceneEventType.Load"/> event as it pertains
        /// to all clients connected when the event was started.  This event signifies that all clients (and server) have
        /// finished the <see cref="SceneEventType.Load"/> event.<br/>
        /// <em>Note: this is useful to know when all clients have loaded the same scene (single or additive mode)</em><br/>
        /// <em>*** Do not start new scene events within scene event notification callbacks.</em><br/>
        /// </summary>
        public event OnEventCompletedDelegateHandler OnLoadEventCompleted;

        /// <summary>
        /// Invoked when a <see cref="SceneEventType.UnloadEventCompleted"/> event is generated by the server.
        /// This event signifies the end of an existing <see cref="SceneEventType.Unload"/> event as it pertains
        /// to all clients connected when the event was started.  This event signifies that all clients (and server) have
        /// finished the <see cref="SceneEventType.Unload"/> event.<br/>
        /// <em>Note: this is useful to know when all clients have unloaded a specific scene.  The <see cref="LoadSceneMode"/> will
        /// always be <see cref="LoadSceneMode.Additive"/> for this event.</em><br/>
        /// <em>*** Do not start new scene events within scene event notification callbacks.</em><br/>
        /// </summary>
        public event OnEventCompletedDelegateHandler OnUnloadEventCompleted;

        /// <summary>
        /// Invoked when a <see cref="SceneEventType.LoadComplete"/> event is generated by a client or server.<br/>
        /// <em>Note: The server receives this message from all clients (including itself).
        /// Each client receives their own notification sent to the server.</em><br/>
        /// <em>*** Do not start new scene events within scene event notification callbacks.</em><br/>
        /// </summary>
        public event OnLoadCompleteDelegateHandler OnLoadComplete;

        /// <summary>
        /// Invoked when a <see cref="SceneEventType.UnloadComplete"/> event is generated by a client or server.<br/>
        /// <em>Note: The server receives this message from all clients (including itself).
        /// Each client receives their own notification sent to the server.</em><br/>
        /// <em>*** Do not start new scene events within scene event notification callbacks.</em><br/>
        /// </summary>
        public event OnUnloadCompleteDelegateHandler OnUnloadComplete;

        /// <summary>
        /// Invoked when a <see cref="SceneEventType.SynchronizeComplete"/> event is generated by a client. <br/>
        /// <em> Note: The server receives this message from the client, but will never generate this event for itself.
        /// Each client receives their own notification sent to the server.  This is useful to know that a client has
        /// completed the entire connection sequence, loaded all scenes, and synchronized all NetworkObjects.</em>
        /// <em>*** Do not start new scene events within scene event notification callbacks.</em><br/>
        /// </summary>
        public event OnSynchronizeCompleteDelegateHandler OnSynchronizeComplete;

        /// <summary>
        /// Delegate declaration for the <see cref="VerifySceneBeforeLoading"/> handler that provides
        /// an additional level of scene loading security and/or validation to assure the scene being loaded
        /// is valid scene to be loaded in the LoadSceneMode specified.
        /// </summary>
        /// <param name="sceneIndex">Build Settings Scenes in Build List index of the scene</param>
        /// <param name="sceneName">Name of the scene</param>
        /// <param name="loadSceneMode">LoadSceneMode the scene is going to be loaded</param>
        /// <returns>true (valid) or false (not valid)</returns>
        public delegate bool VerifySceneBeforeLoadingDelegateHandler(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode);

        /// <summary>
        /// Delegate handler defined by <see cref="VerifySceneBeforeLoadingDelegateHandler"/> that is invoked before the
        /// server or client loads a scene during an active netcode game session.<br/>
        /// <b>Client Side:</b> In order for clients to be notified of this condition you must assign the <see cref="VerifySceneBeforeLoading"/> delegate handler.<br/>
        /// <b>Server Side:</b> <see cref="LoadScene(string, LoadSceneMode)"/> will return <see cref="SceneEventProgressStatus"/>.
        /// </summary>
        public VerifySceneBeforeLoadingDelegateHandler VerifySceneBeforeLoading;

        /// <summary>
        /// The SceneManagerHandler implementation
        /// </summary>
        public ISceneManagerHandler SceneManagerHandler = new DefaultSceneManagerHandler();

        /// <summary>
        ///  The default SceneManagerHandler that interfaces between the SceneManager and NetworkSceneManager
        /// </summary>
        private class DefaultSceneManagerHandler : ISceneManagerHandler
        {
            public AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, ISceneManagerHandler.SceneEventAction sceneEventAction)
            {
                var operation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
                sceneEventAction.Scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
                operation.completed += new Action<AsyncOperation>(asyncOp2 => { sceneEventAction.Invoke(); });
                return operation;
            }

            public AsyncOperation UnloadSceneAsync(Scene scene, ISceneManagerHandler.SceneEventAction sceneEventAction)
            {
                var operation = SceneManager.UnloadSceneAsync(scene);
                operation.completed += new Action<AsyncOperation>(asyncOp2 => { sceneEventAction.Invoke(); });
                return operation;
            }
        }

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

        /// <summary>
        /// Used to track which scenes are currently loaded
        /// We store the scenes as [SceneHandle][Scene] in order to handle the loading and unloading of the same scene additively
        /// Scene handle is only unique locally.  So, clients depend upon the <see cref="ServerSceneHandleToClientSceneHandle"/> in order
        /// to be able to know which specific scene instance the server is instructing the client to unload.
        /// The client links the server scene handle to the client local scene handle upon a scene being loaded
        /// <see cref="GetAndAddNewlyLoadedSceneByName"/>
        /// </summary>
        internal Dictionary<int, Scene> ScenesLoaded = new Dictionary<int, Scene>();

        /// <summary>
        /// Since Scene.handle is unique per client, we create a look-up table between the client and server to associate server unique scene
        /// instances with client unique scene instances
        /// </summary>
        internal Dictionary<int, int> ServerSceneHandleToClientSceneHandle = new Dictionary<int, int>();

        /// <summary>
        /// Hash to build index lookup table
        /// </summary>
        internal Dictionary<uint, int> HashToBuildIndex = new Dictionary<uint, int>();

        /// <summary>
        /// Build index to hash lookup table
        /// </summary>
        internal Dictionary<int, uint> BuildIndexToHash = new Dictionary<int, uint>();

        /// <summary>
        /// The Condition: While a scene is asynchronously loaded in single loading scene mode, if any new NetworkObjects are spawned
        /// they need to be moved into the do not destroy temporary scene
        /// When it is set: Just before starting the asynchronous loading call
        /// When it is unset: After the scene has loaded, the PopulateScenePlacedObjects is called, and all NetworkObjects in the do
        /// not destroy temporary scene are moved into the active scene
        /// </summary>
        internal static bool IsSpawnedObjectsPendingInDontDestroyOnLoad;

        /// <summary>
        /// Client and Server:
        /// Used for all scene event processing
        /// </summary>
        internal Dictionary<uint, SceneEventData> SceneEventDataStore;

        private NetworkManager m_NetworkManager { get; }

        internal Scene DontDestroyOnLoadScene;

        /// <summary>
        /// When true, the <see cref="Debug.LogWarning(object)"/> messages will be turned off
        /// </summary>
        private bool m_DisableValidationWarningMessages;

        /// <summary>
        /// Handle NetworkSeneManager clean up
        /// </summary>
        public void Dispose()
        {
            SceneUnloadEventHandler.Shutdown();

            foreach (var keypair in SceneEventDataStore)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogInfo($"{nameof(SceneEventDataStore)} is disposing {nameof(SceneEventData.SceneEventId)} '{keypair.Key}'.");
                }
                keypair.Value.Dispose();
            }
            SceneEventDataStore.Clear();
            SceneEventDataStore = null;
        }

        /// <summary>
        /// Creates a new SceneEventData object for a new scene event
        /// </summary>
        /// <returns>SceneEventData instance</returns>
        internal SceneEventData BeginSceneEvent()
        {
            var sceneEventData = new SceneEventData(m_NetworkManager);
            SceneEventDataStore.Add(sceneEventData.SceneEventId, sceneEventData);
            return sceneEventData;
        }

        /// <summary>
        /// Disposes and removes SceneEventData object for the scene event
        /// </summary>
        /// <param name="sceneEventId">SceneEventId to end</param>
        internal void EndSceneEvent(uint sceneEventId)
        {
            if (SceneEventDataStore.ContainsKey(sceneEventId))
            {
                SceneEventDataStore[sceneEventId].Dispose();
                SceneEventDataStore.Remove(sceneEventId);
            }
            else
            {
                Debug.LogWarning($"Trying to dispose and remove SceneEventData Id '{sceneEventId}' that no longer exists!");
            }
        }

        /// <summary>
        /// Gets the scene name from full path to the scene
        /// </summary>
        internal string GetSceneNameFromPath(string scenePath)
        {
            var begin = scenePath.LastIndexOf("/", StringComparison.Ordinal) + 1;
            var end = scenePath.LastIndexOf(".", StringComparison.Ordinal);
            return scenePath.Substring(begin, end - begin);
        }

        /// <summary>
        /// Generates the hash values and associated tables
        /// for the scenes in build list
        /// </summary>
        internal void GenerateScenesInBuild()
        {
            HashToBuildIndex.Clear();
            BuildIndexToHash.Clear();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                var hash = XXHash.Hash32(scenePath);
                var buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);

                // In the rare-case scenario where a programmatically generated build has duplicate
                // scene entries, we will log an error and skip the entry
                if (!HashToBuildIndex.ContainsKey(hash))
                {
                    HashToBuildIndex.Add(hash, buildIndex);
                    BuildIndexToHash.Add(buildIndex, hash);
                }
                else
                {
                    Debug.LogError($"{nameof(NetworkSceneManager)} is skipping duplicate scene path entry {scenePath}. Make sure your scenes in build list does not contain duplicates!");
                }
            }
        }

        /// <summary>
        /// Gets the scene name from a hash value generated from the full scene path
        /// </summary>
        internal string SceneNameFromHash(uint sceneHash)
        {
            // In the event there is no scene associated with the scene event then just return "No Scene"
            // This can happen during unit tests when clients first connect and the only scene loaded is the
            // unit test scene (which is ignored by default) that results in a scene event that has no associated
            // scene.  Under this specific special case, we just return "No Scene".
            if (sceneHash == 0)
            {
                return "No Scene";
            }
            return GetSceneNameFromPath(ScenePathFromHash(sceneHash));
        }

        /// <summary>
        /// Gets the full scene path from a hash value
        /// </summary>
        internal string ScenePathFromHash(uint sceneHash)
        {
            if (HashToBuildIndex.ContainsKey(sceneHash))
            {
                return SceneUtility.GetScenePathByBuildIndex(HashToBuildIndex[sceneHash]);
            }
            else
            {
                throw new Exception($"Scene Hash {sceneHash} does not exist in the {nameof(HashToBuildIndex)} table!  Verify that all scenes requiring" +
                    $" server to client synchronization are in the scenes in build list.");
            }
        }

        /// <summary>
        /// When set to true, this will disable the console warnings about
        /// a scene being invalidated.
        /// </summary>
        /// <param name="disabled">true/false</param>
        public void DisableValidationWarnings(bool disabled)
        {
            m_DisableValidationWarningMessages = disabled;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="networkManager">one <see cref="NetworkManager"/> instance per <see cref="NetworkSceneManager"/> instance</param>
        /// <param name="sceneEventDataPoolSize">maximum <see cref="SceneEventData"/> pool size</param>
        internal NetworkSceneManager(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            SceneEventDataStore = new Dictionary<uint, SceneEventData>();

            GenerateScenesInBuild();

            // Since NetworkManager is now always migrated to the DDOL we will use this to get the DDOL scene
            DontDestroyOnLoadScene = networkManager.gameObject.scene;

            ServerSceneHandleToClientSceneHandle.Add(DontDestroyOnLoadScene.handle, DontDestroyOnLoadScene.handle);
            ScenesLoaded.Add(DontDestroyOnLoadScene.handle, DontDestroyOnLoadScene);
        }

        /// <summary>
        /// If the VerifySceneBeforeLoading delegate handler has been set by the user, this will provide
        /// an additional level of security and/or validation that the scene being loaded in the specified
        /// loading mode is "a valid scene to be loaded in the LoadSceneMode specified".
        /// </summary>
        /// <param name="sceneIndex">index into ScenesInBuild</param>
        /// <param name="loadSceneMode">LoadSceneMode the scene is going to be loaded</param>
        /// <returns>true (Valid) or false (Invalid)</returns>
        internal bool ValidateSceneBeforeLoading(uint sceneHash, LoadSceneMode loadSceneMode)
        {
            var validated = true;
            var sceneName = SceneNameFromHash(sceneHash);
            var sceneIndex = SceneUtility.GetBuildIndexByScenePath(sceneName);
            if (VerifySceneBeforeLoading != null)
            {
                validated = VerifySceneBeforeLoading.Invoke((int)sceneIndex, sceneName, loadSceneMode);
            }
            if (!validated && !m_DisableValidationWarningMessages)
            {
                var serverHostorClient = "Client";
                if (m_NetworkManager.IsServer)
                {
                    serverHostorClient = m_NetworkManager.IsHost ? "Host" : "Server";
                }

                Debug.LogWarning($"Scene {sceneName} of Scenes in Build Index {sceneIndex} being loaded in {loadSceneMode} mode failed validation on the {serverHostorClient}!");
            }
            return validated;
        }

        /// <summary>
        /// During soft synchronization of in-scene placed NetworkObjects, this is now used by NetworkSpawnManager.CreateLocalNetworkObject
        /// </summary>
        /// <param name="globalObjectIdHash"></param>
        /// <returns></returns>
        internal NetworkObject GetSceneRelativeInSceneNetworkObject(uint globalObjectIdHash, int? networkSceneHandle)
        {
            if (ScenePlacedObjects.ContainsKey(globalObjectIdHash))
            {
                var sceneHandle = SceneBeingSynchronized.handle;
                if (networkSceneHandle.HasValue && networkSceneHandle.Value != 0)
                {
                    sceneHandle = ServerSceneHandleToClientSceneHandle[networkSceneHandle.Value];
                }
                if (ScenePlacedObjects[globalObjectIdHash].ContainsKey(sceneHandle))
                {
                    return ScenePlacedObjects[globalObjectIdHash][sceneHandle];
                }
            }
            return null;
        }

        /// <summary>
        /// Generic sending of scene event data
        /// </summary>
        /// <param name="targetClientIds">array of client identifiers to receive the scene event message</param>
        private void SendSceneEventData(uint sceneEventId, ulong[] targetClientIds)
        {
            if (targetClientIds.Length == 0)
            {
                // This would be the Host/Server with no clients connected
                // Silently return as there is nothing to be done
                return;
            }
            var message = new SceneEventMessage
            {
                EventData = SceneEventDataStore[sceneEventId]
            };
            var size = m_NetworkManager.SendMessage(ref message, k_DeliveryType, targetClientIds);

            m_NetworkManager.NetworkMetrics.TrackSceneEventSent(targetClientIds, (uint)SceneEventDataStore[sceneEventId].SceneEventType, SceneNameFromHash(SceneEventDataStore[sceneEventId].SceneHash), size);
        }

        /// <summary>
        /// Server and Client:
        /// Invoked when an additively loaded scene is unloaded
        /// </summary>
        private void OnSceneUnloaded(uint sceneEventId, Scene _)
        {
            // If we are shutdown or about to shutdown, then ignore this event
            if (!m_NetworkManager.IsListening || m_NetworkManager.ShutdownInProgress)
            {
                return;
            }

            var sceneEventData = SceneEventDataStore[sceneEventId];
            // First thing we do, if we are a server, is to send the unload scene event.
            if (m_NetworkManager.IsServer)
            {
                // Server sends the unload scene notification after unloading because it will despawn all scene relative in-scene NetworkObjects
                // If we send this event to all clients before the server is finished unloading they will get warning about an object being
                // despawned that no longer exists
                SendSceneEventData(sceneEventId, m_NetworkManager.ConnectedClientsIds.Where(c => c != NetworkManager.ServerClientId).ToArray());

                //Only if we are a host do we want register having loaded for the associated SceneEventProgress
                if (SceneEventProgressTracking.ContainsKey(sceneEventData.SceneEventProgressId) && m_NetworkManager.IsHost)
                {
                    SceneEventProgressTracking[sceneEventData.SceneEventProgressId].AddClientAsDone(NetworkManager.ServerClientId);
                }
            }

            // Next we prepare to send local notifications for unload complete
            sceneEventData.SceneEventType = SceneEventType.UnloadComplete;

            //Notify the client or server that a scene was unloaded
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                SceneEventType = sceneEventData.SceneEventType,
                LoadSceneMode = sceneEventData.LoadSceneMode,
                SceneName = SceneNameFromHash(sceneEventData.SceneHash),
                ClientId = m_NetworkManager.IsServer ? NetworkManager.ServerClientId : m_NetworkManager.LocalClientId
            });

            OnUnloadComplete?.Invoke(m_NetworkManager.LocalClientId, SceneNameFromHash(sceneEventData.SceneHash));

            // Clients send a notification back to the server they have completed the unload scene event
            if (!m_NetworkManager.IsServer)
            {
                SendSceneEventData(sceneEventId, new ulong[] { NetworkManager.ServerClientId });
            }

            EndSceneEvent(sceneEventId);
            // This scene event is now considered "complete"
            m_IsSceneEventActive = false;
        }

        private void EmptySceneUnloadedOperation(uint sceneEventId, Scene _)
        {
            // Do nothing (this is a stub call since it is only used to flush all additively loaded scenes)
        }

        /// <summary>
        /// Clears all scenes when loading in single mode
        /// Since we assume a single mode loaded scene will be considered the "currently active scene",
        /// we only unload any additively loaded scenes.
        /// </summary>
        internal void UnloadAdditivelyLoadedScenes(uint sceneEventId)
        {
            var sceneEventData = SceneEventDataStore[sceneEventId];
            // Unload all additive scenes while making sure we don't try to unload the base scene ( loaded in single mode ).
            var currentActiveScene = SceneManager.GetActiveScene();
            foreach (var keyHandleEntry in ScenesLoaded)
            {
                // Validate the scene as well as ignore the DDOL (which will have a negative buildIndex)
                if (currentActiveScene.name != keyHandleEntry.Value.name && keyHandleEntry.Value.buildIndex >= 0)
                {
                    var sceneUnload = SceneManagerHandler.UnloadSceneAsync(keyHandleEntry.Value,
                        new ISceneManagerHandler.SceneEventAction() { SceneEventId = sceneEventId, EventAction = EmptySceneUnloadedOperation });
                    SceneUnloadEventHandler.RegisterScene(this, keyHandleEntry.Value, LoadSceneMode.Additive, sceneUnload);
                }
            }
            // clear out our scenes loaded list
            ScenesLoaded.Clear();
        }

        /// <summary>
        /// Helper class used to handle "odd ball" scene unload event notification scenarios
        /// when scene switching.
        /// </summary>
        internal class SceneUnloadEventHandler
        {
            private static Dictionary<NetworkManager, List<SceneUnloadEventHandler>> s_Instances = new Dictionary<NetworkManager, List<SceneUnloadEventHandler>>();

            internal static void RegisterScene(NetworkSceneManager networkSceneManager, Scene scene, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation = null)
            {
                var networkManager = networkSceneManager.m_NetworkManager;
                if (!s_Instances.ContainsKey(networkManager))
                {
                    s_Instances.Add(networkManager, new List<SceneUnloadEventHandler>());
                }
                var clientId = networkManager.IsServer ? NetworkManager.ServerClientId : networkManager.LocalClientId;
                s_Instances[networkManager].Add(new SceneUnloadEventHandler(networkSceneManager, scene, clientId, loadSceneMode, asyncOperation));
            }

            private static void SceneUnloadComplete(SceneUnloadEventHandler sceneUnloadEventHandler)
            {
                if (sceneUnloadEventHandler == null || sceneUnloadEventHandler.m_NetworkSceneManager == null || sceneUnloadEventHandler.m_NetworkSceneManager.m_NetworkManager == null)
                {
                    return;
                }
                var networkManager = sceneUnloadEventHandler.m_NetworkSceneManager.m_NetworkManager;
                if (s_Instances.ContainsKey(networkManager))
                {
                    s_Instances[networkManager].Remove(sceneUnloadEventHandler);
                    if (s_Instances[networkManager].Count == 0)
                    {
                        s_Instances.Remove(networkManager);
                    }
                }
            }

            /// <summary>
            /// Called by NetworkSceneManager when it is disposing
            /// </summary>
            internal static void Shutdown()
            {
                foreach (var instanceEntry in s_Instances)
                {
                    foreach (var instance in instanceEntry.Value)
                    {
                        instance.OnShutdown();
                    }
                    instanceEntry.Value.Clear();
                }
                s_Instances.Clear();
            }

            private NetworkSceneManager m_NetworkSceneManager;
            private AsyncOperation m_AsyncOperation;
            private LoadSceneMode m_LoadSceneMode;
            private ulong m_ClientId;
            private Scene m_Scene;
            private bool m_ShuttingDown;

            private void OnShutdown()
            {
                m_ShuttingDown = true;
                SceneManager.sceneUnloaded -= SceneUnloaded;
            }

            private void SceneUnloaded(Scene scene)
            {
                if (m_Scene.handle == scene.handle && !m_ShuttingDown)
                {
                    if (m_NetworkSceneManager != null && m_NetworkSceneManager.m_NetworkManager != null)
                    {
                        m_NetworkSceneManager.OnSceneEvent?.Invoke(new SceneEvent()
                        {
                            AsyncOperation = m_AsyncOperation,
                            SceneEventType = SceneEventType.UnloadComplete,
                            SceneName = m_Scene.name,
                            LoadSceneMode = m_LoadSceneMode,
                            ClientId = m_ClientId
                        });
                        m_NetworkSceneManager.OnUnloadComplete?.Invoke(m_ClientId, m_Scene.name);
                    }
                    SceneManager.sceneUnloaded -= SceneUnloaded;
                    SceneUnloadComplete(this);
                }
            }

            private SceneUnloadEventHandler(NetworkSceneManager networkSceneManager, Scene scene, ulong clientId, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation = null)
            {
                m_LoadSceneMode = loadSceneMode;
                m_AsyncOperation = asyncOperation;
                m_NetworkSceneManager = networkSceneManager;
                m_ClientId = clientId;
                m_Scene = scene;
                SceneManager.sceneUnloaded += SceneUnloaded;
                // Send the initial unload event notification
                m_NetworkSceneManager.OnSceneEvent?.Invoke(new SceneEvent()
                {
                    AsyncOperation = m_AsyncOperation,
                    SceneEventType = SceneEventType.Unload,
                    SceneName = m_Scene.name,
                    LoadSceneMode = m_LoadSceneMode,
                    ClientId = clientId
                });

                m_NetworkSceneManager.OnUnload?.Invoke(networkSceneManager.m_NetworkManager.LocalClientId, m_Scene.name, null);
            }
        }

        /// <summary>
        /// Client and Server:
        /// Generic on scene loaded callback method to be called upon a scene loading
        /// </summary>
        private void OnSceneLoaded(uint sceneEventId, Scene scene)
        {
            // If we are shutdown or about to shutdown, then ignore this event
            if (!m_NetworkManager.IsListening || m_NetworkManager.ShutdownInProgress)
            {
                return;
            }

            var sceneEventData = SceneEventDataStore[sceneEventId];
            var nextScene = scene; // GetAndAddNewlyLoadedSceneByName(SceneNameFromHash(sceneEventData.SceneHash));
            if (!nextScene.isLoaded || !nextScene.IsValid())
            {
                throw new Exception($"Failed to find valid scene internal Unity.Netcode for {nameof(GameObject)}s error!");
            }

            ScenesLoaded.Add(nextScene.handle, nextScene);

            if (sceneEventData.LoadSceneMode == LoadSceneMode.Single)
            {
                SingleScene = nextScene;
                SceneManager.SetActiveScene(nextScene);
            }

            //Get all NetworkObjects loaded by the scene
            PopulateScenePlacedObjects(nextScene);

            if (sceneEventData.LoadSceneMode == LoadSceneMode.Single)
            {
                // Move all objects to the new scene
                MoveObjectsFromDontDestroyOnLoadToScene(nextScene);
            }

            // The Condition: While a scene is asynchronously loaded in single loading scene mode, if any new NetworkObjects are spawned
            // they need to be moved into the do not destroy temporary scene
            // When it is set: Just before starting the asynchronous loading call
            // When it is unset: After the scene has loaded, the PopulateScenePlacedObjects is called, and all NetworkObjects in the do
            // not destroy temporary scene are moved into the active scene
            IsSpawnedObjectsPendingInDontDestroyOnLoad = false;

            if (m_NetworkManager.IsServer)
            {
                OnServerLoadedScene(sceneEventId, nextScene);
            }
            else
            {
                // For the client, we make a server scene handle to client scene handle look up table
                if (!ServerSceneHandleToClientSceneHandle.ContainsKey(sceneEventData.SceneHandle))
                {
                    ServerSceneHandleToClientSceneHandle.Add(sceneEventData.SceneHandle, nextScene.handle);
                }
                else
                {
                    // If the exact same handle exists then there are problems with using handles
                    throw new Exception($"Server Scene Handle ({sceneEventData.SceneHandle}) already exist!  Happened during scene load of {nextScene.name} with Client Handle ({nextScene.handle})");
                }

                OnClientLoadedScene(sceneEventId, nextScene);
            }
        }

        /// <summary>
        /// Used for integration testing, due to the complexities of having all clients loading scenes
        /// this is needed to "filter" out the scenes not loaded by NetworkSceneManager
        /// (i.e. we don't want a late joining player to load all of the other client scenes)
        /// </summary>
        internal Func<Scene, bool> ExcludeSceneFromSychronization;

        /// <summary>
        /// Server Side:
        /// Handles incoming Scene_Event messages for host or server
        /// </summary>
        private void HandleServerSceneEvent(uint sceneEventId, ulong clientId)
        {
            var sceneEventData = SceneEventDataStore[sceneEventId];
            switch (sceneEventData.SceneEventType)
            {
                case SceneEventType.LoadComplete:
                    {
                        // Notify the local server that the client has finished loading a scene
                        OnSceneEvent?.Invoke(new SceneEvent()
                        {
                            SceneEventType = sceneEventData.SceneEventType,
                            LoadSceneMode = sceneEventData.LoadSceneMode,
                            SceneName = SceneNameFromHash(sceneEventData.SceneHash),
                            ClientId = clientId
                        });

                        OnLoadComplete?.Invoke(clientId, SceneNameFromHash(sceneEventData.SceneHash), sceneEventData.LoadSceneMode);

                        if (SceneEventProgressTracking.ContainsKey(sceneEventData.SceneEventProgressId))
                        {
                            SceneEventProgressTracking[sceneEventData.SceneEventProgressId].AddClientAsDone(clientId);
                        }
                        EndSceneEvent(sceneEventId);
                        break;
                    }
                case SceneEventType.UnloadComplete:
                    {
                        if (SceneEventProgressTracking.ContainsKey(sceneEventData.SceneEventProgressId))
                        {
                            SceneEventProgressTracking[sceneEventData.SceneEventProgressId].AddClientAsDone(clientId);
                        }
                        // Notify the local server that the client has finished unloading a scene
                        OnSceneEvent?.Invoke(new SceneEvent()
                        {
                            SceneEventType = sceneEventData.SceneEventType,
                            LoadSceneMode = sceneEventData.LoadSceneMode,
                            SceneName = SceneNameFromHash(sceneEventData.SceneHash),
                            ClientId = clientId
                        });

                        OnUnloadComplete?.Invoke(clientId, SceneNameFromHash(sceneEventData.SceneHash));

                        EndSceneEvent(sceneEventId);
                        break;
                    }
                case SceneEventType.SynchronizeComplete:
                    {
                        // Notify the local server that a client has finished synchronizing
                        OnSceneEvent?.Invoke(new SceneEvent()
                        {
                            SceneEventType = sceneEventData.SceneEventType,
                            SceneName = string.Empty,
                            ClientId = clientId
                        });

                        OnSynchronizeComplete?.Invoke(clientId);

                        // We now can call the client connected callback on the server at this time
                        // This assures the client is fully synchronized with all loaded scenes and
                        // NetworkObjects
                        m_NetworkManager.InvokeOnClientConnectedCallback(clientId);

                        // Check to see if the client needs to resynchronize and before sending the message make sure the client is still connected to avoid
                        // a potential crash within the MessageSystem (i.e. sending to a client that no longer exists)
                        if (sceneEventData.ClientNeedsReSynchronization() && !DisableReSynchronization && m_NetworkManager.ConnectedClients.ContainsKey(clientId))
                        {
                            sceneEventData.SceneEventType = SceneEventType.ReSynchronize;
                            SendSceneEventData(sceneEventId, new ulong[] { clientId });

                            OnSceneEvent?.Invoke(new SceneEvent()
                            {
                                SceneEventType = sceneEventData.SceneEventType,
                                SceneName = string.Empty,
                                ClientId = clientId
                            });
                        }
                        EndSceneEvent(sceneEventId);
                        break;
                    }
                default:
                    {
                        Debug.LogWarning($"{sceneEventData.SceneEventType} is not currently supported!");
                        break;
                    }
            }
        }

        /// <summary>
        /// Moves all NetworkObjects that don't have the <see cref="NetworkObject.DestroyWithScene"/> set to
        /// the "Do not destroy on load" scene.
        /// </summary>
        internal void MoveObjectsToDontDestroyOnLoad()
        {
            // Move ALL NetworkObjects marked to persist scene transitions into the DDOL scene
            var objectsToKeep = new HashSet<NetworkObject>(m_NetworkManager.SpawnManager.SpawnedObjectsList);
            foreach (var sobj in objectsToKeep)
            {
                if (sobj == null)
                {
                    continue;
                }

                if (!sobj.DestroyWithScene || sobj.gameObject.scene == DontDestroyOnLoadScene)
                {
                    // Only move dynamically spawned network objects with no parent as child objects will follow
                    if (sobj.gameObject.transform.parent == null && sobj.IsSceneObject != null && !sobj.IsSceneObject.Value)
                    {
                        UnityEngine.Object.DontDestroyOnLoad(sobj.gameObject);
                    }
                }
                else if (m_NetworkManager.IsServer)
                {
                    sobj.Despawn();
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
        internal void PopulateScenePlacedObjects(Scene sceneToFilterBy, bool clearScenePlacedObjects = true)
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
                var globalObjectIdHash = networkObjectInstance.GlobalObjectIdHash;
                var sceneHandle = networkObjectInstance.GetSceneOriginHandle();
                // We check to make sure the NetworkManager instance is the same one to be "NetcodeIntegrationTestHelpers" compatible and filter the list on a per scene basis (for additive scenes)
                if (networkObjectInstance.IsSceneObject != false && (networkObjectInstance.NetworkManager == null || networkObjectInstance.NetworkManager == m_NetworkManager) && sceneHandle == sceneToFilterBy.handle)
                {
                    if (!ScenePlacedObjects.ContainsKey(globalObjectIdHash))
                    {
                        ScenePlacedObjects.Add(globalObjectIdHash, new Dictionary<int, NetworkObject>());
                    }

                    if (!ScenePlacedObjects[globalObjectIdHash].ContainsKey(sceneHandle))
                    {
                        ScenePlacedObjects[globalObjectIdHash].Add(sceneHandle, networkObjectInstance);
                    }
                    else
                    {
                        var exitingEntryName = ScenePlacedObjects[globalObjectIdHash][sceneHandle] != null ? ScenePlacedObjects[globalObjectIdHash][sceneHandle].name : "Null Entry";
                        throw new Exception($"{networkObjectInstance.name} tried to registered with {nameof(ScenePlacedObjects)} which already contains " +
                            $"the same {nameof(NetworkObject.GlobalObjectIdHash)} value {globalObjectIdHash} for {exitingEntryName}!");
                    }
                }
            }
        }

        /// <summary>
        /// Moves all spawned NetworkObjects (from do not destroy on load) to the scene specified
        /// </summary>
        /// <param name="scene">scene to move the NetworkObjects to</param>
        internal void MoveObjectsFromDontDestroyOnLoadToScene(Scene scene)
        {
            // Move ALL NetworkObjects to the temp scene
            var objectsToKeep = m_NetworkManager.SpawnManager.SpawnedObjectsList;

            foreach (var sobj in objectsToKeep)
            {
                if (sobj == null)
                {
                    continue;
                }
                // If it is in the DDOL then
                if (sobj.gameObject.scene == DontDestroyOnLoadScene)
                {
                    // only move dynamically spawned network objects, with no parent as child objects will follow,
                    // back into the currently active scene
                    if (sobj.gameObject.transform.parent == null && sobj.IsSceneObject != null && !sobj.IsSceneObject.Value)
                    {
                        SceneManager.MoveGameObjectToScene(sobj.gameObject, scene);
                    }
                }
            }
        }
    }
}
