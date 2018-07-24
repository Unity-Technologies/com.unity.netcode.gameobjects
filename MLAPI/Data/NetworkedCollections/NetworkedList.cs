using System.Collections;
using System.Collections.Generic;
using System.IO;
using MLAPI.MonoBehaviours.Core;
using MLAPI.NetworkingManagerComponents.Binary;

namespace MLAPI.Data.NetworkedCollections
{
    /// <summary>
    /// Event based networkedVar container for syncing Lists
    /// </summary>
    /// <typeparam name="T">The type for the list</typeparam>
    public class NetworkedList<T> : IList<T>, INetworkedVar
    {
        internal struct NetworkedListEvent<T>
        {
            internal enum NetworkedListEventType
            {
                Add,
                Insert,
                Remove,
                RemoveAt,
                Value,
                Clear
            }
            
            internal NetworkedListEventType eventType;
            internal T value;
            internal int index;
        }
        
        private readonly IList<T> list = new List<T>();
        private List<NetworkedListEvent<T>> dirtyEvents = new List<NetworkedListEvent<T>>();
        private NetworkedBehaviour networkedBehaviour;
        /// <summary>
        /// Gets the last time the variable was synced
        /// </summary>
        public float LastSyncedTime { get; internal set; }
        /// <summary>
        /// The settings for this container
        /// </summary>
        public readonly NetworkedVarSettings Settings = new NetworkedVarSettings();
        
        
        public NetworkedList()
        {
            
        }
        
        public NetworkedList(NetworkedVarSettings settings)
        {
            this.Settings = settings;
        }
        
        public NetworkedList(NetworkedVarSettings settings, IList<T> value)
        {
            this.Settings = settings;
            this.list = value;
        }
        
        public NetworkedList(IList<T> value)
        {
            this.list = value;
        }
        
        /// <inheritdoc />
        public void ResetDirty()
        {
            dirtyEvents.Clear();
            LastSyncedTime = NetworkingManager.singleton.NetworkTime;
        }

        /// <inheritdoc />
        public bool IsDirty()
        {
            if (dirtyEvents.Count == 0) return false;
            if (Settings.SendTickrate <= 0) return true;
            if (NetworkingManager.singleton.NetworkTime - LastSyncedTime >= (1f / Settings.SendTickrate)) return true;
            return false;
        }

        /// <inheritdoc />
        public string GetChannel()
        {
            return Settings.SendChannel;
        }

        /// <inheritdoc />
        public bool CanClientWrite(uint clientId)
        {
            switch (Settings.WritePermission)
            {
                case NetworkedVarPermission.Everyone:
                    return true;
                case NetworkedVarPermission.ServerOnly:
                    return false;
                case NetworkedVarPermission.OwnerOnly:
                    return networkedBehaviour.OwnerClientId == clientId;
                case NetworkedVarPermission.Custom:
                {
                    if (Settings.WritePermissionCallback == null) return false;
                    return Settings.WritePermissionCallback(clientId);
                }
            }

            return true;
        }

        /// <inheritdoc />
        public bool CanClientRead(uint clientId)
        {
            switch (Settings.ReadPermission)
            {
                case NetworkedVarPermission.Everyone:
                    return true;
                case NetworkedVarPermission.ServerOnly:
                    return false;
                case NetworkedVarPermission.OwnerOnly:
                    return networkedBehaviour.OwnerClientId == clientId;
                case NetworkedVarPermission.Custom:
                {
                    if (Settings.ReadPermissionCallback == null) return false;
                    return Settings.ReadPermissionCallback(clientId);
                }
            }
            return true;
        }

        /// <inheritdoc />
        public void WriteDelta(Stream stream)
        {
            BitWriter writer = new BitWriter(stream);
            writer.WriteUInt16Packed((ushort)dirtyEvents.Count);
            for (int i = 0; i < dirtyEvents.Count; i++)
            {
                writer.WriteBits((byte)dirtyEvents[i].eventType, 3);
                switch (dirtyEvents[i].eventType)
                {
                    //Fuck me these signatures are proper aids
                    case NetworkedList<T>.NetworkedListEvent<T>.NetworkedListEventType.Add:
                        {
                            writer.WriteObjectPacked(dirtyEvents[i].value); //BOX
                        }
                        break;
                    case NetworkedList<T>.NetworkedListEvent<T>.NetworkedListEventType.Insert:
                        {
                            writer.WriteInt32Packed(dirtyEvents[i].index);
                            writer.WriteObjectPacked(dirtyEvents[i].value); //BOX
                        }
                        break;
                    case NetworkedList<T>.NetworkedListEvent<T>.NetworkedListEventType.Remove:
                        {
                            writer.WriteObjectPacked(dirtyEvents[i].value); //BOX
                        }
                        break;
                    case NetworkedList<T>.NetworkedListEvent<T>.NetworkedListEventType.RemoveAt:
                        {
                            writer.WriteInt32Packed(dirtyEvents[i].index);
                        }
                        break;
                    case NetworkedList<T>.NetworkedListEvent<T>.NetworkedListEventType.Value:
                        {
                            writer.WriteInt32Packed(dirtyEvents[i].index);
                            writer.WriteObjectPacked(dirtyEvents[i].value); //BOX
                        }

                        break;
                    case NetworkedList<T>.NetworkedListEvent<T>.NetworkedListEventType.Clear:
                        {
                            //Nothing has to be written
                        }
                        break;
                }
            }
        }

