using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.Linq;
using System.IO;

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
#pragma warning restore IDE1006 // restore naming rule violation check
        {
            None = 0,
            Server = 1,
            Client = 2
        }

        private static void SetUpdateStage<T>(ref T param) where T : IHasUpdateStage
        {
            if (param.UpdateStage == NetworkUpdateStage.Unset)
            {
                param.UpdateStage = NetworkUpdateLoop.UpdateStage;

                if (param.UpdateStage == NetworkUpdateStage.Initialization)
                {
                    param.UpdateStage = NetworkUpdateStage.EarlyUpdate;
                }
            }
        }

#pragma warning disable IDE1006 // disable naming rule violation check
        // NetworkBehaviourILPP will override this in derived classes to return the name of the concrete type
        internal virtual string __getTypeName() => nameof(NetworkBehaviour);
#pragma warning restore IDE1006 // restore naming rule violation check

#pragma warning disable 414 // disable assigned but its value is never used
#pragma warning disable IDE1006 // disable naming rule violation check
        [NonSerialized]
        // RuntimeAccessModifiersILPP will make this `protected`
        internal __RpcExecStage __rpc_exec_stage = __RpcExecStage.None;
#pragma warning restore 414 // restore assigned but its value is never used
#pragma warning restore IDE1006 // restore naming rule violation check

#pragma warning disable IDE1006 // disable naming rule violation check
        // RuntimeAccessModifiersILPP will make this `protected`
        internal NetworkSerializer __beginSendServerRpc(uint rpcMethodId, ServerRpcParams serverRpcParams, RpcDelivery rpcDelivery)
#pragma warning restore IDE1006 // restore naming rule violation check
        {
            PooledNetworkWriter writer;

            SetUpdateStage(ref serverRpcParams.Send);

            if (serverRpcParams.Send.UpdateStage == NetworkUpdateStage.Initialization)
            {
                throw new NotSupportedException(
                    $"{nameof(NetworkUpdateStage.Initialization)} cannot be used as a target for processing RPCs.");
            }

            var messageQueueContainer = NetworkManager.MessageQueueContainer;
            var networkDelivery = rpcDelivery == RpcDelivery.Reliable ? NetworkDelivery.ReliableSequenced : NetworkDelivery.UnreliableSequenced;

            if (IsHost)
            {
                writer = messageQueueContainer.BeginAddQueueItemToFrame(MessageQueueContainer.MessageType.ServerRpc, Time.realtimeSinceStartup, networkDelivery,
                    NetworkManager.ServerClientId, null, MessageQueueHistoryFrame.QueueFrameType.Inbound, serverRpcParams.Send.UpdateStage);
            }
            else
            {
                writer = messageQueueContainer.BeginAddQueueItemToFrame(MessageQueueContainer.MessageType.ServerRpc, Time.realtimeSinceStartup, networkDelivery,
                    NetworkManager.ServerClientId, null, MessageQueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);

                writer.WriteByte((byte)MessageQueueContainer.MessageType.ServerRpc);
                writer.WriteByte((byte)serverRpcParams.Send.UpdateStage); // NetworkUpdateStage
            }

            writer.WriteUInt64Packed(NetworkObjectId); // NetworkObjectId
            writer.WriteUInt16Packed(NetworkBehaviourId); // NetworkBehaviourId
            writer.WriteUInt32Packed(rpcMethodId); // NetworkRpcMethodId


            return writer.Serializer;
        }

#pragma warning disable IDE1006 // disable naming rule violation check
        // RuntimeAccessModifiersILPP will make this `protected`
        internal void __endSendServerRpc(NetworkSerializer serializer, uint rpcMethodId, ServerRpcParams serverRpcParams, RpcDelivery rpcDelivery)
