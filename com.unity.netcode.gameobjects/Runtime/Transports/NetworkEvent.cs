namespace Unity.Netcode
{
    /// <summary>
    /// Represents a netEvent when polling
    /// </summary>
    public enum NetworkEvent
    {
        /// <summary>
        /// New data is received
        /// </summary>
        Data,

        /// <summary>
        /// A client is connected, or client connected to server
        /// </summary>
        Connect,

        /// <summary>
        /// A client disconnected, or client disconnected from server
        /// </summary>
        Disconnect,

        /// <summary>
        /// Transport has encountered an unrecoverable failure
        /// </summary>
        TransportFailure,

        /// <summary>
        /// No new event
        /// </summary>
        Nothing
    }
}
