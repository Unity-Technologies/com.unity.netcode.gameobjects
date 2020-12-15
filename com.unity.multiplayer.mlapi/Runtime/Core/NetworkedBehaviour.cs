using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Text;
using MLAPI.Configuration;
using MLAPI.Hashing;
using MLAPI.Logging;
using MLAPI.Messaging;
using MLAPI.NetworkedVar;
using MLAPI.Profiling;
using MLAPI.Reflection;
using MLAPI.Security;
using MLAPI.Serialization.Pooled;
using MLAPI.Spawning;
using BitStream = MLAPI.Serialization.BitStream;
using MLAPI.Serialization;
using Unity.Profiling;

namespace MLAPI
{
    /// <summary>
    /// The base class to override to write networked code. Inherits MonoBehaviour
    /// </summary>
    public abstract partial class NetworkedBehaviour : MonoBehaviour
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        static ProfilerMarker s_SendClientRPCPerformance = new ProfilerMarker("NetworkedBehaviour.SendClientRPCPerformance");
#endif
        /// <summary>
        /// Gets if the object is the the personal clients player object
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use IsLocalPlayer instead", false)]
        public bool isLocalPlayer => IsLocalPlayer;
        /// <summary>
        /// Gets if the object is the the personal clients player object
        /// </summary>
        public bool IsLocalPlayer => NetworkedObject.IsLocalPlayer;
        /// <summary>
        /// Gets if the object is owned by the local player or if the object is the local player object
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use IsOwner instead", false)]
        public bool isOwner => IsOwner;
        /// <summary>
        /// Gets if the object is owned by the local player or if the object is the local player object
        /// </summary>
        public bool IsOwner => NetworkedObject.IsOwner;
        /// <summary>
        /// Gets if we are executing as server
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use IsServer instead", false)]
        protected bool isServer => IsServer;
        /// <summary>
        /// Gets if we are executing as server
        /// </summary>
        protected static bool IsServer => IsRunning && NetworkingManager.Singleton.IsServer;
        /// <summary>
        /// Gets if we are executing as client
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use IsClient instead")]
        protected bool isClient => IsClient;
        /// <summary>
        /// Gets if we are executing as client
        /// </summary>
        protected bool IsClient => IsRunning && NetworkingManager.Singleton.IsClient;
        /// <summary>
        /// Gets if we are executing as Host, I.E Server and Client
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use IsHost instead", false)]
        protected bool isHost => IsHost;
        /// <summary>
        /// Gets if we are executing as Host, I.E Server and Client
        /// </summary>
        protected bool IsHost => IsRunning && NetworkingManager.Singleton.IsHost;
        private static bool IsRunning => NetworkingManager.Singleton != null && NetworkingManager.Singleton.IsListening;
        /// <summary>
        /// Gets Whether or not the object has a owner
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use IsOwnedByServer instead", false)]
		public bool isOwnedByServer => IsOwnedByServer;
        /// <summary>
        /// Gets Whether or not the object has a owner
        /// </summary>
        public bool IsOwnedByServer => NetworkedObject.IsOwnedByServer;
        /// <summary>
        /// Contains the sender of the currently executing RPC. Useful for the convenience RPC methods
        /// </summary>
        protected ulong ExecutingRpcSender => executingRpcSender;
        internal ulong executingRpcSender;
        /// <summary>
        /// Gets the NetworkedObject that owns this NetworkedBehaviour instance
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use NetworkedObject instead", false)]
        public NetworkedObject networkedObject => NetworkedObject;
        /// <summary>
        /// Gets the NetworkedObject that owns this NetworkedBehaviour instance
        /// </summary>
        public NetworkedObject NetworkedObject
        {
            get
            {
                if (_networkedObject == null)
                {
                    _networkedObject = GetComponentInParent<NetworkedObject>();
                }

                if (_networkedObject == null)
                {
                    throw new NullReferenceException("Could not get NetworkedObject for the NetworkedBehaviour. Are you missing a NetworkedObject component?");
                }

                return _networkedObject;
            }
        }
        /// <summary>
        /// Gets whether or not this NetworkedBehaviour instance has a NetworkedObject owner.
        /// </summary>
        public bool HasNetworkedObject
        {
            get
            {
                if (_networkedObject == null)
                {
                    _networkedObject = GetComponentInParent<NetworkedObject>();
                }

                return _networkedObject != null;
            }
        }

        private NetworkedObject _networkedObject = null;
        /// <summary>
        /// Gets the NetworkId of the NetworkedObject that owns the NetworkedBehaviour instance
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use NetworkId instead", false)]
        public ulong networkId => NetworkId;
        /// <summary>
        /// Gets the NetworkId of the NetworkedObject that owns the NetworkedBehaviour instance
        /// </summary>
        public ulong NetworkId => NetworkedObject.NetworkId;
        /// <summary>
        /// Gets the clientId that owns the NetworkedObject
        /// </summary>
        public ulong OwnerClientId => NetworkedObject.OwnerClientId;

