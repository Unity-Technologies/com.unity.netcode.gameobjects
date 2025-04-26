namespace Unity.Netcode
{
    /// <summary>
    /// Interface for synchronizing custom instantiation payloads during NetworkObject spawning.
    /// Used alongside <see cref="INetworkPrefabInstanceHandler"/> to extend instantiation behavior.
    /// </summary>
    public interface INetworkInstantiationPayloadSynchronizer
	{
        /// <summary>
        /// Provides a method for synchronizing instantiation payload data during the spawn process.
        /// Extends <see cref="INetworkPrefabInstanceHandler"/> to allow passing additional data prior to instantiation
        /// to help identify or configure the local object instance that should be linked to the spawned NetworkObject.
        ///
        /// This method is invoked immediately before <see cref="INetworkPrefabInstanceHandler.Instantiate"/> is called,
        /// allowing you to cache or prepare information needed during instantiation.
        /// </summary>
        /// <param name="serializer">The buffer serializer used to read or write custom instantiation data.</param>
        void OnSynchronize<T>(ref BufferSerializer<T> serializer) where T : IReaderWriter;
	}
}
