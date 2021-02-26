using UnityEngine;

namespace MLAPI
{
    public class NetworkTickSystem : INetworkUpdateSystem
    {
        private const double k_DefaultTickDuration = 0.016;
        private double m_TickDuration;
        private int m_NetworkTick; //How many network ticks have passed?

        private static NetworkTickSystem m_Instance = null;

        // special value to indicate "No tick information"
        public const ushort k_NoTick = ushort.MaxValue;
        // Number of ticks over which the tick number wraps back to 0
        public const ushort k_TickPeriod = k_NoTick - 1;

        public static NetworkTickSystem Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new NetworkTickSystem();
                }
                return m_Instance;
            }
        }

        /// <summary>
        /// GetTick
        /// Gets the current network tick (non-fractional, wrapping around)
        /// </summary>
        /// <returns></returns>
        public ushort GetTick()
        {
            return (ushort)(m_NetworkTick % k_TickPeriod);
        }

        /// <summary>
        /// GetNetworkTime
        /// Network time is calculated from m_NetworkTick and m_TickDuration (tick frequency)
        /// </summary>
        /// <returns>Network Time</returns>
        public double GetNetworkTime()
        {
            return m_NetworkTick * m_TickDuration;
        }

        /// <summary>
        /// UpdateNetworkTick
        /// Called each network loop update during the PreUpdate stage
        /// </summary>
        private void UpdateNetworkTick()
        {
            m_NetworkTick = (int)(Time.unscaledTime / m_TickDuration);
        }

        /// <summary>
        /// Constructor
        /// Defaults to k_DefaultTickDuration if no tick duration is specified
        /// </summary>
        /// <param name="tickDuration">Duration of a network tick</param>
        public NetworkTickSystem(double tickDuration = k_DefaultTickDuration)
        {
            //Assure we don't specify a value less than or equal to zero for tick frequency
            m_TickDuration = (tickDuration <= 0d) ? k_DefaultTickDuration : tickDuration;

            // ticks might not start at 0, so let's update right away at construction
            UpdateNetworkTick();
        }

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            switch (updateStage)
            {
                case NetworkUpdateStage.EarlyUpdate:
                    UpdateNetworkTick();
                    break;
            }
        }
    }
}
