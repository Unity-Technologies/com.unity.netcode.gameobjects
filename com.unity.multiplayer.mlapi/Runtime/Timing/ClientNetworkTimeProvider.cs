using UnityEngine;

namespace MLAPI.Timing
{
    /// <summary>
    /// A <see cref="INetworkTimeProvider"/> which can be used on a client machine to control  and server time based on network conditions.
    /// The <see cref="ClientNetworkTimeProvider"/> speeds up or slows down network time when necessary to adjust for RTT changes.
    /// <see cref="ClientNetworkTimeProvider"/> is one of many ways to implement a network time provider for a client.
    /// If the existing properties are not sufficient one can write a custom provider by implementing the <see cref="INetworkTimeProvider"/> interface.
    /// </summary>
    public class ClientNetworkTimeProvider : INetworkTimeProvider
    {
        private INetworkStats m_NetworkStats;

        /// <summary>
        /// Gets or sets the initial RTT in seconds used when starting a client.
        /// This value will just be used for the initial time values after that the <see cref="m_NetworkStats"/> will provide the RTT.
        /// </summary>
        public float StartRtt { get; set; } = 150f / 1000f; // 150ms

        /// <summary>
        /// Gets or sets the ratio at which the <see cref="ClientNetworkTimeProvider"/> speeds up or slows down time.
        /// </summary>
        public float AdjustmentRatio { get; set; } = 0.01f;

        /// <summary>
        /// Gets or sets a threshold in seconds used to force a hard catchup of network time.
        /// </summary>
        public float HardResetThreshold { get; set; } = 200f / 1000f; // 200ms

        /// <summary>
        /// Gets the current time scale for server time.
        /// </summary>
        public float ServerTimeScale { get; private set; } = 1f;

        /// <summary>
        /// Gets the current time scale for local time.
        /// </summary>
        public float LocalTimeScale { get; private set; } = 1f;

        /// <summary>
        /// Gets or sets the amount of time in seconds the server should buffer incoming client messages.
        /// This increases the difference between local and server time so that messages arrive earlier on the server.
        /// A hy
        /// </summary>
        public float TargetClientBufferTime { get; set; }

        /// <summary>
        /// Gets or sets the amount of the time in seconds the client should buffer incoming messages from the server. This increases server time.
        /// A higher value increases latency but makes the game look more smooth in bad networking conditions.
        /// </summary>
        public float TargetServerBufferTime { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="ClientNetworkTimeProvider"/> class with default buffer values.
        /// </summary>
        /// <param name="networkStats">The <see cref="INetworkStats"/> to get network information from.</param>
        /// <param name="tickRate">The tickrate.</param>
        public ClientNetworkTimeProvider(INetworkStats networkStats, int tickRate)
        {
            m_NetworkStats = networkStats;

            // default buffer values. Can be manually overriden.
            TargetClientBufferTime = 1f / tickRate;
            TargetServerBufferTime = 2f / tickRate;
        }

        /// <inheritdoc/>
        public bool AdvanceTime(ref NetworkTime localTime, ref NetworkTime serverTime, double deltaTime)
        {
            // advance time
            localTime += deltaTime * LocalTimeScale;
            serverTime += deltaTime * ServerTimeScale;

            // time scale adjustment based on whether we are behind / ahead of the server in terms of inputs
            // This implementation uses RTT to calculate that without server input which is not ideal. In the future we might want to add a field to the protocol which allows the server to send the exact input buffers size to the client.
            double lastReceivedSnapshotTime = m_NetworkStats.LastReceivedSnapshotTick.Time;
            double targetServerTime = lastReceivedSnapshotTime - TargetClientBufferTime;
            double targetLocalTime = lastReceivedSnapshotTime + m_NetworkStats.Rtt + TargetServerBufferTime;

            // Reset timescale. Will be recalculated based on new values.
            bool reset = false;
            LocalTimeScale = 1f;
            ServerTimeScale = 1f;

            // reset because too large local offset?
            if (localTime.FixedTime < targetLocalTime - HardResetThreshold || localTime.FixedTime > targetLocalTime + HardResetThreshold)
            {
                reset = true;
            }

            // reset because too large server offset?
            if (serverTime.FixedTime < targetServerTime - HardResetThreshold || serverTime.FixedTime > targetServerTime + HardResetThreshold)
            {
                reset = true;
            }

            // Always reset both times to not break simulation integrity.
            if (reset)
            {
                localTime = new NetworkTime(localTime.TickRate, targetLocalTime);
                serverTime = new NetworkTime(serverTime.TickRate, targetServerTime);
                return true;
            }

            // Adjust time scale
            LocalTimeScale += targetLocalTime > localTime.FixedTime ? AdjustmentRatio : -AdjustmentRatio;
            ServerTimeScale += targetServerTime > serverTime.FixedTime ? AdjustmentRatio : -AdjustmentRatio;

            return false;
        }

        /// <inheritdoc/>
        public void InitializeClient(ref NetworkTime localTime, ref NetworkTime serverTime)
        {
            serverTime += TargetClientBufferTime;
            localTime = serverTime;
            localTime += StartRtt;
        }
    }
}
