namespace Unity.Netcode.TestHelpers.Runtime
{
    /// <summary>
    /// A mock timer that implements the <see cref="IRealTimeProvider"/> interface used with integration testing.
    /// </summary>
    public class MockTimeProvider : IRealTimeProvider
    {
        /// <summary>
        /// Returns realtime since start up.
        /// </summary>
        public float RealTimeSinceStartup => (float)s_DoubleRealTime;

        /// <summary>
        /// Returns the unscaled time.
        /// </summary>
        public float UnscaledTime => (float)s_DoubleRealTime;

        /// <summary>
        /// Returns the unscaled delta time.
        /// </summary>
        public float UnscaledDeltaTime => (float)s_DoubleDelta;

        /// <summary>
        /// Returns the delta time.
        /// </summary>
        public float DeltaTime => (float)s_DoubleDelta;

        // DANGO-EXP TODO: Figure out how we want to handle time travel with fixed delta time.
        /// <summary>
        /// Returns the fixed delta time.
        /// </summary>
        public float FixedDeltaTime => (float)s_DoubleDelta;

        /// <summary>
        /// Static version of the <see cref="RealTimeSinceStartup"/>.
        /// </summary>
        public static float StaticRealTimeSinceStartup => (float)s_DoubleRealTime;

        /// <summary>
        /// Static version of the <see cref="UnscaledTime"/>.
        /// </summary>
        public static float StaticUnscaledTime => (float)s_DoubleRealTime;

        /// <summary>
        /// Static version of the <see cref="UnscaledDeltaTime"/>.
        /// </summary>
        public static float StaticUnscaledDeltaTime => (float)s_DoubleDelta;

        /// <summary>
        /// Static version of the <see cref="DeltaTime"/>.
        /// </summary>
        public static float StaticDeltaTime => (float)s_DoubleDelta;

        private static double s_DoubleRealTime = 0;
        private static double s_DoubleDelta = 0;

        /// <summary>
        /// Invoke to force this time provider to time travel to a specific point in time.
        /// </summary>
        /// <param name="amountOfTimeTraveled">Amount of time to travel.</param>
        public static void TimeTravel(double amountOfTimeTraveled)
        {
            s_DoubleDelta = amountOfTimeTraveled;
            s_DoubleRealTime += amountOfTimeTraveled;
        }

        /// <summary>
        /// Resets the delta and real time values.
        /// </summary>
        public static void Reset()
        {
            s_DoubleDelta = 0;
            s_DoubleRealTime = 0;
        }
    }
}
