#if !NET35
using System;
using System.IO;
using Unity.Collections;

namespace Unity.Netcode
{
    /// <summary>
    /// Event based NetworkVariable container for syncing Sets
    /// </summary>
    /// <typeparam name="T">The type for the set</typeparam>
    public class NetworkSet<T> : NetworkVariableBase where T : unmanaged, IEquatable<T>
    {
        private const int k_Dummy = 1;
        private NativeHashMap<T, int> m_Set = new NativeHashMap<T, int>(64, Allocator.Persistent);
        private NativeList<NetworkSetEvent<T>> m_DirtyEvents = new NativeList<NetworkSetEvent<T>>(64, Allocator.Persistent);

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
        /// <param name="readPerm">The read permissions to use for the NetworkList</param>
        public NetworkSet(NetworkVariableReadPermission readPerm) : base(readPerm) { }

        /// <inheritdoc />
        public override void ResetDirty()
        {
            base.ResetDirty();
            m_DirtyEvents.Clear();
        }

        /// <inheritdoc />
        public override bool IsDirty()
        {
            return base.IsDirty() || m_DirtyEvents.Length > 0;
        }

        /// <inheritdoc />
        public override void WriteDelta(Stream stream)
        {
            using var writer = PooledNetworkWriter.Get(stream);
            writer.WriteUInt16Packed((ushort)m_DirtyEvents.Length);
            for (int i = 0; i < m_DirtyEvents.Length; i++)
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

        /// <inheritdoc />
        public override void WriteField(Stream stream)
        {
            using var writer = PooledNetworkWriter.Get(stream);
            writer.WriteUInt16Packed((ushort)m_Set.Count());

            foreach (var pair in m_Set)
            {
                writer.WriteObjectPacked(pair.Key); //BOX
            }
        }

        /// <inheritdoc />
        public override void ReadField(Stream stream)
        {
            using var reader = PooledNetworkReader.Get(stream);
            m_Set.Clear();
            ushort count = reader.ReadUInt16Packed();

            for (int i = 0; i < count; i++)
            {
                m_Set.Add((T)reader.ReadObjectPacked(typeof(T)), k_Dummy); //BOX
            }
        }

        /// <inheritdoc />
        public override void ReadDelta(Stream stream, bool keepDirtyDelta)
        {
            using var reader = PooledNetworkReader.Get(stream);
            ushort deltaCount = reader.ReadUInt16Packed();
            for (int i = 0; i < deltaCount; i++)
            {
                var eventType = (NetworkSetEvent<T>.EventType)reader.ReadBits(2);
                switch (eventType)
                {
                    case NetworkSetEvent<T>.EventType.Add:
                        {
                            var value = (T)reader.ReadObjectPacked(typeof(T)); //BOX
                            m_Set.Add(value, k_Dummy);

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
                            var value = (T)reader.ReadObjectPacked(typeof(T)); //BOX
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

        public void Add(T item)
        {
            m_Set.Add(item, k_Dummy);

            var setEvent = new NetworkSetEvent<T>()
            {
                Type = NetworkSetEvent<T>.EventType.Add,
                Value = item
            };
            m_DirtyEvents.Add(setEvent);

            if (OnSetChanged != null)
            {
                OnSetChanged(setEvent);
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            m_Set.Clear();

            var setEvent = new NetworkSetEvent<T>()
            {
                Type = NetworkSetEvent<T>.EventType.Clear
            };
            m_DirtyEvents.Add(setEvent);

            if (OnSetChanged != null)
            {
                OnSetChanged(setEvent);
            }
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            return m_Set.ContainsKey(item);
        }

        /// <inheritdoc />
        public void Remove(T item)
        {
            m_Set.Remove(item);

            var setEvent = new NetworkSetEvent<T>()
            {
                Type = NetworkSetEvent<T>.EventType.Remove,
                Value = item
            };
            m_DirtyEvents.Add(setEvent);

            if (OnSetChanged != null)
            {
                OnSetChanged(setEvent);
            }
        }

        /// <inheritdoc />
        public int Count => m_Set.Count();

        public int LastModifiedTick
        {
            get
            {
                // todo: implement proper network tick for NetworkSet
                return NetworkTickSystem.NoTick;
            }
        }

        public void Dispose()
        {
            m_Set.Dispose();
            m_DirtyEvents.Dispose();
        }
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
