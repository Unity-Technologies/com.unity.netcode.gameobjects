namespace Unity.Netcode.Transports.UTP
{
    /// <summary>
    /// Caching structure to track network metrics related information.
    /// </summary>
    public struct NetworkMetricsContext
    {
        /// <summary>
        /// The number of packet sent.
        /// </summary>
        public uint PacketSentCount;
        /// <summary>
        /// The number of packet received.
        /// </summary>
        public uint PacketReceivedCount;
    }
}
