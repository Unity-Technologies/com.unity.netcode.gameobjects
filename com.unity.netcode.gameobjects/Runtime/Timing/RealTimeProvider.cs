using UnityEngine;

namespace Unity.Netcode
{
    public class RealTimeProvider : IRealTimeProvider
    {
        public float RealTimeSinceStartup => Time.realtimeSinceStartup;
        public float UnscaledTime => Time.unscaledTime;
        public float UnscaledDeltaTime => Time.unscaledDeltaTime;
    }
}
