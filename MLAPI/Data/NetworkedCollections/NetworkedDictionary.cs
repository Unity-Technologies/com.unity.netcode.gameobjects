using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI.MonoBehaviours.Core;
using MLAPI.NetworkingManagerComponents.Binary;
using MLAPI.NetworkingManagerComponents.Core;

namespace MLAPI.Data.NetworkedCollections
{
    public class NetworkedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INetworkedVar
    {
        internal struct NetworkedDictionaryEvent<TKey, TValue>
        {
            internal enum NetworkedListEventType
            {
                Add,
                Remove,
                RemovePair,
                Clear,
                Value
            }

            internal NetworkedListEventType eventType;
            internal TKey key;
            internal TValue value;
        }

        public NetworkedVarSettings Settings = new NetworkedVarSettings();
        private readonly IDictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
        private NetworkedBehaviour networkedBehaviour;
        private readonly List<NetworkedDictionaryEvent<TKey, TValue>> dirtyEvents = new List<NetworkedDictionaryEvent<TKey, TValue>>();

        public void ResetDirty()
        {
            dirtyEvents.Clear();
        }

        public string GetChannel()
        {
            return Settings.SendChannel;
        }

        public void SetDeltaFromReader(BitReader reader)
        {
            ushort deltaCount = reader.ReadUShort();
            for (int i = 0; i < deltaCount; i++)
            {
                NetworkedDictionaryEvent<TKey, TValue>.NetworkedListEventType eventType = (NetworkedDictionaryEvent<TKey, TValue>.NetworkedListEventType)reader.ReadBits(3);
                switch (eventType)
                {
                    case global::MLAPI.Data.NetworkedCollections.NetworkedDictionary<TKey, TValue>.NetworkedDictionaryEvent<TKey, TValue>.NetworkedListEventType.Add:
                        {
                            //TODO: readKey
                            //TODO: readVal
                        }
                        break;
                    case global::MLAPI.Data.NetworkedCollections.NetworkedDictionary<TKey, TValue>.NetworkedDictionaryEvent<TKey, TValue>.NetworkedListEventType.Remove:
                        {
                            //TODO: readKey
                        }
                        break;
                    case global::MLAPI.Data.NetworkedCollections.NetworkedDictionary<TKey, TValue>.NetworkedDictionaryEvent<TKey, TValue>.NetworkedListEventType.RemovePair:
                        {
                            //TODO: readKey
                            //TODO: readVal
                        }
                        break;
                    case global::MLAPI.Data.NetworkedCollections.NetworkedDictionary<TKey, TValue>.NetworkedDictionaryEvent<TKey, TValue>.NetworkedListEventType.Clear:
                        {
                            //read nothing
                            dictionary.Clear();
                        }
                        break;
                    case global::MLAPI.Data.NetworkedCollections.NetworkedDictionary<TKey, TValue>.NetworkedDictionaryEvent<TKey, TValue>.NetworkedListEventType.Value:
                        {
                            //TODO: readKey
                            //TODO: readVal
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        public void SetFieldFromReader(BitReader reader)
        {
            ushort entryCount = reader.ReadUShort();

            for (int i = 0; i < entryCount; i++)
            {
                //TODO: readKey
                //TODO: readVal
            }
        }

        public void SetNetworkedBehaviour(NetworkedBehaviour behaviour)
        {
            networkedBehaviour = behaviour;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        public void WriteDeltaToWriter(BitWriter writer)
        {
            writer.WriteUShort((ushort)dirtyEvents.Count);
            for (int i = 0; i < dirtyEvents.Count; i++)
            {
                writer.WriteBits((byte)dirtyEvents[i].eventType, 3);
                switch (dirtyEvents[i].eventType)
                {
                    //Fuck me these signatures are proper aids
                    case global::MLAPI.Data.NetworkedCollections.NetworkedDictionary<TKey, TValue>.NetworkedDictionaryEvent<TKey, TValue>.NetworkedListEventType.Add:
                        {
                            //TODO: writeKey
                            //TODO: writeVal
                        }
                        break;
                    case global::MLAPI.Data.NetworkedCollections.NetworkedDictionary<TKey, TValue>.NetworkedDictionaryEvent<TKey, TValue>.NetworkedListEventType.Remove:
                        {
                            //TODO: writeKey
                        }
                        break;
                    case global::MLAPI.Data.NetworkedCollections.NetworkedDictionary<TKey, TValue>.NetworkedDictionaryEvent<TKey, TValue>.NetworkedListEventType.RemovePair:
                        {
                            //TODO: writeKey
                            //TODO: writeVal
                        }
                        break;
                    case global::MLAPI.Data.NetworkedCollections.NetworkedDictionary<TKey, TValue>.NetworkedDictionaryEvent<TKey, TValue>.NetworkedListEventType.Clear:
                        {
                            //write nothing
                        }
                        break;
                    case global::MLAPI.Data.NetworkedCollections.NetworkedDictionary<TKey, TValue>.NetworkedDictionaryEvent<TKey, TValue>.NetworkedListEventType.Value:
                        {
                            //TODO: writeKey
                            //TODO: writeVal
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        public void WriteFieldToWriter(BitWriter writer)
        {
            writer.WriteUShort((ushort)dictionary.Count);
            for (int i = 0; i < dictionary.Count; i++)
            {
                //TODO: writeKey
                //TODO: writeVal
            }
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

        public bool IsDirty()
        {
            return dirtyEvents.Count > 0;
        }



        public TValue this[TKey key]
        {
            get
            {
                return dictionary[key];
            }
            set
            {
                dictionary[key] = value;
                dirtyEvents.Add(new NetworkedDictionaryEvent<TKey, TValue>()
                {
                    eventType = NetworkedDictionaryEvent<TKey, TValue>.NetworkedListEventType.Value,
                    key = key,
                    value = value
                });
            }
        }

        public ICollection<TKey> Keys => dictionary.Keys;

        public ICollection<TValue> Values => dictionary.Values;

        public int Count => dictionary.Count;

        public bool IsReadOnly => dictionary.IsReadOnly;

        public void Add(TKey key, TValue value)
        {
            dictionary.Add(key, value);
            dirtyEvents.Add(new NetworkedDictionaryEvent<TKey, TValue>()
            {
                eventType = NetworkedDictionaryEvent<TKey, TValue>.NetworkedListEventType.Add,
                key = key,
                value = value
            });
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            dictionary.Add(item);
            dirtyEvents.Add(new NetworkedDictionaryEvent<TKey, TValue>()
            {
                eventType = NetworkedDictionaryEvent<TKey, TValue>.NetworkedListEventType.Add,
                key = item.Key,
                value = item.Value
            });
        }

        public void Clear()
        {
            dictionary.Clear();
            dirtyEvents.Add(new NetworkedDictionaryEvent<TKey, TValue>()
            {
                eventType = NetworkedDictionaryEvent<TKey, TValue>.NetworkedListEventType.Clear
            });
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            dictionary.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            bool state = dictionary.Remove(key);
            if (state)
            {
                dirtyEvents.Add(new NetworkedDictionaryEvent<TKey, TValue>()
                {
                    eventType = NetworkedDictionaryEvent<TKey, TValue>.NetworkedListEventType.Remove,
                    key = key
                });
            }
            return state;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            bool state = dictionary.Remove(item);
            if (state)
            {
                dirtyEvents.Add(new NetworkedDictionaryEvent<TKey, TValue>()
                {
                    eventType = NetworkedDictionaryEvent<TKey, TValue>.NetworkedListEventType.RemovePair,
                    key = item.Key,
                    value = item.Value
                });
            }
            return state;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }
    }
}
