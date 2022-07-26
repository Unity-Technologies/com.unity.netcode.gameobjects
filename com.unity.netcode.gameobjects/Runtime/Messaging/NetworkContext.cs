namespace Unity.Netcode
{
    /// <summary>
    /// Metadata passed into the Receive handler for <see cref="INetworkMessage"/>.
    /// </summary>
    internal ref struct NetworkContext
    {
        /// <summary>
        /// An opaque object used to represent the owner of the MessagingSystem that's receiving the message.
        /// Outside of testing environments, the type of this variable will be <see cref="NetworkManager"/>
        /// </summary>
        public object SystemOwner;

        /// <summary>
        /// The originator of the message
        /// </summary>
        public ulong SenderId;

        /// <summary>
        /// The timestamp at which the message was received
        /// </summary>
        public float Timestamp;

        /// <summary>
        /// The header data that was sent with the message
        /// </summary>
        public MessageHeader Header;

        /// <summary>
        /// The actual serialized size of the header when packed into the buffer
        /// </summary>
        public int SerializedHeaderSize;

        /// <summary>
        /// The size of the message in the buffer, header excluded
        /// </summary>
        public uint MessageSize;
    }
}
