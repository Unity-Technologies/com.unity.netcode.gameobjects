using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MLAPI.NetworkTime
{
    public struct NetworkTime
    {
        private int m_TickRate;
        private float m_TickInterval;

        private int m_Tick;
        private float m_TickDuration;

        public float RenderTime => m_Tick * m_TickInterval + m_TickDuration;

        public int TickRate => m_TickRate;

        public NetworkTime(int tickRate)
        {
            m_TickRate = tickRate;
            m_TickInterval = 1f / m_TickRate;
            m_TickDuration = 0;
            m_Tick = 0;
        }

        public void AddTime(float time)
        {
            m_TickDuration += time;

            var deltaTicks = Mathf.FloorToInt(m_TickDuration * m_TickRate); // Why is there no divrem :(
            m_TickDuration %= m_TickInterval;

            m_Tick += deltaTicks;
        }
    }
}
