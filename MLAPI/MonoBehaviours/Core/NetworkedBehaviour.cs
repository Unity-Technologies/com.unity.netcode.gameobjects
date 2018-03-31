using System;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.NetworkingManagerComponents;
using System.Reflection;
using MLAPI.Attributes;
using System.Linq;
using System.IO;
using MLAPI.Data;

namespace MLAPI
{
    /// <summary>
    /// The base class to override to write networked code. Inherits MonoBehaviour
    /// </summary>
    public abstract class NetworkedBehaviour : MonoBehaviour
    {
        /// <summary>
        /// The minimum delay in seconds between SyncedVar sends
        /// </summary>
        public float SyncVarSyncDelay = 0.1f;
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
                if(_networkedObject == null)
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
        public int ownerClientId
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
            {
                _networkedObject = GetComponentInParent<NetworkedObject>();
            }
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
        /// Registers a message handler
        /// </summary>
        /// <param name="name">The MessageType to register</param>
        /// <param name="action">The callback to get invoked whenever a message is received</param>
        /// <returns>HandlerId for the messageHandler that can be used to deregister the messageHandler</returns>
        protected int RegisterMessageHandler(string name, Action<int, byte[]> action)
        {
            if (!MessageManager.messageTypes.ContainsKey(name))
            {
                Debug.LogWarning("MLAPI: The messageType " + name + " is not registered");
                return -1;
            }
            ushort messageType = MessageManager.messageTypes[name];
            ushort behaviourOrder = networkedObject.GetOrderIndex(this);

            if (!networkedObject.targetMessageActions.ContainsKey(behaviourOrder))
                networkedObject.targetMessageActions.Add(behaviourOrder, new Dictionary<ushort, Action<int, byte[]>>());
            if (networkedObject.targetMessageActions[behaviourOrder].ContainsKey(messageType))
            {
                Debug.LogWarning("MLAPI: Each NetworkedBehaviour can only register one callback per instance per message type");
                return -1;
            }

            networkedObject.targetMessageActions[behaviourOrder].Add(messageType, action);
            int counter = MessageManager.AddIncomingMessageHandler(name, action, networkId);
            registeredMessageHandlers.Add(name, counter);
            return counter;
        }

