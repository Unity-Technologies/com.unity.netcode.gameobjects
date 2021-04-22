using System;
using UnityEngine;

namespace MLAPI.NetworkTime
{
    // todo: This is a pretty minimal tick system. It will be improved in the future
    // It currently relies on Time.unscaledTime and, as such, will start suffering
    // numerical precision issues after 2^23 ticks have passed (float have 23 bits mantissa)
    // For future releases, we'll need to improve on this, probably by leveraging FixedUpdate

    public class NetworkTickSystem
    {

        private INetworkTimeProvider m_NetworkTimeProvider;

        private NetworkTime m_PredictedTime;
        private NetworkTime m_ServerTime;

        // special value to indicate "No tick information"
        public const ushort NoTick = ushort.MaxValue;

        // Number of ticks over which the tick number wraps back to 0
        public const ushort TickPeriod = NoTick - 1;

        public NetworkTime LastReceivedServerSnapshot { get; }

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
        private void UpdateNetworkTick(float deltaTime)
        {
            m_NetworkTimeProvider.HandleTime(ref m_PredictedTime, ref m_ServerTime,  deltaTime);


            //m_NetworkTickCount = (int)(Time.unscaledTime / m_TickIntervalSec);
        }
    }
}
