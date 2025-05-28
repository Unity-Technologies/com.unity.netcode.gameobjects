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
    public abstract class NetworkPrefabInstanceHandlerWithData<T> : INetworkPrefabInstanceHandlerWithData where T : struct, INetworkSerializable
    {
        public abstract NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation, T instantiationData);
        
        public abstract void Destroy(NetworkObject networkObject);

        bool INetworkPrefabInstanceHandlerWithData.HandlesDataType<U>() => typeof(T) == typeof(U);
       
        NetworkObject INetworkPrefabInstanceHandlerWithData.Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation, FastBufferReader reader)
        {
            var startPosition = reader.Position;
            reader.ReadValueSafe(out T _payload);
            var length = reader.Position - startPosition;
           
            NetworkObject networkObject = Instantiate(ownerClientId, position, rotation, _payload);
            reader.Seek(startPosition);
            if (networkObject.InstantiationData == null || networkObject.InstantiationData.Length != length)
            {
                networkObject.InstantiationData = new byte[length];
            }
            reader.ReadBytesSafe(ref networkObject.InstantiationData, length);
            return networkObject;
        }

        NetworkObject INetworkPrefabInstanceHandler.Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation) => Instantiate(ownerClientId, position, rotation, default);
    }

    internal interface INetworkPrefabInstanceHandlerWithData : INetworkPrefabInstanceHandler
    {
        bool HandlesDataType<T>();
        NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation, FastBufferReader reader);
    }
}