        internal bool networkedStartInvoked = false;
        internal bool internalNetworkedStartInvoked = false;
        /// <summary>
        /// Gets called when message handlers are ready to be registered and the networking is setup
        /// </summary>
        public virtual void NetworkStart()
        {

        }

        /// <summary>
        /// Gets called when message handlers are ready to be registered and the networking is setup. Provides a Payload if it was provided
        /// </summary>
        /// <param name="stream">The stream containing the spawn payload</param>
        public virtual void NetworkStart(Stream stream)
        {
            NetworkStart();
        }

        internal void InternalNetworkStart()
        {
            rpcDefinition = RpcTypeDefinition.Get(GetType());
            rpcDelegates = rpcDefinition.CreateTargetedDelegates(this);

            InitializeVars();
        }

        /// <summary>
        /// Gets called when the local client gains ownership of this object
        /// </summary>
        public virtual void OnGainedOwnership()
        {

        }
        /// <summary>
        /// Gets called when we loose ownership of this object
        /// </summary>
        public virtual void OnLostOwnership()
        {

        }

        /// <summary>
        /// Gets behaviourId for this NetworkedBehaviour on this NetworkedObject
        /// </summary>
        /// <returns>The behaviourId for the current NetworkedBehaviour</returns>
        public ushort GetBehaviourId()
        {
            return NetworkedObject.GetOrderIndex(this);
        }

        /// <summary>
        /// Returns a the NetworkedBehaviour with a given behaviourId for the current networkedObject
        /// </summary>
        /// <param name="id">The behaviourId to return</param>
        /// <returns>Returns NetworkedBehaviour with given behaviourId</returns>
        protected NetworkedBehaviour GetBehaviour(ushort id)
        {
            return NetworkedObject.GetBehaviourAtOrderIndex(id);
        }

        #region NetworkedVar

        private bool varInit = false;

        private readonly List<HashSet<int>> channelMappedNetworkedVarIndexes = new List<HashSet<int>>();
        private readonly List<string> channelsForNetworkedVarGroups = new List<string>();
        internal readonly List<INetworkedVar> networkedVarFields = new List<INetworkedVar>();

        private static HashSet<MLAPI.NetworkedObject> touched = new HashSet<MLAPI.NetworkedObject>();
        private static readonly Dictionary<Type, FieldInfo[]> fieldTypes = new Dictionary<Type, FieldInfo[]>();

        private static FieldInfo[] GetFieldInfoForType(Type type)
        {
            if (!fieldTypes.ContainsKey(type))
                fieldTypes.Add(type, GetFieldInfoForTypeRecursive(type));

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

            if (type.BaseType != null && type.BaseType != typeof(NetworkedBehaviour))
            {
                return GetFieldInfoForTypeRecursive(type.BaseType, list);
            }
            else
            {
                return list.OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
            }
        }

