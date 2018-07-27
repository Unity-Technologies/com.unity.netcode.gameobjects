namespace MLAPI.Transports
{
    /// <summary>
    /// Delivery methods
    /// </summary>
    public enum ChannelType
    {
        /// <summary>
        /// Unreliable message
        /// </summary>
        Unreliable,
        /// <summary>
        /// Unreliable with fragmentation support
        /// </summary>
        UnreliableFragmented,
        /// <summary>
        /// Unreliable with sequencing
        /// </summary>
        UnreliableSequenced,
        /// <summary>
        /// Reliable message
        /// </summary>
        Reliable,
        /// <summary>
        /// Reliable with fragmentation support
        /// </summary>
        ReliableFragmented,
        /// <summary>
        /// Reliable message where messages are guaranteed to be in the right order
        /// </summary>
        ReliableSequenced,
        /// <summary>
        /// A unreliable state update message
        /// </summary>
        StateUpdate,
        /// <summary>
        /// A reliable state update message
        /// </summary>
        ReliableStateUpdate,
        /// <summary>
        /// A reliable message with high priority
        /// </summary>
        AllCostDelivery,
        /// <summary>
        /// Unreliable message with fragmentation where older messages are dropped
        /// </summary>
        UnreliableFragmentedSequenced,
        /// <summary>
        /// A reliable message with guaranteed order with fragmentation support
        /// </summary>
        ReliableFragmentedSequenced
    }
}
