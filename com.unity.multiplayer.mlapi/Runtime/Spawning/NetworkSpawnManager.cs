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
        internal static readonly Dictionary<ulong, NetworkObject> PendingSoftSyncObjects = new Dictionary<ulong, NetworkObject>();

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

        internal static readonly Dictionary<ulong, SpawnHandlerDelegate> CustomSpawnHandlers = new Dictionary<ulong, SpawnHandlerDelegate>();
        internal static readonly Dictionary<ulong, DestroyHandlerDelegate> CustomDestroyHandlers = new Dictionary<ulong, DestroyHandlerDelegate>();

        /// <summary>
        /// Registers a delegate for spawning NetworkPrefabs, useful for object pooling
        /// </summary>
        /// <param name="prefabHash">The prefab hash to spawn</param>
        /// <param name="handler">The delegate handler</param>
        public static void RegisterSpawnHandler(ulong prefabHash, SpawnHandlerDelegate handler)
        {
            if (CustomSpawnHandlers.ContainsKey(prefabHash))
            {
                CustomSpawnHandlers[prefabHash] = handler;
            }
            else
            {
                CustomSpawnHandlers.Add(prefabHash, handler);
            }
        }

        /// <summary>
        /// Registers a delegate for destroying NetworkObjects, useful for object pooling
        /// </summary>
        /// <param name="prefabHash">The prefab hash to destroy</param>
        /// <param name="handler">The delegate handler</param>
        public static void RegisterDestroyHandler(ulong prefabHash, DestroyHandlerDelegate handler)
        {
            if (CustomDestroyHandlers.ContainsKey(prefabHash))
            {
                CustomDestroyHandlers[prefabHash] = handler;
            }
            else
            {
                CustomDestroyHandlers.Add(prefabHash, handler);
            }
        }

        /// <summary>
        /// Unregisters the custom spawn handler for a specific prefab hash
        /// </summary>
        /// <param name="prefabHash">The prefab hash of the prefab spawn handler that is to be removed</param>
        public static void UnregisterSpawnHandler(ulong prefabHash)
        {
            CustomSpawnHandlers.Remove(prefabHash);
        }

        /// <summary>
        /// Unregisters the custom destroy handler for a specific prefab hash
        /// </summary>
        /// <param name="prefabHash">The prefab hash of the prefab destroy handler that is to be removed</param>
        public static void UnregisterDestroyHandler(ulong prefabHash)
        {
            CustomDestroyHandlers.Remove(prefabHash);
        }

        internal static readonly Queue<ReleasedNetworkId> ReleasedNetworkObjectIds = new Queue<ReleasedNetworkId>();
        private static ulong s_NetworkObjectIdCounter;

        internal static ulong GetNetworkObjectId()
        {
            if (ReleasedNetworkObjectIds.Count > 0 && NetworkManager.Singleton.NetworkConfig.RecycleNetworkIds && (Time.unscaledTime - ReleasedNetworkObjectIds.Peek().ReleaseTime) >= NetworkManager.Singleton.NetworkConfig.NetworkIdRecycleDelay)
            {
                return ReleasedNetworkObjectIds.Dequeue().NetworkId;
            }

            s_NetworkObjectIdCounter++;

            return s_NetworkObjectIdCounter;
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
                {
                    return i;
                }
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

        internal static void RemoveOwnership(NetworkObject networkObject)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                throw new NotServerException("Only the server can change ownership");
            }

            if (!networkObject.IsSpawned)
            {
                throw new SpawnStateException("Object is not spawned");
            }

            for (int i = NetworkManager.Singleton.ConnectedClients[networkObject.OwnerClientId].OwnedObjects.Count - 1; i > -1; i--)
            {
                if (NetworkManager.Singleton.ConnectedClients[networkObject.OwnerClientId].OwnedObjects[i] == networkObject)
                {
                    NetworkManager.Singleton.ConnectedClients[networkObject.OwnerClientId].OwnedObjects.RemoveAt(i);
                }
            }

            networkObject.OwnerClientIdInternal = null;

            using (var buffer = PooledNetworkBuffer.Get())
            using (var writer = PooledNetworkWriter.Get(buffer))
            {
                writer.WriteUInt64Packed(networkObject.NetworkObjectId);
                writer.WriteUInt64Packed(networkObject.OwnerClientId);

                InternalMessageSender.Send(NetworkConstants.CHANGE_OWNER, NetworkChannel.Internal, buffer);
            }
        }

        internal static void ChangeOwnership(NetworkObject networkObject, ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                throw new NotServerException("Only the server can change ownership");
            }

            if (!networkObject.IsSpawned)
            {
                throw new SpawnStateException("Object is not spawned");
            }

            if (NetworkManager.Singleton.ConnectedClients.ContainsKey(networkObject.OwnerClientId))
            {
                for (int i = NetworkManager.Singleton.ConnectedClients[networkObject.OwnerClientId].OwnedObjects.Count - 1; i >= 0; i--)
                {
                    if (NetworkManager.Singleton.ConnectedClients[networkObject.OwnerClientId].OwnedObjects[i] == networkObject)
                    {
                        NetworkManager.Singleton.ConnectedClients[networkObject.OwnerClientId].OwnedObjects.RemoveAt(i);
                    }
                }
            }

            NetworkManager.Singleton.ConnectedClients[clientId].OwnedObjects.Add(networkObject);
            networkObject.OwnerClientId = clientId;

            using (var buffer = PooledNetworkBuffer.Get())
            using (var writer = PooledNetworkWriter.Get(buffer))
            {
                writer.WriteUInt64Packed(networkObject.NetworkObjectId);
                writer.WriteUInt64Packed(clientId);

                InternalMessageSender.Send(NetworkConstants.CHANGE_OWNER, NetworkChannel.Internal, buffer);
            }
        }

        // Only ran on Client
        internal static NetworkObject CreateLocalNetworkObject(bool softCreate, ulong instanceId, ulong prefabHash, ulong? parentNetworkId, Vector3? position, Quaternion? rotation)
        {
            NetworkObject parentNetworkObject = null;

            if (parentNetworkId != null && SpawnedObjects.ContainsKey(parentNetworkId.Value))
            {
                parentNetworkObject = SpawnedObjects[parentNetworkId.Value];
            }
            else if (parentNetworkId != null)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Cannot find parent. Parent objects always have to be spawned and replicated BEFORE the child");
            }

            if (!NetworkManager.Singleton.NetworkConfig.EnableSceneManagement || NetworkManager.Singleton.NetworkConfig.UsePrefabSync || !softCreate)
            {
                // Create the object
                if (CustomSpawnHandlers.ContainsKey(prefabHash))
                {
                    var networkObject = CustomSpawnHandlers[prefabHash](position.GetValueOrDefault(Vector3.zero), rotation.GetValueOrDefault(Quaternion.identity));

                    if (!ReferenceEquals(parentNetworkObject, null))
                    {
                        networkObject.transform.SetParent(parentNetworkObject.transform, true);
                    }

                    if (NetworkSceneManager.IsSpawnedObjectsPendingInDontDestroyOnLoad)
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
                            NetworkLog.LogError($"Failed to create object locally. [{nameof(prefabHash)}={prefabHash}]. Hash could not be found. Is the prefab registered?");
                        }

                        return null;
                    }

                    var prefab = NetworkManager.Singleton.NetworkConfig.NetworkPrefabs[prefabIndex].Prefab;
                    var networkObject = ((position == null && rotation == null) ? MonoBehaviour.Instantiate(prefab) : MonoBehaviour.Instantiate(prefab, position.GetValueOrDefault(Vector3.zero), rotation.GetValueOrDefault(Quaternion.identity))).GetComponent<NetworkObject>();

                    if (!ReferenceEquals(parentNetworkObject, null))
                    {
                        networkObject.transform.SetParent(parentNetworkObject.transform, true);
                    }

                    if (NetworkSceneManager.IsSpawnedObjectsPendingInDontDestroyOnLoad)
                    {
                        GameObject.DontDestroyOnLoad(networkObject.gameObject);
                    }

                    return networkObject;
                }
            }
            else
            {
                // SoftSync them by mapping
                if (!PendingSoftSyncObjects.ContainsKey(instanceId))
                {
                    // TODO: Fix this message
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                    {
                        NetworkLog.LogError("Cannot find pending soft sync object. Is the projects the same?");
                    }
                    return null;
                }

                var networkObject = PendingSoftSyncObjects[instanceId];
                PendingSoftSyncObjects.Remove(instanceId);

                if (!ReferenceEquals(parentNetworkObject, null))
                {
                    networkObject.transform.SetParent(parentNetworkObject.transform, true);
                }

                return networkObject;
            }
        }

        // Ran on both server and client
        internal static void SpawnNetworkObjectLocally(NetworkObject networkObject, ulong networkId, bool sceneObject, bool playerObject, ulong? ownerClientId, Stream dataStream, bool readPayload, int payloadLength, bool readNetworkVariable, bool destroyWithScene)
        {
            if (ReferenceEquals(networkObject, null))
            {
                throw new ArgumentNullException(nameof(networkObject), "Cannot spawn null object");
            }

            if (networkObject.IsSpawned)
            {
                throw new SpawnStateException("Object is already spawned");
            }

            if (readNetworkVariable && NetworkManager.Singleton.NetworkConfig.EnableNetworkVariable)
            {
                networkObject.SetNetworkVariableData(dataStream);
            }

            if (SpawnedObjects.ContainsKey(networkObject.NetworkObjectId)) return;

            networkObject.IsSpawned = true;

            networkObject.IsSceneObject = sceneObject;
            networkObject.NetworkObjectId = networkId;

            networkObject.DestroyWithScene = sceneObject || destroyWithScene;

            networkObject.OwnerClientIdInternal = ownerClientId;
            networkObject.IsPlayerObject = playerObject;

            SpawnedObjects.Add(networkObject.NetworkObjectId, networkObject);
            SpawnedObjectsList.Add(networkObject);

            if (ownerClientId != null)
            {
                if (NetworkManager.Singleton.IsServer)
                {
                    if (playerObject)
                    {
                        NetworkManager.Singleton.ConnectedClients[ownerClientId.Value].PlayerObject = networkObject;
                    }
                    else
                    {
                        NetworkManager.Singleton.ConnectedClients[ownerClientId.Value].OwnedObjects.Add(networkObject);
                    }
                }
                else if (playerObject && ownerClientId.Value == NetworkManager.Singleton.LocalClientId)
                {
                    NetworkManager.Singleton.ConnectedClients[ownerClientId.Value].PlayerObject = networkObject;
                }
            }

            if (NetworkManager.Singleton.IsServer)
            {
                for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
                {
                    if (networkObject.CheckObjectVisibility == null || networkObject.CheckObjectVisibility(NetworkManager.Singleton.ConnectedClientsList[i].ClientId))
                    {
                        networkObject.m_Observers.Add(NetworkManager.Singleton.ConnectedClientsList[i].ClientId);
                    }
                }
            }

            networkObject.ResetNetworkStartInvoked();

            if (readPayload)
            {
                using (var payloadBuffer = PooledNetworkBuffer.Get())
                {
                    payloadBuffer.CopyUnreadFrom(dataStream, payloadLength);
                    dataStream.Position += payloadLength;
                    payloadBuffer.Position = 0;
                    networkObject.InvokeBehaviourNetworkSpawn(payloadBuffer);
                }
            }
            else
            {
                networkObject.InvokeBehaviourNetworkSpawn(null);
            }
        }

        internal static void SendSpawnCallForObject(ulong clientId, NetworkObject networkObject, Stream payload)
        {
            //Currently, if this is called and the clientId (destination) is the server's client Id, this case
            //will be checked within the below Send function.  To avoid unwarranted allocation of a PooledNetworkBuffer
            //placing this check here. [NSS]
            if (NetworkManager.Singleton.IsServer && clientId == NetworkManager.Singleton.ServerClientId)
            {
                return;
            }

            var rpcQueueContainer = NetworkManager.Singleton.RpcQueueContainer;

            var buffer = PooledNetworkBuffer.Get();
            WriteSpawnCallForObject(buffer, clientId, networkObject, payload);

            var queueItem = new RpcFrameQueueItem
            {
                UpdateStage = NetworkUpdateStage.Update,
                QueueItemType = RpcQueueContainer.QueueItemType.CreateObject,
                NetworkId = 0,
                NetworkBuffer = buffer,
                NetworkChannel = NetworkChannel.Internal,
                ClientNetworkIds = new[] { clientId }
            };
            rpcQueueContainer.AddToInternalMLAPISendQueue(queueItem);
        }

        internal static void WriteSpawnCallForObject(Serialization.NetworkBuffer buffer, ulong clientId, NetworkObject networkObject, Stream payload)
        {
            using (var writer = PooledNetworkWriter.Get(buffer))
            {
                writer.WriteBool(networkObject.IsPlayerObject);
                writer.WriteUInt64Packed(networkObject.NetworkObjectId);
                writer.WriteUInt64Packed(networkObject.OwnerClientId);

                NetworkObject parentNetworkObject = null;

                if (!networkObject.AlwaysReplicateAsRoot && !ReferenceEquals(networkObject.transform.parent, null))
                {
                    parentNetworkObject = networkObject.transform.parent.GetComponent<NetworkObject>();
                }

                if (ReferenceEquals(parentNetworkObject, null))
                {
                    writer.WriteBool(false);
                }
                else
                {
                    writer.WriteBool(true);
                    writer.WriteUInt64Packed(parentNetworkObject.NetworkObjectId);
                }

                if (!NetworkManager.Singleton.NetworkConfig.EnableSceneManagement || NetworkManager.Singleton.NetworkConfig.UsePrefabSync)
                {
                    writer.WriteUInt64Packed(networkObject.PrefabHash);
                }
                else
                {
                    writer.WriteBool(networkObject.IsSceneObject ?? true);

                    if (networkObject.IsSceneObject == null || networkObject.IsSceneObject.Value)
                    {
                        writer.WriteUInt64Packed(networkObject.NetworkInstanceId);
                    }
                    else
                    {
                        writer.WriteUInt64Packed(networkObject.PrefabHash);
                    }
                }

                if (networkObject.IncludeTransformWhenSpawning == null || networkObject.IncludeTransformWhenSpawning(clientId))
                {
                    writer.WriteBool(true);
                    writer.WriteSinglePacked(networkObject.transform.position.x);
                    writer.WriteSinglePacked(networkObject.transform.position.y);
                    writer.WriteSinglePacked(networkObject.transform.position.z);

                    writer.WriteSinglePacked(networkObject.transform.rotation.eulerAngles.x);
                    writer.WriteSinglePacked(networkObject.transform.rotation.eulerAngles.y);
                    writer.WriteSinglePacked(networkObject.transform.rotation.eulerAngles.z);
                }
                else
                {
                    writer.WriteBool(false);
                }

                writer.WriteBool(payload != null);

                if (payload != null)
                {
                    writer.WriteInt32Packed((int)payload.Length);
                }

                if (NetworkManager.Singleton.NetworkConfig.EnableNetworkVariable)
                {
                    networkObject.WriteNetworkVariableData(buffer, clientId);
                }

                if (payload != null) buffer.CopyFrom(payload);
            }
        }

        internal static void DespawnObject(NetworkObject networkObject, bool destroyObject = false)
        {
            if (!networkObject.IsSpawned)
            {
                throw new SpawnStateException("Object is not spawned");
            }

            if (!NetworkManager.Singleton.IsServer)
            {
                throw new NotServerException("Only server can despawn objects");
            }

            OnDestroyObject(networkObject.NetworkObjectId, destroyObject);
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
                    if (CustomDestroyHandlers.ContainsKey(sobj.PrefabHash))
                    {
                        CustomDestroyHandlers[sobj.PrefabHash](sobj);
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
            var networkObjects = MonoBehaviour.FindObjectsOfType<NetworkObject>();

            for (int i = 0; i < networkObjects.Length; i++)
            {
                if (networkObjects[i].IsSceneObject != null && networkObjects[i].IsSceneObject.Value == false)
                {
                    if (CustomDestroyHandlers.ContainsKey(networkObjects[i].PrefabHash))
                    {
                        CustomDestroyHandlers[networkObjects[i].PrefabHash](networkObjects[i]);
                        OnDestroyObject(networkObjects[i].NetworkObjectId, false);
                    }
                    else
                    {
                        MonoBehaviour.Destroy(networkObjects[i].gameObject);
                    }
                }
            }
        }

        internal static void DestroySceneObjects()
        {
            var networkObjects = MonoBehaviour.FindObjectsOfType<NetworkObject>();

            for (int i = 0; i < networkObjects.Length; i++)
            {
                if (networkObjects[i].IsSceneObject == null || networkObjects[i].IsSceneObject.Value == true)
                {
                    if (CustomDestroyHandlers.ContainsKey(networkObjects[i].PrefabHash))
                    {
                        CustomDestroyHandlers[networkObjects[i].PrefabHash](networkObjects[i]);
                        OnDestroyObject(networkObjects[i].NetworkObjectId, false);
                    }
                    else
                    {
                        MonoBehaviour.Destroy(networkObjects[i].gameObject);
                    }
                }
            }
        }

        internal static void CleanDiffedSceneObjects()
        {
            // Clean up the diffed scene objects. I.E scene objects that have been destroyed
            if (PendingSoftSyncObjects.Count > 0)
            {
                var networkObjectsToDestroy = new List<NetworkObject>();

                foreach (var pair in PendingSoftSyncObjects)
                {
                    networkObjectsToDestroy.Add(pair.Value);
                }

                for (int i = 0; i < networkObjectsToDestroy.Count; i++)
                {
                    MonoBehaviour.Destroy(networkObjectsToDestroy[i].gameObject);
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
                    PendingSoftSyncObjects.Add(networkObjects[i].NetworkInstanceId, networkObjects[i]);
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
                    {
                        NetworkManager.Singleton.ConnectedClients[sobj.OwnerClientId].OwnedObjects.RemoveAt(i);
                    }
                }
            }

            sobj.IsSpawned = false;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                if (NetworkManager.Singleton.NetworkConfig.RecycleNetworkIds)
                {
                    ReleasedNetworkObjectIds.Enqueue(new ReleasedNetworkId()
                    {
                        NetworkId = networkId,
                        ReleaseTime = Time.unscaledTime
                    });
                }

                var rpcQueueContainer = NetworkManager.Singleton.RpcQueueContainer;
                if (rpcQueueContainer != null)
                {
                    if (!ReferenceEquals(sobj, null))
                    {
                        // As long as we have any remaining clients, then notify of the object being destroy.
                        if (NetworkManager.Singleton.ConnectedClientsList.Count > 0)
                        {
                            var buffer = PooledNetworkBuffer.Get();
                            using (var writer = PooledNetworkWriter.Get(buffer))
                            {
                                writer.WriteUInt64Packed(networkId);

                                var queueItem = new RpcFrameQueueItem
                                {
                                    UpdateStage = NetworkUpdateStage.PostLateUpdate,
                                    QueueItemType = RpcQueueContainer.QueueItemType.DestroyObject,
                                    NetworkId = networkId,
                                    NetworkBuffer = buffer,
                                    NetworkChannel = NetworkChannel.Internal,
                                    ClientNetworkIds = NetworkManager.Singleton.ConnectedClientsList.Select(c => c.ClientId).ToArray()
                                };

                                rpcQueueContainer.AddToInternalMLAPISendQueue(queueItem);
                            }
                        }
                    }
                }
            }

            var gobj = sobj.gameObject;

            if (destroyGameObject && !ReferenceEquals(gobj, null))
            {
                if (CustomDestroyHandlers.ContainsKey(sobj.PrefabHash))
                {
                    CustomDestroyHandlers[sobj.PrefabHash](sobj);
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
