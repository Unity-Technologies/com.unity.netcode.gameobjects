using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Unity.Netcode
{
    /// <summary>
    /// Used for local notifications of various scene events.  The <see cref="NetworkSceneManager.OnSceneEvent"/> of
    /// delegate type <see cref="NetworkSceneManager.SceneEventDelegate"/> uses this class to provide
    /// scene event status.<br/>
    /// <em>Note: This is only when <see cref="NetworkConfig.EnableSceneManagement"/> is enabled.</em><br/>
    /// <em>*** Do not start new scene events within scene event notification callbacks.</em><br/>
    /// See also: <br/>
    /// <seealso cref="SceneEventType"/>
    /// </summary>
    public class SceneEvent
    {
        /// <summary>
        /// The <see cref="UnityEngine.AsyncOperation"/> returned by <see cref="SceneManager"/><BR/>
        /// This is set for the following <see cref="Netcode.SceneEventType"/>s:
        /// <list type="bullet">
        /// <item><term><see cref="SceneEventType.Load"/></term></item>
        /// <item><term><see cref="SceneEventType.Unload"/></term></item>
        /// </list>
        /// </summary>
        public AsyncOperation AsyncOperation;

        /// <summary>
        /// Will always be set to the current <see cref="Netcode.SceneEventType"/>
        /// </summary>
        public SceneEventType SceneEventType;

        /// <summary>
        /// If applicable, this reflects the type of scene loading or unloading that is occurring.<BR/>
        /// This is set for the following <see cref="Netcode.SceneEventType"/>s:
        /// <list type="bullet">
        /// <item><term><see cref="SceneEventType.Load"/></term></item>
        /// <item><term><see cref="SceneEventType.Unload"/></term></item>
        /// <item><term><see cref="SceneEventType.LoadComplete"/></term></item>
        /// <item><term><see cref="SceneEventType.UnloadComplete"/></term></item>
        /// <item><term><see cref="SceneEventType.LoadEventCompleted"/></term></item>
        /// <item><term><see cref="SceneEventType.UnloadEventCompleted"/></term></item>
        /// </list>
        /// </summary>
        public LoadSceneMode LoadSceneMode;

        /// <summary>
        /// This will be set to the scene name that the event pertains to.<BR/>
        /// This is set for the following <see cref="Netcode.SceneEventType"/>s:
        /// <list type="bullet">
        /// <item><term><see cref="SceneEventType.Load"/></term></item>
        /// <item><term><see cref="SceneEventType.Unload"/></term></item>
        /// <item><term><see cref="SceneEventType.LoadComplete"/></term></item>
        /// <item><term><see cref="SceneEventType.UnloadComplete"/></term></item>
        /// <item><term><see cref="SceneEventType.LoadEventCompleted"/></term></item>
        /// <item><term><see cref="SceneEventType.UnloadEventCompleted"/></term></item>
        /// </list>
        /// </summary>
        public string SceneName;

        /// <summary>
        /// When a scene is loaded, the Scene structure is returned.<BR/>
        /// This is set for the following <see cref="Netcode.SceneEventType"/>s:
        /// <list type="bullet">
        /// <item><term><see cref="SceneEventType.LoadComplete"/></term></item>
        /// </list>
        /// </summary>
        public Scene Scene;

        /// <summary>
        /// The client identifier can vary depending upon the following conditions: <br/>
        /// <list type="number">
        /// <item><term><see cref="Netcode.SceneEventType"/>s that always set the <see cref="ClientId"/>
        /// to the local client identifier, are initiated (and processed locally) by the
        /// server-host, and sent to all clients to be processed.<br/>
        /// <list type="bullet">
        /// <item><term><see cref="SceneEventType.Load"/></term></item>
        /// <item><term><see cref="SceneEventType.Unload"/></term></item>
        /// <item><term><see cref="SceneEventType.Synchronize"/></term></item>
        /// <item><term><see cref="SceneEventType.ReSynchronize"/></term></item>
        /// </list>
        /// </term></item>
        /// <item><term>Events that always set the <see cref="ClientId"/> to the local client identifier,
        /// are initiated (and processed locally) by a client or server-host, and if initiated
        /// by a client will always be sent to and processed on the server-host:
        /// <list type="bullet">
        /// <item><term><see cref="SceneEventType.LoadComplete"/></term></item>
        /// <item><term><see cref="SceneEventType.UnloadComplete"/></term></item>
        /// <item><term><see cref="SceneEventType.SynchronizeComplete"/></term></item>
        /// </list>
        /// </term></item>
        /// <item><term>
        /// Events that always set the <see cref="ClientId"/> to the ServerId:
        /// <list type="bullet">
        /// <item><term><see cref="SceneEventType.LoadEventCompleted"/></term></item>
        /// <item><term><see cref="SceneEventType.UnloadEventCompleted"/></term></item>
        /// </list>
        /// </term></item>
        /// </list>
        /// </summary>
        public ulong ClientId;

        /// <summary>
        /// List of clients that completed a loading or unloading event.<br/>
        /// This is set for the following <see cref="Netcode.SceneEventType"/>s:
        /// <list type="bullet">
        /// <item><term><see cref="SceneEventType.LoadEventCompleted"/></term></item>
        /// <item><term><see cref="SceneEventType.UnloadEventCompleted"/></term></item>
        /// </list>
        /// </summary>
        public List<ulong> ClientsThatCompleted;

        /// <summary>
        /// List of clients that timed out during a loading or unloading event.<br/>
        /// This is set for the following <see cref="Netcode.SceneEventType"/>s:
        /// <list type="bullet">
        /// <item><term><see cref="SceneEventType.LoadEventCompleted"/></term></item>
        /// <item><term><see cref="SceneEventType.UnloadEventCompleted"/></term></item>
        /// </list>
        /// </summary>
        public List<ulong> ClientsThatTimedOut;
    }

    /// <summary>
    /// Main class for managing network scenes when <see cref="NetworkConfig.EnableSceneManagement"/> is enabled.
    /// Uses the <see cref="SceneEventMessage"/> message to communicate <see cref="SceneEventData"/> between the server and client(s)
    /// </summary>
    public class NetworkSceneManager : IDisposable
    {
        private const NetworkDelivery k_DeliveryType = NetworkDelivery.ReliableFragmentedSequenced;
        internal const int InvalidSceneNameOrPath = -1;

        // Used to be able to turn re-synchronization off
        internal static bool DisableReSynchronization;

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
        internal ISceneManagerHandler SceneManagerHandler = new DefaultSceneManagerHandler();

        /// <summary>
        ///  The default SceneManagerHandler that interfaces between the SceneManager and NetworkSceneManager
        /// </summary>
        private class DefaultSceneManagerHandler : ISceneManagerHandler
        {
            public AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, SceneEventProgress sceneEventProgress)
            {
                var operation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
                sceneEventProgress.SetAsyncOperation(operation);
                return operation;
            }

            public AsyncOperation UnloadSceneAsync(Scene scene, SceneEventProgress sceneEventProgress)
            {
                var operation = SceneManager.UnloadSceneAsync(scene);
                sceneEventProgress.SetAsyncOperation(operation);
                return operation;
            }
        }

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
        /// <b>LoadSceneMode.Single:</b> All currently loaded scenes on the client will be unloaded and
        /// the server's currently active scene will be loaded in single mode on the client
        /// unless it was already loaded.<br/>
        /// <b>LoadSceneMode.Additive:</b> All currently loaded scenes are left as they are and any newly loaded
        /// scenes will be loaded additively.  Users need to determine which scenes are valid to load via the
        /// <see cref="VerifySceneBeforeLoading"/> method.
        /// </summary>
        public LoadSceneMode ClientSynchronizationMode { get; internal set; }

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
        /// Gets the associated hash value for the scene name or path
        /// </summary>
        internal uint SceneHashFromNameOrPath(string sceneNameOrPath)
        {
            var buildIndex = SceneUtility.GetBuildIndexByScenePath(sceneNameOrPath);
            if (buildIndex >= 0)
            {
                if (BuildIndexToHash.ContainsKey(buildIndex))
                {
                    return BuildIndexToHash[buildIndex];
                }
                else
                {
                    throw new Exception($"Scene '{sceneNameOrPath}' has a build index of {buildIndex} that does not exist in the {nameof(BuildIndexToHash)} table!");
                }
            }
            else
            {
                throw new Exception($"Scene '{sceneNameOrPath}' couldn't be loaded because it has not been added to the build settings scenes in build list.");
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
        /// This will change how clients are initially synchronized.<br/>
        /// <b>LoadSceneMode.Single:</b> All currently loaded scenes on the client will be unloaded and
        /// the server's currently active scene will be loaded in single mode on the client
        /// unless it was already loaded. <br/>
        /// <b>LoadSceneMode.Additive:</b> All currently loaded scenes are left as they are and any newly loaded
        /// scenes will be loaded additively.  Users need to determine which scenes are valid to load via the
        /// <see cref="VerifySceneBeforeLoading"/> method.
        /// </summary>
        /// <param name="mode"><see cref="LoadSceneMode"/> for initial client synchronization</param>
        public void SetClientSynchronizationMode(LoadSceneMode mode)
        {
            ClientSynchronizationMode = mode;
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
        /// Used for NetcodeIntegrationTest testing in order to properly
        /// assign the right loaded scene to the right client's ScenesLoaded list
        /// </summary>
        internal Func<string, Scene> OverrideGetAndAddNewlyLoadedSceneByName;

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
            if (OverrideGetAndAddNewlyLoadedSceneByName != null)
            {
                return OverrideGetAndAddNewlyLoadedSceneByName.Invoke(sceneName);
            }
            else
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var sceneLoaded = SceneManager.GetSceneAt(i);
                    if (sceneLoaded.name == sceneName)
                    {
                        if (!ScenesLoaded.ContainsKey(sceneLoaded.handle))
                        {
                            ScenesLoaded.Add(sceneLoaded.handle, sceneLoaded);
                            return sceneLoaded;
                        }
                    }
                }

                throw new Exception($"Failed to find any loaded scene named {sceneName}!");
            }
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
            var clientSceneHandle = serverSceneHandle;
            if (ServerSceneHandleToClientSceneHandle.ContainsKey(serverSceneHandle))
            {
                clientSceneHandle = ServerSceneHandleToClientSceneHandle[serverSceneHandle];
                // If we were already set, then ignore
                if (SceneBeingSynchronized.IsValid() && SceneBeingSynchronized.isLoaded && SceneBeingSynchronized.handle == clientSceneHandle)
                {
                    return;
                }

                // Get the scene currently being synchronized
                SceneBeingSynchronized = ScenesLoaded.ContainsKey(clientSceneHandle) ? ScenesLoaded[clientSceneHandle] : new Scene();

                if (!SceneBeingSynchronized.IsValid() || !SceneBeingSynchronized.isLoaded)
                {
                    // Let's go ahead and use the currently active scene under the scenario where a NetworkObject is determined to exist in a scene that the NetworkSceneManager is not aware of
                    SceneBeingSynchronized = SceneManager.GetActiveScene();

                    // Keeping the warning here in the event we cannot find the scene being synchronized
                    Debug.LogWarning($"[{nameof(NetworkSceneManager)}- {nameof(ScenesLoaded)}] Could not find the appropriate scene to set as being synchronized! Using the currently active scene.");
                }
            }
            else
            {
                // Most common scenario for DontDestroyOnLoad is when NetworkManager is set to not be destroyed
                if (serverSceneHandle == DontDestroyOnLoadScene.handle)
                {
                    SceneBeingSynchronized = m_NetworkManager.gameObject.scene;
                    return;
                }
                else
                {
                    // Let's go ahead and use the currently active scene under the scenario where a NetworkObject is determined to exist in a scene that the NetworkSceneManager is not aware of
                    // or the NetworkObject has yet to be moved to that specific scene (i.e. no DontDestroyOnLoad scene exists yet).
                    SceneBeingSynchronized = SceneManager.GetActiveScene();

                    // This could be the scenario where NetworkManager.DontDestroy is false and we are creating the first NetworkObject (client side) to be in the DontDestroyOnLoad scene
                    // Otherwise, this is some other specific scenario that we might not be handling currently.
                    Debug.LogWarning($"[{nameof(SceneEventData)}- Scene Handle Mismatch] {nameof(serverSceneHandle)} could not be found in {nameof(ServerSceneHandleToClientSceneHandle)}. Using the currently active scene.");
                }
            }
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
        /// Entry method for scene unloading validation
        /// </summary>
        /// <param name="scene">the scene to be unloaded</param>
        /// <returns></returns>
        private SceneEventProgress ValidateSceneEventUnLoading(Scene scene)
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

            if (!scene.isLoaded)
            {
                Debug.LogWarning($"{nameof(UnloadScene)} was called, but the scene {scene.name} is not currently loaded!");
                return new SceneEventProgress(null, SceneEventProgressStatus.SceneNotLoaded);
            }

            return ValidateSceneEvent(scene.name, true);
        }

        /// <summary>
        /// Entry method for scene loading validation
        /// </summary>
        /// <param name="sceneName">scene name to load</param>
        /// <returns></returns>
        private SceneEventProgress ValidateSceneEventLoading(string sceneName)
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

            return ValidateSceneEvent(sceneName);
        }

        /// <summary>
        /// Validates the new scene event request by the server-side code.
        /// This also initializes some commonly shared values as well as SceneEventProgress
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns><see cref="SceneEventProgress"/> that should have a <see cref="SceneEventProgress.Status"/> of <see cref="SceneEventProgressStatus.Started"/> otherwise it failed.</returns>
        private SceneEventProgress ValidateSceneEvent(string sceneName, bool isUnloading = false)
        {
            // Return scene event already in progress if one is already in progress
            if (m_IsSceneEventActive)
            {
                return new SceneEventProgress(null, SceneEventProgressStatus.SceneEventInProgress);
            }

            // Return invalid scene name status if the scene name is invalid
            if (SceneUtility.GetBuildIndexByScenePath(sceneName) == InvalidSceneNameOrPath)
            {
                Debug.LogError($"Scene '{sceneName}' couldn't be loaded because it has not been added to the build settings scenes in build list.");
                return new SceneEventProgress(null, SceneEventProgressStatus.InvalidSceneName);
            }

            var sceneEventProgress = new SceneEventProgress(m_NetworkManager)
            {
                SceneHash = SceneHashFromNameOrPath(sceneName)
            };

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

            m_IsSceneEventActive = true;

            // Set our callback delegate handler for completion
            sceneEventProgress.OnComplete = OnSceneEventProgressCompleted;

            return sceneEventProgress;
        }

        /// <summary>
        /// Callback for the <see cref="SceneEventProgress.OnComplete"/> <see cref="SceneEventProgress.OnCompletedDelegate"/> handler
        /// </summary>
        /// <param name="sceneEventProgress"></param>
        private bool OnSceneEventProgressCompleted(SceneEventProgress sceneEventProgress)
        {
            var sceneEventData = BeginSceneEvent();
            var clientsThatCompleted = sceneEventProgress.GetClientsWithStatus(true);
            var clientsThatTimedOut = sceneEventProgress.GetClientsWithStatus(false);
            sceneEventData.SceneEventProgressId = sceneEventProgress.Guid;
            sceneEventData.SceneHash = sceneEventProgress.SceneHash;
            sceneEventData.SceneEventType = sceneEventProgress.SceneEventType;
            sceneEventData.ClientsCompleted = clientsThatCompleted;
            sceneEventData.LoadSceneMode = sceneEventProgress.LoadSceneMode;
            sceneEventData.ClientsTimedOut = clientsThatTimedOut;

            var message = new SceneEventMessage
            {
                EventData = sceneEventData
            };
            var size = m_NetworkManager.SendMessage(ref message, k_DeliveryType, m_NetworkManager.ConnectedClientsIds);

            m_NetworkManager.NetworkMetrics.TrackSceneEventSent(
                m_NetworkManager.ConnectedClientsIds,
                (uint)sceneEventProgress.SceneEventType,
                SceneNameFromHash(sceneEventProgress.SceneHash),
                size);

            // Send a local notification to the server that all clients are done loading or unloading
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                SceneEventType = sceneEventProgress.SceneEventType,
                SceneName = SceneNameFromHash(sceneEventProgress.SceneHash),
                ClientId = NetworkManager.ServerClientId,
                LoadSceneMode = sceneEventProgress.LoadSceneMode,
                ClientsThatCompleted = clientsThatCompleted,
                ClientsThatTimedOut = clientsThatTimedOut,
            });

            if (sceneEventData.SceneEventType == SceneEventType.LoadEventCompleted)
            {
                OnLoadEventCompleted?.Invoke(SceneNameFromHash(sceneEventProgress.SceneHash), sceneEventProgress.LoadSceneMode, sceneEventData.ClientsCompleted, sceneEventData.ClientsTimedOut);
            }
            else
            {
                OnUnloadEventCompleted?.Invoke(SceneNameFromHash(sceneEventProgress.SceneHash), sceneEventProgress.LoadSceneMode, sceneEventData.ClientsCompleted, sceneEventData.ClientsTimedOut);
            }

            EndSceneEvent(sceneEventData.SceneEventId);
            return true;
        }

        /// <summary>
        /// <b>Server Side:</b>
        /// Unloads an additively loaded scene.  If you want to unload a <see cref="LoadSceneMode.Single"/> mode loaded scene load another <see cref="LoadSceneMode.Single"/> scene.
        /// When applicable, the <see cref="AsyncOperation"/> is delivered within the <see cref="SceneEvent"/> via the <see cref="OnSceneEvent"/>
        /// </summary>
        /// <param name="scene"></param>
        /// <returns><see cref="SceneEventProgressStatus"/> (<see cref="SceneEventProgressStatus.Started"/> means it was successful)</returns>
        public SceneEventProgressStatus UnloadScene(Scene scene)
        {
            var sceneName = scene.name;
            var sceneHandle = scene.handle;
            if (!scene.isLoaded)
            {
                Debug.LogWarning($"{nameof(UnloadScene)} was called, but the scene {scene.name} is not currently loaded!");
                return SceneEventProgressStatus.SceneNotLoaded;
            }

            var sceneEventProgress = ValidateSceneEventUnLoading(scene);
            if (sceneEventProgress.Status != SceneEventProgressStatus.Started)
            {
                return sceneEventProgress.Status;
            }

            if (!ScenesLoaded.ContainsKey(sceneHandle))
            {
                Debug.LogError($"{nameof(UnloadScene)} internal error! {sceneName} with handle {scene.handle} is not within the internal scenes loaded dictionary!");
                return SceneEventProgressStatus.InternalNetcodeError;
            }
            var sceneEventData = BeginSceneEvent();
            sceneEventData.SceneEventProgressId = sceneEventProgress.Guid;
            sceneEventData.SceneEventType = SceneEventType.Unload;
            sceneEventData.SceneHash = SceneHashFromNameOrPath(sceneName);
            sceneEventData.LoadSceneMode = LoadSceneMode.Additive; // The only scenes unloaded are scenes that were additively loaded
            sceneEventData.SceneHandle = sceneHandle;

            // This will be the message we send to everyone when this scene event sceneEventProgress is complete
            sceneEventProgress.SceneEventType = SceneEventType.UnloadEventCompleted;

            ScenesLoaded.Remove(scene.handle);
            sceneEventProgress.SceneEventId = sceneEventData.SceneEventId;
            sceneEventProgress.OnSceneEventCompleted = OnSceneUnloaded;
            var sceneUnload = SceneManagerHandler.UnloadSceneAsync(scene, sceneEventProgress);

            // Notify local server that a scene is going to be unloaded
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                AsyncOperation = sceneUnload,
                SceneEventType = sceneEventData.SceneEventType,
                LoadSceneMode = sceneEventData.LoadSceneMode,
                SceneName = sceneName,
                ClientId = NetworkManager.ServerClientId  // Server can only invoke this
            });

            OnUnload?.Invoke(NetworkManager.ServerClientId, sceneName, sceneUnload);

            //Return the status
            return sceneEventProgress.Status;
        }

        /// <summary>
        /// <b>Client Side:</b>
        /// Handles <see cref="SceneEventType.Unload"/> scene events.
        /// </summary>
        private void OnClientUnloadScene(uint sceneEventId)
        {
            var sceneEventData = SceneEventDataStore[sceneEventId];
            var sceneName = SceneNameFromHash(sceneEventData.SceneHash);

            if (!ServerSceneHandleToClientSceneHandle.ContainsKey(sceneEventData.SceneHandle))
            {
                Debug.Log($"Client failed to unload scene {sceneName} " +
                    $"because we are missing the client scene handle due to the server scene handle {sceneEventData.SceneHandle} not being found.");
                EndSceneEvent(sceneEventId);
                return;
            }

            var sceneHandle = ServerSceneHandleToClientSceneHandle[sceneEventData.SceneHandle];

            if (!ScenesLoaded.ContainsKey(sceneHandle))
            {
                // Error scene handle not found!
                throw new Exception($"Client failed to unload scene {sceneName} " +
                    $"because the client scene handle {sceneHandle} was not found in ScenesLoaded!");
            }
            m_IsSceneEventActive = true;
            var sceneEventProgress = new SceneEventProgress(m_NetworkManager);
            sceneEventProgress.SceneEventId = sceneEventData.SceneEventId;
            sceneEventProgress.OnSceneEventCompleted = OnSceneUnloaded;
            var sceneUnload = SceneManagerHandler.UnloadSceneAsync(ScenesLoaded[sceneHandle], sceneEventProgress);

            ScenesLoaded.Remove(sceneHandle);

            // Remove our server to scene handle lookup
            ServerSceneHandleToClientSceneHandle.Remove(sceneEventData.SceneHandle);

            // Notify the local client that a scene is going to be unloaded
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                AsyncOperation = sceneUnload,
                SceneEventType = sceneEventData.SceneEventType,
                LoadSceneMode = LoadSceneMode.Additive,     // The only scenes unloaded are scenes that were additively loaded
                SceneName = sceneName,
                ClientId = m_NetworkManager.LocalClientId   // Server sent this message to the client, but client is executing it
            });

            OnUnload?.Invoke(m_NetworkManager.LocalClientId, sceneName, sceneUnload);
        }

        /// <summary>
        /// Server and Client:
        /// Invoked when an additively loaded scene is unloaded
        /// </summary>
        private void OnSceneUnloaded(uint sceneEventId)
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
                    SceneEventProgressTracking[sceneEventData.SceneEventProgressId].ClientFinishedSceneEvent(NetworkManager.ServerClientId);
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

        private void EmptySceneUnloadedOperation(uint sceneEventId)
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
                    var sceneEventProgress = new SceneEventProgress(m_NetworkManager);
                    sceneEventProgress.SceneEventId = sceneEventId;
                    sceneEventProgress.OnSceneEventCompleted = EmptySceneUnloadedOperation;
                    var sceneUnload = SceneManagerHandler.UnloadSceneAsync(keyHandleEntry.Value, sceneEventProgress);
                    SceneUnloadEventHandler.RegisterScene(this, keyHandleEntry.Value, LoadSceneMode.Additive, sceneUnload);
                }
            }
            // clear out our scenes loaded list
            ScenesLoaded.Clear();
        }

        /// <summary>
        /// <b>Server side:</b>
        /// Loads the scene name in either additive or single loading mode.
        /// When applicable, the <see cref="AsyncOperation"/> is delivered within the <see cref="SceneEvent"/> via <see cref="OnSceneEvent"/>
        /// </summary>
        /// <param name="sceneName">the name of the scene to be loaded</param>
        /// <param name="loadSceneMode">how the scene will be loaded (single or additive mode)</param>
        /// <returns><see cref="SceneEventProgressStatus"/> (<see cref="SceneEventProgressStatus.Started"/> means it was successful)</returns>
        public SceneEventProgressStatus LoadScene(string sceneName, LoadSceneMode loadSceneMode)
        {
            var sceneEventProgress = ValidateSceneEventLoading(sceneName);
            if (sceneEventProgress.Status != SceneEventProgressStatus.Started)
            {
                return sceneEventProgress.Status;
            }

            // This will be the message we send to everyone when this scene event sceneEventProgress is complete
            sceneEventProgress.SceneEventType = SceneEventType.LoadEventCompleted;
            sceneEventProgress.LoadSceneMode = loadSceneMode;

            var sceneEventData = BeginSceneEvent();

            // Now set up the current scene event
            sceneEventData.SceneEventProgressId = sceneEventProgress.Guid;
            sceneEventData.SceneEventType = SceneEventType.Load;
            sceneEventData.SceneHash = SceneHashFromNameOrPath(sceneName);
            sceneEventData.LoadSceneMode = loadSceneMode;
            var sceneEventId = sceneEventData.SceneEventId;
            // This both checks to make sure the scene is valid and if not resets the active scene event
            m_IsSceneEventActive = ValidateSceneBeforeLoading(sceneEventData.SceneHash, loadSceneMode);
            if (!m_IsSceneEventActive)
            {
                EndSceneEvent(sceneEventId);
                return SceneEventProgressStatus.SceneFailedVerification;
            }

            if (sceneEventData.LoadSceneMode == LoadSceneMode.Single)
            {
                // Destroy current scene objects before switching.
                m_NetworkManager.SpawnManager.ServerDestroySpawnedSceneObjects();

                // Preserve the objects that should not be destroyed during the scene event
                MoveObjectsToDontDestroyOnLoad();

                // Now Unload all currently additively loaded scenes
                UnloadAdditivelyLoadedScenes(sceneEventId);

                // Register the active scene for unload scene event notifications
                SceneUnloadEventHandler.RegisterScene(this, SceneManager.GetActiveScene(), LoadSceneMode.Single);
            }

            // Now start loading the scene
            sceneEventProgress.SceneEventId = sceneEventId;
            sceneEventProgress.OnSceneEventCompleted = OnSceneLoaded;
            var sceneLoad = SceneManagerHandler.LoadSceneAsync(sceneName, loadSceneMode, sceneEventProgress);
            // Notify the local server that a scene loading event has begun
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                AsyncOperation = sceneLoad,
                SceneEventType = sceneEventData.SceneEventType,
                LoadSceneMode = sceneEventData.LoadSceneMode,
                SceneName = sceneName,
                ClientId = NetworkManager.ServerClientId
            });

            OnLoad?.Invoke(NetworkManager.ServerClientId, sceneName, sceneEventData.LoadSceneMode, sceneLoad);

            //Return our scene progress instance
            return sceneEventProgress.Status;
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
        /// Client Side:
        /// Handles both forms of scene loading
        /// </summary>
        /// <param name="objectStream">Stream data associated with the event</param>
        private void OnClientSceneLoadingEvent(uint sceneEventId)
        {
            var sceneEventData = SceneEventDataStore[sceneEventId];
            var sceneName = SceneNameFromHash(sceneEventData.SceneHash);

            // Run scene validation before loading a scene
            if (!ValidateSceneBeforeLoading(sceneEventData.SceneHash, sceneEventData.LoadSceneMode))
            {
                EndSceneEvent(sceneEventId);
                return;
            }

            if (sceneEventData.LoadSceneMode == LoadSceneMode.Single)
            {
                // Move ALL NetworkObjects to the temp scene
                MoveObjectsToDontDestroyOnLoad();

                // Now Unload all currently additively loaded scenes
                UnloadAdditivelyLoadedScenes(sceneEventData.SceneEventId);
            }

            // The Condition: While a scene is asynchronously loaded in single loading scene mode, if any new NetworkObjects are spawned
            // they need to be moved into the do not destroy temporary scene
            // When it is set: Just before starting the asynchronous loading call
            // When it is unset: After the scene has loaded, the PopulateScenePlacedObjects is called, and all NetworkObjects in the do
            // not destroy temporary scene are moved into the active scene
            if (sceneEventData.LoadSceneMode == LoadSceneMode.Single)
            {
                IsSpawnedObjectsPendingInDontDestroyOnLoad = true;

                // Register the active scene for unload scene event notifications
                SceneUnloadEventHandler.RegisterScene(this, SceneManager.GetActiveScene(), LoadSceneMode.Single);

            }
            var sceneEventProgress = new SceneEventProgress(m_NetworkManager);
            sceneEventProgress.SceneEventId = sceneEventId;
            sceneEventProgress.OnSceneEventCompleted = OnSceneLoaded;
            var sceneLoad = SceneManagerHandler.LoadSceneAsync(sceneName, sceneEventData.LoadSceneMode, sceneEventProgress);

            OnSceneEvent?.Invoke(new SceneEvent()
            {
                AsyncOperation = sceneLoad,
                SceneEventType = sceneEventData.SceneEventType,
                LoadSceneMode = sceneEventData.LoadSceneMode,
                SceneName = sceneName,
                ClientId = m_NetworkManager.LocalClientId
            });

            OnLoad?.Invoke(m_NetworkManager.LocalClientId, sceneName, sceneEventData.LoadSceneMode, sceneLoad);
        }

        /// <summary>
        /// Client and Server:
        /// Generic on scene loaded callback method to be called upon a scene loading
        /// </summary>
        private void OnSceneLoaded(uint sceneEventId)
        {
            // If we are shutdown or about to shutdown, then ignore this event
            if (!m_NetworkManager.IsListening || m_NetworkManager.ShutdownInProgress)
            {
                return;
            }

            var sceneEventData = SceneEventDataStore[sceneEventId];
            var nextScene = GetAndAddNewlyLoadedSceneByName(SceneNameFromHash(sceneEventData.SceneHash));
            if (!nextScene.isLoaded || !nextScene.IsValid())
            {
                throw new Exception($"Failed to find valid scene internal Unity.Netcode for {nameof(GameObject)}s error!");
            }

            if (sceneEventData.LoadSceneMode == LoadSceneMode.Single)
            {
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
        /// Server side:
        /// On scene loaded callback method invoked by OnSceneLoading only
        /// </summary>
        private void OnServerLoadedScene(uint sceneEventId, Scene scene)
        {
            var sceneEventData = SceneEventDataStore[sceneEventId];
            // Register in-scene placed NetworkObjects with spawn manager
            foreach (var keyValuePairByGlobalObjectIdHash in ScenePlacedObjects)
            {
                foreach (var keyValuePairBySceneHandle in keyValuePairByGlobalObjectIdHash.Value)
                {
                    if (!keyValuePairBySceneHandle.Value.IsPlayerObject)
                    {
                        // All in-scene placed NetworkObjects default to being owned by the server
                        m_NetworkManager.SpawnManager.SpawnNetworkObjectLocally(keyValuePairBySceneHandle.Value,
                            m_NetworkManager.SpawnManager.GetNetworkObjectId(), true, false, NetworkManager.ServerClientId, true);
                    }
                }
            }

            // Add any despawned when spawned in-scene placed NetworkObjects to the scene event data
            sceneEventData.AddDespawnedInSceneNetworkObjects();

            // Set the server's scene's handle so the client can build a look up table
            sceneEventData.SceneHandle = scene.handle;

            // Send all clients the scene load event
            for (int j = 0; j < m_NetworkManager.ConnectedClientsList.Count; j++)
            {
                var clientId = m_NetworkManager.ConnectedClientsList[j].ClientId;
                if (clientId != NetworkManager.ServerClientId)
                {
                    sceneEventData.TargetClientId = clientId;
                    var message = new SceneEventMessage
                    {
                        EventData = sceneEventData
                    };
                    var size = m_NetworkManager.SendMessage(ref message, k_DeliveryType, clientId);
                    m_NetworkManager.NetworkMetrics.TrackSceneEventSent(clientId, (uint)sceneEventData.SceneEventType, scene.name, size);
                }
            }

            m_IsSceneEventActive = false;
            //First, notify local server that the scene was loaded
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                SceneEventType = SceneEventType.LoadComplete,
                LoadSceneMode = sceneEventData.LoadSceneMode,
                SceneName = SceneNameFromHash(sceneEventData.SceneHash),
                ClientId = NetworkManager.ServerClientId,
                Scene = scene,
            });

            OnLoadComplete?.Invoke(NetworkManager.ServerClientId, SceneNameFromHash(sceneEventData.SceneHash), sceneEventData.LoadSceneMode);

            //Second, only if we are a host do we want register having loaded for the associated SceneEventProgress
            if (SceneEventProgressTracking.ContainsKey(sceneEventData.SceneEventProgressId) && m_NetworkManager.IsHost)
            {
                SceneEventProgressTracking[sceneEventData.SceneEventProgressId].ClientFinishedSceneEvent(NetworkManager.ServerClientId);
            }
            EndSceneEvent(sceneEventId);
        }

        /// <summary>
        /// Client side:
        /// On scene loaded callback method invoked by OnSceneLoading only
        /// </summary>
        private void OnClientLoadedScene(uint sceneEventId, Scene scene)
        {
            var sceneEventData = SceneEventDataStore[sceneEventId];
            sceneEventData.DeserializeScenePlacedObjects();

            sceneEventData.SceneEventType = SceneEventType.LoadComplete;
            SendSceneEventData(sceneEventId, new ulong[] { NetworkManager.ServerClientId });
            m_IsSceneEventActive = false;

            // Notify local client that the scene was loaded
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                SceneEventType = SceneEventType.LoadComplete,
                LoadSceneMode = sceneEventData.LoadSceneMode,
                SceneName = SceneNameFromHash(sceneEventData.SceneHash),
                ClientId = m_NetworkManager.LocalClientId,
                Scene = scene,
            });

            OnLoadComplete?.Invoke(m_NetworkManager.LocalClientId, SceneNameFromHash(sceneEventData.SceneHash), sceneEventData.LoadSceneMode);

            EndSceneEvent(sceneEventId);
        }

        /// <summary>
        /// Used for integration testing, due to the complexities of having all clients loading scenes
        /// this is needed to "filter" out the scenes not loaded by NetworkSceneManager
        /// (i.e. we don't want a late joining player to load all of the other client scenes)
        /// </summary>
        internal Func<Scene, bool> ExcludeSceneFromSychronization;

        /// <summary>
        /// Server Side:
        /// This is used for players that have just had their connection approved and will assure they are synchronized
        /// properly if they are late joining
        /// Note: We write out all of the scenes to be loaded first and then all of the NetworkObjects that need to be
        /// synchronized.
        /// </summary>
        /// <param name="clientId">newly joined client identifier</param>
        internal void SynchronizeNetworkObjects(ulong clientId)
        {
            // Update the clients
            m_NetworkManager.SpawnManager.UpdateObservedNetworkObjects(clientId);

            var sceneEventData = BeginSceneEvent();

            sceneEventData.InitializeForSynch();
            sceneEventData.TargetClientId = clientId;
            sceneEventData.LoadSceneMode = ClientSynchronizationMode;
            var activeScene = SceneManager.GetActiveScene();
            sceneEventData.SceneEventType = SceneEventType.Synchronize;

            // Organize how (and when) we serialize our NetworkObjects
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);

                // NetworkSceneManager does not synchronize scenes that are not loaded by NetworkSceneManager
                // unless the scene in question is the currently active scene.
                if (ExcludeSceneFromSychronization != null && !ExcludeSceneFromSychronization(scene))
                {
                    continue;
                }

                var sceneHash = SceneHashFromNameOrPath(scene.path);

                // This would depend upon whether we are additive or not
                // If we are the base scene, then we set the root scene index;
                if (activeScene == scene)
                {
                    if (!ValidateSceneBeforeLoading(sceneHash, sceneEventData.LoadSceneMode))
                    {
                        continue;
                    }
                    sceneEventData.SceneHash = sceneHash;
                    sceneEventData.SceneHandle = scene.handle;
                }
                else if (!ValidateSceneBeforeLoading(sceneHash, LoadSceneMode.Additive))
                {
                    continue;
                }
                sceneEventData.AddSceneToSynchronize(sceneHash, scene.handle);
            }

            sceneEventData.AddSpawnedNetworkObjects();
            sceneEventData.AddDespawnedInSceneNetworkObjects();

            var message = new SceneEventMessage
            {
                EventData = sceneEventData
            };
            var size = m_NetworkManager.SendMessage(ref message, k_DeliveryType, clientId);
            m_NetworkManager.NetworkMetrics.TrackSceneEventSent(clientId, (uint)sceneEventData.SceneEventType, "", size);

            // Notify the local server that the client has been sent the synchronize event
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                SceneEventType = sceneEventData.SceneEventType,
                ClientId = clientId
            });

            OnSynchronize?.Invoke(clientId);

            EndSceneEvent(sceneEventData.SceneEventId);
        }

        /// <summary>
        /// This is called when the client receives the <see cref="SceneEventType.Synchronize"/> event
        /// Note: This can recurse one additional time by the client if the current scene loaded by the client
        /// is already loaded.
        /// </summary>
        private void OnClientBeginSync(uint sceneEventId)
        {
            var sceneEventData = SceneEventDataStore[sceneEventId];
            var sceneHash = sceneEventData.GetNextSceneSynchronizationHash();
            var sceneHandle = sceneEventData.GetNextSceneSynchronizationHandle();
            var sceneName = SceneNameFromHash(sceneHash);
            var activeScene = SceneManager.GetActiveScene();

            var loadSceneMode = sceneHash == sceneEventData.SceneHash ? sceneEventData.LoadSceneMode : LoadSceneMode.Additive;

            // Store the sceneHandle and hash
            sceneEventData.NetworkSceneHandle = sceneHandle;
            sceneEventData.ClientSceneHash = sceneHash;

            // If this is the beginning of the synchronization event, then send client a notification that synchronization has begun
            if (sceneHash == sceneEventData.SceneHash)
            {
                OnSceneEvent?.Invoke(new SceneEvent()
                {
                    SceneEventType = SceneEventType.Synchronize,
                    ClientId = m_NetworkManager.LocalClientId,
                });

                OnSynchronize?.Invoke(m_NetworkManager.LocalClientId);

                // Clear the in-scene placed NetworkObjects when we load the first scene in our synchronization process
                ScenePlacedObjects.Clear();
            }

            // Always check to see if the scene needs to be validated
            if (!ValidateSceneBeforeLoading(sceneHash, loadSceneMode))
            {
                HandleClientSceneEvent(sceneEventId);
                if (m_NetworkManager.LogLevel == LogLevel.Developer)
                {
                    NetworkLog.LogInfo($"Client declined to load the scene {sceneName}, continuing with synchronization.");
                }
                return;
            }

            var shouldPassThrough = false;
            var sceneLoad = (AsyncOperation)null;

            // Check to see if the client already has loaded the scene to be loaded
            if (sceneName == activeScene.name)
            {
                // If the client is already in the same scene, then pass through and
                // don't try to reload it.
                shouldPassThrough = true;
            }

            if (!shouldPassThrough)
            {
                // If not, then load the scene
                var sceneEventProgress = new SceneEventProgress(m_NetworkManager);
                sceneEventProgress.SceneEventId = sceneEventId;
                sceneEventProgress.OnSceneEventCompleted = ClientLoadedSynchronization;
                sceneLoad = SceneManagerHandler.LoadSceneAsync(sceneName, loadSceneMode, sceneEventProgress);

                // Notify local client that a scene load has begun
                OnSceneEvent?.Invoke(new SceneEvent()
                {
                    AsyncOperation = sceneLoad,
                    SceneEventType = SceneEventType.Load,
                    LoadSceneMode = loadSceneMode,
                    SceneName = sceneName,
                    ClientId = m_NetworkManager.LocalClientId,
                });

                OnLoad?.Invoke(m_NetworkManager.LocalClientId, sceneName, loadSceneMode, sceneLoad);
            }
            else
            {
                // If so, then pass through
                ClientLoadedSynchronization(sceneEventId);
            }
        }

        /// <summary>
        /// Once a scene is loaded ( or if it was already loaded) this gets called.
        /// This handles all of the in-scene and dynamically spawned NetworkObject synchronization
        /// </summary>
        /// <param name="sceneIndex">Netcode scene index that was loaded</param>
        private void ClientLoadedSynchronization(uint sceneEventId)
        {
            var sceneEventData = SceneEventDataStore[sceneEventId];
            var sceneName = SceneNameFromHash(sceneEventData.ClientSceneHash);
            var nextScene = GetAndAddNewlyLoadedSceneByName(sceneName);

            if (!nextScene.isLoaded || !nextScene.IsValid())
            {
                throw new Exception($"Failed to find valid scene internal Unity.Netcode for {nameof(GameObject)}s error!");
            }

            var loadSceneMode = (sceneEventData.ClientSceneHash == sceneEventData.SceneHash ? sceneEventData.LoadSceneMode : LoadSceneMode.Additive);

            // For now, during a synchronization event, we will make the first scene the "base/master" scene that denotes a "complete scene switch"
            if (loadSceneMode == LoadSceneMode.Single)
            {
                SceneManager.SetActiveScene(nextScene);
            }

            if (!ServerSceneHandleToClientSceneHandle.ContainsKey(sceneEventData.NetworkSceneHandle))
            {
                ServerSceneHandleToClientSceneHandle.Add(sceneEventData.NetworkSceneHandle, nextScene.handle);
            }
            else
            {
                // If the exact same handle exists then there are problems with using handles
                throw new Exception($"Server Scene Handle ({sceneEventData.SceneHandle}) already exist!  Happened during scene load of {nextScene.name} with Client Handle ({nextScene.handle})");
            }

            // Apply all in-scene placed NetworkObjects loaded by the scene
            PopulateScenePlacedObjects(nextScene, false);

            // Send notification back to server that we finished loading this scene
            var responseSceneEventData = BeginSceneEvent();
            responseSceneEventData.LoadSceneMode = loadSceneMode;
            responseSceneEventData.SceneEventType = SceneEventType.LoadComplete;
            responseSceneEventData.SceneHash = sceneEventData.ClientSceneHash;


            var message = new SceneEventMessage
            {
                EventData = responseSceneEventData
            };
            var size = m_NetworkManager.SendMessage(ref message, k_DeliveryType, NetworkManager.ServerClientId);

            m_NetworkManager.NetworkMetrics.TrackSceneEventSent(NetworkManager.ServerClientId, (uint)responseSceneEventData.SceneEventType, sceneName, size);

            EndSceneEvent(responseSceneEventData.SceneEventId);

            // Send notification to local client that the scene has finished loading
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                SceneEventType = SceneEventType.LoadComplete,
                LoadSceneMode = loadSceneMode,
                SceneName = sceneName,
                Scene = nextScene,
                ClientId = m_NetworkManager.LocalClientId,
            });

            OnLoadComplete?.Invoke(m_NetworkManager.LocalClientId, sceneName, loadSceneMode);

            // Check to see if we still have scenes to load and synchronize with
            HandleClientSceneEvent(sceneEventId);
        }

        /// <summary>
        /// Client Side:
        /// Handles incoming Scene_Event messages for clients
        /// </summary>
        /// <param name="stream">data associated with the event</param>
        private void HandleClientSceneEvent(uint sceneEventId)
        {
            var sceneEventData = SceneEventDataStore[sceneEventId];
            switch (sceneEventData.SceneEventType)
            {
                case SceneEventType.Load:
                    {
                        OnClientSceneLoadingEvent(sceneEventId);
                        break;
                    }
                case SceneEventType.Unload:
                    {
                        OnClientUnloadScene(sceneEventId);
                        break;
                    }
                case SceneEventType.Synchronize:
                    {
                        if (!sceneEventData.IsDoneWithSynchronization())
                        {
                            OnClientBeginSync(sceneEventId);
                        }
                        else
                        {
                            // Include anything in the DDOL scene
                            PopulateScenePlacedObjects(DontDestroyOnLoadScene, false);
                            // Synchronize the NetworkObjects for this scene
                            sceneEventData.SynchronizeSceneNetworkObjects(m_NetworkManager);

                            sceneEventData.SceneEventType = SceneEventType.SynchronizeComplete;
                            SendSceneEventData(sceneEventId, new ulong[] { NetworkManager.ServerClientId });

                            // All scenes are synchronized, let the server know we are done synchronizing
                            m_NetworkManager.IsConnectedClient = true;

                            // Client is now synchronized and fully "connected".  This also means the client can send "RPCs" at this time
                            m_NetworkManager.InvokeOnClientConnectedCallback(m_NetworkManager.LocalClientId);

                            // Notify the client that they have finished synchronizing
                            OnSceneEvent?.Invoke(new SceneEvent()
                            {
                                SceneEventType = sceneEventData.SceneEventType,
                                ClientId = m_NetworkManager.LocalClientId, // Client sent this to the server
                            });

                            OnSynchronizeComplete?.Invoke(m_NetworkManager.LocalClientId);

                            EndSceneEvent(sceneEventId);
                        }
                        break;
                    }
                case SceneEventType.ReSynchronize:
                    {
                        // Notify the local client that they have been re-synchronized after being synchronized with an in progress game session
                        OnSceneEvent?.Invoke(new SceneEvent()
                        {
                            SceneEventType = sceneEventData.SceneEventType,
                            ClientId = NetworkManager.ServerClientId,  // Server sent this to client
                        });

                        EndSceneEvent(sceneEventId);
                        break;
                    }
                case SceneEventType.LoadEventCompleted:
                case SceneEventType.UnloadEventCompleted:
                    {
                        // Notify the local client that all clients have finished loading or unloading
                        OnSceneEvent?.Invoke(new SceneEvent()
                        {
                            SceneEventType = sceneEventData.SceneEventType,
                            LoadSceneMode = sceneEventData.LoadSceneMode,
                            SceneName = SceneNameFromHash(sceneEventData.SceneHash),
                            ClientId = NetworkManager.ServerClientId,
                            ClientsThatCompleted = sceneEventData.ClientsCompleted,
                            ClientsThatTimedOut = sceneEventData.ClientsTimedOut,
                        });

                        if (sceneEventData.SceneEventType == SceneEventType.LoadEventCompleted)
                        {
                            OnLoadEventCompleted?.Invoke(SceneNameFromHash(sceneEventData.SceneHash), sceneEventData.LoadSceneMode, sceneEventData.ClientsCompleted, sceneEventData.ClientsTimedOut);
                        }
                        else
                        {
                            OnUnloadEventCompleted?.Invoke(SceneNameFromHash(sceneEventData.SceneHash), sceneEventData.LoadSceneMode, sceneEventData.ClientsCompleted, sceneEventData.ClientsTimedOut);
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
                            SceneEventProgressTracking[sceneEventData.SceneEventProgressId].ClientFinishedSceneEvent(clientId);
                        }
                        EndSceneEvent(sceneEventId);
                        break;
                    }
                case SceneEventType.UnloadComplete:
                    {
                        if (SceneEventProgressTracking.ContainsKey(sceneEventData.SceneEventProgressId))
                        {
                            SceneEventProgressTracking[sceneEventData.SceneEventProgressId].ClientFinishedSceneEvent(clientId);
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
        /// Both Client and Server: Incoming scene event entry point
        /// </summary>
        /// <param name="clientId">client who sent the scene event</param>
        /// <param name="reader">data associated with the scene event</param>
        internal void HandleSceneEvent(ulong clientId, FastBufferReader reader)
        {
            if (m_NetworkManager != null)
            {
                var sceneEventData = BeginSceneEvent();

                sceneEventData.Deserialize(reader);

                m_NetworkManager.NetworkMetrics.TrackSceneEventReceived(
                   clientId, (uint)sceneEventData.SceneEventType, SceneNameFromHash(sceneEventData.SceneHash), reader.Length);

                if (sceneEventData.IsSceneEventClientSide())
                {
                    HandleClientSceneEvent(sceneEventData.SceneEventId);
                }
                else
                {
                    HandleServerSceneEvent(sceneEventData.SceneEventId, clientId);
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
                if (networkObjectInstance.IsSceneObject != false && networkObjectInstance.NetworkManager == m_NetworkManager && sceneHandle == sceneToFilterBy.handle)
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
