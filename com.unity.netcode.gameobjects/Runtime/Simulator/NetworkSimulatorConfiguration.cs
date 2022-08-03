// NetSim Implementation compilation boilerplate
// All references to UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED should be defined in the same way,
// as any discrepancies are likely to result in build failures
// ---------------------------------------------------------------------------------------------------------------------
#if UNITY_EDITOR || ((DEVELOPMENT_BUILD && !UNITY_MP_TOOLS_NETSIM_DISABLED_IN_DEVELOP) || (!DEVELOPMENT_BUILD && UNITY_MP_TOOLS_NETSIM_ENABLED_IN_RELEASE))
    #define UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
#endif
// ---------------------------------------------------------------------------------------------------------------------

using System;
using UnityEngine;

namespace Unity.Netcode
{
    [Serializable]
    public class NetworkSimulatorConfiguration : INetworkSimulatorConfiguration
    {
        /// <summary>
        /// Network simulation configuration name.
        /// </summary>
        [field: SerializeField]
        public string Name { get; set; }

        /// <summary>
        /// Optional description of the configuration.
        /// </summary>
        [field: SerializeField]
        public string Description { get; set; }

        /// <summary>
        /// Value for the delay between packet in milliseconds.
        /// </summary>
        [field: SerializeField]
        public int PacketDelayMs { get; set; }

        /// <summary>
        /// Value for the network jitter (variance) in milliseconds.
        /// </summary>
        [field: SerializeField]
        public int PacketJitterMs { get; set; }

        /// <summary>
        /// Value for at which interval packet are dropped
        /// This value is a drop every X packet, not in time.
        /// </summary>
        [field: SerializeField]
        public int PacketLossInterval { get; set; }

        /// <summary>
        /// Value for the average percentage of packet are dropped.
        /// </summary>
        [field: SerializeField]
        public int PacketLossPercent { get; set; }

        /// <summary>
        /// Value for the percentage of packet that should be duplicate.
        /// </summary>
        [field: SerializeField]
        public int PacketDuplicationPercent { get; set; }

        /// <summary>
        /// Utility function to create a configuration at runtime.
        /// </summary>
        /// <param name="name">Name of the configuration.</param>
        /// <param name="description">Description of the configuration.</param>
        /// <param name="packetDelayMs">Value for the packet delay in milliseconds.</param>
        /// <param name="packetJitterMs">Value for the network jitter in milliseconds.</param>
        /// <param name="packetLossInterval">Value for the packet loss interval.</param>
        /// <param name="packetLossPercent">Value for the packet loss percentage.</param>
        /// <param name="packetDuplicationPercent">Value for the packet duplication percentage.</param>
        /// <param name="packetFuzzOffset"></param>
        /// <returns>A valid simulation configuration.</returns>
        public static NetworkSimulatorConfiguration Create(
            string name,
            string description = "",
            int packetDelayMs = 0,
            int packetJitterMs = 0,
            int packetLossInterval = 0,
            int packetLossPercent = 0,
            int packetDuplicationPercent = 0)
        {
            var configuration = new NetworkSimulatorConfiguration
            {
                Name = name,
                Description = description,
                PacketDelayMs = packetDelayMs,
                PacketJitterMs = packetJitterMs,
                PacketLossInterval = packetLossInterval,
                PacketLossPercent = packetLossPercent,
                PacketDuplicationPercent = packetDuplicationPercent
            };

            return configuration;
        }
    }
}
