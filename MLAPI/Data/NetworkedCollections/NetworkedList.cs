using System.Collections;
using System.Collections.Generic;
using MLAPI.MonoBehaviours.Core;
using MLAPI.NetworkingManagerComponents.Binary;

namespace MLAPI.Data.NetworkedCollections
{
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
        public NetworkedVarSettings Settings = new NetworkedVarSettings();
        
        public void OnSynced()
        {
            dirtyEvents.Clear();
        }

        public bool IsDirty()
        {
            return dirtyEvents.Count > 0;
        }

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

        public void WriteDeltaToWriter(BitWriter writer)
        {
            for (int i = 0; i < dirtyEvents.Count; i++)
            {
                //TODO: Write event
            }
        }

        public void WriteFieldToWriter(BitWriter writer)
        {
            for (int i = 0; i < list.Count; i++)
            {
                //TODO: Write the field
            }
        }

        public void SetFieldFromReader(BitReader reader)
        {
            ushort count = reader.ReadUShort();
            for (int i = 0; i < count; i++)
            {
                //TODO: Read element
            }
        }

        public void SetDeltaFromReader(BitReader reader)
        {
            ushort deltaCount = reader.ReadUShort();
            for (int i = 0; i < deltaCount; i++)
            {
                //TODO: Read the NetworkedListEvent and apply the instruction
            }
        }

        public void SetNetworkedBehaviour(NetworkedBehaviour behaviour)
        {
            networkedBehaviour = behaviour;
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) list).GetEnumerator();
        }

        public void Add(T item)
        {
            list.Add(item);
            dirtyEvents.Add(new NetworkedListEvent<T>()
            {
                eventType = NetworkedListEvent<T>.NetworkedListEventType.Add,
                value = item
            });
        }

        public void Clear()
        {
            list.Clear();
            dirtyEvents.Add(new NetworkedListEvent<T>()
            {
                eventType = NetworkedListEvent<T>.NetworkedListEventType.Clear
            });
        }

        public bool Contains(T item)
        {
            return list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

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

        public int Count => list.Count;

        public bool IsReadOnly => list.IsReadOnly;

        public int IndexOf(T item)
        {
            return list.IndexOf(item);
        }

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

        public void RemoveAt(int index)
        {
            list.RemoveAt(index);
            dirtyEvents.Add(new NetworkedListEvent<T>()
            {
                eventType = NetworkedListEvent<T>.NetworkedListEventType.RemoveAt,
                index = index
            });
        }

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