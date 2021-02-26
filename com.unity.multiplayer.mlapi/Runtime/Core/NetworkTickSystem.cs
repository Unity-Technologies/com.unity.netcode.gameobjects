using System;
using UnityEngine;

namespace MLAPI
{
    public class NetworkTickSystem : INetworkUpdateSystem, IDisposable
    {
        private const float k_DefaultTickIntervalSec = 1/60f; // Defaults to 60 ticks second
        private float m_TickIntervalSec; // Duration of a tick in seconds
        private int m_NetworkTickCount; // How many network ticks have passed?

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
        /// Defaults to k_DefaultTickIntervalSec if no tick duration is specified
        /// </summary>
        /// <param name="tickIntervalSec">Duration of a network tick</param>
        private NetworkTickSystem(float tickIntervalSec = k_DefaultTickIntervalSec)
        {
            this.RegisterNetworkUpdate(NetworkUpdateStage.EarlyUpdate);

            //Assure we don't specify a value less than or equal to zero for tick frequency
            m_TickIntervalSec = (tickIntervalSec <= 0f) ? k_DefaultTickIntervalSec : tickIntervalSec;

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
        /// Network time is calculated from m_NetworkTickCount and m_TickIntervalSec (tick frequency)
        /// </summary>
        /// <returns>Network Time</returns>
        public float GetNetworkTime()
        {
            return m_NetworkTickCount * m_TickIntervalSec;
        }

        /// <summary>
        /// UpdateNetworkTick
        /// Called each network loop update during the PreUpdate stage
        /// </summary>
        private void UpdateNetworkTick()
        {
            m_NetworkTickCount = (int)(Time.unscaledTime / m_TickIntervalSec);
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
