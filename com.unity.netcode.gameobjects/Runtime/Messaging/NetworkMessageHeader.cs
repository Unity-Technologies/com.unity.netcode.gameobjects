namespace Unity.Netcode
{
    /// <summary>
    /// This is the header data that's serialized to the network when sending an <see cref="INetworkMessage"/>
    /// </summary>
    internal struct NetworkMessageHeader : INetworkSerializeByMemcpy
    {
        /// <summary>
        /// The byte representation of the message type. This is automatically assigned to each message by the NetworkMessageManager.
        /// This value is deterministic only so long as the list of messages remains unchanged - if new messages are added or messages are removed, MessageType assignments may be calculated differently.
        /// </summary>
        public uint MessageType;

        /// <summary>
        /// The total size of the message, NOT including the header.
        /// Stored as a uint to avoid zig-zag encoding, but capped at int.MaxValue.
        /// </summary>
        public uint MessageSize;
    }
}
