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
        private readonly ISet<T> m_Set = new HashSet<T>();
        private readonly List<NetworkSetEvent<T>> m_DirtyEvents = new List<NetworkSetEvent<T>>();
        private NetworkBehaviour m_NetworkBehaviour;

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
            m_Set = value;
        }

        /// <summary>
        /// Creates a NetworkSet with a custom value and the default settings
        /// </summary>
        /// <param name="value">The initial value to use for the NetworkList</param>
        public NetworkSet(ISet<T> value)
        {
            m_Set = value;
        }

        /// <inheritdoc />
        public void ResetDirty()
        {
            m_DirtyEvents.Clear();
            LastSyncedTime = NetworkManager.Singleton.NetworkTime;
        }

        /// <inheritdoc />
        public bool IsDirty()
        {
            if (m_DirtyEvents.Count == 0) return false;
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
                    return m_NetworkBehaviour.OwnerClientId == clientId;
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
                    return m_NetworkBehaviour.OwnerClientId == clientId;
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
            using (var writer = PooledNetworkWriter.Get(stream))
            {
                writer.WriteUInt16Packed((ushort)m_DirtyEvents.Count);
                for (int i = 0; i < m_DirtyEvents.Count; i++)
                {
                    writer.WriteBits((byte)m_DirtyEvents[i].Type, 2);

                    switch (m_DirtyEvents[i].Type)
                    {
                        case NetworkSetEvent<T>.EventType.Add:
                        {
                            writer.WriteObjectPacked(m_DirtyEvents[i].Value); //BOX
                        }
                            break;
                        case NetworkSetEvent<T>.EventType.Remove:
                        {
                            writer.WriteObjectPacked(m_DirtyEvents[i].Value); //BOX
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
            using (var writer = PooledNetworkWriter.Get(stream))
            {
                writer.WriteUInt16Packed((ushort)m_Set.Count);

                foreach (T value in m_Set)
                {
                    writer.WriteObjectPacked(value); //BOX
                }
            }
        }

        /// <inheritdoc />
        public void ReadField(Stream stream, ushort localTick, ushort remoteTick)
        {
            using (var reader = PooledNetworkReader.Get(stream))
            {
                m_Set.Clear();
                ushort count = reader.ReadUInt16Packed();

                for (int i = 0; i < count; i++)
                {
                    m_Set.Add((T)reader.ReadObjectPacked(typeof(T))); //BOX
                }
            }
        }

        /// <inheritdoc />
        public void ReadDelta(Stream stream, bool keepDirtyDelta, ushort localTick, ushort remoteTick)
        {
            using (var reader = PooledNetworkReader.Get(stream))
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
                            m_Set.Add(value);

                            if (OnSetChanged != null)
                            {
                                OnSetChanged(new NetworkSetEvent<T>
                                {
                                    Type = eventType,
                                    Value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkSetEvent<T>()
                                {
                                    Type = eventType,
                                    Value = value
                                });
                            }
                        }
                            break;
                        case NetworkSetEvent<T>.EventType.Remove:
                        {
                            T value = (T)reader.ReadObjectPacked(typeof(T)); //BOX
                            m_Set.Remove(value);

                            if (OnSetChanged != null)
                            {
                                OnSetChanged(new NetworkSetEvent<T>
                                {
                                    Type = eventType,
                                    Value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkSetEvent<T>()
                                {
                                    Type = eventType,
                                    Value = value
                                });
                            }
                        }
                            break;
                        case NetworkSetEvent<T>.EventType.Clear:
                        {
                            //Read nothing
                            m_Set.Clear();

                            if (OnSetChanged != null)
                            {
                                OnSetChanged(new NetworkSetEvent<T>
                                {
                                    Type = eventType,
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkSetEvent<T>()
                                {
                                    Type = eventType
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
            m_NetworkBehaviour = behaviour;
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return m_Set.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_Set.GetEnumerator();
        }

        /// <inheritdoc />
        void ICollection<T>.Add(T item)
        {
            if (NetworkManager.Singleton.IsServer) m_Set.Add(item);

            NetworkSetEvent<T> setEvent = new NetworkSetEvent<T>()
            {
                Type = NetworkSetEvent<T>.EventType.Add,
                Value = item
            };
            m_DirtyEvents.Add(setEvent);

            if (NetworkManager.Singleton.IsServer && OnSetChanged != null)
                OnSetChanged(setEvent);
        }

        /// <inheritdoc />
        public void ExceptWith(IEnumerable<T> other)
        {
            foreach (T value in other)
            {
                if (m_Set.Contains(value))
                {
                    Remove(value);
                }
            }
        }

        /// <inheritdoc />
        public void IntersectWith(IEnumerable<T> other)
        {
            HashSet<T> otherSet = new HashSet<T>(other);

            foreach (T value in m_Set)
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
            return m_Set.IsProperSubsetOf(other);
        }

        /// <inheritdoc />
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return m_Set.IsProperSupersetOf(other);
        }

        /// <inheritdoc />
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return m_Set.IsSubsetOf(other);
        }

        /// <inheritdoc />
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return m_Set.IsSupersetOf(other);
        }

        /// <inheritdoc />
        public bool Overlaps(IEnumerable<T> other)
        {
            return m_Set.Overlaps(other);
        }

        /// <inheritdoc />
        public bool SetEquals(IEnumerable<T> other)
        {
            return m_Set.SetEquals(other);
        }

        /// <inheritdoc />
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            foreach (T value in other)
            {
                if (m_Set.Contains(value))
                {
                    Remove(value);
                }
                else
                {
                    if (NetworkManager.Singleton.IsServer) m_Set.Add(value);

                    NetworkSetEvent<T> setEvent = new NetworkSetEvent<T>()
                    {
                        Type = NetworkSetEvent<T>.EventType.Add,
                        Value = value
                    };
                    m_DirtyEvents.Add(setEvent);

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
                if (!m_Set.Contains(value))
                {
                    if (NetworkManager.Singleton.IsServer) m_Set.Add(value);

                    NetworkSetEvent<T> setEvent = new NetworkSetEvent<T>()
                    {
                        Type = NetworkSetEvent<T>.EventType.Add,
                        Value = value
                    };
                    m_DirtyEvents.Add(setEvent);

                    if (NetworkManager.Singleton.IsServer && OnSetChanged != null)
                        OnSetChanged(setEvent);
                }
            }
        }

        /// <inheritdoc />
        bool ISet<T>.Add(T item)
        {
            if (NetworkManager.Singleton.IsServer) m_Set.Add(item);

            NetworkSetEvent<T> setEvent = new NetworkSetEvent<T>()
            {
                Type = NetworkSetEvent<T>.EventType.Add,
                Value = item
            };
            m_DirtyEvents.Add(setEvent);

            if (NetworkManager.Singleton.IsServer && OnSetChanged != null)
                OnSetChanged(setEvent);

            return true;
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (NetworkManager.Singleton.IsServer) m_Set.Clear();

            NetworkSetEvent<T> setEvent = new NetworkSetEvent<T>()
            {
                Type = NetworkSetEvent<T>.EventType.Clear
            };
            m_DirtyEvents.Add(setEvent);

            if (NetworkManager.Singleton.IsServer && OnSetChanged != null)
                OnSetChanged(setEvent);
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            return m_Set.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex)
        {
            m_Set.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            if (NetworkManager.Singleton.IsServer) m_Set.Remove(item);

            NetworkSetEvent<T> setEvent = new NetworkSetEvent<T>()
            {
                Type = NetworkSetEvent<T>.EventType.Remove,
                Value = item
            };
            m_DirtyEvents.Add(setEvent);

            if (NetworkManager.Singleton.IsServer && OnSetChanged != null)
                OnSetChanged(setEvent);

            return true;
        }

        /// <inheritdoc />
        public int Count => m_Set.Count;

        /// <inheritdoc />
        public bool IsReadOnly => m_Set.IsReadOnly;
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
        public EventType Type;

        /// <summary>
        /// The value changed, added or removed if available.
        /// </summary>
        public T Value;
    }
}
#endif