        internal void InitializeVars()
        {
            if (varInit)
                return;
            varInit = true;

            FieldInfo[] sortedFields = GetFieldInfoForType(GetType());

            for (int i = 0; i < sortedFields.Length; i++)
            {
                Type fieldType = sortedFields[i].FieldType;

                if (fieldType.HasInterface(typeof(INetworkedVar)))
                {
                    INetworkedVar instance = (INetworkedVar)sortedFields[i].GetValue(this);

                    if (instance == null)
                    {
                        instance = (INetworkedVar)Activator.CreateInstance(fieldType, true);
                        sortedFields[i].SetValue(this, instance);
                    }

                    instance.SetNetworkedBehaviour(this);
                    networkedVarFields.Add(instance);
                }
            }

            {
                // Create index map for channels
                Dictionary<string, int> firstLevelIndex = new Dictionary<string, int>();
                int secondLevelCounter = 0;

                for (int i = 0; i < networkedVarFields.Count; i++)
                {
                    string channel = networkedVarFields[i].GetChannel(); // Cache this here. Some developers are stupid. You don't know what shit they will do in their methods

                    if (!firstLevelIndex.ContainsKey(channel))
                    {
                        firstLevelIndex.Add(channel, secondLevelCounter);
                        channelsForNetworkedVarGroups.Add(channel);
                        secondLevelCounter++;
                    }

                    if (firstLevelIndex[channel] >= channelMappedNetworkedVarIndexes.Count)
                    {
                        channelMappedNetworkedVarIndexes.Add(new HashSet<int>());
                    }

                    channelMappedNetworkedVarIndexes[firstLevelIndex[channel]].Add(i);
                }
            }
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        public static ProfilerMarker s_NetworkedBehaviourUpdate = new ProfilerMarker("MLAPI.NetworkedObject.NetworkedBehaviourUpdate");
#endif

        internal static void NetworkedBehaviourUpdate()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_NetworkedBehaviourUpdate.Begin();
#endif
            try
            {
                if (IsServer)
                {
                    touched.Clear();
                    for (int i = 0; i < NetworkingManager.Singleton.ConnectedClientsList.Count; i++)
                    {
                        var client = NetworkingManager.Singleton.ConnectedClientsList[i];
                        var spawnedObjs = SpawnManager.SpawnedObjectsList;
                        touched.UnionWith(spawnedObjs);
                        foreach (var sobj in spawnedObjs)
                        {
                            // Sync just the variables for just the objects this client sees
                            for (int k = 0; k < sobj.childNetworkedBehaviours.Count; k++)
                            {
                                sobj.childNetworkedBehaviours[k].VarUpdate(client.ClientId);
                            }
                        }
                    }

                    // Now, reset all the no-longer-dirty variables
                    foreach (var sobj in touched)
                    {
                        for (int k = 0; k < sobj.childNetworkedBehaviours.Count; k++)
                        {
                            sobj.childNetworkedBehaviours[k].PostNetworkVariableWrite();
                        }
                    }
                }
                else
                {

                    // when client updates the sever, it tells it about all its objects
                    foreach (var sobj in SpawnManager.SpawnedObjectsList)
                    {
                        for (int k = 0; k < sobj.childNetworkedBehaviours.Count; k++)
                        {
                           sobj.childNetworkedBehaviours[k].VarUpdate(NetworkingManager.Singleton.ServerClientId);
                        }
                    }

                    // Now, reset all the no-longer-dirty variables
                    foreach (var sobj in SpawnManager.SpawnedObjectsList)
                    {
                        for (int k = 0; k < sobj.childNetworkedBehaviours.Count; k++)
                        {
                            sobj.childNetworkedBehaviours[k].PostNetworkVariableWrite();
                        }
                    }
                }

            }
            finally
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                s_NetworkedBehaviourUpdate.End();
#endif
            }
        }


        internal void PreNetworkVariableWrite()
        {
            // reset our "which variables got written" data
            networkedVarIndexesToReset.Clear();
            networkedVarIndexesToResetSet.Clear();
        }

        internal void PostNetworkVariableWrite()
        {
            // mark any variables we wrote as no longer dirty
            for (int i = 0; i < networkedVarIndexesToReset.Count; i++)
            {
                networkedVarFields[networkedVarIndexesToReset[i]].ResetDirty();
            }
        }

        internal void VarUpdate(ulong clientId)
        {
            if (!varInit)
                InitializeVars();

            PreNetworkVariableWrite();
            NetworkedVarUpdate(clientId);
        }

        private readonly List<int> networkedVarIndexesToReset = new List<int>();
        private readonly HashSet<int> networkedVarIndexesToResetSet = new HashSet<int>();

        private void NetworkedVarUpdate(ulong clientId)
        {
           if (!CouldHaveDirtyNetworkedVars())
               return;

            for (int j = 0; j < channelMappedNetworkedVarIndexes.Count; j++)
            {
                using (PooledBitStream stream = PooledBitStream.Get())
                {
                    using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                    {
                        writer.WriteUInt64Packed(NetworkId);
                        writer.WriteUInt16Packed(NetworkedObject.GetOrderIndex(this));

                        bool writtenAny = false;
                        for (int k = 0; k < networkedVarFields.Count; k++)
                        {
                            if (!channelMappedNetworkedVarIndexes[j].Contains(k))
                            {
                                // This var does not belong to the currently iterating channel group.
                                if (NetworkingManager.Singleton.NetworkConfig.EnsureNetworkedVarLengthSafety)
                                {
                                    writer.WriteUInt16Packed(0);
                                }
                                else
                                {
                                    writer.WriteBool(false);
                                }
                                continue;
                            }

                            bool isDirty = networkedVarFields[k].IsDirty(); // cache this here. You never know what operations users will do in the dirty methods

                            //   if I'm dirty AND a client, write (server always has all permissions)
                            //   if I'm dirty AND the server AND the client can read me, send.
                            bool shouldWrite = isDirty && (!IsServer || networkedVarFields[k].CanClientRead(clientId));

                            if (NetworkingManager.Singleton.NetworkConfig.EnsureNetworkedVarLengthSafety)
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

                                if (NetworkingManager.Singleton.NetworkConfig.EnsureNetworkedVarLengthSafety)
                                {
                                    using (PooledBitStream varStream = PooledBitStream.Get())
                                    {
                                        networkedVarFields[k].WriteDelta(varStream);
                                        varStream.PadStream();

                                        writer.WriteUInt16Packed((ushort)varStream.Length);
                                        stream.CopyFrom(varStream);
                                    }
                                }
                                else
                                {
                                    networkedVarFields[k].WriteDelta(stream);
                                }

                                if (!networkedVarIndexesToResetSet.Contains(k))
                                {
                                    networkedVarIndexesToResetSet.Add(k);
                                    networkedVarIndexesToReset.Add(k);
                                }
                            }
                        }

                        if (writtenAny)
                        {
                            InternalMessageSender.Send(clientId,
                                MLAPIConstants.MLAPI_NETWORKED_VAR_DELTA,
                                channelsForNetworkedVarGroups[j], stream, SecuritySendFlags.None);
                        }
                    }
                }
            }
        }
        private bool CouldHaveDirtyNetworkedVars()
        {
            // TODO: There should be a better way by reading one dirty variable vs. 'n'
            for (int i = 0; i < networkedVarFields.Count; i++)
            {
                if (networkedVarFields[i].IsDirty())
                    return true;
            }

            return false;
        }

        internal static void HandleNetworkedVarDeltas(List<INetworkedVar> networkedVarList, Stream stream, ulong clientId, NetworkedBehaviour logInstance)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                for (int i = 0; i < networkedVarList.Count; i++)
                {
                    ushort varSize = 0;

                    if (NetworkingManager.Singleton.NetworkConfig.EnsureNetworkedVarLengthSafety)
                    {
                        varSize = reader.ReadUInt16Packed();

                        if (varSize == 0)
                            continue;
                    }
                    else
                    {
                        if (!reader.ReadBool())
                            continue;
                    }

                    if (IsServer && !networkedVarList[i].CanClientWrite(clientId))
                    {
                        if (NetworkingManager.Singleton.NetworkConfig.EnsureNetworkedVarLengthSafety)
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Client wrote to NetworkedVar without permission. " + (logInstance != null ? ("NetworkId: " + logInstance.NetworkId + " BehaviourIndex: " + logInstance.NetworkedObject.GetOrderIndex(logInstance) + " VariableIndex: " + i) : string.Empty));
                            stream.Position += varSize;
                            continue;
                        }
                        else
                        {
                            //This client wrote somewhere they are not allowed. This is critical
                            //We can't just skip this field. Because we don't actually know how to dummy read
                            //That is, we don't know how many bytes to skip. Because the interface doesn't have a
                            //Read that gives us the value. Only a Read that applies the value straight away
                            //A dummy read COULD be added to the interface for this situation, but it's just being too nice.
                            //This is after all a developer fault. A critical error should be fine.
                            // - TwoTen

                            if (NetworkLog.CurrentLogLevel <= LogLevel.Error) NetworkLog.LogError("Client wrote to NetworkedVar without permission. No more variables can be read. This is critical. " + (logInstance != null ? ("NetworkId: " + logInstance.NetworkId + " BehaviourIndex: " + logInstance.NetworkedObject.GetOrderIndex(logInstance) + " VariableIndex: " + i) : string.Empty));
                            return;
                        }
                    }

                    long readStartPos = stream.Position;

                    networkedVarList[i].ReadDelta(stream, IsServer);
                    ProfilerStatManager.networkVarsRcvd.Record();

                    if (NetworkingManager.Singleton.NetworkConfig.EnsureNetworkedVarLengthSafety)
                    {
                        (stream as BitStream).SkipPadBits();

                        if (stream.Position > (readStartPos + varSize))
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Var delta read too far. " + (stream.Position - (readStartPos + varSize)) + " bytes." + (logInstance != null ? ("NetworkId: " + logInstance.NetworkId + " BehaviourIndex: " + logInstance.NetworkedObject.GetOrderIndex(logInstance) + " VariableIndex: " + i) : string.Empty));
                            stream.Position = readStartPos + varSize;
                        }
                        else if (stream.Position < (readStartPos + varSize))
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Var delta read too little. " + ((readStartPos + varSize) - stream.Position) + " bytes." + (logInstance != null ? ("NetworkId: " + logInstance.NetworkId + " BehaviourIndex: " + logInstance.NetworkedObject.GetOrderIndex(logInstance) + " VariableIndex: " + i) : string.Empty));
                            stream.Position = readStartPos + varSize;
                        }
                    }
                }
            }
        }

        internal static void HandleNetworkedVarUpdate(List<INetworkedVar> networkedVarList, Stream stream, ulong clientId, NetworkedBehaviour logInstance)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                for (int i = 0; i < networkedVarList.Count; i++)
                {
                    ushort varSize = 0;

                    if (NetworkingManager.Singleton.NetworkConfig.EnsureNetworkedVarLengthSafety)
                    {
                        varSize = reader.ReadUInt16Packed();

                        if (varSize == 0)
                            continue;
                    }
                    else
                    {
                        if (!reader.ReadBool())
                            continue;
                    }

                    if (IsServer && !networkedVarList[i].CanClientWrite(clientId))
                    {
                        if (NetworkingManager.Singleton.NetworkConfig.EnsureNetworkedVarLengthSafety)
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Client wrote to NetworkedVar without permission. " + (logInstance != null ? ("NetworkId: " + logInstance.NetworkId + " BehaviourIndex: " + logInstance.NetworkedObject.GetOrderIndex(logInstance) + " VariableIndex: " + i) : string.Empty));
                            stream.Position += varSize;
                            continue;
                        }
                        else
                        {
                            //This client wrote somewhere they are not allowed. This is critical
                            //We can't just skip this field. Because we don't actually know how to dummy read
                            //That is, we don't know how many bytes to skip. Because the interface doesn't have a
                            //Read that gives us the value. Only a Read that applies the value straight away
                            //A dummy read COULD be added to the interface for this situation, but it's just being too nice.
                            //This is after all a developer fault. A critical error should be fine.
                            // - TwoTen
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Error) NetworkLog.LogError("Client wrote to NetworkedVar without permission. No more variables can be read. This is critical. " + (logInstance != null ? ("NetworkId: " + logInstance.NetworkId + " BehaviourIndex: " + logInstance.NetworkedObject.GetOrderIndex(logInstance) + " VariableIndex: " + i) : string.Empty));
                            return;
                        }
                    }

                    long readStartPos = stream.Position;

                    networkedVarList[i].ReadField(stream);
                    ProfilerStatManager.networkVarsRcvd.Record();

                    if (NetworkingManager.Singleton.NetworkConfig.EnsureNetworkedVarLengthSafety)
                    {
                        if (stream is BitStream bitStream)
                        {
                            bitStream.SkipPadBits();
                        }

                        if (stream.Position > (readStartPos + varSize))
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Var update read too far. " + (stream.Position - (readStartPos + varSize)) + " bytes." + (logInstance != null ? ("NetworkId: " + logInstance.NetworkId + " BehaviourIndex: " + logInstance.NetworkedObject.GetOrderIndex(logInstance) + " VariableIndex: " + i) : string.Empty));
                            stream.Position = readStartPos + varSize;
                        }
                        else if (stream.Position < (readStartPos + varSize))
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Var update read too little. " + ((readStartPos + varSize) - stream.Position) + " bytes." + (logInstance != null ? ("NetworkId: " + logInstance.NetworkId + " BehaviourIndex: " + logInstance.NetworkedObject.GetOrderIndex(logInstance) + " VariableIndex: " + i) : string.Empty));
                            stream.Position = readStartPos + varSize;
                        }
                    }
                }
            }
        }


        internal static void WriteNetworkedVarData(List<INetworkedVar> networkedVarList, Stream stream, ulong clientId)
        {
            if (networkedVarList.Count == 0)
                return;

            using (PooledBitWriter writer = PooledBitWriter.Get(stream))
            {
                for (int j = 0; j < networkedVarList.Count; j++)
                {
                    bool canClientRead = networkedVarList[j].CanClientRead(clientId);

                    if (NetworkingManager.Singleton.NetworkConfig.EnsureNetworkedVarLengthSafety)
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
                        if (NetworkingManager.Singleton.NetworkConfig.EnsureNetworkedVarLengthSafety)
                        {
                            using (PooledBitStream varStream = PooledBitStream.Get())
                            {
                                networkedVarList[j].WriteField(varStream);
                                varStream.PadStream();

                                writer.WriteUInt16Packed((ushort)varStream.Length);
                                varStream.CopyTo(stream);
                            }
                        }
                        else
                        {
                            networkedVarList[j].WriteField(stream);
                        }
                    }
                }
            }
        }

        internal static void SetNetworkedVarData(List<INetworkedVar> networkedVarList, Stream stream)
        {
            if (networkedVarList.Count == 0)
                return;

            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                for (int j = 0; j < networkedVarList.Count; j++)
                {
                    ushort varSize = 0;

                    if (NetworkingManager.Singleton.NetworkConfig.EnsureNetworkedVarLengthSafety)
                    {
                        varSize = reader.ReadUInt16Packed();

                        if (varSize == 0)
                            continue;
                    }
                    else
                    {
                        if (!reader.ReadBool())
                            continue;
                    }

                    long readStartPos = stream.Position;

                    networkedVarList[j].ReadField(stream);

                    if (NetworkingManager.Singleton.NetworkConfig.EnsureNetworkedVarLengthSafety)
                    {
                        if (stream is BitStream bitStream)
                        {
                            bitStream.SkipPadBits();
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

        #region MESSAGING_SYSTEM
        private static readonly StringBuilder methodInfoStringBuilder = new StringBuilder();
        private static readonly Dictionary<MethodInfo, ulong> methodInfoHashTable = new Dictionary<MethodInfo, ulong>();
        private RpcTypeDefinition rpcDefinition;
        internal RpcDelegate[] rpcDelegates;

        internal static ulong HashMethodName(string name)
        {
            HashSize mode = NetworkingManager.Singleton.NetworkConfig.RpcHashSize;

            if (mode == HashSize.VarIntTwoBytes)
                return name.GetStableHash16();
            if (mode == HashSize.VarIntFourBytes)
                return name.GetStableHash32();
            if (mode == HashSize.VarIntEightBytes)
                return name.GetStableHash64();

            return 0;
        }

        private ulong HashMethod(MethodInfo method)
        {
            if (methodInfoHashTable.ContainsKey(method))
            {
                return methodInfoHashTable[method];
            }

            ulong hash = HashMethodName(GetHashableMethodSignature(method));
            methodInfoHashTable.Add(method, hash);

            return hash;
        }

        internal static string GetHashableMethodSignature(MethodInfo method)
        {
            methodInfoStringBuilder.Length = 0;
            methodInfoStringBuilder.Append(method.Name);

            ParameterInfo[] parameters = method.GetParameters();

            for (int i = 0; i < parameters.Length; i++)
            {
                methodInfoStringBuilder.Append(parameters[i].ParameterType.Name);
            }

            return methodInfoStringBuilder.ToString();
        }

        internal object OnRemoteServerRPC(ulong hash, ulong senderClientId, Stream stream)
        {
            if (!rpcDefinition.serverMethods.ContainsKey(hash))
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("ServerRPC request method not found");
                return null;

            }

            return InvokeServerRPCLocal(hash, senderClientId, stream);
        }

        internal object OnRemoteClientRPC(ulong hash, ulong senderClientId, Stream stream)
        {
            if (!rpcDefinition.clientMethods.ContainsKey(hash))
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("ClientRPC request method not found");
                return null;
            }

            return InvokeClientRPCLocal(hash, senderClientId, stream);
        }

        private object InvokeServerRPCLocal(ulong hash, ulong senderClientId, Stream stream)
        {
            if (rpcDefinition.serverMethods.ContainsKey(hash))
            {
                return rpcDefinition.serverMethods[hash].Invoke(this, senderClientId, stream);
            }

            return null;
        }

        private object InvokeClientRPCLocal(ulong hash, ulong senderClientId, Stream stream)
        {
            if (rpcDefinition.clientMethods.ContainsKey(hash))
            {
                return rpcDefinition.clientMethods[hash].Invoke(this, senderClientId, stream);
            }

            return null;
        }

        //Technically boxed writes are not needed. But save LOC for the non performance sends.
        internal void SendServerRPCBoxed(ulong hash, string channel, SecuritySendFlags security, params object[] parameters)
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        writer.WriteObjectPacked(parameters[i]);
                    }

                    SendServerRPCPerformance(hash, stream, channel, security);
                }
            }
        }

        internal RpcResponse<T> SendServerRPCBoxedResponse<T>(ulong hash, string channel, SecuritySendFlags security, params object[] parameters)
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        writer.WriteObjectPacked(parameters[i]);
                    }

                    return SendServerRPCPerformanceResponse<T>(hash, stream, channel, security);
                }
            }
        }

        internal void SendClientRPCBoxedToClient(ulong hash, ulong clientId, string channel, SecuritySendFlags security, params object[] parameters)
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        writer.WriteObjectPacked(parameters[i]);
                    }
                    SendClientRPCPerformance(hash, clientId, stream, channel, security);
                }
            }
        }

        internal RpcResponse<T> SendClientRPCBoxedResponse<T>(ulong hash, ulong clientId, string channel, SecuritySendFlags security, params object[] parameters)
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        writer.WriteObjectPacked(parameters[i]);
                    }

                    return SendClientRPCPerformanceResponse<T>(hash, clientId, stream, channel, security);
                }
            }
        }

        internal void SendClientRPCBoxed(ulong hash, List<ulong> clientIds, string channel, SecuritySendFlags security, params object[] parameters)
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        writer.WriteObjectPacked(parameters[i]);
                    }
                    SendClientRPCPerformance(hash, clientIds, stream, channel, security);
                }
            }
        }

        internal void SendClientRPCBoxedToEveryoneExcept(ulong clientIdToIgnore, ulong hash, string channel, SecuritySendFlags security, params object[] parameters)
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        writer.WriteObjectPacked(parameters[i]);
                    }
                    SendClientRPCPerformance(hash, stream, clientIdToIgnore, channel, security);
                }
            }
        }

        internal void SendServerRPCPerformance(ulong hash, Stream messageStream, string channel, SecuritySendFlags security)
        {
            if (!IsClient && IsRunning)
            {
                //We are ONLY a server.
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Only client and host can invoke ServerRPC");
                return;
            }

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt64Packed(NetworkId);
                    writer.WriteUInt16Packed(NetworkedObject.GetOrderIndex(this));
                    writer.WriteUInt64Packed(hash);

                    stream.CopyFrom(messageStream);

                    if (IsHost)
                    {
                        messageStream.Position = 0;
                        InvokeServerRPCLocal(hash, NetworkingManager.Singleton.LocalClientId, messageStream);
                    }
                    else
                    {
                        InternalMessageSender.Send(NetworkingManager.Singleton.ServerClientId, MLAPIConstants.MLAPI_SERVER_RPC, string.IsNullOrEmpty(channel) ? "MLAPI_DEFAULT_MESSAGE" : channel, stream, security);
                        ProfilerStatManager.rpcsSent.Record();
                    }
                }
            }
        }

        internal RpcResponse<T> SendServerRPCPerformanceResponse<T>(ulong hash, Stream messageStream, string channel, SecuritySendFlags security)
        {
            if (!IsClient && IsRunning)
            {
                //We are ONLY a server.
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Only client and host can invoke ServerRPC");
                return null;
            }

            ulong responseId = ResponseMessageManager.GenerateMessageId();

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt64Packed(NetworkId);
                    writer.WriteUInt16Packed(NetworkedObject.GetOrderIndex(this));
                    writer.WriteUInt64Packed(hash);

                    if (!IsHost) writer.WriteUInt64Packed(responseId);

                    stream.CopyFrom(messageStream);

                    if (IsHost)
                    {
                        messageStream.Position = 0;
                        object result = InvokeServerRPCLocal(hash, NetworkingManager.Singleton.LocalClientId, messageStream);

                        return new RpcResponse<T>()
                        {
                            Id = responseId,
                            IsDone = true,
                            IsSuccessful = true,
                            Result = result,
                            Type = typeof(T),
                            ClientId = NetworkingManager.Singleton.ServerClientId
                        };
                    }
                    else
                    {
                        RpcResponse<T> response = new RpcResponse<T>()
                        {
                            Id = responseId,
                            IsDone = false,
                            IsSuccessful = false,
                            Type = typeof(T),
                            ClientId = NetworkingManager.Singleton.ServerClientId
                        };

                        ResponseMessageManager.Add(response.Id, response);

                        InternalMessageSender.Send(NetworkingManager.Singleton.ServerClientId, MLAPIConstants.MLAPI_SERVER_RPC_REQUEST, string.IsNullOrEmpty(channel) ? "MLAPI_DEFAULT_MESSAGE" : channel, stream, security);
                        ProfilerStatManager.rpcsSent.Record();

                        return response;
                    }
                }
            }
        }

        internal void SendClientRPCPerformance(ulong hash,  List<ulong> clientIds, Stream messageStream, string channel, SecuritySendFlags security)
        {
            if (!IsServer && IsRunning)
            {
                //We are NOT a server.
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Only servers and hosts can invoke ClientRPC");
                return;
            }

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt64Packed(NetworkId);
                    writer.WriteUInt16Packed(NetworkedObject.GetOrderIndex(this));
                    writer.WriteUInt64Packed(hash);

                    stream.CopyFrom(messageStream);

                    if (IsHost)
                    {
                        if (this.NetworkedObject.observers.Contains(NetworkingManager.Singleton.LocalClientId))
                        {
                            messageStream.Position = 0;
                            InvokeClientRPCLocal(hash, NetworkingManager.Singleton.LocalClientId, messageStream);
                        }
                        else
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Developer) NetworkLog.LogWarning("Silently suppressed ClientRPC because a connected client was not an observer");
                        }
                    }

                    InternalMessageSender.Send(MLAPIConstants.MLAPI_CLIENT_RPC, string.IsNullOrEmpty(channel) ? "MLAPI_DEFAULT_MESSAGE" : channel, clientIds, stream, security);
                    ProfilerStatManager.rpcsSent.Record(clientIds?.Count ?? NetworkingManager.Singleton.ConnectedClientsList.Count);
                }
            }
        }

        internal void SendClientRPCPerformance(ulong hash, Stream messageStream, ulong clientIdToIgnore, string channel, SecuritySendFlags security)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_SendClientRPCPerformance.Begin();
