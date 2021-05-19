using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace MLAPI.Timing
{
    /// <summary>
    /// A struct to represent a point of time in a networked game.
    /// Time is stored as a combination of amount of passed ticks + a duration offset.
    /// This struct is meant to replace the Unity <see cref="Time"/> API for multiplayer gameplay.
    /// </summary>
    public struct NetworkTime
    {
        private int m_TickRate;
        private float m_TickInterval;

        private int m_Tick;
        private float m_TickDurationOffset;

        /// <summary>
        /// Gets the amount of time which has passed since the last network tick.
        /// </summary>
        public float tickDurationOffset => m_TickDurationOffset;

        /// <summary>
        /// The current time. This is a non fixed time value and similar to <see cref="Time.Time"/>
        /// </summary>
        public float Time => m_Tick * m_TickInterval + m_TickDurationOffset;

        /// <summary>
        /// The current fixed network time. This is the time value of the last network tick. Similar to <see cref="Time.FixedTime"/>
        /// </summary>
        public float FixedTime => m_Tick * m_TickInterval;

        /// <summary>
        ///
        /// </summary>
        public float FixedDeltaTime => m_TickInterval;

        /// <summary>
        /// Gets the amount of network ticks which have passed until reaching the current time value.
        /// </summary>
        public int Tick => m_Tick;

        /// <summary>
        /// Gets the tickrate of the system of this <see cref="NetworkTime"/>.
        /// </summary>
        public int TickRate => m_TickRate;

        /// <summary>
        /// Creates a new instance of the <see cref="NetworkTime"/> struct.
        /// </summary>
        /// <param name="tickRate">The tickrate.</param>
        public NetworkTime(int tickRate)
        {
            m_TickRate = tickRate;
            m_TickInterval = 1f / m_TickRate; // potential floating point precision issue, could result in different interval on different machines
            m_TickDurationOffset = 0;
            m_Tick = 0;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="NetworkTime"/> struct.
        /// </summary>
        /// <param name="tickRate">The tickrate.</param>
        /// <param name="tick">The time will be created with a value where this many tick have already passed.</param>
        /// <param name="tickDurationOffset">Can be used to create a <see cref="NetworkTime"/> with a non fixed time value by adding an offset to the given tick value.</param>
        public NetworkTime(int tickRate, int tick, float tickDurationOffset = 0f)
            : this(tickRate)
        {
            Assert.IsTrue(tickDurationOffset < 1f / tickRate);

            m_Tick = tick;
            m_TickDurationOffset = tickDurationOffset;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="NetworkTime"/> struct.
        /// </summary>
        /// <param name="tickRate">The tickrate.</param>
        /// <param name="time">The time value as a float.</param>
        public NetworkTime(int tickRate, float time)
            : this(tickRate)
        {
            this += time;

            // This is due to floating point precision issues.
            if (m_TickDurationOffset < 0)
            {
                this += m_TickDurationOffset;
            }
        }


        /// <summary>
        /// Converts the network time into a fixed time value.
        /// </summary>
        /// <returns>A <see cref="NetworkTime"/> where Time is the FixedTime value of this instance.</returns>
        public NetworkTime ToFixedTime()
        {
            return new NetworkTime(m_TickRate, m_Tick);
        }

        public static NetworkTime operator -(NetworkTime a, NetworkTime b)
        {
            Assert.AreEqual(a.TickRate, b.TickRate, $"NetworkTimes must have same TickRate to subtract.");

            int tick = a.Tick - b.Tick;
            float tickDuration = a.m_TickDurationOffset - b.m_TickDurationOffset;

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
            float tickDuration = a.m_TickDurationOffset + b.m_TickDurationOffset;

            if (tickDuration >= a.m_TickInterval)
            {
                tick++;
                tickDuration -= a.m_TickInterval;
            }

            return new NetworkTime(a.TickRate, tick, tickDuration);
        }

        public static NetworkTime operator +(NetworkTime a, float b)
        {
            a.m_TickDurationOffset += b;

            // This is quite imprecise for large floating point numbers but there is no easy workaround for this.
            var deltaTicks = Mathf.FloorToInt(a.m_TickDurationOffset * a.m_TickRate);
            a.m_TickDurationOffset -= deltaTicks * a.m_TickInterval;

            a.m_Tick += deltaTicks;

            return a;
        }

        public static NetworkTime operator -(NetworkTime a, float b)
        {
            return a + -b;
        }
    }
}
