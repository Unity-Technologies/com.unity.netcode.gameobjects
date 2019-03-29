using System;

namespace MLAPI.Transports
{
    /// <summary>
    /// A transport channel used by the MLAPI
    /// </summary>
    [Serializable]
    public class TransportChannel
    {
        /// <summary>
        /// The name of the channel
        /// </summary>
        public string Name;

        /// <summary>
        /// The type of channel
        /// </summary>
        public ChannelType Type;
    }
}