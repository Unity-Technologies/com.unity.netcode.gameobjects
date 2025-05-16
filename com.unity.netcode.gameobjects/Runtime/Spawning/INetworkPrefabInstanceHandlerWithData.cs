using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Specialized version of <see cref="INetworkPrefabInstanceHandler"/> that receives
    /// custom instantiation data injected by the server before spawning.
    /// </summary>
    public interface INetworkPrefabInstanceHandlerWithData<T> where T : struct, INetworkSerializable
    {
        NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation, T instantiationData);
        void Destroy(NetworkObject networkObject);
    }

    internal interface INetworkPrefabInstanceHandlerWithData : INetworkPrefabInstanceHandler
    {
        NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation, FastBufferReader reader);
        bool HandlesDataType<T>();
    }

    internal class HandlerWrapper<T> : INetworkPrefabInstanceHandlerWithData where T : struct, INetworkSerializable
    {
        private readonly INetworkPrefabInstanceHandlerWithData<T> _impl;
        
        public HandlerWrapper(INetworkPrefabInstanceHandlerWithData<T> impl) => _impl = impl;

        public bool HandlesDataType<U>() => typeof(T) == typeof(U);

        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation, FastBufferReader reader)
        {
            reader.ReadValueSafe(out T _payload);
            return _impl.Instantiate(ownerClientId, position, rotation, _payload);
        }

        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation) => _impl.Instantiate(ownerClientId, position, rotation, default);
        public void Destroy(NetworkObject networkObject) => _impl.Destroy(networkObject);
    }
}