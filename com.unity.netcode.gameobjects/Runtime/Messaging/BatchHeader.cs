namespace Unity.Netcode
{
    /// <summary>
    /// Header placed at the start of each message batch
    /// </summary>
    internal struct BatchHeader : INetworkSerializeByMemcpy
    {
        public ushort ID;
        /// <summary>
        /// Total number of messages in the batch.
        /// </summary>
        public ushort BatchSize;
    }
}
