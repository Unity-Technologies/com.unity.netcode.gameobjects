using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace MLAPI.Timing
{
    public struct NetworkTime
    {
        private int m_TickRate;
        private float m_TickInterval;

        private int m_Tick;
        private float m_TickDuration;

        internal float TickDuration => m_TickDuration;

        public float Time => m_Tick * m_TickInterval + m_TickDuration;

        public float FixedTime => m_Tick * m_TickInterval;

        public float FixedDeltaTime => m_TickInterval;

        public int Tick => m_Tick;

        public int TickRate => m_TickRate;

        public NetworkTime(int tickRate)
        {
            m_TickRate = tickRate;
            m_TickInterval = 1f / m_TickRate; // potential floating point precision issue, could result in different interval on different machines
            m_TickDuration = 0;
            m_Tick = 0;
        }

        public NetworkTime(int tickRate, int tick, float tickDuration = 0f)
            : this(tickRate)
        {
            Assert.IsTrue(tickDuration < 1f / tickRate);

            m_Tick = tick;
            m_TickDuration = tickDuration;
        }

        public NetworkTime(int tickRate, float time)
            : this(tickRate)
        {
            this += time;

            // This is due to floating point precision issues.
            if (m_TickDuration < 0)
            {
                this += m_TickDuration;
            }
        }

        public NetworkTime ToFixedTime()
        {
            return new NetworkTime(m_TickRate, m_Tick);
        }

        public static NetworkTime operator -(NetworkTime a, NetworkTime b)
        {
            Assert.AreEqual(a.TickRate, b.TickRate, $"NetworkTimes must have same TickRate to subtract.");

            int tick = a.Tick - b.Tick;
            float tickDuration = a.m_TickDuration - b.m_TickDuration;

            if (tickDuration < 0)
            {
                tick--;
                tickDuration += a.m_TickInterval;
            }

            return new NetworkTime(a.TickRate, tick, tickDuration);
        }

        public static NetworkTime operator +(NetworkTime a, NetworkTime b)
        {
            Assert.AreEqual(a.TickRate, b.TickRate, $"NetworkTimes must have same TickRate to add.");

            int tick = a.Tick + b.Tick;
            float tickDuration = a.m_TickDuration + b.m_TickDuration;

            if (tickDuration >= a.m_TickInterval)
            {
                tick++;
                tickDuration -= a.m_TickInterval;
            }

            return new NetworkTime(a.TickRate, tick, tickDuration);
        }

        public static NetworkTime operator +(NetworkTime a, float b)
        {
            a.m_TickDuration += b;

            // This is quite imprecise for large floating point numbers but there is no easy workaround for this.
            var deltaTicks = Mathf.FloorToInt(a.m_TickDuration * a.m_TickRate);
            a.m_TickDuration -= deltaTicks * a.m_TickInterval;

            a.m_Tick += deltaTicks;

            return a;
        }

        public static NetworkTime operator -(NetworkTime a, float b)
        {
            return a + -b;
        }
    }
}
