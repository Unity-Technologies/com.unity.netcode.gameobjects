using System.Collections;
using System.Collections.Generic;
using System.IO;
using MLAPI.Serialization.Pooled;
using MLAPI.Transports;

namespace MLAPI.NetworkVariable.Collections
{
    /// <summary>
    /// Event based NetworkVariable container for syncing Dictionaries
    /// </summary>
    /// <typeparam name="TKey">The type for the dictionary keys</typeparam>
    /// <typeparam name="TValue">The type for the dictionary values</typeparam>
    public class NetworkDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INetworkVariable
    {
        /// <summary>
        /// Gets the last time the variable was synced
        /// </summary>
        public float LastSyncedTime { get; internal set; }

        /// <summary>
        /// The settings for this container
        /// </summary>
        public readonly NetworkVariableSettings Settings = new NetworkVariableSettings();

        private readonly IDictionary<TKey, TValue> m_Dictionary = new Dictionary<TKey, TValue>();
        private NetworkBehaviour m_NetworkBehaviour;
        private readonly List<NetworkDictionaryEvent<TKey, TValue>> m_DirtyEvents = new List<NetworkDictionaryEvent<TKey, TValue>>();

        /// <summary>
        /// Delegate type for dictionary changed event
        /// </summary>
        /// <param name="changeEvent">Struct containing information about the change event</param>
        public delegate void OnDictionaryChangedDelegate(NetworkDictionaryEvent<TKey, TValue> changeEvent);

        /// <summary>
        /// The callback to be invoked when the dictionary gets changed
        /// </summary>
        public event OnDictionaryChangedDelegate OnDictionaryChanged;


        /// <summary>
        /// Creates a NetworkDictionary with the default value and settings
        /// </summary>
        public NetworkDictionary() { }

