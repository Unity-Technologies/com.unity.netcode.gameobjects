using MLAPI.MonoBehaviours.Core;
using UnityEngine;

namespace MLAPI.NetworkingManagerComponents.Core
{
    public enum LogLevel
    {
        Developer,
        Normal,
        Nothing
    }

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

        internal static void LogInfo(string message, LogLevel level)
        {
            if (CurrentLogLevel <= level && CurrentLogLevel != LogLevel.Nothing)
                Debug.Log(message);
        }

        internal static void LogWarning(string message, LogLevel level)
        {
            if (CurrentLogLevel <= level && CurrentLogLevel != LogLevel.Nothing)
                Debug.LogWarning(message);
        }

        internal static void LogError(string message, LogLevel level)
        {
            if (CurrentLogLevel <= level && CurrentLogLevel != LogLevel.Nothing)
                Debug.LogError(message);
        }
    }
}
