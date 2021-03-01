using System;

namespace MLAPI.Transports
{
    /// <summary>
    /// A transport channel used by the MLAPI
    /// </summary>
    [Serializable]
    public class TransportChannel
    {
        public TransportChannel(NetworkChannel id, NetworkDelivery type)
        {
            Id = id;
            Type = type;
        }

        /// <summary>
        /// Channel identifier
        /// </summary>
        public NetworkChannel Id;

        /// <summary>
        /// Channel type
        /// </summary>
        public NetworkDelivery Type;
    }
}