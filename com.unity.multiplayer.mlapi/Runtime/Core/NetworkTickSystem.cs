using System;
using UnityEngine;

namespace MLAPI
{
    public class NetworkTickSystem : INetworkUpdateSystem, IDisposable
    {
        private const float k_DefaultTickDuration = 1/60f; // Default to 60 FPS
        private float m_TickInterval; //Duration of a tick
        private int m_NetworkTickCount; //How many network ticks have passed?

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
        /// Constructor
        /// Defaults to k_DefaultTickDuration if no tick duration is specified
        /// </summary>
        /// <param name="tickInterval">Duration of a network tick</param>
        private NetworkTickSystem(float tickInterval = k_DefaultTickDuration)
        {
            this.RegisterNetworkUpdate(NetworkUpdateStage.EarlyUpdate);

            //Assure we don't specify a value less than or equal to zero for tick frequency
            m_TickInterval = (tickInterval <= 0f) ? k_DefaultTickDuration : tickInterval;

            // ticks might not start at 0, so let's update right away at construction
            UpdateNetworkTick();
        }

        public void Dispose()
        {
            this.UnregisterNetworkUpdate(NetworkUpdateStage.EarlyUpdate);
        }

        /// <summary>
        /// GetTick
        /// Gets the current network tick (non-fractional, wrapping around)
        /// </summary>
        /// <returns></returns>
        public ushort GetTick()
        {
            return (ushort)(m_NetworkTickCount % k_TickPeriod);
        }

        /// <summary>
        /// GetNetworkTime
        /// Network time is calculated from m_NetworkTickCount and m_TickInterval (tick frequency)
        /// </summary>
        /// <returns>Network Time</returns>
        public float GetNetworkTime()
        {
            return m_NetworkTickCount * m_TickInterval;
        }

        /// <summary>
        /// UpdateNetworkTick
        /// Called each network loop update during the PreUpdate stage
        /// </summary>
        private void UpdateNetworkTick()
        {
            m_NetworkTickCount = (int)(Time.unscaledTime / m_TickInterval);
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
