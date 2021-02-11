using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MLAPI.Configuration;
using MLAPI.Exceptions;
using MLAPI.Hashing;
using MLAPI.Logging;
using MLAPI.Messaging;
using MLAPI.Security;
using MLAPI.Serialization.Pooled;
using MLAPI.Transports;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MLAPI.Spawning
{
    /// <summary>
    /// Class that handles object spawning
    /// </summary>
    public class SpawnManager
    {
        /// <summary>
        /// The currently spawned objects
        /// </summary>
        public readonly Dictionary<ulong, NetworkedObject> SpawnedObjects = new Dictionary<ulong, NetworkedObject>();
        // Pending SoftSync objects
        internal readonly Dictionary<ulong, NetworkedObject> pendingSoftSyncObjects = new Dictionary<ulong, NetworkedObject>();
        /// <summary>
        /// A list of the spawned objects
        /// </summary>
        public readonly HashSet<NetworkedObject> SpawnedObjectsList = new HashSet<NetworkedObject>();
        /// <summary>
        /// The delegate used when spawning a networked object
        /// </summary>
        /// <param name="position">The position to spawn the object at</param>
        /// <param name="rotation">The rotation to spawn the object with</param>
        public delegate NetworkedObject SpawnHandlerDelegate(Vector3 position, Quaternion rotation);
        /// <summary>
        /// The delegate used when destroying networked objects
        /// </summary>
        /// <param name="networkedObject">The networked object to be destroy</param>
        public delegate void DestroyHandlerDelegate(NetworkedObject networkedObject);

        internal readonly Dictionary<ulong, SpawnHandlerDelegate> customSpawnHandlers = new Dictionary<ulong, SpawnHandlerDelegate>();
        internal readonly Dictionary<ulong, DestroyHandlerDelegate> customDestroyHandlers = new Dictionary<ulong, DestroyHandlerDelegate>();

        private NetworkingManager networkingManager;

        internal SpawnManager(NetworkingManager manager )
        {
            networkingManager = manager;
        }

        /// <summary>
        /// Registers a delegate for spawning networked prefabs, useful for object pooling
        /// </summary>
        /// <param name="prefabHash">The prefab hash to spawn</param>
        /// <param name="handler">The delegate handler</param>
        public void RegisterSpawnHandler(ulong prefabHash, SpawnHandlerDelegate handler)
        {
            if (customSpawnHandlers.ContainsKey(prefabHash))
            {
                customSpawnHandlers[prefabHash] = handler;
            }
            else
            {
                customSpawnHandlers.Add(prefabHash, handler);
            }
        }

        /// <summary>
        /// Registers a delegate for destroying networked objects, useful for object pooling
        /// </summary>
        /// <param name="prefabHash">The prefab hash to destroy</param>
        /// <param name="handler">The delegate handler</param>
        public void RegisterCustomDestroyHandler(ulong prefabHash, DestroyHandlerDelegate handler)
        {
            if (customDestroyHandlers.ContainsKey(prefabHash))
            {
                customDestroyHandlers[prefabHash] = handler;
            }
            else
            {
                customDestroyHandlers.Add(prefabHash, handler);
            }
        }

        /// <summary>
        /// Removes the custom spawn handler for a specific prefab hash
        /// </summary>
        /// <param name="prefabHash">The prefab hash of the prefab spawn handler that is to be removed</param>
        public void RemoveCustomSpawnHandler(ulong prefabHash)
        {
            customSpawnHandlers.Remove(prefabHash);
        }

        /// <summary>
        /// Removes the custom destroy handler for a specific prefab hash
        /// </summary>
        /// <param name="prefabHash">The prefab hash of the prefab destroy handler that is to be removed</param>
        public void RemoveCustomDestroyHandler(ulong prefabHash)
        {
            customDestroyHandlers.Remove(prefabHash);
        }

        internal readonly Queue<ReleasedNetworkId> releasedNetworkObjectIds = new Queue<ReleasedNetworkId>();
        private ulong networkObjectIdCounter;
        internal ulong GetNetworkObjectId()
        {
            if (releasedNetworkObjectIds.Count > 0 && networkingManager.NetworkConfig.RecycleNetworkIds && (Time.unscaledTime - releasedNetworkObjectIds.Peek().ReleaseTime) >= networkingManager.NetworkConfig.NetworkIdRecycleDelay)
            {
                return releasedNetworkObjectIds.Dequeue().NetworkId;
            }
            else
            {
                networkObjectIdCounter++;
                return networkObjectIdCounter;
            }
        }

        /// <summary>
        /// Gets the prefab index of a given prefab hash
        /// </summary>
        /// <param name="hash">The hash of the prefab</param>
        /// <returns>The index of the prefab</returns>
        public int GetNetworkedPrefabIndexOfHash(ulong hash)
        {
            for (int i = 0; i < networkingManager.NetworkConfig.NetworkedPrefabs.Count; i++)
            {
                if (networkingManager.NetworkConfig.NetworkedPrefabs[i].Hash == hash)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Returns the prefab hash for the networked prefab with a given index
        /// </summary>
        /// <param name="index">The networked prefab index</param>
        /// <returns>The prefab hash for the given prefab index</returns>
        public ulong GetPrefabHashFromIndex(int index)
        {
            return networkingManager.NetworkConfig.NetworkedPrefabs[index].Hash;
        }

        /// <summary>
        /// Returns the prefab hash for a given prefab hash generator
        /// </summary>
        /// <param name="generator">The prefab hash generator</param>
        /// <returns>The hash for the given generator</returns>
        public ulong GetPrefabHashFromGenerator(string generator)
        {
            return generator.GetStableHash64();
        }

        /// <summary>
        /// Returns the local player object or null if one does not exist
        /// </summary>
        /// <returns>The local player object or null if one does not exist</returns>
        public NetworkedObject GetLocalPlayerObject()
        {
            if (!networkingManager.ConnectedClients.ContainsKey(networkingManager.LocalClientId)) return null;
            return networkingManager.ConnectedClients[networkingManager.LocalClientId].PlayerObject;
        }

        /// <summary>
        /// Returns the player object with a given clientId or null if one does not exist
        /// </summary>
        /// <returns>The player object with a given clientId or null if one does not exist</returns>
        public NetworkedObject GetPlayerObject(ulong clientId)
        {
            if (!networkingManager.ConnectedClients.ContainsKey(clientId)) return null;
            return networkingManager.ConnectedClients[clientId].PlayerObject;
        }

        internal void RemoveOwnership(NetworkedObject netObject)
        {
            if (!networkingManager.IsServer)
            {
                throw new NotServerException("Only the server can change ownership");
            }

            if (!netObject.IsSpawned)
            {
                throw new SpawnStateException("Object is not spawned");
            }

            for (int i = networkingManager.ConnectedClients[netObject.OwnerClientId].OwnedObjects.Count - 1; i > -1; i--)
            {
                if (networkingManager.ConnectedClients[netObject.OwnerClientId].OwnedObjects[i] == netObject)
                    networkingManager.ConnectedClients[netObject.OwnerClientId].OwnedObjects.RemoveAt(i);
            }

            netObject._ownerClientId = null;

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt64Packed(netObject.NetworkId);
                    writer.WriteUInt64Packed(netObject.OwnerClientId);

                    networkingManager.MessageSender.Send(MLAPIConstants.MLAPI_CHANGE_OWNER, Transport.MLAPI_INTERNAL_CHANNEL, stream, SecuritySendFlags.None);
                }
            }
        }

        internal void ChangeOwnership(NetworkedObject netObject, ulong clientId)
        {
            if (!networkingManager.IsServer)
            {
                throw new NotServerException("Only the server can change ownership");
            }

            if (!netObject.IsSpawned)
            {
                throw new SpawnStateException("Object is not spawned");
            }

            if (networkingManager.ConnectedClients.ContainsKey(netObject.OwnerClientId))
            {
                for (int i = networkingManager.ConnectedClients[netObject.OwnerClientId].OwnedObjects.Count - 1; i >= 0; i--)
                {
                    if (networkingManager.ConnectedClients[netObject.OwnerClientId].OwnedObjects[i] == netObject)
                        networkingManager.ConnectedClients[netObject.OwnerClientId].OwnedObjects.RemoveAt(i);
                }
            }

            networkingManager.ConnectedClients[clientId].OwnedObjects.Add(netObject);
            netObject.OwnerClientId = clientId;

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt64Packed(netObject.NetworkId);
                    writer.WriteUInt64Packed(clientId);

                    networkingManager.MessageSender.Send(MLAPIConstants.MLAPI_CHANGE_OWNER, Transport.MLAPI_INTERNAL_CHANNEL, stream, SecuritySendFlags.None);
                }
            }
        }

        // Only ran on Client
        internal NetworkedObject CreateLocalNetworkedObject(bool softCreate, ulong instanceId, ulong prefabHash, ulong? parentNetworkId, Vector3? position, Quaternion? rotation)
        {
            NetworkedObject parent = null;

            if (parentNetworkId != null && SpawnedObjects.ContainsKey(parentNetworkId.Value))
            {
                parent = SpawnedObjects[parentNetworkId.Value];
            }
            else if (parentNetworkId != null)
            {
                if (NetworkingManager.LogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Cannot find parent. Parent objects always have to be spawned and replicated BEFORE the child");
            }

            if (!networkingManager.NetworkConfig.EnableSceneManagement || networkingManager.NetworkConfig.UsePrefabSync || !softCreate)
            {
                // Create the object
                if (customSpawnHandlers.ContainsKey(prefabHash))
                {
                    NetworkedObject networkedObject = customSpawnHandlers[prefabHash](position.GetValueOrDefault(Vector3.zero), rotation.GetValueOrDefault(Quaternion.identity));

                    if (parent != null)
                    {
                        networkedObject.transform.SetParent(parent.transform, true);
                    }

                    if (networkingManager.NetworkSceneManager.isSpawnedObjectsPendingInDontDestroyOnLoad)
                    {
                        GameObject.DontDestroyOnLoad(networkedObject.gameObject);
                    }

                    return networkedObject;
                }
                else
                {
                    int prefabIndex = GetNetworkedPrefabIndexOfHash(prefabHash);

                    if (prefabIndex < 0)
                    {
                        if (NetworkingManager.LogLevel <= LogLevel.Error) NetworkLog.LogError("Failed to create object locally. [PrefabHash=" + prefabHash + "]. Hash could not be found. Is the prefab registered?");

                        return null;
                    }
                    else
                    {
                        GameObject prefab = networkingManager.NetworkConfig.NetworkedPrefabs[prefabIndex].Prefab;

                        Scene old_scene = SceneManager.GetActiveScene();
                        NetworkedObject networkedObject;
                        try
                        {
                            SceneManager.SetActiveScene(networkingManager.gameObject.scene);
                            networkedObject = ((position == null && rotation == null) ?
                                MonoBehaviour.Instantiate(prefab) :
                                MonoBehaviour.Instantiate(prefab, position.GetValueOrDefault(Vector3.zero), rotation.GetValueOrDefault(Quaternion.identity))).GetComponent<NetworkedObject>();
                        }
                        finally
                        {
                            SceneManager.SetActiveScene(old_scene);
                        }

                        if (parent != null)
                        {
                            networkedObject.transform.SetParent(parent.transform, true);
                        }

                        if (networkingManager.NetworkSceneManager.isSpawnedObjectsPendingInDontDestroyOnLoad)
                        {
                            GameObject.DontDestroyOnLoad(networkedObject.gameObject);
                        }

                        return networkedObject;
                    }
                }
            }
            else
            {
                // SoftSync them by mapping
                if (!pendingSoftSyncObjects.ContainsKey(instanceId))
                {
                    // TODO: Fix this message
                    if (NetworkingManager.LogLevel <= LogLevel.Error) NetworkLog.LogError("Cannot find pending soft sync object. Is the projects the same?");
                    return null;
                }

                NetworkedObject networkedObject = pendingSoftSyncObjects[instanceId];
                pendingSoftSyncObjects.Remove(instanceId);

                if (parent != null)
                {
                    networkedObject.transform.SetParent(parent.transform, true);
                }

                return networkedObject;
            }
        }

        // Ran on both server and client
        internal void SpawnNetworkedObjectLocally(NetworkedObject netObject, ulong networkId, bool sceneObject, bool playerObject, ulong? ownerClientId, Stream dataStream, bool readPayload, int payloadLength, bool readNetworkedVar, bool destroyWithScene)
        {
            if (netObject == null)
            {
                throw new ArgumentNullException(nameof(netObject), "Cannot spawn null object");
            }

            if (netObject.IsSpawned)
            {
                throw new SpawnStateException("Object is already spawned");
            }

            if (readNetworkedVar && networkingManager.NetworkConfig.EnableNetworkedVar)
            {
                netObject.SetNetworkedVarData(dataStream);
            }

            if (SpawnedObjects.ContainsKey(netObject.NetworkId)) return;

            netObject.IsSpawned = true;

            netObject.IsSceneObject = sceneObject;
            netObject.NetworkId = networkId;

            netObject.DestroyWithScene = sceneObject || destroyWithScene;

            netObject._ownerClientId = ownerClientId;
            netObject.IsPlayerObject = playerObject;

            SpawnedObjects.Add(netObject.NetworkId, netObject);
            SpawnedObjectsList.Add(netObject);

            if (ownerClientId != null)
            {
                if (networkingManager.IsServer)
                {
                    if (playerObject)
                    {
                        networkingManager.ConnectedClients[ownerClientId.Value].PlayerObject = netObject;
                    }
                    else
                    {
                        networkingManager.ConnectedClients[ownerClientId.Value].OwnedObjects.Add(netObject);
                    }
                }
                else if (playerObject && ownerClientId.Value == networkingManager.LocalClientId)
                {
                    networkingManager.ConnectedClients[ownerClientId.Value].PlayerObject = netObject;
                }
            }

            if (networkingManager.IsServer)
            {
                for (int i = 0; i < networkingManager.ConnectedClientsList.Count; i++)
                {
                    if (netObject.CheckObjectVisibility == null || netObject.CheckObjectVisibility(networkingManager.ConnectedClientsList[i].ClientId))
                    {
                        netObject.observers.Add(networkingManager.ConnectedClientsList[i].ClientId);
                    }
                }
            }

            netObject.ResetNetworkedStartInvoked();

            if (readPayload)
            {
                using (PooledBitStream payloadStream = PooledBitStream.Get())
                {
                    payloadStream.CopyUnreadFrom(dataStream, payloadLength);
                    dataStream.Position += payloadLength;
                    payloadStream.Position = 0;
                    netObject.InvokeBehaviourNetworkSpawn(payloadStream);
                }
            }
            else
            {
                netObject.InvokeBehaviourNetworkSpawn(null);
            }
        }

        internal void SendSpawnCallForObject(ulong clientId, NetworkedObject netObject, Stream payload)
        {
            //Currently, if this is called and the clientId (destination) is the server's client Id, this case
            //will be checked within the below Send function.  To avoid unwarranted allocation of a PooledBitStream
            //placing this check here. [NSS]
            if (networkingManager.IsServer && clientId == networkingManager.ServerClientId)
            {
                return;
            }

            RpcQueueContainer rpcQueueContainer = networkingManager.rpcQueueContainer;

            var stream = PooledBitStream.Get();
            WriteSpawnCallForObject(stream, clientId, netObject, payload);

            var QueueItem = new FrameQueueItem
            {
                updateStage = NetworkUpdateManager.NetworkUpdateStages.Update,
                queueItemType = RpcQueueContainer.QueueItemType.CreateObject,
                networkId = 0,
                itemStream = stream,
                channel = Transport.MLAPI_INTERNAL_CHANNEL,
                sendFlags = SecuritySendFlags.None,
                clientIds = new[] {clientId}
            };
            rpcQueueContainer.AddToInternalMLAPISendQueue(QueueItem);
        }

        internal void WriteSpawnCallForObject(Serialization.BitStream stream, ulong clientId, NetworkedObject netObject, Stream payload)
        {
            using (PooledBitWriter writer = PooledBitWriter.Get(stream))
            {
                writer.WriteBool(netObject.IsPlayerObject);
                writer.WriteUInt64Packed(netObject.NetworkId);
                writer.WriteUInt64Packed(netObject.OwnerClientId);

                NetworkedObject parent = null;

                if (!netObject.AlwaysReplicateAsRoot && netObject.transform.parent != null)
                {
                    parent = netObject.transform.parent.GetComponent<NetworkedObject>();
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
                    writer.WriteUInt64Packed(netObject.PrefabHash);
                }
                else
                {
                    writer.WriteBool(netObject.IsSceneObject == null ? true : netObject.IsSceneObject.Value);

                    if (netObject.IsSceneObject == null || netObject.IsSceneObject.Value)
                    {
                        writer.WriteUInt64Packed(netObject.NetworkedInstanceId);
                    }
                    else
                    {
                        writer.WriteUInt64Packed(netObject.PrefabHash);
                    }
                }

                if (netObject.IncludeTransformWhenSpawning == null || netObject.IncludeTransformWhenSpawning(clientId))
                {
                    writer.WriteBool(true);
                    writer.WriteSinglePacked(netObject.transform.position.x);
                    writer.WriteSinglePacked(netObject.transform.position.y);
                    writer.WriteSinglePacked(netObject.transform.position.z);

                    writer.WriteSinglePacked(netObject.transform.rotation.eulerAngles.x);
                    writer.WriteSinglePacked(netObject.transform.rotation.eulerAngles.y);
                    writer.WriteSinglePacked(netObject.transform.rotation.eulerAngles.z);
                } else
                {
                    writer.WriteBool(false);
                }

                writer.WriteBool(payload != null);

                if (payload != null)
                {
                    writer.WriteInt32Packed((int) payload.Length);
                }

                if (networkingManager.NetworkConfig.EnableNetworkedVar)
                {
                    netObject.WriteNetworkedVarData(stream, clientId);
                }

                if (payload != null) stream.CopyFrom(payload);
            }
        }

        internal void UnSpawnObject(NetworkedObject netObject, bool destroyObject = false)
        {
            if (!netObject.IsSpawned)
            {
                throw new SpawnStateException("Object is not spawned");
            }

            if (!networkingManager.IsServer)
            {
                throw new NotServerException("Only server unspawn objects");
            }

            OnDestroyObject(netObject.NetworkId, destroyObject);
        }

        // Makes scene objects ready to be reused
        internal void ServerResetShudownStateForSceneObjects()
        {
            foreach (var sobj in SpawnedObjectsList)
            {
                if ((sobj.IsSceneObject != null && sobj.IsSceneObject == true) || sobj.DestroyWithScene)
                {
                    sobj.IsSpawned = false;
                    sobj.DestroyWithScene = false;
                    sobj.IsSceneObject = null;
                }
            }
        }

        internal void ServerDestroySpawnedSceneObjects()
        {
            foreach (var sobj in SpawnedObjectsList)
            {
                if ((sobj.IsSceneObject != null && sobj.IsSceneObject == true) || sobj.DestroyWithScene)
                {
                    if (customDestroyHandlers.ContainsKey(sobj.PrefabHash))
                    {
                        customDestroyHandlers[sobj.PrefabHash](sobj);
                        OnDestroyObject(sobj.NetworkId, false);
                    }
                    else
                    {
                        MonoBehaviour.Destroy(sobj.gameObject);
                    }
                }
            }
        }

        internal void DestroyNonSceneObjects()
        {
            NetworkedObject[] netObjects = MonoBehaviour.FindObjectsOfType<NetworkedObject>();

            for (int i = 0; i < netObjects.Length; i++)
            {
                if (netObjects[i].IsSceneObject != null && netObjects[i].IsSceneObject.Value == false)
                {
                    if (customDestroyHandlers.ContainsKey(netObjects[i].PrefabHash))
                    {
                        customDestroyHandlers[netObjects[i].PrefabHash](netObjects[i]);
                        OnDestroyObject(netObjects[i].NetworkId, false);
                    }
                    else
                    {
                        MonoBehaviour.Destroy(netObjects[i].gameObject);
                    }
                }
            }
        }

        internal void DestroySceneObjects()
        {
            NetworkedObject[] netObjects = MonoBehaviour.FindObjectsOfType<NetworkedObject>();

            for (int i = 0; i < netObjects.Length; i++)
            {
                if (netObjects[i].IsSceneObject == null || netObjects[i].IsSceneObject.Value == true)
                {
                    if (customDestroyHandlers.ContainsKey(netObjects[i].PrefabHash))
                    {
                        customDestroyHandlers[netObjects[i].PrefabHash](netObjects[i]);
                        OnDestroyObject(netObjects[i].NetworkId, false);
                    }
                    else
                    {
                        MonoBehaviour.Destroy(netObjects[i].gameObject);
                    }
                }
            }
        }

        internal void CleanDiffedSceneObjects()
        {
            // Clean up the diffed scene objects. I.E scene objects that have been destroyed
            if (pendingSoftSyncObjects.Count > 0)
            {
                List<NetworkedObject> objectsToDestroy = new List<NetworkedObject>();

                foreach (KeyValuePair<ulong, NetworkedObject> pair in pendingSoftSyncObjects)
                {
                    objectsToDestroy.Add(pair.Value);
                }

                for (int i = 0; i < objectsToDestroy.Count; i++)
                {
                    MonoBehaviour.Destroy(objectsToDestroy[i].gameObject);
                }
            }
        }

        internal void ServerSpawnSceneObjectsOnStartSweep()
        {
            NetworkedObject[] networkedObjects = MonoBehaviour.FindObjectsOfType<NetworkedObject>();

            for (int i = 0; i < networkedObjects.Length; i++)
            {
                if (networkedObjects[i].IsSceneObject == null)
                {
                    SpawnNetworkedObjectLocally(networkedObjects[i], GetNetworkObjectId(), true, false, null, null, false, 0, false, true);
                }
            }
        }

        internal void ClientCollectSoftSyncSceneObjectSweep(NetworkedObject[] networkedObjects)
        {
            if (networkedObjects == null)
                networkedObjects = MonoBehaviour.FindObjectsOfType<NetworkedObject>();

            for (int i = 0; i < networkedObjects.Length; i++)
            {
                if (networkedObjects[i].IsSceneObject == null)
                {
                    pendingSoftSyncObjects.Add(networkedObjects[i].NetworkedInstanceId, networkedObjects[i]);
                }
            }
        }

        internal void OnDestroyObject(ulong networkId, bool destroyGameObject)
        {
            if (networkingManager == null)
                return;

            //Removal of spawned object
            if (!SpawnedObjects.ContainsKey(networkId))
            {
                Debug.LogWarning("Trying to destroy object " + networkId.ToString() + " but it doesn't seem to exist anymore!");
                return;
            }

            var sobj = SpawnedObjects[networkId];

            if (!sobj.IsOwnedByServer && !sobj.IsPlayerObject &&
                networkingManager.ConnectedClients.ContainsKey(sobj.OwnerClientId))
            {
                //Someone owns it.
                for (int i = networkingManager.ConnectedClients[sobj.OwnerClientId].OwnedObjects.Count - 1; i > -1; i--)
                {
                    if (networkingManager.ConnectedClients[sobj.OwnerClientId].OwnedObjects[i].NetworkId == networkId)
                        networkingManager.ConnectedClients[sobj.OwnerClientId].OwnedObjects.RemoveAt(i);
                }
            }

            sobj.IsSpawned = false;

            if (networkingManager != null && networkingManager.IsServer)
            {
                if (networkingManager.NetworkConfig.RecycleNetworkIds)
                {
                    releasedNetworkObjectIds.Enqueue(new ReleasedNetworkId()
                    {
                        NetworkId = networkId,
                        ReleaseTime = Time.unscaledTime
                    });
                }

                var rpcQueueContainer = networkingManager.rpcQueueContainer;
                if (rpcQueueContainer != null)
                {
                    if (sobj != null)
                    {
                        // As long as we have any remaining clients, then notify of the object being destroy.
                        if (networkingManager.ConnectedClientsList.Count > 0)
                        {
                            var stream = PooledBitStream.Get();
                            using (var writer = PooledBitWriter.Get(stream))
                            {
                                writer.WriteUInt64Packed(networkId);

                                var QueueItem = new FrameQueueItem
                                {
                                    queueItemType = RpcQueueContainer.QueueItemType.DestroyObject,
                                    networkId = networkId,
                                    itemStream = stream,
                                    channel = Transport.MLAPI_INTERNAL_CHANNEL,
                                    sendFlags = SecuritySendFlags.None,
                                    clientIds = networkingManager.ConnectedClientsList.Select(c => c.ClientId).ToArray()
                                };
                                rpcQueueContainer.AddToInternalMLAPISendQueue(QueueItem);
                            }
                        }
                    }
                }
            }

            GameObject go = sobj.gameObject;

            if (destroyGameObject && go != null)
            {
                if (customDestroyHandlers.ContainsKey(sobj.PrefabHash))
                {
                    customDestroyHandlers[sobj.PrefabHash](sobj);
                    OnDestroyObject(networkId, false);
                }
                else
                {
                    MonoBehaviour.Destroy(go);
                }
            }

            // for some reason, we can get down here and SpawnedObjects for this
            //  networkId will no longer be here, even as we check this at the start
            //  of the function
            if (SpawnedObjects.ContainsKey(networkId))
            {
                SpawnedObjectsList.Remove(sobj);
                SpawnedObjects.Remove(networkId);
            }
        }
    }
}
