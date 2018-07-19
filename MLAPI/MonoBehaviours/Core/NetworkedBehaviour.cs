using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using MLAPI.Attributes;
using System.Linq;
using MLAPI.Data;
using MLAPI.NetworkingManagerComponents.Binary;
using MLAPI.NetworkingManagerComponents.Core;

namespace MLAPI.MonoBehaviours.Core
{
    /// <summary>
    /// The base class to override to write networked code. Inherits MonoBehaviour
    /// </summary>
    public abstract class NetworkedBehaviour : MonoBehaviour
    {
        /// <summary>
        /// Gets if the object is the the personal clients player object
        /// </summary>
        public bool isLocalPlayer => networkedObject.isLocalPlayer;
        /// <summary>
        /// Gets if the object is owned by the local player or if the object is the local player object
        /// </summary>
        public bool isOwner => networkedObject.isOwner;
        /// <summary>
        /// Gets if the object is owned by the local player and this is not a player object
        /// </summary>
        public bool isObjectOwner => networkedObject.isObjectOwner;
        /// <summary>
        /// Gets if we are executing as server
        /// </summary>
        protected bool isServer => NetworkingManager.singleton.isServer;
        /// <summary>
        /// Gets if we are executing as client
        /// </summary>
        protected bool isClient => NetworkingManager.singleton.isClient;
        /// <summary>
        /// Gets if we are executing as Host, I.E Server and Client
        /// </summary>
        protected bool isHost => NetworkingManager.singleton.isHost;
        /// <summary>
        /// Gets wheter or not the object has a owner
        /// </summary>
        public bool hasOwner => networkedObject.hasOwner;

        /// <summary>
        /// Gets the NetworkedObject that owns this NetworkedBehaviour instance
        /// </summary>
        public NetworkedObject networkedObject
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
        /// <summary>
        /// Gets the NetworkId of the NetworkedObject that owns the NetworkedBehaviour instance
        /// </summary>
        public uint networkId => networkedObject.NetworkId;
        /// <summary>
        /// Gets the clientId that owns the NetworkedObject
        /// </summary>
        public uint OwnerClientId => networkedObject.OwnerClientId;

        private readonly Dictionary<string, int> registeredMessageHandlers = new Dictionary<string, int>();

        private void OnEnable()
        {
            if (_networkedObject == null)
                _networkedObject = GetComponentInParent<NetworkedObject>();

            NetworkedObject.NetworkedBehaviours.Add(this);
            OnEnabled();
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
        /// <param name="payloadReader"></param>
        public virtual void NetworkStart(BitReader payloadReader)
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
            MethodInfo[] methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
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
            return networkedObject.GetOrderIndex(this);
        }

        /// <summary>
        /// Returns a the NetworkedBehaviour with a given behaviourId for the current networkedObject
        /// </summary>
        /// <param name="id">The behaviourId to return</param>
        /// <returns>Returns NetworkedBehaviour with given behaviourId</returns>
        protected NetworkedBehaviour GetBehaviour(ushort id)
        {
            return networkedObject.GetBehaviourAtOrderIndex(id);
        }

        private void OnDisable()
        {
            OnDisabled();
        }

        private void OnDestroy()
        {
            NetworkedObject.NetworkedBehaviours.Remove(this); // O(n)
            OnDestroyed();
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
                fieldTypes.Add(type, type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Instance).OrderBy(x => x.Name).ToArray());
            return fieldTypes[type];
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
                        Type genericType = fieldType.MakeGenericType(fieldType.GetGenericArguments());
                        instance = (INetworkedVar)Activator.CreateInstance(genericType, true);
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

        internal void NetworkedVarUpdate()
        {
            if (!networkedVarInit)
                NetworkedVarInit();
            //TODO: Do this efficiently.

            for (int i = 0; i < NetworkingManager.singleton.ConnectedClientsList.Count; i++)
            {
                //This iterates over every "channel group".
                for (int j = 0; j < channelMappedVarIndexes.Count; j++)
                {
                    using (BitWriter writer = BitWriter.Get())
                    {
                        writer.WriteUInt(networkId);
                        writer.WriteUShort(networkedObject.GetOrderIndex(this));

                        uint clientId = NetworkingManager.singleton.ConnectedClientsList[i].ClientId;
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
                            if (isDirty && (!isServer || networkedVarFields[k].CanClientRead(clientId)))
                            {
                                networkedVarFields[k].WriteDelta(writer);
                            }
                        }

                        if (isServer)
                            InternalMessageHandler.Send(clientId, "MLAPI_NETWORKED_VAR_DELTA", channelsForVarGroups[j], writer);
                        else
                            InternalMessageHandler.Send(NetworkingManager.singleton.NetworkConfig.NetworkTransport.ServerNetId, "MLAPI_NETWORKED_VAR_DELTA", channelsForVarGroups[j], writer);
                    }
                }
            }

