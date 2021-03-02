using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using UnityEngine;
using System.Reflection;
using System.Linq;
using System.IO;
using MLAPI.Configuration;
using MLAPI.Logging;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using MLAPI.Profiling;
using MLAPI.Reflection;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using MLAPI.Spawning;
using MLAPI.Transports;
using Unity.Profiling;

namespace MLAPI
{
    /// <summary>
    /// The base class to override to write network code. Inherits MonoBehaviour
    /// </summary>
    public abstract class NetworkBehaviour : MonoBehaviour
    {
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#if UNITY_2020_2_OR_NEWER
        // RuntimeAccessModifiersILPP will make this `protected`
        internal enum __NExec
#else
        [Obsolete("Please do not use, will no longer be exposed in the future versions (framework internal)")]
        public enum __NExec
#endif
        {
            None = 0,
            Server = 1,
            Client = 2
        }

#pragma warning disable 414
        [NonSerialized]
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
#if UNITY_2020_2_OR_NEWER
        // RuntimeAccessModifiersILPP will make this `protected`
        internal __NExec __nexec = __NExec.None;
#else
        [Obsolete("Please do not use, will no longer be exposed in the future versions (framework internal)")]
        public __NExec __nexec = __NExec.None;
#endif
#pragma warning restore 414

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#if UNITY_2020_2_OR_NEWER
        // RuntimeAccessModifiersILPP will make this `protected`
        internal NetworkSerializer __beginSendServerRpc(ServerRpcParams serverRpcParams, RpcDelivery rpcDelivery)
#else
        [Obsolete("Please do not use, will no longer be exposed in the future versions (framework internal)")]
        public NetworkSerializer __beginSendServerRpc(ServerRpcParams serverRpcParams, RpcDelivery rpcDelivery)
#endif
        {
            PooledNetworkWriter writer;

            var rpcQueueContainer = NetworkManager.Singleton.rpcQueueContainer;
            var isUsingBatching = rpcQueueContainer.IsUsingBatching();
            var transportChannel = rpcDelivery == RpcDelivery.Reliable ? NetworkChannel.ReliableRpc : NetworkChannel.UnreliableRpc;

            if (IsHost)
            {
                writer = rpcQueueContainer.BeginAddQueueItemToFrame(RpcQueueContainer.QueueItemType.ServerRpc, Time.realtimeSinceStartup, transportChannel,
                    NetworkManager.Singleton.ServerClientId, null, QueueHistoryFrame.QueueFrameType.Inbound, serverRpcParams.Send.UpdateStage);

                if (!isUsingBatching)
                {
                    writer.WriteByte(NetworkConstants.k_SERVER_RPC); // MessageType
                }
            }
            else
            {
                writer = rpcQueueContainer.BeginAddQueueItemToFrame(RpcQueueContainer.QueueItemType.ServerRpc, Time.realtimeSinceStartup, transportChannel,
                    NetworkManager.Singleton.ServerClientId, null, QueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);
                if (!isUsingBatching)
                {
                    writer.WriteByte(NetworkConstants.k_SERVER_RPC); // MessageType
                }
            }

            writer.WriteUInt64Packed(NetworkId); // NetworkObjectId
            writer.WriteUInt16Packed(GetNetworkBehaviourId()); // NetworkBehaviourId
            writer.WriteByte((byte)serverRpcParams.Send.UpdateStage); // NetworkUpdateStage

            return writer.Serializer;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#if UNITY_2020_2_OR_NEWER
        // RuntimeAccessModifiersILPP will make this `protected`
        internal void __endSendServerRpc(NetworkSerializer serializer, ServerRpcParams serverRpcParams, RpcDelivery rpcDelivery)
#else
        [Obsolete("Please do not use, will no longer be exposed in the future versions (framework internal)")]
        public void __endSendServerRpc(NetworkSerializer serializer, ServerRpcParams serverRpcParams, RpcDelivery rpcDelivery)
#endif
        {
            if (serializer == null) return;

            var rpcQueueContainer = NetworkManager.Singleton.rpcQueueContainer;
            if (IsHost)
            {
                rpcQueueContainer.EndAddQueueItemToFrame(serializer.Writer, QueueHistoryFrame.QueueFrameType.Inbound, serverRpcParams.Send.UpdateStage);
            }
            else
            {
                rpcQueueContainer.EndAddQueueItemToFrame(serializer.Writer, QueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);
            }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#if UNITY_2020_2_OR_NEWER
        // RuntimeAccessModifiersILPP will make this `protected`
        internal NetworkSerializer __beginSendClientRpc(ClientRpcParams clientRpcParams, RpcDelivery rpcDelivery)
#else
        [Obsolete("Please do not use, will no longer be exposed in the future versions (framework internal)")]
        public NetworkSerializer __beginSendClientRpc(ClientRpcParams clientRpcParams, RpcDelivery rpcDelivery)
#endif
        {
            PooledNetworkWriter writer;

            // This will start a new queue item entry and will then return the writer to the current frame's stream
            var rpcQueueContainer = NetworkManager.Singleton.rpcQueueContainer;
            var isUsingBatching = rpcQueueContainer.IsUsingBatching();
            var transportChannel = rpcDelivery == RpcDelivery.Reliable ? NetworkChannel.ReliableRpc : NetworkChannel.UnreliableRpc;

            ulong[] ClientIds = clientRpcParams.Send.TargetClientIds ?? NetworkManager.Singleton.ConnectedClientsList.Select(c => c.ClientId).ToArray();
            if (clientRpcParams.Send.TargetClientIds != null && clientRpcParams.Send.TargetClientIds.Length == 0)
            {
                ClientIds = NetworkManager.Singleton.ConnectedClientsList.Select(c => c.ClientId).ToArray();
            }

            //NOTES ON BELOW CHANGES:
            //The following checks for IsHost and whether the host client id is part of the clients to recieve the RPC
            //Is part of a patch-fix to handle looping back RPCs into the next frame's inbound queue.
            //!!! This code is temporary and will change (soon) when NetworkSerializer can be configured for mutliple NetworkWriters!!!
            var ContainsServerClientId = ClientIds.Contains(NetworkManager.Singleton.ServerClientId);
            if (IsHost && ContainsServerClientId)
            {
                //Always write to the next frame's inbound queue
                writer = rpcQueueContainer.BeginAddQueueItemToFrame(RpcQueueContainer.QueueItemType.ClientRpc, Time.realtimeSinceStartup, transportChannel,
                    NetworkManager.Singleton.ServerClientId, null, QueueHistoryFrame.QueueFrameType.Inbound, clientRpcParams.Send.UpdateStage);

                //Handle sending to the other clients, if so the above notes explain why this code is here (a temporary patch-fix)
                if (ClientIds.Length > 1)
                {
                    //Set the loopback frame
                    rpcQueueContainer.SetLoopBackFrameItem(clientRpcParams.Send.UpdateStage);

                    //Switch to the outbound queue
                    writer = rpcQueueContainer.BeginAddQueueItemToFrame(RpcQueueContainer.QueueItemType.ClientRpc, Time.realtimeSinceStartup, NetworkChannel.ReliableRpc, NetworkId,
                        ClientIds, QueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);

                    if (!isUsingBatching)
                    {
                        writer.WriteByte(NetworkConstants.k_CLIENT_RPC); // MessageType
                    }
                }
                else
                {
                    if (!isUsingBatching)
                    {
                        writer.WriteByte(NetworkConstants.k_CLIENT_RPC); // MessageType
                    }
                }
            }
            else
            {
                writer = rpcQueueContainer.BeginAddQueueItemToFrame(RpcQueueContainer.QueueItemType.ClientRpc, Time.realtimeSinceStartup, transportChannel, NetworkId,
                    ClientIds, QueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);

                if (!isUsingBatching)
                {
                    writer.WriteByte(NetworkConstants.k_CLIENT_RPC); // MessageType
                }
            }

            writer.WriteUInt64Packed(NetworkId); // NetworkObjectId
            writer.WriteUInt16Packed(GetNetworkBehaviourId()); // NetworkBehaviourId
            writer.WriteByte((byte)clientRpcParams.Send.UpdateStage); // NetworkUpdateStage

            return writer.Serializer;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#if UNITY_2020_2_OR_NEWER
        // RuntimeAccessModifiersILPP will make this `protected`
        internal void __endSendClientRpc(NetworkSerializer serializer, ClientRpcParams clientRpcParams, RpcDelivery rpcDelivery)
#else
        [Obsolete("Please do not use, will no longer be exposed in the future versions (framework internal)")]
        public void __endSendClientRpc(NetworkSerializer serializer, ClientRpcParams clientRpcParams, RpcDelivery rpcDelivery)
#endif
        {
            if (serializer == null) return;

            var rpcQueueContainer = NetworkManager.Singleton.rpcQueueContainer;

            if (IsHost)
            {
                ulong[] ClientIds = clientRpcParams.Send.TargetClientIds ?? NetworkManager.Singleton.ConnectedClientsList.Select(c => c.ClientId).ToArray();
                if (clientRpcParams.Send.TargetClientIds != null && clientRpcParams.Send.TargetClientIds.Length == 0)
                {
                    ClientIds = NetworkManager.Singleton.ConnectedClientsList.Select(c => c.ClientId).ToArray();
                }

                var ContainsServerClientId = ClientIds.Contains(NetworkManager.Singleton.ServerClientId);
                if (ContainsServerClientId && ClientIds.Length == 1)
                {
                    rpcQueueContainer.EndAddQueueItemToFrame(serializer.Writer, QueueHistoryFrame.QueueFrameType.Inbound, clientRpcParams.Send.UpdateStage);
                    return;
                }
            }

            rpcQueueContainer.EndAddQueueItemToFrame(serializer.Writer, QueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);
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
        protected static bool IsServer => IsRunning && NetworkManager.Singleton.IsServer;

        /// <summary>
        /// Gets if we are executing as client
        /// </summary>
        protected bool IsClient => IsRunning && NetworkManager.Singleton.IsClient;

        /// <summary>
        /// Gets if we are executing as Host, I.E Server and Client
        /// </summary>
        protected bool IsHost => IsRunning && NetworkManager.Singleton.IsHost;

        private static bool IsRunning => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

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
                if (ReferenceEquals(m_NetworkObject, null))
                {
                    m_NetworkObject = GetComponentInParent<NetworkObject>();
                }

                if (ReferenceEquals(m_NetworkObject, null))
                {
                    throw new NullReferenceException($"Could not get {nameof(NetworkObject)} for the {nameof(NetworkBehaviour)}. Are you missing a {nameof(NetworkObject)} component?");
                }

                return m_NetworkObject;
            }
        }

        /// <summary>
        /// Gets whether or not this NetworkBehaviour instance has a NetworkObject owner.
        /// </summary>
        public bool HasNetworkObject
        {
            get
            {
                if (ReferenceEquals(m_NetworkObject, null))
                {
                    m_NetworkObject = GetComponentInParent<NetworkObject>();
                }

                return !ReferenceEquals(m_NetworkObject, null);
            }
        }

        private NetworkObject m_NetworkObject = null;

        /// <summary>
        /// Gets the NetworkId of the NetworkObject that owns the NetworkBehaviour instance
        /// </summary>
        public ulong NetworkId => NetworkObject.NetworkId;

        /// <summary>
        /// Gets the clientId that owns the NetworkObject
        /// </summary>
        public ulong OwnerClientId => NetworkObject.OwnerClientId;

        internal bool networkStartInvoked = false;
        internal bool internalNetworkStartInvoked = false;

        /// <summary>
        /// Stores the network tick at the NetworkBehaviourUpdate time
        /// This allows sending NetworkVariables not more often than once per network tick, regardless of the update rate
        /// </summary>
        public static ushort currentTick { get; private set; }

        /// <summary>
        /// Gets called when message handlers are ready to be registered and the network is setup
        /// </summary>
        public virtual void NetworkStart() { }

        /// <summary>
        /// Gets called when message handlers are ready to be registered and the network is setup. Provides a Payload if it was provided
        /// </summary>
        /// <param name="stream">The stream containing the spawn payload</param>
        public virtual void NetworkStart(Stream stream)
        {
            NetworkStart();
        }

        internal void InternalNetworkStart()
        {
            InitializeVariables();
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
        /// Gets BehaviourId for this NetworkBehaviour on this NetworkObject
        /// </summary>
        /// <returns>The behaviourId for the current NetworkBehaviour</returns>
        public ushort GetNetworkBehaviourId()
        {
            return NetworkObject.GetOrderIndex(this);
        }

        /// <summary>
        /// Returns a the NetworkBehaviour with a given BehaviourId for the current NetworkObject
        /// </summary>
        /// <param name="id">The behaviourId to return</param>
        /// <returns>Returns NetworkBehaviour with given behaviourId</returns>
        protected NetworkBehaviour GetNetworkBehaviour(ushort id)
        {
            return NetworkObject.GetNetworkBehaviourAtOrderIndex(id);
        }

        #region NetworkVariable

        private bool varInit = false;

        private readonly List<HashSet<int>> channelMappedNetworkVariableIndexes = new List<HashSet<int>>();
        private readonly List<NetworkChannel> channelsForNetworkVariableGroups = new List<NetworkChannel>();
        internal readonly List<INetworkVariable> networkVariableFields = new List<INetworkVariable>();

        private static HashSet<NetworkObject> touched = new HashSet<NetworkObject>();
        private static readonly Dictionary<Type, FieldInfo[]> fieldTypes = new Dictionary<Type, FieldInfo[]>();

        private static FieldInfo[] GetFieldInfoForType(Type type)
        {
            if (!fieldTypes.ContainsKey(type))
            {
                fieldTypes.Add(type, GetFieldInfoForTypeRecursive(type));
            }

            return fieldTypes[type];
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
            if (varInit) return;
            varInit = true;

            FieldInfo[] sortedFields = GetFieldInfoForType(GetType());

            for (int i = 0; i < sortedFields.Length; i++)
            {
                Type fieldType = sortedFields[i].FieldType;

                if (fieldType.HasInterface(typeof(INetworkVariable)))
                {
                    INetworkVariable instance = (INetworkVariable)sortedFields[i].GetValue(this);

                    if (instance == null)
                    {
                        instance = (INetworkVariable)Activator.CreateInstance(fieldType, true);
                        sortedFields[i].SetValue(this, instance);
                    }

                    instance.SetNetworkBehaviour(this);
                    networkVariableFields.Add(instance);
                }
            }

            {
                // Create index map for channels
                Dictionary<NetworkChannel, int> firstLevelIndex = new Dictionary<NetworkChannel, int>();
                int secondLevelCounter = 0;

                for (int i = 0; i < networkVariableFields.Count; i++)
                {
                    NetworkChannel networkChannel = networkVariableFields[i].GetChannel();

                    if (!firstLevelIndex.ContainsKey(networkChannel))
                    {
                        firstLevelIndex.Add(networkChannel, secondLevelCounter);
                        channelsForNetworkVariableGroups.Add(networkChannel);
                        secondLevelCounter++;
                    }

                    if (firstLevelIndex[networkChannel] >= channelMappedNetworkVariableIndexes.Count)
                    {
                        channelMappedNetworkVariableIndexes.Add(new HashSet<int>());
                    }

                    channelMappedNetworkVariableIndexes[firstLevelIndex[networkChannel]].Add(i);
                }
            }
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        public static ProfilerMarker s_NetworkBehaviourUpdate = new ProfilerMarker(nameof(NetworkBehaviourUpdate));
#endif

        internal static void NetworkBehaviourUpdate()
        {
            // Do not execute NetworkBehaviourUpdate more than once per network tick
            ushort tick = NetworkManager.Singleton.networkTickSystem.GetTick();
            if (tick == currentTick)
            {
                return;
            }

            currentTick = tick;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_NetworkBehaviourUpdate.Begin();
#endif
            try
            {
                if (IsServer)
                {
                    touched.Clear();
                    for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
                    {
                        var client = NetworkManager.Singleton.ConnectedClientsList[i];
                        var spawnedObjs = NetworkSpawnManager.SpawnedObjectsList;
                        touched.UnionWith(spawnedObjs);
                        foreach (var sobj in spawnedObjs)
                        {
                            // Sync just the variables for just the objects this client sees
                            for (int k = 0; k < sobj.ChildNetworkBehaviours.Count; k++)
                            {
                                sobj.ChildNetworkBehaviours[k].VariableUpdate(client.ClientId);
                            }
                        }
                    }

                    // Now, reset all the no-longer-dirty variables
                    foreach (var sobj in touched)
                    {
                        for (int k = 0; k < sobj.ChildNetworkBehaviours.Count; k++)
                        {
                            sobj.ChildNetworkBehaviours[k].PostNetworkVariableWrite();
                        }
                    }
                }
                else
                {
                    // when client updates the sever, it tells it about all its objects
                    foreach (var sobj in NetworkSpawnManager.SpawnedObjectsList)
                    {
                        for (int k = 0; k < sobj.ChildNetworkBehaviours.Count; k++)
                        {
                            sobj.ChildNetworkBehaviours[k].VariableUpdate(NetworkManager.Singleton.ServerClientId);
                        }
                    }

                    // Now, reset all the no-longer-dirty variables
                    foreach (var sobj in NetworkSpawnManager.SpawnedObjectsList)
                    {
                        for (int k = 0; k < sobj.ChildNetworkBehaviours.Count; k++)
                        {
                            sobj.ChildNetworkBehaviours[k].PostNetworkVariableWrite();
                        }
                    }
                }
            }
            finally
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                s_NetworkBehaviourUpdate.End();
#endif
            }
        }


        internal void PreNetworkVariableWrite()
        {
            // reset our "which variables got written" data
            networkVariableIndexesToReset.Clear();
            networkVariableIndexesToResetSet.Clear();
        }

        internal void PostNetworkVariableWrite()
        {
            // mark any variables we wrote as no longer dirty
            for (int i = 0; i < networkVariableIndexesToReset.Count; i++)
            {
                networkVariableFields[networkVariableIndexesToReset[i]].ResetDirty();
            }
        }

        internal void VariableUpdate(ulong clientId)
        {
            if (!varInit) InitializeVariables();

            PreNetworkVariableWrite();
            NetworkVariableUpdate(clientId);
        }

        private readonly List<int> networkVariableIndexesToReset = new List<int>();
        private readonly HashSet<int> networkVariableIndexesToResetSet = new HashSet<int>();

        private void NetworkVariableUpdate(ulong clientId)
        {
            if (!CouldHaveDirtyNetworkVariables()) return;

            for (int j = 0; j < channelMappedNetworkVariableIndexes.Count; j++)
            {
                using (PooledNetworkBuffer buffer = PooledNetworkBuffer.Get())
                {
                    using (PooledNetworkWriter writer = PooledNetworkWriter.Get(buffer))
                    {
                        writer.WriteUInt64Packed(NetworkId);
                        writer.WriteUInt16Packed(NetworkObject.GetOrderIndex(this));

                        // Write the current tick frame
                        // todo: this is currently done per channel, per tick. The snapshot system might improve on this
                        writer.WriteUInt16Packed(currentTick);

                        bool writtenAny = false;
                        for (int k = 0; k < networkVariableFields.Count; k++)
                        {
                            if (!channelMappedNetworkVariableIndexes[j].Contains(k))
                            {
                                // This var does not belong to the currently iterating channel group.
                                if (NetworkManager.Singleton.NetworkConfig.EnsureNetworkVariableLengthSafety)
                                {
                                    writer.WriteUInt16Packed(0);
                                }
                                else
                                {
                                    writer.WriteBool(false);
                                }

                                continue;
                            }

                            bool isDirty = networkVariableFields[k].IsDirty(); // cache this here. You never know what operations users will do in the dirty methods

                            //   if I'm dirty AND a client, write (server always has all permissions)
                            //   if I'm dirty AND the server AND the client can read me, send.
                            bool shouldWrite = isDirty && (!IsServer || networkVariableFields[k].CanClientRead(clientId));

                            if (NetworkManager.Singleton.NetworkConfig.EnsureNetworkVariableLengthSafety)
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

                                if (NetworkManager.Singleton.NetworkConfig.EnsureNetworkVariableLengthSafety)
                                {
                                    using (PooledNetworkBuffer varBuffer = PooledNetworkBuffer.Get())
                                    {
                                        networkVariableFields[k].WriteDelta(varBuffer);
                                        varBuffer.PadStream();

                                        writer.WriteUInt16Packed((ushort)varBuffer.Length);
                                        buffer.CopyFrom(varBuffer);
                                    }
                                }
                                else
                                {
                                    networkVariableFields[k].WriteDelta(buffer);
                                }

                                if (!networkVariableIndexesToResetSet.Contains(k))
                                {
                                    networkVariableIndexesToResetSet.Add(k);
                                    networkVariableIndexesToReset.Add(k);
                                }
                            }
                        }

                        if (writtenAny)
                        {
                            InternalMessageSender.Send(clientId, NetworkConstants.k_NETWORK_VARIABLE_DELTA, channelsForNetworkVariableGroups[j], buffer);
                        }
                    }
                }
            }
        }

        private bool CouldHaveDirtyNetworkVariables()
        {
            // TODO: There should be a better way by reading one dirty variable vs. 'n'
            for (int i = 0; i < networkVariableFields.Count; i++)
            {
                if (networkVariableFields[i].IsDirty())
                    return true;
            }

            return false;
        }

        internal static void HandleNetworkVariableDeltas(List<INetworkVariable> networkVariableList, Stream stream, ulong clientId, NetworkBehaviour logInstance)
        {
            using (PooledNetworkReader reader = PooledNetworkReader.Get(stream))
            {
                // read the remote network tick at which this variable was written.
                ushort remoteTick = reader.ReadUInt16Packed();

                for (int i = 0; i < networkVariableList.Count; i++)
                {
                    ushort varSize = 0;

                    if (NetworkManager.Singleton.NetworkConfig.EnsureNetworkVariableLengthSafety)
                    {
                        varSize = reader.ReadUInt16Packed();

                        if (varSize == 0) continue;
                    }
                    else
                    {
                        if (!reader.ReadBool()) continue;
                    }

                    if (IsServer && !networkVariableList[i].CanClientWrite(clientId))
                    {
                        if (NetworkManager.Singleton.NetworkConfig.EnsureNetworkVariableLengthSafety)
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                            {
                                NetworkLog.LogWarning("Client wrote to NetworkVariable without permission. " + (logInstance != null ? ("NetworkId: " + logInstance.NetworkId + " BehaviourIndex: " + logInstance.NetworkObject.GetOrderIndex(logInstance) + " VariableIndex: " + i) : string.Empty));
                                NetworkLog.LogError("[" + networkVariableList[i].GetType().Name + "]");
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
                            NetworkLog.LogError("Client wrote to NetworkVariable without permission. No more variables can be read. This is critical. " + (logInstance != null ? ("NetworkId: " + logInstance.NetworkId + " BehaviourIndex: " + logInstance.NetworkObject.GetOrderIndex(logInstance) + " VariableIndex: " + i) : string.Empty));
                            NetworkLog.LogError("[" + networkVariableList[i].GetType().Name + "]");
                        }

                        return;
                    }

                    // read the local network tick at which this variable was written.
                    // if this var was updated from our machine, this local tick will be locally valid
                    ushort localTick = reader.ReadUInt16Packed();

                    long readStartPos = stream.Position;

                    networkVariableList[i].ReadDelta(stream, IsServer, localTick, remoteTick);
                    PerformanceDataManager.Increment(ProfilerConstants.NumberNetworkVarsReceived);

                    ProfilerStatManager.networkVarsRcvd.Record();

                    if (NetworkManager.Singleton.NetworkConfig.EnsureNetworkVariableLengthSafety)
                    {
                        (stream as NetworkBuffer).SkipPadBits();

                        if (stream.Position > (readStartPos + varSize))
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Var delta read too far. " + (stream.Position - (readStartPos + varSize)) + " bytes." + (logInstance != null ? ("NetworkId: " + logInstance.NetworkId + " BehaviourIndex: " + logInstance.NetworkObject.GetOrderIndex(logInstance) + " VariableIndex: " + i) : string.Empty));
                            stream.Position = readStartPos + varSize;
                        }
                        else if (stream.Position < (readStartPos + varSize))
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Var delta read too little. " + ((readStartPos + varSize) - stream.Position) + " bytes." + (logInstance != null ? ("NetworkId: " + logInstance.NetworkId + " BehaviourIndex: " + logInstance.NetworkObject.GetOrderIndex(logInstance) + " VariableIndex: " + i) : string.Empty));
                            stream.Position = readStartPos + varSize;
                        }
                    }
                }
            }
        }

