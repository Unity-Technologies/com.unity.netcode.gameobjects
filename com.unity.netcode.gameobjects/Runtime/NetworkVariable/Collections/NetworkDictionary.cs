using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;

namespace Unity.Netcode
{
    /// <summary>
    /// Event based NetworkVariable container for syncing Dictionaries
    /// </summary>
    /// <typeparam name="TKey">The type for the dictionary keys</typeparam>
    /// <typeparam name="TValue">The type for the dictionary values</typeparam>
    public class NetworkDictionary<TKey, TValue> : NetworkVariableBase where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged
    {
        private NativeHashMap<TKey, TValue> m_Dictionary = new NativeHashMap<TKey, TValue>(64, Allocator.Persistent);
        private readonly NativeList<NetworkDictionaryEvent<TKey, TValue>> m_DirtyEvents = new NativeList<NetworkDictionaryEvent<TKey, TValue>>(64, Allocator.Persistent);

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
        /// <param name="readPerm">The read permission to use for this NetworkDictionary</param>
        public NetworkDictionary(NetworkVariableReadPermission readPerm) : base(readPerm) { }

        /// <inheritdoc />
        public override void ResetDirty()
        {
            base.ResetDirty();
            m_DirtyEvents.Clear();
        }

        /// <inheritdoc />
        public override void ReadDelta(Stream stream, bool keepDirtyDelta)
        {
            using var reader = PooledNetworkReader.Get(stream);
            ushort deltaCount = reader.ReadUInt16Packed();
            for (int i = 0; i < deltaCount; i++)
            {
                var eventType = (NetworkDictionaryEvent<TKey, TValue>.EventType)reader.ReadBits(3);
                switch (eventType)
                {
                    case NetworkDictionaryEvent<TKey, TValue>.EventType.Add:
                        {
                            var key = (TKey)reader.ReadObjectPacked(typeof(TKey));
                            var value = (TValue)reader.ReadObjectPacked(typeof(TValue));
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
                            var key = (TKey)reader.ReadObjectPacked(typeof(TKey));
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
                            var key = (TKey)reader.ReadObjectPacked(typeof(TKey));
                            var value = (TValue)reader.ReadObjectPacked(typeof(TValue));

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

        /// <inheritdoc />
        public override void ReadField(Stream stream)
        {
            using var reader = PooledNetworkReader.Get(stream);
            m_Dictionary.Clear();
            ushort entryCount = reader.ReadUInt16Packed();
            for (int i = 0; i < entryCount; i++)
            {
                var key = (TKey)reader.ReadObjectPacked(typeof(TKey));
                var value = (TValue)reader.ReadObjectPacked(typeof(TValue));
                m_Dictionary.Add(key, value);
            }
        }

        /// <inheritdoc />
        public override void WriteDelta(Stream stream)
        {
            using var writer = PooledNetworkWriter.Get(stream);
            writer.WriteUInt16Packed((ushort)m_DirtyEvents.Length);
            for (int i = 0; i < m_DirtyEvents.Length; i++)
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

        /// <inheritdoc />
        public override void WriteField(Stream stream)
        {
            using var writer = PooledNetworkWriter.Get(stream);
            writer.WriteUInt16Packed((ushort)m_Dictionary.Count());
            foreach (var pair in m_Dictionary)
            {
                writer.WriteObjectPacked(pair.Key);
                writer.WriteObjectPacked(pair.Value);
            }
        }

        /// <inheritdoc />
        public override bool IsDirty()
        {
            return base.IsDirty() || m_DirtyEvents.Length > 0;
        }

        /// <inheritdoc />
        public TValue this[TKey key]
        {
            get => m_Dictionary[key];
            set
            {
                m_Dictionary[key] = value;

                var dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
                {
                    Type = NetworkDictionaryEvent<TKey, TValue>.EventType.Value,
                    Key = key,
                    Value = value
                };

                HandleAddDictionaryEvent(dictionaryEvent);
            }
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, out TValue value)
        {
            return m_Dictionary.TryGetValue(key, out value);
        }

        /// <inheritdoc />
        public NativeArray<TKey> Keys(Allocator alloc)
        {
            return m_Dictionary.GetKeyArray(alloc);
        }

        /// <inheritdoc />
        public NativeArray<TValue> Values(Allocator alloc)
        {
            return m_Dictionary.GetValueArray(alloc);
        }

        /// <inheritdoc />
        public int Count => m_Dictionary.Count();

        /// <inheritdoc />
        public void Add(TKey key, TValue value)
        {
            m_Dictionary.Add(key, value);

            var dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
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
            Add(item.Key, item.Value);
        }

        /// <inheritdoc />
        public void Clear()
        {
            m_Dictionary.Clear();

            var dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
            {
                Type = NetworkDictionaryEvent<TKey, TValue>.EventType.Clear
            };

            HandleAddDictionaryEvent(dictionaryEvent);
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return m_Dictionary.ContainsKey(item.Key) &&
               EqualityComparer<TValue>.Default.Equals(item.Value, m_Dictionary[item.Key]);
        }

        /// <inheritdoc />
        public bool ContainsKey(TKey key)
        {
            return m_Dictionary.ContainsKey(key);
        }

        /// <inheritdoc />
        public void Remove(TKey key)
        {
            m_Dictionary.Remove(key);

            TValue value;
            m_Dictionary.TryGetValue(key, out value);

            var dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
            {
                Type = NetworkDictionaryEvent<TKey, TValue>.EventType.Remove,
                Key = key,
                Value = value
            };

            HandleAddDictionaryEvent(dictionaryEvent);
        }

        private void HandleAddDictionaryEvent(NetworkDictionaryEvent<TKey, TValue> dictionaryEvent)
        {
            m_DirtyEvents.Add(dictionaryEvent);
            OnDictionaryChanged?.Invoke(dictionaryEvent);
        }

        public int LastModifiedTick
        {
            get
            {
                // todo: implement proper network tick for NetworkDictionary
                return NetworkTickSystem.NoTick;
            }
        }
        public void Dispose()
        {
            m_Dictionary.Dispose();
            m_DirtyEvents.Dispose();
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