            for (int i = 0; i < networkedVarFields.Count; i++)
            {
                networkedVarFields[i].ResetDirty();
            }
        }

        internal void HandleNetworkedVarDeltas(BitReader reader, uint clientId)
        {
            for (int i = 0; i < networkedVarFields.Count; i++)
            {
                if (!reader.ReadBool())
                    continue;

                if (isServer && !networkedVarFields[i].CanClientWrite(clientId))
                {
                    //This client wrote somewhere they are not allowed. This is critical
                    //We can't just skip this field. Because we don't actually know how to dummy read
                    //That is, we don't know how many bytes to skip. Because the interface doesn't have a 
                    //Read that gives us the value. Only a Read that applies the value straight away
                    //A dummy read COULD be added to the interface for this situation, but it's just being too nice.
                    //This is after all a developer fault. A critical error should be fine.
                    // - TwoTen
                    if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError("Client wrote to NetworkedVar without permission. No more variables can be read. This is critical");
                    return;
                }

                networkedVarFields[i].ReadDelta(reader);
            }
        }

        internal void HandleNetworkedVarUpdate(BitReader reader, uint clientId)
        {
            for (int i = 0; i < networkedVarFields.Count; i++)
            {
                if (!reader.ReadBool())
                    continue;

                if (isServer && !networkedVarFields[i].CanClientWrite(clientId))
                {
                    //This client wrote somewhere they are not allowed. This is critical
                    //We can't just skip this field. Because we don't actually know how to dummy read
                    //That is, we don't know how many bytes to skip. Because the interface doesn't have a 
                    //Read that gives us the value. Only a Read that applies the value straight away
                    //A dummy read COULD be added to the interface for this situation, but it's just being too nice.
                    //This is after all a developer fault. A critical error should be fine.
                    // - TwoTen
                    if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError("Client wrote to NetworkedVar without permission. No more variables can be read. This is critical");
                    return;
                }

                networkedVarFields[i].ReadField(reader);
            }
        }

        #endregion

        #region MESSAGING_SYSTEM
        private static readonly Dictionary<Type, Dictionary<ulong, ClientRPC>> CachedClientRpcs = new Dictionary<Type, Dictionary<ulong, ClientRPC>>();
        private static readonly Dictionary<Type, Dictionary<ulong, ServerRPC>> CachedServerRpcs = new Dictionary<Type, Dictionary<ulong, ServerRPC>>();
        private static readonly HashSet<Type> CachedTypes = new HashSet<Type>();
        private static readonly Dictionary<ulong, string> HashResults = new Dictionary<ulong, string>();

        private ulong HashMethodName(string name)
        {
            AttributeMessageMode mode = NetworkingManager.singleton.NetworkConfig.AttributeMessageMode;
            
            if (mode == AttributeMessageMode.WovenTwoByte)
                return name.GetStableHash16();
            if (mode == AttributeMessageMode.WovenFourByte)
                return name.GetStableHash32();
            if (mode == AttributeMessageMode.WovenEightByte)
                return name.GetStableHash64();

            return 0;
        }
        
