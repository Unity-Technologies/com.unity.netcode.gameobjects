using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using MLAPI.Configuration;
using MLAPI.Exceptions;
using MLAPI.Hashing;
using MLAPI.Logging;
using MLAPI.Messaging;
using MLAPI.Serialization.Pooled;
using MLAPI.Spawning;
using MLAPI.Transports;
using UnityEngine;

namespace MLAPI
{
    /// <summary>
    /// A component used to identify that a GameObject in the network
    /// </summary>
    [AddComponentMenu("MLAPI/NetworkObject", -99)]
    [DisallowMultipleComponent]
    public sealed class NetworkObject : MonoBehaviour
    {
        private void OnValidate()
        {
            // Set this so the hash can be serialized on Scene objects. For prefabs, they are generated at runtime.
            ValidateHash();
        }

        internal void ValidateHash()
        {
            if (string.IsNullOrEmpty(PrefabHashGenerator))
            {
                PrefabHashGenerator = gameObject.name;
            }

            PrefabHash = PrefabHashGenerator.GetStableHash64();
        }

        /// <summary>
        /// Gets the NetworkManager that owns this NetworkObject instance
        /// </summary>
        public NetworkManager NetworkManager => NetworkManager.Singleton;

        /// <summary>
        /// Gets the unique Id of this object that is synced across the network
        /// </summary>
        public ulong NetworkObjectId { get; internal set; }

        /// <summary>
        /// Gets the ClientId of the owner of this NetworkObject
        /// </summary>
        public ulong OwnerClientId
        {
            get
            {
                if (OwnerClientIdInternal == null)
                {
                    return !ReferenceEquals(NetworkManager.Singleton, null) ? NetworkManager.Singleton.ServerClientId : 0;
                }
                else
                {
                    return OwnerClientIdInternal.Value;
                }
            }
            internal set
            {
                if (!ReferenceEquals(NetworkManager.Singleton, null) && value == NetworkManager.Singleton.ServerClientId)
                {
                    OwnerClientIdInternal = null;
                }
                else
                {
                    OwnerClientIdInternal = value;
                }
            }
        }

        internal ulong? OwnerClientIdInternal = null;

        /// <summary>
        /// InstanceId is the id that is unique to the object and scene for a scene object when UsePrefabSync is false.
        /// If UsePrefabSync is true or if it's used on non scene objects, this has no effect.
        /// Should not be set manually
        /// </summary>
        [HideInInspector]
        [SerializeField]
        public ulong NetworkInstanceId;

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
        /// If true, the object will always be replicated as root on clients and the parent will be ignored.
        /// </summary>
        public bool AlwaysReplicateAsRoot;

        /// <summary>
        /// Gets if this object is a player object
        /// </summary>
        public bool IsPlayerObject { get; internal set; }

        /// <summary>
        /// Gets if the object is the the personal clients player object
        /// </summary>
        public bool IsLocalPlayer => !ReferenceEquals(NetworkManager.Singleton, null) && IsPlayerObject && OwnerClientId == NetworkManager.Singleton.LocalClientId;

        /// <summary>
        /// Gets if the object is owned by the local player or if the object is the local player object
        /// </summary>
        public bool IsOwner => !ReferenceEquals(NetworkManager.Singleton, null) && OwnerClientId == NetworkManager.Singleton.LocalClientId;

        /// <summary>
        /// Gets Whether or not the object is owned by anyone
        /// </summary>
        public bool IsOwnedByServer => !ReferenceEquals(NetworkManager.Singleton, null) && OwnerClientId == NetworkManager.Singleton.ServerClientId;

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
        /// Delegate type for checking spawn options
        /// </summary>
        /// <param name="clientId">The clientId to check spawn options for</param>
        public delegate bool SpawnDelegate(ulong clientId);

        /// <summary>
        /// Delegate invoked when the MLAPI needs to know if it should include the transform when spawning the object, if null it will assume true
        /// </summary>
        public SpawnDelegate IncludeTransformWhenSpawning = null;

