using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MLAPI.Configuration;
using MLAPI.Exceptions;
using MLAPI.Hashing;
using MLAPI.Logging;
using MLAPI.Messaging;
using MLAPI.SceneManagement;
using MLAPI.Serialization.Pooled;
using MLAPI.Transports;
using UnityEngine;

namespace MLAPI.Spawning
{
    /// <summary>
    /// Class that handles object spawning
    /// </summary>
    public static class NetworkSpawnManager
    {
        /// <summary>
        /// The currently spawned objects
        /// </summary>
        public static readonly Dictionary<ulong, NetworkObject> SpawnedObjects = new Dictionary<ulong, NetworkObject>();
        // Pending SoftSync objects
        internal static readonly Dictionary<ulong, NetworkObject> pendingSoftSyncObjects = new Dictionary<ulong, NetworkObject>();
        /// <summary>
        /// A list of the spawned objects
        /// </summary>
        public static readonly HashSet<NetworkObject> SpawnedObjectsList = new HashSet<NetworkObject>();
        /// <summary>
        /// The delegate used when spawning a NetworkObject
        /// </summary>
        /// <param name="position">The position to spawn the object at</param>
        /// <param name="rotation">The rotation to spawn the object with</param>
        public delegate NetworkObject SpawnHandlerDelegate(Vector3 position, Quaternion rotation);
        /// <summary>
        /// The delegate used when destroying NetworkObjects
        /// </summary>
        /// <param name="networkObject">The NetworkObject to be destroy</param>
        public delegate void DestroyHandlerDelegate(NetworkObject networkObject);

        internal static readonly Dictionary<ulong, SpawnHandlerDelegate> customSpawnHandlers = new Dictionary<ulong, SpawnHandlerDelegate>();
        internal static readonly Dictionary<ulong, DestroyHandlerDelegate> customDestroyHandlers = new Dictionary<ulong, DestroyHandlerDelegate>();

        /// <summary>
        /// Registers a delegate for spawning NetworkPrefabs, useful for object pooling
        /// </summary>
        /// <param name="prefabHash">The prefab hash to spawn</param>
        /// <param name="handler">The delegate handler</param>
        public static void RegisterSpawnHandler(ulong prefabHash, SpawnHandlerDelegate handler)
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
        /// Registers a delegate for destroying NetworkObjects, useful for object pooling
        /// </summary>
        /// <param name="prefabHash">The prefab hash to destroy</param>
        /// <param name="handler">The delegate handler</param>
        public static void RegisterCustomDestroyHandler(ulong prefabHash, DestroyHandlerDelegate handler)
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
        public static void RemoveCustomSpawnHandler(ulong prefabHash)
        {
            customSpawnHandlers.Remove(prefabHash);
        }

        /// <summary>
        /// Removes the custom destroy handler for a specific prefab hash
        /// </summary>
        /// <param name="prefabHash">The prefab hash of the prefab destroy handler that is to be removed</param>
        public static void RemoveCustomDestroyHandler(ulong prefabHash)
        {
            customDestroyHandlers.Remove(prefabHash);
        }

