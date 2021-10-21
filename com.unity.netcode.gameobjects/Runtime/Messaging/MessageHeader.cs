namespace Unity.Netcode
{
    /// <summary>
    /// This is the header data that's serialized to the network when sending an <see cref="INetworkMessage"/>
    /// </summary>
    internal struct MessageHeader
    {
        /// <summary>
        /// The byte representation of the message type. This is automatically assigned to each message
        /// by the MessagingSystem. This value is deterministic only so long as the list of messages remains
        /// unchanged - if new messages are added or messages are removed, MessageType assignments may be
        /// calculated differently.
        /// </summary>
        public byte MessageType;

        /// <summary>
        /// The total size of the message, NOT including the header.
        /// </summary>
        public ushort MessageSize;
    }
}
