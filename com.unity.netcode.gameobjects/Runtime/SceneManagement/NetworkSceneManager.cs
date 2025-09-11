using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Unity.Netcode
{
    /// <summary>
    /// Used for local notifications of various scene events.  The <see cref="NetworkSceneManager.OnSceneEvent"/> of
    /// delegate type <see cref="NetworkSceneManager.SceneEventDelegate"/> uses this class to provide
    /// scene event status.<br />
    /// <em>Note: This is only when <see cref="NetworkConfig.EnableSceneManagement"/> is enabled.</em><br />
    /// <em>*** Do not start new scene events within scene event notification callbacks.</em><br />
    /// See also: <br />
    /// <see cref="SceneEventType"/>
    /// </summary>
    public class SceneEvent
    {
        /// <summary>
        /// The <see cref="UnityEngine.AsyncOperation"/> returned by <see cref="SceneManager"/><br />
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
        /// If applicable, this reflects the type of scene loading or unloading that is occurring.<br />
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
        /// This will be set to the scene name that the event pertains to.<br />
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
        /// This will be set to the path to the scene that the event pertains to.<br />
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
        public string ScenePath;

        /// <summary>
        /// When a scene is loaded, the Scene structure is returned.<br />
        /// This is set for the following <see cref="Netcode.SceneEventType"/>s:
        /// <list type="bullet">
        /// <item><term><see cref="SceneEventType.LoadComplete"/></term></item>
        /// </list>
        /// </summary>
        public Scene Scene;

        /// <summary>
        /// The client identifier can vary depending upon the following conditions: <br />
        /// <list type="number">
        /// <item><term><see cref="Netcode.SceneEventType"/>s that always set the <see cref="ClientId"/>
        /// to the local client identifier, are initiated (and processed locally) by the
        /// server-host, and sent to all clients to be processed.<br />
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
        /// List of clients that completed a loading or unloading event.<br />
        /// This is set for the following <see cref="Netcode.SceneEventType"/>s:
        /// <list type="bullet">
        /// <item><term><see cref="SceneEventType.LoadEventCompleted"/></term></item>
        /// <item><term><see cref="SceneEventType.UnloadEventCompleted"/></term></item>
        /// </list>
        /// </summary>
        public List<ulong> ClientsThatCompleted;

        /// <summary>
        /// List of clients that timed out during a loading or unloading event.<br />
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
        /// The delegate callback definition for scene event notifications.<br />
        /// See also: <br />
        /// <see cref="SceneEvent"/><br />
        /// <see cref="SceneEventData"/>
        /// </summary>
        /// <param name="sceneEvent">SceneEvent which contains information about the scene event, including type, progress, and scene details</param>
        public delegate void SceneEventDelegate(SceneEvent sceneEvent);

        /// <summary>
        /// Subscribe to this event to receive all <see cref="SceneEventType"/> notifications.<br />
        /// For more details review over <see cref="SceneEvent"/> and <see cref="SceneEventType"/>.<br />
        /// <b>Alternate Single Event Type Notification Registration Options</b><br />
        /// To receive only a specific event type notification or a limited set of notifications you can alternately subscribe to
        /// each notification type individually via the following events:<br />
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
        /// <em>Note: Do not start new scene events within NetworkSceneManager scene event notification callbacks.</em><br />
        /// </summary>
        public event SceneEventDelegate OnSceneEvent;

        /// <summary>
        /// Delegate declaration for the OnLoad event.<br />
        /// See also: <br />
        /// <see cref="SceneEventType.Load"/>for more information
        /// </summary>
        /// <param name="clientId">the client that is processing this event (the server will receive all of these events for every client and itself)</param>
        /// <param name="sceneName">name of the scene being processed</param>
        /// <param name="loadSceneMode">the LoadSceneMode mode for the scene being loaded</param>
        /// <param name="asyncOperation">the associated <see cref="AsyncOperation"/> that can be used for scene loading progress</param>
        public delegate void OnLoadDelegateHandler(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation);

        /// <summary>
        /// Delegate declaration for the OnUnload event.<br />
        /// See also: <br />
        /// <see cref="SceneEventType.Unload"/> for more information
        /// </summary>
        /// <param name="clientId">the client that is processing this event (the server will receive all of these events for every client and itself)</param>
        /// <param name="sceneName">name of the scene being processed</param>
        /// <param name="asyncOperation">the associated <see cref="AsyncOperation"/> that can be used for scene unloading progress</param>
        public delegate void OnUnloadDelegateHandler(ulong clientId, string sceneName, AsyncOperation asyncOperation);

        /// <summary>
        /// Delegate declaration for the OnSynchronize event.<br />
        /// See also: <br />
        /// <see cref="SceneEventType.Synchronize"/> for more information
        /// </summary>
        /// <param name="clientId">the client that is processing this event (the server will receive all of these events for every client and itself)</param>
        public delegate void OnSynchronizeDelegateHandler(ulong clientId);

        /// <summary>
        /// Delegate declaration for the OnLoadEventCompleted and OnUnloadEventCompleted events.<br />
        /// See also:<br />
        /// <see cref="SceneEventType.LoadEventCompleted"/><br />
        /// <see cref="SceneEventType.UnloadEventCompleted"/>
        /// </summary>
        /// <param name="sceneName">scene pertaining to this event</param>
        /// <param name="loadSceneMode"><see cref="LoadSceneMode"/> of the associated event completed</param>
        /// <param name="clientsCompleted">the clients that completed the loading event</param>
        /// <param name="clientsTimedOut">the clients (if any) that timed out during the loading event</param>
        public delegate void OnEventCompletedDelegateHandler(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut);

        /// <summary>
        /// Delegate declaration for the OnLoadComplete event.<br />
        /// See also:<br />
        /// <see cref="SceneEventType.LoadComplete"/> for more information
        /// </summary>
        /// <param name="clientId">the client that is processing this event (the server will receive all of these events for every client and itself)</param>
        /// <param name="sceneName">the scene name pertaining to this event</param>
        /// <param name="loadSceneMode">the mode the scene was loaded in</param>
        public delegate void OnLoadCompleteDelegateHandler(ulong clientId, string sceneName, LoadSceneMode loadSceneMode);

        /// <summary>
        /// Delegate declaration for the OnUnloadComplete event.<br />
        /// See also:<br />
        /// <see cref="SceneEventType.UnloadComplete"/> for more information
        /// </summary>
        /// <param name="clientId">the client that is processing this event (the server will receive all of these events for every client and itself)</param>
        /// <param name="sceneName">the scene name pertaining to this event</param>
        public delegate void OnUnloadCompleteDelegateHandler(ulong clientId, string sceneName);

        /// <summary>
        /// Delegate declaration for the OnSynchronizeComplete event.<br />
        /// See also:<br />
        /// <see cref="SceneEventType.SynchronizeComplete"/> for more information
        /// </summary>
        /// <param name="clientId">the client that completed this event</param>
        public delegate void OnSynchronizeCompleteDelegateHandler(ulong clientId);

        /// <summary>
        /// Invoked when a <see cref="SceneEventType.Load"/> event is started by the server.<br />
        /// <em>Note: The server and connected client(s) will always receive this notification.</em><br />
        /// <em>*** Do not start new scene events within scene event notification callbacks.</em><br />
        /// </summary>
        public event OnLoadDelegateHandler OnLoad;

        /// <summary>
        /// Invoked when a <see cref="SceneEventType.Unload"/> event is started by the server.<br />
        /// <em>Note: The server and connected client(s) will always receive this notification.</em><br />
        /// <em>*** Do not start new scene events within scene event notification callbacks.</em><br />
        /// </summary>
        public event OnUnloadDelegateHandler OnUnload;

        /// <summary>
        /// Invoked when a <see cref="SceneEventType.Synchronize"/> event is started by the server
        /// after a client is approved for connection in order to synchronize the client with the currently loaded
        /// scenes and NetworkObjects.  This event signifies the beginning of the synchronization event.<br />
        /// <em>Note: The server and connected client(s) will always receive this notification.
        /// This event is generated on a per newly connected and approved client basis.</em><br />
        /// <em>*** Do not start new scene events within scene event notification callbacks.</em><br />
        /// </summary>
        public event OnSynchronizeDelegateHandler OnSynchronize;

        /// <summary>
        /// Invoked when a <see cref="SceneEventType.LoadEventCompleted"/> event is generated by the server.
        /// This event signifies the end of an existing <see cref="SceneEventType.Load"/> event as it pertains
        /// to all clients connected when the event was started.  This event signifies that all clients (and server) have
        /// finished the <see cref="SceneEventType.Load"/> event.<br />
        /// <em>Note: this is useful to know when all clients have loaded the same scene (single or additive mode)</em><br />
        /// <em>*** Do not start new scene events within scene event notification callbacks.</em><br />
        /// </summary>
        public event OnEventCompletedDelegateHandler OnLoadEventCompleted;

        /// <summary>
        /// Invoked when a <see cref="SceneEventType.UnloadEventCompleted"/> event is generated by the server.
        /// This event signifies the end of an existing <see cref="SceneEventType.Unload"/> event as it pertains
        /// to all clients connected when the event was started.  This event signifies that all clients (and server) have
        /// finished the <see cref="SceneEventType.Unload"/> event.<br />
        /// <em>Note: this is useful to know when all clients have unloaded a specific scene.  The <see cref="LoadSceneMode"/> will
        /// always be <see cref="LoadSceneMode.Additive"/> for this event.</em><br />
        /// <em>*** Do not start new scene events within scene event notification callbacks.</em><br />
        /// </summary>
        public event OnEventCompletedDelegateHandler OnUnloadEventCompleted;

        /// <summary>
        /// Invoked when a <see cref="SceneEventType.LoadComplete"/> event is generated by a client or server.<br />
        /// <em>Note: The server receives this message from all clients (including itself).
        /// Each client receives their own notification sent to the server.</em><br />
        /// <em>*** Do not start new scene events within scene event notification callbacks.</em><br />
        /// </summary>
        public event OnLoadCompleteDelegateHandler OnLoadComplete;

        /// <summary>
        /// Invoked when a <see cref="SceneEventType.UnloadComplete"/> event is generated by a client or server.<br />
        /// <em>Note: The server receives this message from all clients (including itself).
        /// Each client receives their own notification sent to the server.</em><br />
        /// <em>*** Do not start new scene events within scene event notification callbacks.</em><br />
        /// </summary>
        public event OnUnloadCompleteDelegateHandler OnUnloadComplete;

        /// <summary>
        /// Invoked when a <see cref="SceneEventType.SynchronizeComplete"/> event is generated by a client. <br />
        /// <em> Note: The server receives this message from the client, but will never generate this event for itself.
        /// Each client receives their own notification sent to the server.  This is useful to know that a client has
        /// completed the entire connection sequence, loaded all scenes, and synchronized all NetworkObjects.</em>
        /// <em>*** Do not start new scene events within scene event notification callbacks.</em><br />
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
        /// server or client loads a scene during an active netcode game session.
        /// </summary>
        /// <remarks>
        /// <b>Client Side:</b> In order for clients to be notified of this condition you must assign the <see cref="VerifySceneBeforeLoading"/> delegate handler.<br />
        /// <b>Server Side:</b> <see cref="LoadScene(string, LoadSceneMode)"/> will return <see cref="SceneEventProgressStatus"/>.
        /// </remarks>
        public VerifySceneBeforeLoadingDelegateHandler VerifySceneBeforeLoading;

        /// <summary>
        /// Delegate declaration for the <see cref="VerifySceneBeforeUnloading"/> handler that provides
        /// an additional level of scene unloading validation to assure the scene being unloaded should
        /// be unloaded.
        /// </summary>
        /// <param name="scene">The scene to be unloaded</param>
        /// <returns>true (valid) or false (not valid)</returns>
        public delegate bool VerifySceneBeforeUnloadingDelegateHandler(Scene scene);

        /// <summary>
        /// Client Side Only: <br />
        /// Delegate handler defined by <see cref="VerifySceneBeforeUnloadingDelegateHandler"/> that is only invoked when the client
        /// is finished synchronizing and when <see cref="ClientSynchronizationMode"/> is set to <see cref="LoadSceneMode.Additive"/>.
        /// </summary>
        public VerifySceneBeforeUnloadingDelegateHandler VerifySceneBeforeUnloading;

        /// <summary>
        /// When enabled and <see cref="ClientSynchronizationMode"/> is <see cref="LoadSceneMode.Additive"/>, any scenes not synchronized with
        /// the server will be unloaded unless <see cref="VerifySceneBeforeUnloading"/> returns true. This provides more granular control over
        /// which already loaded client-side scenes not synchronized with the server should be unloaded.
        /// </summary>
        /// <remarks>
        /// If the <see cref="VerifySceneBeforeUnloading"/> delegate callback is not set then any scene loaded on the just synchronized client
        /// will be unloaded.
        /// One scenario is a synchronized client is disconnected for unexpected reasons and attempts to reconnect to the same network session
        /// but still has all scenes that were loaded through server synchronization (initially or through scene events). However, during the
        /// client disconnection period the server unloads one (or more) of the scenes loaded and as such the reconnecting client could still
        /// have the now unloaded scenes still loaded. Enabling this flag coupled with assignment of the assignment of the <see cref="VerifySceneBeforeUnloading"/>
        /// delegate callback provides you with the ability to keep scenes loaded by the client (i.e. UI etc) while discarding any artifact
        /// scenes that no longer need to be loaded.
        /// </remarks>
        public bool PostSynchronizationSceneUnloading;

        private bool m_ActiveSceneSynchronizationEnabled;
        /// <summary>
        /// When enabled, the server or host will synchronize clients with changes to the currently active scene
        /// </summary>
        public bool ActiveSceneSynchronizationEnabled
        {
            get
            {
                return m_ActiveSceneSynchronizationEnabled;
            }
            set
            {
                if (m_ActiveSceneSynchronizationEnabled != value)
                {
                    m_ActiveSceneSynchronizationEnabled = value;
                    if (m_ActiveSceneSynchronizationEnabled)
                    {
                        SceneManager.activeSceneChanged += SceneManager_ActiveSceneChanged;
                    }
                    else
                    {
                        SceneManager.activeSceneChanged -= SceneManager_ActiveSceneChanged;
                    }
                }
            }
        }

        /// <summary>
        /// The SceneManagerHandler implementation
        /// </summary>
        internal ISceneManagerHandler SceneManagerHandler = new DefaultSceneManagerHandler();

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
        internal Dictionary<int, int> ClientSceneHandleToServerSceneHandle = new Dictionary<int, int>();

        /// <summary>
        /// Add the client to server (and vice versa) scene handle lookup.
        /// Add the client-side handle to scene entry in the HandleToScene table.
        /// If it fails (i.e. already added) it returns false.
        /// </summary>
        internal bool UpdateServerClientSceneHandle(int serverHandle, int clientHandle, Scene localScene)
        {
            if (!ServerSceneHandleToClientSceneHandle.ContainsKey(serverHandle))
            {
                ServerSceneHandleToClientSceneHandle.Add(serverHandle, clientHandle);
            }
            else
            {
                return false;
            }

            if (!ClientSceneHandleToServerSceneHandle.ContainsKey(clientHandle))
            {
                ClientSceneHandleToServerSceneHandle.Add(clientHandle, serverHandle);
            }
            else
            {
                return false;
            }

            // It is "Ok" if this already has an entry
            if (!ScenesLoaded.ContainsKey(clientHandle))
            {
                ScenesLoaded.Add(clientHandle, localScene);
            }

            return true;
        }

        /// <summary>
        /// Removes the client to server (and vice versa) scene handles.
        /// If it fails (i.e. already removed) it returns false.
        /// </summary>
        internal bool RemoveServerClientSceneHandle(int serverHandle, int clientHandle)
        {
            if (ServerSceneHandleToClientSceneHandle.ContainsKey(serverHandle))
            {
                ServerSceneHandleToClientSceneHandle.Remove(serverHandle);
            }
            else
            {
                return false;
            }

            if (ClientSceneHandleToServerSceneHandle.ContainsKey(clientHandle))
            {
                ClientSceneHandleToServerSceneHandle.Remove(clientHandle);
            }
            else
            {
                return false;
            }

            if (ScenesLoaded.ContainsKey(clientHandle))
            {
                ScenesLoaded.Remove(clientHandle);
            }
            else
            {
                return false;
            }

            return true;
        }

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

        internal readonly NetworkManager NetworkManager;

        // Keep track of this scene until the NetworkSceneManager is destroyed.
        internal Scene DontDestroyOnLoadScene;

        /// <summary>
        /// This setting changes how clients handle scene loading when initially synchronizing with the server.<br />
        /// See: <see cref="SetClientSynchronizationMode(LoadSceneMode)"/>
        /// </summary>
        /// <remarks>
        /// <b>LoadSceneMode.Single:</b> All currently loaded scenes on the client will be unloaded and the
        /// server's currently active scene will be loaded in single mode on the client unless it was already
        /// loaded.<br />
        /// <b>LoadSceneMode.Additive:</b> All currently loaded scenes are left as they are and any newly loaded
        /// scenes will be loaded additively.  Users need to determine which scenes are valid to load via the
        /// <see cref="VerifySceneBeforeLoading"/> and, if <see cref="PostSynchronizationSceneUnloading"/> is
        /// set, <see cref="VerifySceneBeforeUnloading"/> callback(s).
        /// </remarks>
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
            // Always assure we no longer listen to scene changes when disposed.
            SceneManager.activeSceneChanged -= SceneManager_ActiveSceneChanged;
            SceneUnloadEventHandler.Shutdown();
            foreach (var keypair in SceneEventDataStore)
            {
                if (NetworkLog.CurrentLogLevel == LogLevel.Developer)
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
            var sceneEventData = new SceneEventData(NetworkManager);
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
        /// Used for integration tests, normal runtime mode this will always be LoadSceneMode.Single
        /// </summary>
        internal LoadSceneMode DeferLoadingFilter = LoadSceneMode.Single;
        /// <summary>
        /// Determines if a remote client should defer object creation initiated by CreateObjectMessage
        /// until a scene event is completed.
        /// </summary>
        /// <remarks>
        /// Deferring object creation should only occur when there is a possibility the objects could be
        /// instantiated in a currently active scene that will be unloaded during single mode scene loading
        /// to prevent the newly created objects from being destroyed when the scene is unloaded.
        /// </remarks>
        internal bool ShouldDeferCreateObject()
        {
            // This applies only to remote clients and when scene management is enabled
            if (!NetworkManager.NetworkConfig.EnableSceneManagement || NetworkManager.IsServer)
            {
                return false;
            }
            var synchronizeEventDetected = false;
            var loadingEventDetected = false;
            foreach (var entry in SceneEventDataStore)
            {
                if (entry.Value.SceneEventType == SceneEventType.Synchronize)
                {
                    synchronizeEventDetected = true;
                }

                // When loading a scene and the load scene mode is single we should defer object creation
                if (entry.Value.SceneEventType == SceneEventType.Load && entry.Value.LoadSceneMode == DeferLoadingFilter)
                {
                    loadingEventDetected = true;
                }
            }

            // Synchronizing while in client synchronization mode single --> Defer
            // When not synchronizing but loading a scene in single mode --> Defer
            return (synchronizeEventDetected && ClientSynchronizationMode == LoadSceneMode.Single) || (!synchronizeEventDetected && loadingEventDetected);
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
            // TODO 2023: We could support addressable or asset bundle scenes by
            // adding a method that would allow users to add scenes to this.
            // The method would be server-side only and require an additional SceneEventType
            // that would be used to notify clients of the added scene. This might need
            // to include information about the addressable or asset bundle (i.e. address to load assets)
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
                // In the event there is no scene associated with the scene event then just return "No Scene"
                // This can happen during unit tests when clients first connect and the only scene loaded is the
                // unit test scene (which is ignored by default) that results in a scene event that has no associated
                // scene.  Under this specific special case, we just return "No Scene".
                if (sceneHash == 0)
                {
                    return "No Scene";
                }
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
        /// This setting changes how clients handle scene loading when initially synchronizing with the server.<br />
        /// The server or host should set this value as clients will automatically be synchronized with the server (or host) side.
        /// </summary>
        /// <remarks>
        /// <b>LoadSceneMode.Single:</b> All currently loaded scenes on the client will be unloaded and the
        /// server's currently active scene will be loaded in single mode on the client unless it was already
        /// loaded.<br />
        /// <b>LoadSceneMode.Additive:</b> All currently loaded scenes are left as they are and any newly loaded
        /// scenes will be loaded additively.  Users need to determine which scenes are valid to load via the
        /// <see cref="VerifySceneBeforeLoading"/> and, if <see cref="PostSynchronizationSceneUnloading"/> is
        /// set, <see cref="VerifySceneBeforeUnloading"/> callback(s).
        /// </remarks>
        /// <param name="mode"><see cref="LoadSceneMode"/> for initial client synchronization</param>
        public void SetClientSynchronizationMode(LoadSceneMode mode)
        {
            var networkManager = NetworkManager;
            SceneManagerHandler.SetClientSynchronizationMode(ref networkManager, mode);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="networkManager">one <see cref="Netcode.NetworkManager"/> instance per <see cref="NetworkSceneManager"/> instance</param>
        /// <param name="sceneEventDataPoolSize">maximum <see cref="SceneEventData"/> pool size</param>
        internal NetworkSceneManager(NetworkManager networkManager)
        {
            NetworkManager = networkManager;
            SceneEventDataStore = new Dictionary<uint, SceneEventData>();

            // Generates the scene name to hash value
            GenerateScenesInBuild();

            // Since NetworkManager is now always migrated to the DDOL we will use this to get the DDOL scene
            DontDestroyOnLoadScene = networkManager.gameObject.scene;

            // Since the server tracks loaded scenes, we need to add any currently loaded scenes on the
            // server side when the NetworkManager is started and NetworkSceneManager instantiated when
            // scene management is enabled.
            if (networkManager.IsServer && networkManager.NetworkConfig.EnableSceneManagement)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var loadedScene = SceneManager.GetSceneAt(i);
                    ScenesLoaded.Add(loadedScene.handle, loadedScene);
                }
                SceneManagerHandler.PopulateLoadedScenes(ref ScenesLoaded, NetworkManager);
            }

            // Add to the server to client scene handle table
            UpdateServerClientSceneHandle(DontDestroyOnLoadScene.handle, DontDestroyOnLoadScene.handle, DontDestroyOnLoadScene);
        }

        /// <summary>
        /// Synchronizes clients when the currently active scene is changed
        /// </summary>
        private void SceneManager_ActiveSceneChanged(Scene current, Scene next)
        {
            // If no clients are connected, then don't worry about notifications
            if (!(NetworkManager.ConnectionManager.ConnectedClientIds.Count > (NetworkManager.IsHost ? 1 : 0)))
            {
                return;
            }

            // Don't notify if a scene event is in progress
            foreach (var sceneEventEntry in SceneEventProgressTracking)
            {
                if (!sceneEventEntry.Value.HasTimedOut() && sceneEventEntry.Value.Status == SceneEventProgressStatus.Started)
                {
                    return;
                }
            }

            // If the scene's build index is in the hash table
            if (BuildIndexToHash.ContainsKey(next.buildIndex))
            {
                // Notify clients of the change in active scene
                var sceneEvent = BeginSceneEvent();
                sceneEvent.SceneEventType = SceneEventType.ActiveSceneChanged;
                sceneEvent.ActiveSceneHash = BuildIndexToHash[next.buildIndex];
                SendSceneEventData(sceneEvent.SceneEventId, NetworkManager.ConnectionManager.ConnectedClientIds.Where(c => c != NetworkManager.ServerClientId).ToArray());
                EndSceneEvent(sceneEvent.SceneEventId);
            }
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
            var sceneName = SceneNameFromHash(sceneHash);
            var sceneIndex = SceneUtility.GetBuildIndexByScenePath(sceneName);
            return ValidateSceneBeforeLoading(sceneIndex, sceneName, loadSceneMode);
        }

        /// <summary>
        /// Overloaded version that is invoked by <see cref="ValidateSceneBeforeLoading"/> and <see cref="SynchronizeNetworkObjects"/>.
        /// This specifically is to allow runtime generated scenes to be excluded by the server during synchronization.
        /// </summary>
        internal bool ValidateSceneBeforeLoading(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode)
        {
            var validated = true;
            if (VerifySceneBeforeLoading != null)
            {
                validated = VerifySceneBeforeLoading.Invoke(sceneIndex, sceneName, loadSceneMode);
            }
            if (!validated && !m_DisableValidationWarningMessages)
            {
                var serverHostorClient = "Client";
                if (NetworkManager.IsServer)
                {
                    serverHostorClient = NetworkManager.IsHost ? "Host" : "Server";
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
                            SceneManagerHandler.StartTrackingScene(sceneLoaded, true, NetworkManager);
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
                    SceneBeingSynchronized = NetworkManager.gameObject.scene;
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
            var size = NetworkManager.ConnectionManager.SendMessage(ref message, k_DeliveryType, targetClientIds);

            NetworkManager.NetworkMetrics.TrackSceneEventSent(targetClientIds, (uint)SceneEventDataStore[sceneEventId].SceneEventType, SceneNameFromHash(SceneEventDataStore[sceneEventId].SceneHash), size);
        }

        /// <summary>
        /// Entry method for scene unloading validation
        /// </summary>
        /// <param name="scene">the scene to be unloaded</param>
        /// <returns></returns>
        private SceneEventProgress ValidateSceneEventUnloading(Scene scene)
        {
            if (!NetworkManager.NetworkConfig.EnableSceneManagement)
            {
                Debug.LogWarning($"{nameof(LoadScene)} was called, but {nameof(NetworkConfig.EnableSceneManagement)} was not enabled! Enable {nameof(NetworkConfig.EnableSceneManagement)} prior to starting a client, host, or server prior to using {nameof(NetworkSceneManager)}!");
                return new SceneEventProgress(null, SceneEventProgressStatus.SceneManagementNotEnabled);
            }

            if (!NetworkManager.IsServer)
            {
                Debug.LogWarning($"[{nameof(SceneEventProgressStatus.ServerOnlyAction)}][Unload] Clients cannot invoke the {nameof(UnloadScene)} method!");
                return new SceneEventProgress(null, SceneEventProgressStatus.ServerOnlyAction);
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
            if (!NetworkManager.NetworkConfig.EnableSceneManagement)
            {
                Debug.LogWarning($"{nameof(LoadScene)} was called, but {nameof(NetworkConfig.EnableSceneManagement)} was not enabled! Enable {nameof(NetworkConfig.EnableSceneManagement)} prior to starting a client, host, or server prior to using {nameof(NetworkSceneManager)}!");
                return new SceneEventProgress(null, SceneEventProgressStatus.SceneManagementNotEnabled);
            }

            if (!NetworkManager.IsServer)
            {
                Debug.LogWarning($"[{nameof(SceneEventProgressStatus.ServerOnlyAction)}][Load] Clients cannot invoke the {nameof(LoadScene)} method!");
                return new SceneEventProgress(null, SceneEventProgressStatus.ServerOnlyAction);
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

            var sceneEventProgress = new SceneEventProgress(NetworkManager)
            {
                SceneHash = SceneHashFromNameOrPath(sceneName)
            };

            SceneEventProgressTracking.Add(sceneEventProgress.Guid, sceneEventProgress);

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
            var size = NetworkManager.ConnectionManager.SendMessage(ref message, k_DeliveryType, NetworkManager.ConnectionManager.ConnectedClientIds);

            NetworkManager.NetworkMetrics.TrackSceneEventSent(
                NetworkManager.ConnectionManager.ConnectedClientIds,
                (uint)sceneEventProgress.SceneEventType,
                SceneNameFromHash(sceneEventProgress.SceneHash),
                size);

            // Send a local notification to the server that all clients are done loading or unloading
            InvokeSceneEvents(NetworkManager.ServerClientId, sceneEventData);

            EndSceneEvent(sceneEventData.SceneEventId);
            return true;
        }

        /// <summary>
        /// <b>Server Side:</b>
        /// Unloads an additively loaded scene.  If you want to unload a <see cref="LoadSceneMode.Single"/> mode loaded scene load another <see cref="LoadSceneMode.Single"/> scene.
        /// When applicable, the <see cref="AsyncOperation"/> is delivered within the <see cref="SceneEvent"/> via the <see cref="OnSceneEvent"/>
        /// </summary>
        /// <param name="scene">The Unity Scene to be unloaded</param>
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

            var sceneEventProgress = ValidateSceneEventUnloading(scene);
            if (sceneEventProgress.Status != SceneEventProgressStatus.Started)
            {
                return sceneEventProgress.Status;
            }

            if (!ScenesLoaded.ContainsKey(sceneHandle))
            {
                Debug.LogError($"{nameof(UnloadScene)} internal error! {sceneName} with handle {scene.handle} is not within the internal scenes loaded dictionary!");
                return SceneEventProgressStatus.InternalNetcodeError;
            }
            sceneEventProgress.LoadSceneMode = LoadSceneMode.Additive;

            // Any NetworkObjects marked to not be destroyed with a scene and reside within the scene about to be unloaded
            // should be migrated temporarily into the DDOL, once the scene is unloaded they will be migrated into the
            // currently active scene.
            var networkManager = NetworkManager;
            SceneManagerHandler.MoveObjectsFromSceneToDontDestroyOnLoad(ref networkManager, scene);

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
            InvokeSceneEvents(NetworkManager.ServerClientId, sceneEventData, sceneUnload);

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

            var scene = ScenesLoaded[sceneHandle];
            // Any NetworkObjects marked to not be destroyed with a scene and reside within the scene about to be unloaded
            // should be migrated temporarily into the DDOL, once the scene is unloaded they will be migrated into the
            // currently active scene.
            var networkManager = NetworkManager;
            SceneManagerHandler.MoveObjectsFromSceneToDontDestroyOnLoad(ref networkManager, scene);

            m_IsSceneEventActive = true;
            var sceneEventProgress = new SceneEventProgress(NetworkManager)
            {
                SceneEventId = sceneEventData.SceneEventId,
                OnSceneEventCompleted = OnSceneUnloaded
            };
            var sceneUnload = SceneManagerHandler.UnloadSceneAsync(scene, sceneEventProgress);

            SceneManagerHandler.StopTrackingScene(sceneHandle, sceneName, NetworkManager);

            // Remove our server to scene handle lookup
            if (!RemoveServerClientSceneHandle(sceneEventData.SceneHandle, sceneHandle))
            {
                // If the exact same handle exists then there are problems with using handles
                throw new Exception($"Failed to remove server scene handle ({sceneEventData.SceneHandle}) or client scene handle({sceneHandle})! Happened during scene unload for {sceneName}.");
            }

            // The only scenes unloaded are scenes that were additively loaded
            sceneEventData.LoadSceneMode = LoadSceneMode.Additive;

            // Notify the local client that a scene is going to be unloaded
            InvokeSceneEvents(NetworkManager.LocalClientId, sceneEventData, sceneUnload);
        }

        /// <summary>
        /// Server and Client:
        /// Invoked when an additively loaded scene is unloaded
        /// </summary>
        private void OnSceneUnloaded(uint sceneEventId)
        {
            // If we are shutdown or about to shutdown, then ignore this event
            if (!NetworkManager.IsListening || NetworkManager.ShutdownInProgress)
            {
                return;
            }

            // Migrate the NetworkObjects marked to not be destroyed with the scene into the currently active scene
            MoveObjectsFromDontDestroyOnLoadToScene(SceneManager.GetActiveScene());

            var sceneEventData = SceneEventDataStore[sceneEventId];
            // First thing we do, if we are a server, is to send the unload scene event.
            if (NetworkManager.IsServer)
            {
                // Server sends the unload scene notification after unloading because it will despawn all scene relative in-scene NetworkObjects
                // If we send this event to all clients before the server is finished unloading they will get warning about an object being
                // despawned that no longer exists
                SendSceneEventData(sceneEventId, NetworkManager.ConnectionManager.ConnectedClientIds.Where(c => c != NetworkManager.ServerClientId).ToArray());

                //Only if we are a host do we want register having loaded for the associated SceneEventProgress
                if (SceneEventProgressTracking.ContainsKey(sceneEventData.SceneEventProgressId) && NetworkManager.IsHost)
                {
                    SceneEventProgressTracking[sceneEventData.SceneEventProgressId].ClientFinishedSceneEvent(NetworkManager.ServerClientId);
                }
            }

            // Next we prepare to send local notifications for unload complete
            sceneEventData.SceneEventType = SceneEventType.UnloadComplete;

            //Notify the client or server that a scene was unloaded
            var client = NetworkManager.IsServer ? NetworkManager.ServerClientId : NetworkManager.LocalClientId;
            InvokeSceneEvents(client, sceneEventData);

            // Clients send a notification back to the server they have completed the unload scene event
            if (!NetworkManager.IsServer)
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
                    var sceneEventProgress = new SceneEventProgress(NetworkManager)
                    {
                        SceneEventId = sceneEventId,
                        OnSceneEventCompleted = EmptySceneUnloadedOperation
                    };
                    var sceneUnload = SceneManagerHandler.UnloadSceneAsync(keyHandleEntry.Value, sceneEventProgress);
                    SceneUnloadEventHandler.RegisterScene(this, keyHandleEntry.Value, LoadSceneMode.Additive, sceneUnload);
                }
            }
            // clear out our scenes loaded list
            ScenesLoaded.Clear();
            SceneManagerHandler.ClearSceneTracking(NetworkManager);
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
            // LoadScene can be called with either a sceneName or a scenePath. Ensure that sceneName is correct at this point
            sceneName = SceneNameFromHash(sceneEventData.SceneHash);
            // This both checks to make sure the scene is valid and if not resets the active scene event
            m_IsSceneEventActive = ValidateSceneBeforeLoading(sceneEventData.SceneHash, loadSceneMode);
            if (!m_IsSceneEventActive)
            {
                EndSceneEvent(sceneEventId);
                return SceneEventProgressStatus.SceneFailedVerification;
            }

            if (sceneEventData.LoadSceneMode == LoadSceneMode.Single)
            {
                // The Condition: While a scene is asynchronously loaded in single loading scene mode, if any new NetworkObjects are spawned
                // they need to be moved into the do not destroy temporary scene
                // When it is set: Just before starting the asynchronous loading call
                // When it is unset: After the scene has loaded, the PopulateScenePlacedObjects is called, and all NetworkObjects in the do
                // not destroy temporary scene are moved into the active scene
                IsSpawnedObjectsPendingInDontDestroyOnLoad = true;

                // Destroy current scene objects before switching.
                NetworkManager.SpawnManager.ServerDestroySpawnedSceneObjects();

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
            InvokeSceneEvents(NetworkManager.ServerClientId, sceneEventData, sceneLoad);

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
                var networkManager = networkSceneManager.NetworkManager;
                if (!s_Instances.ContainsKey(networkManager))
                {
                    s_Instances.Add(networkManager, new List<SceneUnloadEventHandler>());
                }
                var clientId = networkManager.IsServer ? NetworkManager.ServerClientId : networkManager.LocalClientId;
                s_Instances[networkManager].Add(new SceneUnloadEventHandler(networkSceneManager, scene, clientId, loadSceneMode, asyncOperation));
            }

            private static void SceneUnloadComplete(SceneUnloadEventHandler sceneUnloadEventHandler)
            {
                if (sceneUnloadEventHandler == null || sceneUnloadEventHandler.m_NetworkSceneManager == null || sceneUnloadEventHandler.m_NetworkSceneManager.NetworkManager == null)
                {
                    return;
                }
                var networkManager = sceneUnloadEventHandler.m_NetworkSceneManager.NetworkManager;
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
                    if (m_NetworkSceneManager != null && m_NetworkSceneManager.NetworkManager != null)
                    {
                        m_NetworkSceneManager.OnSceneEvent?.Invoke(new SceneEvent()
                        {
                            AsyncOperation = m_AsyncOperation,
                            SceneEventType = SceneEventType.UnloadComplete,
                            SceneName = m_Scene.name,
                            ScenePath = m_Scene.path,
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
                    ScenePath = m_Scene.path,
                    LoadSceneMode = m_LoadSceneMode,
                    ClientId = clientId
                });

                m_NetworkSceneManager.OnUnload?.Invoke(networkSceneManager.NetworkManager.LocalClientId, m_Scene.name, null);
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
            var sceneEventProgress = new SceneEventProgress(NetworkManager)
            {
                SceneEventId = sceneEventId,
                OnSceneEventCompleted = OnSceneLoaded
            };
            var sceneLoad = SceneManagerHandler.LoadSceneAsync(sceneName, sceneEventData.LoadSceneMode, sceneEventProgress);

            InvokeSceneEvents(NetworkManager.LocalClientId, sceneEventData, sceneLoad);
        }

        /// <summary>
        /// Client and Server:
        /// Generic on scene loaded callback method to be called upon a scene loading
        /// </summary>
        private void OnSceneLoaded(uint sceneEventId)
        {
            // If we are shutdown or about to shutdown, then ignore this event
            if (!NetworkManager.IsListening || NetworkManager.ShutdownInProgress)
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

            if (NetworkManager.IsServer)
            {
                OnServerLoadedScene(sceneEventId, nextScene);
            }
            else
            {
                // For the client, we make a server scene handle to client scene handle look up table
                if (!UpdateServerClientSceneHandle(sceneEventData.SceneHandle, nextScene.handle, nextScene))
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
                        NetworkManager.SpawnManager.SpawnNetworkObjectLocally(keyValuePairBySceneHandle.Value,
                            NetworkManager.SpawnManager.GetNetworkObjectId(), true, false, NetworkManager.ServerClientId, true);
                    }
                }
            }

            foreach (var keyValuePairByGlobalObjectIdHash in ScenePlacedObjects)
            {
                foreach (var keyValuePairBySceneHandle in keyValuePairByGlobalObjectIdHash.Value)
                {
                    if (!keyValuePairBySceneHandle.Value.IsPlayerObject)
                    {
                        keyValuePairBySceneHandle.Value.InternalInSceneNetworkObjectsSpawned();
                    }
                }
            }

            // Add any despawned when spawned in-scene placed NetworkObjects to the scene event data
            sceneEventData.AddDespawnedInSceneNetworkObjects();

            // Set the server's scene's handle so the client can build a look up table
            sceneEventData.SceneHandle = scene.handle;

            // Send all clients the scene load event
            for (int j = 0; j < NetworkManager.ConnectedClientsList.Count; j++)
            {
                var clientId = NetworkManager.ConnectedClientsList[j].ClientId;
                if (clientId != NetworkManager.ServerClientId)
                {
                    sceneEventData.TargetClientId = clientId;
                    var message = new SceneEventMessage
                    {
                        EventData = sceneEventData
                    };
                    var size = NetworkManager.ConnectionManager.SendMessage(ref message, k_DeliveryType, clientId);
                    NetworkManager.NetworkMetrics.TrackSceneEventSent(clientId, (uint)sceneEventData.SceneEventType, scene.name, size);
                }
            }

            m_IsSceneEventActive = false;
            sceneEventData.SceneEventType = SceneEventType.LoadComplete;
            //First, notify local server that the scene was loaded
            InvokeSceneEvents(NetworkManager.ServerClientId, sceneEventData, scene: scene);

            //Second, only if we are a host do we want register having loaded for the associated SceneEventProgress
            if (SceneEventProgressTracking.ContainsKey(sceneEventData.SceneEventProgressId) && NetworkManager.IsHost)
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

            // Process any pending create object messages that the client received while loading a scene
            ProcessDeferredCreateObjectMessages();

            // Notify local client that the scene was loaded
            InvokeSceneEvents(NetworkManager.LocalClientId, sceneEventData, scene: scene);

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
            NetworkManager.SpawnManager.UpdateObservedNetworkObjects(clientId);

            var sceneEventData = BeginSceneEvent();
            sceneEventData.ClientSynchronizationMode = ClientSynchronizationMode;
            sceneEventData.InitializeForSynch();
            sceneEventData.TargetClientId = clientId;
            sceneEventData.LoadSceneMode = ClientSynchronizationMode;
            var activeScene = SceneManager.GetActiveScene();
            sceneEventData.SceneEventType = SceneEventType.Synchronize;
            if (BuildIndexToHash.ContainsKey(activeScene.buildIndex))
            {
                sceneEventData.ActiveSceneHash = BuildIndexToHash[activeScene.buildIndex];
            }

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

                if (scene == DontDestroyOnLoadScene)
                {
                    continue;
                }

                // This would depend upon whether we are additive or not
                // If we are the base scene, then we set the root scene index;
                if (activeScene == scene)
                {
                    if (!ValidateSceneBeforeLoading(scene.buildIndex, scene.name, sceneEventData.LoadSceneMode))
                    {
                        continue;
                    }
                    sceneEventData.SceneHash = SceneHashFromNameOrPath(scene.path);
                    sceneEventData.SceneHandle = scene.handle;
                }
                else if (!ValidateSceneBeforeLoading(scene.buildIndex, scene.name, LoadSceneMode.Additive))
                {
                    continue;
                }
                sceneEventData.AddSceneToSynchronize(SceneHashFromNameOrPath(scene.path), scene.handle);
            }

            sceneEventData.AddSpawnedNetworkObjects();
            sceneEventData.AddDespawnedInSceneNetworkObjects();

            var message = new SceneEventMessage
            {
                EventData = sceneEventData
            };
            var size = NetworkManager.ConnectionManager.SendMessage(ref message, k_DeliveryType, clientId);
            NetworkManager.NetworkMetrics.TrackSceneEventSent(clientId, (uint)sceneEventData.SceneEventType, "", size);

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
                    ClientId = NetworkManager.LocalClientId,
                });

                OnSynchronize?.Invoke(NetworkManager.LocalClientId);
            }

            // Always check to see if the scene needs to be validated
            if (!ValidateSceneBeforeLoading(sceneHash, loadSceneMode))
            {
                HandleClientSceneEvent(sceneEventId);
                if (NetworkManager.LogLevel == LogLevel.Developer)
                {
                    NetworkLog.LogInfo($"Client declined to load the scene {sceneName}, continuing with synchronization.");
                }
                return;
            }

            var sceneLoad = (AsyncOperation)null;

            // Determines if the client has the scene to be loaded already loaded, if so will return true and the client will skip loading this scene
            // For ClientSynchronizationMode LoadSceneMode.Single, we pass in whether the scene being loaded is the first/primary active scene and if it is already loaded
            // it should pass through to post load processing (ClientLoadedSynchronization).
            // For ClientSynchronizationMode LoadSceneMode.Additive, if the scene is already loaded or the active scene is the scene to be loaded (does not require it to
            // be the initial primary scene) then go ahead and pass through to post load processing (ClientLoadedSynchronization).
            var shouldPassThrough = SceneManagerHandler.ClientShouldPassThrough(sceneName, sceneHash == sceneEventData.SceneHash, ClientSynchronizationMode, NetworkManager);

            if (!shouldPassThrough)
            {
                // If not, then load the scene
                var sceneEventProgress = new SceneEventProgress(NetworkManager)
                {
                    SceneEventId = sceneEventId,
                    OnSceneEventCompleted = ClientLoadedSynchronization
                };
                sceneLoad = SceneManagerHandler.LoadSceneAsync(sceneName, loadSceneMode, sceneEventProgress);

                // Notify local client that a scene load has begun
                OnSceneEvent?.Invoke(new SceneEvent()
                {
                    AsyncOperation = sceneLoad,
                    SceneEventType = SceneEventType.Load,
                    LoadSceneMode = loadSceneMode,
                    SceneName = sceneName,
                    ScenePath = ScenePathFromHash(sceneHash),
                    ClientId = NetworkManager.LocalClientId,
                });

                OnLoad?.Invoke(NetworkManager.LocalClientId, sceneName, loadSceneMode, sceneLoad);
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
            var nextScene = SceneManagerHandler.GetSceneFromLoadedScenes(sceneName, NetworkManager);
            if (!nextScene.IsValid())
            {
                nextScene = GetAndAddNewlyLoadedSceneByName(sceneName);
            }

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

            // For the client, we make a server scene handle to client scene handle look up table
            if (!UpdateServerClientSceneHandle(sceneEventData.NetworkSceneHandle, nextScene.handle, nextScene))
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
            var size = NetworkManager.ConnectionManager.SendMessage(ref message, k_DeliveryType, NetworkManager.ServerClientId);

            NetworkManager.NetworkMetrics.TrackSceneEventSent(NetworkManager.ServerClientId, (uint)responseSceneEventData.SceneEventType, sceneName, size);

            EndSceneEvent(responseSceneEventData.SceneEventId);

            // Send notification to local client that the scene has finished loading
            InvokeSceneEvents(NetworkManager.LocalClientId, responseSceneEventData, scene: nextScene);

            // Check to see if we still have scenes to load and synchronize with
            HandleClientSceneEvent(sceneEventId);
        }

        /// <summary>
        /// Makes sure that client-side instantiated dynamically spawned NetworkObjects are migrated
        /// into the same scene (if not already) as they are on the server-side during the initial
        /// client connection synchronization process.
        /// </summary>
        private void SynchronizeNetworkObjectScene()
        {
            foreach (var networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                // This is only done for dynamically spawned NetworkObjects
                // Theoretically, a server could have NetworkObjects in a server-side only scene, if the client doesn't have that scene loaded
                // then skip it (it will reside in the currently active scene in this scenario on the client-side)
                if (networkObject.IsSceneObject.Value == false && ServerSceneHandleToClientSceneHandle.ContainsKey(networkObject.NetworkSceneHandle))
                {
                    networkObject.SceneOriginHandle = ServerSceneHandleToClientSceneHandle[networkObject.NetworkSceneHandle];



                    // If the NetworkObject does not have a parent and is not in the same scene as it is on the server side, then find the right scene
                    // and move it to that scene.
                    if (networkObject.gameObject.scene.handle != networkObject.SceneOriginHandle && networkObject.transform.parent == null)
                    {
                        if (ScenesLoaded.ContainsKey(networkObject.SceneOriginHandle))
                        {
                            var scene = ScenesLoaded[networkObject.SceneOriginHandle];
                            if (scene == DontDestroyOnLoadScene)
                            {
                                Debug.Log($"{networkObject.gameObject.name} migrating into DDOL!");
                            }

                            SceneManager.MoveGameObjectToScene(networkObject.gameObject, scene);
                        }
                        else if (NetworkManager.LogLevel <= LogLevel.Normal)
                        {
                            NetworkLog.LogWarningServer($"[Client-{NetworkManager.LocalClientId}][{networkObject.gameObject.name}] Server - " +
                                $"client scene mismatch detected! Client-side has no scene loaded with handle ({networkObject.SceneOriginHandle})!");
                        }
                    }
                }
            }
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
                case SceneEventType.ActiveSceneChanged:
                    {
                        if (HashToBuildIndex.ContainsKey(sceneEventData.ActiveSceneHash))
                        {
                            var scene = SceneManager.GetSceneByBuildIndex(HashToBuildIndex[sceneEventData.ActiveSceneHash]);
                            if (scene.isLoaded)
                            {
                                SceneManager.SetActiveScene(scene);
                            }
                        }
                        break;
                    }
                case SceneEventType.ObjectSceneChanged:
                    {
                        MigrateNetworkObjectsIntoScenes();
                        break;
                    }
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

                            // If needed, set the currently active scene
                            if (HashToBuildIndex.ContainsKey(sceneEventData.ActiveSceneHash))
                            {
                                var targetActiveScene = SceneManager.GetSceneByBuildIndex(HashToBuildIndex[sceneEventData.ActiveSceneHash]);
                                if (targetActiveScene.isLoaded && targetActiveScene.handle != SceneManager.GetActiveScene().handle)
                                {
                                    SceneManager.SetActiveScene(targetActiveScene);
                                }
                            }

                            // Spawn and Synchronize all NetworkObjects
                            sceneEventData.SynchronizeSceneNetworkObjects(NetworkManager);

                            // If needed, migrate dynamically spawned NetworkObjects to the same scene as they are on the server
                            SynchronizeNetworkObjectScene();

                            // Process any pending create object messages that the client received during synchronization
                            ProcessDeferredCreateObjectMessages();

                            sceneEventData.SceneEventType = SceneEventType.SynchronizeComplete;
                            SendSceneEventData(sceneEventId, new ulong[] { NetworkManager.ServerClientId });

                            // All scenes are synchronized, let the server know we are done synchronizing
                            NetworkManager.IsConnectedClient = true;

                            // Client is now synchronized and fully "connected".  This also means the client can send "RPCs" at this time
                            NetworkManager.ConnectionManager.InvokeOnClientConnectedCallback(NetworkManager.LocalClientId);

                            // Notify the client that they have finished synchronizing
                            OnSceneEvent?.Invoke(new SceneEvent()
                            {
                                SceneEventType = sceneEventData.SceneEventType,
                                ClientId = NetworkManager.LocalClientId, // Client sent this to the server
                            });

                            // Process any SceneEventType.ObjectSceneChanged messages that
                            // were deferred while synchronizing and migrate the associated
                            // NetworkObjects to their newly assigned scenes.
                            sceneEventData.ProcessDeferredObjectSceneChangedEvents();

                            // Only if PostSynchronizationSceneUnloading is set and we are running in client synchronization
                            // mode additive do we unload any remaining scene that was not synchronized (otherwise any loaded
                            // scene not synchronized by the server will remain loaded)
                            if (PostSynchronizationSceneUnloading && ClientSynchronizationMode == LoadSceneMode.Additive)
                            {
                                SceneManagerHandler.UnloadUnassignedScenes(NetworkManager);
                            }

                            OnSynchronizeComplete?.Invoke(NetworkManager.LocalClientId);

                            // For convenience, notify all NetworkBehaviours that synchronization is complete.
                            foreach (var networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
                            {
                                networkObject.InternalNetworkSessionSynchronized();
                            }

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
                        InvokeSceneEvents(NetworkManager.ServerClientId, sceneEventData);
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
                case SceneEventType.UnloadComplete:
                    {
                        // Notify the local server that the client has finished unloading a scene
                        InvokeSceneEvents(clientId, sceneEventData);

                        if (SceneEventProgressTracking.ContainsKey(sceneEventData.SceneEventProgressId))
                        {
                            SceneEventProgressTracking[sceneEventData.SceneEventProgressId].ClientFinishedSceneEvent(clientId);
                        }

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

                        // At this point the client is considered fully "connected"
                        NetworkManager.ConnectedClients[clientId].IsConnected = true;

                        // All scenes are synchronized, let the server know we are done synchronizing
                        OnSynchronizeComplete?.Invoke(clientId);

                        // At this time the client is fully synchronized with all loaded scenes and
                        // NetworkObjects and should be considered "fully connected". Send the
                        // notification that the client is connected.
                        // TODO 2023: We should have a better name for this or have multiple states the
                        // client progresses through (the name and associated legacy behavior/expected state
                        // of the client was persisted since MLAPI)
                        NetworkManager.ConnectionManager.InvokeOnClientConnectedCallback(clientId);

                        if (NetworkManager.IsHost)
                        {
                            NetworkManager.ConnectionManager.InvokeOnPeerConnectedCallback(clientId);
                        }

                        // Check to see if the client needs to resynchronize and before sending the message make sure the client is still connected to avoid
                        // a potential crash within the MessageSystem (i.e. sending to a client that no longer exists)
                        if (sceneEventData.ClientNeedsReSynchronization() && !DisableReSynchronization && NetworkManager.ConnectedClients.ContainsKey(clientId))
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
            if (NetworkManager != null)
            {
                var sceneEventData = BeginSceneEvent();

                sceneEventData.Deserialize(reader);

                NetworkManager.NetworkMetrics.TrackSceneEventReceived(
                   clientId, (uint)sceneEventData.SceneEventType, SceneNameFromHash(sceneEventData.SceneHash), reader.Length);

                if (sceneEventData.IsSceneEventClientSide())
                {
                    // If the client is being synchronized for the first time do some initialization
                    if (sceneEventData.SceneEventType == SceneEventType.Synchronize)
                    {
                        ScenePlacedObjects.Clear();
                        // Set the server's configured client synchronization mode on the client side
                        ClientSynchronizationMode = sceneEventData.ClientSynchronizationMode;

                        // Only if ClientSynchronizationMode is Additive and the client receives a synchronize scene event
                        if (ClientSynchronizationMode == LoadSceneMode.Additive)
                        {
                            // Check for scenes already loaded and create a table of scenes already loaded (SceneEntries) that will be
                            // used if the server is synchronizing the same scenes (i.e. if a matching scene is already loaded on the
                            // client side, then that scene will be used as opposed to loading another scene). This allows for clients
                            // to reconnect to a network session without having to unload all of the scenes and reload all of the scenes.
                            SceneManagerHandler.PopulateLoadedScenes(ref ScenesLoaded, NetworkManager);
                        }
                    }
                    HandleClientSceneEvent(sceneEventData.SceneEventId);
                }
                else
                {
                    HandleServerSceneEvent(sceneEventData.SceneEventId, clientId);
                }
            }
            else
            {
                Debug.LogError($"{nameof(HandleSceneEvent)} was invoked but {nameof(Netcode.NetworkManager)} reference was null!");
            }
        }

        /// <summary>
        /// Moves all NetworkObjects that don't have the <see cref="NetworkObject.DestroyWithScene"/> set to
        /// the "Do not destroy on load" scene.
        /// </summary>
        internal void MoveObjectsToDontDestroyOnLoad()
        {
            // Create a local copy of the spawned objects list since the spawn manager will adjust the list as objects
            // are despawned.
            var localSpawnedObjectsHashSet = new HashSet<NetworkObject>(NetworkManager.SpawnManager.SpawnedObjectsList);
            foreach (var networkObject in localSpawnedObjectsHashSet)
            {
                if (networkObject == null || (networkObject != null && networkObject.gameObject.scene == DontDestroyOnLoadScene))
                {
                    continue;
                }

                // Only NetworkObjects marked to not be destroyed with the scene
                if (!networkObject.DestroyWithScene)
                {
                    // Only move dynamically spawned NetworkObjects with no parent as the children will follow
                    if (networkObject.gameObject.transform.parent == null && networkObject.IsSceneObject != null && !networkObject.IsSceneObject.Value)
                    {
                        UnityEngine.Object.DontDestroyOnLoad(networkObject.gameObject);
                    }
                }
                else if (NetworkManager.IsServer)
                {
                    networkObject.Despawn();
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

#if UNITY_2023_1_OR_NEWER
            var networkObjects = UnityEngine.Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.InstanceID);
#else
            var networkObjects = UnityEngine.Object.FindObjectsOfType<NetworkObject>();
#endif

            // Just add every NetworkObject found that isn't already in the list
            // With additive scenes, we can have multiple in-scene placed NetworkObjects with the same GlobalObjectIdHash value
            // During Client Side Synchronization: We add them on a FIFO basis, for each scene loaded without clearing, and then
            // at the end of scene loading we use this list to soft synchronize all in-scene placed NetworkObjects
            foreach (var networkObjectInstance in networkObjects)
            {
                var globalObjectIdHash = networkObjectInstance.GlobalObjectIdHash;
                var sceneHandle = networkObjectInstance.gameObject.scene.handle;
                // We check to make sure the NetworkManager instance is the same one to be "NetcodeIntegrationTestHelpers" compatible and filter the list on a per scene basis (for additive scenes)
                if (networkObjectInstance.IsSceneObject != false && (networkObjectInstance.NetworkManager == NetworkManager ||
                    networkObjectInstance.NetworkManagerOwner == null) && sceneHandle == sceneToFilterBy.handle)
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
            foreach (var networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject == null)
                {
                    continue;
                }
                // If it is in the DDOL then
                if (networkObject.gameObject.scene == DontDestroyOnLoadScene && !networkObject.DestroyWithScene)
                {
                    // only move dynamically spawned network objects, with no parent as child objects will follow,
                    // back into the currently active scene
                    if (networkObject.gameObject.transform.parent == null && networkObject.IsSceneObject != null && !networkObject.IsSceneObject.Value)
                    {
                        SceneManager.MoveGameObjectToScene(networkObject.gameObject, scene);
                    }
                }
            }
        }

        /// <summary>
        /// Holds a list of scene handles (server-side relative) and NetworkObjects migrated into it
        /// during the current frame.
        /// </summary>
        internal Dictionary<int, List<NetworkObject>> ObjectsMigratedIntoNewScene = new Dictionary<int, List<NetworkObject>>();

        /// <summary>
        /// Handles notifying clients when a NetworkObject has been migrated into a new scene
        /// </summary>
        internal void NotifyNetworkObjectSceneChanged(NetworkObject networkObject)
        {
            // Really, this should never happen but in case it does
            if (!NetworkManager.IsServer)
            {
                if (NetworkManager.LogLevel == LogLevel.Developer)
                {
                    NetworkLog.LogErrorServer("[Please Report This Error][NotifyNetworkObjectSceneChanged] A client is trying to notify of an object's scene change!");
                }
                return;
            }

            // Ignore in-scene placed NetworkObjects
            if (networkObject.IsSceneObject != false)
            {
                // Really, this should ever happen but in case it does
                if (NetworkManager.LogLevel == LogLevel.Developer)
                {
                    NetworkLog.LogErrorServer("[Please Report This Error][NotifyNetworkObjectSceneChanged] Trying to notify in-scene placed object scene change!");
                }
                return;
            }

            // Ignore if the scene is the currently active scene and the NetworkObject is auto synchronizing/migrating
            // to the currently active scene.
            if (networkObject.gameObject.scene == SceneManager.GetActiveScene() && networkObject.ActiveSceneSynchronization)
            {
                return;
            }

            // Don't notify if a scene event is in progress
            // Note: This does not apply to SceneEventType.Synchronize since synchronization isn't a global connected client event.
            foreach (var sceneEventEntry in SceneEventProgressTracking)
            {
                if (!sceneEventEntry.Value.HasTimedOut() && sceneEventEntry.Value.Status == SceneEventProgressStatus.Started)
                {
                    return;
                }
            }

            // Otherwise, add the NetworkObject into the list of NetworkObjects who's scene has changed
            if (!ObjectsMigratedIntoNewScene.ContainsKey(networkObject.gameObject.scene.handle))
            {
                ObjectsMigratedIntoNewScene.Add(networkObject.gameObject.scene.handle, new List<NetworkObject>());
            }
            ObjectsMigratedIntoNewScene[networkObject.gameObject.scene.handle].Add(networkObject);
        }

        /// <summary>
        /// Invoked by clients when processing a <see cref="SceneEventType.ObjectSceneChanged"/> event
        /// or invoked by <see cref="SceneEventData.ProcessDeferredObjectSceneChangedEvents"/> when a client finishes
        /// synchronization.
        /// </summary>
        internal void MigrateNetworkObjectsIntoScenes()
        {
            try
            {
                foreach (var sceneEntry in ObjectsMigratedIntoNewScene)
                {
                    if (ServerSceneHandleToClientSceneHandle.ContainsKey(sceneEntry.Key))
                    {
                        var clientSceneHandle = ServerSceneHandleToClientSceneHandle[sceneEntry.Key];
                        if (ScenesLoaded.ContainsKey(ServerSceneHandleToClientSceneHandle[sceneEntry.Key]))
                        {
                            var scene = ScenesLoaded[clientSceneHandle];
                            foreach (var networkObject in sceneEntry.Value)
                            {
                                SceneManager.MoveGameObjectToScene(networkObject.gameObject, scene);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NetworkLog.LogErrorServer($"{ex.Message}\n Stack Trace:\n {ex.StackTrace}");
            }

            // Clear out the list once complete
            ObjectsMigratedIntoNewScene.Clear();
        }


        private List<int> m_ScenesToRemoveFromObjectMigration = new List<int>();

        /// <summary>
        /// Should be invoked during PostLateUpdate just prior to the NetworkMessageManager processes its outbound message queue.
        /// </summary>
        internal void CheckForAndSendNetworkObjectSceneChanged()
        {
            // Early exit if not the server or there is nothing pending
            if (!NetworkManager.IsServer || ObjectsMigratedIntoNewScene.Count == 0)
            {
                return;
            }

            // Double check that the NetworkObjects to migrate still exist
            m_ScenesToRemoveFromObjectMigration.Clear();
            foreach (var sceneEntry in ObjectsMigratedIntoNewScene)
            {
                for (int i = sceneEntry.Value.Count - 1; i >= 0; i--)
                {
                    // Remove NetworkObjects that are no longer spawned
                    if (!sceneEntry.Value[i].IsSpawned)
                    {
                        sceneEntry.Value.RemoveAt(i);
                    }
                }
                // If the scene entry no longer has any NetworkObjects to migrate
                // then add it to the list of scenes to be removed from the table
                // of scenes containing NetworkObjects to migrate.
                if (sceneEntry.Value.Count == 0)
                {
                    m_ScenesToRemoveFromObjectMigration.Add(sceneEntry.Key);
                }
            }

            // Remove sceneHandle entries that no longer have any NetworkObjects remaining
            foreach (var sceneHandle in m_ScenesToRemoveFromObjectMigration)
            {
                ObjectsMigratedIntoNewScene.Remove(sceneHandle);
            }

            // If there is nothing to send a migration notification for then exit
            if (ObjectsMigratedIntoNewScene.Count == 0)
            {
                return;
            }

            // Some NetworkObjects still exist, send the message
            var sceneEvent = BeginSceneEvent();
            sceneEvent.SceneEventType = SceneEventType.ObjectSceneChanged;
            SendSceneEventData(sceneEvent.SceneEventId, NetworkManager.ConnectionManager.ConnectedClientIds.Where(c => c != NetworkManager.ServerClientId).ToArray());
            EndSceneEvent(sceneEvent.SceneEventId);
        }

        // Used to handle client-side scene migration messages received while
        // a client is synchronizing
        internal struct DeferredObjectsMovedEvent
        {
            internal Dictionary<int, List<ulong>> ObjectsMigratedTable;
        }
        internal List<DeferredObjectsMovedEvent> DeferredObjectsMovedEvents = new List<DeferredObjectsMovedEvent>();

        internal struct DeferredObjectCreation
        {
            internal ulong SenderId;
            internal uint MessageSize;
            internal NetworkObject.SceneObject SceneObject;
            internal FastBufferReader FastBufferReader;
        }

        internal List<DeferredObjectCreation> DeferredObjectCreationList = new List<DeferredObjectCreation>();
        internal int DeferredObjectCreationCount;

        internal void DeferCreateObject(ulong senderId, uint messageSize, NetworkObject.SceneObject sceneObject, FastBufferReader fastBufferReader)
        {
            var deferredObjectCreationEntry = new DeferredObjectCreation()
            {
                SenderId = senderId,
                MessageSize = messageSize,
                SceneObject = sceneObject,
            };

            unsafe
            {
                deferredObjectCreationEntry.FastBufferReader = new FastBufferReader(fastBufferReader.GetUnsafePtrAtCurrentPosition(), Allocator.Persistent, fastBufferReader.Length - fastBufferReader.Position);
            }

            DeferredObjectCreationList.Add(deferredObjectCreationEntry);
        }

        private void ProcessDeferredCreateObjectMessages()
        {
            // If no pending create object messages exit early
            if (DeferredObjectCreationList.Count == 0)
            {
                return;
            }
            var networkManager = NetworkManager;
            // Process all deferred create object messages.
            foreach (var deferredObjectCreation in DeferredObjectCreationList)
            {
                CreateObjectMessage.CreateObject(ref networkManager, deferredObjectCreation.SenderId, deferredObjectCreation.MessageSize, deferredObjectCreation.SceneObject, deferredObjectCreation.FastBufferReader);
            }
            DeferredObjectCreationCount = DeferredObjectCreationList.Count;
            DeferredObjectCreationList.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvokeSceneEvents(ulong clientId, SceneEventData eventData, AsyncOperation asyncOperation = null, Scene scene = default)
        {
            var sceneName = SceneNameFromHash(eventData.SceneHash);
            OnSceneEvent?.Invoke(new SceneEvent()
            {
                AsyncOperation = asyncOperation,
                SceneEventType = eventData.SceneEventType,
                SceneName = sceneName,
                ScenePath = ScenePathFromHash(eventData.SceneHash),
                ClientId = clientId,
                LoadSceneMode = eventData.LoadSceneMode,
                ClientsThatCompleted = eventData.ClientsCompleted,
                ClientsThatTimedOut = eventData.ClientsTimedOut,
                Scene = scene,
            });

            switch (eventData.SceneEventType)
            {
                case SceneEventType.Load:
                    OnLoad?.Invoke(clientId, sceneName, eventData.LoadSceneMode, asyncOperation);
                    break;
                case SceneEventType.Unload:
                    OnUnload?.Invoke(clientId, sceneName, asyncOperation);
                    break;
                case SceneEventType.LoadComplete:
                    OnLoadComplete?.Invoke(clientId, sceneName, eventData.LoadSceneMode);
                    break;
                case SceneEventType.UnloadComplete:
                    OnUnloadComplete?.Invoke(clientId, sceneName);
                    break;
                case SceneEventType.LoadEventCompleted:
                    OnLoadEventCompleted?.Invoke(SceneNameFromHash(eventData.SceneHash), eventData.LoadSceneMode, eventData.ClientsCompleted, eventData.ClientsTimedOut);
                    break;
                case SceneEventType.UnloadEventCompleted:
                    OnUnloadEventCompleted?.Invoke(SceneNameFromHash(eventData.SceneHash), eventData.LoadSceneMode, eventData.ClientsCompleted, eventData.ClientsTimedOut);
                    break;
            }
        }
    }
}
