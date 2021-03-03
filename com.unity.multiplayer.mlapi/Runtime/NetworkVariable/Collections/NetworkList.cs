using System.Collections;
using System.Collections.Generic;
using System.IO;
using MLAPI.Serialization.Pooled;
using MLAPI.Transports;

namespace MLAPI.NetworkVariable.Collections
{
    /// <summary>
    /// Event based NetworkVariable container for syncing Lists
    /// </summary>
    /// <typeparam name="T">The type for the list</typeparam>
    public class NetworkList<T> : IList<T>, INetworkVariable
    {
        private readonly IList<T> list = new List<T>();
        private readonly List<NetworkListEvent<T>> dirtyEvents = new List<NetworkListEvent<T>>();
        private NetworkBehaviour networkBehaviour;

        /// <summary>
        /// Gets the last time the variable was synced
        /// </summary>
        public float LastSyncedTime { get; internal set; }

        /// <summary>
        /// The settings for this container
        /// </summary>
        public readonly NetworkVariableSettings Settings = new NetworkVariableSettings();

        /// <summary>
        /// Delegate type for list changed event
        /// </summary>
        /// <param name="changeEvent">Struct containing information about the change event</param>
        public delegate void OnListChangedDelegate(NetworkListEvent<T> changeEvent);

        /// <summary>
        /// The callback to be invoked when the list gets changed
        /// </summary>
        public event OnListChangedDelegate OnListChanged;

        /// <summary>
        /// Creates a NetworkList with the default value and settings
        /// </summary>
        public NetworkList() { }

