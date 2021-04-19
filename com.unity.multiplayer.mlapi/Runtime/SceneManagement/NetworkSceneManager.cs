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

        private static Scene s_LastScene;
        private static string s_NextSceneName;
        private static bool s_IsSwitching = false;
        internal static uint CurrentSceneIndex = 0;
        internal static Guid CurrentSceneSwitchProgressGuid = new Guid();
        internal static bool IsSpawnedObjectsPendingInDontDestroyOnLoad = false;

        private NetworkManager m_NetworkManager { get; }

        internal NetworkSceneManager(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
        }

        internal void SetCurrentSceneIndex()
        {
            if (!SceneNameToIndex.ContainsKey(SceneManager.GetActiveScene().name))
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning($"The current scene ({SceneManager.GetActiveScene().name}) is not regisered as a network scene.");
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

            m_NetworkManager.SpawnManager.ServerDestroySpawnedSceneObjects(); //Destroy current scene objects before switching.
            s_IsSwitching = true;
            s_LastScene = SceneManager.GetActiveScene();

            var switchSceneProgress = new SceneSwitchProgress(m_NetworkManager);
            SceneSwitchProgresses.Add(switchSceneProgress.Guid, switchSceneProgress);
            CurrentSceneSwitchProgressGuid = switchSceneProgress.Guid;

            // Move ALL NetworkObjects to the temp scene
            MoveObjectsToDontDestroyOnLoad();

            IsSpawnedObjectsPendingInDontDestroyOnLoad = true;

            // Switch scene
            AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            s_NextSceneName = sceneName;

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
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning("Server requested a scene switch to a non-registered scene");
                }

                return;
            }

            s_LastScene = SceneManager.GetActiveScene();

            // Move ALL NetworkObjects to the temp scene
            MoveObjectsToDontDestroyOnLoad();

            IsSpawnedObjectsPendingInDontDestroyOnLoad = true;

            string sceneName = SceneIndexToString[sceneIndex];

            var sceneLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            s_NextSceneName = sceneName;

            sceneLoad.completed += asyncOp2 => OnSceneLoaded(switchSceneGuid, objectStream);
            OnSceneSwitchStarted?.Invoke(sceneLoad);
        }

        internal void OnFirstSceneSwitchSync(uint sceneIndex, Guid switchSceneGuid)
        {
            if (!SceneIndexToString.ContainsKey(sceneIndex) || !RegisteredSceneNames.Contains(SceneIndexToString[sceneIndex]))
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning("Server requested a scene switch to a non-registered scene");
                }

                return;
            }

            if (SceneManager.GetActiveScene().name == SceneIndexToString[sceneIndex])
            {
                return; //This scene is already loaded. This usually happends at first load
            }

            s_LastScene = SceneManager.GetActiveScene();
            string sceneName = SceneIndexToString[sceneIndex];
            s_NextSceneName = sceneName;
            CurrentActiveSceneIndex = SceneNameToIndex[sceneName];

            IsSpawnedObjectsPendingInDontDestroyOnLoad = true;
            SceneManager.LoadScene(sceneName);

            using (var buffer = PooledNetworkBuffer.Get())
            using (var writer = PooledNetworkWriter.Get(buffer))
            {
                writer.WriteByteArray(switchSceneGuid.ToByteArray());
                InternalMessageSender.Send(m_NetworkManager.ServerClientId, NetworkConstants.CLIENT_SWITCH_SCENE_COMPLETED, NetworkChannel.Internal, buffer);
            }

            s_IsSwitching = false;
        }

        private void OnSceneLoaded(Guid switchSceneGuid, Stream objectStream)
        {
            CurrentActiveSceneIndex = SceneNameToIndex[s_NextSceneName];
            var nextScene = SceneManager.GetSceneByName(s_NextSceneName);
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
                var networkObjects = UnityEngine.Object.FindObjectsOfType<NetworkObject>();
                for (int i = 0; i < networkObjects.Length; i++)
                {
                    if (networkObjects[i].IsSceneObject == null)
                    {
                        m_NetworkManager.SpawnManager.SpawnNetworkObjectLocally(networkObjects[i], m_NetworkManager.SpawnManager.GetNetworkObjectId(), true, false, null, null, false, 0, false, true);
                        newSceneObjects.Add(networkObjects[i]);
                    }
                }
            }

            for (int j = 0; j < m_NetworkManager.ConnectedClientsList.Count; j++)
            {
                if (m_NetworkManager.ConnectedClientsList[j].ClientId != m_NetworkManager.ServerClientId)
                {
                    using (var buffer = PooledNetworkBuffer.Get())
                    using (var writer = PooledNetworkWriter.Get(buffer))
                    {
                        writer.WriteUInt32Packed(CurrentActiveSceneIndex);
                        writer.WriteByteArray(switchSceneGuid.ToByteArray());

                        uint sceneObjectsToSpawn = 0;
                        for (int i = 0; i < newSceneObjects.Count; i++)
                        {
                            if (newSceneObjects[i].Observers.Contains(m_NetworkManager.ConnectedClientsList[j].ClientId))
                            {
                                sceneObjectsToSpawn++;
                            }
                        }

                        writer.WriteUInt32Packed(sceneObjectsToSpawn);

                        for (int i = 0; i < newSceneObjects.Count; i++)
                        {
                            if (newSceneObjects[i].Observers.Contains(m_NetworkManager.ConnectedClientsList[j].ClientId))
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
                                    //We don't have a parent
                                    writer.WriteBool(false);
                                }
                                else
                                {
                                    //We do have a parent
                                    writer.WriteBool(true);
                                    //Write the parent's NetworkObjectId to be used for linking back to the child
                                    writer.WriteUInt64Packed(parentNetworkObject.NetworkObjectId);
                                }

                                writer.WriteUInt32Packed(newSceneObjects[i].GlobalObjectIdHash);
                                if (newSceneObjects[i].IncludeTransformWhenSpawning == null || newSceneObjects[i].IncludeTransformWhenSpawning(newSceneObjects[i].OwnerClientId))
                                {
                                    //Set the position and rotation data marker to true (i.e. flag to know, when reading from the stream, that postion and roation data follows).
                                    writer.WriteBool(true);

                                    //Write position
                                    writer.WriteSinglePacked(newSceneObjects[i].transform.position.x);
                                    writer.WriteSinglePacked(newSceneObjects[i].transform.position.y);
                                    writer.WriteSinglePacked(newSceneObjects[i].transform.position.z);

                                    //Write rotation
                                    writer.WriteSinglePacked(newSceneObjects[i].transform.rotation.eulerAngles.x);
                                    writer.WriteSinglePacked(newSceneObjects[i].transform.rotation.eulerAngles.y);
                                    writer.WriteSinglePacked(newSceneObjects[i].transform.rotation.eulerAngles.z);
                                }
                                else
                                {
                                    //Set the position and rotation data marker to false (i.e. flag to know, when reading from the stream, that postion and roation data *was not included*)
                                    writer.WriteBool(false);
                                }

                                if (m_NetworkManager.NetworkConfig.EnableNetworkVariable)
                                {
                                    newSceneObjects[i].WriteNetworkVariableData(buffer, m_NetworkManager.ConnectedClientsList[j].ClientId);
                                }
                            }
                        }
                        InternalMessageSender.Send(m_NetworkManager.ConnectedClientsList[j].ClientId, NetworkConstants.SWITCH_SCENE, NetworkChannel.Internal, buffer);
                    }
                }
            }

            //Tell server that scene load is completed
            if (m_NetworkManager.IsHost)
            {
                OnClientSwitchSceneCompleted(m_NetworkManager.LocalClientId, switchSceneGuid);
            }

            s_IsSwitching = false;

            OnSceneSwitched?.Invoke();
        }

        private void OnSceneUnloadClient(Guid switchSceneGuid, Stream objectStream)
        {
            var networkObjects = UnityEngine.Object.FindObjectsOfType<NetworkObject>();
            m_NetworkManager.SpawnManager.ClientCollectSoftSyncSceneObjectSweep(networkObjects);

            using (var reader = PooledNetworkReader.Get(objectStream))
            {
                var newObjectsCount = reader.ReadUInt32Packed();

                for (int i = 0; i < newObjectsCount; i++)
                {
                    var isPlayerObject = reader.ReadBool();
                    var networkId = reader.ReadUInt64Packed();
                    var ownerClientId = reader.ReadUInt64Packed();
                    var hasParent = reader.ReadBool();
                    ulong? parentNetworkId = null;

                    if (hasParent)
                    {
                        parentNetworkId = reader.ReadUInt32Packed();
                    }

                    var prefabHash = reader.ReadUInt32Packed();
                    Vector3? position = null;
                    Quaternion? rotation = null;

                    //Check to see if we have position and rotation values that follows
                    if (reader.ReadBool())
                    {
                        position = new Vector3(reader.ReadSinglePacked(), reader.ReadSinglePacked(), reader.ReadSinglePacked());
                        rotation = Quaternion.Euler(reader.ReadSinglePacked(), reader.ReadSinglePacked(), reader.ReadSinglePacked());
                    }

                    var networkObject = m_NetworkManager.SpawnManager.CreateLocalNetworkObject(true, prefabHash, ownerClientId, parentNetworkId, position, rotation);
                    m_NetworkManager.SpawnManager.SpawnNetworkObjectLocally(networkObject, networkId, true, isPlayerObject, ownerClientId, objectStream, false, 0, true, false);

                    var bufferQueue = m_NetworkManager.BufferManager.ConsumeBuffersForNetworkId(networkId);

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

            using (var buffer = PooledNetworkBuffer.Get())
            using (var writer = PooledNetworkWriter.Get(buffer))
            {
                writer.WriteByteArray(switchSceneGuid.ToByteArray());
                InternalMessageSender.Send(m_NetworkManager.ServerClientId, NetworkConstants.CLIENT_SWITCH_SCENE_COMPLETED, NetworkChannel.Internal, buffer);
            }

            s_IsSwitching = false;

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
            var objectsToKeep = m_NetworkManager.SpawnManager.SpawnedObjectsList;

            foreach (var sobj in objectsToKeep)
            {
                //In case an object has been set as a child of another object it has to be unchilded in order to be moved from one scene to another.
                if (sobj.gameObject.transform.parent != null)
                {
                    sobj.gameObject.transform.parent = null;
                }

                UnityEngine.Object.DontDestroyOnLoad(sobj.gameObject);
            }
        }

        private void MoveObjectsToScene(Scene scene)
        {
            // Move ALL NetworkObjects to the temp scene
            var objectsToKeep = m_NetworkManager.SpawnManager.SpawnedObjectsList;

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
