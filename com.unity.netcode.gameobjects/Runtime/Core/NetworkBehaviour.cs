using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;


namespace Unity.Netcode
{
    public class RpcException : Exception
    {
        public RpcException(string message) : base(message)
        {

        }
    }

    /// <summary>
    /// The base class to override to write network code. Inherits MonoBehaviour
    /// </summary>
    public abstract class NetworkBehaviour : MonoBehaviour
    {
#pragma warning disable IDE1006 // disable naming rule violation check

        // RuntimeAccessModifiersILPP will make this `public`
        internal delegate void RpcReceiveHandler(NetworkBehaviour behaviour, FastBufferReader reader, __RpcParams parameters);

        // RuntimeAccessModifiersILPP will make this `public`
        internal static readonly Dictionary<Type, Dictionary<uint, RpcReceiveHandler>> __rpc_func_table = new Dictionary<Type, Dictionary<uint, RpcReceiveHandler>>();

#if DEVELOPMENT_BUILD || UNITY_EDITOR || UNITY_MP_TOOLS_NET_STATS_MONITOR_ENABLED_IN_RELEASE
        // RuntimeAccessModifiersILPP will make this `public`
        internal static readonly Dictionary<Type, Dictionary<uint, string>> __rpc_name_table = new Dictionary<Type, Dictionary<uint, string>>();
#endif

        // RuntimeAccessModifiersILPP will make this `protected`
        internal enum __RpcExecStage
        {
            // Technically will overlap with None and Server
            // but it doesn't matter since we don't use None and Server
            Send = 0,
            Execute = 1,

            // Backward compatibility, not used...
            None = 0,
            Server = 1,
            Client = 2,
        }
        // NetworkBehaviourILPP will override this in derived classes to return the name of the concrete type
        internal virtual string __getTypeName() => nameof(NetworkBehaviour);

        [NonSerialized]
        // RuntimeAccessModifiersILPP will make this `protected`
        internal __RpcExecStage __rpc_exec_stage = __RpcExecStage.Send;
#pragma warning restore IDE1006 // restore naming rule violation check