        internal static readonly Queue<ReleasedNetworkId> releasedNetworkObjectIds = new Queue<ReleasedNetworkId>();
        private static ulong networkObjectIdCounter;
        internal static ulong GetNetworkObjectId()
        {
            if (releasedNetworkObjectIds.Count > 0 && NetworkManager.Singleton.NetworkConfig.RecycleNetworkIds && (Time.unscaledTime - releasedNetworkObjectIds.Peek().ReleaseTime) >= NetworkManager.Singleton.NetworkConfig.NetworkIdRecycleDelay)
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
        public static int GetNetworkPrefabIndexOfHash(ulong hash)
        {
            for (int i = 0; i < NetworkManager.Singleton.NetworkConfig.NetworkPrefabs.Count; i++)
            {
                if (NetworkManager.Singleton.NetworkConfig.NetworkPrefabs[i].Hash == hash)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Returns the prefab hash for the NetworkPrefab with a given index
        /// </summary>
        /// <param name="index">The NetworkPrefab index</param>
        /// <returns>The prefab hash for the given prefab index</returns>
        public static ulong GetPrefabHashFromIndex(int index)
        {
            return NetworkManager.Singleton.NetworkConfig.NetworkPrefabs[index].Hash;
        }

        /// <summary>
        /// Returns the prefab hash for a given prefab hash generator
        /// </summary>
        /// <param name="generator">The prefab hash generator</param>
        /// <returns>The hash for the given generator</returns>
        public static ulong GetPrefabHashFromGenerator(string generator)
        {
            return generator.GetStableHash64();
        }

        /// <summary>
        /// Returns the local player object or null if one does not exist
        /// </summary>
        /// <returns>The local player object or null if one does not exist</returns>
        public static NetworkObject GetLocalPlayerObject()
        {
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(NetworkManager.Singleton.LocalClientId)) return null;
            return NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject;
        }

        /// <summary>
        /// Returns the player object with a given clientId or null if one does not exist
        /// </summary>
        /// <returns>The player object with a given clientId or null if one does not exist</returns>
        public static NetworkObject GetPlayerNetworkObject(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) return null;
            return NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        }

        internal static void RemoveOwnership(NetworkObject netObject)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                throw new NotServerException("Only the server can change ownership");
            }

            if (!netObject.IsSpawned)
            {
                throw new SpawnStateException("Object is not spawned");
            }

            for (int i = NetworkManager.Singleton.ConnectedClients[netObject.OwnerClientId].OwnedObjects.Count - 1; i > -1; i--)
            {
                if (NetworkManager.Singleton.ConnectedClients[netObject.OwnerClientId].OwnedObjects[i] == netObject)
                    NetworkManager.Singleton.ConnectedClients[netObject.OwnerClientId].OwnedObjects.RemoveAt(i);
            }

			netObject._ownerClientId = null;

            using (PooledNetworkBuffer buffer = PooledNetworkBuffer.Get())
            {
                using (PooledNetworkWriter writer = PooledNetworkWriter.Get(buffer))
                {
                    writer.WriteUInt64Packed(netObject.NetworkObjectId);
                    writer.WriteUInt64Packed(netObject.OwnerClientId);

                    InternalMessageSender.Send(NetworkConstants.CHANGE_OWNER, NetworkChannel.Internal, buffer);
                }
            }
        }

        internal static void ChangeOwnership(NetworkObject netObject, ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                throw new NotServerException("Only the server can change ownership");
            }

            if (!netObject.IsSpawned)
            {
                throw new SpawnStateException("Object is not spawned");
            }

            if (NetworkManager.Singleton.ConnectedClients.ContainsKey(netObject.OwnerClientId))
            {
                for (int i = NetworkManager.Singleton.ConnectedClients[netObject.OwnerClientId].OwnedObjects.Count - 1; i >= 0; i--)
                {
                    if (NetworkManager.Singleton.ConnectedClients[netObject.OwnerClientId].OwnedObjects[i] == netObject)
                        NetworkManager.Singleton.ConnectedClients[netObject.OwnerClientId].OwnedObjects.RemoveAt(i);
                }
            }

            NetworkManager.Singleton.ConnectedClients[clientId].OwnedObjects.Add(netObject);
            netObject.OwnerClientId = clientId;

