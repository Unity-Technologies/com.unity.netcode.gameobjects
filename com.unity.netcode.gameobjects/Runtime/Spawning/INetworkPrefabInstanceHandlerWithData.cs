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
        bool HandlesDataType<T>();
        void ReadInstantiationData<TReaderWriter>(ref BufferSerializer<TReaderWriter> serializer) where TReaderWriter : IReaderWriter;
    }

    internal class HandlerWrapper<T> : INetworkPrefabInstanceHandlerWithData where T : struct, INetworkSerializable
    {
        private readonly INetworkPrefabInstanceHandlerWithData<T> _impl;
        private T _payload;

        public HandlerWrapper(INetworkPrefabInstanceHandlerWithData<T> impl) => _impl = impl;
        public bool HandlesDataType<U>() => typeof(T) == typeof(U);
        public void ReadInstantiationData<TReaderWriter>(ref BufferSerializer<TReaderWriter> serializer) where TReaderWriter : IReaderWriter => serializer.SerializeValue(ref _payload);
        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation) => _impl.Instantiate(ownerClientId, position, rotation, _payload);
        public void Destroy(NetworkObject networkObject) => _impl.Destroy(networkObject);
    }
}