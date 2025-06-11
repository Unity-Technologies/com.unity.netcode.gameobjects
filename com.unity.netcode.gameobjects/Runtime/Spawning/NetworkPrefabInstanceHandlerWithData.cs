using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Specialized version of <see cref="INetworkPrefabInstanceHandler"/> that receives
    /// custom instantiation data injected by the server before spawning.
    /// </summary>
    public abstract class NetworkPrefabInstanceHandlerWithData<T> : INetworkPrefabInstanceHandlerWithData where T : struct, INetworkSerializable
    {
        /// <inheritdoc cref="INetworkPrefabInstanceHandler.Instantiate"/>
        /// <param name="ownerClientId">The client ID that will own the instantiated object.</param>
        /// <param name="position">The world position where the object should be spawned.</param>
        /// <param name="rotation">The world rotation for the spawned object.</param>
        /// <param name="instantiationData">Custom data of type <typeparamref name="T"/> provided by the server to be used during instantiation.</param>
        public abstract NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation, T instantiationData);

        /// <inheritdoc cref="INetworkPrefabInstanceHandler.Destroy"/>
        public abstract void Destroy(NetworkObject networkObject);

        bool INetworkPrefabInstanceHandlerWithData.HandlesDataType<TK>() => typeof(T) == typeof(TK);

        NetworkObject INetworkPrefabInstanceHandlerWithData.Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation, byte[] instantiationData)
        {
            using var reader = new FastBufferReader(instantiationData, Collections.Allocator.Temp);
            reader.ReadValueSafe(out T payload);

            var networkObject = Instantiate(ownerClientId, position, rotation, payload);
            networkObject.InstantiationData = instantiationData;

            return networkObject;
        }

        NetworkObject INetworkPrefabInstanceHandler.Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation) => Instantiate(ownerClientId, position, rotation, default);
    }

    internal interface INetworkPrefabInstanceHandlerWithData : INetworkPrefabInstanceHandler
    {
        public bool HandlesDataType<T>();
        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation, byte[] instantiationData);
    }
}
