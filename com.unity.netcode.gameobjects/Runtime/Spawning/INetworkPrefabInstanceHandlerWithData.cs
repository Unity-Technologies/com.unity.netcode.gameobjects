using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Specialized version of <see cref="INetworkPrefabInstanceHandler"/> that receives
    /// custom instantiation data injected by the server before spawning.
    /// </summary>
    public interface INetworkPrefabInstanceHandlerWithData<T> : INetworkPrefabInstanceHandlerWithData where T : struct, INetworkSerializable
    {
        static readonly Dictionary<INetworkPrefabInstanceHandlerWithData, T> _table = new();

        NetworkObject INetworkPrefabInstanceHandler.Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation) => Instantiate(ownerClientId, position, rotation, _table[this]);
        void INetworkPrefabInstanceHandlerWithData.RemoveDataEntry(INetworkPrefabInstanceHandlerWithData instance) => _table.Remove(instance);
        bool INetworkPrefabInstanceHandlerWithData.HandlesDataType<U>() => typeof(T) == typeof(U);
        void INetworkPrefabInstanceHandlerWithData.ReadInstantiationData<RW>(ref BufferSerializer<RW> serializer)
        {
            _table.TryGetValue(this, out var value);
            serializer.SerializeValue(ref value);
            _table[this] = value;
        }

        NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation, T instantiationData);
    }

    /// <summary>
    /// Internal use only. Do not implement directly. Use <see cref="INetworkPrefabInstanceHandlerWithData{T}"/> instead.
    /// </summary>
    public interface INetworkPrefabInstanceHandlerWithData : INetworkPrefabInstanceHandler
    {
        /// <summary>
        /// Invoked during deserialization to read the instantiation data associated with this prefab instance.
        /// </summary>
        void ReadInstantiationData<T>(ref BufferSerializer<T> serializer) where T : IReaderWriter;

        /// <summary>
        /// Removes the data entry for the given instance.
        /// Is important to call this when the instance isnt referenced to avoid memory leaks.
        /// </summary>
        /// <param name="instance"></param>
        void RemoveDataEntry(INetworkPrefabInstanceHandlerWithData instance);

        /// <summary>
        /// Returns true if <typeparamref name="T"/> matches the expected instantiation data type for this handler.
        /// </summary>
        bool HandlesDataType<T>() where T : struct, INetworkSerializable;
    }
}
