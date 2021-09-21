using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;

namespace Unity.Netcode
{
    /// <summary>
    /// Event based NetworkVariable container for syncing Lists
    /// </summary>
    /// <typeparam name="T">The type for the list</typeparam>
    public class NetworkList<T> : NetworkVariableBase where T : unmanaged, IEquatable<T>
    {
        private NativeList<T> m_List = new NativeList<T>(64, Allocator.Persistent);
        private NativeList<NetworkListEvent<T>> m_DirtyEvents = new NativeList<NetworkListEvent<T>>(64, Allocator.Persistent);

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
        /// <param name="readPerm">The read permission to use for the NetworkList</param>
        /// <param name="values">The initial value to use for the NetworkList</param>
        public NetworkList(NetworkVariableReadPermission readPerm, IEnumerable<T> values) : base(readPerm)
        {
            foreach (var value in values)
            {
                m_List.Add(value);
            }
        }

        /// <summary>
        /// Creates a NetworkList with a custom value and the default settings
        /// </summary>
        /// <param name="values">The initial value to use for the NetworkList</param>
        public NetworkList(IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                m_List.Add(value);
            }
        }

        /// <inheritdoc />
        public override void ResetDirty()
        {
            base.ResetDirty();
            m_DirtyEvents.Clear();
        }

        /// <inheritdoc />
        public override bool IsDirty()
        {
            // we call the base class to allow the SetDirty() mechanism to work
            return base.IsDirty() || m_DirtyEvents.Length > 0;
        }

