using System;

namespace MLAPI.Transports.Ruffles
{
    /// <summary>
    /// A transport channel used by the MLAPI
    /// </summary>
    [Serializable]
    public class RufflesChannel
    {
        /// <summary>
        /// The name of the channel
        /// </summary>
        public string Name;

        /// <summary>
        /// The type of channel
        /// </summary>
        public global::Ruffles.Channeling.ChannelType Type;
    }
}