#endif
            if (!IsServer && IsRunning)
            {
                //We are NOT a server.
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Only servers and hosts can invoke ClientRPC");
                return;
            }

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt64Packed(NetworkId);
                    writer.WriteUInt16Packed(NetworkedObject.GetOrderIndex(this));
                    writer.WriteUInt64Packed(hash);

                    stream.CopyFrom(messageStream);


                    if (IsHost && NetworkingManager.Singleton.LocalClientId != clientIdToIgnore)
                    {
                        if (this.NetworkedObject.observers.Contains(NetworkingManager.Singleton.LocalClientId))
                        {
                            messageStream.Position = 0;
                            InvokeClientRPCLocal(hash, NetworkingManager.Singleton.LocalClientId, messageStream);
                        }
                        else
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Developer) NetworkLog.LogWarning("Silently suppressed ClientRPC because a connected client was not an observer");
                        }
                    }

                    InternalMessageSender.Send(MLAPIConstants.MLAPI_CLIENT_RPC, string.IsNullOrEmpty(channel) ? "MLAPI_DEFAULT_MESSAGE" : channel, clientIdToIgnore, stream, security);
                    ProfilerStatManager.rpcsSent.Record(NetworkingManager.Singleton.ConnectedClientsList.Count - 1);
                }
            }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_SendClientRPCPerformance.End();
