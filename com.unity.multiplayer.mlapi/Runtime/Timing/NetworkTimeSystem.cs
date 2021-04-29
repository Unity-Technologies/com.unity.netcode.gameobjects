using System;
using UnityEngine;

namespace MLAPI.Timing
{
    public class NetworkTimeSystem
    {
        private INetworkTimeProvider m_NetworkTimeProvider;
        private int m_TickRate;

        private NetworkTime m_PredictedTime;
        private NetworkTime m_ServerTime;

        // special value to indicate "No tick information"
        public const int NoTick = int.MinValue;

        // Number of ticks over which the tick number wraps back to 0
        // public const ushort TickPeriod = NoTick - 1;

        //public NetworkTime LastReceivedServerSnapshot { get; }

        public NetworkTime PredictedTime => m_PredictedTime;

        public NetworkTime ServerTime => m_ServerTime;

        public int TickRate => m_TickRate;

        /// <summary>
        /// Constructor
        /// Defaults to k_DefaultTickIntervalSec if no tick duration is specified
        /// </summary>
        /// <param name="tickRate">How many network ticks to run per second.</param>
        public NetworkTimeSystem(int tickRate)
        {
            m_NetworkTimeProvider = new FixedNetworkTimeProvider();

            m_PredictedTime = new NetworkTime(tickRate);
            m_ServerTime = new NetworkTime(tickRate);

            m_TickRate = tickRate;
        }

        /// <summary>
        /// UpdateNetworkTick
        /// Called each network loop update during the PreUpdate stage
        /// </summary>
        public void AdvanceNetworkTime(float deltaTime)
        {
            m_NetworkTimeProvider.HandleTime(ref m_PredictedTime, ref m_ServerTime, deltaTime);

            //m_NetworkTickCount = (int)(Time.unscaledTime / m_TickIntervalSec);
        }
    }
}