        private const int k_RpcMessageDefaultSize = 1024; // 1k
        private const int k_RpcMessageMaximumSize = 1024 * 64; // 64k

#pragma warning disable IDE1006 // disable naming rule violation check
        // RuntimeAccessModifiersILPP will make this `protected`
        internal FastBufferWriter __beginSendServerRpc(uint rpcMethodId, ServerRpcParams serverRpcParams, RpcDelivery rpcDelivery)
#pragma warning restore IDE1006 // restore naming rule violation check
        {
            return new FastBufferWriter(k_RpcMessageDefaultSize, Allocator.Temp, k_RpcMessageMaximumSize);
        }

#pragma warning disable IDE1006 // disable naming rule violation check
        // RuntimeAccessModifiersILPP will make this `protected`
        internal void __endSendServerRpc(ref FastBufferWriter bufferWriter, uint rpcMethodId, ServerRpcParams serverRpcParams, RpcDelivery rpcDelivery)
#pragma warning restore IDE1006 // restore naming rule violation check
        {
            var serverRpcMessage = new ServerRpcMessage
            {
                Metadata = new RpcMetadata
                {
                    NetworkObjectId = NetworkObjectId,
                    NetworkBehaviourId = NetworkBehaviourId,
                    NetworkRpcMethodId = rpcMethodId,
                },
                WriteBuffer = bufferWriter
            };

            NetworkDelivery networkDelivery;
            switch (rpcDelivery)
            {
                default:
                case RpcDelivery.Reliable:
                    networkDelivery = NetworkDelivery.ReliableFragmentedSequenced;
                    break;
                case RpcDelivery.Unreliable:
                    if (bufferWriter.Length > NetworkManager.MessageManager.NonFragmentedMessageMaxSize)
                    {
                        throw new OverflowException("RPC parameters are too large for unreliable delivery.");
                    }
                    networkDelivery = NetworkDelivery.Unreliable;
                    break;
            }

            var rpcWriteSize = 0;

            // If we are a server/host then we just no op and send to ourself
            if (IsHost || IsServer)
            {
                using var tempBuffer = new FastBufferReader(bufferWriter, Allocator.Temp);
                var context = new NetworkContext
                {
                    SenderId = NetworkManager.ServerClientId,
                    Timestamp = NetworkManager.RealTimeProvider.RealTimeSinceStartup,
                    SystemOwner = NetworkManager,
                    // header information isn't valid since it's not a real message.
                    // RpcMessage doesn't access this stuff so it's just left empty.
                    Header = new NetworkMessageHeader(),
                    SerializedHeaderSize = 0,
                    MessageSize = 0
                };
                serverRpcMessage.ReadBuffer = tempBuffer;
                serverRpcMessage.Handle(ref context);
                rpcWriteSize = tempBuffer.Length;
            }
            else
            {
                rpcWriteSize = NetworkManager.ConnectionManager.SendMessage(ref serverRpcMessage, networkDelivery, NetworkManager.ServerClientId);
            }

            bufferWriter.Dispose();
#if DEVELOPMENT_BUILD || UNITY_EDITOR || UNITY_MP_TOOLS_NET_STATS_MONITOR_ENABLED_IN_RELEASE
            if (__rpc_name_table[GetType()].TryGetValue(rpcMethodId, out var rpcMethodName))
            {
                NetworkManager.NetworkMetrics.TrackRpcSent(
                    NetworkManager.ServerClientId,
                    NetworkObject,
                    rpcMethodName,
                    __getTypeName(),
                    rpcWriteSize);
            }
#endif
        }

#pragma warning disable IDE1006 // disable naming rule violation check
        // RuntimeAccessModifiersILPP will make this `protected`
        internal FastBufferWriter __beginSendClientRpc(uint rpcMethodId, ClientRpcParams clientRpcParams, RpcDelivery rpcDelivery)
#pragma warning restore IDE1006 // restore naming rule violation check
        {
            return new FastBufferWriter(k_RpcMessageDefaultSize, Allocator.Temp, k_RpcMessageMaximumSize);
        }

#pragma warning disable IDE1006 // disable naming rule violation check
        // RuntimeAccessModifiersILPP will make this `protected`
        internal void __endSendClientRpc(ref FastBufferWriter bufferWriter, uint rpcMethodId, ClientRpcParams clientRpcParams, RpcDelivery rpcDelivery)
#pragma warning restore IDE1006 // restore naming rule violation check
        {
            var clientRpcMessage = new ClientRpcMessage
            {
                Metadata = new RpcMetadata
                {
                    NetworkObjectId = NetworkObjectId,
                    NetworkBehaviourId = NetworkBehaviourId,
                    NetworkRpcMethodId = rpcMethodId,
                },
                WriteBuffer = bufferWriter
            };

            NetworkDelivery networkDelivery;
            switch (rpcDelivery)
            {
                default:
                case RpcDelivery.Reliable:
                    networkDelivery = NetworkDelivery.ReliableFragmentedSequenced;
                    break;
                case RpcDelivery.Unreliable:
                    if (bufferWriter.Length > NetworkManager.MessageManager.NonFragmentedMessageMaxSize)
                    {
                        throw new OverflowException("RPC parameters are too large for unreliable delivery.");
                    }
                    networkDelivery = NetworkDelivery.Unreliable;
                    break;
            }

            var rpcWriteSize = 0;

            // We check to see if we need to shortcut for the case where we are the host/server and we can send a clientRPC
            // to ourself. Sadly we have to figure that out from the list of clientIds :(
            bool shouldSendToHost = false;
            if (clientRpcParams.Send.TargetClientIds != null)
            {
                foreach (var targetClientId in clientRpcParams.Send.TargetClientIds)
                {
                    if (targetClientId == NetworkManager.ServerClientId)
                    {
                        shouldSendToHost = true;
                        break;
                    }

                    // Check to make sure we are sending to only observers, if not log an error.
                    if (NetworkManager.LogLevel >= LogLevel.Error && !NetworkObject.Observers.Contains(targetClientId))
                    {
                        NetworkLog.LogError(GenerateObserverErrorMessage(clientRpcParams, targetClientId));
                    }
                }

                rpcWriteSize = NetworkManager.ConnectionManager.SendMessage(ref clientRpcMessage, networkDelivery, in clientRpcParams.Send.TargetClientIds);
            }
            else if (clientRpcParams.Send.TargetClientIdsNativeArray != null)
            {
                foreach (var targetClientId in clientRpcParams.Send.TargetClientIdsNativeArray)
                {
                    if (targetClientId == NetworkManager.ServerClientId)
                    {
                        shouldSendToHost = true;
                        break;
                    }

                    // Check to make sure we are sending to only observers, if not log an error.
                    if (NetworkManager.LogLevel >= LogLevel.Error && !NetworkObject.Observers.Contains(targetClientId))
                    {
                        NetworkLog.LogError(GenerateObserverErrorMessage(clientRpcParams, targetClientId));
                    }
                }

                rpcWriteSize = NetworkManager.ConnectionManager.SendMessage(ref clientRpcMessage, networkDelivery, clientRpcParams.Send.TargetClientIdsNativeArray.Value);
            }
            else
            {
                var observerEnumerator = NetworkObject.Observers.GetEnumerator();
                while (observerEnumerator.MoveNext())
                {
                    // Skip over the host
                    if (IsHost && observerEnumerator.Current == NetworkManager.LocalClientId)
                    {
                        shouldSendToHost = true;
                        continue;
                    }
                    rpcWriteSize = NetworkManager.ConnectionManager.SendMessage(ref clientRpcMessage, networkDelivery, observerEnumerator.Current);
                }
            }

            // If we are a server/host then we just no op and send to ourself
            if (shouldSendToHost)
            {
                using var tempBuffer = new FastBufferReader(bufferWriter, Allocator.Temp);
                var context = new NetworkContext
                {
                    SenderId = NetworkManager.ServerClientId,
                    Timestamp = NetworkManager.RealTimeProvider.RealTimeSinceStartup,
                    SystemOwner = NetworkManager,
                    // header information isn't valid since it's not a real message.
                    // RpcMessage doesn't access this stuff so it's just left empty.
                    Header = new NetworkMessageHeader(),
                    SerializedHeaderSize = 0,
                    MessageSize = 0
                };
                clientRpcMessage.ReadBuffer = tempBuffer;
                clientRpcMessage.Handle(ref context);
            }

            bufferWriter.Dispose();
#if DEVELOPMENT_BUILD || UNITY_EDITOR || UNITY_MP_TOOLS_NET_STATS_MONITOR_ENABLED_IN_RELEASE
            if (__rpc_name_table[GetType()].TryGetValue(rpcMethodId, out var rpcMethodName))
            {
                if (clientRpcParams.Send.TargetClientIds != null)
                {
                    foreach (var targetClientId in clientRpcParams.Send.TargetClientIds)
                    {
                        NetworkManager.NetworkMetrics.TrackRpcSent(
                            targetClientId,
                            NetworkObject,
                            rpcMethodName,
                            __getTypeName(),
                            rpcWriteSize);
                    }
                }
                else if (clientRpcParams.Send.TargetClientIdsNativeArray != null)
                {
                    foreach (var targetClientId in clientRpcParams.Send.TargetClientIdsNativeArray)
                    {
                        NetworkManager.NetworkMetrics.TrackRpcSent(
                            targetClientId,
                            NetworkObject,
                            rpcMethodName,
                            __getTypeName(),
                            rpcWriteSize);
                    }
                }
                else
                {
                    var observerEnumerator = NetworkObject.Observers.GetEnumerator();
                    while (observerEnumerator.MoveNext())
                    {
                        NetworkManager.NetworkMetrics.TrackRpcSent(
                            observerEnumerator.Current,
                            NetworkObject,
                            rpcMethodName,
                            __getTypeName(),
                            rpcWriteSize);
                    }
                }
            }
#endif
        }


#pragma warning disable IDE1006 // disable naming rule violation check
        // RuntimeAccessModifiersILPP will make this `protected`
        internal FastBufferWriter __beginSendRpc(uint rpcMethodId, RpcParams rpcParams, RpcAttribute.RpcAttributeParams attributeParams, SendTo defaultTarget, RpcDelivery rpcDelivery)
#pragma warning restore IDE1006 // restore naming rule violation check
        {
            if (attributeParams.RequireOwnership && !IsOwner)
            {
                throw new RpcException("This RPC can only be sent by its owner.");
            }
            return new FastBufferWriter(k_RpcMessageDefaultSize, Allocator.Temp, k_RpcMessageMaximumSize);
        }

#pragma warning disable IDE1006 // disable naming rule violation check
        // RuntimeAccessModifiersILPP will make this `protected`
        internal void __endSendRpc(ref FastBufferWriter bufferWriter, uint rpcMethodId, RpcParams rpcParams, RpcAttribute.RpcAttributeParams attributeParams, SendTo defaultTarget, RpcDelivery rpcDelivery)
#pragma warning restore IDE1006 // restore naming rule violation check
        {
            var rpcMessage = new RpcMessage
            {
                Metadata = new RpcMetadata
                {
                    NetworkObjectId = NetworkObjectId,
                    NetworkBehaviourId = NetworkBehaviourId,
                    NetworkRpcMethodId = rpcMethodId,
                },
                SenderClientId = NetworkManager.LocalClientId,
                WriteBuffer = bufferWriter
            };

            NetworkDelivery networkDelivery;
            switch (rpcDelivery)
            {
                default:
                case RpcDelivery.Reliable:
                    networkDelivery = NetworkDelivery.ReliableFragmentedSequenced;
                    break;
                case RpcDelivery.Unreliable:
                    if (bufferWriter.Length > NetworkManager.MessageManager.NonFragmentedMessageMaxSize)
                    {
                        throw new OverflowException("RPC parameters are too large for unreliable delivery.");
                    }
                    networkDelivery = NetworkDelivery.Unreliable;
                    break;
            }

            if (rpcParams.Send.Target == null)
            {
                switch (defaultTarget)
                {
                    case SendTo.Everyone:
                        rpcParams.Send.Target = RpcTarget.Everyone;
                        break;
                    case SendTo.Owner:
                        rpcParams.Send.Target = RpcTarget.Owner;
                        break;
                    case SendTo.Server:
                        rpcParams.Send.Target = RpcTarget.Server;
                        break;
                    case SendTo.NotServer:
                        rpcParams.Send.Target = RpcTarget.NotServer;
                        break;
                    case SendTo.NotMe:
                        rpcParams.Send.Target = RpcTarget.NotMe;
                        break;
                    case SendTo.NotOwner:
                        rpcParams.Send.Target = RpcTarget.NotOwner;
                        break;
                    case SendTo.Me:
                        rpcParams.Send.Target = RpcTarget.Me;
                        break;
                    case SendTo.ClientsAndHost:
                        rpcParams.Send.Target = RpcTarget.ClientsAndHost;
                        break;
                    case SendTo.SpecifiedInParams:
                        throw new RpcException("This method requires a runtime-specified send target.");
                }
            }
            else if (defaultTarget != SendTo.SpecifiedInParams && !attributeParams.AllowTargetOverride)
            {
                throw new RpcException("Target override is not allowed for this method.");
            }

            if (rpcParams.Send.LocalDeferMode == LocalDeferMode.Default)
            {
                rpcParams.Send.LocalDeferMode = attributeParams.DeferLocal ? LocalDeferMode.Defer : LocalDeferMode.SendImmediate;
            }

            rpcParams.Send.Target.Send(this, ref rpcMessage, networkDelivery, rpcParams);

            bufferWriter.Dispose();
        }

#pragma warning disable IDE1006 // disable naming rule violation check
        // RuntimeAccessModifiersILPP will make this `protected`
        internal static NativeList<T> __createNativeList<T>() where T : unmanaged
#pragma warning restore IDE1006 // restore naming rule violation check
        {
            return new NativeList<T>(Allocator.Temp);
        }