        internal static void HandleNetworkVariableUpdate(List<INetworkVariable> networkVariableList, Stream stream, ulong clientId, NetworkBehaviour logInstance)
        {
            using (PooledNetworkReader reader = PooledNetworkReader.Get(stream))
            {
                for (int i = 0; i < networkVariableList.Count; i++)
                {
                    ushort varSize = 0;

                    if (NetworkManager.Singleton.NetworkConfig.EnsureNetworkVariableLengthSafety)
                    {
                        varSize = reader.ReadUInt16Packed();

                        if (varSize == 0) continue;
                    }
                    else
                    {
                        if (!reader.ReadBool()) continue;
                    }

                    if (IsServer && !networkVariableList[i].CanClientWrite(clientId))
                    {
                        if (NetworkManager.Singleton.NetworkConfig.EnsureNetworkVariableLengthSafety)
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Client wrote to NetworkVariable without permission. " + (logInstance != null ? ("NetworkId: " + logInstance.NetworkId + " BehaviourIndex: " + logInstance.NetworkObject.GetOrderIndex(logInstance) + " VariableIndex: " + i) : string.Empty));
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
                            NetworkLog.LogError("Client wrote to NetworkVariable without permission. No more variables can be read. This is critical. " + (logInstance != null ? ("NetworkId: " + logInstance.NetworkId + " BehaviourIndex: " + logInstance.NetworkObject.GetOrderIndex(logInstance) + " VariableIndex: " + i) : string.Empty));
                        }

                        return;
                    }

                    long readStartPos = stream.Position;

                    networkVariableList[i].ReadField(stream, NetworkTickSystem.k_NoTick, NetworkTickSystem.k_NoTick);
                    PerformanceDataManager.Increment(ProfilerConstants.NumberNetworkVarsReceived);

                    ProfilerStatManager.networkVarsRcvd.Record();

                    if (NetworkManager.Singleton.NetworkConfig.EnsureNetworkVariableLengthSafety)
                    {
                        if (stream is NetworkBuffer networkBuffer)
                        {
                            networkBuffer.SkipPadBits();
                        }

                        if (stream.Position > (readStartPos + varSize))
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Var update read too far. " + (stream.Position - (readStartPos + varSize)) + " bytes." + (logInstance != null ? ("NetworkId: " + logInstance.NetworkId + " BehaviourIndex: " + logInstance.NetworkObject.GetOrderIndex(logInstance) + " VariableIndex: " + i) : string.Empty));
                            stream.Position = readStartPos + varSize;
                        }
                        else if (stream.Position < (readStartPos + varSize))
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Var update read too little. " + ((readStartPos + varSize) - stream.Position) + " bytes." + (logInstance != null ? ("NetworkId: " + logInstance.NetworkId + " BehaviourIndex: " + logInstance.NetworkObject.GetOrderIndex(logInstance) + " VariableIndex: " + i) : string.Empty));
                            stream.Position = readStartPos + varSize;
                        }
                    }
                }
            }
        }


        internal static void WriteNetworkVariableData(List<INetworkVariable> networkVariableList, Stream stream, ulong clientId)
        {
            if (networkVariableList.Count == 0) return;

            using (PooledNetworkWriter writer = PooledNetworkWriter.Get(stream))
            {
                for (int j = 0; j < networkVariableList.Count; j++)
                {
                    bool canClientRead = networkVariableList[j].CanClientRead(clientId);

                    if (NetworkManager.Singleton.NetworkConfig.EnsureNetworkVariableLengthSafety)
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
                        if (NetworkManager.Singleton.NetworkConfig.EnsureNetworkVariableLengthSafety)
                        {
                            using (PooledNetworkBuffer varBuffer = PooledNetworkBuffer.Get())
                            {
                                networkVariableList[j].WriteField(varBuffer);
                                varBuffer.PadStream();

                                writer.WriteUInt16Packed((ushort)varBuffer.Length);
                                varBuffer.CopyTo(stream);
                            }
                        }
                        else
                        {
                            networkVariableList[j].WriteField(stream);
                        }
                    }
                }
            }
        }

        internal static void SetNetworkVariableData(List<INetworkVariable> networkVariableList, Stream stream)
        {
            if (networkVariableList.Count == 0) return;

            using (PooledNetworkReader reader = PooledNetworkReader.Get(stream))
            {
                for (int j = 0; j < networkVariableList.Count; j++)
                {
                    ushort varSize = 0;

                    if (NetworkManager.Singleton.NetworkConfig.EnsureNetworkVariableLengthSafety)
                    {
                        varSize = reader.ReadUInt16Packed();

                        if (varSize == 0) continue;
                    }
                    else
                    {
                        if (!reader.ReadBool()) continue;
                    }

                    long readStartPos = stream.Position;

                    networkVariableList[j].ReadField(stream, NetworkTickSystem.k_NoTick, NetworkTickSystem.k_NoTick);

                    if (NetworkManager.Singleton.NetworkConfig.EnsureNetworkVariableLengthSafety)
                    {
                        if (stream is NetworkBuffer networkBuffer)
                        {
                            networkBuffer.SkipPadBits();
                        }

                        if (stream.Position > (readStartPos + varSize))
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Var data read too far. " + (stream.Position - (readStartPos + varSize)) + " bytes.");
                            stream.Position = readStartPos + varSize;
                        }
                        else if (stream.Position < (readStartPos + varSize))
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Var data read too little. " + ((readStartPos + varSize) - stream.Position) + " bytes.");
                            stream.Position = readStartPos + varSize;
                        }
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Gets the local instance of a object with a given NetworkId
        /// </summary>
        /// <param name="networkId"></param>
        /// <returns></returns>
        protected NetworkObject GetNetworkObject(ulong networkId) => NetworkSpawnManager.SpawnedObjects.ContainsKey(networkId) ? NetworkSpawnManager.SpawnedObjects[networkId] : null;
    }
}