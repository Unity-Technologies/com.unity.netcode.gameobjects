using UnityEngine;

namespace MLAPI.Logging
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public enum LogLevel
    {
        Developer,
        Normal,
        Error,
        Nothing
    }

    public static class LogHelper
    {
        public static LogLevel CurrentLogLevel
        {
            get
            {
                if (NetworkingManager.singleton == null)
                    return LogLevel.Normal;
                else
                    return NetworkingManager.singleton.LogLevel;
            }
        }

        public static void LogInfo(string message) => Debug.Log("[MLAPI] " + message);
        public static void LogWarning(string message) => Debug.LogWarning("[MLAPI] " + message);
        public static void LogError(string message) => Debug.LogError("[MLAPI] " + message);
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
