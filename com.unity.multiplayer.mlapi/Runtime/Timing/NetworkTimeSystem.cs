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
        private double m_Time;

        private double m_CurrentLocalTimeOffset;

        private double m_DesiredLocalTimeOffset;

        private double m_CurrentServerTimeOffset;

        private double m_DesiredServerTimeOffset;

        /// <summary>
        /// Gets or sets the amount of time in seconds the server should buffer incoming client messages.
        /// This increases the difference between local and server time so that messages arrive earlier on the server.
        /// </summary>
        public double LocalBuffer { get; set; }

        /// <summary>
        /// Gets or sets the amount of the time in seconds the client should buffer incoming messages from the server. This increases server time.
        /// A higher value increases latency but makes the game look more smooth in bad networking conditions.
        /// </summary>
        public double ServerBuffer { get; set; }

        /// <summary>
        /// Gets or sets a threshold in seconds used to force a hard catchup of network time.
        /// </summary>
        public double HardResetThreshold { get; set; }

        /// <summary>
        /// Gets or sets the ratio at which the NetworkTimeSystem speeds up or slows down time.
        /// </summary>
        public double AdjustmentRatio { get; set; }

        public double LocalTime => m_Time + m_CurrentLocalTimeOffset;

        public double ServerTime => m_Time + m_CurrentServerTimeOffset;

        //TODO This is used as a workaround to pass this back into the sync function will be removed once we get correct value.
        internal double TimeSystemInternalTime => m_Time;

        public NetworkTimeSystem(double localBuffer, double serverBuffer, double hardResetThreshold, double adjustmentRatio = 0.01d)
        {
            LocalBuffer = localBuffer;
            ServerBuffer = serverBuffer;
            HardResetThreshold = hardResetThreshold;
            AdjustmentRatio = adjustmentRatio;
        }

        public static NetworkTimeSystem ServerTimeSystem()
        {
            return new NetworkTimeSystem(0, 0, 0);
        }

        public bool Advance(double deltaTime)
        {
            m_Time += deltaTime;

            if (Math.Abs(m_DesiredLocalTimeOffset - m_CurrentLocalTimeOffset) > HardResetThreshold || Math.Abs(m_DesiredServerTimeOffset - m_CurrentServerTimeOffset) > HardResetThreshold)
            {
                m_Time += m_DesiredServerTimeOffset;

                m_DesiredLocalTimeOffset -= m_DesiredServerTimeOffset;
                m_CurrentLocalTimeOffset = m_DesiredLocalTimeOffset;

                m_DesiredServerTimeOffset = 0;
                m_CurrentServerTimeOffset = 0;

                return true;
            }

            m_CurrentLocalTimeOffset += deltaTime * (m_DesiredLocalTimeOffset > m_CurrentLocalTimeOffset ? AdjustmentRatio : -AdjustmentRatio);
            m_CurrentServerTimeOffset += deltaTime * (m_DesiredServerTimeOffset > m_CurrentServerTimeOffset ? AdjustmentRatio : -AdjustmentRatio);

            return false;
        }

        public void Initialize(double serverTime, double rtt)
        {
            Sync(serverTime, rtt);
            Advance(0);
        }

        public void Sync(double serverTime, double rtt)
        {
            var timeDif = serverTime - m_Time;

            m_DesiredServerTimeOffset = timeDif - ServerBuffer;
            m_DesiredLocalTimeOffset = timeDif + rtt + LocalBuffer;
        }
    }
}
