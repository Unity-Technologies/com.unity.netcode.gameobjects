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
            reader.ReadValueSafe(out T _payload);
            return Instantiate(ownerClientId, position, rotation, _payload);
        }

        NetworkObject INetworkPrefabInstanceHandler.Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation) => Instantiate(ownerClientId, position, rotation, default);
    }

    internal interface INetworkPrefabInstanceHandlerWithData : INetworkPrefabInstanceHandler
    {
        bool HandlesDataType<T>();
        NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation, FastBufferReader reader);
    }
}