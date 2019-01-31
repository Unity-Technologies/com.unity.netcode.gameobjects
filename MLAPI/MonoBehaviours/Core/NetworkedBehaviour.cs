using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.Linq;
using MLAPI.Data;
using System.IO;
using MLAPI.Components;
using MLAPI.Configuration;
using MLAPI.Internal;
using MLAPI.Logging;
using MLAPI.NetworkedVar;
using MLAPI.Serialization;

namespace MLAPI
{
    /// <summary>
    /// The base class to override to write networked code. Inherits MonoBehaviour
    /// </summary>
    public abstract partial class NetworkedBehaviour : MonoBehaviour
    {
        [Obsolete("Use IsLocalPlayer instead", false)]
        public bool isLocalPlayer => IsLocalPlayer;
        /// <summary>
        /// Gets if the object is the the personal clients player object
        /// </summary>
        public bool IsLocalPlayer => NetworkedObject.IsLocalPlayer;
        [Obsolete("Use IsOwner instead", false)]
        public bool isOwner => IsOwner;
        /// <summary>
        /// Gets if the object is owned by the local player or if the object is the local player object
        /// </summary>
        public bool IsOwner => NetworkedObject.IsOwner;
        [Obsolete("Use IsServer instead", false)]
        protected bool isServer => IsServer;
        /// <summary>
        /// Gets if we are executing as server
        /// </summary>
        protected bool IsServer => IsRunning && NetworkingManager.Singleton != null && NetworkingManager.Singleton.IsServer;
        [Obsolete("Use IsClient instead")]
        protected bool isClient => IsClient;
        /// <summary>
        /// Gets if we are executing as client
        /// </summary>
        protected bool IsClient => IsRunning && NetworkingManager.Singleton != null && NetworkingManager.Singleton.IsClient;
        [Obsolete("Use IsHost instead", false)]
        protected bool isHost => IsHost;
        /// <summary>
        /// Gets if we are executing as Host, I.E Server and Client
        /// </summary>
        protected bool IsHost => IsRunning && NetworkingManager.Singleton != null && NetworkingManager.Singleton.IsHost;
        private bool IsRunning => NetworkingManager.Singleton != null && (NetworkingManager.Singleton == null || NetworkingManager.Singleton.IsListening);
        [Obsolete("Use IsOwnedByServer instead", false)]
		public bool isOwnedByServer => IsOwnedByServer;
        /// <summary>
        /// Gets wheter or not the object has a owner
        /// </summary>
        public bool IsOwnedByServer => NetworkedObject.IsOwnedByServer;
        /// <summary>
        /// Contains the sender of the currently executing RPC. Useful for the convenience RPC methods
        /// </summary>
        protected uint ExecutingRpcSender { get; private set; }
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
                return _networkedObject;
            }
        }

        private NetworkedObject _networkedObject = null;
        [Obsolete("Use NetworkId instead", false)]
        public uint networkId => NetworkId;
        /// <summary>
        /// Gets the NetworkId of the NetworkedObject that owns the NetworkedBehaviour instance
        /// </summary>
        public uint NetworkId => NetworkedObject.NetworkId;
        /// <summary>
        /// Gets the clientId that owns the NetworkedObject
        /// </summary>
        public uint OwnerClientId => NetworkedObject.OwnerClientId;

        private void OnEnable()
        {
            if (_networkedObject == null)
                _networkedObject = GetComponentInParent<NetworkedObject>();

            NetworkedObject.NetworkedBehaviours.Add(this);
            OnEnabled();
        }

        private void OnDisable()
        {
            OnDisabled();
        }

        private void OnDestroy()
        {
            NetworkedObject.NetworkedBehaviours.Remove(this); // O(n)
            CachedClientRpcs.Remove(this);
            CachedServerRpcs.Remove(this);
            OnDestroyed();
        }

        internal bool networkedStartInvoked = false;
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
            CacheAttributes();
            WarnUnityReflectionMethodUse();
            NetworkedVarInit();
        }

        private void WarnUnityReflectionMethodUse()
        {
            MethodInfo[] methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == "OnDestroy")
                {
                    throw new Exception("The method \"OnDestroy\" is not allowed to be defined in classes that inherit NetworkedBehaviour. Please override the \"OnDestroyed\" method instead");
                }
                else if (methods[i].Name == "OnDisable")
                {
                    throw new Exception("The method \"OnDisable\" is not allowed to be defined in classes that inherit NetworkedBehaviour. Please override the \"OnDisabled\" method instead");
                }
                else if (methods[i].Name == "OnEnable")
                {
                    throw new Exception("The method \"OnEnable\" is not allowed to be defined in classes that inherit NetworkedBehaviour. Please override the \"OnEnable\" method instead");
                }
            }
        }

        /// <summary>
        /// Invoked when the object is Disabled
        /// </summary>
        public virtual void OnDisabled()
        {

        }

        /// <summary>
        /// Invoked when the object is Destroyed
        /// </summary>
        public virtual void OnDestroyed()
        {

        }

        /// <summary>
        /// Invoked when the object is Enabled
        /// </summary>
        public virtual void OnEnabled()
        {

        }

        /// <summary>                                                                               
        /// Gets called when SyncedVars gets updated                                                
        /// </summary>                                                                              
        public virtual void OnSyncVarUpdate()
        {                                                                                                  
            
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

        private bool networkedVarInit = false;
        private readonly List<HashSet<int>> channelMappedVarIndexes = new List<HashSet<int>>();
        private readonly List<string> channelsForVarGroups = new List<string>();
        internal readonly List<INetworkedVar> networkedVarFields = new List<INetworkedVar>();
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
                return list.OrderBy(x => x.Name).ToArray();
            }
        }
        
        internal List<INetworkedVar> GetDummyNetworkedVars()
        {
            List<INetworkedVar> networkedVars = new List<INetworkedVar>();
            FieldInfo[] sortedFields = GetFieldInfoForType(GetType());
            for (int i = 0; i < sortedFields.Length; i++)
            {
                Type fieldType = sortedFields[i].FieldType;
                if (fieldType.HasInterface(typeof(INetworkedVar)))
                {
                    INetworkedVar instance = null;
                    if (fieldType.IsGenericTypeDefinition)
                    {
                        Type genericType = fieldType.MakeGenericType(fieldType.GetGenericArguments());
                        instance = (INetworkedVar)Activator.CreateInstance(genericType, true);
                    }
                    else
                    {
                        instance = (INetworkedVar)Activator.CreateInstance(fieldType, true);
                    }
                    instance.SetNetworkedBehaviour(this);
                    networkedVars.Add(instance);
                }
            }
            return networkedVars;
        }

        internal void NetworkedVarInit()
        {
            if (networkedVarInit)
                return;
            networkedVarInit = true;

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

            //Create index map for channels
            Dictionary<string, int> firstLevelIndex = new Dictionary<string, int>();
            int secondLevelCounter = 0;
            for (int i = 0; i < networkedVarFields.Count; i++)
            {
                string channel = networkedVarFields[i].GetChannel(); //Cache this here. Some developers are stupid. You don't know what shit they will do in their methods
                if (!firstLevelIndex.ContainsKey(channel))
                {
                    firstLevelIndex.Add(channel, secondLevelCounter);
                    channelsForVarGroups.Add(channel);
                    secondLevelCounter++;
                }
                if (firstLevelIndex[channel] >= channelMappedVarIndexes.Count)
                    channelMappedVarIndexes.Add(new HashSet<int>());
                channelMappedVarIndexes[firstLevelIndex[channel]].Add(i);
            }
        }
        
        private readonly List<int> networkedVarIndexesToReset = new List<int>();
        private readonly HashSet<int> networkedVarIndexesToResetSet = new HashSet<int>();
        internal void NetworkedVarUpdate()
        {
            if (!networkedVarInit)
                NetworkedVarInit();

            //TODO: Do this efficiently.

            if (!CouldHaveDirtyVars()) 
                return;

            networkedVarIndexesToReset.Clear();
            networkedVarIndexesToResetSet.Clear();
            
            for (int i = 0; i < NetworkingManager.Singleton.ConnectedClientsList.Count; i++)
            {
                //This iterates over every "channel group".
                for (int j = 0; j < channelMappedVarIndexes.Count; j++)
                {
                    using (PooledBitStream stream = PooledBitStream.Get())
                    {
                        using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                        {
                            writer.WriteUInt32Packed(NetworkId);
                            writer.WriteUInt16Packed(NetworkedObject.GetOrderIndex(this));

                            uint clientId = NetworkingManager.Singleton.ConnectedClientsList[i].ClientId;
                            bool writtenAny = false;
                            for (int k = 0; k < networkedVarFields.Count; k++)
                            {
                                if (!channelMappedVarIndexes[j].Contains(k))
                                {
                                    //This var does not belong to the currently iterating channel group.
                                    writer.WriteBool(false);
                                    continue;
                                }

                                bool isDirty = networkedVarFields[k].IsDirty(); //cache this here. You never know what operations users will do in the dirty methods
                                writer.WriteBool(isDirty);

                                if (isDirty && (!IsServer || networkedVarFields[k].CanClientRead(clientId)))
                                {
                                    writtenAny = true;
                                    networkedVarFields[k].WriteDelta(stream);
                                    if (!networkedVarIndexesToResetSet.Contains(k))
                                    {
                                        networkedVarIndexesToResetSet.Add(k);
                                        networkedVarIndexesToReset.Add(k);
                                    }
                                }
                            }
                            
                            if (writtenAny)
                            {
                                if (IsServer)
                                    InternalMessageHandler.Send(clientId, MLAPIConstants.MLAPI_NETWORKED_VAR_DELTA, channelsForVarGroups[j], stream, SecuritySendFlags.None);
                                else
                                    InternalMessageHandler.Send(NetworkingManager.Singleton.ServerClientId, MLAPIConstants.MLAPI_NETWORKED_VAR_DELTA, channelsForVarGroups[j], stream, SecuritySendFlags.None);   
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < networkedVarIndexesToReset.Count; i++)
            {
                networkedVarFields[networkedVarIndexesToReset[i]].ResetDirty();
            }
        }

        private bool CouldHaveDirtyVars()
        {
            for (int i = 0; i < networkedVarFields.Count; i++)
            {
                if (networkedVarFields[i].IsDirty()) 
                    return true;
            }

            return false;
        }


        internal static void HandleNetworkedVarDeltas(List<INetworkedVar> networkedVarList, Stream stream, uint clientId, NetworkedBehaviour logInstance)
        {
            // TODO: Lot's of performance improvements to do here.

            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                for (int i = 0; i < networkedVarList.Count; i++)
                {
                    if (!reader.ReadBool())
                        continue;

                    if (NetworkingManager.Singleton.IsServer && !networkedVarList[i].CanClientWrite(clientId))
                    {
                        //This client wrote somewhere they are not allowed. This is critical
                        //We can't just skip this field. Because we don't actually know how to dummy read
                        //That is, we don't know how many bytes to skip. Because the interface doesn't have a 
                        //Read that gives us the value. Only a Read that applies the value straight away
                        //A dummy read COULD be added to the interface for this situation, but it's just being too nice.
                        //This is after all a developer fault. A critical error should be fine.
                        // - TwoTen

                        if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError("Client wrote to NetworkedVar without permission. No more variables can be read. This is critical. " + (logInstance != null ? ("NetworkId: " + logInstance.NetworkId + " BehaviourIndex: " + logInstance.NetworkedObject.GetOrderIndex(logInstance) + " VariableIndex: " + i) : string.Empty));
                        return;
                    }

                    networkedVarList[i].ReadDelta(stream, NetworkingManager.Singleton.IsServer);
                }
            }
        }

        internal static void HandleNetworkedVarUpdate(List<INetworkedVar> networkedVarList, Stream stream, uint clientId, NetworkedBehaviour logInstance)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                for (int i = 0; i < networkedVarList.Count; i++)
                {
                    if (!reader.ReadBool())
                        continue;

                    if (NetworkingManager.Singleton.IsServer && !networkedVarList[i].CanClientWrite(clientId))
                    {
                        //This client wrote somewhere they are not allowed. This is critical
                        //We can't just skip this field. Because we don't actually know how to dummy read
                        //That is, we don't know how many bytes to skip. Because the interface doesn't have a 
                        //Read that gives us the value. Only a Read that applies the value straight away
                        //A dummy read COULD be added to the interface for this situation, but it's just being too nice.
                        //This is after all a developer fault. A critical error should be fine.
                        // - TwoTen
                        if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError("Client wrote to NetworkedVar without permission. No more variables can be read. This is critical. " + (logInstance != null ? ("NetworkId: " + logInstance.NetworkId + " BehaviourIndex: " + logInstance.NetworkedObject.GetOrderIndex(logInstance) + " VariableIndex: " + i) : string.Empty));
                        return;
                    }

                    networkedVarList[i].ReadField(stream);
                }
            }
        }

        internal static void WriteNetworkedVarData(List<INetworkedVar> networkedVarList, PooledBitWriter writer, Stream stream, uint clientId)
        {
            if (networkedVarList.Count == 0)
                return;

            for (int j = 0; j < networkedVarList.Count; j++)
            {
                bool canClientRead = networkedVarList[j].CanClientRead(clientId);
                writer.WriteBool(canClientRead);
                if (canClientRead) networkedVarList[j].WriteField(stream);
            }
        }

        internal static void SetNetworkedVarData(List<INetworkedVar> networkedVarList, PooledBitReader reader, Stream stream)
        {
            if (networkedVarList.Count == 0)
                return;

            for (int j = 0; j < networkedVarList.Count; j++)
            {
                if (reader.ReadBool()) networkedVarList[j].ReadField(stream);
            }
        }


        #endregion

        #region MESSAGING_SYSTEM
        private readonly Dictionary<NetworkedBehaviour, Dictionary<ulong, ClientRPC>> CachedClientRpcs = new Dictionary<NetworkedBehaviour, Dictionary<ulong, ClientRPC>>();
        private readonly Dictionary<NetworkedBehaviour, Dictionary<ulong, ServerRPC>> CachedServerRpcs = new Dictionary<NetworkedBehaviour, Dictionary<ulong, ServerRPC>>();
        private static readonly Dictionary<Type, MethodInfo[]> Methods = new Dictionary<Type, MethodInfo[]>();
        private static readonly Dictionary<ulong, string> HashResults = new Dictionary<ulong, string>();

        private ulong HashMethodName(string name)
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

        private MethodInfo[] GetNetworkedBehaviorChildClassesMethods(Type type, List<MethodInfo> list = null) 
        {
            if (list == null) 
            {
                list = new List<MethodInfo>();
                list.AddRange(type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
            }
            else
            {
                list.AddRange(type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance));
            }

            if (type.BaseType != null && type.BaseType != typeof(NetworkedBehaviour))
            {
                return GetNetworkedBehaviorChildClassesMethods(type.BaseType, list);
            }
            else
            {
                return list.ToArray();
            }
        }

        private void CacheAttributes()
        {
            Type type = GetType();
            
            CachedClientRpcs.Add(this, new Dictionary<ulong, ClientRPC>());
            CachedServerRpcs.Add(this, new Dictionary<ulong, ServerRPC>());

            MethodInfo[] methods;
            if (Methods.ContainsKey(type)) methods = Methods[type];
            else
            {
                methods = GetNetworkedBehaviorChildClassesMethods(type);
                Methods.Add(type, methods);
            }

            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].IsDefined(typeof(ServerRPC), true))
                {
                    ServerRPC[] attributes = (ServerRPC[])methods[i].GetCustomAttributes(typeof(ServerRPC), true);
                    if (attributes.Length > 1)
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Having more than 1 ServerRPC attribute per method is not supported.");
                    }

                    ParameterInfo[] parameters = methods[i].GetParameters();
                    if (parameters.Length == 2 && parameters[0].ParameterType == typeof(uint) && parameters[1].ParameterType == typeof(Stream) && methods[i].ReturnType == typeof(void))
                    {
                        //use delegate
                        attributes[0].rpcDelegate = (RpcDelegate)Delegate.CreateDelegate(typeof(RpcDelegate), this, methods[i].Name);
                    }
                    else
                    {
                        if (methods[i].ReturnType != typeof(void) && !SerializationHelper.IsTypeSupported(methods[i].ReturnType))
                        {
                            if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogWarning("Invalid return type of RPC. Has to be either void or RpcResponse<T> with a serializable type");
                        }

                        attributes[0].reflectionMethod = new ReflectionMethod(methods[i]);
                    }
                    
                    ulong hash = HashMethodName(methods[i].Name);
                    if (HashResults.ContainsKey(hash) && HashResults[hash] != methods[i].Name)
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError($"Hash collision detected for RPC method. The method \"{methods[i].Name}\" collides with the method \"{HashResults[hash]}\". This can be solved by increasing the amount of bytes to use for hashing in the NetworkConfig or changing the name of one of the conflicting methods.");
                    }
                    else if (!HashResults.ContainsKey(hash))
                    {
                        HashResults.Add(hash, methods[i].Name);
                    }

                    CachedServerRpcs[this].Add(hash, attributes[0]);
                }
                
                if (methods[i].IsDefined(typeof(ClientRPC), true))
                {
                    ClientRPC[] attributes = (ClientRPC[])methods[i].GetCustomAttributes(typeof(ClientRPC), true);
                    if (attributes.Length > 1)
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Having more than 1 ClientRPC attribute per method is not supported.");
                    }

                    ParameterInfo[] parameters = methods[i].GetParameters();
                    if (parameters.Length == 2 && parameters[0].ParameterType == typeof(uint) && parameters[1].ParameterType == typeof(Stream) && methods[i].ReturnType == typeof(void))
                    {
                        //use delegate
                        attributes[0].rpcDelegate = (RpcDelegate)Delegate.CreateDelegate(typeof(RpcDelegate), this, methods[i].Name);
                    }
                    else
                    {
                        if (methods[i].ReturnType != typeof(void) && !SerializationHelper.IsTypeSupported(methods[i].ReturnType))
                        {
                            if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogWarning("Invalid return type of RPC. Has to be either void or RpcResponse<T> with a serializable type");
                        }
                        
                        attributes[0].reflectionMethod = new ReflectionMethod(methods[i]);
                    }

                    ulong hash = HashMethodName(methods[i].Name);
                    if (HashResults.ContainsKey(hash) && HashResults[hash] != methods[i].Name)
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError($"Hash collision detected for RPC method. The method \"{methods[i].Name}\" collides with the method \"{HashResults[hash]}\". This can be solved by increasing the amount of bytes to use for hashing in the NetworkConfig or changing the name of one of the conflicting methods.");
                    }
                    else if (!HashResults.ContainsKey(hash))
                    {
                        HashResults.Add(hash, methods[i].Name);
                    }

                    CachedClientRpcs[this].Add(HashMethodName(methods[i].Name), attributes[0]);
                }     
            }
        }

        internal object OnRemoteServerRPC(ulong hash, uint senderClientId, Stream stream)
        {
            if (!CachedServerRpcs.ContainsKey(this) || !CachedServerRpcs[this].ContainsKey(hash))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("ServerRPC request method not found");
                return null;
            }
            
            return InvokeServerRPCLocal(hash, senderClientId, stream);
        }
        
        internal object OnRemoteClientRPC(ulong hash, uint senderClientId, Stream stream)
        {
            if (!CachedClientRpcs.ContainsKey(this) || !CachedClientRpcs[this].ContainsKey(hash))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("ClientRPC request method not found");
                return null;
            }
            
            return InvokeClientRPCLocal(hash, senderClientId, stream);
        }

        private object InvokeServerRPCLocal(ulong hash, uint senderClientId, Stream stream)
        {
            if (!CachedServerRpcs.ContainsKey(this) || !CachedServerRpcs[this].ContainsKey(hash))
                return null;
            
            ServerRPC rpc = CachedServerRpcs[this][hash];

            if (rpc.RequireOwnership && senderClientId != OwnerClientId)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only owner can invoke ServerRPC that is marked to require ownership");
                return null;
            }

            if (stream.Position != 0)
            {
                //Create a new stream so that the stream they get ONLY contains user data and not MLAPI headers
                using (PooledBitStream userStream = PooledBitStream.Get())
                {
                    userStream.CopyUnreadFrom(stream);
                    userStream.Position = 0;
                    ExecutingRpcSender = senderClientId;

                    if (rpc.reflectionMethod != null)
                    {
                        ExecutingRpcSender = senderClientId;
                        
                        return rpc.reflectionMethod.Invoke(this, userStream);
                    }

                    if (rpc.rpcDelegate != null)
                    {
                        rpc.rpcDelegate(senderClientId, userStream);
                    }

                    return null;
                }
            }
            else
            {
                ExecutingRpcSender = senderClientId;

                if (rpc.reflectionMethod != null)
                {
                    ExecutingRpcSender = senderClientId;
                    
                    return rpc.reflectionMethod.Invoke(this, stream);
                }

                if (rpc.rpcDelegate != null)
                {
                    rpc.rpcDelegate(senderClientId, stream);
                }

                return null;
            }
        }

        private object InvokeClientRPCLocal(ulong hash, uint senderClientId, Stream stream)
        {
            if (!CachedClientRpcs.ContainsKey(this) || !CachedClientRpcs[this].ContainsKey(hash))
                return null;
            
            ClientRPC rpc = CachedClientRpcs[this][hash];

            if (stream.Position != 0)
            {
                //Create a new stream so that the stream they get ONLY contains user data and not MLAPI headers
                using (PooledBitStream userStream = PooledBitStream.Get())
                {
                    userStream.CopyUnreadFrom(stream);
                    userStream.Position = 0;

                    if (rpc.reflectionMethod != null)
                    {
                        return rpc.reflectionMethod.Invoke(this, userStream);
                    }

                    if (rpc.rpcDelegate != null)
                    {
                        rpc.rpcDelegate(senderClientId, userStream);
                    }

                    return null;
                }
            }
            else
            {
                if (rpc.reflectionMethod != null)
                {
                    return rpc.reflectionMethod.Invoke(this, stream);
                }

                if (rpc.rpcDelegate != null)
                {
                    rpc.rpcDelegate(senderClientId, stream);
                }

                return null;
            }
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
        
        internal void SendClientRPCBoxed(ulong hash, uint clientId, string channel, SecuritySendFlags security, params object[] parameters)
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
        
        internal RpcResponse<T> SendClientRPCBoxedResponse<T>(ulong hash, uint clientId, string channel, SecuritySendFlags security, params object[] parameters)
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
        
        internal void SendClientRPCBoxed(ulong hash, List<uint> clientIds, string channel, SecuritySendFlags security, params object[] parameters)
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

        internal void SendClientRPCBoxed(uint clientIdToIgnore, ulong hash, string channel, SecuritySendFlags security, params object[] parameters)
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
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only server and host can invoke ServerRPC");
                return;
            }

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt32Packed(NetworkId);
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
                        InternalMessageHandler.Send(NetworkingManager.Singleton.ServerClientId, MLAPIConstants.MLAPI_SERVER_RPC, string.IsNullOrEmpty(channel) ? "MLAPI_DEFAULT_MESSAGE" : channel, stream, security);
                    }
                }
            }
        }
        
        internal RpcResponse<T> SendServerRPCPerformanceResponse<T>(ulong hash, Stream messageStream, string channel, SecuritySendFlags security)
        {
            if (!IsClient && IsRunning)
            {
                //We are ONLY a server.
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only server and host can invoke ServerRPC");
                return null;
            }

            ulong responseId = ResponseMessageManager.GenerateMessageId();

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt32Packed(NetworkId);
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
                        
                        InternalMessageHandler.Send(NetworkingManager.Singleton.ServerClientId, MLAPIConstants.MLAPI_SERVER_RPC_REQUEST, string.IsNullOrEmpty(channel) ? "MLAPI_DEFAULT_MESSAGE" : channel, stream, security);

                        return response;
                    }
                }
            }
        }

        internal void SendClientRPCPerformance(ulong hash,  List<uint> clientIds, Stream messageStream, string channel, SecuritySendFlags security)
        {            
            if (!IsServer && IsRunning)
            {
                //We are NOT a server.
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only clients and host can invoke ClientRPC");
                return;
            }

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt32Packed(NetworkId);
                    writer.WriteUInt16Packed(NetworkedObject.GetOrderIndex(this));
                    writer.WriteUInt64Packed(hash);

                    stream.CopyFrom(messageStream);

                    if (clientIds == null)
                    {
                        for (int i = 0; i < NetworkingManager.Singleton.ConnectedClientsList.Count; i++)
                        {
                            if (IsHost && NetworkingManager.Singleton.ConnectedClientsList[i].ClientId == NetworkingManager.Singleton.LocalClientId)
                            {
                                messageStream.Position = 0;
                                InvokeClientRPCLocal(hash, NetworkingManager.Singleton.LocalClientId, messageStream);
                            }
                            else
                            {
                                InternalMessageHandler.Send(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId, MLAPIConstants.MLAPI_CLIENT_RPC, string.IsNullOrEmpty(channel) ? "MLAPI_DEFAULT_MESSAGE" : channel, stream, security);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < clientIds.Count; i++)
                        {
                            if (IsHost && clientIds[i] == NetworkingManager.Singleton.LocalClientId)
                            {
                                messageStream.Position = 0;
                                InvokeClientRPCLocal(hash, NetworkingManager.Singleton.LocalClientId, messageStream);
                            }
                            else
                            {
                                InternalMessageHandler.Send(clientIds[i], MLAPIConstants.MLAPI_CLIENT_RPC, string.IsNullOrEmpty(channel) ? "MLAPI_DEFAULT_MESSAGE" : channel, stream, security);
                            }
                        }
                    }
                }
            }
        }

        internal void SendClientRPCPerformance(ulong hash, Stream messageStream, uint clientIdToIgnore, string channel, SecuritySendFlags security)
        {
            if (!IsServer && IsRunning)
            {
                //We are NOT a server.
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only clients and host can invoke ClientRPC");
                return;
            }

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt32Packed(NetworkId);
                    writer.WriteUInt16Packed(NetworkedObject.GetOrderIndex(this));
                    writer.WriteUInt64Packed(hash);

                    stream.CopyFrom(messageStream);


                    for (int i = 0; i < NetworkingManager.Singleton.ConnectedClientsList.Count; i++)
                    {
                        if (NetworkingManager.Singleton.ConnectedClientsList[i].ClientId == clientIdToIgnore)
                            continue;
                        if (IsHost && NetworkingManager.Singleton.ConnectedClientsList[i].ClientId == NetworkingManager.Singleton.LocalClientId)
                        {
                            messageStream.Position = 0;
                            InvokeClientRPCLocal(hash, NetworkingManager.Singleton.LocalClientId, messageStream);
                        }
                        else
                        {
                            InternalMessageHandler.Send(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId, MLAPIConstants.MLAPI_CLIENT_RPC, string.IsNullOrEmpty(channel) ? "MLAPI_DEFAULT_MESSAGE" : channel, stream, security);
                        }
                    }
                }
            }
        }

        internal void SendClientRPCPerformance(ulong hash, uint clientId, Stream messageStream, string channel, SecuritySendFlags security)
        {
            if (!IsServer && IsRunning)
            {
                //We are NOT a server.
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only clients and host can invoke ClientRPC");
                return;
            }

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt32Packed(NetworkId);
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
                        InternalMessageHandler.Send(clientId, MLAPIConstants.MLAPI_CLIENT_RPC, string.IsNullOrEmpty(channel) ? "MLAPI_DEFAULT_MESSAGE" : channel, stream, security);
                    }
                }
            }
        }
        
        internal RpcResponse<T> SendClientRPCPerformanceResponse<T>(ulong hash, uint clientId, Stream messageStream, string channel, SecuritySendFlags security)
        {
            if (!IsServer && IsRunning)
            {
                //We are NOT a server.
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only clients and host can invoke ClientRPC");
                return null;
            }

            ulong responseId = ResponseMessageManager.GenerateMessageId();
            
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt32Packed(NetworkId);
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
                        
                        InternalMessageHandler.Send(clientId, MLAPIConstants.MLAPI_CLIENT_RPC_REQUEST, string.IsNullOrEmpty(channel) ? "MLAPI_DEFAULT_MESSAGE" : channel, stream, security);
                        
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
        protected NetworkedObject GetNetworkedObject(uint networkId)
        {
            if(SpawnManager.SpawnedObjects.ContainsKey(networkId))
                return SpawnManager.SpawnedObjects[networkId];
            return null;
        }
    }
}
