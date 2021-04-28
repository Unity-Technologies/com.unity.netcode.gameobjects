using System;
using UnityEngine;

namespace MLAPI.NetworkTime
{
    public class NetworkTickSystem
    {
        private INetworkTimeProvider m_NetworkTimeProvider;

        private NetworkTime m_PredictedTime;
        private NetworkTime m_ServerTime;

        // special value to indicate "No tick information"
        public const ushort NoTick = ushort.MaxValue;

        // Number of ticks over which the tick number wraps back to 0
        // public const ushort TickPeriod = NoTick - 1;

        //public NetworkTime LastReceivedServerSnapshot { get; }

        public NetworkTime PredictedTime => m_PredictedTime;

        /// <summary>
        /// Constructor
        /// Defaults to k_DefaultTickIntervalSec if no tick duration is specified
        /// </summary>
        /// <param name="tickRate">How many network ticks to run per second.</param>
        public NetworkTickSystem(int tickRate)
        {
            m_NetworkTimeProvider = new FixedNetworkTimeProvider();

            m_PredictedTime = new NetworkTime(tickRate);
            m_ServerTime = new NetworkTime(tickRate);
        }


        /// <summary>
        /// UpdateNetworkTick
        /// Called each network loop update during the PreUpdate stage
        /// </summary>
        public void UpdateNetworkTick(float deltaTime)
        {
            m_NetworkTimeProvider.HandleTime(ref m_PredictedTime, ref m_ServerTime,  deltaTime);


            //m_NetworkTickCount = (int)(Time.unscaledTime / m_TickIntervalSec);
        }
    }
}