        /// <summary>
        /// Creates a NetworkDictionary with the default value and custom settings
        /// </summary>
        /// <param name="settings">The settings to use for the NetworkDictionary</param>
        public NetworkDictionary(NetworkVariableSettings settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// Creates a NetworkDictionary with a custom value and custom settings
        /// </summary>
        /// <param name="settings">The settings to use for the NetworkDictionary</param>
        /// <param name="value">The initial value to use for the NetworkDictionary</param>
        public NetworkDictionary(NetworkVariableSettings settings, IDictionary<TKey, TValue> value)
        {
            Settings = settings;
            m_Dictionary = value;
        }

        /// <summary>
        /// Creates a NetworkDictionary with a custom value and the default settings
        /// </summary>
        /// <param name="value">The initial value to use for the NetworkDictionary</param>
        public NetworkDictionary(IDictionary<TKey, TValue> value)
        {
            m_Dictionary = value;
        }

        /// <inheritdoc />
        public void ResetDirty()
        {
            m_DirtyEvents.Clear();
            LastSyncedTime = NetworkManager.Singleton.NetworkTime;
        }

        /// <inheritdoc />
        public NetworkChannel GetChannel()
        {
            return Settings.SendNetworkChannel;
        }

        /// <inheritdoc />
        public void ReadDelta(Stream stream, bool keepDirtyDelta, ushort localTick, ushort remoteTick)
        {
            using (var reader = PooledNetworkReader.Get(stream))
            {
                ushort deltaCount = reader.ReadUInt16Packed();
                for (int i = 0; i < deltaCount; i++)
                {
                    NetworkDictionaryEvent<TKey, TValue>.EventType eventType = (NetworkDictionaryEvent<TKey, TValue>.EventType)reader.ReadBits(3);
                    switch (eventType)
                    {
                        case NetworkDictionaryEvent<TKey, TValue>.EventType.Add:
                        {
                            TKey key = (TKey)reader.ReadObjectPacked(typeof(TKey));
                            TValue value = (TValue)reader.ReadObjectPacked(typeof(TValue));
                            m_Dictionary.Add(key, value);

                            if (OnDictionaryChanged != null)
                            {
                                OnDictionaryChanged(new NetworkDictionaryEvent<TKey, TValue>
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkDictionaryEvent<TKey, TValue>()
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                            }
                        }
                            break;
                        case NetworkDictionaryEvent<TKey, TValue>.EventType.Remove:
                        {
                            TKey key = (TKey)reader.ReadObjectPacked(typeof(TKey));
                            TValue value;
                            m_Dictionary.TryGetValue(key, out value);
                            m_Dictionary.Remove(key);

                            if (OnDictionaryChanged != null)
                            {
                                OnDictionaryChanged(new NetworkDictionaryEvent<TKey, TValue>
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkDictionaryEvent<TKey, TValue>()
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                            }
                        }
                            break;
                        case NetworkDictionaryEvent<TKey, TValue>.EventType.RemovePair:
                        {
                            TKey key = (TKey)reader.ReadObjectPacked(typeof(TKey));
                            TValue value = (TValue)reader.ReadObjectPacked(typeof(TValue));
                            m_Dictionary.Remove(new KeyValuePair<TKey, TValue>(key, value));

                            if (OnDictionaryChanged != null)
                            {
                                OnDictionaryChanged(new NetworkDictionaryEvent<TKey, TValue>
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkDictionaryEvent<TKey, TValue>()
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                            }
                        }
                            break;
                        case NetworkDictionaryEvent<TKey, TValue>.EventType.Clear:
                        {
                            //read nothing
                            m_Dictionary.Clear();

                            if (OnDictionaryChanged != null)
                            {
                                OnDictionaryChanged(new NetworkDictionaryEvent<TKey, TValue>
                                {
                                    Type = eventType
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkDictionaryEvent<TKey, TValue>
                                {
                                    Type = eventType
                                });
                            }
                        }
                            break;
                        case NetworkDictionaryEvent<TKey, TValue>.EventType.Value:
                        {
                            TKey key = (TKey)reader.ReadObjectPacked(typeof(TKey));
                            TValue value = (TValue)reader.ReadObjectPacked(typeof(TValue));

                            m_Dictionary[key] = value;

                            if (OnDictionaryChanged != null)
                            {
                                OnDictionaryChanged(new NetworkDictionaryEvent<TKey, TValue>
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkDictionaryEvent<TKey, TValue>()
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                            }
                        }
                            break;
                    }
                }
            }
        }

        /// <inheritdoc />
        public void ReadField(Stream stream, ushort localTick, ushort remoteTick)
        {
            using (var reader = PooledNetworkReader.Get(stream))
            {
                m_Dictionary.Clear();
                ushort entryCount = reader.ReadUInt16Packed();
                for (int i = 0; i < entryCount; i++)
                {
                    TKey key = (TKey)reader.ReadObjectPacked(typeof(TKey));
                    TValue value = (TValue)reader.ReadObjectPacked(typeof(TValue));
                    m_Dictionary.Add(key, value);
                }
            }
        }

        /// <inheritdoc />
        public void SetNetworkBehaviour(NetworkBehaviour behaviour)
        {
            m_NetworkBehaviour = behaviour;
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, out TValue value)
        {
            return m_Dictionary.TryGetValue(key, out value);
        }

        /// <inheritdoc />
        public void WriteDelta(Stream stream)
        {
            using (var writer = PooledNetworkWriter.Get(stream))
            {
                writer.WriteUInt16Packed((ushort)m_DirtyEvents.Count);
                for (int i = 0; i < m_DirtyEvents.Count; i++)
                {
                    writer.WriteBits((byte)m_DirtyEvents[i].Type, 3);
                    switch (m_DirtyEvents[i].Type)
                    {
                        case NetworkDictionaryEvent<TKey, TValue>.EventType.Add:
                        {
                            writer.WriteObjectPacked(m_DirtyEvents[i].Key);
                            writer.WriteObjectPacked(m_DirtyEvents[i].Value);
                        }
                            break;
                        case NetworkDictionaryEvent<TKey, TValue>.EventType.Remove:
                        {
                            writer.WriteObjectPacked(m_DirtyEvents[i].Key);
                        }
                            break;
                        case NetworkDictionaryEvent<TKey, TValue>.EventType.RemovePair:
                        {
                            writer.WriteObjectPacked(m_DirtyEvents[i].Key);
                            writer.WriteObjectPacked(m_DirtyEvents[i].Value);
                        }
                            break;
                        case NetworkDictionaryEvent<TKey, TValue>.EventType.Clear:
                        {
                            //write nothing
                        }
                            break;
                        case NetworkDictionaryEvent<TKey, TValue>.EventType.Value:
                        {
                            writer.WriteObjectPacked(m_DirtyEvents[i].Key);
                            writer.WriteObjectPacked(m_DirtyEvents[i].Value);
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
                writer.WriteUInt16Packed((ushort)m_Dictionary.Count);
                foreach (KeyValuePair<TKey, TValue> pair in m_Dictionary)
                {
                    writer.WriteObjectPacked(pair.Key);
                    writer.WriteObjectPacked(pair.Value);
                }
            }
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
        public bool IsDirty()
        {
            if (m_DirtyEvents.Count == 0) return false;
            if (Settings.SendTickrate == 0) return true;
            if (Settings.SendTickrate < 0) return false;
            if (NetworkManager.Singleton.NetworkTime - LastSyncedTime >= (1f / Settings.SendTickrate)) return true;
            return false;
        }


        /// <inheritdoc />
        public TValue this[TKey key]
        {
            get => m_Dictionary[key];
            set
            {
                if (NetworkManager.Singleton.IsServer) m_Dictionary[key] = value;

                NetworkDictionaryEvent<TKey, TValue> dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
                {
                    Type = NetworkDictionaryEvent<TKey, TValue>.EventType.Value,
                    Key = key,
                    Value = value
                };

                HandleAddDictionaryEvent(dictionaryEvent);
            }
        }

        /// <inheritdoc />
        public ICollection<TKey> Keys => m_Dictionary.Keys;

        /// <inheritdoc />
        public ICollection<TValue> Values => m_Dictionary.Values;

        /// <inheritdoc />
        public int Count => m_Dictionary.Count;

        /// <inheritdoc />
        public bool IsReadOnly => m_Dictionary.IsReadOnly;

        /// <inheritdoc />
        public void Add(TKey key, TValue value)
        {
            if (NetworkManager.Singleton.IsServer) m_Dictionary.Add(key, value);

            NetworkDictionaryEvent<TKey, TValue> dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
            {
                Type = NetworkDictionaryEvent<TKey, TValue>.EventType.Add,
                Key = key,
                Value = value
            };

            HandleAddDictionaryEvent(dictionaryEvent);
        }

        /// <inheritdoc />
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            if (NetworkManager.Singleton.IsServer) m_Dictionary.Add(item);

            NetworkDictionaryEvent<TKey, TValue> dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
            {
                Type = NetworkDictionaryEvent<TKey, TValue>.EventType.Add,
                Key = item.Key,
                Value = item.Value
            };

            HandleAddDictionaryEvent(dictionaryEvent);
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (NetworkManager.Singleton.IsServer) m_Dictionary.Clear();

            NetworkDictionaryEvent<TKey, TValue> dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
            {
                Type = NetworkDictionaryEvent<TKey, TValue>.EventType.Clear
            };

            HandleAddDictionaryEvent(dictionaryEvent);
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return m_Dictionary.Contains(item);
        }

        /// <inheritdoc />
        public bool ContainsKey(TKey key)
        {
            return m_Dictionary.ContainsKey(key);
        }

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            m_Dictionary.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return m_Dictionary.GetEnumerator();
        }

        /// <inheritdoc />
        public bool Remove(TKey key)
        {
            if (NetworkManager.Singleton.IsServer)
                m_Dictionary.Remove(key);

            TValue value;
            m_Dictionary.TryGetValue(key, out value);

            NetworkDictionaryEvent<TKey, TValue> dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
            {
                Type = NetworkDictionaryEvent<TKey, TValue>.EventType.Remove,
                Key = key,
                Value = value
            };

            HandleAddDictionaryEvent(dictionaryEvent);

            return true;
        }


        /// <inheritdoc />
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (NetworkManager.Singleton.IsServer) m_Dictionary.Remove(item);

            NetworkDictionaryEvent<TKey, TValue> dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
            {
                Type = NetworkDictionaryEvent<TKey, TValue>.EventType.RemovePair,
                Key = item.Key,
                Value = item.Value
            };

            HandleAddDictionaryEvent(dictionaryEvent);
            return true;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_Dictionary.GetEnumerator();
        }

        private void HandleAddDictionaryEvent(NetworkDictionaryEvent<TKey, TValue> dictionaryEvent)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                if (NetworkManager.Singleton.ConnectedClients.Count > 0)
                {
                    m_DirtyEvents.Add(dictionaryEvent);
                }

                OnDictionaryChanged?.Invoke(dictionaryEvent);
            }
            else
            {
                m_DirtyEvents.Add(dictionaryEvent);
            }
        }
    }

    /// <summary>
    /// Struct containing event information about changes to a NetworkDictionary.
    /// </summary>
    /// <typeparam name="TKey">The type for the dictionary key that the event is about</typeparam>
    /// <typeparam name="TValue">The type for the dictionary value that the event is about</typeparam>
    public struct NetworkDictionaryEvent<TKey, TValue>
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
            /// Remove pair
            /// </summary>
            RemovePair,

            /// <summary>
            /// Clear
            /// </summary>
            Clear,

            /// <summary>
            /// Value changed
            /// </summary>
            Value
        }

        /// <summary>
        /// Enum representing the operation made to the dictionary.
        /// </summary>
        public EventType Type;

        /// <summary>
        /// the key changed, added or removed if available.
        /// </summary>
        public TKey Key;

        /// <summary>
        /// The value changed, added or removed if available.
        /// </summary>
        public TValue Value;
    }
}