            using (PooledNetworkBuffer buffer = PooledNetworkBuffer.Get())
            {
                using (PooledNetworkWriter writer = PooledNetworkWriter.Get(buffer))
                {
                    writer.WriteUInt64Packed(netObject.NetworkObjectId);
                    writer.WriteUInt64Packed(clientId);

                    InternalMessageSender.Send(NetworkConstants.CHANGE_OWNER, NetworkChannel.Internal, buffer);
                }
            }
        }

        // Only ran on Client
        internal static NetworkObject CreateLocalNetworkObject(bool softCreate, ulong instanceId, ulong prefabHash, ulong? parentNetworkId, Vector3? position, Quaternion? rotation)
        {
            NetworkObject parent = null;

            if (parentNetworkId != null && SpawnedObjects.ContainsKey(parentNetworkId.Value))
            {
                parent = SpawnedObjects[parentNetworkId.Value];
            }
            else if (parentNetworkId != null)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Cannot find parent. Parent objects always have to be spawned and replicated BEFORE the child");
            }

            if (!NetworkManager.Singleton.NetworkConfig.EnableSceneManagement || NetworkManager.Singleton.NetworkConfig.UsePrefabSync || !softCreate)
            {
                // Create the object
                if (customSpawnHandlers.ContainsKey(prefabHash))
                {
                    NetworkObject networkObject = customSpawnHandlers[prefabHash](position.GetValueOrDefault(Vector3.zero), rotation.GetValueOrDefault(Quaternion.identity));

                    if (!ReferenceEquals(parent, null))
                    {
                        networkObject.transform.SetParent(parent.transform, true);
                    }

                    if (NetworkSceneManager.isSpawnedObjectsPendingInDontDestroyOnLoad)
                    {
                        GameObject.DontDestroyOnLoad(networkObject.gameObject);
                    }

                    return networkObject;
                }
                else
                {
                    int prefabIndex = GetNetworkPrefabIndexOfHash(prefabHash);

                    if (prefabIndex < 0)
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                        {
                            NetworkLog.LogError("Failed to create object locally. [PrefabHash=" + prefabHash + "]. Hash could not be found. Is the prefab registered?");
                        }

                        return null;
                    }
                    else
                    {
                        GameObject prefab = NetworkManager.Singleton.NetworkConfig.NetworkPrefabs[prefabIndex].Prefab;

                        NetworkObject networkObject = ((position == null && rotation == null) ? MonoBehaviour.Instantiate(prefab) : MonoBehaviour.Instantiate(prefab, position.GetValueOrDefault(Vector3.zero), rotation.GetValueOrDefault(Quaternion.identity))).GetComponent<NetworkObject>();

                        if (!ReferenceEquals(parent, null))
                        {
                            networkObject.transform.SetParent(parent.transform, true);
                        }

                        if (NetworkSceneManager.isSpawnedObjectsPendingInDontDestroyOnLoad)
                        {
                            GameObject.DontDestroyOnLoad(networkObject.gameObject);
                        }

                        return networkObject;
                    }
                }
            }
            else
            {
                // SoftSync them by mapping
                if (!pendingSoftSyncObjects.ContainsKey(instanceId))
                {
                    // TODO: Fix this message
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Error) NetworkLog.LogError("Cannot find pending soft sync object. Is the projects the same?");
                    return null;
                }

                NetworkObject networkObject = pendingSoftSyncObjects[instanceId];
                pendingSoftSyncObjects.Remove(instanceId);

                if (!ReferenceEquals(parent, null))
                {
                    networkObject.transform.SetParent(parent.transform, true);
                }

                return networkObject;
            }
        }

        // Ran on both server and client
        internal static void SpawnNetworkObjectLocally(NetworkObject netObject, ulong networkId, bool sceneObject, bool playerObject, ulong? ownerClientId, Stream dataStream, bool readPayload, int payloadLength, bool readNetworkVariable, bool destroyWithScene)
        {
            if (ReferenceEquals(netObject, null))
            {
                throw new ArgumentNullException(nameof(netObject), "Cannot spawn null object");
            }

            if (netObject.IsSpawned)
            {
                throw new SpawnStateException("Object is already spawned");
            }

            if (readNetworkVariable && NetworkManager.Singleton.NetworkConfig.EnableNetworkVariable)
            {
                netObject.SetNetworkVariableData(dataStream);
            }

            if (SpawnedObjects.ContainsKey(netObject.NetworkObjectId)) return;

            netObject.IsSpawned = true;

            netObject.IsSceneObject = sceneObject;
            netObject.NetworkObjectId = networkId;

            netObject.DestroyWithScene = sceneObject || destroyWithScene;

            netObject._ownerClientId = ownerClientId;
            netObject.IsPlayerObject = playerObject;

            SpawnedObjects.Add(netObject.NetworkObjectId, netObject);
            SpawnedObjectsList.Add(netObject);

            if (ownerClientId != null)
            {
                if (NetworkManager.Singleton.IsServer)
                {
                    if (playerObject)
                    {
                        NetworkManager.Singleton.ConnectedClients[ownerClientId.Value].PlayerObject = netObject;
                    }
                    else
                    {
                        NetworkManager.Singleton.ConnectedClients[ownerClientId.Value].OwnedObjects.Add(netObject);
                    }
                }
                else if (playerObject && ownerClientId.Value == NetworkManager.Singleton.LocalClientId)
                {
                    NetworkManager.Singleton.ConnectedClients[ownerClientId.Value].PlayerObject = netObject;
                }
            }

            if (NetworkManager.Singleton.IsServer)
            {
                for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
                {
                    if (netObject.CheckObjectVisibility == null || netObject.CheckObjectVisibility(NetworkManager.Singleton.ConnectedClientsList[i].ClientId))
                    {
                        netObject.observers.Add(NetworkManager.Singleton.ConnectedClientsList[i].ClientId);
                    }
                }
            }

            netObject.ResetNetworkStartInvoked();

            if (readPayload)
            {
                using (PooledNetworkBuffer payloadBuffer = PooledNetworkBuffer.Get())
                {
                    payloadBuffer.CopyUnreadFrom(dataStream, payloadLength);
                    dataStream.Position += payloadLength;
                    payloadBuffer.Position = 0;
                    netObject.InvokeBehaviourNetworkSpawn(payloadBuffer);
                }
            }
            else
            {
                netObject.InvokeBehaviourNetworkSpawn(null);
            }
        }

        internal static void SendSpawnCallForObject(ulong clientId, NetworkObject netObject, Stream payload)
        {
            //Currently, if this is called and the clientId (destination) is the server's client Id, this case
            //will be checked within the below Send function.  To avoid unwarranted allocation of a PooledNetworkBuffer
            //placing this check here. [NSS]
            if (NetworkManager.Singleton.IsServer && clientId == NetworkManager.Singleton.ServerClientId)
            {
                return;
            }

            RpcQueueContainer rpcQueueContainer = NetworkManager.Singleton.rpcQueueContainer;

            var stream = PooledNetworkBuffer.Get();
            WriteSpawnCallForObject(stream, clientId, netObject, payload);

            var queueItem = new RpcFrameQueueItem
            {
                updateStage = NetworkUpdateStage.Update,
                queueCreationTime = Time.realtimeSinceStartup,
                queueItemType = RpcQueueContainer.QueueItemType.CreateObject,
                networkId = 0,
                itemBuffer = stream,
                networkChannel = NetworkChannel.Internal,
                clientIds = new[] {clientId}
            };
            rpcQueueContainer.AddToInternalMLAPISendQueue(queueItem);
        }

        internal static void WriteSpawnCallForObject(Serialization.NetworkBuffer buffer, ulong clientId, NetworkObject netObject, Stream payload)
        {
            using (PooledNetworkWriter writer = PooledNetworkWriter.Get(buffer))
            {
                writer.WriteBool(netObject.IsPlayerObject);
                writer.WriteUInt64Packed(netObject.NetworkObjectId);
                writer.WriteUInt64Packed(netObject.OwnerClientId);

                NetworkObject parent = null;

                if (!netObject.AlwaysReplicateAsRoot && netObject.transform.parent != null)
                {
                    parent = netObject.transform.parent.GetComponent<NetworkObject>();
                }

                if (parent == null)
                {
                    writer.WriteBool(false);
                }
                else
                {
                    writer.WriteBool(true);
                    writer.WriteUInt64Packed(parent.NetworkObjectId);
                }

                if (!NetworkManager.Singleton.NetworkConfig.EnableSceneManagement || NetworkManager.Singleton.NetworkConfig.UsePrefabSync)
                {
                    writer.WriteUInt64Packed(netObject.PrefabHash);
                }
                else
                {
                    writer.WriteBool(netObject.IsSceneObject == null ? true : netObject.IsSceneObject.Value);

                    if (netObject.IsSceneObject == null || netObject.IsSceneObject.Value)
                    {
                        writer.WriteUInt64Packed(netObject.NetworkInstanceId);
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

                if (NetworkManager.Singleton.NetworkConfig.EnableNetworkVariable)
                {
                    netObject.WriteNetworkVariableData(buffer, clientId);
                }

                if (payload != null) buffer.CopyFrom(payload);
            }
        }

        internal static void DespawnObject(NetworkObject netObject, bool destroyObject = false)
        {
            if (!netObject.IsSpawned)
            {
                throw new SpawnStateException("Object is not spawned");
            }

            if (!NetworkManager.Singleton.IsServer)
            {
                throw new NotServerException("Only server can despawn objects");
            }

            OnDestroyObject(netObject.NetworkObjectId, destroyObject);
        }

        // Makes scene objects ready to be reused
        internal static void ServerResetShudownStateForSceneObjects()
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

        internal static void ServerDestroySpawnedSceneObjects()
        {
            foreach (var sobj in SpawnedObjectsList)
            {
                if ((sobj.IsSceneObject != null && sobj.IsSceneObject == true) || sobj.DestroyWithScene)
                {
                    if (customDestroyHandlers.ContainsKey(sobj.PrefabHash))
                    {
                        customDestroyHandlers[sobj.PrefabHash](sobj);
                        OnDestroyObject(sobj.NetworkObjectId, false);
                    }
                    else
                    {
                        MonoBehaviour.Destroy(sobj.gameObject);
                    }
                }
            }
        }

        internal static void DestroyNonSceneObjects()
        {
            NetworkObject[] netObjects = MonoBehaviour.FindObjectsOfType<NetworkObject>();

            for (int i = 0; i < netObjects.Length; i++)
            {
                if (netObjects[i].IsSceneObject != null && netObjects[i].IsSceneObject.Value == false)
                {
                    if (customDestroyHandlers.ContainsKey(netObjects[i].PrefabHash))
                    {
                        customDestroyHandlers[netObjects[i].PrefabHash](netObjects[i]);
                        OnDestroyObject(netObjects[i].NetworkObjectId, false);
                    }
                    else
                    {
                        MonoBehaviour.Destroy(netObjects[i].gameObject);
                    }
                }
            }
        }

        internal static void DestroySceneObjects()
        {
            NetworkObject[] netObjects = MonoBehaviour.FindObjectsOfType<NetworkObject>();

            for (int i = 0; i < netObjects.Length; i++)
            {
                if (netObjects[i].IsSceneObject == null || netObjects[i].IsSceneObject.Value == true)
                {
                    if (customDestroyHandlers.ContainsKey(netObjects[i].PrefabHash))
                    {
                        customDestroyHandlers[netObjects[i].PrefabHash](netObjects[i]);
                        OnDestroyObject(netObjects[i].NetworkObjectId, false);
                    }
                    else
                    {
                        MonoBehaviour.Destroy(netObjects[i].gameObject);
                    }
                }
            }
        }

        internal static void CleanDiffedSceneObjects()
        {
            // Clean up the diffed scene objects. I.E scene objects that have been destroyed
            if (pendingSoftSyncObjects.Count > 0)
            {
                List<NetworkObject> objectsToDestroy = new List<NetworkObject>();

                foreach (KeyValuePair<ulong, NetworkObject> pair in pendingSoftSyncObjects)
                {
                    objectsToDestroy.Add(pair.Value);
                }

                for (int i = 0; i < objectsToDestroy.Count; i++)
                {
                    MonoBehaviour.Destroy(objectsToDestroy[i].gameObject);
                }
            }
        }

        internal static void ServerSpawnSceneObjectsOnStartSweep()
        {
            var networkObjects = MonoBehaviour.FindObjectsOfType<NetworkObject>();
            for (int i = 0; i < networkObjects.Length; i++)
            {
                if (networkObjects[i].IsSceneObject == null)
                {
                    SpawnNetworkObjectLocally(networkObjects[i], GetNetworkObjectId(), true, false, null, null, false, 0, false, true);
                }
            }
        }

        internal static void ClientCollectSoftSyncSceneObjectSweep(NetworkObject[] networkObjects)
        {
            if (networkObjects == null)
            {
                networkObjects = MonoBehaviour.FindObjectsOfType<NetworkObject>();
            }

            for (int i = 0; i < networkObjects.Length; i++)
            {
                if (networkObjects[i].IsSceneObject == null)
                {
                    pendingSoftSyncObjects.Add(networkObjects[i].NetworkInstanceId, networkObjects[i]);
                }
            }
        }

        internal static void OnDestroyObject(ulong networkId, bool destroyGameObject)
        {
            if (ReferenceEquals(NetworkManager.Singleton, null)) return;

            //Removal of spawned object
            if (!SpawnedObjects.ContainsKey(networkId))
            {
                Debug.LogWarning($"Trying to destroy object {networkId} but it doesn't seem to exist anymore!");
                return;
            }

            var sobj = SpawnedObjects[networkId];
            if (!sobj.IsOwnedByServer && !sobj.IsPlayerObject && NetworkManager.Singleton.ConnectedClients.ContainsKey(sobj.OwnerClientId))
            {
                //Someone owns it.
                for (int i = NetworkManager.Singleton.ConnectedClients[sobj.OwnerClientId].OwnedObjects.Count - 1; i > -1; i--)
                {
                    if (NetworkManager.Singleton.ConnectedClients[sobj.OwnerClientId].OwnedObjects[i].NetworkObjectId == networkId)
                        NetworkManager.Singleton.ConnectedClients[sobj.OwnerClientId].OwnedObjects.RemoveAt(i);
                }
            }

            sobj.IsSpawned = false;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                if (NetworkManager.Singleton.NetworkConfig.RecycleNetworkIds)
                {
                    releasedNetworkObjectIds.Enqueue(new ReleasedNetworkId()
                    {
                        NetworkId = networkId,
                        ReleaseTime = Time.unscaledTime
                    });
                }

                var rpcQueueContainer = NetworkManager.Singleton.rpcQueueContainer;
                if (rpcQueueContainer != null)
                {
                    if (sobj != null)
                    {
                        // As long as we have any remaining clients, then notify of the object being destroy.
                        if (NetworkManager.Singleton.ConnectedClientsList.Count > 0)
                        {
                            var stream = PooledNetworkBuffer.Get();
                            using (var writer = PooledNetworkWriter.Get(stream))
                            {
                                writer.WriteUInt64Packed(networkId);

                                var queueItem = new RpcFrameQueueItem
                                {
                                    updateStage = NetworkUpdateStage.PostLateUpdate,
                                    queueCreationTime = Time.realtimeSinceStartup,
                                    queueItemType = RpcQueueContainer.QueueItemType.DestroyObject,
                                    networkId = networkId,
                                    itemBuffer = stream,
                                    networkChannel = NetworkChannel.Internal,
                                    clientIds = NetworkManager.Singleton.ConnectedClientsList.Select(c => c.ClientId).ToArray()
                                };
                                rpcQueueContainer.AddToInternalMLAPISendQueue(queueItem);
                            }
                        }
                    }
                }
            }

            var gobj = sobj.gameObject;
            if (destroyGameObject && gobj != null)
            {
                if (customDestroyHandlers.ContainsKey(sobj.PrefabHash))
                {
                    customDestroyHandlers[sobj.PrefabHash](sobj);
                    OnDestroyObject(networkId, false);
                }
                else
                {
                    MonoBehaviour.Destroy(gobj);
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
