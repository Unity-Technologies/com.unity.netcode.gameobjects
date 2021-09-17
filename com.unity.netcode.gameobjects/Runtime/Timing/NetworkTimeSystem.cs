using System;

namespace Unity.Netcode
{
    /// <summary>
    /// <see cref="NetworkTimeSystem"/> is a standalone system which can be used to run a network time simulation.
    /// The network time system maintains both a local and a server time. The local time is based on
    /// </summary>
    public class NetworkTimeSystem
    {
        private double m_TimeSec;
        private double m_CurrentLocalTimeOffset;
        private double m_DesiredLocalTimeOffset;
        private double m_CurrentServerTimeOffset;
        private double m_DesiredServerTimeOffset;

        /// <summary>
        /// Gets or sets the amount of time in seconds the server should buffer incoming client messages.
        /// This increases the difference between local and server time so that messages arrive earlier on the server.
        /// </summary>
        public double LocalBufferSec { get; set; }

        /// <summary>
        /// Gets or sets the amount of the time in seconds the client should buffer incoming messages from the server. This increases server time.
        /// A higher value increases latency but makes the game look more smooth in bad networking conditions.
        /// This value must be higher than the tick length client side.
        /// </summary>
        public double ServerBufferSec { get; set; }

        /// <summary>
        /// Gets or sets a threshold in seconds used to force a hard catchup of network time.
        /// </summary>
        public double HardResetThresholdSec { get; set; }

        /// <summary>
        /// Gets or sets the ratio at which the NetworkTimeSystem speeds up or slows down time.
        /// </summary>
        public double AdjustmentRatio { get; set; }
        public double LocalTime => m_TimeSec + m_CurrentLocalTimeOffset;
        public double ServerTime => m_TimeSec + m_CurrentServerTimeOffset;

        internal double LastSyncedServerTimeSec { get; private set; }
        internal double LastSyncedRttSec { get; private set; }

        public NetworkTimeSystem(double localBufferSec, double serverBufferSec, double hardResetThresholdSec, double adjustmentRatio = 0.01d)
        {
            LocalBufferSec = localBufferSec;
            ServerBufferSec = serverBufferSec;
            HardResetThresholdSec = hardResetThresholdSec;
            AdjustmentRatio = adjustmentRatio;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="NetworkTimeSystem"/> class for a server instance.
        /// The server will not apply any buffer values which ensures that local time equals server time.
        /// </summary>
        /// <returns>The instance.</returns>
        public static NetworkTimeSystem ServerTimeSystem()
        {
            return new NetworkTimeSystem(0, 0, double.MaxValue);
        }

        /// <summary>
        /// Advances the time system by a certain amount of time. Should be called once per frame with Time.deltaTime or similar.
        /// </summary>
        /// <param name="deltaTimeSec">The amount of time to advance. The delta time which passed since Advance was last called.</param>
        /// <returns></returns>
        public bool Advance(double deltaTimeSec)
        {
            m_TimeSec += deltaTimeSec;

            if (Math.Abs(m_DesiredLocalTimeOffset - m_CurrentLocalTimeOffset) > HardResetThresholdSec || Math.Abs(m_DesiredServerTimeOffset - m_CurrentServerTimeOffset) > HardResetThresholdSec)
            {
                m_TimeSec += m_DesiredServerTimeOffset;

                m_DesiredLocalTimeOffset -= m_DesiredServerTimeOffset;
                m_CurrentLocalTimeOffset = m_DesiredLocalTimeOffset;

                m_DesiredServerTimeOffset = 0;
                m_CurrentServerTimeOffset = 0;

                return true;
            }

            m_CurrentLocalTimeOffset += deltaTimeSec * (m_DesiredLocalTimeOffset > m_CurrentLocalTimeOffset ? AdjustmentRatio : -AdjustmentRatio);
            m_CurrentServerTimeOffset += deltaTimeSec * (m_DesiredServerTimeOffset > m_CurrentServerTimeOffset ? AdjustmentRatio : -AdjustmentRatio);

            return false;
        }

        /// <summary>
        /// Resets the time system to a time based on the given network parameters.
        /// </summary>
        /// <param name="serverTimeSec">The most recent server time value received in seconds.</param>
        /// <param name="rttSec">The current RTT in seconds. Can be an averaged or a raw value.</param>
        public void Reset(double serverTimeSec, double rttSec)
        {
            Sync(serverTimeSec, rttSec);
            Advance(0);
        }

        /// <summary>
        /// Synchronizes the time system with up-to-date network statistics but does not change any time values or advance the time.
        /// </summary>
        /// <param name="serverTimeSec">The most recent server time value received in seconds.</param>
        /// <param name="rttSec">The current RTT in seconds. Can be an averaged or a raw value.</param>
        public void Sync(double serverTimeSec, double rttSec)
        {
            LastSyncedRttSec = rttSec;
            LastSyncedServerTimeSec = serverTimeSec;

            var timeDif = serverTimeSec - m_TimeSec;

            m_DesiredServerTimeOffset = timeDif - ServerBufferSec;
            m_DesiredLocalTimeOffset = timeDif + rttSec + LocalBufferSec;
        }
    }
}