        internal string GenerateObserverErrorMessage(ClientRpcParams clientRpcParams, ulong targetClientId)
        {
            var containerNameHoldingId = clientRpcParams.Send.TargetClientIds != null ? nameof(ClientRpcParams.Send.TargetClientIds) : nameof(ClientRpcParams.Send.TargetClientIdsNativeArray);
            return $"Sending ClientRpc to non-observer! {containerNameHoldingId} contains clientId {targetClientId} that is not an observer!";
        }

        /// <summary>
        /// Gets the NetworkManager that owns this NetworkBehaviour instance
        ///   See note around `NetworkObject` for how there is a chicken / egg problem when we are not initialized
        /// </summary>
        public NetworkManager NetworkManager
        {
            get
            {
                if (NetworkObject?.NetworkManager != null)
                    return NetworkObject?.NetworkManager;
                {
                }

                return NetworkManager.Singleton;
            }
        }

        // This erroneously tries to simplify these method references but the docs do not pick it up correctly
        // because they try to resolve it on the field rather than the class of the same name.
#pragma warning disable IDE0001
        /// <summary>
        /// Provides access to the various <see cref="SendTo"/> targets at runtime, as well as
        /// runtime-bound targets like <see cref="Unity.Netcode.RpcTarget.Single"/>,
        /// <see cref="Unity.Netcode.RpcTarget.Group(NativeArray{ulong})"/>,
        /// <see cref="Unity.Netcode.RpcTarget.Group(NativeList{ulong})"/>,
        /// <see cref="Unity.Netcode.RpcTarget.Group(ulong[])"/>,
        /// <see cref="Unity.Netcode.RpcTarget.Group{T}(T)"/>, <see cref="Unity.Netcode.RpcTarget.Not(ulong)"/>,
        /// <see cref="Unity.Netcode.RpcTarget.Not(NativeArray{ulong})"/>,
        /// <see cref="Unity.Netcode.RpcTarget.Not(NativeList{ulong})"/>,
        /// <see cref="Unity.Netcode.RpcTarget.Not(ulong[])"/>, and
        /// <see cref="Unity.Netcode.RpcTarget.Not{T}(T)"/>
        /// </summary>
#pragma warning restore IDE0001
        public RpcTarget RpcTarget => NetworkManager.RpcTarget;

        /// <summary>
        /// If a NetworkObject is assigned, it will return whether or not this NetworkObject
        /// is the local player object.  If no NetworkObject is assigned it will always return false.
        /// </summary>
        public bool IsLocalPlayer { get; private set; }

        /// <summary>
        /// Gets if the object is owned by the local player or if the object is the local player object
        /// </summary>
        public bool IsOwner { get; internal set; }

