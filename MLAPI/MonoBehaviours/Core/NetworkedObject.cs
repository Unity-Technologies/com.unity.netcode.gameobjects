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
        /// <summary>
        /// Gets the unique ID of this object that is synced across the network
        /// </summary>
        [HideInInspector]
        public uint NetworkId
        {
            get
            {
                return networkId;
            }
        }
        internal uint networkId;
        /// <summary>
        /// Gets the clientId of the owner of this NetworkedObject
        /// </summary>
        [HideInInspector]
        public int OwnerClientId
        {
            get
            {
                return ownerClientId;
            }
        }
        internal int ownerClientId = -2;
        /// <summary>
        /// The index of the prefab used to spawn this in the spawnablePrefabs list
        /// </summary>
        [HideInInspector]
        public int SpawnablePrefabIndex
        {
            get
            {
                return spawnablePrefabIndex;
            }
        }
        internal int spawnablePrefabIndex;
        /// <summary>
        /// Gets if this object is a player object
        /// </summary>
        [HideInInspector]
        public bool isPlayerObject
        {
            get
            {
                return _isPlayerObject;
            }
        }
        internal bool _isPlayerObject = false;
        /// <summary>
        /// Gets or sets if this object should be replicated across the network. Can only be changed before the object is spawned
        /// </summary>
        [SerializeField]
        public bool ServerOnly = false;
        /// <summary>
        /// Gets if this object is part of a pool
        /// </summary>
        [HideInInspector]
        public bool isPooledObject
        {
            get
            {
                return _isPooledObject;
            }
        }
        internal bool _isPooledObject = false;
        /// <summary>
        /// Gets the poolId this object is part of
        /// </summary>
        [HideInInspector]
        public ushort PoolId
        {
            get
            {
                return poolId;
            }
        }
        internal ushort poolId;
        /// <summary>
        /// Gets if the object is the the personal clients player object
        /// </summary>
        public bool isLocalPlayer
        {
            get
            {
                return isPlayerObject && (OwnerClientId == NetworkingManager.singleton.MyClientId || (OwnerClientId == -1 && NetworkingManager.singleton.isHost));
            }
        }
        /// <summary>
        /// Gets if the object is owned by the local player
        /// </summary>
        public bool isOwner
        {
            get
            {
                return !isPlayerObject && (OwnerClientId == NetworkingManager.singleton.MyClientId || (OwnerClientId == -1 && NetworkingManager.singleton.isHost));
            }
        }

        /// <summary>
        /// Gets if the object has yet been spawned across the network
        /// </summary>
        public bool isSpawned
        {
            get
            {
                return _isSpawned;
            }
        }
        internal bool _isSpawned = false;
        internal bool sceneObject = false;

        private void OnDestroy()
        {
            if (NetworkingManager.singleton != null)
                SpawnManager.OnDestroyObject(NetworkId, false);
        }

        /// <summary>
        /// Spawns this GameObject across the network. Can only be called from the Server
        /// </summary>
        public void Spawn()
        {
            if (NetworkingManager.singleton != null)
                SpawnManager.OnSpawnObject(this);
        }
        /// <summary>
        /// Spawns an object across the network with a given owner. Can only be called from server
        /// </summary>
        /// <param name="clientId">The clientId to own the object</param>
        public void SpawnWithOwnership(int clientId)
        {
            if (NetworkingManager.singleton != null)
                SpawnManager.OnSpawnObject(this, clientId);
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
        public void ChangeOwnership(int newOwnerClientId)
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

        internal void InvokeBehaviourNetworkSpawn()
        {
            for (int i = 0; i < childNetworkedBehaviours.Count; i++)
            {
                //We check if we are it's networkedObject owner incase a networkedObject exists as a child of our networkedObject.
                if(!childNetworkedBehaviours[i].networkedStartInvoked)
                {
                    childNetworkedBehaviours[i].NetworkStart();
                    childNetworkedBehaviours[i].SyncVarInit();
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

        //Flushes all syncVars to client
        internal void FlushToClient(int clientId)
        {
            for (int i = 0; i < childNetworkedBehaviours.Count; i++)
            {
                childNetworkedBehaviours[i].FlushToClient(clientId);
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
        internal Dictionary<ushort, Dictionary<ushort, Action<int, byte[]>>> targetMessageActions = new Dictionary<ushort, Dictionary<ushort, Action<int, byte[]>>>();
    }
}
