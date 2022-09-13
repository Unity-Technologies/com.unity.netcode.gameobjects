using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;
using Unity.Collections;

namespace Unity.Netcode
{
    /// <summary>
    /// The base class to override to write network code. Inherits MonoBehaviour
    /// </summary>
    public abstract class NetworkBehaviour : MonoBehaviour
    {
#pragma warning disable IDE1006 // disable naming rule violation check
        // RuntimeAccessModifiersILPP will make this `protected`
        internal enum __RpcExecStage
        {
            None = 0,
            Server = 1,
            Client = 2
        }

        // NetworkBehaviourILPP will override this in derived classes to return the name of the concrete type
        internal virtual string __getTypeName() => nameof(NetworkBehaviour);

        [NonSerialized]
        // RuntimeAccessModifiersILPP will make this `protected`
        internal __RpcExecStage __rpc_exec_stage = __RpcExecStage.None;
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
                    if (bufferWriter.Length > MessagingSystem.NON_FRAGMENTED_MESSAGE_MAX_SIZE)
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
                    Timestamp = Time.realtimeSinceStartup,
                    SystemOwner = NetworkManager,
                    // header information isn't valid since it's not a real message.
                    // RpcMessage doesn't access this stuff so it's just left empty.
                    Header = new MessageHeader(),
                    SerializedHeaderSize = 0,
                    MessageSize = 0
                };
                serverRpcMessage.ReadBuffer = tempBuffer;
                serverRpcMessage.Handle(ref context);
                rpcWriteSize = tempBuffer.Length;
            }
            else
            {
                rpcWriteSize = NetworkManager.SendMessage(ref serverRpcMessage, networkDelivery, NetworkManager.ServerClientId);
            }

            bufferWriter.Dispose();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (NetworkManager.__rpc_name_table.TryGetValue(rpcMethodId, out var rpcMethodName))
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
                    if (bufferWriter.Length > MessagingSystem.NON_FRAGMENTED_MESSAGE_MAX_SIZE)
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

                rpcWriteSize = NetworkManager.SendMessage(ref clientRpcMessage, networkDelivery, in clientRpcParams.Send.TargetClientIds);
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

                rpcWriteSize = NetworkManager.SendMessage(ref clientRpcMessage, networkDelivery, clientRpcParams.Send.TargetClientIdsNativeArray.Value);
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
                    rpcWriteSize = NetworkManager.MessagingSystem.SendMessage(ref clientRpcMessage, networkDelivery, observerEnumerator.Current);
                }
            }

            // If we are a server/host then we just no op and send to ourself
            if (shouldSendToHost)
            {
                using var tempBuffer = new FastBufferReader(bufferWriter, Allocator.Temp);
                var context = new NetworkContext
                {
                    SenderId = NetworkManager.ServerClientId,
                    Timestamp = Time.realtimeSinceStartup,
                    SystemOwner = NetworkManager,
                    // header information isn't valid since it's not a real message.
                    // RpcMessage doesn't access this stuff so it's just left empty.
                    Header = new MessageHeader(),
                    SerializedHeaderSize = 0,
                    MessageSize = 0
                };
                clientRpcMessage.ReadBuffer = tempBuffer;
                clientRpcMessage.Handle(ref context);
            }

            bufferWriter.Dispose();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (NetworkManager.__rpc_name_table.TryGetValue(rpcMethodId, out var rpcMethodName))
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

        internal string GenerateObserverErrorMessage(ClientRpcParams clientRpcParams, ulong targetClientId)
        {
            var containerNameHoldingId = clientRpcParams.Send.TargetClientIds != null ? nameof(ClientRpcParams.Send.TargetClientIds) : nameof(ClientRpcParams.Send.TargetClientIdsNativeArray);
            return $"Sending ClientRpc to non-observer! {containerNameHoldingId} contains clientId {targetClientId} that is not an observer!";
        }

        /// <summary>
        /// Gets the NetworkManager that owns this NetworkBehaviour instance
        ///   See note around `NetworkObject` for how there is a chicken / egg problem when we are not initialized
        /// </summary>
        public NetworkManager NetworkManager => NetworkObject.NetworkManager;

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
        protected bool IsServer { get; private set; }

        /// <summary>
        /// Gets if we are executing as client
        /// </summary>
        protected bool IsClient { get; private set; }


        /// <summary>
        /// Gets if we are executing as Host, I.E Server and Client
        /// </summary>
        protected bool IsHost { get; private set; }

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

        /// <summary>
        /// Gets the NetworkObject that owns this NetworkBehaviour instance
        ///  TODO: this needs an overhaul.  It's expensive, it's ja little naive in how it looks for networkObject in
        ///   its parent and worst, it creates a puzzle if you are a NetworkBehaviour wanting to see if you're live or not
        ///   (e.g. editor code).  All you want to do is find out if NetworkManager is null, but to do that you
        ///   need NetworkObject, but if you try and grab NetworkObject and NetworkManager isn't up you'll get
        ///   the warning below.  This is why IsBehaviourEditable had to be created.  Matt was going to re-do
        ///   how NetworkObject works but it was close to the release and too risky to change
        ///
        /// </summary>
        public NetworkObject NetworkObject
        {
            get
            {
                if (m_NetworkObject == null)
                {
                    m_NetworkObject = GetComponentInParent<NetworkObject>();
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
                }
            }
            else // Shouldn't happen, but if so then set the properties to their default value;
            {
                OwnerClientId = NetworkObjectId = default;
                IsOwnedByServer = IsOwner = IsHost = IsClient = IsServer = default;
                NetworkBehaviourId = default;
            }
        }

        /// <summary>
        /// Gets called when the <see cref="NetworkObject"/> gets spawned, message handlers are ready to be registered and the network is setup.
        /// </summary>
        public virtual void OnNetworkSpawn() { }

        /// <summary>
        /// Gets called when the <see cref="NetworkObject"/> gets despawned. Is called both on the server and clients.
        /// </summary>
        public virtual void OnNetworkDespawn() { }

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
        /// Gets called when the local client gains ownership of this object
        /// </summary>
        public virtual void OnGainedOwnership() { }

        internal void InternalOnGainedOwnership()
        {
            UpdateNetworkProperties();
            OnGainedOwnership();
        }

        /// <summary>
        /// Gets called when we loose ownership of this object
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
        internal readonly List<NetworkVariableBase> NetworkVariableFields = new List<NetworkVariableBase>();

        private static Dictionary<Type, FieldInfo[]> s_FieldTypes = new Dictionary<Type, FieldInfo[]>();

        private static FieldInfo[] GetFieldInfoForType(Type type)
        {
            if (!s_FieldTypes.ContainsKey(type))
            {
                s_FieldTypes.Add(type, GetFieldInfoForTypeRecursive(type));
            }

            return s_FieldTypes[type];
        }

        private static FieldInfo[] GetFieldInfoForTypeRecursive(Type type, List<FieldInfo> list = null)
        {
            if (list == null)
            {
                list = new List<FieldInfo>();
                list.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
            }
            else
            {
                list.AddRange(type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance));
            }

            if (type.BaseType != null && type.BaseType != typeof(NetworkBehaviour))
            {
                return GetFieldInfoForTypeRecursive(type.BaseType, list);
            }

            return list.OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
        }

        internal void InitializeVariables()
        {
            if (m_VarInit)
            {
                return;
            }

            m_VarInit = true;

            var sortedFields = GetFieldInfoForType(GetType());
            for (int i = 0; i < sortedFields.Length; i++)
            {
                var fieldType = sortedFields[i].FieldType;
                if (fieldType.IsSubclassOf(typeof(NetworkVariableBase)))
                {
                    var instance = (NetworkVariableBase)sortedFields[i].GetValue(this);

                    if (instance == null)
                    {
                        throw new Exception($"{GetType().FullName}.{sortedFields[i].Name} cannot be null. All {nameof(NetworkVariableBase)} instances must be initialized.");
                    }

                    instance.Initialize(this);

                    var instanceNameProperty = fieldType.GetProperty(nameof(NetworkVariableBase.Name));
                    var sanitizedVariableName = sortedFields[i].Name.Replace("<", string.Empty).Replace(">k__BackingField", string.Empty);
                    instanceNameProperty?.SetValue(instance, sanitizedVariableName);

                    NetworkVariableFields.Add(instance);
                }
            }

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
                    NetworkVariableFields[i].ResetDirty();
                }
            }
            else
            {
                // mark any variables we wrote as no longer dirty
                for (int i = 0; i < NetworkVariableIndexesToReset.Count; i++)
                {
                    NetworkVariableFields[NetworkVariableIndexesToReset[i]].ResetDirty();
                }
            }

            MarkVariablesDirty(false);
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
                        shouldSend = true;
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
                        var tmpWriter = new FastBufferWriter(MessagingSystem.NON_FRAGMENTED_MESSAGE_MAX_SIZE, Allocator.Temp, MessagingSystem.FRAGMENTED_MESSAGE_MAX_SIZE);
                        using (tmpWriter)
                        {
                            message.Serialize(tmpWriter);
                        }
                    }
                    else
                    {
                        NetworkManager.SendMessage(ref message, m_DeliveryTypesForNetworkVariableGroups[j], targetClientId);
                    }
                }
            }
        }

        private bool CouldHaveDirtyNetworkVariables()
        {
            // TODO: There should be a better way by reading one dirty variable vs. 'n'
            for (int i = 0; i < NetworkVariableFields.Count; i++)
            {
                if (NetworkVariableFields[i].IsDirty())
                {
                    return true;
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

        internal void WriteNetworkVariableData(FastBufferWriter writer, ulong targetClientId)
        {
            if (NetworkVariableFields.Count == 0)
            {
                return;
            }

            for (int j = 0; j < NetworkVariableFields.Count; j++)
            {
                bool canClientRead = NetworkVariableFields[j].CanClientRead(targetClientId);

                if (canClientRead)
                {
                    var writePos = writer.Position;
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
                    writer.WriteValueSafe((ushort)0);
                }
            }
        }

        internal void SetNetworkVariableData(FastBufferReader reader)
        {
            if (NetworkVariableFields.Count == 0)
            {
                return;
            }

            for (int j = 0; j < NetworkVariableFields.Count; j++)
            {
                reader.ReadValueSafe(out ushort varSize);
                if (varSize == 0)
                {
                    continue;
                }

                var readStartPos = reader.Position;
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
        /// Invoked when the <see cref="GameObject"/> the <see cref="NetworkBehaviour"/> is attached to.
        /// NOTE:  If you override this, you will want to always invoke this base class version of this
        /// <see cref="OnDestroy"/> method!!
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
