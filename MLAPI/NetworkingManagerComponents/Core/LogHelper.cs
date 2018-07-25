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
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    internal static class LogHelper
    {
        internal static LogLevel CurrentLogLevel
        {
            get
            {
                if (NetworkingManager.singleton == null)
                    return LogLevel.Normal;
                else
                    return NetworkingManager.singleton.LogLevel;
            }
        }

        internal static void LogInfo(string message) => Debug.Log("[MLAPI] " + message);
        internal static void LogWarning(string message) => Debug.LogWarning("[MLAPI] " + message);
        internal static void LogError(string message) => Debug.LogError("[MLAPI] " + message);
    }
}
