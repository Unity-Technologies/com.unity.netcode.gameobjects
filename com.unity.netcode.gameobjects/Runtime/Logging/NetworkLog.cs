using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Unity.Netcode.Logging;
using UnityEngine;

namespace Unity.Netcode
{
    internal struct LogConfiguration
    {
        internal bool LogNetworkManagerRole;
    }

    /// <summary>
    /// Helper class for logging
    /// </summary>
    public static class NetworkLog
    {
        internal static LogConfiguration Config;
        private static ContextualLogger s_Log = new(true);

        /// <summary>
        /// Configures the NetworkLog for integration tests.
        /// </summary>
        internal static void ConfigureIntegrationTestLogging(NetworkManager networkManager, bool enableVerboseDebug = false)
        {
            // useCompatibilityMode when verboseDebug is not enabled
            s_Log = new ContextualLogger(networkManager, !enableVerboseDebug);
            // This setting will do nothing if the logger is created with useCompatibilityMode=true
            Config.LogNetworkManagerRole = enableVerboseDebug;
        }

        /// <summary>
        /// Gets the current log level.
        /// </summary>
        /// <value>The current log level.</value>
        // TODO: Work on deprecating this field.
        public static LogLevel CurrentLogLevel => NetworkManager.Singleton == null ? LogLevel.Normal : NetworkManager.Singleton.LogLevel;

        // internal logging

        /// <summary>
        /// Locally logs a info log with Netcode prefixing.
        /// </summary>
        /// <param name="message">The message to log</param>
        [HideInCallstack]
        public static void LogInfo(string message) => s_Log.Info(new Context(LogLevel.Normal, message, true));
        [HideInCallstack]
        internal static void LogInfo(Context context) => s_Log.Info(context);

        /// <summary>
        /// Locally logs a warning log with Netcode prefixing.
        /// </summary>
        /// <param name="message">The message to log</param>
        [HideInCallstack]
        public static void LogWarning(string message) => s_Log.Warning(new Context(LogLevel.Error, message, true));
        [HideInCallstack]
        internal static void LogWarning(Context context) => s_Log.Warning(context);

        /// <summary>
        /// Locally logs a error log with Netcode prefixing.
        /// </summary>
        /// <param name="message">The message to log</param>
        [HideInCallstack]
        public static void LogError(string message) => s_Log.Error(new Context(LogLevel.Error, message, true));
        [HideInCallstack]
        internal static void LogError(Context context) => s_Log.Error(context);

        // internal static void Log(LogLevel level, object message, Object gameObject) => Logger.Log($"[Netcode] {message} ({(int)level})");

        /// <summary>
        /// Logs an info log locally and on the server if possible.
        /// </summary>
        /// <param name="message">The message to log</param>
        [HideInCallstack]
        public static void LogInfoServer(string message) => s_Log.InfoServer(new Context(LogLevel.Normal, message, true));

        /// <summary>
        /// Logs an info log locally and on the session owner if possible.
        /// </summary>
        /// <param name="message">The message to log</param>
        [HideInCallstack]
        public static void LogInfoSessionOwner(string message) => s_Log.InfoServer(new Context(LogLevel.Normal, message, true));

        /// <summary>
        /// Logs a warning log locally and on the server if possible.
        /// </summary>
        /// <param name="message">The message to log</param>
        [HideInCallstack]
        public static void LogWarningServer(string message) => s_Log.WarningServer(new Context(LogLevel.Error, message, true));

        /// <summary>
        /// Logs an error log locally and on the server if possible.
        /// </summary>
        /// <param name="message">The message to log</param>
        [HideInCallstack]
        public static void LogErrorServer(string message) => s_Log.ErrorServer(new Context(LogLevel.Error, message, true));

        internal static LogType GetMessageLogType(UnityEngine.LogType engineLogType)
        {
            return engineLogType switch
            {
                UnityEngine.LogType.Error => LogType.Error,
                UnityEngine.LogType.Warning => LogType.Warning,
                UnityEngine.LogType.Log => LogType.Info,
                _ => LogType.None
            };
        }


        private const string k_SenderId = "SenderId";
        internal static Context BuildContextForServerMessage([NotNull] NetworkManager networkManager, LogLevel level, ulong senderId, string message)
        {
            var ctx = new Context(level, message, true).AddInfo(k_SenderId, senderId);
            if (TryGetNetworkObjectName(networkManager, message, out var name))
            {
                ctx.AddTag(name);
            }
            return ctx;
        }

        internal enum LogType : byte
        {
            Info,
            Warning,
            Error,
            None
        }

        private static readonly Regex k_GlobalObjectIdHash = new($@"\[{nameof(NetworkObject.GlobalObjectIdHash)}=(\d+)\]", RegexOptions.Compiled);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryGetNetworkObjectName([NotNull] NetworkManager networkManager, string message, out string name)
        {
            name = null;
            if (!k_GlobalObjectIdHash.IsMatch(message))
            {
                return false;
            }

            var stringHash = k_GlobalObjectIdHash.Match(message).Groups[1].Value;
            if (!ulong.TryParse(stringHash, out var globalObjectIdHash))
            {
                return false;
            }

            if (!networkManager.SpawnManager.SpawnedObjects.TryGetValue(globalObjectIdHash, out var networkObject))
            {
                return false;
            }

            name = networkObject.name;
            return true;
        }

    }
}