        /// <summary>
        /// Creates a NetworkList with the default value and custom settings
        /// </summary>
        /// <param name="settings">The settings to use for the NetworkList</param>
        public NetworkList(NetworkVariableSettings settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// Creates a NetworkList with a custom value and custom settings
        /// </summary>
        /// <param name="settings">The settings to use for the NetworkList</param>
        /// <param name="value">The initial value to use for the NetworkList</param>
        public NetworkList(NetworkVariableSettings settings, IList<T> value)
        {
            Settings = settings;
            list = value;
        }

        /// <summary>
        /// Creates a NetworkList with a custom value and the default settings
        /// </summary>
        /// <param name="value">The initial value to use for the NetworkList</param>
        public NetworkList(IList<T> value)
        {
            list = value;
        }

        /// <inheritdoc />
        public void ResetDirty()
        {
            dirtyEvents.Clear();
            LastSyncedTime = NetworkManager.Singleton.NetworkTime;
        }

        /// <inheritdoc />
        public bool IsDirty()
        {
            if (dirtyEvents.Count == 0) return false;
            if (Settings.SendTickrate == 0) return true;
            if (Settings.SendTickrate < 0) return false;
            if (NetworkManager.Singleton.NetworkTime - LastSyncedTime >= (1f / Settings.SendTickrate)) return true;
            return false;
        }

        /// <inheritdoc />
        public NetworkChannel GetChannel()
        {
            return Settings.SendNetworkChannel;
        }

        /// <inheritdoc />
        public bool CanClientWrite(ulong clientId)
        {
            switch (Settings.WritePermission)
            {
                case NetworkVariablePermission.Everyone:
                    return true;
                case NetworkVariablePermission.ServerOnly:
                    return false;
                case NetworkVariablePermission.OwnerOnly:
                    return networkBehaviour.OwnerClientId == clientId;
                case NetworkVariablePermission.Custom:
                {
                    if (Settings.WritePermissionCallback == null) return false;
                    return Settings.WritePermissionCallback(clientId);
                }
            }

            return true;
        }

        /// <inheritdoc />
        public bool CanClientRead(ulong clientId)
        {
            switch (Settings.ReadPermission)
            {
                case NetworkVariablePermission.Everyone:
                    return true;
                case NetworkVariablePermission.ServerOnly:
                    return false;
                case NetworkVariablePermission.OwnerOnly:
                    return networkBehaviour.OwnerClientId == clientId;
                case NetworkVariablePermission.Custom:
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
            using (PooledNetworkWriter writer = PooledNetworkWriter.Get(stream))
            {
                writer.WriteUInt16Packed((ushort)dirtyEvents.Count);
                for (int i = 0; i < dirtyEvents.Count; i++)
                {
                    writer.WriteBits((byte)dirtyEvents[i].eventType, 3);
                    switch (dirtyEvents[i].eventType)
                    {
                        case NetworkListEvent<T>.EventType.Add:
                        {
                            writer.WriteObjectPacked(dirtyEvents[i].value); //BOX
                        }
                            break;
                        case NetworkListEvent<T>.EventType.Insert:
                        {
                            writer.WriteInt32Packed(dirtyEvents[i].index);
                            writer.WriteObjectPacked(dirtyEvents[i].value); //BOX
                        }
                            break;
                        case NetworkListEvent<T>.EventType.Remove:
                        {
                            writer.WriteObjectPacked(dirtyEvents[i].value); //BOX
                        }
                            break;
                        case NetworkListEvent<T>.EventType.RemoveAt:
                        {
                            writer.WriteInt32Packed(dirtyEvents[i].index);
                        }
                            break;
                        case NetworkListEvent<T>.EventType.Value:
                        {
                            writer.WriteInt32Packed(dirtyEvents[i].index);
                            writer.WriteObjectPacked(dirtyEvents[i].value); //BOX
                        }
                            break;
                        case NetworkListEvent<T>.EventType.Clear:
                        {
                            //Nothing has to be written
                        }
                            break;
                    }
                }
            }
        }

        /// <inheritdoc />
        public void WriteField(Stream stream)
        {
            using (PooledNetworkWriter writer = PooledNetworkWriter.Get(stream))
            {
                writer.WriteUInt16Packed((ushort)list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    writer.WriteObjectPacked(list[i]); //BOX
                }
            }
        }

        /// <inheritdoc />
        public void ReadField(Stream stream, ushort localTick, ushort remoteTick)
        {
            using (PooledNetworkReader reader = PooledNetworkReader.Get(stream))
            {
                list.Clear();
                ushort count = reader.ReadUInt16Packed();
                for (int i = 0; i < count; i++)
                {
                    list.Add((T)reader.ReadObjectPacked(typeof(T))); //BOX
                }
            }
        }

        /// <inheritdoc />
        public void ReadDelta(Stream stream, bool keepDirtyDelta, ushort localTick, ushort remoteTick)
        {
            using (PooledNetworkReader reader = PooledNetworkReader.Get(stream))
            {
                ushort deltaCount = reader.ReadUInt16Packed();
                for (int i = 0; i < deltaCount; i++)
                {
                    NetworkListEvent<T>.EventType eventType = (NetworkListEvent<T>.EventType)reader.ReadBits(3);
                    switch (eventType)
                    {
                        case NetworkListEvent<T>.EventType.Add:
                        {
                            list.Add((T)reader.ReadObjectPacked(typeof(T))); //BOX

                            if (OnListChanged != null)
                            {
                                OnListChanged(new NetworkListEvent<T>
                                {
                                    eventType = eventType,
                                    index = list.Count - 1,
                                    value = list[list.Count - 1]
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                dirtyEvents.Add(new NetworkListEvent<T>()
                                {
                                    eventType = eventType,
                                    index = list.Count - 1,
                                    value = list[list.Count - 1]
                                });
                            }
                        }
                            break;
                        case NetworkListEvent<T>.EventType.Insert:
                        {
                            int index = reader.ReadInt32Packed();
                            list.Insert(index, (T)reader.ReadObjectPacked(typeof(T))); //BOX

                            if (OnListChanged != null)
                            {
                                OnListChanged(new NetworkListEvent<T>
                                {
                                    eventType = eventType,
                                    index = index,
                                    value = list[index]
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                dirtyEvents.Add(new NetworkListEvent<T>()
                                {
                                    eventType = eventType,
                                    index = index,
                                    value = list[index]
                                });
                            }
                        }
                            break;
                        case NetworkListEvent<T>.EventType.Remove:
                        {
                            T value = (T)reader.ReadObjectPacked(typeof(T)); //BOX
                            int index = list.IndexOf(value);
                            list.RemoveAt(index);

                            if (OnListChanged != null)
                            {
                                OnListChanged(new NetworkListEvent<T>
                                {
                                    eventType = eventType,
                                    index = index,
                                    value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                dirtyEvents.Add(new NetworkListEvent<T>()
                                {
                                    eventType = eventType,
                                    index = index,
                                    value = value
                                });
                            }
                        }
                            break;
                        case NetworkListEvent<T>.EventType.RemoveAt:
                        {
                            int index = reader.ReadInt32Packed();
                            T value = list[index];
                            list.RemoveAt(index);

                            if (OnListChanged != null)
                            {
                                OnListChanged(new NetworkListEvent<T>
                                {
                                    eventType = eventType,
                                    index = index,
                                    value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                dirtyEvents.Add(new NetworkListEvent<T>()
                                {
                                    eventType = eventType,
                                    index = index,
                                    value = value
                                });
                            }
                        }
                            break;
                        case NetworkListEvent<T>.EventType.Value:
                        {
                            int index = reader.ReadInt32Packed();
                            T value = (T)reader.ReadObjectPacked(typeof(T)); //BOX
                            if (index < list.Count) list[index] = value;

                            if (OnListChanged != null)
                            {
                                OnListChanged(new NetworkListEvent<T>
                                {
                                    eventType = eventType,
                                    index = index,
                                    value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                dirtyEvents.Add(new NetworkListEvent<T>()
                                {
                                    eventType = eventType,
                                    index = index,
                                    value = value
                                });
                            }
                        }
                            break;
                        case NetworkListEvent<T>.EventType.Clear:
                        {
                            //Read nothing
                            list.Clear();

                            if (OnListChanged != null)
                            {
                                OnListChanged(new NetworkListEvent<T>
                                {
                                    eventType = eventType,
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                dirtyEvents.Add(new NetworkListEvent<T>()
                                {
                                    eventType = eventType
                                });
                            }
                        }
                            break;
                    }
                }
            }
        }

        /// <inheritdoc />
        public void SetNetworkBehaviour(NetworkBehaviour behaviour)
        {
            networkBehaviour = behaviour;
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)list).GetEnumerator();
        }

        /// <inheritdoc />
        public void Add(T item)
        {
            if (NetworkManager.Singleton.IsServer) list.Add(item);

            NetworkListEvent<T> listEvent = new NetworkListEvent<T>()
            {
                eventType = NetworkListEvent<T>.EventType.Add,
                value = item,
                index = list.Count - 1
            };

            HandleAddListEvent(listEvent);
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (NetworkManager.Singleton.IsServer) list.Clear();

            NetworkListEvent<T> listEvent = new NetworkListEvent<T>()
            {
                eventType = NetworkListEvent<T>.EventType.Clear
            };

            HandleAddListEvent(listEvent);
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
            if (NetworkManager.Singleton.IsServer) list.Remove(item);

            NetworkListEvent<T> listEvent = new NetworkListEvent<T>()
            {
                eventType = NetworkListEvent<T>.EventType.Remove,
                value = item
            };

            HandleAddListEvent(listEvent);
            return true;
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
            if (NetworkManager.Singleton.IsServer) list.Insert(index, item);

            NetworkListEvent<T> listEvent = new NetworkListEvent<T>()
            {
                eventType = NetworkListEvent<T>.EventType.Insert,
                index = index,
                value = item
            };

            HandleAddListEvent(listEvent);
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            if (NetworkManager.Singleton.IsServer) list.RemoveAt(index);

            NetworkListEvent<T> listEvent = new NetworkListEvent<T>()
            {
                eventType = NetworkListEvent<T>.EventType.RemoveAt,
                index = index
            };

            HandleAddListEvent(listEvent);
        }


        /// <inheritdoc />
        public T this[int index]
        {
            get => list[index];
            set
            {
                if (NetworkManager.Singleton.IsServer)
                    list[index] = value;

                NetworkListEvent<T> listEvent = new NetworkListEvent<T>()
                {
                    eventType = NetworkListEvent<T>.EventType.Value,
                    index = index,
                    value = value
                };

                HandleAddListEvent(listEvent);
            }
        }

        private void HandleAddListEvent(NetworkListEvent<T> listEvent)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                if (NetworkManager.Singleton.ConnectedClients.Count > 0)
                {
                    dirtyEvents.Add(listEvent);
                }

                OnListChanged?.Invoke(listEvent);
            }
            else
            {
                dirtyEvents.Add(listEvent);
            }
        }
    }

    /// <summary>
    /// Struct containing event information about changes to a NetworkList.
    /// </summary>
    /// <typeparam name="T">The type for the list that the event is about</typeparam>
    public struct NetworkListEvent<T>
    {
        /// <summary>
        /// Enum representing the different operations available for triggering an event.
        /// </summary>
        public enum EventType
        {
            /// <summary>
            /// Add
            /// </summary>
            Add,

            /// <summary>
            /// Insert
            /// </summary>
            Insert,

            /// <summary>
            /// Remove
            /// </summary>
            Remove,

            /// <summary>
            /// Remove at
            /// </summary>
            RemoveAt,

            /// <summary>
            /// Value changed
            /// </summary>
            Value,

            /// <summary>
            /// Clear
            /// </summary>
            Clear
        }

        /// <summary>
        /// Enum representing the operation made to the list.
        /// </summary>
        public EventType eventType;

        /// <summary>
        /// The value changed, added or removed if available.
        /// </summary>
        public T value;

        /// <summary>
        /// the index changed, added or removed if available
        /// </summary>
        public int index;
    }
}