namespace Unity.Netcode.GameObjects.Timing
{
    internal interface IRealTimeProvider
    {
        public float RealTimeSinceStartup { get; }
        public float UnscaledTime { get; }
        public float UnscaledDeltaTime { get; }
        public float DeltaTime { get; }
        public float FixedDeltaTime { get; }
    }
}
