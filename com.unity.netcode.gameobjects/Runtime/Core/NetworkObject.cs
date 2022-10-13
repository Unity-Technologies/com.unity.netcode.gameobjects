using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Netcode
{
    /// <summary>
    /// A component used to identify that a GameObject in the network
    /// </summary>
    [AddComponentMenu("Netcode/Network Object", -99)]
    [DisallowMultipleComponent]
    public sealed class NetworkObject : MonoBehaviour
    {
        [HideInInspector]
        [SerializeField]
        internal uint GlobalObjectIdHash;

#if UNITY_EDITOR
        private void OnValidate()
        {
            GenerateGlobalObjectIdHash();
        }

        internal void GenerateGlobalObjectIdHash()
        {
            // do NOT regenerate GlobalObjectIdHash for NetworkPrefabs while Editor is in PlayMode
            if (UnityEditor.EditorApplication.isPlaying && !string.IsNullOrEmpty(gameObject.scene.name))
            {
                return;
            }

            // do NOT regenerate GlobalObjectIdHash if Editor is transitioning into or out of PlayMode
            if (!UnityEditor.EditorApplication.isPlaying && UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var globalObjectIdString = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(this).ToString();
            GlobalObjectIdHash = XXHash.Hash32(globalObjectIdString);
        }
#endif // UNITY_EDITOR

        /// <summary>
        /// Gets the NetworkManager that owns this NetworkObject instance
        /// </summary>
        public NetworkManager NetworkManager => NetworkManagerOwner ?? NetworkManager.Singleton;

        /// <summary>
        /// The NetworkManager that owns this NetworkObject.
        /// This property controls where this NetworkObject belongs.
        /// This property is null by default currently, which means that the above NetworkManager getter will return the Singleton.
        /// In the future this is the path where alternative NetworkManagers should be injected for running multi NetworkManagers
        /// </summary>
        internal NetworkManager NetworkManagerOwner;

        /// <summary>
        /// Gets the unique Id of this object that is synced across the network
        /// </summary>
        public ulong NetworkObjectId { get; internal set; }

        /// <summary>
        /// Gets the ClientId of the owner of this NetworkObject
        /// </summary>
        public ulong OwnerClientId { get; internal set; }

        /// <summary>
        /// If true, the object will always be replicated as root on clients and the parent will be ignored.
        /// </summary>
        public bool AlwaysReplicateAsRoot;

        /// <summary>
        /// Gets if this object is a player object
        /// </summary>
        public bool IsPlayerObject { get; internal set; }

        /// <summary>
        /// Gets if the object is the personal clients player object
        /// </summary>
        public bool IsLocalPlayer => NetworkManager != null && IsPlayerObject && OwnerClientId == NetworkManager.LocalClientId;

        /// <summary>
        /// Gets if the object is owned by the local player or if the object is the local player object
        /// </summary>
        public bool IsOwner => NetworkManager != null && OwnerClientId == NetworkManager.LocalClientId;

        /// <summary>
        /// Gets Whether or not the object is owned by anyone
        /// </summary>
        public bool IsOwnedByServer => NetworkManager != null && OwnerClientId == NetworkManager.ServerClientId;

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
        public bool DestroyWithScene { get; set; }

        /// <summary>
        /// Delegate type for checking visibility
        /// </summary>
        /// <param name="clientId">The clientId to check visibility for</param>
        public delegate bool VisibilityDelegate(ulong clientId);

        /// <summary>
        /// Delegate invoked when the netcode needs to know if the object should be visible to a client, if null it will assume true
        /// </summary>
        public VisibilityDelegate CheckObjectVisibility = null;

        /// <summary>
        /// Delegate type for checking spawn options
        /// </summary>
        /// <param name="clientId">The clientId to check spawn options for</param>
        public delegate bool SpawnDelegate(ulong clientId);

        /// <summary>
        /// Delegate invoked when the netcode needs to know if it should include the transform when spawning the object, if null it will assume true
        /// </summary>
        public SpawnDelegate IncludeTransformWhenSpawning = null;

        /// <summary>
        /// Whether or not to destroy this object if it's owner is destroyed.
        /// If true, the objects ownership will be given to the server.
        /// </summary>
        public bool DontDestroyWithOwner;

        /// <summary>
        /// Whether or not to enable automatic NetworkObject parent synchronization.
        /// </summary>
        public bool AutoObjectParentSync = true;

        internal readonly HashSet<ulong> Observers = new HashSet<ulong>();

#if MULTIPLAYER_TOOLS
        private string m_CachedNameForMetrics;
#endif
        internal string GetNameForMetrics()
        {
#if MULTIPLAYER_TOOLS
            return m_CachedNameForMetrics ??= name;
#else
            return null;
#endif
        }

        private readonly HashSet<ulong> m_EmptyULongHashSet = new HashSet<ulong>();
        /// <summary>
        /// Returns Observers enumerator
        /// </summary>
        /// <returns>Observers enumerator</returns>
        public HashSet<ulong>.Enumerator GetObservers()
        {
            if (!IsSpawned)
            {
                return m_EmptyULongHashSet.GetEnumerator();
            }

            return Observers.GetEnumerator();
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
                return false;
            }
            return Observers.Contains(clientId);
        }

        /// <summary>
        ///  In the event the scene of origin gets unloaded, we keep
        ///  the most important part to uniquely identify in-scene
        ///  placed NetworkObjects
        /// </summary>
        internal int SceneOriginHandle = 0;

        private Scene m_SceneOrigin;
        /// <summary>
        /// The scene where the NetworkObject was first instantiated
        /// Note: Primarily for in-scene placed NetworkObjects
        /// We need to keep track of the original scene of origin for
        /// the NetworkObject in order to be able to uniquely identify it
        /// using the scene of origin's handle.
        /// </summary>
        internal Scene SceneOrigin
        {
            get
            {
                return m_SceneOrigin;
            }

            set
            {
                // The scene origin should only be set once.
                // Once set, it should never change.
                if (SceneOriginHandle == 0 && value.IsValid() && value.isLoaded)
                {
                    m_SceneOrigin = value;
                    SceneOriginHandle = value.handle;
                }
            }
        }

        /// <summary>
        /// Helper method to return the correct scene handle
        /// Note: Do not use this within NetworkSpawnManager.SpawnNetworkObjectLocallyCommon
        /// </summary>
        internal int GetSceneOriginHandle()
        {
            if (SceneOriginHandle == 0 && IsSpawned && IsSceneObject != false)
            {
                throw new Exception($"{nameof(GetSceneOriginHandle)} called when {nameof(SceneOriginHandle)} is still zero but the {nameof(NetworkObject)} is already spawned!");
            }
            return SceneOriginHandle != 0 ? SceneOriginHandle : gameObject.scene.handle;
        }

        private void Awake()
        {
            SetCachedParent(transform.parent);
            SceneOrigin = gameObject.scene;
        }

        /// <summary>
        /// Makes the previously hidden <see cref="NetworkObject"/> "netcode visible" to the targeted client.
        /// </summary>
        /// <remarks>
        /// Usage: Use to start sending updates for a previously hidden <see cref="NetworkObject"/> to the targeted client.<br />
        /// <br />
        /// Dynamically Spawned: <see cref="NetworkObject"/>s will be instantiated and spawned on the targeted client side.<br />
        /// In-Scene Placed: The instantiated but despawned <see cref="NetworkObject"/>s will be spawned on the targeted client side.<br />
        /// <br />
        /// See Also:<br />
        /// <see cref="NetworkShow(ulong)"/><br />
        /// <see cref="NetworkHide(ulong)"/> or <see cref="NetworkHide(List{NetworkObject}, ulong)"/><br />
        /// </remarks>
        /// <param name="clientId">The targeted client</param>
        public void NetworkShow(ulong clientId)
        {
            if (!IsSpawned)
            {
                throw new SpawnStateException("Object is not spawned");
            }

            if (!NetworkManager.IsServer)
            {
                throw new NotServerException("Only server can change visibility");
            }

            if (Observers.Contains(clientId))
            {
                throw new VisibilityChangeException("The object is already visible");
            }

            Observers.Add(clientId);

            NetworkManager.SpawnManager.SendSpawnCallForObject(clientId, this);
        }


        /// <summary>
        /// Makes a list of previously hidden <see cref="NetworkObject"/>s "netcode visible" for the client specified.
        /// </summary>
        /// <remarks>
        /// Usage: Use to start sending updates for previously hidden <see cref="NetworkObject"/>s to the targeted client.<br />
        /// <br />
        /// Dynamically Spawned: <see cref="NetworkObject"/>s will be instantiated and spawned on the targeted client's side.<br />
        /// In-Scene Placed: Already instantiated but despawned <see cref="NetworkObject"/>s will be spawned on the targeted client's side.<br />
        /// <br />
        /// See Also:<br />
        /// <see cref="NetworkShow(ulong)"/><br />
        /// <see cref="NetworkHide(ulong)"/> or <see cref="NetworkHide(List{NetworkObject}, ulong)"/><br />
        /// </remarks>
        /// <param name="networkObjects">The objects to become "netcode visible" to the targeted client</param>
        /// <param name="clientId">The targeted client</param>
        public static void NetworkShow(List<NetworkObject> networkObjects, ulong clientId)
        {
            if (networkObjects == null || networkObjects.Count == 0)
            {
                throw new ArgumentNullException("At least one " + nameof(NetworkObject) + " has to be provided");
            }

            NetworkManager networkManager = networkObjects[0].NetworkManager;

            if (!networkManager.IsServer)
            {
                throw new NotServerException("Only server can change visibility");
            }

            // Do the safety loop first to prevent putting the netcode in an invalid state.
            for (int i = 0; i < networkObjects.Count; i++)
            {
                if (!networkObjects[i].IsSpawned)
                {
                    throw new SpawnStateException("Object is not spawned");
                }

                if (networkObjects[i].Observers.Contains(clientId))
                {
                    throw new VisibilityChangeException($"{nameof(NetworkObject)} with NetworkId: {networkObjects[i].NetworkObjectId} is already visible");
                }

                if (networkObjects[i].NetworkManager != networkManager)
                {
                    throw new ArgumentNullException("All " + nameof(NetworkObject) + "s must belong to the same " + nameof(NetworkManager));
                }
            }

            foreach (var networkObject in networkObjects)
            {
                networkObject.NetworkShow(clientId);
            }
        }

        /// <summary>
        /// Hides the <see cref="NetworkObject"/> from the targeted client.
        /// </summary>
        /// <remarks>
        /// Usage: Use to stop sending updates to the targeted client, "netcode invisible", for a currently visible <see cref="NetworkObject"/>.<br />
        /// <br />
        /// Dynamically Spawned: <see cref="NetworkObject"/>s will be despawned and destroyed on the targeted client's side.<br />
        /// In-Scene Placed: <see cref="NetworkObject"/>s will only be despawned on the targeted client's side.<br />
        /// <br />
        /// See Also:<br />
        /// <see cref="NetworkHide(List{NetworkObject}, ulong)"/><br />
        /// <see cref="NetworkShow(ulong)"/> or <see cref="NetworkShow(List{NetworkObject}, ulong)"/><br />
        /// </remarks>
        /// <param name="clientId">The targeted client</param>
        public void NetworkHide(ulong clientId)
        {
            if (!IsSpawned)
            {
                throw new SpawnStateException("Object is not spawned");
            }

            if (!NetworkManager.IsServer)
            {
                throw new NotServerException("Only server can change visibility");
            }

            if (!Observers.Contains(clientId))
            {
                throw new VisibilityChangeException("The object is already hidden");
            }

            if (clientId == NetworkManager.ServerClientId)
            {
                throw new VisibilityChangeException("Cannot hide an object from the server");
            }

            Observers.Remove(clientId);

            var message = new DestroyObjectMessage
            {
                NetworkObjectId = NetworkObjectId,
                DestroyGameObject = !IsSceneObject.Value
            };
            // Send destroy call
            var size = NetworkManager.SendMessage(ref message, NetworkDelivery.ReliableSequenced, clientId);
            NetworkManager.NetworkMetrics.TrackObjectDestroySent(clientId, this, size);
        }

        /// <summary>
        /// Hides a list of <see cref="NetworkObject"/>s from the targeted client.
        /// </summary>
        /// <remarks>
        /// Usage: Use to stop sending updates to the targeted client, "netcode invisible", for the currently visible <see cref="NetworkObject"/>s.<br />
        /// <br />
        /// Dynamically Spawned: <see cref="NetworkObject"/>s will be despawned and destroyed on the targeted client's side.<br />
        /// In-Scene Placed: <see cref="NetworkObject"/>s will only be despawned on the targeted client's side.<br />
        /// <br />
        /// See Also:<br />
        /// <see cref="NetworkHide(ulong)"/><br />
        /// <see cref="NetworkShow(ulong)"/> or <see cref="NetworkShow(List{NetworkObject}, ulong)"/><br />
        /// </remarks>
        /// <param name="networkObjects">The <see cref="NetworkObject"/>s that will become "netcode invisible" to the targeted client</param>
        /// <param name="clientId">The targeted client</param>
        public static void NetworkHide(List<NetworkObject> networkObjects, ulong clientId)
        {
            if (networkObjects == null || networkObjects.Count == 0)
            {
                throw new ArgumentNullException("At least one " + nameof(NetworkObject) + " has to be provided");
            }

            var networkManager = networkObjects[0].NetworkManager;

            if (!networkManager.IsServer)
            {
                throw new NotServerException("Only server can change visibility");
            }

            if (clientId == NetworkManager.ServerClientId)
            {
                throw new VisibilityChangeException("Cannot hide an object from the server");
            }

            // Do the safety loop first to prevent putting the netcode in an invalid state.
            for (int i = 0; i < networkObjects.Count; i++)
            {
                if (!networkObjects[i].IsSpawned)
                {
                    throw new SpawnStateException("Object is not spawned");
                }

                if (!networkObjects[i].Observers.Contains(clientId))
                {
                    throw new VisibilityChangeException($"{nameof(NetworkObject)} with {nameof(NetworkObjectId)}: {networkObjects[i].NetworkObjectId} is already hidden");
                }

                if (networkObjects[i].NetworkManager != networkManager)
                {
                    throw new ArgumentNullException("All " + nameof(NetworkObject) + "s must belong to the same " + nameof(NetworkManager));
                }
            }

            foreach (var networkObject in networkObjects)
            {
                networkObject.NetworkHide(clientId);
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager != null && NetworkManager.IsListening && NetworkManager.IsServer == false && IsSpawned &&
                (IsSceneObject == null || (IsSceneObject.Value != true)))
            {
                throw new NotServerException($"Destroy a spawned {nameof(NetworkObject)} on a non-host client is not valid. Call {nameof(Destroy)} or {nameof(Despawn)} on the server/host instead.");
            }

            if (NetworkManager != null && NetworkManager.SpawnManager != null &&
                NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(NetworkObjectId, out var networkObject))
            {
                if (this == networkObject)
                {
                    NetworkManager.SpawnManager.OnDespawnObject(networkObject, false);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SpawnInternal(bool destroyWithScene, ulong ownerClientId, bool playerObject)
        {
            if (!NetworkManager.IsListening)
            {
                throw new NotListeningException($"{nameof(NetworkManager)} is not listening, start a server or host before spawning objects");
            }

            if (!NetworkManager.IsServer)
            {
                throw new NotServerException($"Only server can spawn {nameof(NetworkObject)}s");
            }

            NetworkManager.SpawnManager.SpawnNetworkObjectLocally(this, NetworkManager.SpawnManager.GetNetworkObjectId(), IsSceneObject.HasValue && IsSceneObject.Value, playerObject, ownerClientId, destroyWithScene);

            for (int i = 0; i < NetworkManager.ConnectedClientsList.Count; i++)
            {
                if (Observers.Contains(NetworkManager.ConnectedClientsList[i].ClientId))
                {
                    NetworkManager.SpawnManager.SendSpawnCallForObject(NetworkManager.ConnectedClientsList[i].ClientId, this);
                }
            }
        }

        /// <summary>
        /// Spawns this <see cref="NetworkObject"/> across the network. Can only be called from the Server
        /// </summary>
        /// <param name="destroyWithScene">Should the object be destroyed when the scene is changed</param>
        public void Spawn(bool destroyWithScene = false)
        {
            SpawnInternal(destroyWithScene, NetworkManager.ServerClientId, false);
        }

        /// <summary>
        /// Spawns a <see cref="NetworkObject"/> across the network with a given owner. Can only be called from server
        /// </summary>
        /// <param name="clientId">The clientId to own the object</param>
        /// <param name="destroyWithScene">Should the object be destroyed when the scene is changed</param>
        public void SpawnWithOwnership(ulong clientId, bool destroyWithScene = false)
        {
            SpawnInternal(destroyWithScene, clientId, false);
        }

        /// <summary>
        /// Spawns a <see cref="NetworkObject"/> across the network and makes it the player object for the given client
        /// </summary>
        /// <param name="clientId">The clientId who's player object this is</param>
        /// <param name="destroyWithScene">Should the object be destroyed when the scene is changed</param>
        public void SpawnAsPlayerObject(ulong clientId, bool destroyWithScene = false)
        {
            SpawnInternal(destroyWithScene, clientId, true);
        }

        /// <summary>
        /// Despawns the <see cref="GameObject"/> of this <see cref="NetworkObject"/> and sends a destroy message for it to all connected clients.
        /// </summary>
        /// <param name="destroy">(true) the <see cref="GameObject"/> will be destroyed (false) the <see cref="GameObject"/> will persist after being despawned</param>
        public void Despawn(bool destroy = true)
        {
            MarkVariablesDirty(false);
            NetworkManager.SpawnManager.DespawnObject(this, destroy);
        }

        /// <summary>
        /// Removes all ownership of an object from any client. Can only be called from server
        /// </summary>
        public void RemoveOwnership()
        {
            NetworkManager.SpawnManager.RemoveOwnership(this);
        }

        /// <summary>
        /// Changes the owner of the object. Can only be called from server
        /// </summary>
        /// <param name="newOwnerClientId">The new owner clientId</param>
        public void ChangeOwnership(ulong newOwnerClientId)
        {
            NetworkManager.SpawnManager.ChangeOwnership(this, newOwnerClientId);
        }

        internal void InvokeBehaviourOnLostOwnership()
        {
            // Server already handles this earlier, hosts should ignore, all clients should update
            if (!NetworkManager.IsServer)
            {
                NetworkManager.SpawnManager.UpdateOwnershipTable(this, OwnerClientId, true);
            }

            for (int i = 0; i < ChildNetworkBehaviours.Count; i++)
            {
                ChildNetworkBehaviours[i].InternalOnLostOwnership();
            }
        }

        internal void InvokeBehaviourOnGainedOwnership()
        {
            // Server already handles this earlier, hosts should ignore and only client owners should update
            if (!NetworkManager.IsServer && NetworkManager.LocalClientId == OwnerClientId)
            {
                NetworkManager.SpawnManager.UpdateOwnershipTable(this, OwnerClientId);
            }

            for (int i = 0; i < ChildNetworkBehaviours.Count; i++)
            {
                if (ChildNetworkBehaviours[i].gameObject.activeInHierarchy)
                {
                    ChildNetworkBehaviours[i].InternalOnGainedOwnership();
                }
                else
                {
                    Debug.LogWarning($"{ChildNetworkBehaviours[i].gameObject.name} is disabled! Netcode for GameObjects does not support disabled NetworkBehaviours! The {ChildNetworkBehaviours[i].GetType().Name} component was skipped during ownership assignment!");
                }
            }
        }

        internal void InvokeBehaviourOnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
        {
            for (int i = 0; i < ChildNetworkBehaviours.Count; i++)
            {
                ChildNetworkBehaviours[i].OnNetworkObjectParentChanged(parentNetworkObject);
            }
        }

        private ulong? m_LatestParent; // What is our last set parent NetworkObject's ID?
        private Transform m_CachedParent; // What is our last set parent Transform reference?
        private bool m_CachedWorldPositionStays = true; // Used to preserve the world position stays parameter passed in TrySetParent

        internal void SetCachedParent(Transform parentTransform)
        {
            m_CachedParent = parentTransform;
        }

        internal ulong? GetNetworkParenting() => m_LatestParent;

        internal void SetNetworkParenting(ulong? latestParent, bool worldPositionStays)
        {
            m_LatestParent = latestParent;
            m_CachedWorldPositionStays = worldPositionStays;
        }

        /// <summary>
        /// Set the parent of the NetworkObject transform.
        /// </summary>
        /// <param name="parent">The new parent for this NetworkObject transform will be the child of.</param>
        /// <param name="worldPositionStays">If true, the parent-relative position, scale and rotation are modified such that the object keeps the same world space position, rotation and scale as before.</param>
        /// <returns>Whether or not reparenting was successful.</returns>
        public bool TrySetParent(Transform parent, bool worldPositionStays = true)
        {
            var networkObject = parent.GetComponent<NetworkObject>();

            // If the parent doesn't have a NetworkObjet then return false, otherwise continue trying to parent
            return networkObject == null ? false : TrySetParent(networkObject, worldPositionStays);
        }

        /// <summary>
        /// Set the parent of the NetworkObject transform.
        /// </summary>
        /// <param name="parent">The new parent for this NetworkObject transform will be the child of.</param>
        /// <param name="worldPositionStays">If true, the parent-relative position, scale and rotation are modified such that the object keeps the same world space position, rotation and scale as before.</param>
        /// <returns>Whether or not reparenting was successful.</returns>
        public bool TrySetParent(GameObject parent, bool worldPositionStays = true)
        {
            // If we are removing ourself from a parent
            if (parent == null)
            {
                return TrySetParent((NetworkObject)null, worldPositionStays);
            }

            var networkObject = parent.GetComponent<NetworkObject>();

            // If the parent doesn't have a NetworkObjet then return false, otherwise continue trying to parent
            return networkObject == null ? false : TrySetParent(networkObject, worldPositionStays);
        }

        /// <summary>
        /// Used when despawning the parent, we want to preserve the cached WorldPositionStays value
        /// </summary>
        internal bool TryRemoveParentCachedWorldPositionStays()
        {
            return TrySetParent((NetworkObject)null, m_CachedWorldPositionStays);
        }

        /// <summary>
        /// Removes the parent of the NetworkObject's transform
        /// </summary>
        /// <remarks>
        /// This is a more convenient way to remove the parent without  having to cast the null value to either <see cref="GameObject"/> or <see cref="NetworkObject"/>
        /// </remarks>
        /// <param name="worldPositionStays">If true, the parent-relative position, scale and rotation are modified such that the object keeps the same world space position, rotation and scale as before.</param>
        /// <returns></returns>
        public bool TryRemoveParent(bool worldPositionStays = true)
        {
            return TrySetParent((NetworkObject)null, worldPositionStays);
        }

        /// <summary>
        /// Set the parent of the NetworkObject transform.
        /// </summary>
        /// <param name="parent">The new parent for this NetworkObject transform will be the child of.</param>
        /// <param name="worldPositionStays">If true, the parent-relative position, scale and rotation are modified such that the object keeps the same world space position, rotation and scale as before.</param>
        /// <returns>Whether or not reparenting was successful.</returns>
        public bool TrySetParent(NetworkObject parent, bool worldPositionStays = true)
        {
            if (!AutoObjectParentSync)
            {
                return false;
            }

            if (NetworkManager == null || !NetworkManager.IsListening)
            {
                return false;
            }

            if (!NetworkManager.IsServer)
            {
                return false;
            }

            if (!IsSpawned)
            {
                return false;
            }

            if (parent != null && !parent.IsSpawned)
            {
                return false;
            }
            m_CachedWorldPositionStays = worldPositionStays;

            if (parent == null)
            {
                transform.SetParent(null, worldPositionStays);
            }
            else
            {
                transform.SetParent(parent.transform, worldPositionStays);
            }

            return true;
        }

        private void OnTransformParentChanged()
        {
            if (!AutoObjectParentSync)
            {
                return;
            }

            if (transform.parent == m_CachedParent)
            {
                return;
            }

            if (NetworkManager == null || !NetworkManager.IsListening)
            {
                transform.parent = m_CachedParent;
                Debug.LogException(new NotListeningException($"{nameof(NetworkManager)} is not listening, start a server or host before reparenting"));
                return;
            }

            if (!NetworkManager.IsServer)
            {
                transform.parent = m_CachedParent;
                Debug.LogException(new NotServerException($"Only the server can reparent {nameof(NetworkObject)}s"));
                return;
            }

            if (!IsSpawned)
            {
                transform.parent = m_CachedParent;
                Debug.LogException(new SpawnStateException($"{nameof(NetworkObject)} can only be reparented after being spawned"));
                return;
            }
            var removeParent = false;
            var parentTransform = transform.parent;
            if (parentTransform != null)
            {
                if (!transform.parent.TryGetComponent<NetworkObject>(out var parentObject))
                {
                    transform.parent = m_CachedParent;
                    Debug.LogException(new InvalidParentException($"Invalid parenting, {nameof(NetworkObject)} moved under a non-{nameof(NetworkObject)} parent"));
                    return;
                }

                if (!parentObject.IsSpawned)
                {
                    transform.parent = m_CachedParent;
                    Debug.LogException(new SpawnStateException($"{nameof(NetworkObject)} can only be reparented under another spawned {nameof(NetworkObject)}"));
                    return;
                }

                m_LatestParent = parentObject.NetworkObjectId;
            }
            else
            {
                m_LatestParent = null;
                removeParent = m_CachedParent != null;
            }

            ApplyNetworkParenting(removeParent);

            var message = new ParentSyncMessage
            {
                NetworkObjectId = NetworkObjectId,
                IsLatestParentSet = m_LatestParent != null && m_LatestParent.HasValue,
                LatestParent = m_LatestParent,
                RemoveParent = removeParent,
                WorldPositionStays = m_CachedWorldPositionStays,
                Position = m_CachedWorldPositionStays ? transform.position : transform.localPosition,
                Rotation = m_CachedWorldPositionStays ? transform.rotation : transform.localRotation,
                Scale = transform.localScale,
            };

            // We need to preserve the m_CachedWorldPositionStays value until after we create the message
            // in order to assure any local space values changed/reset get applied properly. If our
            // parent is null then go ahead and reset the m_CachedWorldPositionStays the default value.
            if (parentTransform == null)
            {
                m_CachedWorldPositionStays = true;
            }

            unsafe
            {
                var maxCount = NetworkManager.ConnectedClientsIds.Count;
                ulong* clientIds = stackalloc ulong[maxCount];
                int idx = 0;
                foreach (var clientId in NetworkManager.ConnectedClientsIds)
                {
                    if (Observers.Contains(clientId))
                    {
                        clientIds[idx++] = clientId;
                    }
                }

                NetworkManager.SendMessage(ref message, NetworkDelivery.ReliableSequenced, clientIds, idx);
            }
        }

        // We're keeping this set called OrphanChildren which contains NetworkObjects
        // because at the time we initialize/spawn NetworkObject locally, we might not have its parent replicated from the other side
        //
        // For instance, if we're spawning NetworkObject 5 and its parent is 10, what should happen if we do not have 10 yet?
        // let's say 10 is on the way to be replicated in a few frames and we could fix that parent-child relationship later.
        //
        // If you couldn't find your parent, we put you into OrphanChildren set and every time we spawn another NetworkObject locally due to replication,
        // we call CheckOrphanChildren() method and quickly iterate over OrphanChildren set and see if we can reparent/adopt one.
        internal static HashSet<NetworkObject> OrphanChildren = new HashSet<NetworkObject>();

        internal bool ApplyNetworkParenting(bool removeParent = false, bool ignoreNotSpawned = false)
        {
            if (!AutoObjectParentSync)
            {
                return false;
            }

            // SPECIAL CASE:
            // The ignoreNotSpawned is a special case scenario where a late joining client has joined
            // and loaded one or more scenes that contain nested in-scene placed NetworkObject children
            // yet the server's synchronization information does not indicate the NetworkObject in question
            // has a parent. Under this scenario, we want to remove the parent before spawning and setting
            // the transform values. This is the only scenario where the ignoreNotSpawned parameter is used.
            if (!IsSpawned && !ignoreNotSpawned)
            {
                return false;
            }

            // Handle the first in-scene placed NetworkObject parenting scenarios. Once the m_LatestParent
            // has been set, this will not be entered into again (i.e. the later code will be invoked and
            // users will get notifications when the parent changes).
            var isInScenePlaced = IsSceneObject.HasValue && IsSceneObject.Value;
            if (transform.parent != null && !removeParent && !m_LatestParent.HasValue && isInScenePlaced)
            {
                var parentNetworkObject = transform.parent.GetComponent<NetworkObject>();

                // If parentNetworkObject is null then the parent is a GameObject without a NetworkObject component
                // attached. Under this case, we preserve the hierarchy but we don't keep track of the parenting.
                // Note: We only start tracking parenting if the user removes the child from the standard GameObject
                // parent and then re-parents the child under a GameObject with a NetworkObject component attached.
                if (parentNetworkObject == null)
                {
                    return true;
                }
                else // If the parent still isn't spawned add this to the orphaned children and return false
                if (!parentNetworkObject.IsSpawned)
                {
                    OrphanChildren.Add(this);
                    return false;
                }
                else
                {
                    // If we made it this far, go ahead and set the network parenting values
                    // with the default WorldPoisitonSays value
                    SetNetworkParenting(parentNetworkObject.NetworkObjectId, true);

                    // Set the cached parent
                    m_CachedParent = parentNetworkObject.transform;

                    return true;
                }
            }

            // If we are removing the parent or our latest parent is not set, then remove the parent
            // removeParent is only set when:
            //  - The server-side NetworkObject.OnTransformParentChanged is invoked and the parent is being removed
            //  - The client-side when handling a ParentSyncMessage
            // When clients are synchronizing only the m_LatestParent.HasValue will not have a value if there is no parent
            // or a parent was removed prior to the client connecting (i.e. in-scene placed NetworkObjects)
            if (removeParent || !m_LatestParent.HasValue)
            {
                m_CachedParent = null;
                // We must use Transform.SetParent when taking WorldPositionStays into
                // consideration, otherwise just setting transform.parent = null defaults
                // to WorldPositionStays which can cause scaling issues if the parent's
                // scale is not the default (Vetctor3.one) value.
                transform.SetParent(null, m_CachedWorldPositionStays);
                InvokeBehaviourOnNetworkObjectParentChanged(null);
                return true;
            }

            // If we have a latest parent id but it hasn't been spawned yet, then add this instance to the orphanChildren
            // HashSet and return false (i.e. parenting not applied yet)
            if (m_LatestParent.HasValue && !NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(m_LatestParent.Value))
            {
                OrphanChildren.Add(this);
                return false;
            }

            // If we made it here, then parent this instance under the parentObject
            var parentObject = NetworkManager.SpawnManager.SpawnedObjects[m_LatestParent.Value];

            m_CachedParent = parentObject.transform;
            transform.SetParent(parentObject.transform, m_CachedWorldPositionStays);

            InvokeBehaviourOnNetworkObjectParentChanged(parentObject);
            return true;
        }

        internal static void CheckOrphanChildren()
        {
            var objectsToRemove = new List<NetworkObject>();
            foreach (var orphanObject in OrphanChildren)
            {
                if (orphanObject.ApplyNetworkParenting())
                {
                    objectsToRemove.Add(orphanObject);
                }
            }
            foreach (var networkObject in objectsToRemove)
            {
                OrphanChildren.Remove(networkObject);
            }
        }

        internal void InvokeBehaviourNetworkSpawn()
        {
            NetworkManager.SpawnManager.UpdateOwnershipTable(this, OwnerClientId);

            for (int i = 0; i < ChildNetworkBehaviours.Count; i++)
            {
                if (ChildNetworkBehaviours[i].gameObject.activeInHierarchy)
                {
                    ChildNetworkBehaviours[i].InternalOnNetworkSpawn();
                }
                else
                {
                    Debug.LogWarning($"{ChildNetworkBehaviours[i].gameObject.name} is disabled! Netcode for GameObjects does not support spawning disabled NetworkBehaviours! The {ChildNetworkBehaviours[i].GetType().Name} component was skipped during spawn!");
                }
            }
            for (int i = 0; i < ChildNetworkBehaviours.Count; i++)
            {
                if (ChildNetworkBehaviours[i].gameObject.activeInHierarchy)
                {
                    ChildNetworkBehaviours[i].VisibleOnNetworkSpawn();
                }
            }
        }

        internal void InvokeBehaviourNetworkDespawn()
        {
            NetworkManager.SpawnManager.UpdateOwnershipTable(this, OwnerClientId, true);

            for (int i = 0; i < ChildNetworkBehaviours.Count; i++)
            {
                ChildNetworkBehaviours[i].InternalOnNetworkDespawn();
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

        internal void WriteNetworkVariableData(FastBufferWriter writer, ulong targetClientId)
        {
            for (int i = 0; i < ChildNetworkBehaviours.Count; i++)
            {
                var behavior = ChildNetworkBehaviours[i];
                behavior.InitializeVariables();
                behavior.WriteNetworkVariableData(writer, targetClientId);
            }
        }

        internal void MarkVariablesDirty(bool dirty)
        {
            for (int i = 0; i < ChildNetworkBehaviours.Count; i++)
            {
                var behavior = ChildNetworkBehaviours[i];
                behavior.MarkVariablesDirty(dirty);
            }
        }

        // NGO currently guarantees that the client will receive spawn data for all objects in one network tick.
        //  Children may arrive before their parents; when they do they are stored in OrphanedChildren and then
        //  resolved when their parents arrived.  Because we don't send a partial list of spawns (yet), something
        //  has gone wrong if by the end of an update we still have unresolved orphans
        //

        // if and when we have different systems for where it is expected that orphans survive across ticks,
        //   then this warning will remind us that we need to revamp the system because then we can no longer simply
        //   spawn the orphan without its parent (at least, not when its transform is set to local coords mode)
        //   - because then you'll have children popping at the wrong location not having their parent's global position to root them
        //   - and then they'll pop to the correct location after they get the parent, and that would be not good
        internal static void VerifyParentingStatus()
        {
            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
            {
                if (OrphanChildren.Count > 0)
                {
                    NetworkLog.LogWarning($"{nameof(NetworkObject)} ({OrphanChildren.Count}) children not resolved to parents by the end of frame");
                }
            }
        }
        internal void SetNetworkVariableData(FastBufferReader reader)
        {
            for (int i = 0; i < ChildNetworkBehaviours.Count; i++)
            {
                var behaviour = ChildNetworkBehaviours[i];
                behaviour.InitializeVariables();
                behaviour.SetNetworkVariableData(reader);
            }
        }

        internal ushort GetNetworkBehaviourOrderIndex(NetworkBehaviour instance)
        {
            // read the cached index, and verify it first
            if (instance.NetworkBehaviourIdCache < ChildNetworkBehaviours.Count)
            {
                if (ChildNetworkBehaviours[instance.NetworkBehaviourIdCache] == instance)
                {
                    return instance.NetworkBehaviourIdCache;
                }

                // invalid cached id reset
                instance.NetworkBehaviourIdCache = default;
            }

            for (ushort i = 0; i < ChildNetworkBehaviours.Count; i++)
            {
                if (ChildNetworkBehaviours[i] == instance)
                {
                    // cache the id, for next query
                    instance.NetworkBehaviourIdCache = i;
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

        internal struct SceneObject
        {
            public struct HeaderData : INetworkSerializeByMemcpy
            {
                public ulong NetworkObjectId;
                public ulong OwnerClientId;
                public uint Hash;

                public bool IsPlayerObject;
                public bool HasParent;
                public bool IsSceneObject;
                public bool HasTransform;
            }

            public HeaderData Header;

            //If(Metadata.HasParent)
            public ulong ParentObjectId;

            //If(Metadata.HasTransform)
            public struct TransformData : INetworkSerializeByMemcpy
            {
                public Vector3 Position;
                public Quaternion Rotation;
                public Vector3 Scale;
            }

            public TransformData Transform;

            //If(Metadata.IsReparented)
            public bool IsLatestParentSet;

            //If(IsLatestParentSet)
            public ulong? LatestParent;

            public NetworkObject OwnerObject;
            public ulong TargetClientId;

            public int NetworkSceneHandle;

            public bool WorldPositionStays;

            public unsafe void Serialize(FastBufferWriter writer)
            {
                var writeSize = sizeof(HeaderData);
                if (Header.HasParent)
                {
                    writeSize += FastBufferWriter.GetWriteSize(ParentObjectId);
                    writeSize += FastBufferWriter.GetWriteSize(WorldPositionStays);
                    writeSize += FastBufferWriter.GetWriteSize(IsLatestParentSet);
                    writeSize += IsLatestParentSet ? FastBufferWriter.GetWriteSize<ulong>() : 0;
                }
                writeSize += Header.HasTransform ? FastBufferWriter.GetWriteSize<TransformData>() : 0;
                writeSize += Header.IsSceneObject ? FastBufferWriter.GetWriteSize<int>() : 0;

                if (!writer.TryBeginWrite(writeSize))
                {
                    throw new OverflowException("Could not serialize SceneObject: Out of buffer space.");
                }

                writer.WriteValue(Header);

                if (Header.HasParent)
                {
                    writer.WriteValue(ParentObjectId);
                    writer.WriteValue(WorldPositionStays);
                    writer.WriteValue(IsLatestParentSet);
                    if (IsLatestParentSet)
                    {
                        writer.WriteValue(LatestParent.Value);
                    }
                }

                if (Header.HasTransform)
                {
                    writer.WriteValue(Transform);
                }

                // In-Scene NetworkObjects are uniquely identified NetworkPrefabs defined by their
                // NetworkSceneHandle and GlobalObjectIdHash. Since each loaded scene has a unique
                // handle, it provides us with a unique and persistent "scene prefab asset" instance.
                // This is only set on in-scene placed NetworkObjects to reduce the over-all packet
                // sizes for dynamically spawned NetworkObjects.
                if (Header.IsSceneObject)
                {
                    writer.WriteValue(OwnerObject.GetSceneOriginHandle());
                }

                OwnerObject.WriteNetworkVariableData(writer, TargetClientId);
            }

            public unsafe void Deserialize(FastBufferReader reader)
            {
                if (!reader.TryBeginRead(sizeof(HeaderData)))
                {
                    throw new OverflowException("Could not deserialize SceneObject: Out of buffer space.");
                }
                reader.ReadValue(out Header);
                var readSize = 0;
                if (Header.HasParent)
                {
                    readSize += FastBufferWriter.GetWriteSize(ParentObjectId);
                    readSize += FastBufferWriter.GetWriteSize(WorldPositionStays);
                    readSize += FastBufferWriter.GetWriteSize(IsLatestParentSet);
                    // We need to read at this point in order to get the IsLatestParentSet value
                    if (!reader.TryBeginRead(readSize))
                    {
                        throw new OverflowException("Could not deserialize SceneObject: Out of buffer space.");
                    }

                    // Read the initial parenting related properties
                    reader.ReadValue(out ParentObjectId);
                    reader.ReadValue(out WorldPositionStays);
                    reader.ReadValue(out IsLatestParentSet);

                    // Now calculate the remaining bytes to read
                    readSize = 0;
                    readSize += IsLatestParentSet ? FastBufferWriter.GetWriteSize<ulong>() : 0;
                }

                readSize += Header.HasTransform ? FastBufferWriter.GetWriteSize<TransformData>() : 0;
                readSize += Header.IsSceneObject ? FastBufferWriter.GetWriteSize<int>() : 0;

                // Try to begin reading the remaining bytes
                if (!reader.TryBeginRead(readSize))
                {
                    throw new OverflowException("Could not deserialize SceneObject: Out of buffer space.");
                }

                if (IsLatestParentSet)
                {
                    reader.ReadValueSafe(out ulong latestParent);
                    LatestParent = latestParent;
                }

                if (Header.HasTransform)
                {
                    reader.ReadValue(out Transform);
                }

                // In-Scene NetworkObjects are uniquely identified NetworkPrefabs defined by their
                // NetworkSceneHandle and GlobalObjectIdHash. Since each loaded scene has a unique
                // handle, it provides us with a unique and persistent "scene prefab asset" instance.
                // Client-side NetworkSceneManagers use this to locate their local instance of the
                // NetworkObject instance.
                if (Header.IsSceneObject)
                {
                    reader.ReadValueSafe(out NetworkSceneHandle);
                }
            }
        }

        internal void PostNetworkVariableWrite()
        {
            for (int k = 0; k < ChildNetworkBehaviours.Count; k++)
            {
                ChildNetworkBehaviours[k].PostNetworkVariableWrite();
            }
        }

        internal SceneObject GetMessageSceneObject(ulong targetClientId)
        {
            var obj = new SceneObject
            {
                Header = new SceneObject.HeaderData
                {
                    IsPlayerObject = IsPlayerObject,
                    NetworkObjectId = NetworkObjectId,
                    OwnerClientId = OwnerClientId,
                    IsSceneObject = IsSceneObject ?? true,
                    Hash = HostCheckForGlobalObjectIdHashOverride(),
                },
                OwnerObject = this,
                TargetClientId = targetClientId
            };

            NetworkObject parentNetworkObject = null;

            if (!AlwaysReplicateAsRoot && transform.parent != null)
            {
                parentNetworkObject = transform.parent.GetComponent<NetworkObject>();
            }

            if (parentNetworkObject != null)
            {
                obj.Header.HasParent = true;
                obj.ParentObjectId = parentNetworkObject.NetworkObjectId;
                obj.WorldPositionStays = m_CachedWorldPositionStays;
                var latestParent = GetNetworkParenting();
                var isLatestParentSet = latestParent != null && latestParent.HasValue;
                obj.IsLatestParentSet = isLatestParentSet;
                if (isLatestParentSet)
                {
                    obj.LatestParent = latestParent.Value;
                }
            }

            if (IncludeTransformWhenSpawning == null || IncludeTransformWhenSpawning(OwnerClientId))
            {
                obj.Header.HasTransform = true;
                obj.Transform = new SceneObject.TransformData
                {
                    // If we are parented and we have the m_CachedWorldPositionStays disabled, then use local space
                    // values as opposed world space values.
                    Position = parentNetworkObject && !m_CachedWorldPositionStays ? transform.localPosition : transform.position,
                    Rotation = parentNetworkObject && !m_CachedWorldPositionStays ? transform.localRotation : transform.rotation,

                    // We only use the lossyScale if the NetworkObject has a parent. Multi-generation nested children scales can
                    // impact the final scale of the child NetworkObject in question. The solution is to use the lossy scale
                    // which can be thought of as "world space scale".
                    // More information:
                    // https://docs.unity3d.com/ScriptReference/Transform-lossyScale.html
                    Scale = parentNetworkObject && !m_CachedWorldPositionStays ? transform.localScale : transform.lossyScale,
                };
            }

            return obj;
        }

        /// <summary>
        /// Used to deserialize a serialized scene object which occurs
        /// when the client is approved or during a scene transition
        /// </summary>
        /// <param name="sceneObject">Deserialized scene object data</param>
        /// <param name="variableData">reader for the NetworkVariable data</param>
        /// <param name="networkManager">NetworkManager instance</param>
        /// <returns>optional to use NetworkObject deserialized</returns>
        internal static NetworkObject AddSceneObject(in SceneObject sceneObject, FastBufferReader variableData, NetworkManager networkManager)
        {
            //Attempt to create a local NetworkObject
            var networkObject = networkManager.SpawnManager.CreateLocalNetworkObject(sceneObject);

            if (networkObject == null)
            {
                // Log the error that the NetworkObject failed to construct
                Debug.LogError($"Failed to spawn {nameof(NetworkObject)} for Hash {sceneObject.Header.Hash}.");

                // If we failed to load this NetworkObject, then skip past the network variable data
                variableData.ReadValueSafe(out ushort varSize);
                variableData.Seek(variableData.Position + varSize);

                // We have nothing left to do here.
                return null;
            }

            // Spawn the NetworkObject
            networkManager.SpawnManager.SpawnNetworkObjectLocally(networkObject, sceneObject, variableData, false);

            return networkObject;
        }

        /// <summary>
        /// Only applies to Host mode.
        /// Will return the registered source NetworkPrefab's GlobalObjectIdHash if one exists.
        /// Server and Clients will always return the NetworkObject's GlobalObjectIdHash.
        /// </summary>
        /// <returns></returns>
        internal uint HostCheckForGlobalObjectIdHashOverride()
        {
            if (NetworkManager.IsHost)
            {
                if (NetworkManager.PrefabHandler.ContainsHandler(this))
                {
                    var globalObjectIdHash = NetworkManager.PrefabHandler.GetSourceGlobalObjectIdHash(GlobalObjectIdHash);
                    return globalObjectIdHash == 0 ? GlobalObjectIdHash : globalObjectIdHash;
                }
                else if (NetworkManager.NetworkConfig.OverrideToNetworkPrefab.ContainsKey(GlobalObjectIdHash))
                {
                    return NetworkManager.NetworkConfig.OverrideToNetworkPrefab[GlobalObjectIdHash];
                }
            }

            return GlobalObjectIdHash;
        }

        /// <summary>
        /// Removes a NetworkBehaviour from the ChildNetworkBehaviours list when destroyed
        /// while the NetworkObject is still spawned.
        /// </summary>
        internal void OnNetworkBehaviourDestroyed(NetworkBehaviour networkBehaviour)
        {
            if (networkBehaviour.IsSpawned && IsSpawned)
            {
                if (NetworkManager.LogLevel == LogLevel.Developer)
                {
                    NetworkLog.LogWarning($"{nameof(NetworkBehaviour)}-{networkBehaviour.name} is being destroyed while {nameof(NetworkObject)}-{name} is still spawned! (could break state synchronization)");
                }
                ChildNetworkBehaviours.Remove(networkBehaviour);
            }
        }
    }
}
