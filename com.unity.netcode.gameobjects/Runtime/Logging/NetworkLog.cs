using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Helper class for logging
    /// </summary>
    public static class NetworkLog
    {
        /// <summary>
        /// Gets the current log level.
        /// </summary>
        /// <value>The current log level.</value>
        public static LogLevel CurrentLogLevel => NetworkManager.Singleton == null ? LogLevel.Normal : NetworkManager.Singleton.LogLevel;

        // internal logging
        internal static void LogInfo(string message) => Debug.Log($"[Netcode] {message}");
        internal static void LogWarning(string message) => Debug.LogWarning($"[Netcode] {message}");
        internal static void LogError(string message) => Debug.LogError($"[Netcode] {message}");

        /// <summary>
        /// Logs an info log locally and on the server if possible.
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void LogInfoServer(string message) => LogServer(message, LogType.Info);

        /// <summary>
        /// Logs a warning log locally and on the server if possible.
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void LogWarningServer(string message) => LogServer(message, LogType.Warning);

        /// <summary>
        /// Logs an error log locally and on the server if possible.
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void LogErrorServer(string message) => LogServer(message, LogType.Error);

        private static void LogServer(string message, LogType logType)
        {
            // Get the sender of the local log
            ulong localId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;

            switch (logType)
            {
                case LogType.Info:
                    LogInfoServerLocal(message, localId);
                    break;
                case LogType.Warning:
                    LogWarningServerLocal(message, localId);
                    break;
                case LogType.Error:
                    LogErrorServerLocal(message, localId);
                    break;
            }

            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer && NetworkManager.Singleton.NetworkConfig.EnableNetworkLogs)
            {

                var networkMessage = new ServerLogMessage
                {
                    LogType = logType,
                    Message = message
                };
                var size = NetworkManager.Singleton.SendMessage(networkMessage, NetworkDelivery.ReliableFragmentedSequenced,
                    NetworkManager.Singleton.ServerClientId);

                NetworkManager.Singleton.NetworkMetrics.TrackServerLogSent(NetworkManager.Singleton.ServerClientId, (uint)logType, size);
            }
        }

        internal static void LogInfoServerLocal(string message, ulong sender) => Debug.Log($"[Netcode-Server Sender={sender}] {message}");
        internal static void LogWarningServerLocal(string message, ulong sender) => Debug.LogWarning($"[Netcode-Server Sender={sender}] {message}");
        internal static void LogErrorServerLocal(string message, ulong sender) => Debug.LogError($"[Netcode-Server Sender={sender}] {message}");

        internal enum LogType : byte
        {
            Info,
            Warning,
            Error,
            None
        }
    }
}
