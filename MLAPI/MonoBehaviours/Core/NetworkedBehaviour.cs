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
    public abstract class NetworkedBehaviour : MonoBehaviour
    {
        public float SyncVarSyncDelay = 0.1f;
        public bool isLocalPlayer
        {
            get
            {
                return networkedObject.isLocalPlayer;
            }
        }
        protected bool isServer
        {
            get
            {
                return NetworkingManager.singleton.isServer;
            }
        }
        protected bool isClient
        {
            get
            {
                return NetworkingManager.singleton.isClient;
            }
        }
        protected bool isHost
        {
            get
            {
                return NetworkingManager.singleton.isHost;
            }
        }
        protected bool isOwner
        {
            get
            {
                return networkedObject.isOwner;
            }
        }
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
        public uint networkId
        {
            get
            {
                return networkedObject.NetworkId;
            }
        }

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
        public virtual void NetworkStart()
        {

        }

        public virtual void OnGainedOwnership()
        {

        }

        public virtual void OnLostOwnership()
        {

        }

        protected int RegisterMessageHandler(string name, Action<int, byte[]> action)
        {
            int counter = MessageManager.AddIncomingMessageHandler(name, action, networkId);
            registeredMessageHandlers.Add(name, counter);
            return counter;
        }

        protected void DeregisterMessageHandler(string name, int counter)
        {
            MessageManager.RemoveIncomingMessageHandler(name, counter, networkId);
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
        //A dirty field is a field that's not synced.
        public bool[] dirtyFields;
        internal void SyncVarInit()
        {
            FieldInfo[] sortedFields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Instance).OrderBy(x => x.Name).ToArray();
            for (byte i = 0; i < sortedFields.Length; i++)
            {
                if(sortedFields[i].IsDefined(typeof(SyncedVar), true))
                {
                    if (sortedFields[i].FieldType == typeof(bool))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Bool);
                    }
                    else if(sortedFields[i].FieldType == typeof(byte))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Byte);
                    }
                    else if (sortedFields[i].FieldType == typeof(char))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Char);
                    }
                    else if (sortedFields[i].FieldType == typeof(double))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Double);
                    }
                    else if (sortedFields[i].FieldType == typeof(float))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Single);
                    }
                    else if (sortedFields[i].FieldType == typeof(int))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Int);
                    }
                    else if (sortedFields[i].FieldType == typeof(long))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Long);
                    }
                    else if (sortedFields[i].FieldType == typeof(sbyte))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.SByte);
                    }
                    else if (sortedFields[i].FieldType == typeof(short))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Short);
                    }
                    else if (sortedFields[i].FieldType == typeof(uint))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.UInt);
                    }
                    else if (sortedFields[i].FieldType == typeof(ulong))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.ULong);
                    }
                    else if (sortedFields[i].FieldType == typeof(ushort))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.UShort);
                    }
                    else if(sortedFields[i].FieldType == typeof(string))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.String);
                    }
                    else if(sortedFields[i].FieldType == typeof(Vector3))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Vector3);
                    }
                    else if(sortedFields[i].FieldType == typeof(Vector2))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Vector2);
                    }
                    else if (sortedFields[i].FieldType == typeof(Quaternion))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.Quaternion);
                    }
                    else if(sortedFields[i].FieldType == typeof(byte[]))
                    {
                        syncedFields.Add(sortedFields[i]);
                        syncedFieldValues.Add(sortedFields[i].GetValue(this));
                        syncedFieldTypes.Add(FieldType.ByteArray);
                    }
                    else
                    {
                        Debug.LogError("MLAPI: The type " + sortedFields[i].FieldType.ToString() + " can not be used as a syncvar");
                    }
                }
            }
            if(dirtyFields.Length > 255)
            {
                Debug.LogError("MLAPI: You can not have more than 255 SyncVar's per NetworkedBehaviour!");
            }
            dirtyFields = new bool[syncedFields.Count];
        }

        internal void OnSyncVarUpdate(object value, byte fieldIndex)
        {
            syncedFields[fieldIndex].SetValue(this, value);
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
            NetworkingManager.singleton.Send(NetworkingManager.singleton.serverClientId, messageType, channelName, data, networkId);            
        }

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
            NetworkingManager.singleton.Send(ownerClientId, messageType, channelName, data, networkId);
        }

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
            NetworkingManager.singleton.Send(messageType, channelName, data, ownerClientId, null);
        }

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
            NetworkingManager.singleton.Send(messageType, channelName, data, ownerClientId, networkId);
        }

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
            NetworkingManager.singleton.Send(clientId, messageType, channelName, data, networkId);
        }

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
            NetworkingManager.singleton.Send(clientIds, messageType, channelName, data, networkId);
        }

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
            NetworkingManager.singleton.Send(clientIds, messageType, channelName, data, networkId);
        }

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
            NetworkingManager.singleton.Send(messageType, channelName, data, networkId);
        }

        protected NetworkedObject GetNetworkedObject(uint networkId)
        {
            return SpawnManager.spawnedObjects[networkId];
        }
    }
}
