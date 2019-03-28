namespace MLAPI.Transports
{
    /// <summary>
    /// A transport channel used by the MLAPI
    /// </summary>
    public class TransportChannel
    {
        /// <summary>
        /// The name of the channel
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The type of channel
        /// </summary>
        public ChannelType Type { get; set; }
    }
}