#endif
        }

        internal void SendClientRPCPerformance(ulong hash, ulong clientId, Stream messageStream, string channel, SecuritySendFlags security)
        {
            if (!IsServer && IsRunning)
            {
                //We are NOT a server.
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Only servers and hosts can invoke ClientRPC");
                return;
            }

            if (!this.NetworkedObject.observers.Contains(clientId))
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Cannot send ClientRPC to client without visibility to the object");
                return;
            }

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt64Packed(NetworkId);
                    writer.WriteUInt16Packed(NetworkedObject.GetOrderIndex(this));
                    writer.WriteUInt64Packed(hash);

                    stream.CopyFrom(messageStream);

                    if (IsHost && clientId == NetworkingManager.Singleton.LocalClientId)
                    {
                        messageStream.Position = 0;
                        InvokeClientRPCLocal(hash, NetworkingManager.Singleton.LocalClientId, messageStream);
                    }
                    else
                    {
                        InternalMessageSender.Send(clientId, MLAPIConstants.MLAPI_CLIENT_RPC, string.IsNullOrEmpty(channel) ? "MLAPI_DEFAULT_MESSAGE" : channel, stream, security);
                        ProfilerStatManager.rpcsSent.Record();
                    }
                }
            }
        }

        internal RpcResponse<T> SendClientRPCPerformanceResponse<T>(ulong hash, ulong clientId, Stream messageStream, string channel, SecuritySendFlags security)
        {
            if (!IsServer && IsRunning)
            {
                //We are NOT a server.
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Only servers and hosts can invoke ClientRPC");
                return null;
            }

            if (!this.NetworkedObject.observers.Contains(clientId))
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Cannot send ClientRPC to client without visibility to the object");
                return null;
            }

            ulong responseId = ResponseMessageManager.GenerateMessageId();

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt64Packed(NetworkId);
                    writer.WriteUInt16Packed(NetworkedObject.GetOrderIndex(this));
                    writer.WriteUInt64Packed(hash);

                    if (!(IsHost && clientId == NetworkingManager.Singleton.LocalClientId)) writer.WriteUInt64Packed(responseId);

                    stream.CopyFrom(messageStream);

                    if (IsHost && clientId == NetworkingManager.Singleton.LocalClientId)
                    {
                        messageStream.Position = 0;
                        object result = InvokeClientRPCLocal(hash, NetworkingManager.Singleton.LocalClientId, messageStream);

                        return new RpcResponse<T>()
                        {
                            Id = responseId,
                            IsDone = true,
                            IsSuccessful = true,
                            Result = result,
                            Type = typeof(T),
                            ClientId = clientId
                        };
                    }
                    else
                    {
                        RpcResponse<T> response = new RpcResponse<T>()
                        {
                            Id = responseId,
                            IsDone = false,
                            IsSuccessful = false,
                            Type = typeof(T),
                            ClientId = clientId
                        };

                        ResponseMessageManager.Add(response.Id, response);

                        InternalMessageSender.Send(clientId, MLAPIConstants.MLAPI_CLIENT_RPC_REQUEST, string.IsNullOrEmpty(channel) ? "MLAPI_DEFAULT_MESSAGE" : channel, stream, security);
                        ProfilerStatManager.rpcsSent.Record();

                        return response;
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
        protected NetworkedObject GetNetworkedObject(ulong networkId)
        {
            if(SpawnManager.SpawnedObjects.ContainsKey(networkId))
                return SpawnManager.SpawnedObjects[networkId];
            return null;
        }
    }
}