        /// <summary>
        /// Gets if we are executing as server
        /// </summary>
        public bool IsServer { get; private set; }

        /// <summary>
        /// Gets if the server (local or remote) is a host - i.e., also a client
        /// </summary>
        public bool ServerIsHost { get; private set; }

        /// <summary>
        /// Gets if we are executing as client
        /// </summary>
        public bool IsClient { get; private set; }


        /// <summary>
        /// Gets if we are executing as Host, I.E Server and Client
        /// </summary>
        public bool IsHost { get; private set; }

        /// <summary>
        /// Gets Whether or not the object has a owner
        /// </summary>
        public bool IsOwnedByServer { get; internal set; }

        /// <summary>
        /// Used to determine if it is safe to access NetworkObject and NetworkManager from within a NetworkBehaviour component
        /// Primarily useful when checking NetworkObject/NetworkManager properties within FixedUpate
        /// </summary>
        public bool IsSpawned { get; internal set; }

        internal bool IsBehaviourEditable()
        {
            // Only server can MODIFY. So allow modification if network is either not running or we are server
            return !m_NetworkObject ||
                m_NetworkObject.NetworkManager == null ||
                m_NetworkObject.NetworkManager.IsListening == false ||
                m_NetworkObject.NetworkManager.IsServer;
        }

        ///  TODO: this needs an overhaul.  It's expensive, it's ja little naive in how it looks for networkObject in
        ///   its parent and worst, it creates a puzzle if you are a NetworkBehaviour wanting to see if you're live or not
        ///   (e.g. editor code).  All you want to do is find out if NetworkManager is null, but to do that you
        ///   need NetworkObject, but if you try and grab NetworkObject and NetworkManager isn't up you'll get
        ///   the warning below.  This is why IsBehaviourEditable had to be created.  Matt was going to re-do
        ///   how NetworkObject works but it was close to the release and too risky to change
        /// <summary>
        /// Gets the NetworkObject that owns this NetworkBehaviour instance
        /// </summary>
        public NetworkObject NetworkObject
        {
            get
            {
                if (m_NetworkObject != null)
                {
                    return m_NetworkObject;
                }

                try
                {
                    m_NetworkObject = GetComponentInParent<NetworkObject>();
                }
                catch (Exception)
                {
                    return null;
                }

                // ShutdownInProgress check:
                // This prevents an edge case scenario where the NetworkManager is shutting down but user code
                // in Update and/or in FixedUpdate could still be checking NetworkBehaviour.NetworkObject directly (i.e. does it exist?)
                // or NetworkBehaviour.IsSpawned (i.e. to early exit if not spawned) which, in turn, could generate several Warning messages
                // per spawned NetworkObject.  Checking for ShutdownInProgress prevents these unnecessary LogWarning messages.
                // We must check IsSpawned, otherwise a warning will be logged under certain valid conditions (see OnDestroy)
                if (IsSpawned && m_NetworkObject == null && (NetworkManager.Singleton == null || !NetworkManager.Singleton.ShutdownInProgress))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"Could not get {nameof(NetworkObject)} for the {nameof(NetworkBehaviour)}. Are you missing a {nameof(NetworkObject)} component?");
                    }
                }