#pragma warning restore IDE1006 // restore naming rule violation check
        {
            if (serializer == null)
            {
                return;
            }

            SetUpdateStage(ref serverRpcParams.Send);

            var rpcMessageSize = IsHost
                ? NetworkManager.MessageQueueContainer.EndAddQueueItemToFrame(serializer.Writer, MessageQueueHistoryFrame.QueueFrameType.Inbound, serverRpcParams.Send.UpdateStage)
                : NetworkManager.MessageQueueContainer.EndAddQueueItemToFrame(serializer.Writer, MessageQueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (NetworkManager.__rpc_name_table.TryGetValue(rpcMethodId, out var rpcMethodName))
            {
                NetworkManager.NetworkMetrics.TrackRpcSent(
                    NetworkManager.ServerClientId,
                    NetworkObjectId,
                    rpcMethodName,
                    __getTypeName(),
                    rpcMessageSize);
            }
#endif
        }

#pragma warning disable IDE1006 // disable naming rule violation check
        // RuntimeAccessModifiersILPP will make this `protected`
        internal NetworkSerializer __beginSendClientRpc(uint rpcMethodId, ClientRpcParams clientRpcParams, RpcDelivery rpcDelivery)
#pragma warning restore IDE1006 // restore naming rule violation check
        {
            PooledNetworkWriter writer;

            SetUpdateStage(ref clientRpcParams.Send);

            if (clientRpcParams.Send.UpdateStage == NetworkUpdateStage.Initialization)
            {
                throw new NotSupportedException(
                    $"{nameof(NetworkUpdateStage.Initialization)} cannot be used as a target for processing RPCs.");
            }

            // This will start a new queue item entry and will then return the writer to the current frame's stream
            var networkDelivery = rpcDelivery == RpcDelivery.Reliable ? NetworkDelivery.ReliableSequenced : NetworkDelivery.UnreliableSequenced;

            ulong[] clientIds = clientRpcParams.Send.TargetClientIds ?? NetworkManager.ConnectedClientsIds;
            if (clientRpcParams.Send.TargetClientIds != null && clientRpcParams.Send.TargetClientIds.Length == 0)
            {
                clientIds = NetworkManager.ConnectedClientsIds;
            }

            //NOTES ON BELOW CHANGES:
            //The following checks for IsHost and whether the host client id is part of the clients to recieve the RPC
            //Is part of a patch-fix to handle looping back RPCs into the next frame's inbound queue.
            //!!! This code is temporary and will change (soon) when NetworkSerializer can be configured for mutliple NetworkWriters!!!
            var containsServerClientId = clientIds.Contains(NetworkManager.ServerClientId);
            bool addHeader = true;
            var messageQueueContainer = NetworkManager.MessageQueueContainer;
            if (IsHost && containsServerClientId)
            {
                //Always write to the next frame's inbound queue
                writer = messageQueueContainer.BeginAddQueueItemToFrame(MessageQueueContainer.MessageType.ClientRpc, Time.realtimeSinceStartup, networkDelivery,
                    NetworkManager.ServerClientId, null, MessageQueueHistoryFrame.QueueFrameType.Inbound, clientRpcParams.Send.UpdateStage);

                //Handle sending to the other clients, if so the above notes explain why this code is here (a temporary patch-fix)
                if (clientIds.Length > 1)
                {
                    //Set the loopback frame
                    messageQueueContainer.SetLoopBackFrameItem(clientRpcParams.Send.UpdateStage);

                    //Switch to the outbound queue
                    writer = messageQueueContainer.BeginAddQueueItemToFrame(MessageQueueContainer.MessageType.ClientRpc, Time.realtimeSinceStartup, networkDelivery, NetworkObjectId,
                        clientIds, MessageQueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);
                }
                else
                {
                    addHeader = false;
                }
            }
            else
            {
                writer = messageQueueContainer.BeginAddQueueItemToFrame(MessageQueueContainer.MessageType.ClientRpc, Time.realtimeSinceStartup, networkDelivery, NetworkObjectId,
                    clientIds, MessageQueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);
            }

            if (addHeader)
            {
                writer.WriteByte((byte)MessageQueueContainer.MessageType.ClientRpc);
                writer.WriteByte((byte)clientRpcParams.Send.UpdateStage); // NetworkUpdateStage
            }
            writer.WriteUInt64Packed(NetworkObjectId); // NetworkObjectId
            writer.WriteUInt16Packed(NetworkBehaviourId); // NetworkBehaviourId
            writer.WriteUInt32Packed(rpcMethodId); // NetworkRpcMethodId


            return writer.Serializer;
        }

#pragma warning disable IDE1006 // disable naming rule violation check
        // RuntimeAccessModifiersILPP will make this `protected`
        internal void __endSendClientRpc(NetworkSerializer serializer, uint rpcMethodId, ClientRpcParams clientRpcParams, RpcDelivery rpcDelivery)
#pragma warning restore IDE1006 // restore naming rule violation check
        {
            if (serializer == null)
            {
                return;
            }

            SetUpdateStage(ref clientRpcParams.Send);

            if (IsHost)
            {
                ulong[] clientIds = clientRpcParams.Send.TargetClientIds ?? NetworkManager.ConnectedClientsIds;
                if (clientRpcParams.Send.TargetClientIds != null && clientRpcParams.Send.TargetClientIds.Length == 0)
                {
                    clientIds = NetworkManager.ConnectedClientsIds;
                }

                var containsServerClientId = clientIds.Contains(NetworkManager.ServerClientId);
                if (containsServerClientId && clientIds.Length == 1)
                {
                    NetworkManager.MessageQueueContainer.EndAddQueueItemToFrame(serializer.Writer, MessageQueueHistoryFrame.QueueFrameType.Inbound, clientRpcParams.Send.UpdateStage);

                    return;
                }
            }

            var messageSize = NetworkManager.MessageQueueContainer.EndAddQueueItemToFrame(serializer.Writer, MessageQueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (NetworkManager.__rpc_name_table.TryGetValue(rpcMethodId, out var rpcMethodName))
            {
                NetworkManager.NetworkMetrics.TrackRpcSent(
                    NetworkManager.ConnectedClients.Select(x => x.Key).ToArray(),
                    NetworkObjectId,
                    rpcMethodName,
                    __getTypeName(),
                    messageSize);
            }
#endif
        }

        /// <summary>
        /// Gets the NetworkManager that owns this NetworkBehaviour instance
        /// </summary>
        public NetworkManager NetworkManager => NetworkObject.NetworkManager;

        /// <summary>
        /// Gets if the object is the the personal clients player object
        /// </summary>
        public bool IsLocalPlayer => NetworkObject.IsLocalPlayer;

        /// <summary>
        /// Gets if the object is owned by the local player or if the object is the local player object
        /// </summary>
        public bool IsOwner => NetworkObject.IsOwner;

        /// <summary>
        /// Gets if we are executing as server
        /// </summary>
        protected bool IsServer => IsRunning && NetworkManager.IsServer;

        /// <summary>
        /// Gets if we are executing as client
        /// </summary>
        protected bool IsClient => IsRunning && NetworkManager.IsClient;

        /// <summary>
        /// Gets if we are executing as Host, I.E Server and Client
        /// </summary>
        protected bool IsHost => IsRunning && NetworkManager.IsHost;

        private bool IsRunning => NetworkManager != null && NetworkManager.IsListening;

        /// <summary>
        /// Gets Whether or not the object has a owner
        /// </summary>
        public bool IsOwnedByServer => NetworkObject.IsOwnedByServer;

        /// <summary>
        /// Gets the NetworkObject that owns this NetworkBehaviour instance
        /// </summary>
        public NetworkObject NetworkObject
        {
            get
            {
                if (m_NetworkObject == null)
                {
                    m_NetworkObject = GetComponentInParent<NetworkObject>();
                }

                if (m_NetworkObject == null && NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning($"Could not get {nameof(NetworkObject)} for the {nameof(NetworkBehaviour)}. Are you missing a {nameof(NetworkObject)} component?");
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
        public ulong NetworkObjectId => NetworkObject.NetworkObjectId;

        /// <summary>
        /// Gets NetworkId for this NetworkBehaviour from the owner NetworkObject
        /// </summary>
        public ushort NetworkBehaviourId => NetworkObject.GetNetworkBehaviourOrderIndex(this);

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
        public ulong OwnerClientId => NetworkObject.OwnerClientId;

        /// <summary>
        /// Gets called when the <see cref="NetworkObject"/> gets spawned, message handlers are ready to be registered and the network is setup.
        /// </summary>
        public virtual void OnNetworkSpawn() { }

        /// <summary>
        /// Gets called when the <see cref="NetworkObject"/> gets despawned. Is called both on the server and clients.
        /// </summary>
        public virtual void OnNetworkDespawn() { }

        /// <summary>
        /// Used to determine if it is safe to access NetworkObject and NetworkManager from within a NetworkBehaviour component
        /// Primarily useful when checking NetworkObject/NetworkManager properties within FixedUpate
        /// </summary>
        public bool IsSpawned { get; internal set; }

        internal void InternalOnNetworkSpawn()
        {
            IsSpawned = true;
            InitializeVariables();
        }

        internal void InternalOnNetworkDespawn()
        {
            IsSpawned = false;
        }

        /// <summary>
        /// Gets called when the local client gains ownership of this object
        /// </summary>
        public virtual void OnGainedOwnership() { }

        /// <summary>
        /// Gets called when we loose ownership of this object
        /// </summary>
        public virtual void OnLostOwnership() { }

        /// <summary>
        /// Gets called when the parent NetworkObject of this NetworkBehaviour's NetworkObject has changed
        /// </summary>
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

            FieldInfo[] sortedFields = GetFieldInfoForType(GetType());

            for (int i = 0; i < sortedFields.Length; i++)
            {
                Type fieldType = sortedFields[i].FieldType;

                if (fieldType.IsSubclassOf(typeof(NetworkVariableBase)))
                {
                    var instance = (NetworkVariableBase)sortedFields[i].GetValue(this);

                    if (instance == null)
                    {
                        instance = (NetworkVariableBase)Activator.CreateInstance(fieldType, true);
                        sortedFields[i].SetValue(this, instance);
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
            m_NetworkVariableIndexesToReset.Clear();
            m_NetworkVariableIndexesToResetSet.Clear();
        }

        internal void PostNetworkVariableWrite()
        {
            // mark any variables we wrote as no longer dirty
            for (int i = 0; i < m_NetworkVariableIndexesToReset.Count; i++)
            {
                NetworkVariableFields[m_NetworkVariableIndexesToReset[i]].ResetDirty();
            }
        }

        internal void VariableUpdate(ulong clientId)
        {
            if (!m_VarInit)
            {
                InitializeVariables();
            }

            PreNetworkVariableWrite();
            NetworkVariableUpdate(clientId, NetworkBehaviourId);
        }

        private readonly List<int> m_NetworkVariableIndexesToReset = new List<int>();
        private readonly HashSet<int> m_NetworkVariableIndexesToResetSet = new HashSet<int>();

        private void NetworkVariableUpdate(ulong clientId, int behaviourIndex)
        {
            if (!CouldHaveDirtyNetworkVariables())
            {
                return;
            }

            if (NetworkManager.NetworkConfig.UseSnapshotDelta)
            {
                for (int k = 0; k < NetworkVariableFields.Count; k++)
                {
                    NetworkManager.SnapshotSystem.Store(NetworkObjectId, behaviourIndex, k, NetworkVariableFields[k]);
                }
            }

            if (!NetworkManager.NetworkConfig.UseSnapshotDelta)
            {
                for (int j = 0; j < m_DeliveryMappedNetworkVariableIndices.Count; j++)
                {
                    using var buffer = PooledNetworkBuffer.Get();
                    using var writer = PooledNetworkWriter.Get(buffer);
                    // TODO: could skip this if no variables dirty, though obsolete w/ Snapshot
                    writer.WriteUInt64Packed(NetworkObjectId);
                    writer.WriteUInt16Packed(NetworkObject.GetNetworkBehaviourOrderIndex(this));

                    var bufferSizeCapture = new BufferSizeCapture(buffer);

                    var writtenAny = false;
                    for (int k = 0; k < NetworkVariableFields.Count; k++)
                    {
                        if (!m_DeliveryMappedNetworkVariableIndices[j].Contains(k))
                        {
                            // This var does not belong to the currently iterating delivery group.
                            if (NetworkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                            {
                                writer.WriteUInt16Packed(0);
                            }
                            else
                            {
                                writer.WriteBool(false);
                            }

                            continue;
                        }

                        //   if I'm dirty AND a client, write (server always has all permissions)
                        //   if I'm dirty AND the server AND the client can read me, send.
                        bool shouldWrite = NetworkVariableFields[k].ShouldWrite(clientId, IsServer);

                        if (NetworkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                        {
                            if (!shouldWrite)
                            {
                                writer.WriteUInt16Packed(0);
                            }
                        }
                        else
                        {
                            writer.WriteBool(shouldWrite);
                        }

                        if (shouldWrite)
                        {
                            writtenAny = true;

                            if (NetworkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                            {
                                using var varBuffer = PooledNetworkBuffer.Get();
                                NetworkVariableFields[k].WriteDelta(varBuffer);
                                varBuffer.PadBuffer();

                                writer.WriteUInt16Packed((ushort)varBuffer.Length);
                                buffer.CopyFrom(varBuffer);
                            }
                            else
                            {
                                NetworkVariableFields[k].WriteDelta(buffer);
                                buffer.PadBuffer();
                            }

                            if (!m_NetworkVariableIndexesToResetSet.Contains(k))
                            {
                                m_NetworkVariableIndexesToResetSet.Add(k);
                                m_NetworkVariableIndexesToReset.Add(k);
                            }

                            NetworkManager.NetworkMetrics.TrackNetworkVariableDeltaSent(
                                clientId,
                                NetworkObjectId,
                                name,
                                NetworkVariableFields[k].Name,
                                __getTypeName(),
                                bufferSizeCapture.Flush());
                        }
                    }

                    if (writtenAny)
                    {
                        var context = NetworkManager.MessageQueueContainer.EnterInternalCommandContext(
                            MessageQueueContainer.MessageType.NetworkVariableDelta, m_DeliveryTypesForNetworkVariableGroups[j],
                            new[] { clientId }, NetworkUpdateLoop.UpdateStage);
                        if (context != null)
                        {
                            using var nonNullContext = (InternalCommandContext)context;
                            nonNullContext.NetworkWriter.WriteBytes(buffer.GetBuffer(), buffer.Position);
                        }
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

        internal void HandleNetworkVariableDeltas(Stream stream, ulong clientId)
        {
            using var reader = PooledNetworkReader.Get(stream);
            for (int i = 0; i < NetworkVariableFields.Count; i++)
            {
                ushort varSize = 0;

                if (NetworkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                {
                    varSize = reader.ReadUInt16Packed();

                    if (varSize == 0)
                    {
                        continue;
                    }
                }
                else
                {
                    if (!reader.ReadBool())
                    {
                        continue;
                    }
                }

                if (NetworkManager.IsServer)
                {
                    // we are choosing not to fire an exception here, because otherwise a malicious client could use this to crash the server
                    if (NetworkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                        {
                            NetworkLog.LogWarning($"Client wrote to {typeof(NetworkVariable<>).Name} without permission. => {nameof(NetworkObjectId)}: {NetworkObjectId} - {nameof(NetworkObject.GetNetworkBehaviourOrderIndex)}(): {NetworkObject.GetNetworkBehaviourOrderIndex(this)} - VariableIndex: {i}");
                            NetworkLog.LogError($"[{NetworkVariableFields[i].GetType().Name}]");
                        }

                        stream.Position += varSize;
                        continue;
                    }

                    //This client wrote somewhere they are not allowed. This is critical
                    //We can't just skip this field. Because we don't actually know how to dummy read
                    //That is, we don't know how many bytes to skip. Because the interface doesn't have a
                    //Read that gives us the value. Only a Read that applies the value straight away
                    //A dummy read COULD be added to the interface for this situation, but it's just being too nice.
                    //This is after all a developer fault. A critical error should be fine.
                    // - TwoTen
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                    {
                        NetworkLog.LogError($"Client wrote to {typeof(NetworkVariable<>).Name} without permission. No more variables can be read. This is critical. => {nameof(NetworkObjectId)}: {NetworkObjectId} - {nameof(NetworkObject.GetNetworkBehaviourOrderIndex)}(): {NetworkObject.GetNetworkBehaviourOrderIndex(this)} - VariableIndex: {i}");
                        NetworkLog.LogError($"[{NetworkVariableFields[i].GetType().Name}]");
                    }

                    return;
                }
                long readStartPos = stream.Position;

                NetworkVariableFields[i].ReadDelta(stream, NetworkManager.IsServer);
                NetworkManager.NetworkMetrics.TrackNetworkVariableDeltaReceived(
                    clientId,
                    NetworkObjectId,
                    name,
                    NetworkVariableFields[i].Name,
                    __getTypeName(),
                    stream.Length);

                (stream as NetworkBuffer).SkipPadBits();

                if (NetworkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                {
                    if (stream.Position > (readStartPos + varSize))
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                        {
                            NetworkLog.LogWarning(
                                $"Var delta read too far. {stream.Position - (readStartPos + varSize)} bytes. => {nameof(NetworkObjectId)}: {NetworkObjectId} - {nameof(NetworkObject.GetNetworkBehaviourOrderIndex)}(): {NetworkObject.GetNetworkBehaviourOrderIndex(this)} - VariableIndex: {i}");
                        }

                        stream.Position = readStartPos + varSize;
                    }
                    else if (stream.Position < (readStartPos + varSize))
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                        {
                            NetworkLog.LogWarning(
                                $"Var delta read too little. {(readStartPos + varSize) - stream.Position} bytes. => {nameof(NetworkObjectId)}: {NetworkObjectId} - {nameof(NetworkObject.GetNetworkBehaviourOrderIndex)}(): {NetworkObject.GetNetworkBehaviourOrderIndex(this)} - VariableIndex: {i}");
                        }

                        stream.Position = readStartPos + varSize;
                    }
                }
            }
        }

        internal void WriteNetworkVariableData(Stream stream, ulong clientId)
        {
            if (NetworkVariableFields.Count == 0)
            {
                return;
            }

            using var writer = PooledNetworkWriter.Get(stream);
            for (int j = 0; j < NetworkVariableFields.Count; j++)
            {
                bool canClientRead = NetworkVariableFields[j].CanClientRead(clientId);

                if (NetworkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                {
                    if (!canClientRead)
                    {
                        writer.WriteUInt16Packed(0);
                    }
                }
                else
                {
                    writer.WriteBool(canClientRead);
                }

                if (canClientRead)
                {
                    if (NetworkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                    {
                        using var varBuffer = PooledNetworkBuffer.Get();
                        NetworkVariableFields[j].WriteField(varBuffer);
                        varBuffer.PadBuffer();

                        writer.WriteUInt16Packed((ushort)varBuffer.Length);
                        varBuffer.CopyTo(stream);
                    }
                    else
                    {
                        NetworkVariableFields[j].WriteField(stream);
                        writer.WritePadBits();
                    }
                }
            }
        }

        internal void SetNetworkVariableData(Stream stream)
        {
            if (NetworkVariableFields.Count == 0)
            {
                return;
            }

            using var reader = PooledNetworkReader.Get(stream);
            for (int j = 0; j < NetworkVariableFields.Count; j++)
            {
                ushort varSize = 0;

                if (NetworkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                {
                    varSize = reader.ReadUInt16Packed();

                    if (varSize == 0)
                    {
                        continue;
                    }
                }
                else
                {
                    if (!reader.ReadBool())
                    {
                        continue;
                    }
                }

                long readStartPos = stream.Position;

                NetworkVariableFields[j].ReadField(stream);
                reader.SkipPadBits();

                if (NetworkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                {
                    if (stream is NetworkBuffer networkBuffer)
                    {
                        networkBuffer.SkipPadBits();
                    }

                    if (stream.Position > (readStartPos + varSize))
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                        {
                            NetworkLog.LogWarning($"Var data read too far. {stream.Position - (readStartPos + varSize)} bytes.");
                        }

                        stream.Position = readStartPos + varSize;
                    }
                    else if (stream.Position < (readStartPos + varSize))
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                        {
                            NetworkLog.LogWarning($"Var data read too little. {(readStartPos + varSize) - stream.Position} bytes.");
                        }

                        stream.Position = readStartPos + varSize;
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

        public void OnDestroy()
        {
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