        /// <inheritdoc />
        public override void WriteDelta(Stream stream)
        {
            using var writer = PooledNetworkWriter.Get(stream);

            if (base.IsDirty())
            {
                writer.WriteUInt16Packed(1);
                writer.WriteByte((byte)NetworkListEvent<T>.EventType.Full);
                WriteField(stream);

                return;
            }

            writer.WriteUInt16Packed((ushort)m_DirtyEvents.Length);
            for (int i = 0; i < m_DirtyEvents.Length; i++)
            {
                writer.WriteByte((byte)m_DirtyEvents[i].Type);
                switch (m_DirtyEvents[i].Type)
                {
                    case NetworkListEvent<T>.EventType.Add:
                        {
                            writer.WriteObjectPacked(m_DirtyEvents[i].Value); //BOX
                        }
                        break;
                    case NetworkListEvent<T>.EventType.Insert:
                        {
                            writer.WriteInt32Packed(m_DirtyEvents[i].Index);
                            writer.WriteObjectPacked(m_DirtyEvents[i].Value); //BOX
                        }
                        break;
                    case NetworkListEvent<T>.EventType.Remove:
                        {
                            writer.WriteObjectPacked(m_DirtyEvents[i].Value); //BOX
                        }
                        break;
                    case NetworkListEvent<T>.EventType.RemoveAt:
                        {
                            writer.WriteInt32Packed(m_DirtyEvents[i].Index);
                        }
                        break;
                    case NetworkListEvent<T>.EventType.Value:
                        {
                            writer.WriteInt32Packed(m_DirtyEvents[i].Index);
                            writer.WriteObjectPacked(m_DirtyEvents[i].Value); //BOX
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

        /// <inheritdoc />
        public override void WriteField(Stream stream)
        {
            using var writer = PooledNetworkWriter.Get(stream);
            writer.WriteUInt16Packed((ushort)m_List.Length);
            for (int i = 0; i < m_List.Length; i++)
            {
                writer.WriteObjectPacked(m_List[i]); //BOX
            }
        }

        /// <inheritdoc />
        public override void ReadField(Stream stream)
        {
            using var reader = PooledNetworkReader.Get(stream);
            m_List.Clear();
            ushort count = reader.ReadUInt16Packed();
            for (int i = 0; i < count; i++)
            {
                m_List.Add((T)reader.ReadObjectPacked(typeof(T))); //BOX
            }
        }

        /// <inheritdoc />
        public override void ReadDelta(Stream stream, bool keepDirtyDelta)
        {
            using var reader = PooledNetworkReader.Get(stream);
            ushort deltaCount = reader.ReadUInt16Packed();
            for (int i = 0; i < deltaCount; i++)
            {
                var eventType = (NetworkListEvent<T>.EventType)reader.ReadByte();
                switch (eventType)
                {
                    case NetworkListEvent<T>.EventType.Add:
                        {
                            m_List.Add((T)reader.ReadObjectPacked(typeof(T))); //BOX

                            if (OnListChanged != null)
                            {
                                OnListChanged(new NetworkListEvent<T>
                                {
                                    Type = eventType,
                                    Index = m_List.Length - 1,
                                    Value = m_List[m_List.Length - 1]
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkListEvent<T>()
                                {
                                    Type = eventType,
                                    Index = m_List.Length - 1,
                                    Value = m_List[m_List.Length - 1]
                                });
                            }
                        }
                        break;
                    case NetworkListEvent<T>.EventType.Insert:
                        {
                            int index = reader.ReadInt32Packed();
                            m_List.InsertRangeWithBeginEnd(index, index + 1);
                            m_List[index] = (T)reader.ReadObjectPacked(typeof(T)); //BOX

                            if (OnListChanged != null)
                            {
                                OnListChanged(new NetworkListEvent<T>
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = m_List[index]
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkListEvent<T>()
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = m_List[index]
                                });
                            }
                        }
                        break;
                    case NetworkListEvent<T>.EventType.Remove:
                        {
                            var value = (T)reader.ReadObjectPacked(typeof(T)); //BOX
                            int index = NativeArrayExtensions.IndexOf(m_List, value);
                            if (index == -1)
                            {
                                break;
                            }

                            m_List.RemoveAt(index);

                            if (OnListChanged != null)
                            {
                                OnListChanged(new NetworkListEvent<T>
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkListEvent<T>()
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = value
                                });
                            }
                        }
                        break;
                    case NetworkListEvent<T>.EventType.RemoveAt:
                        {
                            int index = reader.ReadInt32Packed();
                            T value = m_List[index];
                            m_List.RemoveAt(index);

                            if (OnListChanged != null)
                            {
                                OnListChanged(new NetworkListEvent<T>
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkListEvent<T>()
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = value
                                });
                            }
                        }
                        break;
                    case NetworkListEvent<T>.EventType.Value:
                        {
                            int index = reader.ReadInt32Packed();
                            var value = (T)reader.ReadObjectPacked(typeof(T)); //BOX
                            if (index < m_List.Length)
                            {
                                m_List[index] = value;
                            }

                            if (OnListChanged != null)
                            {
                                OnListChanged(new NetworkListEvent<T>
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkListEvent<T>()
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = value
                                });
                            }
                        }
                        break;
                    case NetworkListEvent<T>.EventType.Clear:
                        {
                            //Read nothing
                            m_List.Clear();

                            if (OnListChanged != null)
                            {
                                OnListChanged(new NetworkListEvent<T>
                                {
                                    Type = eventType,
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkListEvent<T>()
                                {
                                    Type = eventType
                                });
                            }
                        }
                        break;
                    case NetworkListEvent<T>.EventType.Full:
                        {
                            ReadField(stream);
                            ResetDirty();
                        }
                        break;
                }
            }
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return m_List.GetEnumerator();
        }

        /// <inheritdoc />
        public void Add(T item)
        {
            m_List.Add(item);

            var listEvent = new NetworkListEvent<T>()
            {
                Type = NetworkListEvent<T>.EventType.Add,
                Value = item,
                Index = m_List.Length - 1
            };

            HandleAddListEvent(listEvent);
        }

        /// <inheritdoc />
        public void Clear()
        {
            m_List.Clear();

            var listEvent = new NetworkListEvent<T>()
            {
                Type = NetworkListEvent<T>.EventType.Clear
            };

            HandleAddListEvent(listEvent);
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            int index = NativeArrayExtensions.IndexOf(m_List, item);
            return index == -1;
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            int index = NativeArrayExtensions.IndexOf(m_List, item);
            if (index == -1)
            {
                return false;
            }

            m_List.RemoveAt(index);
            var listEvent = new NetworkListEvent<T>()
            {
                Type = NetworkListEvent<T>.EventType.Remove,
                Value = item
            };

            HandleAddListEvent(listEvent);
            return true;
        }

        /// <inheritdoc />
        public int Count => m_List.Length;

        /// <inheritdoc />
        public int IndexOf(T item)
        {
            return m_List.IndexOf(item);
        }

        /// <inheritdoc />
        public void Insert(int index, T item)
        {
            m_List.InsertRangeWithBeginEnd(index, index + 1);
            m_List[index] = item;

            var listEvent = new NetworkListEvent<T>()
            {
                Type = NetworkListEvent<T>.EventType.Insert,
                Index = index,
                Value = item
            };

            HandleAddListEvent(listEvent);
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            m_List.RemoveAt(index);

            var listEvent = new NetworkListEvent<T>()
            {
                Type = NetworkListEvent<T>.EventType.RemoveAt,
                Index = index
            };

            HandleAddListEvent(listEvent);
        }

        /// <inheritdoc />
        public T this[int index]
        {
            get => m_List[index];
            set
            {
                m_List[index] = value;

                var listEvent = new NetworkListEvent<T>()
                {
                    Type = NetworkListEvent<T>.EventType.Value,
                    Index = index,
                    Value = value
                };

                HandleAddListEvent(listEvent);
            }
        }

        private void HandleAddListEvent(NetworkListEvent<T> listEvent)
        {
            m_DirtyEvents.Add(listEvent);
            OnListChanged?.Invoke(listEvent);
        }

        public int LastModifiedTick
        {
            get
            {
                // todo: implement proper network tick for NetworkList
                return NetworkTickSystem.NoTick;
            }
        }

        public override void Dispose()
        {
            m_List.Dispose();
            m_DirtyEvents.Dispose();
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
            Clear,

            /// <summary>
            /// Full list refresh
            /// </summary>
            Full
        }

        /// <summary>
        /// Enum representing the operation made to the list.
        /// </summary>
        public EventType Type;

        /// <summary>
        /// The value changed, added or removed if available.
        /// </summary>
        public T Value;

        /// <summary>
        /// the index changed, added or removed if available
        /// </summary>
        public int Index;
    }
}