        private void CacheAttributes()
        {
            Type type = GetType();
            if (CachedTypes.Contains(type)) return; //Already cached
            
            CachedTypes.Add(type);
            CachedClientRpcs.Add(type, new Dictionary<ulong, ClientRPC>());
            CachedServerRpcs.Add(type, new Dictionary<ulong, ServerRPC>());
            
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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
                    if (parameters.Length == 2 && parameters[0].ParameterType == typeof(uint) && parameters[1].ParameterType == typeof(BitReader))
                    {
                        //use delegate
                        attributes[0].rpcDelegate = (RpcDelegate)Delegate.CreateDelegate(typeof(RpcDelegate), this, methods[i], true);
                    }
                    else
                    {
                        attributes[0].reflectionMethod = new ReflectionMehtod(methods[i]);
                    }
                    
                    ulong hash = HashMethodName(methods[i].Name);
                    if (HashResults.ContainsKey(hash) && HashResults[hash] != methods[i].Name)
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError("Hash collision detected for RPC method. The method \"" + methods[i].Name + "\" collides with the method \"" + HashResults[hash] + "\". This can be solved by increasing the amount of bytes to use for hashing in the NetworkConfig or changing the name of one of the conflicting methods.");
                    }
                    else
                    {
                        HashResults.Add(hash, methods[i].Name);
                    }

