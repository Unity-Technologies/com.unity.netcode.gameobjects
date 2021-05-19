using UnityEngine;

namespace MLAPI.Timing
{
    /// <summary>
    /// Interface to abstract access to network conditions. This exists mainly for testing purposes but also to decouple functionality from <see cref="NetworkManager"/>,
    /// </summary>
    public interface INetworkStats
    {
        /// <summary>
        /// Gets the current RTT in seconds. Additional smoothing for the RTT can be implemented this does not have to exactly match the real world value.
        /// </summary>
        float Rtt { get; }

        /// <summary>
        /// Gets the tick of the most recently received snapshot ack.
        /// </summary>
        NetworkTime LastReceivedSnapshotTick { get; }
    }
}
