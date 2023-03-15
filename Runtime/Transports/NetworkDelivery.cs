namespace Unity.Netcode
{
    /// <summary>
    /// Delivery methods
    /// </summary>
    public enum NetworkDelivery
    {
        /// <summary>
        /// Unreliable message
        /// </summary>
        Unreliable,

        /// <summary>
        /// Unreliable with sequencing
        /// </summary>
        UnreliableSequenced,

        /// <summary>
        /// Reliable message
        /// </summary>
        Reliable,

        /// <summary>
        /// Reliable message where messages are guaranteed to be in the right order
        /// </summary>
        ReliableSequenced,

        /// <summary>
        /// A reliable message with guaranteed order with fragmentation support
        /// </summary>
        ReliableFragmentedSequenced
    }
}
