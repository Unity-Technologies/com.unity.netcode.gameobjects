namespace Unity.Netcode
{
    /// <summary>
    /// Interface for implementing custom serializable types.
    /// </summary>
    public interface INetworkSerializable
    {
        /// <summary>
        /// Provides bi-directional serialization to read and write the desired data to serialize this type.
        /// </summary>
        /// <param name="serializer">The serializer to use to read and write the data.</param>
        /// <typeparam name="T">
        /// Either BufferSerializerReader or BufferSerializerWriter, depending whether the serializer
        /// is in read mode or write mode.
        /// </typeparam>
        void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter;
    }
}
