using UnityEngine;

namespace MLAPI.Timing
{
    /// <summary>
    /// The <see cref="INetworkTimeProvider"/> interface drives the advance of network time on both the client and the server.
    /// Can be used to adjust buffering for both incoming server updates and outgoing messages by adjusting <see cref="NetworkTime.PredictedTime"/> and <see cref="NetworkTime.ServerTime"/>.
    /// On the server the <see cref="ServerNetworkTimeProvider"/> or a similar implementation which runs at a fixed tick should be used.
    /// </summary>
    public interface INetworkTimeProvider
    {
        /// <summary>
        /// Called by the <see cref="NetworkTimeSystem"/> once per frame to advance time.
        /// </summary>
        /// <param name="predictedTime">The predicted time to advance.</param>
        /// <param name="serverTime">The server time to advance.</param>
        /// <param name="deltaTime">The delta time which passed.</param>
        /// <returns>false if advancing the the time succeeded; otherwise true if there was a hard correction.</returns>
        public bool AdvanceTime(ref NetworkTime predictedTime, ref NetworkTime serverTime, float deltaTime);

        /// <summary>
        /// Called once on clients only to initialize time. This function needs to set <see cref="predictedTime"/> to a value which is atleast expected RTT ahead of server time.
        /// </summary>
        /// <param name="predictedTime">The predicted time to initialize. In value is serverTime.</param>
        /// <param name="serverTime">the server time to initialize. In value is a time matching the tick of the initial received approval packet.</param>
        public void InitializeClient(ref NetworkTime predictedTime, ref NetworkTime serverTime);
    }
}
