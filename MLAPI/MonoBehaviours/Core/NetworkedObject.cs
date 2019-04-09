using System;
using System.Collections.Generic;
using System.IO;
using MLAPI.Components;
using MLAPI.Data;
using MLAPI.Exceptions;
using MLAPI.Internal;
using MLAPI.Logging;
using MLAPI.Serialization;
using UnityEngine;
using BitStream = MLAPI.Serialization.BitStream;

namespace MLAPI
{
    /// <summary>
    /// A component used to identify that a GameObject is networked
    /// </summary>
    [AddComponentMenu("MLAPI/NetworkedObject", -99)]
    public sealed class NetworkedObject : MonoBehaviour
    {
        internal static readonly List<NetworkedBehaviour> NetworkedBehaviours = new List<NetworkedBehaviour>();

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(PrefabHashGenerator))
            {
                PrefabHash = 0;
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The NetworkedObject " + gameObject.name + " does not have a PrefabHashGenerator. It has been set to the gameObject name");
                PrefabHashGenerator = gameObject.name;
            }
            
            PrefabHash = PrefabHashGenerator.GetStableHash64();
        }

        /// <summary>
        /// Gets the unique ID of this object that is synced across the network
        /// </summary>
        public ulong NetworkId { get; internal set; }
        /// <summary>
        /// Gets the clientId of the owner of this NetworkedObject
        /// </summary>
        public ulong OwnerClientId
        {
            get
            {
                if (_ownerClientId == null)
					return NetworkingManager.Singleton != null ? NetworkingManager.Singleton.ServerClientId : 0;
                else
                    return _ownerClientId.Value;
            }
            internal set
            {
				if (NetworkingManager.Singleton != null && value == NetworkingManager.Singleton.ServerClientId)
                    _ownerClientId = null;
                else
                    _ownerClientId = value;
            }
        }
        
        internal ulong? _ownerClientId = null;
        
        /// <summary>
        /// InstanceId is the id that is unique to the object and scene for a scene object when UsePrefabSync is false.
        /// If UsePrefabSync is true or if it's used on non scene objects, this has no effect.
        /// Should not be set manually
        /// </summary>
        [HideInInspector]
        [SerializeField]
        public ulong NetworkedInstanceId;
        /// <summary>
        /// The Prefab unique hash. This should not be set my the user but rather changed by editing the PrefabHashGenerator.
        /// It has to be the same for all instances of a prefab
        /// </summary>
        [HideInInspector]
        [SerializeField]
        public ulong PrefabHash;
        /// <summary>
        /// The generator used to change the PrefabHash. This should be set the same for all instances of a prefab.
        /// It has to be unique in relation to other prefabs
        /// </summary>
        [SerializeField]
        public string PrefabHashGenerator;
        /// <summary>
        /// Gets if this object is a player object
        /// </summary>
        [Obsolete("Use IsPlayerObject instead", false)]
        public bool isPlayerObject => IsPlayerObject;
        /// <summary>
        /// Gets if this object is a player object
        /// </summary>
        public bool IsPlayerObject { get; internal set; }
        /// <summary>
        /// Gets if the object is the the personal clients player object
        /// </summary>
        [Obsolete("Use IsLocalPlayer instead", false)]
		public bool isLocalPlayer => IsLocalPlayer;
        /// <summary>
        /// Gets if the object is the the personal clients player object
        /// </summary>
        public bool IsLocalPlayer => NetworkingManager.Singleton != null && IsPlayerObject && OwnerClientId == NetworkingManager.Singleton.LocalClientId;
        /// <summary>
        /// Gets if the object is owned by the local player or if the object is the local player object
        /// </summary>
        [Obsolete("Use IsOwner instead", false)]
        public bool isOwner => IsOwner;
        /// <summary>
        /// Gets if the object is owned by the local player or if the object is the local player object
        /// </summary>
        public bool IsOwner => NetworkingManager.Singleton != null && OwnerClientId == NetworkingManager.Singleton.LocalClientId;
        /// <summary>
        /// Gets wheter or not the object is owned by anyone
        /// </summary>
        [Obsolete("Use IsOwnedByServer instead", false)]
		public bool isOwnedByServer => IsOwnedByServer;
        /// <summary>
        /// Gets wheter or not the object is owned by anyone
        /// </summary>
        public bool IsOwnedByServer => NetworkingManager.Singleton != null && OwnerClientId == NetworkingManager.Singleton.ServerClientId;
        /// <summary>
        /// Gets if the object has yet been spawned across the network
        /// </summary>
        [Obsolete("Use IsSpawned instead", false)]
        public bool isSpawned => IsSpawned;
        /// <summary>
        /// Gets if the object has yet been spawned across the network
        /// </summary>
        public bool IsSpawned { get; internal set; }
        /// <summary>
        /// Gets if the object is a SceneObject, null if it's not yet spawned but is a scene object.
        /// </summary>
        public bool? IsSceneObject { get; internal set; }
        /// <summary>
        /// Gets whether or not the object should be automatically removed when the scene is unloaded.
        /// </summary>
        public bool DestroyWithScene { get; internal set; }

        /// <summary>
        /// Delegate type for checking visibility
        /// </summary>
        /// <param name="clientId">The clientId to check visibility for</param>
        public delegate bool VisibilityDelegate(ulong clientId);

        /// <summary>
        /// Delegate invoked when the MLAPI needs to know if the object should be visible to a client, if null it will assume true
        /// </summary>
        public VisibilityDelegate CheckObjectVisibility = null;
        
        /// <summary>
        /// Whether or not to destroy this object if it's owner is destroyed.
        /// If false, the objects ownership will be given to the server.
        /// </summary>
        public bool DontDestroyWithOwner;

        internal readonly HashSet<ulong> observers = new HashSet<ulong>();

        /// <summary>
        /// Returns Observers enumerator
        /// </summary>
        /// <returns>Observers enumerator</returns>
        public HashSet<ulong>.Enumerator GetObservers()
        {
            if (!IsSpawned)
            {
                throw new SpawnStateException("Object is not spawned");
            }
            
            return observers.GetEnumerator();
        }

        /// <summary>
        /// Whether or not this object is visible to a specific client
        /// </summary>
        /// <param name="clientId">The clientId of the client</param>
        /// <returns>True if the client knows about the object</returns>
        public bool IsNetworkVisibleTo(ulong clientId)
        {
            if (!IsSpawned)
            {
                throw new SpawnStateException("Object is not spawned");
            }
            
            return observers.Contains(clientId);
        }

        /// <summary>
        /// Shows a previously hidden object to a client
        /// </summary>
        /// <param name="clientId">The client to show the object to</param>
        /// <param name="payload">An optional payload to send as part of the spawn</param>
        public void NetworkShow(ulong clientId, Stream payload = null)
        {
            if (!IsSpawned)
            {
                throw new SpawnStateException("Object is not spawned");
            }
            
            if (!NetworkingManager.Singleton.IsServer)
            {
                throw new NotServerException("Only server can change visibility");
            }
            
            if (!observers.Contains(clientId))
            {
                // Send spawn call
                observers.Add(clientId);
                
                SpawnManager.SendSpawnCallForObject(clientId, this, payload);
            }
        }

        /// <summary>
        /// Hides a object from a specific client
        /// </summary>
        /// <param name="clientId">The client to hide the object for</param>
        public void NetworkHide(ulong clientId)
        {
            if (!IsSpawned)
            {
                throw new SpawnStateException("Object is not spawned");
            }
            
            if (!NetworkingManager.Singleton.IsServer)
            {
                throw new NotServerException("Only server can change visibility");
            }
            
            if (observers.Contains(clientId) && clientId != NetworkingManager.Singleton.ServerClientId)
            {
                // Send destroy call
                observers.Remove(clientId);
                
                using (PooledBitStream stream = PooledBitStream.Get())
                {
                    using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                    {
                        writer.WriteUInt64Packed(NetworkId);

                        InternalMessageHandler.Send(MLAPIConstants.MLAPI_DESTROY_OBJECT, "MLAPI_INTERNAL", stream, SecuritySendFlags.None, null);
                    }
                }
            }
        }
        
        private void OnDestroy()
        {
            if (NetworkingManager.Singleton != null)
                SpawnManager.OnDestroyObject(NetworkId, false);
        }

        /// <summary>
        /// Spawns this GameObject across the network. Can only be called from the Server
        /// </summary>
        /// <param name="spawnPayload">The writer containing the spawn payload</param>
        /// <param name="destroyWithScene">Should the object be destroyd when the scene is changed</param>
        public void Spawn(Stream spawnPayload = null, bool destroyWithScene = false)
        {
            if (spawnPayload != null)
                spawnPayload.Position = 0;
            
            SpawnManager.SpawnNetworkedObjectLocally(this, SpawnManager.GetNetworkObjectId(), false, false, null, spawnPayload, spawnPayload != null, spawnPayload == null ? 0 : (int)spawnPayload.Length, false, destroyWithScene);

            for (int i = 0; i < NetworkingManager.Singleton.ConnectedClientsList.Count; i++)
            {
                if (observers.Contains(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId))
                {
                    SpawnManager.SendSpawnCallForObject(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId, this, spawnPayload);
                }
            }
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
        /// <param name="destroyWithScene">Should the object be destroyd when the scene is changed</param>
        public void SpawnWithOwnership(ulong clientId, Stream spawnPayload = null, bool destroyWithScene = false)
        {
            if (spawnPayload != null)
                spawnPayload.Position = 0;
            
            SpawnManager.SpawnNetworkedObjectLocally(this, SpawnManager.GetNetworkObjectId(), false, false, clientId, spawnPayload, spawnPayload != null, spawnPayload == null ? 0 : (int)spawnPayload.Length, false, destroyWithScene);

            for (int i = 0; i < NetworkingManager.Singleton.ConnectedClientsList.Count; i++)
            {
                if (observers.Contains(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId))
                {
                    SpawnManager.SendSpawnCallForObject(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId, this, spawnPayload);
                }
            }
        }

        /// <summary>
        /// Spawns an object across the network and makes it the player object for the given client
        /// </summary>
        /// <param name="clientId">The clientId whos player object this is</param>
        /// <param name="spawnPayload">The writer containing the spawn payload</param>
        /// <param name="destroyWithScene">Should the object be destroyd when the scene is changed</param>
        public void SpawnAsPlayerObject(ulong clientId, Stream spawnPayload = null, bool destroyWithScene = false)
        {
            if (spawnPayload != null)
                spawnPayload.Position = 0;
            
            SpawnManager.SpawnNetworkedObjectLocally(this, SpawnManager.GetNetworkObjectId(), false, true, clientId, spawnPayload, spawnPayload != null, spawnPayload == null ? 0 : (int)spawnPayload.Length, false, destroyWithScene);
            
            for (int i = 0; i < NetworkingManager.Singleton.ConnectedClientsList.Count; i++)
            {
                if (observers.Contains(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId))
                {
                    SpawnManager.SendSpawnCallForObject(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId, this, spawnPayload);
                }
            }
        }

        /// <summary>
        /// Removes all ownership of an object from any client. Can only be called from server
        /// </summary>
        public void RemoveOwnership()
        {
            SpawnManager.RemoveOwnership(this);
        }
        /// <summary>
        /// Changes the owner of the object. Can only be called from server
        /// </summary>
        /// <param name="newOwnerClientId">The new owner clientId</param>
        public void ChangeOwnership(ulong newOwnerClientId)
        {
            SpawnManager.ChangeOwnership(this, newOwnerClientId);
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

        internal void InvokeBehaviourNetworkSpawn(Stream stream)
        {
            for (int i = 0; i < childNetworkedBehaviours.Count; i++)
            {
                //We check if we are it's networkedObject owner incase a networkedObject exists as a child of our networkedObject.
                if(!childNetworkedBehaviours[i].networkedStartInvoked)
                {
                    childNetworkedBehaviours[i].InternalNetworkStart();
                    childNetworkedBehaviours[i].NetworkStart(stream);
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
                    NetworkedBehaviour[] behaviours = GetComponentsInChildren<NetworkedBehaviour>(true);
                    for (int i = 0; i < behaviours.Length; i++)
                    {
                        if (behaviours[i].NetworkedObject == this)
                            _childNetworkedBehaviours.Add(behaviours[i]);
                    }
                }
                return _childNetworkedBehaviours;
            }
        }

        private static int _lastProcessedBehaviour = 0;
        internal static void NetworkedBehaviourUpdate()
        {
            int amountToProcess = NetworkingManager.Singleton.NetworkConfig.MaxBehaviourUpdatesPerTick <= 0 ? NetworkedBehaviours.Count : Mathf.Max(NetworkingManager.Singleton.NetworkConfig.MaxBehaviourUpdatesPerTick, NetworkedBehaviours.Count);

            for (int i = 0; i < amountToProcess; i++)
            {
                if (_lastProcessedBehaviour >= NetworkedBehaviours.Count)
                    _lastProcessedBehaviour = 0;

                NetworkedBehaviours[_lastProcessedBehaviour].NetworkedVarUpdate();
                _lastProcessedBehaviour++;
            }
        }
        
        internal void WriteNetworkedVarData(Stream stream, ulong clientId)
        {
            for (int i = 0; i < childNetworkedBehaviours.Count; i++)
            {
                childNetworkedBehaviours[i].NetworkedVarInit();
                NetworkedBehaviour.WriteNetworkedVarData(childNetworkedBehaviours[i].networkedVarFields, stream, clientId);
            }
        }

        internal void SetNetworkedVarData(Stream stream)
        {
            for (int i = 0; i < childNetworkedBehaviours.Count; i++)
            {
                childNetworkedBehaviours[i].NetworkedVarInit();
                NetworkedBehaviour.SetNetworkedVarData(childNetworkedBehaviours[i].networkedVarFields, stream);
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
            if (index >= childNetworkedBehaviours.Count)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError("Behaviour index was out of bounds. Did you mess up the order of your NetworkedBehaviours?");
                return null;
            }
            return childNetworkedBehaviours[index];
        }
    }
}
