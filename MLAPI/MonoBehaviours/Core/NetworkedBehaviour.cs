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
        public bool isLocalPlayer
        {
            get
            {
                return networkedObject.isLocalPlayer;
            }
        }
        /// <summary>
        /// Gets if the object is owned by the local player
        /// </summary>
        public bool isOwner
        {
            get
            {
                return networkedObject.isOwner;
            }
        }
        /// <summary>
        /// Gets if we are executing as server
        /// </summary>
        protected bool isServer
        {
            get
            {
                return NetworkingManager.singleton.isServer;
            }
        }
        /// <summary>
        /// Gets if we are executing as client
        /// </summary>
        protected bool isClient
        {
            get
            {
                return NetworkingManager.singleton.isClient;
            }
        }
        /// <summary>
        /// Gets if we are executing as Host, I.E Server and Client
        /// </summary>
        protected bool isHost
        {
            get
            {
                return NetworkingManager.singleton.isHost;
            }
        }
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
        public uint networkId
        {
            get
            {
                return networkedObject.NetworkId;
            }
        }
        /// <summary>
        /// Gets the clientId that owns the NetworkedObject
        /// </summary>
        public uint ownerClientId
        {
            get
            {
                return networkedObject.OwnerClientId;
            }
        }

        //Change data type
        private Dictionary<string, int> registeredMessageHandlers = new Dictionary<string, int>();

        private void OnEnable()
        {
            if (_networkedObject == null)
                _networkedObject = GetComponentInParent<NetworkedObject>();

            if (NetworkingManager.singleton != null)
                CacheAttributedMethods();
            else
                NetworkingManager.onSingletonSet += CacheAttributedMethods;

            NetworkedObject.NetworkedBehaviours.Add(this);
        }

        internal bool networkedStartInvoked = false;
        /// <summary>
        /// Gets called when message handlers are ready to be registered and the networking is setup
        /// </summary>
        public virtual void NetworkStart()
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

        internal Dictionary<string, MethodInfo> cachedMethods = new Dictionary<string, MethodInfo>();
        internal Dictionary<string, string> messageChannelName = new Dictionary<string, string>();

        /// <summary>
        /// Called when a new client connects
        /// </summary>
        /// <param name="clientId">The clientId of the new client</param>
        /// <returns>Wheter or not the object should be visible</returns>
        public virtual bool OnCheckObserver(uint clientId)
        {
            return true;
        }

        /// <summary>
        /// Called when observers are to be rebuilt
        /// </summary>
        /// <param name="observers">The observers to use</param>
        /// <returns>Wheter or not we changed anything</returns>
        public virtual bool OnRebuildObservers(HashSet<uint> observers)
        {
            return false;
        }

        /// <summary>
        /// Triggers a "OnRebuildObservers" and updates the observers
        /// </summary>
        public void RebuildObservers()
        {
            networkedObject.RebuildObservers();
        }

        /// <summary>
        /// Invoked when visibility changes
        /// </summary>
        /// <param name="visible"></param>
        public virtual void OnSetLocalVisibility(bool visible)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].enabled = visible;
        }

        private void CacheAttributedMethods()
        {
            if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.Disabled)
                return;

            MethodInfo[] methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].IsDefined(typeof(Command), true) || methods[i].IsDefined(typeof(ClientRpc), true) || methods[i].IsDefined(typeof(TargetRpc), true))
                {
                    Data.Cache.RegisterMessageAttributeName(methods[i].Name, NetworkingManager.singleton.NetworkConfig.AttributeMessageMode);
                    if (!cachedMethods.ContainsKey(methods[i].Name))
                        cachedMethods.Add(methods[i].Name, methods[i]);
                }
                if (methods[i].IsDefined(typeof(Command), true) && !messageChannelName.ContainsKey(methods[i].Name))
                    messageChannelName.Add(methods[i].Name, ((Command[])methods[i].GetCustomAttributes(typeof(Command), true))[0].channelName);
                if (methods[i].IsDefined(typeof(ClientRpc), true) && !messageChannelName.ContainsKey(methods[i].Name))
                    messageChannelName.Add(methods[i].Name, ((ClientRpc[])methods[i].GetCustomAttributes(typeof(ClientRpc), true))[0].channelName);
                if (methods[i].IsDefined(typeof(TargetRpc), true) && !messageChannelName.ContainsKey(methods[i].Name))
                    messageChannelName.Add(methods[i].Name, ((TargetRpc[])methods[i].GetCustomAttributes(typeof(TargetRpc), true))[0].channelName);
            }
        }

        /// <summary>
        /// Calls a Command method on server
        /// </summary>
        /// <param name="methodName">Method name to invoke</param>
        /// <param name="methodParams">Method parameters to send</param>
        protected void InvokeCommand(string methodName, params object[] methodParams)
        {
            if (NetworkingManager.singleton.isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Cannot invoke commands from server");
                return;
            }
            if (ownerClientId != NetworkingManager.singleton.MyClientId)
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

            ulong hash = Data.Cache.GetMessageAttributeHash(methodName, NetworkingManager.singleton.NetworkConfig.AttributeMessageMode);
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteUInt(networkId);
                writer.WriteUShort(networkedObject.GetOrderIndex(this));
                writer.WriteULong(hash);
                writer.WriteBits((byte)methodParams.Length, 5);
                for (int i = 0; i < methodParams.Length; i++)
                {
                    FieldType fieldType = FieldTypeHelper.GetFieldType(methodParams[i].GetType());
                    writer.WriteBits((byte)fieldType, 5);
                    FieldTypeHelper.WriteFieldType(writer, methodParams[i], fieldType);
                }

                InternalMessageHandler.Send(NetworkingManager.singleton.NetworkConfig.NetworkTransport.ServerNetId, "MLAPI_COMMAND", messageChannelName[methodName], writer, null);
            }
        }

        /// <summary>
        /// Calls a ClientRpc method on all clients
        /// </summary>
        /// <param name="methodName">Method name to invoke</param>
        /// <param name="methodParams">Method parameters to send</param>
        protected void InvokeClientRpc(string methodName, params object[] methodParams)
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

            ulong hash = Data.Cache.GetMessageAttributeHash(methodName, NetworkingManager.singleton.NetworkConfig.AttributeMessageMode);
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteUInt(networkId);
                writer.WriteUShort(networkedObject.GetOrderIndex(this));
                writer.WriteULong(hash);
                writer.WriteBits((byte)methodParams.Length, 5);

                for (int i = 0; i < methodParams.Length; i++)
                {
                    FieldType fieldType = FieldTypeHelper.GetFieldType(methodParams[i].GetType());
                    writer.WriteBits((byte)fieldType, 5);
                    FieldTypeHelper.WriteFieldType(writer, methodParams[i], fieldType);
                }
                InternalMessageHandler.Send("MLAPI_RPC", messageChannelName[methodName], writer, networkId);
            }
        }

        /// <summary>
        /// Calls a TargetRpc method on the owner client
        /// </summary>
        /// <param name="methodName">Method name to invoke</param>
        /// <param name="methodParams">Method parameters to send</param>
        protected void InvokeTargetRpc(string methodName, params object[] methodParams)
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

            ulong hash = Data.Cache.GetMessageAttributeHash(methodName, NetworkingManager.singleton.NetworkConfig.AttributeMessageMode);
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteUInt(networkId);
                writer.WriteUShort(networkedObject.GetOrderIndex(this));
                writer.WriteULong(hash);
                writer.WriteBits((byte)methodParams.Length, 5);
                for (int i = 0; i < methodParams.Length; i++)
                {
                    FieldType fieldType = FieldTypeHelper.GetFieldType(methodParams[i].GetType());
                    writer.WriteBits((byte)fieldType, 5);
                    FieldTypeHelper.WriteFieldType(writer, methodParams[i], fieldType);
                }
                InternalMessageHandler.Send(ownerClientId, "MLAPI_RPC", messageChannelName[methodName], writer, networkId);
            }
        }

        /// <summary>
        /// Registers a message handler
        /// </summary>
        /// <param name="name">The MessageType to register</param>
        /// <param name="action">The callback to get invoked whenever a message is received</param>
        /// <returns>HandlerId for the messageHandler that can be used to deregister the messageHandler</returns>
        protected int RegisterMessageHandler(string name, Action<uint, BitReader> action)
        {
            if (!MessageManager.messageTypes.ContainsKey(name))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The messageType " + name + " is not registered");
                return -1;
            }
            ushort messageType = MessageManager.messageTypes[name];
            ushort behaviourOrder = networkedObject.GetOrderIndex(this);

            if (!networkedObject.targetMessageActions.ContainsKey(behaviourOrder))
                networkedObject.targetMessageActions.Add(behaviourOrder, new Dictionary<ushort, Action<uint, BitReader>>());
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
                networkedObject.targetMessageActions.Add(behaviourOrder, new Dictionary<ushort, Action<uint, BitReader>>());
            if (networkedObject.targetMessageActions[behaviourOrder].ContainsKey(messageType))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Each NetworkedBehaviour can only register one callback per instance per message type");
                return -1;
            }

            Action<uint, BitReader> convertedAction = (clientId, reader) =>
            {
                action.Invoke(clientId, reader.ReadByteArray());
            };

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
            NetworkedObject.NetworkedBehaviours.Remove(this);
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<string, int> pair in registeredMessageHandlers)
            {
                DeregisterMessageHandler(pair.Key, pair.Value);
            }
        }

        #region SYNC_VAR
        internal List<SyncedVarField> syncedVarFields = new List<SyncedVarField>();
        private HashSet<uint> OutOfSyncClients = new HashSet<uint>();
        private bool syncVarInit = false;
        internal bool[] syncMask;
        internal void SyncVarInit()
        {
            if (syncVarInit)
                return;
            syncVarInit = true;
            FieldInfo[] sortedFields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Instance).OrderBy(x => x.Name).ToArray();
            for (int i = 0; i < sortedFields.Length; i++)
            {
                if(sortedFields[i].IsDefined(typeof(SyncedVar), true))
                {
                    SyncedVar attribute = ((SyncedVar)sortedFields[i].GetCustomAttributes(typeof(SyncedVar), true)[0]);

                    MethodInfo hookMethod = null;

                    if (!string.IsNullOrEmpty(attribute.hookMethodName))
                        hookMethod = GetType().GetMethod(attribute.hookMethodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    
                    FieldType fieldType = FieldTypeHelper.GetFieldType(sortedFields[i].FieldType);
                    if (fieldType != FieldType.Invalid)
                    {
                        syncedVarFields.Add(new SyncedVarField()
                        {
                            Dirty = false,
                            Target = attribute.target,
                            FieldInfo = sortedFields[i],
                            FieldType = fieldType,
                            FieldValue = sortedFields[i].GetValue(this),
                            HookMethod = hookMethod,
                            Attribute = attribute
                        });
                    }
                    else
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogError("MLAPI: The type " + sortedFields[i].FieldType.ToString() + " can not be used as a syncvar");
                    }
                }
            }
            syncMask = new bool[syncedVarFields.Count];
        }

        internal void OnSyncVarUpdate(object value, int fieldIndex)
        {
            syncedVarFields[fieldIndex].FieldInfo.SetValue(this, value);
            if (syncedVarFields[fieldIndex].HookMethod != null)
                syncedVarFields[fieldIndex].HookMethod.Invoke(this, null);
        }

        internal void FlushToClient(uint clientId)
        {
            //This NetworkedBehaviour has no SyncVars
            if (syncedVarFields.Count == 0)
                return;

            using (BitWriter writer = BitWriter.Get())
            {
                //Write all indexes
                int syncCount = 0;
                for (int i = 0; i < syncedVarFields.Count; i++)
                {
                    if (!syncedVarFields[i].Target)
                        syncCount++;
                    else if (syncedVarFields[i].Target && ownerClientId == clientId)
                        syncCount++;
                }
                if (syncCount == 0)
                    return;
                
                writer.WriteUInt(networkId); //NetId
                writer.WriteUShort(networkedObject.GetOrderIndex(this)); //Behaviour OrderIndex

                bool[] mask = GetDirtyMask(false, clientId);
                for (int i = 0; i < mask.Length; i++) writer.WriteBool(mask[i]);

                for (int i = 0; i < syncedVarFields.Count; i++)
                {
                    if (syncedVarFields[i].Target && clientId != ownerClientId)
                        continue;
                    FieldTypeHelper.WriteFieldType(writer, syncedVarFields[i].FieldInfo, this, syncedVarFields[i].FieldType);
                }
                bool observed = InternalMessageHandler.Send(clientId, "MLAPI_SYNC_VAR_UPDATE", "MLAPI_INTERNAL", writer, networkId);
                if (observed)
                    OutOfSyncClients.Remove(clientId);
            }
        }

        private ref bool[] GetDirtyMask(bool ignoreTarget, uint? clientId = null)
        {
            for (int i = 0; i < syncedVarFields.Count; i++)
                syncMask[i] = (clientId == null && ignoreTarget && syncedVarFields[i].Dirty && !syncedVarFields[i].Target) ||
                               (clientId == null && !ignoreTarget && syncedVarFields[i].Dirty) || 
                                (clientId != null && !syncedVarFields[i].Target) || 
                                 (clientId != null && syncedVarFields[i].Target && ownerClientId == clientId.Value);
            return ref syncMask;
        }

        internal void SyncVarUpdate()
        {
            if (!syncVarInit)
                SyncVarInit();
            
            if (!SetDirtyness())
                return;

            int nonTargetDirtyCount = 0;
            int totalDirtyCount = 0;
            int dirtyTargets = 0;
            for (int i = 0; i < syncedVarFields.Count; i++)
            {
                if (syncedVarFields[i].Dirty)
                    totalDirtyCount++;
                if (syncedVarFields[i].Target && syncedVarFields[i].Dirty)
                    dirtyTargets++;
                if (syncedVarFields[i].Dirty && !syncedVarFields[i].Target)
                    nonTargetDirtyCount++;
            }

            if (totalDirtyCount == 0)
                return; //All up to date!

            // If we don't have targets. We can send one big message, 
            // thus only serializing it once. Otherwise, we have to create two messages. One for the non targets and one for the target
            if (dirtyTargets == 0)
            {
                //It's sync time!
                using (BitWriter writer = BitWriter.Get())
                {
                    //Write all indexes
                    writer.WriteUInt(networkId); //NetId
                    writer.WriteUShort(networkedObject.GetOrderIndex(this)); //Behaviour OrderIndex

                    bool[] mask = GetDirtyMask(false);
                    for (int i = 0; i < mask.Length; i++) writer.WriteBool(mask[i]);

                    for (int i = 0; i < syncedVarFields.Count; i++)
                    {
                        //Writes all the indexes of the dirty syncvars.
                        if (syncedVarFields[i].Dirty == true)
                        {
                            FieldTypeHelper.WriteFieldType(writer, syncedVarFields[i].FieldInfo, this, syncedVarFields[i].FieldType);
                            syncedVarFields[i].FieldValue = syncedVarFields[i].FieldInfo.GetValue(this);
                            syncedVarFields[i].Dirty = false;
                        }
                    }
                    List<uint> stillDirtyIds = InternalMessageHandler.Send("MLAPI_SYNC_VAR_UPDATE", "MLAPI_INTERNAL", writer, networkId);
                    if (stillDirtyIds != null)
                    {
                        for (int i = 0; i < stillDirtyIds.Count; i++)
                            OutOfSyncClients.Add(stillDirtyIds[i]);
                    }
                }
            }
            else
            {
                if (!(isHost && ownerClientId == NetworkingManager.singleton.NetworkConfig.NetworkTransport.HostDummyId))
                {
                    //It's sync time. This is the target receivers packet.
                    using (BitWriter writer = BitWriter.Get())
                    {
                        //Write all indexes
                        writer.WriteUInt(networkId); //NetId
                        writer.WriteUShort(networkedObject.GetOrderIndex(this)); //Behaviour OrderIndex

                        bool[] mask = GetDirtyMask(false);
                        for (int i = 0; i < mask.Length; i++) writer.WriteBool(mask[i]);

                        for (int i = 0; i < syncedVarFields.Count; i++)
                        {
                            //Writes all the indexes of the dirty syncvars.
                            if (syncedVarFields[i].Dirty == true)
                            {
                                FieldTypeHelper.WriteFieldType(writer, syncedVarFields[i].FieldInfo, this, syncedVarFields[i].FieldType);
                                if (nonTargetDirtyCount == 0)
                                {
                                    //Only targeted SyncedVars were changed. Thus we need to set them as non dirty here since it wont be done by the next loop.
                                    syncedVarFields[i].FieldValue = syncedVarFields[i].FieldInfo.GetValue(this);
                                    syncedVarFields[i].Dirty = false;
                                }
                            }
                        }
                        bool observing = !InternalMessageHandler.Send(ownerClientId, "MLAPI_SYNC_VAR_UPDATE", "MLAPI_INTERNAL", writer, networkId); //Send only to target
                        if (!observing)
                            OutOfSyncClients.Add(ownerClientId);
                    }
                }

                if (nonTargetDirtyCount == 0)
                    return;

                //It's sync time. This is the NON target receivers packet.
                using (BitWriter writer = BitWriter.Get())
                {
                    //Write all indexes
                    writer.WriteUInt(networkId); //NetId
                    writer.WriteUShort(networkedObject.GetOrderIndex(this)); //Behaviour OrderIndex

                    bool[] mask = GetDirtyMask(true);
                    for (int i = 0; i < mask.Length; i++) writer.WriteBool(mask[i]);

                    for (int i = 0; i < syncedVarFields.Count; i++)
                    {
                        //Writes all the indexes of the dirty syncvars.
                        if (syncedVarFields[i].Dirty == true && !syncedVarFields[i].Target)
                        {
                            FieldTypeHelper.WriteFieldType(writer, syncedVarFields[i].FieldInfo, this, syncedVarFields[i].FieldType);
                            syncedVarFields[i].FieldValue = syncedVarFields[i].FieldInfo.GetValue(this);
                            syncedVarFields[i].Dirty = false;
                        }
                    }
                    List<uint> stillDirtyIds = InternalMessageHandler.Send("MLAPI_SYNC_VAR_UPDATE", "MLAPI_INTERNAL", writer, ownerClientId, networkId, null, null); // Send to everyone except target.
                    if (stillDirtyIds != null)
                    {
                        for (int i = 0; i < stillDirtyIds.Count; i++)
                            OutOfSyncClients.Add(stillDirtyIds[i]);
                    }
                }
            }
        }

        private bool SetDirtyness()
        {
            if (!isServer)
                return false;

            bool dirty = false;
            for (int i = 0; i < syncedVarFields.Count; i++)
            {
                if (NetworkingManager.singleton.NetworkTime - syncedVarFields[i].Attribute.lastSyncTime < syncedVarFields[i].Attribute.syncDelay)
                    continue;
                if (!syncedVarFields[i].FieldInfo.GetValue(this).Equals(syncedVarFields[i].FieldValue))
                {
                    syncedVarFields[i].Dirty = true; //This fields value is out of sync!
                    syncedVarFields[i].Attribute.lastSyncTime = NetworkingManager.singleton.NetworkTime;
                    dirty = true;
                }
                else
                {
                    syncedVarFields[i].Attribute.lastSyncTime = NetworkingManager.singleton.NetworkTime;
                    syncedVarFields[i].Dirty = false; //Up to date;
                }
            }
            return dirty;
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
            using (BitWriter writer = BitWriter.Get())
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
        protected void SendToServer(string messageType, string channelName, BitWriter writer)
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
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(NetworkingManager.singleton.NetworkConfig.NetworkTransport.ServerNetId, messageType, channelName, writer, null, networkId, networkedObject.GetOrderIndex(this));
            }
        }

        /// <summary>
        /// Sends a buffer to the server from client. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        protected void SendToServerTarget(string messageType, string channelName, BitWriter writer)
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
            InternalMessageHandler.Send(NetworkingManager.singleton.NetworkConfig.NetworkTransport.ServerNetId, messageType, channelName, writer, null, networkId, networkedObject.GetOrderIndex(this));
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
        /// Sends a buffer to the server from client
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToLocalClient(string messageType, string channelName, byte[] data, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(ownerClientId, messageType, channelName, writer, fromNetId);
            }
        }

        /// <summary>
        /// Sends a buffer to the server from client
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToLocalClient(string messageType, string channelName, BitWriter writer, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            InternalMessageHandler.Send(ownerClientId, messageType, channelName, writer, fromNetId);
        }

        /// <summary>
        /// Sends a binary serialized class to the server from client
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToLocalClient<T>(string messageType, string channelName, T instance, bool respectObservers = false)
        {
            SendToLocalClient(messageType, channelName, BinarySerializer.Serialize<T>(instance), respectObservers);
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
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(ownerClientId, messageType, channelName, writer, null, networkId, networkedObject.GetOrderIndex(this));
            }
        }

        /// <summary>
        /// Sends a buffer to the client that owns this object from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        protected void SendToLocalClientTarget(string messageType, string channelName, BitWriter writer)
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
            InternalMessageHandler.Send(ownerClientId, messageType, channelName, writer, null, networkId, networkedObject.GetOrderIndex(this));
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
        protected void SendToNonLocalClients(string messageType, string channelName, byte[] data, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(messageType, channelName, writer, ownerClientId, fromNetId, null, null);
            }
        }

        /// <summary>
        /// Sends a buffer to all clients except to the owner object from the server
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToNonLocalClients(string messageType, string channelName, BitWriter writer, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            InternalMessageHandler.Send(messageType, channelName, writer, ownerClientId, fromNetId, null, null);
        }

        /// <summary>
        /// Sends a binary serialized class to all clients except to the owner object from the server
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToNonLocalClients<T>(string messageType, string channelName, T instance, bool respectObservers = false)
        {
            SendToNonLocalClients(messageType, channelName, BinarySerializer.Serialize<T>(instance), respectObservers);
        }

        /// <summary>
        /// Sends a buffer to all clients except to the owner object from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToNonLocalClientsTarget(string messageType, string channelName, byte[] data, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(messageType, channelName, writer, ownerClientId, fromNetId, networkId, networkedObject.GetOrderIndex(this));
            }
        }

        /// <summary>
        /// Sends a buffer to all clients except to the owner object from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToNonLocalClientsTarget(string messageType, string channelName, BitWriter writer, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            InternalMessageHandler.Send(messageType, channelName, writer, ownerClientId, fromNetId, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
        /// Sends a binary serialized class to all clients except to the owner object from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToNonLocalClientsTarget<T>(string messageType, string channelName, T instance, bool respectObservers = false)
        {
            SendToNonLocalClientsTarget(messageType, channelName, BinarySerializer.Serialize<T>(instance), respectObservers);
        }

        /// <summary>
        /// Sends a buffer to a client with a given clientId from Server
        /// </summary>
        /// <param name="clientId">The clientId to send the message to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClient(uint clientId, string messageType, string channelName, byte[] data, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(clientId, messageType, channelName, writer, fromNetId);
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
        protected void SendToClient(uint clientId, string messageType, string channelName, BitWriter writer, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            InternalMessageHandler.Send(clientId, messageType, channelName, writer, fromNetId);
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
        protected void SendToClient<T>(int clientId, string messageType, string channelName, T instance, bool respectObservers = false)
        {
            SendToClient(clientId, messageType, channelName, BinarySerializer.Serialize<T>(instance), respectObservers);
        }

        /// <summary>
        /// Sends a buffer to a client with a given clientId from Server. Only handlers on this NetworkedBehaviour gets invoked
        /// </summary>
        /// <param name="clientId">The clientId to send the message to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientTarget(uint clientId, string messageType, string channelName, byte[] data, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(clientId, messageType, channelName, writer, fromNetId, networkId, networkedObject.GetOrderIndex(this));
            }
        }

        /// <summary>
        /// Sends a buffer to a client with a given clientId from Server. Only handlers on this NetworkedBehaviour gets invoked
        /// </summary>
        /// <param name="clientId">The clientId to send the message to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientTarget(uint clientId, string messageType, string channelName, BitWriter writer, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            InternalMessageHandler.Send(clientId, messageType, channelName, writer, fromNetId, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
        /// Sends a buffer to a client with a given clientId from Server. Only handlers on this NetworkedBehaviour gets invoked
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="clientId">The clientId to send the message to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientTarget<T>(int clientId, string messageType, string channelName, T instance, bool respectObservers = false)
        {
            SendToClientTarget(clientId, messageType, channelName, BinarySerializer.Serialize<T>(instance), respectObservers);
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClients(uint[] clientIds, string messageType, string channelName, byte[] data, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(clientIds, messageType, channelName, writer, fromNetId);
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
        protected void SendToClients(uint[] clientIds, string messageType, string channelName, BitWriter writer, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            InternalMessageHandler.Send(clientIds, messageType, channelName, writer, fromNetId);
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
        protected void SendToClients<T>(int[] clientIds, string messageType, string channelName, T instance, bool respectObservers = false)
        {
            SendToClients(clientIds, messageType, channelName, BinarySerializer.Serialize<T>(instance), respectObservers);
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server. Only handlers on this NetworkedBehaviour gets invoked
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientsTarget(uint[] clientIds, string messageType, string channelName, byte[] data, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(clientIds, messageType, channelName, writer, fromNetId, networkId, networkedObject.GetOrderIndex(this));
            }
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server. Only handlers on this NetworkedBehaviour gets invoked
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientsTarget(uint[] clientIds, string messageType, string channelName, BitWriter writer, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            InternalMessageHandler.Send(clientIds, messageType, channelName, writer, fromNetId, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server. Only handlers on this NetworkedBehaviour gets invoked
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientsTarget<T>(int[] clientIds, string messageType, string channelName, T instance, bool respectObservers = false)
        {
            SendToClientsTarget(clientIds, messageType, channelName, BinarySerializer.Serialize<T>(instance), respectObservers);
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClients(List<uint> clientIds, string messageType, string channelName, byte[] data, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(clientIds, messageType, channelName, writer, fromNetId);
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
        protected void SendToClients(List<uint> clientIds, string messageType, string channelName, BitWriter writer, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            InternalMessageHandler.Send(clientIds, messageType, channelName, writer, fromNetId);
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
        protected void SendToClients<T>(List<int> clientIds, string messageType, string channelName, T instance, bool respectObservers = false)
        {
            SendToClients(clientIds, messageType, channelName, BinarySerializer.Serialize<T>(instance), respectObservers);
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server. Only handlers on this NetworkedBehaviour gets invoked
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientsTarget(List<uint> clientIds, string messageType, string channelName, byte[] data, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(clientIds, messageType, channelName, writer, fromNetId, networkId, networkedObject.GetOrderIndex(this));
            }
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server. Only handlers on this NetworkedBehaviour gets invoked
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientsTarget(List<uint> clientIds, string messageType, string channelName, BitWriter writer, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            InternalMessageHandler.Send(clientIds, messageType, channelName, writer, fromNetId, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server. Only handlers on this NetworkedBehaviour gets invoked
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientsTarget<T>(List<uint> clientIds, string messageType, string channelName, T instance, bool respectObservers = false)
        {
            SendToClientsTarget(clientIds, messageType, channelName, BinarySerializer.Serialize<T>(instance), respectObservers);
        }

        /// <summary>
        /// Sends a buffer to all clients from the server
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClients(string messageType, string channelName, byte[] data, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(messageType, channelName, writer, fromNetId);
            }
        }

        /// <summary>
        /// Sends a buffer to all clients from the server
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClients(string messageType, string channelName, BitWriter writer, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            InternalMessageHandler.Send(messageType, channelName, writer, fromNetId);
        }

        /// <summary>
        /// Sends a buffer to all clients from the server
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClients<T>(string messageType, string channelName, T instance, bool respectObservers = false)
        {
            SendToClients(messageType, channelName, BinarySerializer.Serialize<T>(instance), respectObservers);
        }

        /// <summary>
        /// Sends a buffer to all clients from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientsTarget(string messageType, string channelName, byte[] data, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteByteArray(data);
                InternalMessageHandler.Send(messageType, channelName, writer, fromNetId, networkId, networkedObject.GetOrderIndex(this));
            }
        }

        /// <summary>
        /// Sends a buffer to all clients from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="writer">The binary data to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientsTarget(string messageType, string channelName, BitWriter writer, bool respectObservers = false)
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
            uint? fromNetId = respectObservers ? (uint?)networkId : null;
            InternalMessageHandler.Send(messageType, channelName, writer, fromNetId, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
        /// Sends a buffer to all clients from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        /// <param name="respectObservers">If this is true, the message will only be sent to clients observing the sender object</param>
        protected void SendToClientsTarget<T>(string messageType, string channelName, T instance, bool respectObservers = false)
        {
            SendToClientsTarget(messageType, channelName, BinarySerializer.Serialize<T>(instance), respectObservers);
        }
        #endregion

        /// <summary>
        /// Gets the local instance of a object with a given NetworkId
        /// </summary>
        /// <param name="networkId"></param>
        /// <returns></returns>
        protected NetworkedObject GetNetworkedObject(uint networkId)
        {
            return SpawnManager.spawnedObjects[networkId];
        }
    }
}
