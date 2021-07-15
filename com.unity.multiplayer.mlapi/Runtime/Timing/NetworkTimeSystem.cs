using System;
using UnityEngine;

namespace MLAPI.Timing
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
        public double localBufferSec { get; set; }

        /// <summary>
        /// Gets or sets the amount of the time in seconds the client should buffer incoming messages from the server. This increases server time.
        /// A higher value increases latency but makes the game look more smooth in bad networking conditions.
        /// </summary>
        public double serverBufferSec { get; set; }

        /// <summary>
        /// Gets or sets a threshold in seconds used to force a hard catchup of network time.
        /// </summary>
        public double hardResetThresholdSec { get; set; }

        /// <summary>
        /// Gets or sets the ratio at which the NetworkTimeSystem speeds up or slows down time.
        /// </summary>
        public double AdjustmentRatio { get; set; }

        public double LocalTime => m_TimeSec + m_CurrentLocalTimeOffset;

        public double ServerTime => m_TimeSec + m_CurrentServerTimeOffset;

        //TODO This is used as a workaround to pass this back into the sync function will be removed once we get correct value.
        // Once we have a sequence-ack algorithm we get access to the last received tick time instead.
        // See NetworkManager.OnNetworkPreUpdate where this is used. Because we don't receive time sync messages from the server yet we just manually fake a time advancement by adding delta time to our existing time.
        internal double TimeSystemInternalTime => m_TimeSec;

        public NetworkTimeSystem(double localBufferSec, double serverBufferSec, double hardResetThresholdSec, double adjustmentRatio = 0.01d)
        {
            this.localBufferSec = localBufferSec;
            this.serverBufferSec = serverBufferSec;
            this.hardResetThresholdSec = hardResetThresholdSec;
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

            if (Math.Abs(m_DesiredLocalTimeOffset - m_CurrentLocalTimeOffset) > hardResetThresholdSec || Math.Abs(m_DesiredServerTimeOffset - m_CurrentServerTimeOffset) > hardResetThresholdSec)
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

        public void Reset(double serverTimeSec, double rttSec)
        {
            Sync(serverTimeSec, rttSec);
            Advance(0);
        }

        public void Sync(double serverTimeSec, double rttSec)
        {
            var timeDif = serverTimeSec - m_TimeSec;

            m_DesiredServerTimeOffset = timeDif - serverBufferSec;
            m_DesiredLocalTimeOffset = timeDif + rttSec + localBufferSec;
        }
    }
}