        /// <summary>
        /// Deregisters a given message handler
        /// </summary>
        /// <param name="name">The MessageType to deregister</param>
        /// <param name="counter">The messageHandlerId to deregister</param>
        protected void DeregisterMessageHandler(string name, int counter)
        {
            MessageManager.RemoveIncomingMessageHandler(name, counter, networkId);
            ushort messageType = MessageManager.messageTypes[name];
            ushort behaviourOrder = networkedObject.GetOrderIndex(this);

            if (networkedObject.targetMessageActions.ContainsKey(behaviourOrder) && 
                networkedObject.targetMessageActions[behaviourOrder].ContainsKey(messageType))
            {
                networkedObject.targetMessageActions[behaviourOrder].Remove(messageType);
            }
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
        private List<FieldInfo> syncedFields = new List<FieldInfo>();
        internal List<FieldType> syncedFieldTypes = new List<FieldType>();
        private List<object> syncedFieldValues = new List<object>();
        private List<MethodInfo> syncedVarHooks = new List<MethodInfo>();
        //A dirty field is a field that's not synced.
        private bool[] dirtyFields;
        internal void SyncVarInit()
        {
            FieldInfo[] sortedFields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Instance).OrderBy(x => x.Name).ToArray();
            for (byte i = 0; i < sortedFields.Length; i++)
            {
                if(sortedFields[i].IsDefined(typeof(SyncedVar), true))
                {
                    object[] syncedVarAttributes = sortedFields[i].GetCustomAttributes(typeof(SyncedVar), true);
                    MethodInfo method = null;
                    for (int j = 0; j < syncedVarAttributes.Length; j++)
                    {
                        if(!string.IsNullOrEmpty(((SyncedVar)syncedVarAttributes[j]).hook))
                        {
                            method = GetType().GetMethod(((SyncedVar)syncedVarAttributes[j]).hook, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                            break;
                        }
                    }
                    if (sortedFields[i].FieldType == typeof(bool))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Bool);
                        syncedVarHooks.Add(method);
                    }
                    else if(sortedFields[i].FieldType == typeof(byte))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Byte);
                        syncedVarHooks.Add(method);
                    }
                    else if (sortedFields[i].FieldType == typeof(char))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Char);
                        syncedVarHooks.Add(method);
                    }
                    else if (sortedFields[i].FieldType == typeof(double))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Double);
                        syncedVarHooks.Add(method);
                    }
                    else if (sortedFields[i].FieldType == typeof(float))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Single);
                        syncedVarHooks.Add(method);
                    }
                    else if (sortedFields[i].FieldType == typeof(int))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Int);
                        syncedVarHooks.Add(method);
                    }
                    else if (sortedFields[i].FieldType == typeof(long))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Long);
                        syncedVarHooks.Add(method);
                    }
                    else if (sortedFields[i].FieldType == typeof(sbyte))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.SByte);
                        syncedVarHooks.Add(method);
                    }
                    else if (sortedFields[i].FieldType == typeof(short))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Short);
                        syncedVarHooks.Add(method);
                    }
                    else if (sortedFields[i].FieldType == typeof(uint))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.UInt);
                        syncedVarHooks.Add(method);
                    }
                    else if (sortedFields[i].FieldType == typeof(ulong))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.ULong);
                        syncedVarHooks.Add(method);
                    }
                    else if (sortedFields[i].FieldType == typeof(ushort))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.UShort);
                        syncedVarHooks.Add(method);
                    }
                    else if(sortedFields[i].FieldType == typeof(string))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.String);
                        syncedVarHooks.Add(method);
                    }
                    else if(sortedFields[i].FieldType == typeof(Vector3))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Vector3);
                        syncedVarHooks.Add(method);
                    }
                    else if(sortedFields[i].FieldType == typeof(Vector2))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Vector2);
                        syncedVarHooks.Add(method);
                    }
                    else if (sortedFields[i].FieldType == typeof(Quaternion))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Quaternion);
                        syncedVarHooks.Add(method);
                    }
                    else if(sortedFields[i].FieldType == typeof(byte[]))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.ByteArray);
                        syncedVarHooks.Add(method);
                    }
                    else
                    {
                        Debug.LogError("MLAPI: The type " + sortedFields[i].FieldType.ToString() + " can not be used as a syncvar");
                    }
                }
            }
            dirtyFields = new bool[syncedFields.Count];
            if (dirtyFields.Length > 255)
            {
                Debug.LogError("MLAPI: You can not have more than 255 SyncVar's per NetworkedBehaviour!");
            }
        }

        internal void OnSyncVarUpdate(object value, byte fieldIndex)
        {
            syncedFields[fieldIndex].SetValue(this, value);
            if (syncedVarHooks[fieldIndex] != null)
                syncedVarHooks[fieldIndex].Invoke(this, null);
        }

        internal void FlushToClient(int clientId)
        {
            //This NetworkedBehaviour has no SyncVars
            if (dirtyFields.Length == 0)
                return;

            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    //Write all indexes
                    writer.Write((byte)dirtyFields.Length);
                    writer.Write(networkId); //NetId
                    writer.Write(networkedObject.GetOrderIndex(this)); //Behaviour OrderIndex
                    for (byte i = 0; i < dirtyFields.Length; i++)
                    {
                        writer.Write(i); //FieldIndex
                        switch (syncedFieldTypes[i])
                        {
                            case FieldType.Bool:
                                writer.Write((bool)syncedFields[i].GetValue(this));
                                break;
                            case FieldType.Byte:
                                writer.Write((byte)syncedFields[i].GetValue(this));
                                break;
                            case FieldType.Char:
                                writer.Write((char)syncedFields[i].GetValue(this));
                                break;
                            case FieldType.Double:
                                writer.Write((double)syncedFields[i].GetValue(this));
                                break;
                            case FieldType.Single:
                                writer.Write((float)syncedFields[i].GetValue(this));
                                break;
                            case FieldType.Int:
                                writer.Write((int)syncedFields[i].GetValue(this));
                                break;
                            case FieldType.Long:
                                writer.Write((long)syncedFields[i].GetValue(this));
                                break;
                            case FieldType.SByte:
                                writer.Write((sbyte)syncedFields[i].GetValue(this));
                                break;
                            case FieldType.Short:
                                writer.Write((short)syncedFields[i].GetValue(this));
                                break;
                            case FieldType.UInt:
                                writer.Write((uint)syncedFields[i].GetValue(this));
                                break;
                            case FieldType.ULong:
                                writer.Write((ulong)syncedFields[i].GetValue(this));
                                break;
                            case FieldType.UShort:
                                writer.Write((ushort)syncedFields[i].GetValue(this));
                                break;
                            case FieldType.String:
                                writer.Write((string)syncedFields[i].GetValue(this));
                                break;
                            case FieldType.Vector3:
                                Vector3 vector3 = (Vector3)syncedFields[i].GetValue(this);
                                writer.Write(vector3.x);
                                writer.Write(vector3.y);
                                writer.Write(vector3.z);
                                break;
                            case FieldType.Vector2:
                                Vector2 vector2 = (Vector2)syncedFields[i].GetValue(this);
                                writer.Write(vector2.x);
                                writer.Write(vector2.y);
                                break;
                            case FieldType.Quaternion:
                                Vector3 euler = ((Quaternion)syncedFields[i].GetValue(this)).eulerAngles;
                                writer.Write(euler.x);
                                writer.Write(euler.y);
                                writer.Write(euler.z);
                                break;
                            case FieldType.ByteArray:
                                writer.Write((ushort)((byte[])syncedFields[i].GetValue(this)).Length);
                                writer.Write((byte[])syncedFields[i].GetValue(this));
                                break;
                        }
                    }
                }
                NetworkingManager.singleton.Send(clientId, "MLAPI_SYNC_VAR_UPDATE", "MLAPI_INTERNAL", stream.ToArray());
            }
        }

        private float lastSyncTime = 0f;
        internal void SyncVarUpdate()
        {
            SetDirtyness();
            if(Time.time - lastSyncTime >= SyncVarSyncDelay)
            {
                byte dirtyCount = (byte)dirtyFields.Count(x => x == true);
                if (dirtyCount == 0)
                    return; //All up to date!
                //It's sync time!
                using (MemoryStream stream = new MemoryStream())
                {
                    using(BinaryWriter writer = new BinaryWriter(stream))
                    {
                        //Write all indexes
                        writer.Write(dirtyCount);
                        writer.Write(networkId); //NetId
                        writer.Write(networkedObject.GetOrderIndex(this)); //Behaviour OrderIndex
                        for (byte i = 0; i < dirtyFields.Length; i++)
                        {
                            //Writes all the indexes of the dirty syncvars.
                            if (dirtyFields[i] == true)
                            {
                                writer.Write(i); //FieldIndex
                                switch (syncedFieldTypes[i])
                                {
                                    case FieldType.Bool:
                                        writer.Write((bool)syncedFields[i].GetValue(this));
                                        break;
                                    case FieldType.Byte:
                                        writer.Write((byte)syncedFields[i].GetValue(this));
                                        break;
                                    case FieldType.Char:
                                        writer.Write((char)syncedFields[i].GetValue(this));
                                        break;
                                    case FieldType.Double:
                                        writer.Write((double)syncedFields[i].GetValue(this));
                                        break;
                                    case FieldType.Single:
                                        writer.Write((float)syncedFields[i].GetValue(this));
                                        break;
                                    case FieldType.Int:
                                        writer.Write((int)syncedFields[i].GetValue(this));
                                        break;
                                    case FieldType.Long:
                                        writer.Write((long)syncedFields[i].GetValue(this));
                                        break;
                                    case FieldType.SByte:
                                        writer.Write((sbyte)syncedFields[i].GetValue(this));
                                        break;
                                    case FieldType.Short:
                                        writer.Write((short)syncedFields[i].GetValue(this));
                                        break;
                                    case FieldType.UInt:
                                        writer.Write((uint)syncedFields[i].GetValue(this));
                                        break;
                                    case FieldType.ULong:
                                        writer.Write((ulong)syncedFields[i].GetValue(this));
                                        break;
                                    case FieldType.UShort:
                                        writer.Write((ushort)syncedFields[i].GetValue(this));
                                        break;
                                    case FieldType.String:
                                        writer.Write((string)syncedFields[i].GetValue(this));
                                        break;
                                    case FieldType.Vector3:
                                        Vector3 vector3 = (Vector3)syncedFields[i].GetValue(this);
                                        writer.Write(vector3.x);
                                        writer.Write(vector3.y);
                                        writer.Write(vector3.z);
                                        break;
                                    case FieldType.Vector2:
                                        Vector2 vector2 = (Vector2)syncedFields[i].GetValue(this);
                                        writer.Write(vector2.x);
                                        writer.Write(vector2.y);
                                        break;
                                    case FieldType.Quaternion:
                                        Vector3 euler = ((Quaternion)syncedFields[i].GetValue(this)).eulerAngles;
                                        writer.Write(euler.x);
                                        writer.Write(euler.y);
                                        writer.Write(euler.z);
                                        break;
                                    case FieldType.ByteArray:
                                        writer.Write((ushort)((byte[])syncedFields[i].GetValue(this)).Length);
                                        writer.Write((byte[])syncedFields[i].GetValue(this));
                                        break;

                                }
                                syncedFieldValues[i] = syncedFields[i].GetValue(this);
                                dirtyFields[i] = false;
                            }
                        }
                    }
                    NetworkingManager.singleton.Send("MLAPI_SYNC_VAR_UPDATE", "MLAPI_INTERNAL", stream.ToArray());
                }
                lastSyncTime = Time.time;
            }
        }

        private void SetDirtyness()
        {
            if (!isServer)
                return;
            for (int i = 0; i < syncedFields.Count; i++)
            {
                switch (syncedFieldTypes[i])
                {
                    case FieldType.Bool:
                        if ((bool)syncedFields[i].GetValue(this) != (bool)syncedFieldValues[i])
                            dirtyFields[i] = true; //This fields value is out of sync!
                        else
                            dirtyFields[i] = false; //Up to date
                        break;
                    case FieldType.Byte:
                        if ((byte)syncedFields[i].GetValue(this) != (byte)syncedFieldValues[i])
                            dirtyFields[i] = true; //This fields value is out of sync!
                        else
                            dirtyFields[i] = false; //Up to date
                        break;
                    case FieldType.Char:
                        if ((char)syncedFields[i].GetValue(this) != (char)syncedFieldValues[i])
                            dirtyFields[i] = true; //This fields value is out of sync!
                        else
                            dirtyFields[i] = false; //Up to date
                        break;
                    case FieldType.Double:
                        if ((double)syncedFields[i].GetValue(this) != (double)syncedFieldValues[i])
                            dirtyFields[i] = true; //This fields value is out of sync!
                        else
                            dirtyFields[i] = false; //Up to date
                        break;
                    case FieldType.Single:
                        if ((float)syncedFields[i].GetValue(this) != (float)syncedFieldValues[i])
                            dirtyFields[i] = true; //This fields value is out of sync!
                        else
                            dirtyFields[i] = false; //Up to date
                        break;
                    case FieldType.Int:
                        if ((int)syncedFields[i].GetValue(this) != (int)syncedFieldValues[i])
                            dirtyFields[i] = true; //This fields value is out of sync!
                        else
                            dirtyFields[i] = false; //Up to date
                        break;
                    case FieldType.Long:
                        if ((long)syncedFields[i].GetValue(this) != (long)syncedFieldValues[i])
                            dirtyFields[i] = true; //This fields value is out of sync!
                        else
                            dirtyFields[i] = false; //Up to date
                        break;
                    case FieldType.SByte:
                        if ((sbyte)syncedFields[i].GetValue(this) != (sbyte)syncedFieldValues[i])
                            dirtyFields[i] = true; //This fields value is out of sync!
                        else
                            dirtyFields[i] = false; //Up to date
                        break;
                    case FieldType.Short:
                        if ((short)syncedFields[i].GetValue(this) != (short)syncedFieldValues[i])
                            dirtyFields[i] = true; //This fields value is out of sync!
                        else
                            dirtyFields[i] = false; //Up to date
                        break;
                    case FieldType.UInt:
                        if ((uint)syncedFields[i].GetValue(this) != (uint)syncedFieldValues[i])
                            dirtyFields[i] = true; //This fields value is out of sync!
                        else
                            dirtyFields[i] = false; //Up to date
                        break;
                    case FieldType.ULong:
                        if ((ulong)syncedFields[i].GetValue(this) != (ulong)syncedFieldValues[i])
                            dirtyFields[i] = true; //This fields value is out of sync!
                        else
                            dirtyFields[i] = false; //Up to date
                        break;
                    case FieldType.UShort:
                        if ((ushort)syncedFields[i].GetValue(this) != (ushort)syncedFieldValues[i])
                            dirtyFields[i] = true; //This fields value is out of sync!
                        else
                            dirtyFields[i] = false; //Up to date
                        break;
                    case FieldType.String:
                        if ((string)syncedFields[i].GetValue(this) != (string)syncedFieldValues[i])
                            dirtyFields[i] = true; //This fields value is out of sync!
                        else
                            dirtyFields[i] = false; //Up to date
                        break;
                    case FieldType.Vector3:
                        if ((Vector3)syncedFields[i].GetValue(this) != (Vector3)syncedFieldValues[i])
                            dirtyFields[i] = true; //This fields value is out of sync!
                        else
                            dirtyFields[i] = false; //Up to date
                        break;
                    case FieldType.Vector2:
                        if ((Vector2)syncedFields[i].GetValue(this) != (Vector2)syncedFieldValues[i])
                            dirtyFields[i] = true; //This fields value is out of sync!
                        else
                            dirtyFields[i] = false; //Up to date
                        break;
                    case FieldType.Quaternion:
                        if ((Quaternion)syncedFields[i].GetValue(this) != (Quaternion)syncedFieldValues[i])
                            dirtyFields[i] = true; //This fields value is out of sync!
                        else
                            dirtyFields[i] = false; //Up to date
                        break;
                    case FieldType.ByteArray:
                        if(((byte[])syncedFields[i].GetValue(this)).SequenceEqual(((byte[])syncedFieldValues[i])))
                            dirtyFields[i] = true; //This fields value is out of sync!
                        else
                            dirtyFields[i] = false; //Up to date
                        break;
                }
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
            if(MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (isServer)
            {
                Debug.LogWarning("MLAPI: Server can not send messages to server.");
                return;
            }
            NetworkingManager.singleton.Send(NetworkingManager.singleton.serverClientId, messageType, channelName, data);
        }

        /// <summary>
        /// Sends a buffer to the server from client. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        protected void SendToServerTarget(string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (isServer)
            {
                Debug.LogWarning("MLAPI: Server can not send messages to server.");
                return;
            }
            NetworkingManager.singleton.Send(NetworkingManager.singleton.serverClientId, messageType, channelName, data, networkId, networkedObject.GetOrderIndex(this));            
        }

        /// <summary>
        /// Sends a buffer to the server from client
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        protected void SendToLocalClient(string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageTypes.Contains(messageType)))
            {
                Debug.LogWarning("MLAPI: Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }
            NetworkingManager.singleton.Send(ownerClientId, messageType, channelName, data);
        }

        /// <summary>
        /// Sends a buffer to the client that owns this object from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        protected void SendToLocalClientTarget(string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageTypes.Contains(messageType)))
            {
                Debug.LogWarning("MLAPI: Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }
            NetworkingManager.singleton.Send(ownerClientId, messageType, channelName, data, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
        /// Sends a buffer to all clients except to the owner object from the server
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        protected void SendToNonLocalClients(string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(messageType, channelName, data, ownerClientId);
        }

        /// <summary>
        /// Sends a buffer to all clients except to the owner object from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        protected void SendToNonLocalClientsTarget(string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(messageType, channelName, data, ownerClientId, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
        /// Sends a buffer to a client with a given clientId from Server
        /// </summary>
        /// <param name="clientId">The clientId to send the message to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        protected void SendToClient(int clientId, string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageTypes.Contains(messageType)))
            {
                Debug.LogWarning("MLAPI: Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }
            NetworkingManager.singleton.Send(clientId, messageType, channelName, data);
        }

        /// <summary>
        /// Sends a buffer to a client with a given clientId from Server. Only handlers on this NetworkedBehaviour gets invoked
        /// </summary>
        /// <param name="clientId">The clientId to send the message to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        protected void SendToClientTarget(int clientId, string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageTypes.Contains(messageType)))
            {
                Debug.LogWarning("MLAPI: Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }
            NetworkingManager.singleton.Send(clientId, messageType, channelName, data, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        protected void SendToClients(int[] clientIds, string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(clientIds, messageType, channelName, data);
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server. Only handlers on this NetworkedBehaviour gets invoked
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        protected void SendToClientsTarget(int[] clientIds, string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(clientIds, messageType, channelName, data, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        protected void SendToClients(List<int> clientIds, string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(clientIds, messageType, channelName, data);
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server. Only handlers on this NetworkedBehaviour gets invoked
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        protected void SendToClientsTarget(List<int> clientIds, string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(clientIds, messageType, channelName, data, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
        /// Sends a buffer to all clients from the server
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        protected void SendToClients(string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(messageType, channelName, data);
        }

        /// <summary>
        /// Sends a buffer to all clients from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        protected void SendToClientsTarget(string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(messageType, channelName, data, networkId, networkedObject.GetOrderIndex(this));
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
