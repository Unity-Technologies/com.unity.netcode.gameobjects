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

        public float Time => m_Tick * m_TickInterval + m_TickDuration;

        public float FixedTime => m_Tick * m_TickInterval;

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

        public NetworkTime(int tickRate, float renderTime)
            : this(tickRate)
        {
            AddTime(renderTime);
        }

        // public NetworkTime ToFixedTime()
        // {
        //     return new NetworkTime(TickRate, Tick, 0f);
        // }

        public void AddTime(float time)
        {
            m_TickDuration += time;

            var deltaTicks = Mathf.FloorToInt(m_TickDuration * m_TickRate); // Why is there no divrem :(
            m_TickDuration %= m_TickInterval;

            m_Tick += deltaTicks;
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
    }
}
