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

        private NetworkManager m_NetworkManager { get; }

        internal NetworkSceneManager(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            SceneEventData = new SceneEventData();
        }

        internal void SetCurrentSceneIndex()
        {
            if (!SceneNameToIndex.TryGetValue(SceneManager.GetActiveScene().name, out CurrentSceneIndex))
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning($"The current scene ({SceneManager.GetActiveScene().name}) is not regisered as a network scene.");
                }

                return;
            }

            CurrentActiveSceneIndex = CurrentSceneIndex;
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
        /// Switches to a scene with a given name. Can only be called from Server
        /// </summary>
        /// <param name="sceneName">The name of the scene to switch to</param>
        /// <param name="loadSceneMode">The mode to load the scene (Additive vs Single)</param>
        /// <returns>SceneSwitchProgress</returns>
        public SceneSwitchProgress SwitchScene(string sceneName, LoadSceneMode loadSceneMode = LoadSceneMode.Single, List<string> scenesToUnload = null)
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

            if (loadSceneMode != LoadSceneMode.Additive)
            {
                m_NetworkManager.SpawnManager.ServerDestroySpawnedSceneObjects(); //Destroy current scene objects before switching.
            }
            s_IsSwitching = true;

            var switchSceneProgress = new SceneSwitchProgress(m_NetworkManager);
            SceneSwitchProgresses.Add(switchSceneProgress.Guid, switchSceneProgress);
            CurrentSceneSwitchProgressGuid = switchSceneProgress.Guid;

            SceneEventData.SwitchSceneGuid = switchSceneProgress.Guid;
            SceneEventData.SceneEventType = SceneEventData.SceneEventTypes.SWITCH;
            CurrentActiveSceneIndex = SceneNameToIndex[sceneName];
            SceneEventData.SceneIndex = CurrentActiveSceneIndex;
            SceneEventData.LoadSceneMode = loadSceneMode;

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

                    m_NetworkManager.MessageSender.Send(NetworkManager.Singleton.ServerClientId, NetworkConstants.ALL_CLIENTS_LOADED_SCENE, NetworkChannel.Internal, buffer);
                }
            };

            if (loadSceneMode != LoadSceneMode.Additive)
            {
                // Move ALL NetworkObjects to the temp scene
                MoveObjectsToDontDestroyOnLoad();
            }

            IsSpawnedObjectsPendingInDontDestroyOnLoad = true;

            // Switch scene
            AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);

            s_NextSceneName = sceneName;

            sceneLoad.completed += (AsyncOperation asyncOp2) => { OnSceneLoaded(); };
            switchSceneProgress.SetSceneLoadOperation(sceneLoad);
            OnSceneSwitchStarted?.Invoke(sceneLoad);

            return switchSceneProgress;
        }

        // Called on client
        internal void OnSceneSwitch(Stream objectStream)
        {
            SceneEventData.CopyUndreadFromStream(objectStream);

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
                return; //This scene is already loaded. This usually happends at first load
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





        private void OnSceneLoaded()
        {
            var nextScene = SceneManager.GetSceneByName(s_NextSceneName);
            SceneManager.SetActiveScene(nextScene);

            //Get all NetworkObjects loaded by the scene
            PopulateScenePlacedObjects(nextScene);

            if (SceneEventData.LoadSceneMode == LoadSceneMode.Single)
            {
                // Move all objects to the new scene
                MoveObjectsToScene(nextScene);
            }

            IsSpawnedObjectsPendingInDontDestroyOnLoad = false;

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
                            SynchronizeInSceneObjects(m_NetworkManager.ConnectedClientsList[j].ClientId,writer);

                            m_NetworkManager.MessageSender.Send(m_NetworkManager.ConnectedClientsList[j].ClientId, NetworkConstants.SCENE_EVENT, NetworkChannel.Internal, buffer);
                        }
                    }
                }
            }

            // Tell server that scene load is completed
            if (m_NetworkManager.IsHost)
            {
                OnClientSwitchSceneCompleted(m_NetworkManager.LocalClientId, SceneEventData.SwitchSceneGuid);
            }

            s_IsSwitching = false;

            OnSceneSwitched?.Invoke();
        }

        private void OnClientLoadedScene()
        {
            var networkObjects = UnityEngine.Object.FindObjectsOfType<NetworkObject>();

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
                SceneEventData.SceneEventType = SceneEventData.SceneEventTypes.SWITCH_COMPLETE;
                writer.WriteObjectPacked(SceneEventData);
                //writer.WriteByteArray(SceneEventData.SwitchSceneGuid.ToByteArray());
                m_NetworkManager.MessageSender.Send(m_NetworkManager.ServerClientId, NetworkConstants.SCENE_EVENT, NetworkChannel.Internal, buffer);
            }

            s_IsSwitching = false;

            OnSceneSwitched?.Invoke();
        }



        internal void SynchronizeInSceneObjects(ulong clientId, NetworkWriter writer)
        {
            writer.WriteObjectPacked(SceneEventData);
            //writer.WriteUInt32Packed(CurrentActiveSceneIndex);
            //writer.WriteByteArray(switchSceneGuid.ToByteArray());
            //writer.WriteBool(loadSceneMode == LoadSceneMode.Additive);

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

        internal bool HasSceneMismatch(uint sceneIndex) => SceneManager.GetActiveScene().name != SceneIndexToString[sceneIndex];

        // Called on server
        internal void OnClientSwitchSceneCompleted(ulong clientId, Guid switchSceneGuid)
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

        private void HandleClientSceneEvent(Stream stream)
        {
            switch (SceneEventData.SceneEventType)
            {
                case SceneEventData.SceneEventTypes.SWITCH:
                    {
                        OnSceneSwitch(stream);
                        break;
                    }
                default:
                    {
                        Debug.LogWarning($"{SceneEventData.SceneEventType} is not currently supported!");
                        break;
                    }
            }
        }

        private void HandleServerSceneEvent(ulong clientId, Stream stream)
        {
            switch (SceneEventData.SceneEventType)
            {
                case SceneEventData.SceneEventTypes.SWITCH_COMPLETE:
                    {
                        OnClientSwitchSceneCompleted(clientId,SceneEventData.SwitchSceneGuid);
                        break;
                    }
                default:
                    {
                        Debug.LogWarning($"{SceneEventData.SceneEventType} is not currently supported!");
                        break;
                    }
            }
        }


        public void HandleSceneEvent(ulong clientId, Stream stream)
        {
            if (m_NetworkManager != null)
            {
                if (stream != null)
                {
                    var reader = NetworkReaderPool.GetReader(stream);
                    SceneEventData = (SceneEventData)reader.ReadObjectPacked(typeof(SceneEventData));
                    if(SceneEventData.IsSceneEventClientSide())
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
                    Debug.LogError($"Scene Event {nameof(OnSceneSwitch)} was invoked with a null stream!");
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
    public class SceneEventData : INetworkSerializable,IDisposable
    {
        public enum SceneEventTypes
        {
            SWITCH,             //Server to client
            LOAD,               //Server to client
            UNLOAD,             //Server to client
            SWITCH_COMPLETE,    //Client to server
            LOAD_COMPLETE,      //Client to server
            UNLOAD_COMPLETE     //Client to server
        }

        public SceneEventTypes SceneEventType;
        public LoadSceneMode LoadSceneMode;
        public Guid SwitchSceneGuid;
        public uint SceneIndex;


        internal PooledNetworkBuffer InternalBuffer;

        public bool IsSceneEventClientSide()
        {
            switch(SceneEventType)
            {
                case SceneEventTypes.LOAD:
                case SceneEventTypes.SWITCH:
                case SceneEventTypes.UNLOAD:
                    {
                        return true;
                    }
            }
            return false;
        }
        private void OnWrite(NetworkWriter writer)
        {
            writer.WriteByte((byte)SceneEventType);
            writer.WriteByte((byte)LoadSceneMode);
            writer.WriteByteArray(SwitchSceneGuid.ToByteArray());
            writer.WriteUInt32Packed(SceneIndex);
        }

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

            SwitchSceneGuid = new Guid(reader.ReadByteArray());

            SceneIndex = reader.ReadUInt32Packed();

        }

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

        internal void CopyUndreadFromStream(Stream stream)
        {
            InternalBuffer.Position = 0;
            InternalBuffer.CopyUnreadFrom(stream);
            InternalBuffer.Position = 0;
        }

        public void Dispose()
        {
            if(InternalBuffer != null)
            {
                NetworkBufferPool.PutBackInPool(InternalBuffer);
                InternalBuffer = null;
            }
        }

        public SceneEventData()
        {
            InternalBuffer = NetworkBufferPool.GetBuffer();
        }
    }
}
