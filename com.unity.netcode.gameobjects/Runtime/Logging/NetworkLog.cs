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
        private static readonly ContextualLogger k_Log =  new(true);

        internal static void SetNetworkManager(NetworkManager networkManager)
        {
            k_Log.UpdateNetworkManagerContext(networkManager);
        }
        /// <summary>
        /// Gets the current log level.
        /// </summary>
        /// <value>The current log level.</value>
        // [Obsolete("Use the LogLevel directly on the NetworkManager instead")]
        public static LogLevel CurrentLogLevel => NetworkManager.Singleton == null ? LogLevel.Normal : NetworkManager.Singleton.LogLevel;

        internal static LogConfiguration Config = new LogConfiguration();

        // internal logging

        /// <summary>
        /// Locally logs a info log with Netcode prefixing.
        /// </summary>
        /// <param name="message">The message to log</param>
        [HideInCallstack]
        public static void LogInfo(string message, [CallerMemberName] string memberName = "") => k_Log.Info(new Context(LogLevel.Normal, message, memberName));
        [HideInCallstack]
        internal static void LogInfo(Context context) => k_Log.Info(context);

        /// <summary>
        /// Locally logs a warning log with Netcode prefixing.
        /// </summary>
        /// <param name="message">The message to log</param>
        [HideInCallstack]
        public static void LogWarning(string message, [CallerMemberName] string memberName = "") => k_Log.Warning(new Context(LogLevel.Error, message, memberName));
        [HideInCallstack]
        internal static void LogWarning(Context context) => k_Log.Warning(context);

        /// <summary>
        /// Locally logs a error log with Netcode prefixing.
        /// </summary>
        /// <param name="message">The message to log</param>
        [HideInCallstack]
        public static void LogError(string message, [CallerMemberName] string memberName = "") => k_Log.Error(new Context(LogLevel.Error, message, memberName));
        [HideInCallstack]
        internal static void LogError(Context context) => k_Log.Error(context);

        // internal static void Log(LogLevel level, object message, Object gameObject) => Logger.Log($"[Netcode] {message} ({(int)level})");

        /// <summary>
        /// Logs an info log locally and on the server if possible.
        /// </summary>
        /// <param name="message">The message to log</param>
        [HideInCallstack]
        public static void LogInfoServer(string message, [CallerMemberName] string memberName = "") => k_Log.InfoServer(new Context(LogLevel.Normal, message, memberName));

        /// <summary>
        /// Logs an info log locally and on the session owner if possible.
        /// </summary>
        /// <param name="message">The message to log</param>
        [HideInCallstack]
        public static void LogInfoSessionOwner(string message, [CallerMemberName] string memberName = "") => k_Log.InfoServer(new Context(LogLevel.Normal, message, memberName));

        /// <summary>
        /// Logs a warning log locally and on the server if possible.
        /// </summary>
        /// <param name="message">The message to log</param>
        [HideInCallstack]
        public static void LogWarningServer(string message, [CallerMemberName] string memberName = "") => k_Log.WarningServer(new Context(LogLevel.Error, message, memberName));

        /// <summary>
        /// Logs an error log locally and on the server if possible.
        /// </summary>
        /// <param name="message">The message to log</param>
        [HideInCallstack]
        public static void LogErrorServer(string message, [CallerMemberName] string memberName = "") => k_Log.ErrorServer(new Context(LogLevel.Error, message, memberName));

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

        internal static void SendLogToAuthority(NetworkManager networkManager, LogType logType, ulong senderId, string message)
        {
            var networkMessage = new ServerLogMessage
            {
                LogType = logType,
                Message = message,
                SenderId = senderId
            };
            var size = networkManager.ConnectionManager.SendMessage(ref networkMessage, MessageDeliveryType<ServerLogMessage>.DefaultDelivery, NetworkManager.ServerClientId);
            networkManager.NetworkMetrics.TrackServerLogSent(NetworkManager.ServerClientId, (uint)logType, size);
        }

        private const string k_SenderId = "SenderId";
        internal static Context ContextWithSenderId([NotNull] NetworkManager networkManager, LogLevel level, ulong senderId, string message)
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
