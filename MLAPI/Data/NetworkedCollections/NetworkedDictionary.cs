using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI.MonoBehaviours.Core;
using MLAPI.NetworkingManagerComponents.Binary;
using MLAPI.NetworkingManagerComponents.Core;

namespace MLAPI.Data.NetworkedCollections
{
    /// <summary>
    /// Event based networkedVar container for syncing Lists
    /// </summary>
    /// <typeparam name="TKey">The type for the dictionary keys</typeparam>
    /// <typeparam name="TValue">The type for the dctionary values</typeparam>
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

        /// <summary>
        /// Gets the last time the variable was synced
        /// </summary>
        public float LastSyncedTime { get; internal set; }   
        /// <summary>
        /// The settings for this container
        /// </summary>
        public readonly NetworkedVarSettings Settings = new NetworkedVarSettings();
        private readonly IDictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
        private NetworkedBehaviour networkedBehaviour;
        private readonly List<NetworkedDictionaryEvent<TKey, TValue>> dirtyEvents = new List<NetworkedDictionaryEvent<TKey, TValue>>();

        /// <inheritdoc />
        public void ResetDirty()
        {
            dirtyEvents.Clear();
        }

        /// <inheritdoc />
        public string GetChannel()
        {
            return Settings.SendChannel;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void SetFieldFromReader(BitReader reader)
        {
            ushort entryCount = reader.ReadUShort();

            for (int i = 0; i < entryCount; i++)
            {
                //TODO: readKey
                //TODO: readVal
            }
        }

        /// <inheritdoc />
        public void SetNetworkedBehaviour(NetworkedBehaviour behaviour)
        {
            networkedBehaviour = behaviour;
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void WriteFieldToWriter(BitWriter writer)
        {
            writer.WriteUShort((ushort)dictionary.Count);
            for (int i = 0; i < dictionary.Count; i++)
            {
                //TODO: writeKey
                //TODO: writeVal
            }
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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
        
        /// <inheritdoc />
        public bool IsDirty()
        {
            if (dirtyEvents.Count == 0) return false;
            if (Settings.SendOnChange) return true;
            if (NetworkingManager.singleton.NetworkTime - LastSyncedTime >= Settings.SendDelay) return true;
            return false;
        }


        /// <inheritdoc />
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
            dictionary.Add(key, value);
            dirtyEvents.Add(new NetworkedDictionaryEvent<TKey, TValue>()
            {
                eventType = NetworkedDictionaryEvent<TKey, TValue>.NetworkedListEventType.Add,
                key = key,
                value = value
            });
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void Clear()
        {
            dictionary.Clear();
            dirtyEvents.Add(new NetworkedDictionaryEvent<TKey, TValue>()
            {
                eventType = NetworkedDictionaryEvent<TKey, TValue>.NetworkedListEventType.Clear
            });
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

        /// <inheritdoc />
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