                return m_NetworkObject;
            }
        }

        /// <summary>
        /// Gets whether or not this NetworkBehaviour instance has a NetworkObject owner.
        /// </summary>
        public bool HasNetworkObject => NetworkObject != null;

        private NetworkObject m_NetworkObject = null;

        /// <summary>
        /// Gets the NetworkId of the NetworkObject that owns this NetworkBehaviour
        /// </summary>
        public ulong NetworkObjectId { get; internal set; }

        /// <summary>
        /// Gets NetworkId for this NetworkBehaviour from the owner NetworkObject
        /// </summary>
        public ushort NetworkBehaviourId { get; internal set; }

        /// <summary>
        /// Internally caches the Id of this behaviour in a NetworkObject. Makes look-up faster
        /// </summary>
        internal ushort NetworkBehaviourIdCache = 0;

        /// <summary>
        /// Returns a the NetworkBehaviour with a given BehaviourId for the current NetworkObject
        /// </summary>
        /// <param name="behaviourId">The behaviourId to return</param>
        /// <returns>Returns NetworkBehaviour with given behaviourId</returns>
        protected NetworkBehaviour GetNetworkBehaviour(ushort behaviourId)
        {
            return NetworkObject.GetNetworkBehaviourAtOrderIndex(behaviourId);
        }

        /// <summary>
        /// Gets the ClientId that owns the NetworkObject
        /// </summary>
        public ulong OwnerClientId { get; internal set; }

        /// <summary>
        /// Updates properties with network session related
        /// dependencies such as a NetworkObject's spawned
        /// state or NetworkManager's session state.
        /// </summary>
        internal void UpdateNetworkProperties()
        {
            // Set NetworkObject dependent properties
            if (NetworkObject != null)
            {
                // Set identification related properties
                NetworkObjectId = NetworkObject.NetworkObjectId;
                IsLocalPlayer = NetworkObject.IsLocalPlayer;

                // This is "OK" because GetNetworkBehaviourOrderIndex uses the order of
                // NetworkObject.ChildNetworkBehaviours which is set once when first
                // accessed.
                NetworkBehaviourId = NetworkObject.GetNetworkBehaviourOrderIndex(this);

                // Set ownership related properties
                IsOwnedByServer = NetworkObject.IsOwnedByServer;
                IsOwner = NetworkObject.IsOwner;
                OwnerClientId = NetworkObject.OwnerClientId;

                // Set NetworkManager dependent properties
                if (NetworkManager != null)
                {
                    IsHost = NetworkManager.IsListening && NetworkManager.IsHost;
                    IsClient = NetworkManager.IsListening && NetworkManager.IsClient;
                    IsServer = NetworkManager.IsListening && NetworkManager.IsServer;
                    ServerIsHost = NetworkManager.IsListening && NetworkManager.ServerIsHost;
                }
            }
            else // Shouldn't happen, but if so then set the properties to their default value;
            {
                OwnerClientId = NetworkObjectId = default;
                IsOwnedByServer = IsOwner = IsHost = IsClient = IsServer = ServerIsHost = default;
                NetworkBehaviourId = default;
            }
        }

        /// <summary>
        /// Gets called after the <see cref="NetworkObject"/> is spawned. No NetworkBehaviours associated with the NetworkObject will have had <see cref="OnNetworkSpawn"/> invoked yet.
        /// A reference to <see cref="NetworkManager"/> is passed in as a parameter to determine the context of execution (IsServer/IsClient)
        /// </summary>
        /// <remarks>
        /// <param name="networkManager">a ref to the <see cref="NetworkManager"/> since this is not yet set on the <see cref="NetworkBehaviour"/></param>
        /// The <see cref="NetworkBehaviour"/> will not have anything assigned to it at this point in time.
        /// Settings like ownership, NetworkBehaviourId, NetworkManager, and most other spawn related properties will not be set.
        /// This can be used to handle things like initializing/instantiating a NetworkVariable or the like.
        /// </remarks>
        protected virtual void OnNetworkPreSpawn(ref NetworkManager networkManager) { }

        /// <summary>
        /// Gets called when the <see cref="NetworkObject"/> gets spawned, message handlers are ready to be registered and the network is setup.
        /// </summary>
        public virtual void OnNetworkSpawn() { }

        /// <summary>
        /// Gets called after the <see cref="NetworkObject"/> is spawned. All NetworkBehaviours associated with the NetworkObject will have had <see cref="OnNetworkSpawn"/> invoked.
        /// </summary>
        /// <remarks>
        /// Will be invoked on each <see cref="NetworkBehaviour"/> associated with the <see cref="NetworkObject"/> being spawned.
        /// All associated <see cref="NetworkBehaviour"/> components will have had <see cref="OnNetworkSpawn"/> invoked on the spawned <see cref="NetworkObject"/>.
        /// </remarks>
        protected virtual void OnNetworkPostSpawn() { }

        /// <summary>
        /// [Client-Side Only]
        /// When a new client joins it is synchronized with all spawned NetworkObjects and scenes loaded for the session joined. At the end of the synchronization process, when all
        /// <see cref="NetworkObject"/>s and scenes (if scene management is enabled) have finished synchronizing, all NetworkBehaviour components associated with spawned <see cref="NetworkObject"/>s
        /// will have this method invoked.
        /// </summary>
        /// <remarks>
        /// This can be used to handle post synchronization actions where you might need to access a different NetworkObject and/or NetworkBehaviour not local to the current NetworkObject context.
        /// This is only invoked on clients during a client-server network topology session.
        /// </remarks>
        protected virtual void OnNetworkSessionSynchronized() { }

        /// <summary>
        /// [Client & Server Side]
        /// When a scene is loaded an in-scene placed NetworkObjects are all spawned, this method is invoked on all of the newly spawned in-scene placed NetworkObjects.
        /// </summary>
        /// <remarks>
        /// This can be used to handle post scene loaded actions for in-scene placed NetworkObjcts where you might need to access a different NetworkObject and/or NetworkBehaviour not local to the current NetworkObject context.
        /// </remarks>
        protected virtual void OnInSceneObjectsSpawned() { }

        /// <summary>
        /// Gets called when the <see cref="NetworkObject"/> gets despawned. Is called both on the server and clients.
        /// </summary>
        public virtual void OnNetworkDespawn() { }

        internal void NetworkPreSpawn(ref NetworkManager networkManager)
        {
            try
            {
                OnNetworkPreSpawn(ref networkManager);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        internal void InternalOnNetworkSpawn()
        {
            IsSpawned = true;
            InitializeVariables();
            UpdateNetworkProperties();
        }

        internal void VisibleOnNetworkSpawn()
        {
            try
            {
                OnNetworkSpawn();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            InitializeVariables();
            if (IsServer)
            {
                // Since we just spawned the object and since user code might have modified their NetworkVariable, esp.
                // NetworkList, we need to mark the object as free of updates.
                // This should happen for all objects on the machine triggering the spawn.
                PostNetworkVariableWrite(true);
            }
        }

        internal void NetworkPostSpawn()
        {
            try
            {
                OnNetworkPostSpawn();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        internal void NetworkSessionSynchronized()
        {
            try
            {
                OnNetworkSessionSynchronized();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        internal void InSceneNetworkObjectsSpawned()
        {
            try
            {
                OnInSceneObjectsSpawned();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        internal void InternalOnNetworkDespawn()
        {
            IsSpawned = false;
            UpdateNetworkProperties();
            try
            {
                OnNetworkDespawn();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Invoked on both the server and the local client of the owner when <see cref="Netcode.NetworkObject"/> ownership is assigned.
        /// </summary>
        public virtual void OnGainedOwnership() { }

        internal void InternalOnGainedOwnership()
        {
            UpdateNetworkProperties();
            OnGainedOwnership();
        }

        /// <summary>
        /// Invoked on all clients, override this method to be notified of any
        /// ownership changes (even if the instance was niether the previous or
        /// newly assigned current owner).
        /// </summary>
        /// <param name="previous">the previous owner</param>
        /// <param name="current">the current owner</param>
        protected virtual void OnOwnershipChanged(ulong previous, ulong current)
        {

        }

        internal void InternalOnOwnershipChanged(ulong previous, ulong current)
        {
            OnOwnershipChanged(previous, current);
        }

        /// <summary>
        /// Invoked on the local client when it loses ownership of the associated <see cref="Netcode.NetworkObject"/>.
        /// This method is also invoked on the server when any client loses ownership.
        /// </summary>
        public virtual void OnLostOwnership() { }

        internal void InternalOnLostOwnership()
        {
            UpdateNetworkProperties();
            OnLostOwnership();
        }

        /// <summary>
        /// Gets called when the parent NetworkObject of this NetworkBehaviour's NetworkObject has changed
        /// </summary>
        /// <param name="parentNetworkObject">the new <see cref="NetworkObject"/> parent</param>
        public virtual void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject) { }

        private bool m_VarInit = false;

        private readonly List<HashSet<int>> m_DeliveryMappedNetworkVariableIndices = new List<HashSet<int>>();
        private readonly List<NetworkDelivery> m_DeliveryTypesForNetworkVariableGroups = new List<NetworkDelivery>();

        // RuntimeAccessModifiersILPP will make this `protected`
        internal readonly List<NetworkVariableBase> NetworkVariableFields = new List<NetworkVariableBase>();

#pragma warning disable IDE1006 // disable naming rule violation check
        // RuntimeAccessModifiersILPP will make this `protected`
        internal virtual void __initializeVariables()
#pragma warning restore IDE1006 // restore naming rule violation check
        {
            // ILPP generates code for all NetworkBehaviour subtypes to initialize each type's network variables.
        }

#pragma warning disable IDE1006 // disable naming rule violation check
        // RuntimeAccessModifiersILPP will make this `protected`
        internal virtual void __initializeRpcs()
#pragma warning restore IDE1006 // restore naming rule violation check
        {
            // ILPP generates code for all NetworkBehaviour subtypes to initialize each type's RPCs.
        }

#pragma warning disable IDE1006 // disable naming rule violation check
        // RuntimeAccessModifiersILPP will make this `protected`
        internal void __registerRpc(uint hash, RpcReceiveHandler handler, string rpcMethodName)
#pragma warning restore IDE1006 // restore naming rule violation check
        {
            __rpc_func_table[GetType()][hash] = handler;
#if DEVELOPMENT_BUILD || UNITY_EDITOR || UNITY_MP_TOOLS_NET_STATS_MONITOR_ENABLED_IN_RELEASE
            __rpc_name_table[GetType()][hash] = rpcMethodName;
#endif
        }

#pragma warning disable IDE1006 // disable naming rule violation check
        // RuntimeAccessModifiersILPP will make this `protected`
        // Using this method here because ILPP doesn't seem to let us do visibility modification on properties.
        internal void __nameNetworkVariable(NetworkVariableBase variable, string varName)
#pragma warning restore IDE1006 // restore naming rule violation check
        {
            variable.Name = varName;
        }

        internal void InitializeVariables()
        {
            if (m_VarInit)
            {
                return;
            }

            m_VarInit = true;

            if (!__rpc_func_table.ContainsKey(GetType()))
            {
                __rpc_func_table[GetType()] = new Dictionary<uint, RpcReceiveHandler>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_MP_TOOLS_NET_STATS_MONITOR_ENABLED_IN_RELEASE
                __rpc_name_table[GetType()] = new Dictionary<uint, string>();
#endif
                __initializeRpcs();
            }
            __initializeVariables();

            {
                // Create index map for delivery types
                var firstLevelIndex = new Dictionary<NetworkDelivery, int>();
                int secondLevelCounter = 0;

                for (int i = 0; i < NetworkVariableFields.Count; i++)
                {
                    var networkDelivery = NetworkVariableBase.Delivery;
                    if (!firstLevelIndex.ContainsKey(networkDelivery))
                    {
                        firstLevelIndex.Add(networkDelivery, secondLevelCounter);
                        m_DeliveryTypesForNetworkVariableGroups.Add(networkDelivery);
                        secondLevelCounter++;
                    }

                    if (firstLevelIndex[networkDelivery] >= m_DeliveryMappedNetworkVariableIndices.Count)
                    {
                        m_DeliveryMappedNetworkVariableIndices.Add(new HashSet<int>());
                    }

                    m_DeliveryMappedNetworkVariableIndices[firstLevelIndex[networkDelivery]].Add(i);
                }
            }
        }

        internal void PreNetworkVariableWrite()
        {
            // reset our "which variables got written" data
            NetworkVariableIndexesToReset.Clear();
            NetworkVariableIndexesToResetSet.Clear();
        }

        internal void PostNetworkVariableWrite(bool forced = false)
        {
            if (forced)
            {
                // Mark every variable as no longer dirty. We just spawned the object and whatever the game code did
                // during OnNetworkSpawn has been sent and needs to be cleared
                for (int i = 0; i < NetworkVariableFields.Count; i++)
                {
                    var networkVariable = NetworkVariableFields[i];
                    if (networkVariable.IsDirty())
                    {
                        if (networkVariable.CanSend())
                        {
                            networkVariable.UpdateLastSentTime();
                            networkVariable.ResetDirty();
                            networkVariable.SetDirty(false);
                        }
                    }
                }
            }
            else
            {
                // mark any variables we wrote as no longer dirty
                for (int i = 0; i < NetworkVariableIndexesToReset.Count; i++)
                {
                    var networkVariable = NetworkVariableFields[NetworkVariableIndexesToReset[i]];
                    if (networkVariable.IsDirty())
                    {
                        if (networkVariable.CanSend())
                        {
                            networkVariable.UpdateLastSentTime();
                            networkVariable.ResetDirty();
                            networkVariable.SetDirty(false);
                        }
                    }
                }
            }
        }

        internal void PreVariableUpdate()
        {
            if (!m_VarInit)
            {
                InitializeVariables();
            }
            PreNetworkVariableWrite();
        }

        internal void VariableUpdate(ulong targetClientId)
        {
            NetworkVariableUpdate(targetClientId, NetworkBehaviourId);
        }

        internal readonly List<int> NetworkVariableIndexesToReset = new List<int>();
        internal readonly HashSet<int> NetworkVariableIndexesToResetSet = new HashSet<int>();

        private void NetworkVariableUpdate(ulong targetClientId, int behaviourIndex)
        {
            if (!CouldHaveDirtyNetworkVariables())
            {
                return;
            }

            for (int j = 0; j < m_DeliveryMappedNetworkVariableIndices.Count; j++)
            {
                var shouldSend = false;
                for (int k = 0; k < NetworkVariableFields.Count; k++)
                {
                    var networkVariable = NetworkVariableFields[k];
                    if (networkVariable.IsDirty() && networkVariable.CanClientRead(targetClientId))
                    {
                        if (networkVariable.CanSend())
                        {
                            shouldSend = true;
                        }
                        break;
                    }
                }

                if (shouldSend)
                {
                    var message = new NetworkVariableDeltaMessage
                    {
                        NetworkObjectId = NetworkObjectId,
                        NetworkBehaviourIndex = NetworkObject.GetNetworkBehaviourOrderIndex(this),
                        NetworkBehaviour = this,
                        TargetClientId = targetClientId,
                        DeliveryMappedNetworkVariableIndex = m_DeliveryMappedNetworkVariableIndices[j]
                    };
                    // TODO: Serialization is where the IsDirty flag gets changed.
                    // Messages don't get sent from the server to itself, so if we're host and sending to ourselves,
                    // we still have to actually serialize the message even though we're not sending it, otherwise
                    // the dirty flag doesn't change properly. These two pieces should be decoupled at some point
                    // so we don't have to do this serialization work if we're not going to use the result.
                    if (IsServer && targetClientId == NetworkManager.ServerClientId)
                    {
                        var tmpWriter = new FastBufferWriter(NetworkManager.MessageManager.NonFragmentedMessageMaxSize, Allocator.Temp, NetworkManager.MessageManager.FragmentedMessageMaxSize);
                        using (tmpWriter)
                        {
                            message.Serialize(tmpWriter, message.Version);
                        }
                    }
                    else
                    {
                        NetworkManager.ConnectionManager.SendMessage(ref message, m_DeliveryTypesForNetworkVariableGroups[j], targetClientId);
                    }
                }
            }
        }

        private bool CouldHaveDirtyNetworkVariables()
        {
            // TODO: There should be a better way by reading one dirty variable vs. 'n'
            for (int i = 0; i < NetworkVariableFields.Count; i++)
            {
                var networkVariable = NetworkVariableFields[i];
                if (networkVariable.IsDirty())
                {
                    if (networkVariable.CanSend())
                    {
                        return true;
                    }
                    // If it's dirty but can't be sent yet, we have to keep monitoring it until one of the
                    // conditions blocking its send changes.
                    NetworkManager.BehaviourUpdater.AddForUpdate(NetworkObject);
                }
            }

            return false;
        }

        internal void MarkVariablesDirty(bool dirty)
        {
            for (int j = 0; j < NetworkVariableFields.Count; j++)
            {
                NetworkVariableFields[j].SetDirty(dirty);
            }
        }

        /// <summary>
        /// Synchronizes by setting only the NetworkVariable field values that the client has permission to read.
        /// Note: This is only invoked when first synchronizing a NetworkBehaviour (i.e. late join or spawned NetworkObject)
        /// </summary>
        /// <remarks>
        /// When NetworkConfig.EnsureNetworkVariableLengthSafety is enabled each NetworkVariable field will be preceded
        /// by the number of bytes written for that specific field.
        /// </remarks>
        internal void WriteNetworkVariableData(FastBufferWriter writer, ulong targetClientId)
        {
            if (NetworkVariableFields.Count == 0)
            {
                return;
            }

            for (int j = 0; j < NetworkVariableFields.Count; j++)
            {

                if (NetworkVariableFields[j].CanClientRead(targetClientId))
                {
                    if (NetworkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                    {
                        var writePos = writer.Position;
                        // Note: This value can't be packed because we don't know how large it will be in advance
                        // we reserve space for it, then write the data, then come back and fill in the space
                        // to pack here, we'd have to write data to a temporary buffer and copy it in - which
                        // isn't worth possibly saving one byte if and only if the data is less than 63 bytes long...
                        // The way we do packing, any value > 63 in a ushort will use the full 2 bytes to represent.
                        writer.WriteValueSafe((ushort)0);
                        var startPos = writer.Position;
                        NetworkVariableFields[j].WriteField(writer);
                        var size = writer.Position - startPos;
                        writer.Seek(writePos);
                        writer.WriteValueSafe((ushort)size);
                        writer.Seek(startPos + size);
                    }
                    else
                    {
                        NetworkVariableFields[j].WriteField(writer);
                    }
                }
                else // Only if EnsureNetworkVariableLengthSafety, otherwise just skip
                if (NetworkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                {
                    writer.WriteValueSafe((ushort)0);
                }
            }
        }

        /// <summary>
        /// Synchronizes by setting only the NetworkVariable field values that the client has permission to read.
        /// Note: This is only invoked when first synchronizing a NetworkBehaviour (i.e. late join or spawned NetworkObject)
        /// </summary>
        /// <remarks>
        /// When NetworkConfig.EnsureNetworkVariableLengthSafety is enabled each NetworkVariable field will be preceded
        /// by the number of bytes written for that specific field.
        /// </remarks>
        internal void SetNetworkVariableData(FastBufferReader reader, ulong clientId)
        {
            if (NetworkVariableFields.Count == 0)
            {
                return;
            }

            for (int j = 0; j < NetworkVariableFields.Count; j++)
            {
                var varSize = (ushort)0;
                var readStartPos = 0;
                if (NetworkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                {
                    reader.ReadValueSafe(out varSize);
                    if (varSize == 0)
                    {
                        continue;
                    }
                    readStartPos = reader.Position;
                }
                else // If the client cannot read this field, then skip it
                if (!NetworkVariableFields[j].CanClientRead(clientId))
                {
                    continue;
                }

                NetworkVariableFields[j].ReadField(reader);

                if (NetworkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                {
                    if (reader.Position > (readStartPos + varSize))
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                        {
                            NetworkLog.LogWarning($"Var data read too far. {reader.Position - (readStartPos + varSize)} bytes.");
                        }

                        reader.Seek(readStartPos + varSize);
                    }
                    else if (reader.Position < (readStartPos + varSize))
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                        {
                            NetworkLog.LogWarning($"Var data read too little. {(readStartPos + varSize) - reader.Position} bytes.");
                        }

                        reader.Seek(readStartPos + varSize);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the local instance of a object with a given NetworkId
        /// </summary>
        /// <param name="networkId"></param>
        /// <returns></returns>
        protected NetworkObject GetNetworkObject(ulong networkId)
        {
            return NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(networkId, out NetworkObject networkObject) ? networkObject : null;
        }

        /// <summary>
        /// Override this method if your derived NetworkBehaviour requires custom synchronization data.
        /// Note: Use of this method is only for the initial client synchronization of NetworkBehaviours
        /// and will increase the payload size for client synchronization and dynamically spawned
        /// <see cref="NetworkObject"/>s.
        /// </summary>
        /// <remarks>
        /// When serializing (writing) this will be invoked during the client synchronization period and
        /// when spawning new NetworkObjects.
        /// When deserializing (reading), this will be invoked prior to the NetworkBehaviour's associated
        /// NetworkObject being spawned.
        /// </remarks>
        /// <param name="serializer">The serializer to use to read and write the data.</param>
        /// <typeparam name="T">
        /// Either BufferSerializerReader or BufferSerializerWriter, depending whether the serializer
        /// is in read mode or write mode.
        /// </typeparam>
        /// <param name="targetClientId">the relative client identifier being synchronized</param>
        protected virtual void OnSynchronize<T>(ref BufferSerializer<T> serializer) where T : IReaderWriter
        {

        }

        public virtual void OnReanticipate(double lastRoundTripTime)
        {

        }

        /// <summary>
        /// The relative client identifier targeted for the serialization of this <see cref="NetworkBehaviour"/> instance.
        /// </summary>
        /// <remarks>
        /// This value will be set prior to <see cref="OnSynchronize{T}(ref BufferSerializer{T})"/> being invoked.
        /// For writing (server-side), this is useful to know which client will receive the serialized data.
        /// For reading (client-side), this will be the <see cref="NetworkManager.LocalClientId"/>.
        /// When synchronization of this instance is complete, this value will be reset to 0
        /// </remarks>
        protected ulong m_TargetIdBeingSynchronized { get; private set; }

        /// <summary>
        /// Internal method that determines if a NetworkBehaviour has additional synchronization data to
        /// be synchronized when first instantiated prior to its associated NetworkObject being spawned.
        /// </summary>
        /// <remarks>
        /// This includes try-catch blocks to recover from exceptions that might occur and continue to
        /// synchronize any remaining NetworkBehaviours.
        /// </remarks>
        /// <returns>true if it wrote synchronization data and false if it did not</returns>
        internal bool Synchronize<T>(ref BufferSerializer<T> serializer, ulong targetClientId = 0) where T : IReaderWriter
        {
            m_TargetIdBeingSynchronized = targetClientId;
            if (serializer.IsWriter)
            {
                // Get the writer to handle seeking and determining how many bytes were written
                var writer = serializer.GetFastBufferWriter();
                // Save our position before we attempt to write anything so we can seek back to it (i.e. error occurs)
                var positionBeforeWrite = writer.Position;
                writer.WriteValueSafe(NetworkBehaviourId);

                // Save our position where we will write the final size being written so we can skip over it in the
                // event an exception occurs when deserializing.
                var sizePosition = writer.Position;
                writer.WriteValueSafe((ushort)0);

                // Save our position before synchronizing to determine how much was written
                var positionBeforeSynchronize = writer.Position;
                var threwException = false;
                try
                {
                    OnSynchronize(ref serializer);
                }
                catch (Exception ex)
                {
                    threwException = true;
                    if (NetworkManager.LogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"{name} threw an exception during synchronization serialization, this {nameof(NetworkBehaviour)} is being skipped and will not be synchronized!");
                        if (NetworkManager.LogLevel == LogLevel.Developer)
                        {
                            NetworkLog.LogError($"{ex.Message}\n {ex.StackTrace}");
                        }
                    }
                }
                var finalPosition = writer.Position;

                // Reset before exiting
                m_TargetIdBeingSynchronized = default;
                // If we wrote nothing then skip writing anything for this NetworkBehaviour
                if (finalPosition == positionBeforeSynchronize || threwException)
                {
                    writer.Seek(positionBeforeWrite);
                    // Truncate back to the size before
                    writer.Truncate();
                    return false;
                }
                else
                {
                    // Write the number of bytes serialized to handle exceptions on the deserialization side
                    var bytesWritten = finalPosition - positionBeforeSynchronize;
                    writer.Seek(sizePosition);
                    writer.WriteValueSafe((ushort)bytesWritten);
                    writer.Seek(finalPosition);
                }
                return true;
            }
            else
            {
                var reader = serializer.GetFastBufferReader();
                // We will always read the expected byte count
                reader.ReadValueSafe(out ushort expectedBytesToRead);

                // Save our position before we begin synchronization deserialization
                var positionBeforeSynchronize = reader.Position;
                var synchronizationError = false;
                try
                {
                    // Invoke synchronization
                    OnSynchronize(ref serializer);
                }
                catch (Exception ex)
                {
                    if (NetworkManager.LogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"{name} threw an exception during synchronization deserialization, this {nameof(NetworkBehaviour)} is being skipped and will not be synchronized!");
                        if (NetworkManager.LogLevel == LogLevel.Developer)
                        {
                            NetworkLog.LogError($"{ex.Message}\n {ex.StackTrace}");
                        }
                    }
                    synchronizationError = true;
                }

                var totalBytesRead = reader.Position - positionBeforeSynchronize;
                if (totalBytesRead != expectedBytesToRead)
                {
                    if (NetworkManager.LogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"{name} read {totalBytesRead} bytes but was expected to read {expectedBytesToRead} bytes during synchronization deserialization! This {nameof(NetworkBehaviour)} is being skipped and will not be synchronized!");
                    }
                    synchronizationError = true;
                }

                // Reset before exiting
                m_TargetIdBeingSynchronized = default;

                // Skip over the entry if deserialization fails
                if (synchronizationError)
                {
                    var skipToPosition = positionBeforeSynchronize + expectedBytesToRead;
                    reader.Seek(skipToPosition);
                    return false;
                }
                return true;
            }
        }


        /// <summary>
        /// Invoked when the <see cref="GameObject"/> the <see cref="NetworkBehaviour"/> is attached to is destroyed.
        /// NOTE:  If you override this, you should invoke this base class version of this
        /// <see cref="OnDestroy"/> method.
        /// </summary>
        public virtual void OnDestroy()
        {
            if (NetworkObject != null && NetworkObject.IsSpawned && IsSpawned)
            {
                // If the associated NetworkObject is still spawned then this
                // NetworkBehaviour will be removed from the NetworkObject's
                // ChildNetworkBehaviours list.
                NetworkObject.OnNetworkBehaviourDestroyed(this);
            }

            // this seems odd to do here, but in fact especially in tests we can find ourselves
            //  here without having called InitializedVariables, which causes problems if any
            //  of those variables use native containers (e.g. NetworkList) as they won't be
            //  registered here and therefore won't be cleaned up.
            //
            // we should study to understand the initialization patterns
            if (!m_VarInit)
            {
                InitializeVariables();
            }


            for (int i = 0; i < NetworkVariableFields.Count; i++)
            {
                NetworkVariableFields[i].Dispose();
            }
        }
    }
}
