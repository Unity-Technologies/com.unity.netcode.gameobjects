using System.Collections.Generic;
using System;
using System.IO;
using MLAPI.Configuration;
using MLAPI.Exceptions;
using MLAPI.Logging;
using MLAPI.Messaging;
using MLAPI.Serialization.Pooled;
using MLAPI.Spawning;
using UnityEngine;
using UnityEngine.SceneManagement;
using MLAPI.Messaging.Buffering;
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
        /// Event that is invoked when the scene is switched
        /// </summary>
        public event SceneSwitchedDelegate OnSceneSwitched;

        /// <summary>
        /// Event that is invoked when a local scene switch has started
        /// </summary>
        public event SceneSwitchStartedDelegate OnSceneSwitchStarted;

        internal readonly HashSet<string> RegisteredSceneNames = new HashSet<string>();
        internal readonly Dictionary<string, uint> SceneNameToIndex = new Dictionary<string, uint>();
        internal readonly Dictionary<uint, string> SceneIndexToString = new Dictionary<uint, string>();
        internal readonly Dictionary<Guid, SceneSwitchProgress> SceneSwitchProgresses = new Dictionary<Guid, SceneSwitchProgress>();

        private Scene m_LastScene;
        private string m_NextSceneName;
        private bool m_IsSwitching = false;
        internal uint CurrentSceneIndex = 0;
        internal Guid CurrentSceneSwitchProgressGuid = new Guid();
        internal bool IsSpawnedObjectsPendingInDontDestroyOnLoad = false;

        private NetworkManager m_NetworkManager;

        internal NetworkSceneManager(NetworkManager manager) { m_NetworkManager = manager;  }

        internal void SetCurrentSceneIndex()
        {
            if (!SceneNameToIndex.ContainsKey(SceneManager.GetActiveScene().name))
            {
                if (m_NetworkManager.NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    m_NetworkManager.NetworkLog.LogWarning($"The current scene ({SceneManager.GetActiveScene().name}) is not regisered as a network scene.");
                }

                return;
            }

            CurrentSceneIndex = SceneNameToIndex[SceneManager.GetActiveScene().name];
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
        public SceneSwitchProgress SwitchScene(string sceneName)
        {
            if (!m_NetworkManager.IsServer)
            {
                throw new NotServerException("Only server can start a scene switch");
            }

            if (m_IsSwitching)
            {
                if (m_NetworkManager.NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    m_NetworkManager.NetworkLog.LogWarning("Scene switch already in progress");
                }

                return null;
            }

            if (!RegisteredSceneNames.Contains(sceneName))
            {
                if (m_NetworkManager.NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    m_NetworkManager.NetworkLog.LogWarning($"The scene {sceneName} is not registered as a switchable scene.");
                }

                return null;
            }

            m_NetworkManager.NetworkSpawnManager.ServerDestroySpawnedSceneObjects(); //Destroy current scene objects before switching.
            m_IsSwitching = true;
            m_LastScene = SceneManager.GetActiveScene();

            var switchSceneProgress = new SceneSwitchProgress(m_NetworkManager);
            SceneSwitchProgresses.Add(switchSceneProgress.Guid, switchSceneProgress);
            CurrentSceneSwitchProgressGuid = switchSceneProgress.Guid;

            // Move ALL NetworkObjects to the temp scene
            MoveObjectsToDontDestroyOnLoad();

            IsSpawnedObjectsPendingInDontDestroyOnLoad = true;

            // Switch scene
            AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            m_NextSceneName = sceneName;

            sceneLoad.completed += (AsyncOperation asyncOp2) => { OnSceneLoaded(switchSceneProgress.Guid, null); };
            switchSceneProgress.SetSceneLoadOperation(sceneLoad);
            OnSceneSwitchStarted?.Invoke(sceneLoad);

            return switchSceneProgress;
        }

        // Called on client
        internal void OnSceneSwitch(uint sceneIndex, Guid switchSceneGuid, Stream objectStream)
        {
            if (!SceneIndexToString.ContainsKey(sceneIndex) || !RegisteredSceneNames.Contains(SceneIndexToString[sceneIndex]))
            {
                if (m_NetworkManager.NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    m_NetworkManager.NetworkLog.LogWarning("Server requested a scene switch to a non-registered scene");
                }

                return;
            }

            m_LastScene = SceneManager.GetActiveScene();

            // Move ALL NetworkObjects to the temp scene
            MoveObjectsToDontDestroyOnLoad();

            IsSpawnedObjectsPendingInDontDestroyOnLoad = true;

            string sceneName = SceneIndexToString[sceneIndex];

            var sceneLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            m_NextSceneName = sceneName;

            sceneLoad.completed += asyncOp2 => OnSceneLoaded(switchSceneGuid, objectStream);
            OnSceneSwitchStarted?.Invoke(sceneLoad);
        }

        internal void OnFirstSceneSwitchSync(uint sceneIndex, Guid switchSceneGuid)
        {
            if (!SceneIndexToString.ContainsKey(sceneIndex) || !RegisteredSceneNames.Contains(SceneIndexToString[sceneIndex]))
            {
                if (m_NetworkManager.NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    m_NetworkManager.NetworkLog.LogWarning("Server requested a scene switch to a non-registered scene");
                }

                return;
            }

            if (SceneManager.GetActiveScene().name == SceneIndexToString[sceneIndex])
            {
                return; //This scene is already loaded. This usually happends at first load
            }

            m_LastScene = SceneManager.GetActiveScene();
            string sceneName = SceneIndexToString[sceneIndex];
            m_NextSceneName = sceneName;
            CurrentActiveSceneIndex = SceneNameToIndex[sceneName];

            IsSpawnedObjectsPendingInDontDestroyOnLoad = true;
            SceneManager.LoadScene(sceneName);

            using (var buffer = PooledNetworkBuffer.Get())
            using (var writer = m_NetworkManager.NetworkWriterPool.GetWriter(buffer))
            {
                writer.WriteByteArray(switchSceneGuid.ToByteArray());
                m_NetworkManager.InternalMessageSender.Send(m_NetworkManager.ServerClientId, NetworkConstants.CLIENT_SWITCH_SCENE_COMPLETED, NetworkChannel.Internal, buffer);
            }

            m_IsSwitching = false;
        }

        private void OnSceneLoaded(Guid switchSceneGuid, Stream objectStream)
        {
            CurrentActiveSceneIndex = SceneNameToIndex[m_NextSceneName];
            var nextScene = SceneManager.GetSceneByName(m_NextSceneName);
            SceneManager.SetActiveScene(nextScene);

            // Move all objects to the new scene
            MoveObjectsToScene(nextScene);

            IsSpawnedObjectsPendingInDontDestroyOnLoad = false;

            CurrentSceneIndex = CurrentActiveSceneIndex;

            if (m_NetworkManager.IsServer)
            {
                OnSceneUnloadServer(switchSceneGuid);
            }
            else
            {
                OnSceneUnloadClient(switchSceneGuid, objectStream);
            }
        }

        private void OnSceneUnloadServer(Guid switchSceneGuid)
        {
            // Justification: Rare alloc, could(should?) reuse
            var newSceneObjects = new List<NetworkObject>();
            {
                var networkObjects = Spawning.NetworkSpawnManager.FindObjectsInScene<NetworkObject>(m_NetworkManager.gameObject.scene);
                for (int i = 0; i < networkObjects.Count; i++)
                {
                    if (networkObjects[i].IsSceneObject == null)
                    {
                        m_NetworkManager.NetworkSpawnManager.SpawnNetworkObjectLocally(networkObjects[i],
                            m_NetworkManager.NetworkSpawnManager.GetNetworkObjectId(), true, false, null, null, false, 0, false, true);
                        newSceneObjects.Add(networkObjects[i]);
                    }
                }
            }

            for (int j = 0; j < m_NetworkManager.ConnectedClientsList.Count; j++)
            {
                if (m_NetworkManager.ConnectedClientsList[j].ClientId != m_NetworkManager.ServerClientId)
                {
                    using (var buffer = PooledNetworkBuffer.Get())
                    using (var writer = m_NetworkManager.NetworkWriterPool.GetWriter(buffer))
                    {
                        writer.WriteUInt32Packed(CurrentActiveSceneIndex);
                        writer.WriteByteArray(switchSceneGuid.ToByteArray());

                        uint sceneObjectsToSpawn = 0;
                        for (int i = 0; i < newSceneObjects.Count; i++)
                        {
                            if (newSceneObjects[i].m_Observers.Contains(m_NetworkManager.ConnectedClientsList[j].ClientId))
                            {
                                sceneObjectsToSpawn++;
                            }
                        }

                        writer.WriteUInt32Packed(sceneObjectsToSpawn);

                        for (int i = 0; i < newSceneObjects.Count; i++)
                        {
                            if (newSceneObjects[i].m_Observers.Contains(m_NetworkManager.ConnectedClientsList[j].ClientId))
                            {
                                writer.WriteBool(newSceneObjects[i].IsPlayerObject);
                                writer.WriteUInt64Packed(newSceneObjects[i].NetworkObjectId);
                                writer.WriteUInt64Packed(newSceneObjects[i].OwnerClientId);

                                NetworkObject parentNetworkObject = null;

                                if (!newSceneObjects[i].AlwaysReplicateAsRoot && newSceneObjects[i].transform.parent != null)
                                {
                                    parentNetworkObject = newSceneObjects[i].transform.parent.GetComponent<NetworkObject>();
                                }

                                if (parentNetworkObject == null)
                                {
                                    writer.WriteBool(false);
                                }
                                else
                                {
                                    writer.WriteBool(true);
                                    writer.WriteUInt64Packed(parentNetworkObject.NetworkObjectId);
                                }

                                if (!m_NetworkManager.NetworkConfig.EnableSceneManagement || m_NetworkManager.NetworkConfig.UsePrefabSync)
                                {
                                    writer.WriteUInt64Packed(newSceneObjects[i].PrefabHash);

                                    writer.WriteSinglePacked(newSceneObjects[i].transform.position.x);
                                    writer.WriteSinglePacked(newSceneObjects[i].transform.position.y);
                                    writer.WriteSinglePacked(newSceneObjects[i].transform.position.z);

                                    writer.WriteSinglePacked(newSceneObjects[i].transform.rotation.eulerAngles.x);
                                    writer.WriteSinglePacked(newSceneObjects[i].transform.rotation.eulerAngles.y);
                                    writer.WriteSinglePacked(newSceneObjects[i].transform.rotation.eulerAngles.z);
                                }
                                else
                                {
                                    writer.WriteUInt64Packed(newSceneObjects[i].NetworkInstanceId);
                                }

                                if (m_NetworkManager.NetworkConfig.EnableNetworkVariable)
                                {
                                    newSceneObjects[i].WriteNetworkVariableData(buffer, m_NetworkManager.ConnectedClientsList[j].ClientId);
                                }
                            }
                        }

                        m_NetworkManager.InternalMessageSender.Send(m_NetworkManager.ConnectedClientsList[j].ClientId, NetworkConstants.SWITCH_SCENE, NetworkChannel.Internal, buffer);
                    }
                }
            }

            //Tell server that scene load is completed
            if (m_NetworkManager.IsHost)
            {
                OnClientSwitchSceneCompleted(m_NetworkManager.LocalClientId, switchSceneGuid);
            }

            m_IsSwitching = false;

            OnSceneSwitched?.Invoke();
        }

        private void OnSceneUnloadClient(Guid switchSceneGuid, Stream objectStream)
        {
            if (!m_NetworkManager.NetworkConfig.EnableSceneManagement || m_NetworkManager.NetworkConfig.UsePrefabSync)
            {
                m_NetworkManager.NetworkSpawnManager.DestroySceneObjects();

                using (var reader = m_NetworkManager.NetworkReaderPool.GetReader(objectStream))
                {
                    uint newObjectsCount = reader.ReadUInt32Packed();

                    for (int i = 0; i < newObjectsCount; i++)
                    {
                        bool isPlayerObject = reader.ReadBool();
                        ulong networkId = reader.ReadUInt64Packed();
                        ulong owner = reader.ReadUInt64Packed();
                        bool hasParent = reader.ReadBool();
                        ulong? parentNetworkId = null;

                        if (hasParent)
                        {
                            parentNetworkId = reader.ReadUInt64Packed();
                        }

                        ulong prefabHash = reader.ReadUInt64Packed();

                        Vector3? position = null;
                        Quaternion? rotation = null;
                        if (reader.ReadBool())
                        {
                            position = new Vector3(reader.ReadSinglePacked(), reader.ReadSinglePacked(), reader.ReadSinglePacked());
                            rotation = Quaternion.Euler(reader.ReadSinglePacked(), reader.ReadSinglePacked(), reader.ReadSinglePacked());
                        }

                        var networkObject = m_NetworkManager.NetworkSpawnManager.CreateLocalNetworkObject(false, 0, prefabHash, parentNetworkId, position, rotation);
                        m_NetworkManager.NetworkSpawnManager.SpawnNetworkObjectLocally(networkObject, networkId, true, isPlayerObject, owner, objectStream, false, 0, true, false);

                        var bufferQueue = BufferManager.ConsumeBuffersForNetworkId(networkId);

                        // Apply buffered messages
                        if (bufferQueue != null)
                        {
                            while (bufferQueue.Count > 0)
                            {
                                BufferManager.BufferedMessage message = bufferQueue.Dequeue();
                                m_NetworkManager.HandleIncomingData(message.SenderClientId, message.NetworkChannel, new ArraySegment<byte>(message.NetworkBuffer.GetBuffer(), (int)message.NetworkBuffer.Position, (int)message.NetworkBuffer.Length), message.ReceiveTime, false);
                                BufferManager.RecycleConsumedBufferedMessage(message);
                            }
                        }
                    }
                }
            }
            else
            {
                var networkObjects = Spawning.NetworkSpawnManager.FindObjectsInScene<NetworkObject>(m_NetworkManager.gameObject.scene);
                m_NetworkManager.NetworkSpawnManager.ClientCollectSoftSyncSceneObjectSweep(networkObjects);

                using (var reader = m_NetworkManager.NetworkReaderPool.GetReader(objectStream))
                {
                    uint newObjectsCount = reader.ReadUInt32Packed();

                    for (int i = 0; i < newObjectsCount; i++)
                    {
                        bool isPlayerObject = reader.ReadBool();
                        ulong networkId = reader.ReadUInt64Packed();
                        ulong owner = reader.ReadUInt64Packed();
                        bool hasParent = reader.ReadBool();
                        ulong? parentNetworkId = null;

                        if (hasParent)
                        {
                            parentNetworkId = reader.ReadUInt64Packed();
                        }

                        ulong instanceId = reader.ReadUInt64Packed();

                        var networkObject = m_NetworkManager.NetworkSpawnManager.CreateLocalNetworkObject(true, instanceId, 0, parentNetworkId, null, null);
                        m_NetworkManager.NetworkSpawnManager.SpawnNetworkObjectLocally(networkObject, networkId, true, isPlayerObject, owner, objectStream, false, 0, true, false);

                        var bufferQueue = BufferManager.ConsumeBuffersForNetworkId(networkId);

                        // Apply buffered messages
                        if (bufferQueue != null)
                        {
                            while (bufferQueue.Count > 0)
                            {
                                BufferManager.BufferedMessage message = bufferQueue.Dequeue();
                                m_NetworkManager.HandleIncomingData(message.SenderClientId, message.NetworkChannel, new ArraySegment<byte>(message.NetworkBuffer.GetBuffer(), (int)message.NetworkBuffer.Position, (int)message.NetworkBuffer.Length), message.ReceiveTime, false);
                                BufferManager.RecycleConsumedBufferedMessage(message);
                            }
                        }
                    }
                }
            }

            using (var buffer = PooledNetworkBuffer.Get())
            using (var writer = m_NetworkManager.NetworkWriterPool.GetWriter(buffer))
            {
                writer.WriteByteArray(switchSceneGuid.ToByteArray());
                m_NetworkManager.InternalMessageSender.Send(m_NetworkManager.ServerClientId, NetworkConstants.CLIENT_SWITCH_SCENE_COMPLETED, NetworkChannel.Internal, buffer);
            }

            m_IsSwitching = false;

            OnSceneSwitched?.Invoke();
        }

        internal bool HasSceneMismatch(uint sceneIndex) => SceneManager.GetActiveScene().name != SceneIndexToString[sceneIndex];

        // Called on server
        internal void OnClientSwitchSceneCompleted(ulong clientId, Guid switchSceneGuid)
        {
            if (switchSceneGuid == Guid.Empty)
            {
                //If Guid is empty it means the client has loaded the start scene of the server and the server would never have a switchSceneProgresses created for the start scene.
                return;
            }

            if (!SceneSwitchProgresses.ContainsKey(switchSceneGuid))
            {
                return;
            }

            SceneSwitchProgresses[switchSceneGuid].AddClientAsDone(clientId);
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
            var objectsToKeep = m_NetworkManager.NetworkSpawnManager.SpawnedObjectsList;

            foreach (var sobj in objectsToKeep)
            {
                //In case an object has been set as a child of another object it has to be unchilded in order to be moved from one scene to another.
                if (sobj.gameObject.transform.parent != null)
                {
                    sobj.gameObject.transform.parent = null;
                }

                MonoBehaviour.DontDestroyOnLoad(sobj.gameObject);
            }
        }

        private void MoveObjectsToScene(Scene scene)
        {
            // Move ALL NetworkObjects to the temp scene
            var objectsToKeep = m_NetworkManager.NetworkSpawnManager.SpawnedObjectsList;

            foreach (var sobj in objectsToKeep)
            {
                //In case an object has been set as a child of another object it has to be unchilded in order to be moved from one scene to another.
                if (sobj.gameObject.transform.parent != null)
                {
                    sobj.gameObject.transform.parent = null;
                }

                SceneManager.MoveGameObjectToScene(sobj.gameObject, scene);
            }
        }
    }
}