        /// <inheritdoc />
        public void WriteField(Stream stream)
        {
            BitWriter writer = new BitWriter(stream);
            writer.WriteUInt16Packed((ushort)list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                writer.WriteObjectPacked(list[i]); //BOX
            }
        }

        /// <inheritdoc />
        public void ReadField(Stream stream)
        {
            BitReader reader = new BitReader(stream);
            list.Clear();
            ushort count = reader.ReadUInt16Packed();
            for (int i = 0; i < count; i++)
            {
                list.Add((T)reader.ReadObjectPacked(typeof(T))); //BOX
            }
        }

        /// <inheritdoc />
        public void ReadDelta(Stream stream)
        {
            BitReader reader = new BitReader(stream);
            ushort deltaCount = reader.ReadUInt16Packed();
            for (int i = 0; i < deltaCount; i++)
            {
                NetworkedListEvent<T>.NetworkedListEventType eventType = (NetworkedListEvent<T>.NetworkedListEventType)reader.ReadBits(3);
                switch (eventType)
                {
                    case NetworkedListEvent<T>.NetworkedListEventType.Add:
                        {
                            list.Add((T)reader.ReadObjectPacked(typeof(T))); //BOX
                        }
                        break;
                    case NetworkedList<T>.NetworkedListEvent<T>.NetworkedListEventType.Insert:
                        {
                            int index = reader.ReadInt32Packed();
                            list.Insert(index, (T)reader.ReadObjectPacked(typeof(T))); //BOX
                        }
                        break;
                    case NetworkedList<T>.NetworkedListEvent<T>.NetworkedListEventType.Remove:
                        {
                            list.Remove((T)reader.ReadObjectPacked(typeof(T))); //BOX
                        }
                        break;
                    case NetworkedList<T>.NetworkedListEvent<T>.NetworkedListEventType.RemoveAt:
                        {
                            int index = reader.ReadInt32Packed();
                            list.RemoveAt(index);
                        }
                        break;
                    case NetworkedList<T>.NetworkedListEvent<T>.NetworkedListEventType.Value:
                        {
                            int index = reader.ReadInt32Packed();
                            if (index < list.Count) list[index] = (T)reader.ReadObjectPacked(typeof(T)); //BOX
                        }
                        break;
                    case NetworkedList<T>.NetworkedListEvent<T>.NetworkedListEventType.Clear:
                        {
                            //Read nothing
                            list.Clear();
                        }
                        break;
                }
            }
        }

        /// <inheritdoc />
        public void SetNetworkedBehaviour(NetworkedBehaviour behaviour)
        {
            networkedBehaviour = behaviour;
        }
        
        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) list).GetEnumerator();
        }

        /// <inheritdoc />
        public void Add(T item)
        {
            list.Add(item);
            dirtyEvents.Add(new NetworkedListEvent<T>()
            {
                eventType = NetworkedListEvent<T>.NetworkedListEventType.Add,
                value = item
            });
        }

        /// <inheritdoc />
        public void Clear()
        {
            list.Clear();
            dirtyEvents.Add(new NetworkedListEvent<T>()
            {
                eventType = NetworkedListEvent<T>.NetworkedListEventType.Clear
            });
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            return list.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            bool state = list.Remove(item);
            if (state)
            {
                dirtyEvents.Add(new NetworkedListEvent<T>()
                {
                    eventType = NetworkedListEvent<T>.NetworkedListEventType.Remove,
                    value = item
                });
            }
            return state;
        }

        /// <inheritdoc />
        public int Count => list.Count;

        /// <inheritdoc />
        public bool IsReadOnly => list.IsReadOnly;

        /// <inheritdoc />
        public int IndexOf(T item)
        {
            return list.IndexOf(item);
        }

        /// <inheritdoc />
        public void Insert(int index, T item)
        {
            list.Insert(index, item);
            dirtyEvents.Add(new NetworkedListEvent<T>()
            {
                eventType = NetworkedListEvent<T>.NetworkedListEventType.Insert,
                index =  index,
                value = item
            });
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            list.RemoveAt(index);
            dirtyEvents.Add(new NetworkedListEvent<T>()
            {
                eventType = NetworkedListEvent<T>.NetworkedListEventType.RemoveAt,
                index = index
            });
        }

        /// <inheritdoc />
        public T this[int index]
        {
            get
            {
                return list[index];
            }
            set
            {
                list[index] = value;
                dirtyEvents.Add(new NetworkedListEvent<T>()
                {
                    eventType = NetworkedListEvent<T>.NetworkedListEventType.Value,
                    index = index,
                    value = value
                });
            }
        }
    }
}
