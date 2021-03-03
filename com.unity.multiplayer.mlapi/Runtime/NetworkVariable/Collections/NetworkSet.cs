#if !NET35
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MLAPI.Serialization.Pooled;
using MLAPI.Transports;

namespace MLAPI.NetworkVariable.Collections
{
    /// <summary>
    /// Event based NetworkVariable container for syncing Sets
    /// </summary>
    /// <typeparam name="T">The type for the set</typeparam>
    public class NetworkSet<T> : ISet<T>, INetworkVariable
    {
        private readonly ISet<T> set = new HashSet<T>();
        private readonly List<NetworkSetEvent<T>> dirtyEvents = new List<NetworkSetEvent<T>>();
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
        /// Delegate type for set changed event
        /// </summary>
        /// <param name="changeEvent">Struct containing information about the change event</param>
        public delegate void OnSetChangedDelegate(NetworkSetEvent<T> changeEvent);

        /// <summary>
        /// The callback to be invoked when the set gets changed
        /// </summary>
        public event OnSetChangedDelegate OnSetChanged;

        /// <summary>
        /// Creates a NetworkSet with the default value and settings
        /// </summary>
        public NetworkSet() { }

        /// <summary>
        /// Creates a NetworkSet with the default value and custom settings
        /// </summary>
        /// <param name="settings">The settings to use for the NetworkList</param>
        public NetworkSet(NetworkVariableSettings settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// Creates a NetworkSet with a custom value and custom settings
        /// </summary>
        /// <param name="settings">The settings to use for the NetworkSet</param>
        /// <param name="value">The initial value to use for the NetworkSet</param>
        public NetworkSet(NetworkVariableSettings settings, ISet<T> value)
        {
            Settings = settings;
            set = value;
        }

        /// <summary>
        /// Creates a NetworkSet with a custom value and the default settings
        /// </summary>
        /// <param name="value">The initial value to use for the NetworkList</param>
        public NetworkSet(ISet<T> value)
        {
            set = value;
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
                    writer.WriteBits((byte)dirtyEvents[i].eventType, 2);

                    switch (dirtyEvents[i].eventType)
                    {
                        case NetworkSetEvent<T>.EventType.Add:
                        {
                            writer.WriteObjectPacked(dirtyEvents[i].value); //BOX
                        }
                            break;
                        case NetworkSetEvent<T>.EventType.Remove:
                        {
                            writer.WriteObjectPacked(dirtyEvents[i].value); //BOX
                        }
                            break;
                        case NetworkSetEvent<T>.EventType.Clear:
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
                writer.WriteUInt16Packed((ushort)set.Count);

                foreach (T value in set)
                {
                    writer.WriteObjectPacked(value); //BOX
                }
            }
        }

        /// <inheritdoc />
        public void ReadField(Stream stream, ushort localTick, ushort remoteTick)
        {
            using (PooledNetworkReader reader = PooledNetworkReader.Get(stream))
            {
                set.Clear();
                ushort count = reader.ReadUInt16Packed();

                for (int i = 0; i < count; i++)
                {
                    set.Add((T)reader.ReadObjectPacked(typeof(T))); //BOX
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
                    NetworkSetEvent<T>.EventType eventType = (NetworkSetEvent<T>.EventType)reader.ReadBits(2);
                    switch (eventType)
                    {
                        case NetworkSetEvent<T>.EventType.Add:
                        {
                            T value = (T)reader.ReadObjectPacked(typeof(T)); //BOX
                            set.Add(value);

                            if (OnSetChanged != null)
                            {
                                OnSetChanged(new NetworkSetEvent<T>
                                {
                                    eventType = eventType,
                                    value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                dirtyEvents.Add(new NetworkSetEvent<T>()
                                {
                                    eventType = eventType,
                                    value = value
                                });
                            }
                        }
                            break;
                        case NetworkSetEvent<T>.EventType.Remove:
                        {
                            T value = (T)reader.ReadObjectPacked(typeof(T)); //BOX
                            set.Remove(value);

                            if (OnSetChanged != null)
                            {
                                OnSetChanged(new NetworkSetEvent<T>
                                {
                                    eventType = eventType,
                                    value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                dirtyEvents.Add(new NetworkSetEvent<T>()
                                {
                                    eventType = eventType,
                                    value = value
                                });
                            }
                        }
                            break;
                        case NetworkSetEvent<T>.EventType.Clear:
                        {
                            //Read nothing
                            set.Clear();

                            if (OnSetChanged != null)
                            {
                                OnSetChanged(new NetworkSetEvent<T>
                                {
                                    eventType = eventType,
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                dirtyEvents.Add(new NetworkSetEvent<T>()
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
            return set.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return set.GetEnumerator();
        }

        /// <inheritdoc />
        void ICollection<T>.Add(T item)
        {
            if (NetworkManager.Singleton.IsServer) set.Add(item);

            NetworkSetEvent<T> setEvent = new NetworkSetEvent<T>()
            {
                eventType = NetworkSetEvent<T>.EventType.Add,
                value = item
            };
            dirtyEvents.Add(setEvent);

            if (NetworkManager.Singleton.IsServer && OnSetChanged != null)
                OnSetChanged(setEvent);
        }

        /// <inheritdoc />
        public void ExceptWith(IEnumerable<T> other)
        {
            foreach (T value in other)
            {
                if (set.Contains(value))
                {
                    Remove(value);
                }
            }
        }

        /// <inheritdoc />
        public void IntersectWith(IEnumerable<T> other)
        {
            HashSet<T> otherSet = new HashSet<T>(other);

            foreach (T value in set)
            {
                if (!otherSet.Contains(value))
                {
                    Remove(value);
                }
            }
        }

        /// <inheritdoc />
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return set.IsProperSubsetOf(other);
        }

        /// <inheritdoc />
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return set.IsProperSupersetOf(other);
        }

        /// <inheritdoc />
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return set.IsSubsetOf(other);
        }

        /// <inheritdoc />
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return set.IsSupersetOf(other);
        }

        /// <inheritdoc />
        public bool Overlaps(IEnumerable<T> other)
        {
            return set.Overlaps(other);
        }

        /// <inheritdoc />
        public bool SetEquals(IEnumerable<T> other)
        {
            return set.SetEquals(other);
        }

        /// <inheritdoc />
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            foreach (T value in other)
            {
                if (set.Contains(value))
                {
                    Remove(value);
                }
                else
                {
                    if (NetworkManager.Singleton.IsServer) set.Add(value);

                    NetworkSetEvent<T> setEvent = new NetworkSetEvent<T>()
                    {
                        eventType = NetworkSetEvent<T>.EventType.Add,
                        value = value
                    };
                    dirtyEvents.Add(setEvent);

                    if (NetworkManager.Singleton.IsServer && OnSetChanged != null)
                        OnSetChanged(setEvent);
                }
            }
        }

        /// <inheritdoc />
        public void UnionWith(IEnumerable<T> other)
        {
            foreach (T value in other)
            {
                if (!set.Contains(value))
                {
                    if (NetworkManager.Singleton.IsServer) set.Add(value);

                    NetworkSetEvent<T> setEvent = new NetworkSetEvent<T>()
                    {
                        eventType = NetworkSetEvent<T>.EventType.Add,
                        value = value
                    };
                    dirtyEvents.Add(setEvent);

                    if (NetworkManager.Singleton.IsServer && OnSetChanged != null)
                        OnSetChanged(setEvent);
                }
            }
        }

        /// <inheritdoc />
        bool ISet<T>.Add(T item)
        {
            if (NetworkManager.Singleton.IsServer) set.Add(item);

            NetworkSetEvent<T> setEvent = new NetworkSetEvent<T>()
            {
                eventType = NetworkSetEvent<T>.EventType.Add,
                value = item
            };
            dirtyEvents.Add(setEvent);

            if (NetworkManager.Singleton.IsServer && OnSetChanged != null)
                OnSetChanged(setEvent);

            return true;
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (NetworkManager.Singleton.IsServer) set.Clear();

            NetworkSetEvent<T> setEvent = new NetworkSetEvent<T>()
            {
                eventType = NetworkSetEvent<T>.EventType.Clear
            };
            dirtyEvents.Add(setEvent);

            if (NetworkManager.Singleton.IsServer && OnSetChanged != null)
                OnSetChanged(setEvent);
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            return set.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex)
        {
            set.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            if (NetworkManager.Singleton.IsServer) set.Remove(item);

            NetworkSetEvent<T> setEvent = new NetworkSetEvent<T>()
            {
                eventType = NetworkSetEvent<T>.EventType.Remove,
                value = item
            };
            dirtyEvents.Add(setEvent);

            if (NetworkManager.Singleton.IsServer && OnSetChanged != null)
                OnSetChanged(setEvent);

            return true;
        }

        /// <inheritdoc />
        public int Count => set.Count;

        /// <inheritdoc />
        public bool IsReadOnly => set.IsReadOnly;
    }

    /// <summary>
    /// Struct containing event information about changes to a NetworkSet.
    /// </summary>
    /// <typeparam name="T">The type for the set that the event is about</typeparam>
    public struct NetworkSetEvent<T>
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
            /// Remove
            /// </summary>
            Remove,

            /// <summary>
            /// Clear
            /// </summary>
            Clear
        }

        /// <summary>
        /// Enum representing the operation made to the set.
        /// </summary>
        public EventType eventType;

        /// <summary>
        /// The value changed, added or removed if available.
        /// </summary>
        public T value;
    }
}
#endif