        /// <summary>
        /// Whether or not to destroy this object if it's owner is destroyed.
        /// If false, the objects ownership will be given to the server.
        /// </summary>
        public bool DontDestroyWithOwner;

        internal readonly HashSet<ulong> m_Observers = new HashSet<ulong>();

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

            return m_Observers.GetEnumerator();
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

            return m_Observers.Contains(clientId);
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

            if (!NetworkManager.Singleton.IsServer)
            {
                throw new NotServerException("Only server can change visibility");
            }

            if (m_Observers.Contains(clientId))
            {
                throw new VisibilityChangeException("The object is already visible");
            }

            // Send spawn call
            m_Observers.Add(clientId);

            NetworkSpawnManager.SendSpawnCallForObject(clientId, this, payload);
        }

        /// <summary>
        /// Shows a list of previously hidden objects to a client
        /// </summary>
        /// <param name="networkObjects">The objects to show</param>
        /// <param name="clientId">The client to show the objects to</param>
        /// <param name="payload">An optional payload to send as part of the spawns</param>
        public static void NetworkShow(List<NetworkObject> networkObjects, ulong clientId, Stream payload = null)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                throw new NotServerException("Only server can change visibility");
            }

            // Do the safety loop first to prevent putting the MLAPI in an invalid state.
            for (int i = 0; i < networkObjects.Count; i++)
            {
                if (!networkObjects[i].IsSpawned)
                {
                    throw new SpawnStateException("Object is not spawned");
                }

                if (networkObjects[i].m_Observers.Contains(clientId))
                {
                    throw new VisibilityChangeException($"{nameof(NetworkObject)} with NetworkId: {networkObjects[i].NetworkObjectId} is already visible");
                }
            }

            using (var buffer = PooledNetworkBuffer.Get())
            using (var writer = PooledNetworkWriter.Get(buffer))
            {
                writer.WriteUInt16Packed((ushort)networkObjects.Count);

                for (int i = 0; i < networkObjects.Count; i++)
                {
                    // Send spawn call
                    networkObjects[i].m_Observers.Add(clientId);

                    NetworkSpawnManager.WriteSpawnCallForObject(buffer, clientId, networkObjects[i], payload);
                }

                InternalMessageSender.Send(clientId, NetworkConstants.ADD_OBJECTS, NetworkChannel.Internal, buffer);
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

            if (!NetworkManager.Singleton.IsServer)
            {
                throw new NotServerException("Only server can change visibility");
            }

            if (!m_Observers.Contains(clientId))
            {
                throw new VisibilityChangeException("The object is already hidden");
            }

            if (clientId == NetworkManager.Singleton.ServerClientId)
            {
                throw new VisibilityChangeException("Cannot hide an object from the server");
            }


            // Send destroy call
            m_Observers.Remove(clientId);

            using (var buffer = PooledNetworkBuffer.Get())
            using (var writer = PooledNetworkWriter.Get(buffer))
            {
                writer.WriteUInt64Packed(NetworkObjectId);

                InternalMessageSender.Send(clientId, NetworkConstants.DESTROY_OBJECT, NetworkChannel.Internal, buffer);
            }
        }

        /// <summary>
        /// Hides a list of objects from a client
        /// </summary>
        /// <param name="networkObjects">The objects to hide</param>
        /// <param name="clientId">The client to hide the objects from</param>
        public static void NetworkHide(List<NetworkObject> networkObjects, ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                throw new NotServerException("Only server can change visibility");
            }

            if (clientId == NetworkManager.Singleton.ServerClientId)
            {
                throw new VisibilityChangeException("Cannot hide an object from the server");
            }

            // Do the safety loop first to prevent putting the MLAPI in an invalid state.
            for (int i = 0; i < networkObjects.Count; i++)
            {
                if (!networkObjects[i].IsSpawned)
                {
                    throw new SpawnStateException("Object is not spawned");
                }

                if (!networkObjects[i].m_Observers.Contains(clientId))
                {
                    throw new VisibilityChangeException($"{nameof(NetworkObject)} with {nameof(NetworkObjectId)}: {networkObjects[i].NetworkObjectId} is already hidden");
                }
            }

            using (var buffer = PooledNetworkBuffer.Get())
            using (var writer = PooledNetworkWriter.Get(buffer))
            {
                writer.WriteUInt16Packed((ushort)networkObjects.Count);

                for (int i = 0; i < networkObjects.Count; i++)
                {
                    // Send destroy call
                    networkObjects[i].m_Observers.Remove(clientId);

                    writer.WriteUInt64Packed(networkObjects[i].NetworkObjectId);
                }

                InternalMessageSender.Send(clientId, NetworkConstants.DESTROY_OBJECTS, NetworkChannel.Internal, buffer);
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkSpawnManager.OnDestroyObject(NetworkObjectId, false);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SpawnInternal(Stream spawnPayload, bool destroyWithScene , ulong? ownerClientId, bool playerObject)
        {
            if (!NetworkManager.Singleton.IsListening)
            {
                throw new NotListeningException($"{nameof(NetworkManager)} isn't listening, start a server or host before spawning objects.");
            }

            if (!NetworkManager.Singleton.IsServer)
            {
                throw new NotServerException($"Only server can spawn {nameof(NetworkObject)}s");
            }

            if (spawnPayload != null) spawnPayload.Position = 0;

            NetworkSpawnManager.SpawnNetworkObjectLocally(this, NetworkSpawnManager.GetNetworkObjectId(), false, playerObject, ownerClientId, spawnPayload, spawnPayload != null, spawnPayload == null ? 0 : (int)spawnPayload.Length, false, destroyWithScene);

            for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
            {
                if (m_Observers.Contains(NetworkManager.Singleton.ConnectedClientsList[i].ClientId))
                {
                    NetworkSpawnManager.SendSpawnCallForObject(NetworkManager.Singleton.ConnectedClientsList[i].ClientId, this, spawnPayload);
                }
            }
        }

        /// <summary>
        /// Spawns this GameObject across the network. Can only be called from the Server
        /// </summary>
        /// <param name="spawnPayload">The writer containing the spawn payload</param>
        /// <param name="destroyWithScene">Should the object be destroyd when the scene is changed</param>
        public void Spawn(Stream spawnPayload = null, bool destroyWithScene = false)
        {
            SpawnInternal(spawnPayload, destroyWithScene, null, false);
        }

        /// <summary>
        /// Spawns an object across the network with a given owner. Can only be called from server
        /// </summary>
        /// <param name="clientId">The clientId to own the object</param>
        /// <param name="spawnPayload">The writer containing the spawn payload</param>
        /// <param name="destroyWithScene">Should the object be destroyd when the scene is changed</param>
        public void SpawnWithOwnership(ulong clientId, Stream spawnPayload = null, bool destroyWithScene = false)
        {
            SpawnInternal(spawnPayload, destroyWithScene, clientId, false);
        }

        /// <summary>
        /// Spawns an object across the network and makes it the player object for the given client
        /// </summary>
        /// <param name="clientId">The clientId whos player object this is</param>
        /// <param name="spawnPayload">The writer containing the spawn payload</param>
        /// <param name="destroyWithScene">Should the object be destroyd when the scene is changed</param>
        public void SpawnAsPlayerObject(ulong clientId, Stream spawnPayload = null, bool destroyWithScene = false)
        {
            SpawnInternal(spawnPayload, destroyWithScene, clientId, true);
        }

        /// <summary>
        /// Despawns this GameObject and destroys it for other clients. This should be used if the object should be kept on the server
        /// </summary>
        public void Despawn(bool destroy = false)
        {
            NetworkSpawnManager.DespawnObject(this, destroy);
        }


        /// <summary>
        /// Removes all ownership of an object from any client. Can only be called from server
        /// </summary>
        public void RemoveOwnership()
        {
            NetworkSpawnManager.RemoveOwnership(this);
        }

        /// <summary>
        /// Changes the owner of the object. Can only be called from server
        /// </summary>
        /// <param name="newOwnerClientId">The new owner clientId</param>
        public void ChangeOwnership(ulong newOwnerClientId)
        {
            NetworkSpawnManager.ChangeOwnership(this, newOwnerClientId);
        }

        internal void InvokeBehaviourOnLostOwnership()
        {
            for (int i = 0; i < ChildNetworkBehaviours.Count; i++)
            {
                ChildNetworkBehaviours[i].OnLostOwnership();
            }
        }

        internal void InvokeBehaviourOnGainedOwnership()
        {
            for (int i = 0; i < ChildNetworkBehaviours.Count; i++)
            {
                ChildNetworkBehaviours[i].OnGainedOwnership();
            }
        }

        internal void ResetNetworkStartInvoked()
        {
            if (ChildNetworkBehaviours != null)
            {
                for (int i = 0; i < ChildNetworkBehaviours.Count; i++)
                {
                    ChildNetworkBehaviours[i].NetworkStartInvoked = false;
                }
            }
        }

        internal void InvokeBehaviourNetworkSpawn(Stream stream)
        {
            for (int i = 0; i < ChildNetworkBehaviours.Count; i++)
            {
                //We check if we are it's NetworkObject owner incase a NetworkObject exists as a child of our NetworkObject
                if (!ChildNetworkBehaviours[i].NetworkStartInvoked)
                {
                    if (!ChildNetworkBehaviours[i].InternalNetworkStartInvoked)
                    {
                        ChildNetworkBehaviours[i].InternalNetworkStart();
                        ChildNetworkBehaviours[i].InternalNetworkStartInvoked = true;
                    }

                    ChildNetworkBehaviours[i].NetworkStart(stream);
                    ChildNetworkBehaviours[i].NetworkStartInvoked = true;
                }
            }
        }

        private List<NetworkBehaviour> m_ChildNetworkBehaviours;

        internal List<NetworkBehaviour> ChildNetworkBehaviours
        {
            get
            {
                if (m_ChildNetworkBehaviours != null)
                {
                    return m_ChildNetworkBehaviours;
                }

                m_ChildNetworkBehaviours = new List<NetworkBehaviour>();
                var networkBehaviours = GetComponentsInChildren<NetworkBehaviour>(true);
                for (int i = 0; i < networkBehaviours.Length; i++)
                {
                    if (networkBehaviours[i].NetworkObject == this)
                    {
                        m_ChildNetworkBehaviours.Add(networkBehaviours[i]);
                    }
                }

                return m_ChildNetworkBehaviours;
            }
        }

        internal void WriteNetworkVariableData(Stream stream, ulong clientId)
        {
            for (int i = 0; i < ChildNetworkBehaviours.Count; i++)
            {
                ChildNetworkBehaviours[i].InitializeVariables();
                NetworkBehaviour.WriteNetworkVariableData(ChildNetworkBehaviours[i].NetworkVariableFields, stream, clientId);
            }
        }

        internal void SetNetworkVariableData(Stream stream)
        {
            for (int i = 0; i < ChildNetworkBehaviours.Count; i++)
            {
                ChildNetworkBehaviours[i].InitializeVariables();
                NetworkBehaviour.SetNetworkVariableData(ChildNetworkBehaviours[i].NetworkVariableFields, stream);
            }
        }

        internal ushort GetNetworkBehaviourOrderIndex(NetworkBehaviour instance)
        {
            for (ushort i = 0; i < ChildNetworkBehaviours.Count; i++)
            {
                if (ChildNetworkBehaviours[i] == instance)
                {
                    return i;
                }
            }

            return 0;
        }

        internal NetworkBehaviour GetNetworkBehaviourAtOrderIndex(ushort index)
        {
            if (index >= ChildNetworkBehaviours.Count)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                {
                    NetworkLog.LogError($"Behaviour index was out of bounds. Did you mess up the order of your {nameof(NetworkBehaviour)}s?");
                }

                return null;
            }

            return ChildNetworkBehaviours[index];
        }
    }
}
