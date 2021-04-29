using System;
using MLAPI.Serialization;
using UnityEngine;

namespace MLAPI.Timing
{
    public class NetworkTimeSystem
    {
        private INetworkTimeProvider m_NetworkTimeProvider;
        private int m_TickRate;

        private NetworkTime m_PredictedTime;
        private NetworkTime m_ServerTime;

        /// <summary>
        /// Special value to indicate "No tick information"
        /// </summary>
        public const int NoTick = int.MinValue;

        //public NetworkTime LastReceivedServerSnapshot { get; }

        /// <summary>
        /// The current predicted time. This is the time at which predicted or client authoritative objects move. This value is accurate when called in Update or NetworkFixedUpdate but does not work correctly for FixedUpdate.
        /// </summary>
        public NetworkTime PredictedTime => m_PredictedTime;

        /// <summary>
        /// The current server time. This value is mostly used for internal purposes and to interpolate state received from the server. This value is accurate when called in Update or NetworkFixedUpdate but does not work correctly for FixedUpdate.
        /// </summary>
        public NetworkTime ServerTime => m_ServerTime;

        /// <summary>
        /// The TickRate of the time system. This decides how often a fixed network tick is run.
        /// </summary>
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