                    CachedServerRpcs[type].Add(hash, attributes[0]);
                }
                
                if (methods[i].IsDefined(typeof(ClientRPC), true))
                {
                    ClientRPC[] attributes = (ClientRPC[])methods[i].GetCustomAttributes(typeof(ClientRPC), true);
                    if (attributes.Length > 1)
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Having more than 1 ClientRPC attribute per method is not supported.");
                    }

                    ParameterInfo[] parameters = methods[i].GetParameters();
                    if (parameters.Length == 2 && parameters[0].ParameterType == typeof(uint) && parameters[1].ParameterType == typeof(BitReader))
                    {
                        //use delegate
                        attributes[0].rpcDelegate = (RpcDelegate)Delegate.CreateDelegate(typeof(RpcDelegate), this, methods[i], true);
                    }
                    else
                    {
                        attributes[0].reflectionMethod = new ReflectionMehtod(methods[i]);
                    }
                    
                    CachedClientRpcs[type].Add(HashMethodName(methods[i].Name), attributes[0]);
                }     
            }
        }

        internal void OnRemoteServerRPC(ulong hash, uint senderClientId, BitReader reader)
        {
            if (!CachedServerRpcs.ContainsKey(GetType()) || !CachedServerRpcs[GetType()].ContainsKey(hash))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("ServerRPC request method not found");
                return;
            }
            InvokeServerRPCLocal(hash, senderClientId, reader);
        }
        
        internal void OnRemoteClientRPC(ulong hash, uint senderClientId, BitReader reader)
        {
            if (!CachedServerRpcs.ContainsKey(GetType()) || !CachedServerRpcs[GetType()].ContainsKey(hash))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("ServerRPC request method not found");
                return;
            }
            InvokeClientRPCLocal(hash, senderClientId, reader);
        }

        private void InvokeServerRPCLocal(ulong hash, uint senderClientId, BitReader reader)
        {
            if (!CachedServerRpcs.ContainsKey(GetType()) || !CachedServerRpcs[GetType()].ContainsKey(hash))
                return;
            
            ServerRPC rpc = CachedServerRpcs[GetType()][hash];

            if (rpc.RequireOwnership && senderClientId != OwnerClientId)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only owner can invoke ServerRPC that is marked to require ownership");
                return;
            }
            
            if (rpc.reflectionMethod != null)
            {
                rpc.reflectionMethod.Invoke(this, reader);
            }

            if (rpc.rpcDelegate != null)
            {
                rpc.rpcDelegate(senderClientId, reader);
            }
        }

        private void InvokeClientRPCLocal(ulong hash, uint senderClientId, BitReader reader)
        {
            if (!CachedClientRpcs.ContainsKey(GetType()) || !CachedClientRpcs[GetType()].ContainsKey(hash))
                return;
            
            ClientRPC rpc = CachedClientRpcs[GetType()][hash];
            
            if (rpc.reflectionMethod != null)
            {
                rpc.reflectionMethod.Invoke(this, reader);
            }

            if (rpc.rpcDelegate != null)
            {
                rpc.rpcDelegate(senderClientId, reader);
            }
        }
        
        //Technically boxed writes are not needed. But save LOC for the non performance sends.
        internal void SendServerRPCBoxed(ulong hash, params object[] parameters)
        {
            using (BitWriter writer = BitWriter.Get())
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    writer.WriteObject(parameters[i]);
                }
                SendServerRPCPerformance(hash, writer);
            }
        }
        
        internal void SendClientRPCBoxed(ulong hash, List<uint> clientIds, params object[] parameters)
        {
            using (BitWriter writer = BitWriter.Get())
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    writer.WriteObject(parameters[i]);
                }
                SendClientRPCPerformance(hash, clientIds, writer);
            }
        }
        
        internal void SendServerRPCPerformance(ulong hash, BitWriter writer)
        {
            if (!isClient)
            {
                //We are ONLY a server.
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only server and host can invoke ServerRPC");
                return;
            }

            if (isHost)
            {
                using (BitReader reader = BitReader.Get(writer.Finalize()))
                {
                    InvokeServerRPCLocal(hash, NetworkingManager.singleton.LocalClientId, reader);   
                }
            }
            
            InternalMessageHandler.Send(NetworkingManager.singleton.NetworkConfig.NetworkTransport.ServerNetId, "MLAPI_SERVER_RPC", "MLAPI_USER_CHANNEL", writer);
        }

        internal void SendClientRPCPerformance(ulong hash,  List<uint> clientIds, BitWriter writer)
        {
            if (!isServer)
            {
                //We are NOT a server.
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only clients and host can invoke ClientRPC");
                return;
            }
            
            for (int i = 0; i < clientIds.Count; i++)
            {
                if (isHost && clientIds[i] == NetworkingManager.singleton.LocalClientId)
                {
                    using (BitReader reader = BitReader.Get(writer.Finalize()))
                    {
                        InvokeClientRPCLocal(hash, NetworkingManager.singleton.LocalClientId, reader);   
                    }
                }
                else
                {
                    using (BitWriter rpcWriter = BitWriter.Get())
                    {
                        writer.WriteUInt(networkId);
                        writer.WriteUShort(networkedObject.GetOrderIndex(this));
                        writer.WriteULong(hash);
                        
                        writer.WriteWriter(writer);
                        
                        InternalMessageHandler.Send(clientIds[i], "MLAPI_CLIENT_RPC", "MLAPI_USER_CHANNEL", rpcWriter);
                    }
                }
            } 
        }
        #endregion

        #region SEND METHODS
        public delegate void Action<T1, T2, T3, T4, T5>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5);
        public delegate void Action<T1, T2, T3, T4, T5, T6>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6);
        public delegate void Action<T1, T2, T3, T4, T5, T6, T7>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7);
        public delegate void Action<T1, T2, T3, T4, T5, T6, T7, T8>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8);
        public delegate void Action<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9);
        public delegate void Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10);
       
        //BOXED
        public void InvokeServerRPC(Action method)
        {
            SendServerRPCBoxed(HashMethodName(method.Method.Name));
        }
        
        public void InvokeServerRPC<T1>(Action<T1> method, T1 t1)
        {
            SendServerRPCBoxed(HashMethodName(method.Method.Name), t1);
        }
        
        public void InvokeClientRPC(Action method, List<uint> clientIds)
        {
            SendClientRPCBoxed(HashMethodName(method.Method.Name), clientIds);
        }
        
        public void InvokeClientRPC<T1>(Action<T1> method, List<uint> clientIds, T1 t1)
        {
            SendClientRPCBoxed(HashMethodName(method.Method.Name), clientIds, t1);
        }
        
        //Performance
        public void InvokeServerRPC(RpcDelegate method, BitWriter writer)
        {
            SendServerRPCPerformance(HashMethodName(method.Method.Name), writer);
        }
        
        public void InvokeClientRPC(RpcDelegate method, BitWriter writer)
        {
            SendServerRPCPerformance(HashMethodName(method.Method.Name), writer);
        }
        
        public void InvokeServerRPC(string methodName, BitWriter writer)
        {
            SendServerRPCPerformance(HashMethodName(methodName), writer);
        }
        
        public void InvokeClientRPC(string methodName, List<uint> clientIds, BitWriter writer)
        {
            SendClientRPCPerformance(HashMethodName(methodName), clientIds, writer);
        }
        #endregion

        /// <summary>
        /// Gets the local instance of a object with a given NetworkId
        /// </summary>
        /// <param name="networkId"></param>
        /// <returns></returns>
        protected NetworkedObject GetNetworkedObject(uint networkId)
        {
            return SpawnManager.SpawnedObjects[networkId];
        }
    }
}
