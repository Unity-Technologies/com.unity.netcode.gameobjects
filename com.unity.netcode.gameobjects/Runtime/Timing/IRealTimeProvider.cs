namespace Unity.Netcode
{
    public interface IRealTimeProvider
    {
        public float RealTimeSinceStartup { get; }
        public float UnscaledTime { get; }
        public float UnscaledDeltaTime { get; }
        public float DeltaTime { get; }
    }
}
