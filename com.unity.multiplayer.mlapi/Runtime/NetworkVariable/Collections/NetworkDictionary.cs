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

        private readonly IDictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
        private NetworkBehaviour networkBehaviour;
        private readonly List<NetworkDictionaryEvent<TKey, TValue>> dirtyEvents = new List<NetworkDictionaryEvent<TKey, TValue>>();

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
            dictionary = value;
        }

        /// <summary>
        /// Creates a NetworkDictionary with a custom value and the default settings
        /// </summary>
        /// <param name="value">The initial value to use for the NetworkDictionary</param>
        public NetworkDictionary(IDictionary<TKey, TValue> value)
        {
            dictionary = value;
        }

        /// <inheritdoc />
        public void ResetDirty()
        {
            dirtyEvents.Clear();
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
            using (PooledNetworkReader reader = PooledNetworkReader.Get(stream))
            {
                ushort deltaCount = reader.ReadUInt16Packed();
                for (int i = 0; i < deltaCount; i++)
                {
                    NetworkDictionaryEvent<TKey, TValue>.NetworkListEventType eventType = (NetworkDictionaryEvent<TKey, TValue>.NetworkListEventType)reader.ReadBits(3);
                    switch (eventType)
                    {
                        case NetworkDictionaryEvent<TKey, TValue>.NetworkListEventType.Add:
                        {
                            TKey key = (TKey)reader.ReadObjectPacked(typeof(TKey));
                            TValue value = (TValue)reader.ReadObjectPacked(typeof(TValue));
                            dictionary.Add(key, value);

                            if (OnDictionaryChanged != null)
                            {
                                OnDictionaryChanged(new NetworkDictionaryEvent<TKey, TValue>
                                {
                                    eventType = eventType,
                                    key = key,
                                    value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                dirtyEvents.Add(new NetworkDictionaryEvent<TKey, TValue>()
                                {
                                    eventType = eventType,
                                    key = key,
                                    value = value
                                });
                            }
                        }
                            break;
                        case NetworkDictionaryEvent<TKey, TValue>.NetworkListEventType.Remove:
                        {
                            TKey key = (TKey)reader.ReadObjectPacked(typeof(TKey));
                            TValue value;
                            dictionary.TryGetValue(key, out value);
                            dictionary.Remove(key);

                            if (OnDictionaryChanged != null)
                            {
                                OnDictionaryChanged(new NetworkDictionaryEvent<TKey, TValue>
                                {
                                    eventType = eventType,
                                    key = key,
                                    value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                dirtyEvents.Add(new NetworkDictionaryEvent<TKey, TValue>()
                                {
                                    eventType = eventType,
                                    key = key,
                                    value = value
                                });
                            }
                        }
                            break;
                        case NetworkDictionaryEvent<TKey, TValue>.NetworkListEventType.RemovePair:
                        {
                            TKey key = (TKey)reader.ReadObjectPacked(typeof(TKey));
                            TValue value = (TValue)reader.ReadObjectPacked(typeof(TValue));
                            dictionary.Remove(new KeyValuePair<TKey, TValue>(key, value));

                            if (OnDictionaryChanged != null)
                            {
                                OnDictionaryChanged(new NetworkDictionaryEvent<TKey, TValue>
                                {
                                    eventType = eventType,
                                    key = key,
                                    value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                dirtyEvents.Add(new NetworkDictionaryEvent<TKey, TValue>()
                                {
                                    eventType = eventType,
                                    key = key,
                                    value = value
                                });
                            }
                        }
                            break;
                        case NetworkDictionaryEvent<TKey, TValue>.NetworkListEventType.Clear:
                        {
                            //read nothing
                            dictionary.Clear();

                            if (OnDictionaryChanged != null)
                            {
                                OnDictionaryChanged(new NetworkDictionaryEvent<TKey, TValue>
                                {
                                    eventType = eventType
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                dirtyEvents.Add(new NetworkDictionaryEvent<TKey, TValue>
                                {
                                    eventType = eventType
                                });
                            }
                        }
                            break;
                        case NetworkDictionaryEvent<TKey, TValue>.NetworkListEventType.Value:
                        {
                            TKey key = (TKey)reader.ReadObjectPacked(typeof(TKey));
                            TValue value = (TValue)reader.ReadObjectPacked(typeof(TValue));

                            dictionary[key] = value;

                            if (OnDictionaryChanged != null)
                            {
                                OnDictionaryChanged(new NetworkDictionaryEvent<TKey, TValue>
                                {
                                    eventType = eventType,
                                    key = key,
                                    value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                dirtyEvents.Add(new NetworkDictionaryEvent<TKey, TValue>()
                                {
                                    eventType = eventType,
                                    key = key,
                                    value = value
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
            using (PooledNetworkReader reader = PooledNetworkReader.Get(stream))
            {
                dictionary.Clear();
                ushort entryCount = reader.ReadUInt16Packed();
                for (int i = 0; i < entryCount; i++)
                {
                    TKey key = (TKey)reader.ReadObjectPacked(typeof(TKey));
                    TValue value = (TValue)reader.ReadObjectPacked(typeof(TValue));
                    dictionary.Add(key, value);
                }
            }
        }

        /// <inheritdoc />
        public void SetNetworkBehaviour(NetworkBehaviour behaviour)
        {
            networkBehaviour = behaviour;
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
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
                        case NetworkDictionaryEvent<TKey, TValue>.NetworkListEventType.Add:
                        {
                            writer.WriteObjectPacked(dirtyEvents[i].key);
                            writer.WriteObjectPacked(dirtyEvents[i].value);
                        }
                            break;
                        case NetworkDictionaryEvent<TKey, TValue>.NetworkListEventType.Remove:
                        {
                            writer.WriteObjectPacked(dirtyEvents[i].key);
                        }
                            break;
                        case NetworkDictionaryEvent<TKey, TValue>.NetworkListEventType.RemovePair:
                        {
                            writer.WriteObjectPacked(dirtyEvents[i].key);
                            writer.WriteObjectPacked(dirtyEvents[i].value);
                        }
                            break;
                        case NetworkDictionaryEvent<TKey, TValue>.NetworkListEventType.Clear:
                        {
                            //write nothing
                        }
                            break;
                        case NetworkDictionaryEvent<TKey, TValue>.NetworkListEventType.Value:
                        {
                            writer.WriteObjectPacked(dirtyEvents[i].key);
                            writer.WriteObjectPacked(dirtyEvents[i].value);
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
                writer.WriteUInt16Packed((ushort)dictionary.Count);
                foreach (KeyValuePair<TKey, TValue> pair in dictionary)
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
        public bool IsDirty()
        {
            if (dirtyEvents.Count == 0) return false;
            if (Settings.SendTickrate == 0) return true;
            if (Settings.SendTickrate < 0) return false;
            if (NetworkManager.Singleton.NetworkTime - LastSyncedTime >= (1f / Settings.SendTickrate)) return true;
            return false;
        }


        /// <inheritdoc />
        public TValue this[TKey key]
        {
            get => dictionary[key];
            set
            {
                if (NetworkManager.Singleton.IsServer) dictionary[key] = value;

                NetworkDictionaryEvent<TKey, TValue> dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
                {
                    eventType = NetworkDictionaryEvent<TKey, TValue>.NetworkListEventType.Value,
                    key = key,
                    value = value
                };

                HandleAddDictionaryEvent(dictionaryEvent);
            }
        }

        /// <inheritdoc />
        public ICollection<TKey> Keys => dictionary.Keys;

        /// <inheritdoc />
        public ICollection<TValue> Values => dictionary.Values;

        /// <inheritdoc />
        public int Count => dictionary.Count;

        /// <inheritdoc />
        public bool IsReadOnly => dictionary.IsReadOnly;

        /// <inheritdoc />
        public void Add(TKey key, TValue value)
        {
            if (NetworkManager.Singleton.IsServer) dictionary.Add(key, value);

            NetworkDictionaryEvent<TKey, TValue> dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
            {
                eventType = NetworkDictionaryEvent<TKey, TValue>.NetworkListEventType.Add,
                key = key,
                value = value
            };

            HandleAddDictionaryEvent(dictionaryEvent);
        }

        /// <inheritdoc />
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            if (NetworkManager.Singleton.IsServer) dictionary.Add(item);

            NetworkDictionaryEvent<TKey, TValue> dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
            {
                eventType = NetworkDictionaryEvent<TKey, TValue>.NetworkListEventType.Add,
                key = item.Key,
                value = item.Value
            };

            HandleAddDictionaryEvent(dictionaryEvent);
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (NetworkManager.Singleton.IsServer) dictionary.Clear();

            NetworkDictionaryEvent<TKey, TValue> dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
            {
                eventType = NetworkDictionaryEvent<TKey, TValue>.NetworkListEventType.Clear
            };

            HandleAddDictionaryEvent(dictionaryEvent);
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.Contains(item);
        }

        /// <inheritdoc />
        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            dictionary.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        /// <inheritdoc />
        public bool Remove(TKey key)
        {
            if (NetworkManager.Singleton.IsServer)
                dictionary.Remove(key);

            TValue value;
            dictionary.TryGetValue(key, out value);

            NetworkDictionaryEvent<TKey, TValue> dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
            {
                eventType = NetworkDictionaryEvent<TKey, TValue>.NetworkListEventType.Remove,
                key = key,
                value = value
            };

            HandleAddDictionaryEvent(dictionaryEvent);

            return true;
        }


        /// <inheritdoc />
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (NetworkManager.Singleton.IsServer) dictionary.Remove(item);

            NetworkDictionaryEvent<TKey, TValue> dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
            {
                eventType = NetworkDictionaryEvent<TKey, TValue>.NetworkListEventType.RemovePair,
                key = item.Key,
                value = item.Value
            };

            HandleAddDictionaryEvent(dictionaryEvent);
            return true;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        private void HandleAddDictionaryEvent(NetworkDictionaryEvent<TKey, TValue> dictionaryEvent)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                if (NetworkManager.Singleton.ConnectedClients.Count > 0)
                {
                    dirtyEvents.Add(dictionaryEvent);
                }

                OnDictionaryChanged?.Invoke(dictionaryEvent);
            }
            else
            {
                dirtyEvents.Add(dictionaryEvent);
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
        public enum NetworkListEventType
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
        public NetworkListEventType eventType;

        /// <summary>
        /// the key changed, added or removed if available.
        /// </summary>
        public TKey key;

        /// <summary>
        /// The value changed, added or removed if available.
        /// </summary>
        public TValue value;
    }
}