namespace Unity.Netcode.TestHelpers.Runtime
{
    public class MockTimeProvider : IRealTimeProvider
    {
        public float RealTimeSinceStartup => (float)s_doubleRealTime;
        public float UnscaledTime => (float)s_doubleRealTime;
        public float UnscaledDeltaTime => (float)s_doubleDelta;


        public static float StaticRealTimeSinceStartup => (float)s_doubleRealTime;
        public static float StaticUnscaledTime => (float)s_doubleRealTime;
        public static float StaticUnscaledDeltaTime => (float)s_doubleDelta;

        private static double s_doubleRealTime = 0;
        private static double s_doubleDelta = 0;

        public static void TimeTravel(double amountOfTimeTraveled)
        {
            s_doubleDelta = amountOfTimeTraveled;
            s_doubleRealTime += amountOfTimeTraveled;
        }

        public static void Reset()
        {
            s_doubleDelta = 0;
            s_doubleRealTime = 0;
        }
    }
}
