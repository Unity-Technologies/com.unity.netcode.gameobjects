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
        public virtual void NetworkStart(BitReaderDeprecated payloadReader)
        {
            NetworkStart();
        }

        internal void InternalNetworkStart()
        {
            CacheAttributedMethods();
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

        internal Dictionary<ulong, MethodInfo> cachedMethods = new Dictionary<ulong, MethodInfo>();
        internal Dictionary<string, string> messageChannelName = new Dictionary<string, string>();

        private List<MethodInfo> getMethodsRecursive(Type type, List<MethodInfo> list = null) {
            if(list == null) {
                list = new List<MethodInfo>();
            }
            if(type == typeof(NetworkedBehaviour)) {
                return list;
            }
            list.AddRange(type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy));
            return getMethodsRecursive(type.BaseType, list); 
        }

        private void CacheAttributedMethods()
        {
            if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.Disabled)
                return;

            MethodInfo[] methods = getMethodsRecursive(GetType()).ToArray();
            foreach (MethodInfo method in methods)
            {
                if (method.IsDefined(typeof(Command), true) || method.IsDefined(typeof(ClientRpc), true) || method.IsDefined(typeof(TargetRpc), true))
                {
                    ulong hash = 0;

                    if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.WovenTwoByte)
                        hash = method.Name.GetStableHash16();
                    else if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.WovenFourByte)
                        hash = method.Name.GetStableHash32();
                    else if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.WovenEightByte)
                        hash = method.Name.GetStableHash64();

                    if (cachedMethods.ContainsKey(hash))
                    {
                        MethodInfo previous = cachedMethods[hash];
                        if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError(string.Format("Method {0} and {1} have the same hash.  Rename one of the methods or increase Attribute Message Mode", previous.Name, method.Name));
                    }
                    cachedMethods[hash] = method;
                }
                if (method.IsDefined(typeof(Command), true) && !messageChannelName.ContainsKey(method.Name))
                    messageChannelName.Add(method.Name, ((Command[])method.GetCustomAttributes(typeof(Command), true))[0].channelName);
                if (method.IsDefined(typeof(ClientRpc), true) && !messageChannelName.ContainsKey(method.Name))
                    messageChannelName.Add(method.Name, ((ClientRpc[])method.GetCustomAttributes(typeof(ClientRpc), true))[0].channelName);
                if (method.IsDefined(typeof(TargetRpc), true) && !messageChannelName.ContainsKey(method.Name))
                    messageChannelName.Add(method.Name, ((TargetRpc[])method.GetCustomAttributes(typeof(TargetRpc), true))[0].channelName);
            }
        }

        /// <summary>
        /// Calls a Command method on server
        /// </summary>
        /// <param name="methodName">Method name to invoke</param>
        /// <param name="methodParams">Method parameters to send</param>
        public void InvokeCommand(string methodName, params object[] methodParams)
        {
            if (OwnerClientId != NetworkingManager.singleton.LocalClientId && !isLocalPlayer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Cannot invoke command for object without ownership");
                return;
            }
            if (!methodName.StartsWith("Cmd"))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid Command name. Command methods have to start with Cmd");
                return;
            }
            if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.Disabled)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Calling InvokeCommand is not allowed when AttributeMessageMode is set to disabled");
                return;
            }

            ulong hash = 0;
            if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.WovenTwoByte)
                hash = methodName.GetStableHash16();
            else if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.WovenFourByte)
                hash = methodName.GetStableHash32();
            else if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.WovenEightByte)
                hash = methodName.GetStableHash64();

            if (NetworkingManager.singleton.isServer)
            {
                if (isHost)
                {
                    cachedMethods[hash].Invoke(this, methodParams);
                    return;
                }
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Cannot invoke commands from server");
                return;
            }

            using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
            {
                writer.WriteUInt(networkId);
                writer.WriteUShort(networkedObject.GetOrderIndex(this));
                writer.WriteULong(hash);
                for (int i = 0; i < methodParams.Length; i++)
                    FieldTypeHelper.WriteFieldType(writer, methodParams[i]);

                InternalMessageHandler.Send(NetworkingManager.singleton.NetworkConfig.NetworkTransport.ServerNetId, "MLAPI_COMMAND", messageChannelName[methodName], writer, null);
            }
        }

        /// <summary>
        /// Calls a ClientRpc method on all clients
        /// </summary>
        /// <param name="methodName">Method name to invoke</param>
        /// <param name="methodParams">Method parameters to send</param>
        public void InvokeClientRpc(string methodName, params object[] methodParams)
        {
            if (!NetworkingManager.singleton.isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Cannot invoke ClientRpc from client");
                return;
            }
            if (!methodName.StartsWith("Rpc"))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid Command name. Command methods have to start with Cmd");
                return;
            }
            if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.Disabled)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Calling InvokeClientRpc is not allowed when AttributeMessageMode is set to disabled");
                return;
            }

            ulong hash = 0;
            if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.WovenTwoByte)
                hash = methodName.GetStableHash16();
            else if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.WovenFourByte)
                hash = methodName.GetStableHash32();
            else if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.WovenEightByte)
                hash = methodName.GetStableHash64();

            if (isHost) cachedMethods[hash].Invoke(this, methodParams);

            using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
            {
                writer.WriteUInt(networkId);
                writer.WriteUShort(networkedObject.GetOrderIndex(this));
                writer.WriteULong(hash);

                for (int i = 0; i < methodParams.Length; i++)
                    FieldTypeHelper.WriteFieldType(writer, methodParams[i]);
                InternalMessageHandler.Send("MLAPI_RPC", messageChannelName[methodName], writer, networkId);
            }
        }

        /// <summary>
        /// Calls a TargetRpc method on the owner client
        /// </summary>
        /// <param name="methodName">Method name to invoke</param>
        /// <param name="methodParams">Method parameters to send</param>
        public void InvokeTargetRpc(string methodName, params object[] methodParams)
        {
            if (!NetworkingManager.singleton.isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Cannot invoke ClientRpc from client");
                return;
            }
            if (!methodName.StartsWith("Target"))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid Command name. Command methods have to start with Cmd");
                return;
            }
            if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.Disabled)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Calling InvokeTargetRpc is not allowed when AttributeMessageMode is set to disabled");
                return;
            }

            ulong hash = 0;
            if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.WovenTwoByte)
                hash = methodName.GetStableHash16();
            else if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.WovenFourByte)
                hash = methodName.GetStableHash32();
            else if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.WovenEightByte)
                hash = methodName.GetStableHash64();

            if (isHost) cachedMethods[hash].Invoke(this, methodParams);

            using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
            {
                writer.WriteUInt(networkId);
                writer.WriteUShort(networkedObject.GetOrderIndex(this));
                writer.WriteULong(hash);
                for (int i = 0; i < methodParams.Length; i++)
                    FieldTypeHelper.WriteFieldType(writer, methodParams[i]);
                InternalMessageHandler.Send(OwnerClientId, "MLAPI_RPC", messageChannelName[methodName], writer, networkId);
            }
        }

        #region ActionInvokes
        /// <summary>
        /// Calls a Command method on server
        /// </summary>
        /// <param name="method">Method to invoke</param>
        public void InvokeCommand(Action method) {
            InvokeCommand(method.Method.Name);
        }
        /// <summary>
        /// Calls a Command method on server
        /// </summary>
        /// <param name="method">Method to invoke</param>
        /// <param name="p1">Method parameter to send</param>
        public void InvokeCommand<T1>(Action<T1> method, T1 p1) {
            InvokeCommand(method.Method.Name, new object[] { p1 });
        }
        /// <summary>
        /// Calls a Command method on server
        /// </summary>
        /// <param name="method">Method to invoke</param>
        /// <param name="p1">Method parameter to send</param>
        /// <param name="p2">Method parameter to send</param>
        public void InvokeCommand<T1, T2>(Action<T1, T2> method, T1 p1, T2 p2) {
            InvokeCommand(method.Method.Name, new object[] { p1, p2});
        }
        /// <summary>
        /// Calls a Command method on server
        /// </summary>
        /// <param name="method">Method to invoke</param>
        /// <param name="p1">Method parameter to send</param>
        /// <param name="p2">Method parameter to send</param>
        /// <param name="p3">Method parameter to send</param>
        public void InvokeCommand<T1, T2, T3>(Action<T1, T2, T3> method, T1 p1, T2 p2, T3 p3) {
            InvokeCommand(method.Method.Name, new object[] { p1, p2, p3 });
        }
        /// <summary>
        /// Calls a Command method on server
        /// </summary>
        /// <param name="method">Method to invoke</param>
        /// <param name="p1">Method parameter to send</param>
        /// <param name="p2">Method parameter to send</param>
        /// <param name="p3">Method parameter to send</param>
        /// <param name="p4">Method parameter to send</param>
        public void InvokeCommand<T1, T2, T3, T4>(Action<T1, T2, T3, T4> method, T1 p1, T2 p2, T3 p3, T4 p4) {
            InvokeCommand(method.Method.Name, new object[] { p1, p2, p3, p4 });
        }

        /// <summary>
        /// Calls a ClientRpc method on all clients
        /// </summary>
        /// <param name="method">Method to invoke</param>
        public void InvokeClientRpc(Action method) {
            InvokeClientRpc(method.Method.Name);
        }
        /// <summary>
        /// Calls a ClientRpc method on all clients
        /// </summary>
        /// <param name="method">Method to invoke</param>
        /// <param name="p1">Method parameter to send</param>
        public void InvokeClientRpc<T1>(Action<T1> method, T1 p1) {
            InvokeClientRpc(method.Method.Name, new object[] { p1 });
        }
        /// <summary>
        /// Calls a ClientRpc method on all clients
        /// </summary>
        /// <param name="method">Method to invoke</param>
        /// <param name="p1">Method parameter to send</param>
        /// <param name="p2">Method parameter to send</param>
        public void InvokeClientRpc<T1, T2>(Action<T1, T2> method, T1 p1, T2 p2) {
            InvokeClientRpc(method.Method.Name, new object[] { p1, p2 });
        }
        /// <summary>
        /// Calls a ClientRpc method on all clients
        /// </summary>
        /// <param name="method">Method to invoke</param>
        /// <param name="p1">Method parameter to send</param>
        /// <param name="p2">Method parameter to send</param>
        /// <param name="p3">Method parameter to send</param>
        public void InvokeClientRpc<T1, T2, T3>(Action<T1, T2, T3> method, T1 p1, T2 p2, T3 p3) {
            InvokeClientRpc(method.Method.Name, new object[] { p1, p2, p3 });
        }
        /// <summary>
        /// Calls a ClientRpc method on all clients
        /// </summary>
        /// <param name="method">Method to invoke</param>
        /// <param name="p1">Method parameter to send</param>
        /// <param name="p2">Method parameter to send</param>
        /// <param name="p3">Method parameter to send</param>
        /// <param name="p4">Method parameter to send</param>
        public void InvokeClientRpc<T1, T2, T3, T4>(Action<T1, T2, T3, T4> method, T1 p1, T2 p2, T3 p3, T4 p4) {
            InvokeClientRpc(method.Method.Name, new object[] { p1, p2, p3, p4 });
        }

        /// <summary>
        /// Calls a TargetRpc method on the owner client
        /// </summary>
        /// <param name="method">Method to invoke</param>
        public void InvokeTargetRpc(Action method) {
            InvokeTargetRpc(method.Method.Name);
        }
        /// <summary>
        /// Calls a TargetRpc method on the owner client
        /// </summary>
        /// <param name="p1">Method parameter to send</param>
        /// <param name="method">Method to invoke</param>
        public void InvokeTargetRpc<T1>(Action<T1> method, T1 p1) {
            InvokeTargetRpc(method.Method.Name, new object[] { p1 });
        }
        /// <summary>
        /// Calls a TargetRpc method on the owner client
        /// </summary>
        /// <param name="p1">Method parameter to send</param>
        /// <param name="p2">Method parameter to send</param>
        /// <param name="method">Method to invoke</param>
        public void InvokeTargetRpc<T1, T2>(Action<T1, T2> method, T1 p1, T2 p2) {
            InvokeTargetRpc(method.Method.Name, new object[] { p1, p2 });
        }
        /// <summary>
        /// Calls a TargetRpc method on the owner client
        /// </summary>
        /// <param name="p1">Method parameter to send</param>
        /// <param name="p2">Method parameter to send</param>
        /// <param name="p3">Method parameter to send</param>
        /// <param name="method">Method to invoke</param>
        public void InvokeTargetRpc<T1, T2, T3>(System.Action<T1, T2, T3> method, T1 p1, T2 p2, T3 p3) {
            InvokeTargetRpc(method.Method.Name, new object[] { p1, p2, p3 });
        }
        /// <summary>
        /// Calls a TargetRpc method on the owner client
        /// </summary>
        /// <param name="p1">Method parameter to send</param>
        /// <param name="p2">Method parameter to send</param>
        /// <param name="p3">Method parameter to send</param>
        /// <param name="p4">Method parameter to send</param>
        /// <param name="method">Method to invoke</param>
        public void InvokeTargetRpc<T1, T2, T3, T4>(System.Action<T1, T2, T3, T4> method, T1 p1, T2 p2, T3 p3, T4 p4) {
            InvokeTargetRpc(method.Method.Name, new object[] { p1, p2, p3, p4 });
        }
        #endregion

        /// <summary>
        /// Registers a message handler
        /// </summary>
        /// <param name="name">The MessageType to register</param>
        /// <param name="action">The callback to get invoked whenever a message is received</param>
        /// <returns>HandlerId for the messageHandler that can be used to deregister the messageHandler</returns>
        protected int RegisterMessageHandler(string name, Action<uint, BitReaderDeprecated> action)
        {
            if (!MessageManager.messageTypes.ContainsKey(name))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The messageType " + name + " is not registered");
                return -1;
            }
            ushort messageType = MessageManager.messageTypes[name];
            ushort behaviourOrder = networkedObject.GetOrderIndex(this);

            if (!networkedObject.targetMessageActions.ContainsKey(behaviourOrder))
                networkedObject.targetMessageActions.Add(behaviourOrder, new Dictionary<ushort, Action<uint, BitReaderDeprecated>>());
            if (networkedObject.targetMessageActions[behaviourOrder].ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Each NetworkedBehaviour can only register one callback per instance per message type");
                return -1;
            }

            networkedObject.targetMessageActions[behaviourOrder].Add(messageType, action);
            int counter = MessageManager.AddIncomingMessageHandler(name, action);
            registeredMessageHandlers.Add(name, counter);
            return counter;
        }

        /// <summary>
        /// Registers a message handler
        /// </summary>
        /// <param name="name">The MessageType to register</param>
        /// <param name="action">The callback to get invoked whenever a message is received</param>
        /// <returns>HandlerId for the messageHandler that can be used to deregister the messageHandler</returns>
        [Obsolete("The overload (uint, byte[]) for RegisterMessageHandler is obsolete, use (uint, BitReader) instead")]
        protected int RegisterMessageHandler(string name, Action<uint, byte[]> action)
        {
            if (!MessageManager.messageTypes.ContainsKey(name))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The messageType " + name + " is not registered");
                return -1;
            }
            ushort messageType = MessageManager.messageTypes[name];
            ushort behaviourOrder = networkedObject.GetOrderIndex(this);

            if (!networkedObject.targetMessageActions.ContainsKey(behaviourOrder))
                networkedObject.targetMessageActions.Add(behaviourOrder, new Dictionary<ushort, Action<uint, BitReaderDeprecated>>());
            if (networkedObject.targetMessageActions[behaviourOrder].ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Each NetworkedBehaviour can only register one callback per instance per message type");
                return -1;
            }

            void convertedAction(uint clientId, BitReaderDeprecated reader)
            {
                action.Invoke(clientId, reader.ReadByteArray());
            }

            networkedObject.targetMessageActions[behaviourOrder].Add(messageType, convertedAction);
            int counter = MessageManager.AddIncomingMessageHandler(name, convertedAction);
            registeredMessageHandlers.Add(name, counter);
            return counter;
        }

        /// <summary>
        /// Deserializes a message that has been serialized by the BinarySerializer. This is the same as calling BinarySerializer.Deserialize
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="binary">The serialized version</param>
        /// <returns>Instance of type</returns>
        protected T DeserializeMessage<T>(byte[] binary) where T : new()
        {
            return BinarySerializer.Deserialize<T>(binary);
        }

        /// <summary>
        /// Deregisters a given message handler
        /// </summary>
        /// <param name="name">The MessageType to deregister</param>
        /// <param name="counter">The messageHandlerId to deregister</param>
        protected void DeregisterMessageHandler(string name, int counter)
        {
            MessageManager.RemoveIncomingMessageHandler(name, counter);
            ushort messageType = MessageManager.messageTypes[name];
            ushort behaviourOrder = networkedObject.GetOrderIndex(this);

            if (networkedObject.targetMessageActions.ContainsKey(behaviourOrder) && 
                networkedObject.targetMessageActions[behaviourOrder].ContainsKey(messageType))
            {
                networkedObject.targetMessageActions[behaviourOrder].Remove(messageType);
            }
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
            foreach (KeyValuePair<string, int> pair in registeredMessageHandlers)
            {
                DeregisterMessageHandler(pair.Key, pair.Value);
            }

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
                            InternalMessageHandler.Send(clientId, "MLAPI_NETWORKED_VAR_DELTA", channelsForVarGroups[j], writer, null);
                        else
                            InternalMessageHandler.Send(NetworkingManager.singleton.NetworkConfig.NetworkTransport.ServerNetId, "MLAPI_NETWORKED_VAR_DELTA", channelsForVarGroups[j], writer, null);
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

        #region SEND METHODS
        /// <summary>
        /// Sends a buffer to the server from client
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        protected void SendToServer(string messageType, string channelName, byte[] data)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Server can not send messages to server");
                return;
            }
            using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(NetworkingManager.singleton.NetworkConfig.NetworkTransport.ServerNetId, messageType, channelName, writer, null);
            }
        }

        /// <summary>
        /// Sends a buffer to the server from client
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        protected void SendToServer(string messageType, string channelName, BitWriterDeprecated writer)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Server can not send messages to server");
                return;
            }
            InternalMessageHandler.Send(NetworkingManager.singleton.NetworkConfig.NetworkTransport.ServerNetId, messageType, channelName, writer, null);
        }

        /// <summary>
        /// Sends a binary serialized class to the server from client 
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="instance">The instance to send</param>
        protected void SendToServer<T>(string messageType, string channelName, T instance)
        {
            SendToServer(messageType, channelName, BinarySerializer.Serialize<T>(instance));
        }

        /// <summary>
        /// Sends a buffer to the server from client. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        protected void SendToServerTarget(string messageType, string channelName, byte[] data)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Server can not send messages to server");
                return;
            }
            using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(NetworkingManager.singleton.NetworkConfig.NetworkTransport.ServerNetId, messageType, channelName, writer, networkId, networkedObject.GetOrderIndex(this));
            }
        }

        /// <summary>
        /// Sends a buffer to the server from client. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        protected void SendToServerTarget(string messageType, string channelName, BitWriterDeprecated writer)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Server can not send messages to server");
                return;
            }
            InternalMessageHandler.Send(NetworkingManager.singleton.NetworkConfig.NetworkTransport.ServerNetId, messageType, channelName, writer, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
        /// Sends a binary serialized class to the server from client. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="instance">The instance to send</param>
        protected void SendToServerTarget<T>(string messageType, string channelName, T instance)
        {
            SendToServerTarget(messageType, channelName, BinarySerializer.Serialize<T>(instance));
        }

        /// <summary>
        /// Sends a buffer to the client that owns this object from the server.
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToLocalClient(string messageType, string channelName, byte[] data)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageHashSet.Contains(MessageManager.messageTypes[messageType])))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }

            using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(OwnerClientId, messageType, channelName, writer);
            }
        }

        /// <summary>
        /// Sends a buffer to the client that owns this object from the server.
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToLocalClient(string messageType, string channelName, BitWriterDeprecated writer)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageHashSet.Contains(MessageManager.messageTypes[messageType])))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }
            InternalMessageHandler.Send(OwnerClientId, messageType, channelName, writer);
        }

        /// <summary>
        /// Sends a binary serialized class to the client that owns this object from the server.
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToLocalClient<T>(string messageType, string channelName, T instance)
        {
            SendToLocalClient(messageType, channelName, BinarySerializer.Serialize<T>(instance));
        }

        /// <summary>
        /// Sends a buffer to the client that owns this object from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        protected void SendToLocalClientTarget(string messageType, string channelName, byte[] data)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageHashSet.Contains(MessageManager.messageTypes[messageType])))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }
            using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(OwnerClientId, messageType, channelName, writer, networkId, networkedObject.GetOrderIndex(this));
            }
        }

        /// <summary>
        /// Sends a buffer to the client that owns this object from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        protected void SendToLocalClientTarget(string messageType, string channelName, BitWriterDeprecated writer)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageHashSet.Contains(MessageManager.messageTypes[messageType])))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }
            InternalMessageHandler.Send(OwnerClientId, messageType, channelName, writer, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>gh
        /// Sends a buffer to the client that owns this object from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        protected void SendToLocalClientTarget<T>(string messageType, string channelName, T instance)
        {
            SendToLocalClientTarget(messageType, channelName, BinarySerializer.Serialize<T>(instance));
        }

        /// <summary>
        /// Sends a buffer to all clients except to the owner object from the server
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToNonLocalClients(string messageType, string channelName, byte[] data)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }
            using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(messageType, channelName, writer, OwnerClientId, null, null);
            }
        }

        /// <summary>
        /// Sends a buffer to all clients except to the owner object from the server
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToNonLocalClients(string messageType, string channelName, BitWriterDeprecated writer)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }
            InternalMessageHandler.Send(messageType, channelName, writer);
        }

        /// <summary>
        /// Sends a binary serialized class to all clients except to the owner object from the server
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToNonLocalClients<T>(string messageType, string channelName, T instance)
        {
            SendToNonLocalClients(messageType, channelName, BinarySerializer.Serialize<T>(instance));
        }

        /// <summary>
        /// Sends a buffer to all clients except to the owner object from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToNonLocalClientsTarget(string messageType, string channelName, byte[] data)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }
            using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(messageType, channelName, writer, OwnerClientId, networkId, networkedObject.GetOrderIndex(this));
            }
        }

        /// <summary>
        /// Sends a buffer to all clients except to the owner object from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToNonLocalClientsTarget(string messageType, string channelName, BitWriterDeprecated writer)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }
            InternalMessageHandler.Send(messageType, channelName, writer, OwnerClientId, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
        /// Sends a binary serialized class to all clients except to the owner object from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToNonLocalClientsTarget<T>(string messageType, string channelName, T instance)
        {
            SendToNonLocalClientsTarget(messageType, channelName, BinarySerializer.Serialize<T>(instance));
        }

        /// <summary>
        /// Sends a buffer to a client with a given clientId from Server
        /// </summary>
        /// <param name="clientId">The clientId to send the message to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClient(uint clientId, string messageType, string channelName, byte[] data)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageHashSet.Contains(MessageManager.messageTypes[messageType])))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }

            using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(clientId, messageType, channelName, writer);
            }
        }


        /// <summary>
        /// Sends a buffer to a client with a given clientId from Server
        /// </summary>
        /// <param name="clientId">The clientId to send the message to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClient(uint clientId, string messageType, string channelName, BitWriterDeprecated writer)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageHashSet.Contains(MessageManager.messageTypes[messageType])))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }

            InternalMessageHandler.Send(clientId, messageType, channelName, writer);
        }

        /// <summary>
        /// Sends a binary serialized class to a client with a given clientId from Server
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="clientId">The clientId to send the message to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClient<T>(int clientId, string messageType, string channelName, T instance)
        {
            SendToClient(clientId, messageType, channelName, BinarySerializer.Serialize<T>(instance));
        }

        /// <summary>
        /// Sends a buffer to a client with a given clientId from Server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="clientId">The clientId to send the message to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientTarget(uint clientId, string messageType, string channelName, byte[] data)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageHashSet.Contains(MessageManager.messageTypes[messageType])))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }

            using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(clientId, messageType, channelName, writer, networkId, networkedObject.GetOrderIndex(this));
            }
        }

        /// <summary>
        /// Sends a buffer to a client with a given clientId from Server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="clientId">The clientId to send the message to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientTarget(uint clientId, string messageType, string channelName, BitWriterDeprecated writer)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageHashSet.Contains(MessageManager.messageTypes[messageType])))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }
            InternalMessageHandler.Send(clientId, messageType, channelName, writer, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
        /// Sends a buffer to a client with a given clientId from Server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="clientId">The clientId to send the message to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientTarget<T>(int clientId, string messageType, string channelName, T instance)
        {
            SendToClientTarget(clientId, messageType, channelName, BinarySerializer.Serialize<T>(instance));
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClients(uint[] clientIds, string messageType, string channelName, byte[] data)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }

            using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(clientIds, messageType, channelName, writer);
            }
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClients(uint[] clientIds, string messageType, string channelName, BitWriterDeprecated writer)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }

            InternalMessageHandler.Send(clientIds, messageType, channelName, writer);
        }

        /// <summary>
        /// Sends a binary serialized class to multiple clients from the server
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClients<T>(int[] clientIds, string messageType, string channelName, T instance)
        {
            SendToClients(clientIds, messageType, channelName, BinarySerializer.Serialize<T>(instance));
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientsTarget(uint[] clientIds, string messageType, string channelName, byte[] data)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }

            using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(clientIds, messageType, channelName, writer, networkId, networkedObject.GetOrderIndex(this));
            }
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientsTarget(uint[] clientIds, string messageType, string channelName, BitWriterDeprecated writer)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }
            InternalMessageHandler.Send(clientIds, messageType, channelName, writer, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientsTarget<T>(int[] clientIds, string messageType, string channelName, T instance)
        {
            SendToClientsTarget(clientIds, messageType, channelName, BinarySerializer.Serialize<T>(instance));
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClients(List<uint> clientIds, string messageType, string channelName, byte[] data)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }

            using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(clientIds, messageType, channelName, writer);
            }
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClients(List<uint> clientIds, string messageType, string channelName, BitWriterDeprecated writer)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }

            InternalMessageHandler.Send(clientIds, messageType, channelName, writer);
        }

        /// <summary>
        /// Sends a binary serialized class to multiple clients from the server
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClients<T>(List<int> clientIds, string messageType, string channelName, T instance)
        {
            SendToClients(clientIds, messageType, channelName, BinarySerializer.Serialize<T>(instance));
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientsTarget(List<uint> clientIds, string messageType, string channelName, byte[] data)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }

            using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(clientIds, messageType, channelName, writer, networkId, networkedObject.GetOrderIndex(this));
            }
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientsTarget(List<uint> clientIds, string messageType, string channelName, BitWriterDeprecated writer)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }

            InternalMessageHandler.Send(clientIds, messageType, channelName, writer, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientsTarget<T>(List<uint> clientIds, string messageType, string channelName, T instance)
        {
            SendToClientsTarget(clientIds, messageType, channelName, BinarySerializer.Serialize<T>(instance));
        }

        /// <summary>
        /// Sends a buffer to all clients from the server
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClients(string messageType, string channelName, byte[] data)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }

            using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(messageType, channelName, writer);
            }
        }

        /// <summary>
        /// Sends a buffer to all clients from the server
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClients(string messageType, string channelName, BitWriterDeprecated writer)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }

            InternalMessageHandler.Send(messageType, channelName, writer);
        }

        /// <summary>
        /// Sends a buffer to all clients from the server
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClients<T>(string messageType, string channelName, T instance)
        {
            SendToClients(messageType, channelName, BinarySerializer.Serialize<T>(instance));
        }

        /// <summary>
        /// Sends a buffer to all clients from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientsTarget(string messageType, string channelName, byte[] data)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }

            using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(messageType, channelName, writer, networkId, networkedObject.GetOrderIndex(this));
            }
        }

        /// <summary>
        /// Sends a buffer to all clients from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientsTarget(string messageType, string channelName, BitWriterDeprecated writer)
        {
            if (!MessageManager.messageTypes.ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid message type \"" + channelName + "\"");
                return;
            }
            if (MessageManager.messageTypes[messageType] < 32)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sending messages from client to other clients is not yet supported");
                return;
            }
            InternalMessageHandler.Send(messageType, channelName, writer, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
        /// Sends a buffer to all clients from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientsTarget<T>(string messageType, string channelName, T instance)
        {
            SendToClientsTarget(messageType, channelName, BinarySerializer.Serialize<T>(instance));
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
