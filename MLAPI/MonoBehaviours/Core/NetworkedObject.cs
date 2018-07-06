using MLAPI.Data;
using MLAPI.NetworkingManagerComponents.Binary;
using MLAPI.NetworkingManagerComponents.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MLAPI.MonoBehaviours.Core
{
    /// <summary>
    /// A component used to identify that a GameObject is networked
    /// </summary>
    [AddComponentMenu("MLAPI/NetworkedObject", -99)]
    public sealed class NetworkedObject : MonoBehaviour
    {
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(NetworkedPrefabName))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The NetworkedObject " + gameObject.name + " does not have a NetworkedPrefabName. It has been set to the gameObject name");
                NetworkedPrefabName = gameObject.name;
            }
        }

        /// <summary>
        /// Gets the unique ID of this object that is synced across the network
        /// </summary>
        public uint NetworkId { get; internal set; }
        /// <summary>
        /// Gets the clientId of the owner of this NetworkedObject
        /// </summary>
        public uint OwnerClientId
        {
            get
            {
                if (_ownerClientId == null)
                    return NetworkingManager.singleton.NetworkConfig.NetworkTransport.InvalidDummyId;
                else
                    return _ownerClientId.Value;
            }
            internal set
            {
                if (value == NetworkingManager.singleton.NetworkConfig.NetworkTransport.InvalidDummyId)
                    _ownerClientId = null;
                else
                    _ownerClientId = value;
            }
        }
        private uint? _ownerClientId = null;
        /// <summary>
        /// The name of the NetworkedPrefab
        /// </summary>
        public string NetworkedPrefabName = string.Empty;
        /// <summary>
        /// Gets if this object is a player object
        /// </summary>
        public bool isPlayerObject { get; internal set; }
        /// <summary>
        /// Gets if this object is part of a pool
        /// </summary>
        public bool isPooledObject { get; internal set; }
        /// <summary>
        /// Gets the poolId this object is part of
        /// </summary>
        public ushort PoolId { get; internal set; }
        /// <summary>
        /// Gets if the object is the the personal clients player object
        /// </summary>
        public bool isLocalPlayer => isPlayerObject && (OwnerClientId == NetworkingManager.singleton.LocalClientId || (OwnerClientId == NetworkingManager.singleton.NetworkConfig.NetworkTransport.HostDummyId && NetworkingManager.singleton.isHost));
        /// <summary>
        /// Gets if the object is owned by the local player or if the object is the local player object
        /// </summary>
        public bool isOwner => isLocalPlayer || isObjectOwner;
        /// <summary>
        /// Gets if the object is owned by the local player and this is not a player object
        /// </summary>
        public bool isObjectOwner => !isPlayerObject && (OwnerClientId == NetworkingManager.singleton.LocalClientId || (OwnerClientId == NetworkingManager.singleton.NetworkConfig.NetworkTransport.HostDummyId && NetworkingManager.singleton.isHost));
        /// <summary>
        /// Gets wheter or not the object is owned by anyone
        /// </summary>
        public bool hasOwner => OwnerClientId != NetworkingManager.singleton.NetworkConfig.NetworkTransport.InvalidDummyId;
        /// <summary>
        /// Gets if the object has yet been spawned across the network
        /// </summary>
        public bool isSpawned { get; internal set; }
        internal bool? sceneObject = null;

        /// <summary>
        /// The current clients observing the object
        /// </summary>
        public readonly HashSet<uint> observers = new HashSet<uint>();
        private readonly HashSet<uint> previousObservers = new HashSet<uint>();

        internal void RebuildObservers(uint? clientId = null)
        {
            bool initial = clientId != null;
            if (initial)
            {
                bool shouldBeAdded = true;
                for (int i = 0; i < childNetworkedBehaviours.Count; i++)
                {
                    bool state = childNetworkedBehaviours[i].OnCheckObserver(clientId.Value);
                    if (state == false)
                    {
                        shouldBeAdded = false;
                        break;
                    }
                }
                if (shouldBeAdded)
                    observers.Add(clientId.Value);
            }
            else
            {
                previousObservers.Clear();
                foreach (var item in observers)
                    previousObservers.Add(item);
                bool update = false;
                for (int i = 0; i < childNetworkedBehaviours.Count; i++)
                {
                    bool changed = childNetworkedBehaviours[i].OnRebuildObservers(observers);
                    if (changed)
                    {
                        update = true;
                        break;
                    }
                }
                if (update)
                {
                    foreach (KeyValuePair<uint, NetworkedClient> pair in NetworkingManager.singleton.ConnectedClients)
                    {
                        if (pair.Key == NetworkingManager.singleton.NetworkConfig.NetworkTransport.HostDummyId)
                            continue;
                        if ((previousObservers.Contains(pair.Key) && !observers.Contains(pair.Key)) ||
                            (!previousObservers.Contains(pair.Key) && observers.Contains(pair.Key)))
                        {
                            //Something changed for this client.
                            using (BitWriter writer = BitWriter.Get())
                            {
                                writer.WriteUInt(NetworkId);
                                writer.WriteBool(observers.Contains(pair.Key));

                                if (observers.Contains(pair.Key))
                                    WriteFormattedSyncedVarData(writer);

                                InternalMessageHandler.Send(pair.Key, "MLAPI_SET_VISIBILITY", "MLAPI_INTERNAL", writer, null);
                            }
                        }
                    }
                }
                else
                {
                    foreach (var item in previousObservers)
                        observers.Add(item);
                }
                previousObservers.Clear();
            }
        }

        internal void SetLocalVisibility(bool visibility)
        {
            for (int i = 0; i < childNetworkedBehaviours.Count; i++)
            {
                childNetworkedBehaviours[i].OnSetLocalVisibility(visibility);
            }
        }

        private void OnDestroy()
        {
            if (NetworkingManager.singleton != null)
                SpawnManager.OnDestroyObject(NetworkId, false);
        }

        /// <summary>
        /// Spawns this GameObject across the network. Can only be called from the Server
        /// </summary>
        public void Spawn(BitWriter spawnPayload = null)
        {
            SpawnManager.SpawnObject(this, null, spawnPayload);
        }

        /// <summary>
        /// Unspawns this GameObject and destroys it for other clients. This should be used if the object should be kept on the server
        /// </summary>
        public void UnSpawn()
        {
            SpawnManager.UnSpawnObject(this);
        }

        /// <summary>
        /// Spawns an object across the network with a given owner. Can only be called from server
        /// </summary>
        /// <param name="clientId">The clientId to own the object</param>
        /// <param name="spawnPayload">The writer containing the spawn payload</param>
        public void SpawnWithOwnership(uint clientId, BitWriter spawnPayload = null)
        {
            SpawnManager.SpawnObject(this, clientId, spawnPayload);
        }

        /// <summary>
        /// Spawns an object across the network and makes it the player object for the given client
        /// </summary>
        /// <param name="clientId">The clientId whos player object this is</param>
        /// <param name="spawnPayload">The writer containing the spawn payload</param>
        public void SpawnAsPlayerObject(uint clientId, BitWriter spawnPayload = null)
        {
            SpawnManager.SpawnPlayerObject(this, clientId, spawnPayload);
        }

        /// <summary>
        /// Removes all ownership of an object from any client. Can only be called from server
        /// </summary>
        public void RemoveOwnership()
        {
            SpawnManager.RemoveOwnership(NetworkId);
        }
        /// <summary>
        /// Changes the owner of the object. Can only be called from server
        /// </summary>
        /// <param name="newOwnerClientId">The new owner clientId</param>
        public void ChangeOwnership(uint newOwnerClientId)
        {
            SpawnManager.ChangeOwnership(NetworkId, newOwnerClientId);
        }

        internal void InvokeBehaviourOnLostOwnership()
        {
            for (int i = 0; i < childNetworkedBehaviours.Count; i++)
            {
                childNetworkedBehaviours[i].OnLostOwnership();
            }
        }

        internal void InvokeBehaviourOnGainedOwnership()
        {
            for (int i = 0; i < childNetworkedBehaviours.Count; i++)
            {
                childNetworkedBehaviours[i].OnGainedOwnership();
            }
        }

        internal void InvokeBehaviourNetworkSpawn(BitReader reader)
        {
            for (int i = 0; i < childNetworkedBehaviours.Count; i++)
            {
                //We check if we are it's networkedObject owner incase a networkedObject exists as a child of our networkedObject.
                if(!childNetworkedBehaviours[i].networkedStartInvoked)
                {
                    childNetworkedBehaviours[i].InternalNetworkStart();
                    childNetworkedBehaviours[i].NetworkStart(reader);
                    childNetworkedBehaviours[i].SyncVarInit();
                    childNetworkedBehaviours[i].networkedStartInvoked = true;
                }
            }
        }

        private List<NetworkedBehaviour> _childNetworkedBehaviours;
        internal List<NetworkedBehaviour> childNetworkedBehaviours
        {
            get
            {
                if(_childNetworkedBehaviours == null)
                {
                    _childNetworkedBehaviours = new List<NetworkedBehaviour>();
                    NetworkedBehaviour[] behaviours = GetComponentsInChildren<NetworkedBehaviour>();
                    for (int i = 0; i < behaviours.Length; i++)
                    {
                        if (behaviours[i].networkedObject == this)
                            _childNetworkedBehaviours.Add(behaviours[i]);
                    }
                }
                return _childNetworkedBehaviours;
            }
        }

        internal static List<NetworkedBehaviour> NetworkedBehaviours = new List<NetworkedBehaviour>();
        internal static void InvokeSyncvarUpdate()
        {
            for (int i = 0; i < NetworkedBehaviours.Count; i++)
            {
                NetworkedBehaviours[i].SyncVarUpdate();
            }
        }


        //Writes SyncedVar data in a formatted way so that the SetFormattedSyncedVarData method can read it.
        //The format doesn't NECCECARLY correspond with the "general syncedVar message layout" 
        //as this should only be used for reading SyncedVar data that is to be read by the SetFormattedData method
        //*
        //The data contains every syncedvar on every behaviour that belongs to this object
        internal void WriteFormattedSyncedVarData(BitWriter writer)
        {
            for (int i = 0; i < childNetworkedBehaviours.Count; i++)
            {
                childNetworkedBehaviours[i].SyncVarInit();
                if (childNetworkedBehaviours[i].syncedVarFields.Count == 0)
                    continue;
                writer.WriteUShort(GetOrderIndex(childNetworkedBehaviours[i])); //Write the behaviourId
                for (int j = 0; j < childNetworkedBehaviours[i].syncedVarFields.Count; j++)
                    FieldTypeHelper.WriteFieldType(writer, childNetworkedBehaviours[i].syncedVarFields[j].FieldValue);
            }
        }

        //Reads formatted data that the "WriteFormattedSyncedVarData" has written and applies the values to SyncedVar fields
        internal void SetFormattedSyncedVarData(BitReader reader)
        {
            for (int i = 0; i < childNetworkedBehaviours.Count; i++)
            {
                childNetworkedBehaviours[i].SyncVarInit();
                if (childNetworkedBehaviours[i].syncedVarFields.Count == 0)
                    continue;
                NetworkedBehaviour behaviour = GetBehaviourAtOrderIndex(reader.ReadUShort());
                for (int j = 0; j < childNetworkedBehaviours[i].syncedVarFields.Count; j++)
                {
                    childNetworkedBehaviours[i].syncedVarFields[j].FieldInfo.SetValue(behaviour, 
                        FieldTypeHelper.ReadFieldType(reader, childNetworkedBehaviours[i].syncedVarFields[j].FieldInfo.FieldType));
                }
                behaviour.OnSyncVarUpdate();
            }
        }

        //Forces a SycnedVar update to a specific client.
        internal void FlushSyncedVarsToClient(uint clientId)
        {
            for (int i = 0; i < childNetworkedBehaviours.Count; i++)
            {
                childNetworkedBehaviours[i].FlushSyncedVarsToClient(clientId);
            }
        }

        internal ushort GetOrderIndex(NetworkedBehaviour instance)
        {
            for (ushort i = 0; i < childNetworkedBehaviours.Count; i++)
            {
                if (childNetworkedBehaviours[i] == instance)
                    return i;
            }
            return 0;
        }

        internal NetworkedBehaviour GetBehaviourAtOrderIndex(ushort index)
        {
            //TODO index out of bounds
            return childNetworkedBehaviours[index];
        }

        //Key: behaviourOrderId, value key: messageType, value value callback 
        internal Dictionary<ushort, Dictionary<ushort, Action<uint, BitReader>>> targetMessageActions = new Dictionary<ushort, Dictionary<ushort, Action<uint, BitReader>>>();
    }
}
