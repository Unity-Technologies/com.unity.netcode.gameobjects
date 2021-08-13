using System;

namespace Unity.Netcode
{
    /// <summary>
    /// A transport channel used by the netcode
    /// </summary>
    [Serializable]
    public struct TransportChannel
    {
        public TransportChannel(NetworkChannel channel, NetworkDelivery delivery)
        {
            Channel = channel;
            Delivery = delivery;
        }

        /// <summary>
        /// Channel identifier
        /// </summary>
        public NetworkChannel Channel;

        /// <summary>
        /// Delivery type
        /// </summary>
        public NetworkDelivery Delivery;
    }
}
