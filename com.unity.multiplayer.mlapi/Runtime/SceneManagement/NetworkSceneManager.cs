using System.Collections.Generic;
using System;
using System.IO;
using MLAPI.Configuration;
using MLAPI.Exceptions;
using MLAPI.Logging;
using MLAPI.Messaging;
using MLAPI.Security;
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

        internal readonly HashSet<string> registeredSceneNames = new HashSet<string>();
        internal readonly Dictionary<string, uint> sceneNameToIndex = new Dictionary<string, uint>();
        internal readonly Dictionary<uint, string> sceneIndexToString = new Dictionary<uint, string>();
        internal readonly Dictionary<Guid, SceneSwitchProgress> sceneSwitchProgresses = new Dictionary<Guid, SceneSwitchProgress>();
        private Scene lastScene;
        private string nextSceneName;
        private bool isSwitching = false;
        internal uint currentSceneIndex = 0;
        internal Guid currentSceneSwitchProgressGuid = new Guid();
        internal bool isSpawnedObjectsPendingInDontDestroyOnLoad = false;

        private NetworkingManager networkingManager;

        internal NetworkSceneManager(NetworkingManager manager)
        {
            networkingManager = manager;
        }

        internal void SetCurrentSceneIndex()
        {
            if (!sceneNameToIndex.ContainsKey(SceneManager.GetActiveScene().name))
            {
                if (NetworkingManager.LogLevel <= LogLevel.Normal) NetworkLog.LogWarning("The current scene (" + SceneManager.GetActiveScene().name + ") is not regisered as a network scene.");
                return;
            }
            currentSceneIndex = sceneNameToIndex[SceneManager.GetActiveScene().name];
            CurrentActiveSceneIndex = currentSceneIndex;
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
            if (!networkingManager.NetworkConfig.AllowRuntimeSceneChanges)
            {
                throw new NetworkConfigurationException("Cannot change the scene configuration when AllowRuntimeSceneChanges is false");
            }

            registeredSceneNames.Add(sceneName);
            sceneIndexToString.Add(index, sceneName);
            sceneNameToIndex.Add(sceneName, index);
        }

        /// <summary>
        /// Switches to a scene with a given name. Can only be called from Server
        /// </summary>
        /// <param name="sceneName">The name of the scene to switch to</param>
        public SceneSwitchProgress SwitchScene(string sceneName)
        {
            if (!networkingManager.IsServer)
            {
                throw new NotServerException("Only server can start a scene switch");
            }
            else if (isSwitching)
            {
                if (NetworkingManager.LogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Scene switch already in progress");
                return null;
            }
            else if (!registeredSceneNames.Contains(sceneName))
            {
                if (NetworkingManager.LogLevel <= LogLevel.Normal) NetworkLog.LogWarning("The scene " + sceneName + " is not registered as a switchable scene.");
                return null;
            }

            networkingManager.SpawnManager.ServerDestroySpawnedSceneObjects(); //Destroy current scene objects before switching.
            isSwitching = true;
            lastScene = SceneManager.GetActiveScene();

            SceneSwitchProgress switchSceneProgress = new SceneSwitchProgress(networkingManager);
            sceneSwitchProgresses.Add(switchSceneProgress.guid, switchSceneProgress);
            currentSceneSwitchProgressGuid = switchSceneProgress.guid;

            // Move ALL networked objects to the temp scene
            MoveObjectsToDontDestroyOnLoad();

            isSpawnedObjectsPendingInDontDestroyOnLoad = true;

            // Switch scene
            AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            nextSceneName = sceneName;

            sceneLoad.completed += (AsyncOperation asyncOp2) => { OnSceneLoaded(switchSceneProgress.guid, null); };

            switchSceneProgress.SetSceneLoadOperation(sceneLoad);

            if (OnSceneSwitchStarted != null)
            {
                OnSceneSwitchStarted(sceneLoad);
            }

            return switchSceneProgress;
        }

        // Called on client
        internal void OnSceneSwitch(uint sceneIndex, Guid switchSceneGuid, Stream objectStream)
        {
            if (!sceneIndexToString.ContainsKey(sceneIndex) || !registeredSceneNames.Contains(sceneIndexToString[sceneIndex]))
            {
                if (NetworkingManager.LogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Server requested a scene switch to a non registered scene");
                return;
            }

            lastScene = SceneManager.GetActiveScene();

            // Move ALL networked objects to the temp scene
            MoveObjectsToDontDestroyOnLoad();

            isSpawnedObjectsPendingInDontDestroyOnLoad = true;

            string sceneName = sceneIndexToString[sceneIndex];

            AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            nextSceneName = sceneName;

            sceneLoad.completed += (AsyncOperation asyncOp2) =>
            {
                OnSceneLoaded(switchSceneGuid, objectStream);
            };

            if (OnSceneSwitchStarted != null)
            {
                OnSceneSwitchStarted(sceneLoad);
            }
        }

        internal void OnFirstSceneSwitchSync(uint sceneIndex, Guid switchSceneGuid)
        {
            if (!sceneIndexToString.ContainsKey(sceneIndex) || !registeredSceneNames.Contains(sceneIndexToString[sceneIndex]))
            {
                if (NetworkingManager.LogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Server requested a scene switch to a non registered scene");
                return;
            }
            else if (SceneManager.GetActiveScene().name == sceneIndexToString[sceneIndex])
            {
                return; //This scene is already loaded. This usually happends at first load
            }

            lastScene = SceneManager.GetActiveScene();
            string sceneName = sceneIndexToString[sceneIndex];
            nextSceneName = sceneName;
            CurrentActiveSceneIndex = sceneNameToIndex[sceneName];

            isSpawnedObjectsPendingInDontDestroyOnLoad = true;
            SceneManager.LoadScene(sceneName);

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteByteArray(switchSceneGuid.ToByteArray());
                    networkingManager.MessageSender.Send(networkingManager.ServerClientId, MLAPIConstants.MLAPI_CLIENT_SWITCH_SCENE_COMPLETED, Transport.MLAPI_INTERNAL_CHANNEL, stream, SecuritySendFlags.None);
                }
            }

            isSwitching = false;
        }

        private void OnSceneLoaded(Guid switchSceneGuid, Stream objectStream)
        {
            CurrentActiveSceneIndex = sceneNameToIndex[nextSceneName];
            Scene nextScene = SceneManager.GetSceneByName(nextSceneName);
            SceneManager.SetActiveScene(nextScene);

            // Move all objects to the new scene
            MoveObjectsToScene(nextScene);

            isSpawnedObjectsPendingInDontDestroyOnLoad = false;

            currentSceneIndex = CurrentActiveSceneIndex;

            if (networkingManager.IsServer)
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
            List<NetworkedObject> newSceneObjects = new List<NetworkedObject>();

            {
                NetworkedObject[] networkedObjects = MonoBehaviour.FindObjectsOfType<NetworkedObject>();

                for (int i = 0; i < networkedObjects.Length; i++)
                {
                    if (networkedObjects[i].IsSceneObject == null)
                    {
                        networkingManager.SpawnManager.SpawnNetworkedObjectLocally(networkedObjects[i],
                            networkingManager.SpawnManager.GetNetworkObjectId(), true, false, null, null, false, 0, false, true);

                        newSceneObjects.Add(networkedObjects[i]);
                    }
                }
            }


            for (int j = 0; j < networkingManager.ConnectedClientsList.Count; j++)
            {
                if (networkingManager.ConnectedClientsList[j].ClientId != networkingManager.ServerClientId)
                {
                    using (PooledBitStream stream = PooledBitStream.Get())
                    {
                        using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                        {
                            writer.WriteUInt32Packed(CurrentActiveSceneIndex);
                            writer.WriteByteArray(switchSceneGuid.ToByteArray());

                            uint sceneObjectsToSpawn = 0;
                            for (int i = 0; i < newSceneObjects.Count; i++)
                            {
                                if (newSceneObjects[i].observers.Contains(networkingManager.ConnectedClientsList[j].ClientId))
                                    sceneObjectsToSpawn++;
                            }

                            writer.WriteUInt32Packed(sceneObjectsToSpawn);

                            for (int i = 0; i < newSceneObjects.Count; i++)
                            {
                                if (newSceneObjects[i].observers.Contains(networkingManager.ConnectedClientsList[j].ClientId))
                                {
                                    writer.WriteBool(newSceneObjects[i].IsPlayerObject);
                                    writer.WriteUInt64Packed(newSceneObjects[i].NetworkId);
                                    writer.WriteUInt64Packed(newSceneObjects[i].OwnerClientId);

                                    NetworkedObject parent = null;

                                    if (!newSceneObjects[i].AlwaysReplicateAsRoot && newSceneObjects[i].transform.parent != null)
                                    {
                                        parent = newSceneObjects[i].transform.parent.GetComponent<NetworkedObject>();
                                    }

                                    if (parent == null)
                                    {
                                        writer.WriteBool(false);
                                    }
                                    else
                                    {
                                        writer.WriteBool(true);
                                        writer.WriteUInt64Packed(parent.NetworkId);
                                    }

                                    if (!networkingManager.NetworkConfig.EnableSceneManagement || networkingManager.NetworkConfig.UsePrefabSync)
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
                                        writer.WriteUInt64Packed(newSceneObjects[i].NetworkedInstanceId);
                                    }

                                    if (networkingManager.NetworkConfig.EnableNetworkedVar)
                                    {
                                        newSceneObjects[i].WriteNetworkedVarData(stream, networkingManager.ConnectedClientsList[j].ClientId);
                                    }
                                }
                            }
                        }

                        networkingManager.MessageSender.Send(networkingManager.ConnectedClientsList[j].ClientId, MLAPIConstants.MLAPI_SWITCH_SCENE, Transport.MLAPI_INTERNAL_CHANNEL, stream, SecuritySendFlags.None);
                    }
                }
            }

            //Tell server that scene load is completed
            if (networkingManager.IsHost)
            {
                OnClientSwitchSceneCompleted(networkingManager.LocalClientId, switchSceneGuid);
            }

            isSwitching = false;

            if (OnSceneSwitched != null)
            {
                OnSceneSwitched();
            }
        }

        private void OnSceneUnloadClient(Guid switchSceneGuid, Stream objectStream)
        {
            if (!networkingManager.NetworkConfig.EnableSceneManagement || networkingManager.NetworkConfig.UsePrefabSync)
            {
                networkingManager.SpawnManager.DestroySceneObjects();

                using (PooledBitReader reader = networkingManager.PooledBitReaders.GetReader(objectStream))
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

                        NetworkedObject networkedObject = networkingManager.SpawnManager.CreateLocalNetworkedObject(false, 0, prefabHash, parentNetworkId, position, rotation);
                        networkingManager.SpawnManager.SpawnNetworkedObjectLocally(networkedObject, networkId, true, isPlayerObject, owner, objectStream, false, 0, true, false);

                        Queue<BufferManager.BufferedMessage> bufferQueue = BufferManager.ConsumeBuffersForNetworkId(networkId);

                        // Apply buffered messages
                        if (bufferQueue != null)
                        {
                            while (bufferQueue.Count > 0)
                            {
                                BufferManager.BufferedMessage message = bufferQueue.Dequeue();

                                networkingManager.HandleIncomingData(message.sender, message.channel, new ArraySegment<byte>(message.payload.GetBuffer(), (int)message.payload.Position, (int)message.payload.Length), message.receiveTime, false);

                                BufferManager.RecycleConsumedBufferedMessage(message);
                            }
                        }
                    }
                }
            }
            else
            {
                NetworkedObject[] networkedObjects = MonoBehaviour.FindObjectsOfType<NetworkedObject>();

                networkingManager.SpawnManager.ClientCollectSoftSyncSceneObjectSweep(networkedObjects);

                using (PooledBitReader reader = networkingManager.PooledBitReaders.GetReader(objectStream))
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

                        NetworkedObject networkedObject = networkingManager.SpawnManager.CreateLocalNetworkedObject(true, instanceId, 0, parentNetworkId, null, null);
                        networkingManager.SpawnManager.SpawnNetworkedObjectLocally(networkedObject, networkId, true, isPlayerObject, owner, objectStream, false, 0, true, false);

                        Queue<BufferManager.BufferedMessage> bufferQueue = BufferManager.ConsumeBuffersForNetworkId(networkId);

                        // Apply buffered messages
                        if (bufferQueue != null)
                        {
                            while (bufferQueue.Count > 0)
                            {
                                BufferManager.BufferedMessage message = bufferQueue.Dequeue();

                                networkingManager.HandleIncomingData(message.sender, message.channel, new ArraySegment<byte>(message.payload.GetBuffer(), (int)message.payload.Position, (int)message.payload.Length), message.receiveTime, false);

                                BufferManager.RecycleConsumedBufferedMessage(message);
                            }
                        }
                    }
                }
            }

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteByteArray(switchSceneGuid.ToByteArray());
                    networkingManager.MessageSender.Send(networkingManager.ServerClientId,
                        MLAPIConstants.MLAPI_CLIENT_SWITCH_SCENE_COMPLETED, Transport.MLAPI_INTERNAL_CHANNEL, stream, SecuritySendFlags.None);
                }
            }

            isSwitching = false;

            if (OnSceneSwitched != null)
            {
                OnSceneSwitched();
            }
        }

        internal bool HasSceneMismatch(uint sceneIndex)
        {
            return SceneManager.GetActiveScene().name != sceneIndexToString[sceneIndex];
        }

        // Called on server
        internal void OnClientSwitchSceneCompleted(ulong clientId, Guid switchSceneGuid)
        {
            if (switchSceneGuid == Guid.Empty)
            {
                //If Guid is empty it means the client has loaded the start scene of the server and the server would never have a switchSceneProgresses created for the start scene.
                return;
            }
            if (!sceneSwitchProgresses.ContainsKey(switchSceneGuid))
            {
                return;
            }

            sceneSwitchProgresses[switchSceneGuid].AddClientAsDone(clientId);
        }


        internal void RemoveClientFromSceneSwitchProgresses(ulong clientId)
        {
            foreach (SceneSwitchProgress switchSceneProgress in sceneSwitchProgresses.Values)
            {
                switchSceneProgress.RemoveClientAsDone(clientId);
            }
        }

        private void MoveObjectsToDontDestroyOnLoad()
        {
            // Move ALL networked objects to the temp scene
            HashSet<NetworkedObject> objectsToKeep = networkingManager.SpawnManager.SpawnedObjectsList;

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
            // Move ALL networked objects to the temp scene
            HashSet<NetworkedObject> objectsToKeep = networkingManager.SpawnManager.SpawnedObjectsList;

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
