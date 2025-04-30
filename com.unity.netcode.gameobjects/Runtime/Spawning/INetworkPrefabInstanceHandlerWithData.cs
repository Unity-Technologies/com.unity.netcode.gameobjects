namespace Unity.Netcode
{
    /// <summary>
    /// Specialized version of <see cref="INetworkPrefabInstanceHandler"/> that supports synchronizing
    /// custom data prior to the instantiation of a <see cref="NetworkObject"/>.
    /// </summary>
    public interface INetworkPrefabInstanceHandlerWithData : INetworkPrefabInstanceHandler
    {
        /// <summary>
        /// Allows synchronizing custom instantiation data before the object is instantiated. <br/>
        /// Called before <see cref="INetworkPrefabInstanceHandler.Instantiate"/>.
        /// </summary>
        /// <param name="serializer">The serializer used to synchronize the custom instantiation data.</param>
        void OnSynchronizeInstantiationData<T>(ref BufferSerializer<T> serializer) where T : IReaderWriter;
    }
}
