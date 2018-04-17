using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using MLAPI.Attributes;
using System.Linq;
using System.IO;
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
        protected int RegisterMessageHandler(string name, Action<uint, byte[]> action)
        {
            if (!MessageManager.messageTypes.ContainsKey(name))
            {
                Debug.LogWarning("MLAPI: The messageType " + name + " is not registered");
                return -1;
            }
            ushort messageType = MessageManager.messageTypes[name];
            ushort behaviourOrder = networkedObject.GetOrderIndex(this);

            if (!networkedObject.targetMessageActions.ContainsKey(behaviourOrder))
                networkedObject.targetMessageActions.Add(behaviourOrder, new Dictionary<ushort, Action<uint, byte[]>>());
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
        internal List<SyncedVarField> syncedVarFields = new List<SyncedVarField>();
        private bool syncVarInit = false;
        internal void SyncVarInit()
        {
            if (syncVarInit)
                return;
            syncVarInit = true;
            FieldInfo[] sortedFields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Instance).OrderBy(x => x.Name).ToArray();
            for (byte i = 0; i < sortedFields.Length; i++)
            {
                if(sortedFields[i].IsDefined(typeof(SyncedVar), true))
                {
                    object[] syncedVarAttributes = sortedFields[i].GetCustomAttributes(typeof(SyncedVar), true);
                    MethodInfo method = null;
                    if (!string.IsNullOrEmpty(((SyncedVar)syncedVarAttributes[0]).hook))
                    {
                        method = GetType().GetMethod(((SyncedVar)syncedVarAttributes[0]).hook, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                        break;
                    }
                    if (sortedFields[i].FieldType == typeof(bool))
                    {
                        syncedVarFields.Add(new SyncedVarField()
                        {
                            Dirty = false,
                            Target = ((SyncedVar)sortedFields[i].GetCustomAttributes(typeof(SyncedVar), true)[0]).target,
                            FieldInfo = sortedFields[i],
                            FieldType = FieldType.Bool,
                            FieldValue = sortedFields[i].GetValue(this),
                            HookMethod = method
                        });
                    }
                    else if(sortedFields[i].FieldType == typeof(byte))
                    {
                        syncedVarFields.Add(new SyncedVarField()
                        {
                            Dirty = false,
                            Target = ((SyncedVar)sortedFields[i].GetCustomAttributes(typeof(SyncedVar), true)[0]).target,
                            FieldInfo = sortedFields[i],
                            FieldType = FieldType.Bool,
                            FieldValue = sortedFields[i].GetValue(this),
                            HookMethod = method
                        });
                    }
                    else if (sortedFields[i].FieldType == typeof(char))
                    {
                        syncedVarFields.Add(new SyncedVarField()
                        {
                            Dirty = false,
                            Target = ((SyncedVar)sortedFields[i].GetCustomAttributes(typeof(SyncedVar), true)[0]).target,
                            FieldInfo = sortedFields[i],
                            FieldType = FieldType.Bool,
                            FieldValue = sortedFields[i].GetValue(this),
                            HookMethod = method
                        });
                    }
                    else if (sortedFields[i].FieldType == typeof(double))
                    {
                        syncedVarFields.Add(new SyncedVarField()
                        {
                            Dirty = false,
                            Target = ((SyncedVar)sortedFields[i].GetCustomAttributes(typeof(SyncedVar), true)[0]).target,
                            FieldInfo = sortedFields[i],
                            FieldType = FieldType.Bool,
                            FieldValue = sortedFields[i].GetValue(this),
                            HookMethod = method
                        });
                    }
                    else if (sortedFields[i].FieldType == typeof(float))
                    {
                        syncedVarFields.Add(new SyncedVarField()
                        {
                            Dirty = false,
                            Target = ((SyncedVar)sortedFields[i].GetCustomAttributes(typeof(SyncedVar), true)[0]).target,
                            FieldInfo = sortedFields[i],
                            FieldType = FieldType.Bool,
                            FieldValue = sortedFields[i].GetValue(this),
                            HookMethod = method
                        });
                    }
                    else if (sortedFields[i].FieldType == typeof(int))
                    {
                        syncedVarFields.Add(new SyncedVarField()
                        {
                            Dirty = false,
                            Target = ((SyncedVar)sortedFields[i].GetCustomAttributes(typeof(SyncedVar), true)[0]).target,
                            FieldInfo = sortedFields[i],
                            FieldType = FieldType.Bool,
                            FieldValue = sortedFields[i].GetValue(this),
                            HookMethod = method
                        });
                    }
                    else if (sortedFields[i].FieldType == typeof(long))
                    {
                        syncedVarFields.Add(new SyncedVarField()
                        {
                            Dirty = false,
                            Target = ((SyncedVar)sortedFields[i].GetCustomAttributes(typeof(SyncedVar), true)[0]).target,
                            FieldInfo = sortedFields[i],
                            FieldType = FieldType.Bool,
                            FieldValue = sortedFields[i].GetValue(this),
                            HookMethod = method
                        });
                    }
                    else if (sortedFields[i].FieldType == typeof(sbyte))
                    {
                        syncedVarFields.Add(new SyncedVarField()
                        {
                            Dirty = false,
                            Target = ((SyncedVar)sortedFields[i].GetCustomAttributes(typeof(SyncedVar), true)[0]).target,
                            FieldInfo = sortedFields[i],
                            FieldType = FieldType.Bool,
                            FieldValue = sortedFields[i].GetValue(this),
                            HookMethod = method
                        });
                    }
                    else if (sortedFields[i].FieldType == typeof(short))
                    {
                        syncedVarFields.Add(new SyncedVarField()
                        {
                            Dirty = false,
                            Target = ((SyncedVar)sortedFields[i].GetCustomAttributes(typeof(SyncedVar), true)[0]).target,
                            FieldInfo = sortedFields[i],
                            FieldType = FieldType.Bool,
                            FieldValue = sortedFields[i].GetValue(this),
                            HookMethod = method
                        });
                    }
                    else if (sortedFields[i].FieldType == typeof(uint))
                    {
                        syncedVarFields.Add(new SyncedVarField()
                        {
                            Dirty = false,
                            Target = ((SyncedVar)sortedFields[i].GetCustomAttributes(typeof(SyncedVar), true)[0]).target,
                            FieldInfo = sortedFields[i],
                            FieldType = FieldType.Bool,
                            FieldValue = sortedFields[i].GetValue(this),
                            HookMethod = method
                        });
                    }
                    else if (sortedFields[i].FieldType == typeof(ulong))
                    {
                        syncedVarFields.Add(new SyncedVarField()
                        {
                            Dirty = false,
                            Target = ((SyncedVar)sortedFields[i].GetCustomAttributes(typeof(SyncedVar), true)[0]).target,
                            FieldInfo = sortedFields[i],
                            FieldType = FieldType.Bool,
                            FieldValue = sortedFields[i].GetValue(this),
                            HookMethod = method
                        });
                    }
                    else if (sortedFields[i].FieldType == typeof(ushort))
                    {
                        syncedVarFields.Add(new SyncedVarField()
                        {
                            Dirty = false,
                            Target = ((SyncedVar)sortedFields[i].GetCustomAttributes(typeof(SyncedVar), true)[0]).target,
                            FieldInfo = sortedFields[i],
                            FieldType = FieldType.Bool,
                            FieldValue = sortedFields[i].GetValue(this),
                            HookMethod = method
                        });
                    }
                    else if(sortedFields[i].FieldType == typeof(string))
                    {
                        syncedVarFields.Add(new SyncedVarField()
                        {
                            Dirty = false,
                            Target = ((SyncedVar)sortedFields[i].GetCustomAttributes(typeof(SyncedVar), true)[0]).target,
                            FieldInfo = sortedFields[i],
                            FieldType = FieldType.Bool,
                            FieldValue = sortedFields[i].GetValue(this),
                            HookMethod = method
                        });
                    }
                    else if(sortedFields[i].FieldType == typeof(Vector3))
                    {
                        syncedVarFields.Add(new SyncedVarField()
                        {
                            Dirty = false,
                            Target = ((SyncedVar)sortedFields[i].GetCustomAttributes(typeof(SyncedVar), true)[0]).target,
                            FieldInfo = sortedFields[i],
                            FieldType = FieldType.Bool,
                            FieldValue = sortedFields[i].GetValue(this),
                            HookMethod = method
                        });
                    }
                    else if(sortedFields[i].FieldType == typeof(Vector2))
                    {
                        syncedVarFields.Add(new SyncedVarField()
                        {
                            Dirty = false,
                            Target = ((SyncedVar)sortedFields[i].GetCustomAttributes(typeof(SyncedVar), true)[0]).target,
                            FieldInfo = sortedFields[i],
                            FieldType = FieldType.Bool,
                            FieldValue = sortedFields[i].GetValue(this),
                            HookMethod = method
                        });
                    }
                    else if (sortedFields[i].FieldType == typeof(Quaternion))
                    {
                        syncedVarFields.Add(new SyncedVarField()
                        {
                            Dirty = false,
                            Target = ((SyncedVar)sortedFields[i].GetCustomAttributes(typeof(SyncedVar), true)[0]).target,
                            FieldInfo = sortedFields[i],
                            FieldType = FieldType.Bool,
                            FieldValue = sortedFields[i].GetValue(this),
                            HookMethod = method
                        });
                    }
                    else if(sortedFields[i].FieldType == typeof(byte[]))
                    {
                        syncedVarFields.Add(new SyncedVarField()
                        {
                            Dirty = false,
                            Target = ((SyncedVar)sortedFields[i].GetCustomAttributes(typeof(SyncedVar), true)[0]).target,
                            FieldInfo = sortedFields[i],
                            FieldType = FieldType.Bool,
                            FieldValue = sortedFields[i].GetValue(this),
                            HookMethod = method
                        });
                    }
                    else
                    {
                        Debug.LogError("MLAPI: The type " + sortedFields[i].FieldType.ToString() + " can not be used as a syncvar");
                    }
                }
            }
            if (syncedVarFields.Count > 255)
            {
                Debug.LogError("MLAPI: You can not have more than 255 SyncVar's per NetworkedBehaviour!");
            }
        }

        internal void OnSyncVarUpdate(object value, byte fieldIndex)
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

            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
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
                    writer.Write((byte)syncCount);
                    writer.Write(networkId); //NetId
                    writer.Write(networkedObject.GetOrderIndex(this)); //Behaviour OrderIndex
                    for (byte i = 0; i < syncedVarFields.Count; i++)
                    {
                        if (syncedVarFields[i].Target && clientId != ownerClientId)
                            continue;
                        writer.Write(i); //FieldIndex
                        switch (syncedVarFields[i].FieldType)
                        {
                            case FieldType.Bool:
                                writer.Write((bool)syncedVarFields[i].FieldInfo.GetValue(this));
                                break;
                            case FieldType.Byte:
                                writer.Write((byte)syncedVarFields[i].FieldInfo.GetValue(this));
                                break;
                            case FieldType.Char:
                                writer.Write((char)syncedVarFields[i].FieldInfo.GetValue(this));
                                break;
                            case FieldType.Double:
                                writer.Write((double)syncedVarFields[i].FieldInfo.GetValue(this));
                                break;
                            case FieldType.Single:
                                writer.Write((float)syncedVarFields[i].FieldInfo.GetValue(this));
                                break;
                            case FieldType.Int:
                                writer.Write((int)syncedVarFields[i].FieldInfo.GetValue(this));
                                break;
                            case FieldType.Long:
                                writer.Write((long)syncedVarFields[i].FieldInfo.GetValue(this));
                                break;
                            case FieldType.SByte:
                                writer.Write((sbyte)syncedVarFields[i].FieldInfo.GetValue(this));
                                break;
                            case FieldType.Short:
                                writer.Write((short)syncedVarFields[i].FieldInfo.GetValue(this));
                                break;
                            case FieldType.UInt:
                                writer.Write((uint)syncedVarFields[i].FieldInfo.GetValue(this));
                                break;
                            case FieldType.ULong:
                                writer.Write((ulong)syncedVarFields[i].FieldInfo.GetValue(this));
                                break;
                            case FieldType.UShort:
                                writer.Write((ushort)syncedVarFields[i].FieldInfo.GetValue(this));
                                break;
                            case FieldType.String:
                                writer.Write((string)syncedVarFields[i].FieldInfo.GetValue(this));
                                break;
                            case FieldType.Vector3:
                                Vector3 vector3 = (Vector3)syncedVarFields[i].FieldInfo.GetValue(this);
                                writer.Write(vector3.x);
                                writer.Write(vector3.y);
                                writer.Write(vector3.z);
                                break;
                            case FieldType.Vector2:
                                Vector2 vector2 = (Vector2)syncedVarFields[i].FieldInfo.GetValue(this);
                                writer.Write(vector2.x);
                                writer.Write(vector2.y);
                                break;
                            case FieldType.Quaternion:
                                Vector3 euler = ((Quaternion)syncedVarFields[i].FieldInfo.GetValue(this)).eulerAngles;
                                writer.Write(euler.x);
                                writer.Write(euler.y);
                                writer.Write(euler.z);
                                break;
                            case FieldType.ByteArray:
                                writer.Write((ushort)((byte[])syncedVarFields[i].FieldInfo.GetValue(this)).Length);
                                writer.Write((byte[])syncedVarFields[i].FieldInfo.GetValue(this));
                                break;
                        }
                    }
                }
                InternalMessageHandler.Send(clientId, "MLAPI_SYNC_VAR_UPDATE", "MLAPI_INTERNAL", stream.ToArray());
            }
        }

        private float lastSyncTime = 0f;
        internal void SyncVarUpdate()
        {
            if (!syncVarInit)
                SyncVarInit();
            SetDirtyness();
            if(NetworkingManager.singleton.NetworkTime - lastSyncTime >= SyncVarSyncDelay)
            {
                byte nonTargetDirtyCount = 0;
                byte totalDirtyCount = 0;
                byte dirtyTargets = 0;
                for (byte i = 0; i < syncedVarFields.Count; i++)
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
                    using (MemoryStream stream = new MemoryStream())
                    {
                        using (BinaryWriter writer = new BinaryWriter(stream))
                        {
                            //Write all indexes
                            writer.Write(totalDirtyCount);
                            writer.Write(networkId); //NetId
                            writer.Write(networkedObject.GetOrderIndex(this)); //Behaviour OrderIndex
                            for (byte i = 0; i < syncedVarFields.Count; i++)
                            {
                                //Writes all the indexes of the dirty syncvars.
                                if (syncedVarFields[i].Dirty == true)
                                {
                                    writer.Write(i); //FieldIndex
                                    switch (syncedVarFields[i].FieldType)
                                    {
                                        case FieldType.Bool:
                                            writer.Write((bool)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Byte:
                                            writer.Write((byte)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Char:
                                            writer.Write((char)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Double:
                                            writer.Write((double)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Single:
                                            writer.Write((float)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Int:
                                            writer.Write((int)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Long:
                                            writer.Write((long)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.SByte:
                                            writer.Write((sbyte)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Short:
                                            writer.Write((short)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.UInt:
                                            writer.Write((uint)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.ULong:
                                            writer.Write((ulong)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.UShort:
                                            writer.Write((ushort)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.String:
                                            writer.Write((string)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Vector3:
                                            Vector3 vector3 = (Vector3)syncedVarFields[i].FieldInfo.GetValue(this);
                                            writer.Write(vector3.x);
                                            writer.Write(vector3.y);
                                            writer.Write(vector3.z);
                                            break;
                                        case FieldType.Vector2:
                                            Vector2 vector2 = (Vector2)syncedVarFields[i].FieldInfo.GetValue(this);
                                            writer.Write(vector2.x);
                                            writer.Write(vector2.y);
                                            break;
                                        case FieldType.Quaternion:
                                            Vector3 euler = ((Quaternion)syncedVarFields[i].FieldInfo.GetValue(this)).eulerAngles;
                                            writer.Write(euler.x);
                                            writer.Write(euler.y);
                                            writer.Write(euler.z);
                                            break;
                                        case FieldType.ByteArray:
                                            writer.Write((ushort)((byte[])syncedVarFields[i].FieldInfo.GetValue(this)).Length);
                                            writer.Write((byte[])syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;

                                    }
                                    syncedVarFields[i].FieldValue = syncedVarFields[i].FieldInfo.GetValue(this);
                                    syncedVarFields[i].Dirty = false;
                                }
                            }
                        }
                        InternalMessageHandler.Send("MLAPI_SYNC_VAR_UPDATE", "MLAPI_INTERNAL", stream.ToArray());
                    }
                }
                else
                {
                    //It's sync time. This is the target receivers packet.
                    using (MemoryStream stream = new MemoryStream())
                    {
                        using (BinaryWriter writer = new BinaryWriter(stream))
                        {
                            //Write all indexes
                            writer.Write(totalDirtyCount);
                            writer.Write(networkId); //NetId
                            writer.Write(networkedObject.GetOrderIndex(this)); //Behaviour OrderIndex
                            for (byte i = 0; i < syncedVarFields.Count; i++)
                            {
                                //Writes all the indexes of the dirty syncvars.
                                if (syncedVarFields[i].Dirty == true)
                                {
                                    writer.Write(i); //FieldIndex
                                    switch (syncedVarFields[i].FieldType)
                                    {
                                        case FieldType.Bool:
                                            writer.Write((bool)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Byte:
                                            writer.Write((byte)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Char:
                                            writer.Write((char)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Double:
                                            writer.Write((double)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Single:
                                            writer.Write((float)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Int:
                                            writer.Write((int)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Long:
                                            writer.Write((long)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.SByte:
                                            writer.Write((sbyte)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Short:
                                            writer.Write((short)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.UInt:
                                            writer.Write((uint)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.ULong:
                                            writer.Write((ulong)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.UShort:
                                            writer.Write((ushort)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.String:
                                            writer.Write((string)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Vector3:
                                            Vector3 vector3 = (Vector3)syncedVarFields[i].FieldInfo.GetValue(this);
                                            writer.Write(vector3.x);
                                            writer.Write(vector3.y);
                                            writer.Write(vector3.z);
                                            break;
                                        case FieldType.Vector2:
                                            Vector2 vector2 = (Vector2)syncedVarFields[i].FieldInfo.GetValue(this);
                                            writer.Write(vector2.x);
                                            writer.Write(vector2.y);
                                            break;
                                        case FieldType.Quaternion:
                                            Vector3 euler = ((Quaternion)syncedVarFields[i].FieldInfo.GetValue(this)).eulerAngles;
                                            writer.Write(euler.x);
                                            writer.Write(euler.y);
                                            writer.Write(euler.z);
                                            break;
                                        case FieldType.ByteArray:
                                            writer.Write((ushort)((byte[])syncedVarFields[i].FieldInfo.GetValue(this)).Length);
                                            writer.Write((byte[])syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;

                                    }
                                }
                            }
                        }
                        InternalMessageHandler.Send(ownerClientId, "MLAPI_SYNC_VAR_UPDATE", "MLAPI_INTERNAL", stream.ToArray()); //Send only to target
                    }

                    //It's sync time. This is the NON target receivers packet.
                    using (MemoryStream stream = new MemoryStream())
                    {
                        using (BinaryWriter writer = new BinaryWriter(stream))
                        {
                            //Write all indexes
                            writer.Write(nonTargetDirtyCount);
                            writer.Write(networkId); //NetId
                            writer.Write(networkedObject.GetOrderIndex(this)); //Behaviour OrderIndex
                            for (byte i = 0; i < syncedVarFields.Count; i++)
                            {
                                //Writes all the indexes of the dirty syncvars.
                                if (syncedVarFields[i].Dirty == true && !syncedVarFields[i].Target)
                                {
                                    writer.Write(i); //FieldIndex
                                    switch (syncedVarFields[i].FieldType)
                                    {
                                        case FieldType.Bool:
                                            writer.Write((bool)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Byte:
                                            writer.Write((byte)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Char:
                                            writer.Write((char)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Double:
                                            writer.Write((double)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Single:
                                            writer.Write((float)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Int:
                                            writer.Write((int)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Long:
                                            writer.Write((long)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.SByte:
                                            writer.Write((sbyte)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Short:
                                            writer.Write((short)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.UInt:
                                            writer.Write((uint)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.ULong:
                                            writer.Write((ulong)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.UShort:
                                            writer.Write((ushort)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.String:
                                            writer.Write((string)syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;
                                        case FieldType.Vector3:
                                            Vector3 vector3 = (Vector3)syncedVarFields[i].FieldInfo.GetValue(this);
                                            writer.Write(vector3.x);
                                            writer.Write(vector3.y);
                                            writer.Write(vector3.z);
                                            break;
                                        case FieldType.Vector2:
                                            Vector2 vector2 = (Vector2)syncedVarFields[i].FieldInfo.GetValue(this);
                                            writer.Write(vector2.x);
                                            writer.Write(vector2.y);
                                            break;
                                        case FieldType.Quaternion:
                                            Vector3 euler = ((Quaternion)syncedVarFields[i].FieldInfo.GetValue(this)).eulerAngles;
                                            writer.Write(euler.x);
                                            writer.Write(euler.y);
                                            writer.Write(euler.z);
                                            break;
                                        case FieldType.ByteArray:
                                            writer.Write((ushort)((byte[])syncedVarFields[i].FieldInfo.GetValue(this)).Length);
                                            writer.Write((byte[])syncedVarFields[i].FieldInfo.GetValue(this));
                                            break;

                                    }
                                    syncedVarFields[i].FieldValue = syncedVarFields[i].FieldInfo.GetValue(this);
                                    syncedVarFields[i].Dirty = false;
                                }
                            }
                        }
                        InternalMessageHandler.Send("MLAPI_SYNC_VAR_UPDATE", "MLAPI_INTERNAL", stream.ToArray(), ownerClientId); // Send to everyone except target.
                    }
                }
                lastSyncTime = NetworkingManager.singleton.NetworkTime;
            }
        }

        private void SetDirtyness()
        {
            if (!isServer)
                return;
            for (int i = 0; i < syncedVarFields.Count; i++)
            {
                switch (syncedVarFields[i].FieldType)
                {
                    case FieldType.Bool:
                        if ((bool)syncedVarFields[i].FieldInfo.GetValue(this) != (bool)syncedVarFields[i].FieldValue)
                            syncedVarFields[i].Dirty = true; //This fields value is out of sync!
                        else
                            syncedVarFields[i].Dirty = false; //Up to date
                        break;
                    case FieldType.Byte:
                        if ((byte)syncedVarFields[i].FieldInfo.GetValue(this) != (byte)syncedVarFields[i].FieldValue)
                            syncedVarFields[i].Dirty = true; //This fields value is out of sync!
                        else
                            syncedVarFields[i].Dirty = false; //Up to date
                        break;
                    case FieldType.Char:
                        if ((char)syncedVarFields[i].FieldInfo.GetValue(this) != (char)syncedVarFields[i].FieldValue)
                            syncedVarFields[i].Dirty = true; //This fields value is out of sync!
                        else
                            syncedVarFields[i].Dirty = false; //Up to date
                        break;
                    case FieldType.Double:
                        if ((double)syncedVarFields[i].FieldInfo.GetValue(this) != (double)syncedVarFields[i].FieldValue)
                            syncedVarFields[i].Dirty = true; //This fields value is out of sync!
                        else
                            syncedVarFields[i].Dirty = false; //Up to date
                        break;
                    case FieldType.Single:
                        if ((float)syncedVarFields[i].FieldInfo.GetValue(this) != (float)syncedVarFields[i].FieldValue)
                            syncedVarFields[i].Dirty = true; //This fields value is out of sync!
                        else
                            syncedVarFields[i].Dirty = false; //Up to date
                        break;
                    case FieldType.Int:
                        if ((int)syncedVarFields[i].FieldInfo.GetValue(this) != (int)syncedVarFields[i].FieldValue)
                            syncedVarFields[i].Dirty = true; //This fields value is out of sync!
                        else
                            syncedVarFields[i].Dirty = false; //Up to date
                        break;
                    case FieldType.Long:
                        if ((long)syncedVarFields[i].FieldInfo.GetValue(this) != (long)syncedVarFields[i].FieldValue)
                            syncedVarFields[i].Dirty = true; //This fields value is out of sync!
                        else
                            syncedVarFields[i].Dirty = false; //Up to date
                        break;
                    case FieldType.SByte:
                        if ((sbyte)syncedVarFields[i].FieldInfo.GetValue(this) != (sbyte)syncedVarFields[i].FieldValue)
                            syncedVarFields[i].Dirty = true; //This fields value is out of sync!
                        else
                            syncedVarFields[i].Dirty = false; //Up to date
                        break;
                    case FieldType.Short:
                        if ((short)syncedVarFields[i].FieldInfo.GetValue(this) != (short)syncedVarFields[i].FieldValue)
                            syncedVarFields[i].Dirty = true; //This fields value is out of sync!
                        else
                            syncedVarFields[i].Dirty = false; //Up to date
                        break;
                    case FieldType.UInt:
                        if ((uint)syncedVarFields[i].FieldInfo.GetValue(this) != (uint)syncedVarFields[i].FieldValue)
                            syncedVarFields[i].Dirty = true; //This fields value is out of sync!
                        else
                            syncedVarFields[i].Dirty = false; //Up to date
                        break;
                    case FieldType.ULong:
                        if ((ulong)syncedVarFields[i].FieldInfo.GetValue(this) != (ulong)syncedVarFields[i].FieldValue)
                            syncedVarFields[i].Dirty = true; //This fields value is out of sync!
                        else
                            syncedVarFields[i].Dirty = false; //Up to date
                        break;
                    case FieldType.UShort:
                        if ((ushort)syncedVarFields[i].FieldInfo.GetValue(this) != (ushort)syncedVarFields[i].FieldValue)
                            syncedVarFields[i].Dirty = true; //This fields value is out of sync!
                        else
                            syncedVarFields[i].Dirty = false; //Up to date
                        break;
                    case FieldType.String:
                        if ((string)syncedVarFields[i].FieldInfo.GetValue(this) != (string)syncedVarFields[i].FieldValue)
                            syncedVarFields[i].Dirty = true; //This fields value is out of sync!
                        else
                            syncedVarFields[i].Dirty = false; //Up to date
                        break;
                    case FieldType.Vector3:
                        if ((Vector3)syncedVarFields[i].FieldInfo.GetValue(this) != (Vector3)syncedVarFields[i].FieldValue)
                            syncedVarFields[i].Dirty = true; //This fields value is out of sync!
                        else
                            syncedVarFields[i].Dirty = false; //Up to date
                        break;
                    case FieldType.Vector2:
                        if ((Vector2)syncedVarFields[i].FieldInfo.GetValue(this) != (Vector2)syncedVarFields[i].FieldValue)
                            syncedVarFields[i].Dirty = true; //This fields value is out of sync!
                        else
                            syncedVarFields[i].Dirty = false; //Up to date
                        break;
                    case FieldType.Quaternion:
                        if ((Quaternion)syncedVarFields[i].FieldInfo.GetValue(this) != (Quaternion)syncedVarFields[i].FieldValue)
                            syncedVarFields[i].Dirty = true; //This fields value is out of sync!
                        else
                            syncedVarFields[i].Dirty = false; //Up to date
                        break;
                    case FieldType.ByteArray:
                        if(((byte[])syncedVarFields[i].FieldInfo.GetValue(this)).SequenceEqual(((byte[])syncedVarFields[i].FieldValue)))
                            syncedVarFields[i].Dirty = true; //This fields value is out of sync!
                        else
                            syncedVarFields[i].Dirty = false; //Up to date
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
            InternalMessageHandler.Send(NetId.ServerNetId.GetClientId(), messageType, channelName, data);
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
            InternalMessageHandler.Send(NetId.ServerNetId.GetClientId(), messageType, channelName, data, networkId, networkedObject.GetOrderIndex(this));            
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
        protected void SendToLocalClient(string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageHashSet.Contains(MessageManager.messageTypes[messageType])))
            {
                Debug.LogWarning("MLAPI: Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }
            InternalMessageHandler.Send(ownerClientId, messageType, channelName, data);
        }

        /// <summary>
        /// Sends a binary serialized class to the server from client
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
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
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageHashSet.Contains(MessageManager.messageTypes[messageType])))
            {
                Debug.LogWarning("MLAPI: Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }
            InternalMessageHandler.Send(ownerClientId, messageType, channelName, data, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
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
            InternalMessageHandler.Send(messageType, channelName, data, ownerClientId);
        }

        /// <summary>
        /// Sends a binary serialized class to all clients except to the owner object from the server
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
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
            InternalMessageHandler.Send(messageType, channelName, data, ownerClientId, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
        /// Sends a binary serialized class to all clients except to the owner object from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
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
        protected void SendToClient(uint clientId, string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageHashSet.Contains(MessageManager.messageTypes[messageType])))
            {
                Debug.LogWarning("MLAPI: Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }
            InternalMessageHandler.Send(clientId, messageType, channelName, data);
        }

        /// <summary>
        /// Sends a binary serialized class to a client with a given clientId from Server
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="clientId">The clientId to send the message to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        protected void SendToClient<T>(int clientId, string messageType, string channelName, T instance)
        {
            SendToClient(clientId, messageType, channelName, BinarySerializer.Serialize<T>(instance));
        }

        /// <summary>
        /// Sends a buffer to a client with a given clientId from Server. Only handlers on this NetworkedBehaviour gets invoked
        /// </summary>
        /// <param name="clientId">The clientId to send the message to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        protected void SendToClientTarget(uint clientId, string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageHashSet.Contains(MessageManager.messageTypes[messageType])))
            {
                Debug.LogWarning("MLAPI: Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }
            InternalMessageHandler.Send(clientId, messageType, channelName, data, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
        /// Sends a buffer to a client with a given clientId from Server. Only handlers on this NetworkedBehaviour gets invoked
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="clientId">The clientId to send the message to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
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
        protected void SendToClients(uint[] clientIds, string messageType, string channelName, byte[] data)
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
            InternalMessageHandler.Send(clientIds, messageType, channelName, data);
        }

        /// <summary>
        /// Sends a binary serialized class to multiple clients from the server
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        protected void SendToClients<T>(int[] clientIds, string messageType, string channelName, T instance)
        {
            SendToClients(clientIds, messageType, channelName, BinarySerializer.Serialize<T>(instance));
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server. Only handlers on this NetworkedBehaviour gets invoked
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        protected void SendToClientsTarget(uint[] clientIds, string messageType, string channelName, byte[] data)
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
            InternalMessageHandler.Send(clientIds, messageType, channelName, data, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server. Only handlers on this NetworkedBehaviour gets invoked
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
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
        protected void SendToClients(List<uint> clientIds, string messageType, string channelName, byte[] data)
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
            InternalMessageHandler.Send(clientIds, messageType, channelName, data);
        }

        /// <summary>
        /// Sends a binary serialized class to multiple clients from the server
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
        protected void SendToClients<T>(List<int> clientIds, string messageType, string channelName, T instance)
        {
            SendToClients(clientIds, messageType, channelName, BinarySerializer.Serialize<T>(instance));
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server. Only handlers on this NetworkedBehaviour gets invoked
        /// </summary>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>
        /// <param name="data">The binary data to send</param>
        protected void SendToClientsTarget(List<uint> clientIds, string messageType, string channelName, byte[] data)
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
            InternalMessageHandler.Send(clientIds, messageType, channelName, data, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
        /// Sends a buffer to multiple clients from the server. Only handlers on this NetworkedBehaviour gets invoked
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="clientIds">The clientId's to send to</param>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
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
            InternalMessageHandler.Send(messageType, channelName, data);
        }

        /// <summary>
        /// Sends a buffer to all clients from the server
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
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
            InternalMessageHandler.Send(messageType, channelName, data, networkId, networkedObject.GetOrderIndex(this));
        }

        /// <summary>
        /// Sends a buffer to all clients from the server. Only handlers on this NetworkedBehaviour will get invoked
        /// </summary>
        /// <typeparam name="T">The class type to send</typeparam>
        /// <param name="messageType">User defined messageType</param>
        /// <param name="channelName">User defined channelName</param>	
        /// <param name="instance">The instance to send</param>
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
            return SpawnManager.spawnedObjects[networkId];
        }
    }
}
