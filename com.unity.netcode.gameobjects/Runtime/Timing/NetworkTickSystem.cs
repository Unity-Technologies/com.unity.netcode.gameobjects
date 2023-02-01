using System;
using Unity.Profiling;

namespace Unity.Netcode
{
    /// <summary>
    /// Provides discretized time.
    /// This is useful for games that require ticks happening at regular interval on the server and clients.
    /// </summary>
    public class NetworkTickSystem
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private static ProfilerMarker s_Tick = new ProfilerMarker($"{nameof(NetworkTickSystem)}.Tick");
#endif

        /// <summary>
        /// Special value to indicate "No tick information"
        /// </summary>
        public const int NoTick = int.MinValue;

        /// <summary>
        /// The TickRate of the tick system. This is used to decide how often a fixed network tick is run.
        /// </summary>
        public int TickRate { get; protected set; }

        /// <summary>
        /// Gets invoked before every network tick.
        /// </summary>
        public event Action Tick;

        /// <summary>
        /// Creates a new instance of the <see cref="NetworkTickSystem"/> class.
        /// </summary>
        /// <param name="tickRate">The tick rate</param>
        public NetworkTickSystem(int tickRate)
        {
            if (tickRate == 0)
            {
                throw new ArgumentException("Tickrate must be a positive value.", nameof(tickRate));
            }

            TickRate = tickRate;
            Tick = null;
        }

        /// <summary>
        /// Resets the tick system to the given network time.
        /// </summary>
        /// <param name="localTimeSec">The local time in seconds.</param>
        /// <param name="serverTimeSec">The server time in seconds.</param>
        public void Reset()
        {
            FractionTick = 0.0;
            CurrentTick = 0;
        }

        /// <summary>
        /// Called after advancing the time system to run ticks based on the difference in time.
        /// </summary>
        /// <param name="timeElapsed">Amount of time passed since last call to this</param>
        public void UpdateTick(double timeElapsed)
        {
            FractionTick += timeElapsed;
            while (FractionTick >= 1.0 / TickRate)
            {
                FractionTick -= 1.0 / TickRate;
                CurrentTick++;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                s_Tick.Begin();
#endif
                Tick?.Invoke();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                s_Tick.End();
#endif
            }
        }

        public int CurrentTick { get; protected set; }
        public double FractionTick { get; protected set; }

        public double CurrentTime()
        {
            return (double)CurrentTick / TickRate + FractionTick;
        }

    }
}
