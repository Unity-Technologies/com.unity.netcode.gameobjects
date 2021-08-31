using System;

namespace Unity.Netcode
{
    /// <summary>
    /// A transport channel used by the netcode
    /// </summary>
    [Serializable]
    public struct TransportChannel
    {
        public TransportChannel(NetworkDelivery delivery)
        {
            Delivery = delivery;
        }

        /// <summary>
        /// Delivery type
        /// </summary>
        public NetworkDelivery Delivery;
    }
}
