using MLAPI.Configuration;
using MLAPI.Messaging;
using MLAPI.Security;
using MLAPI.Serialization.Pooled;
using MLAPI.Transports;
using UnityEngine;

namespace MLAPI.Logging
{
    /// <summary>
    /// Helper class for logging
    /// </summary>
    public class NetworkLog
    {
        private NetworkingManager networkingManager;

        internal NetworkLog(NetworkingManager manager)
        {
            networkingManager = manager;
        }

        /// <summary>
        /// Gets the current log level.
        /// </summary>
        /// <value>The current log level.</value>
        internal static LogLevel CurrentLogLevel => NetworkingManager.LogLevel;

        // MLAPI internal logging
        internal static void LogInfo(string message) => Debug.Log($"[MLAPI]" + message);
        internal static void LogWarning(string message) => Debug.LogWarning("[MLAPI] " + message);
        internal static void LogError(string message) => Debug.LogError("[MLAPI] " + message);

        /// <summary>
        /// Logs an info log locally and on the server if possible.
        /// </summary>
        /// <param name="message">The message to log</param>
        public void LogInfoServer(string message) => LogServer(message, LogType.Info);
        /// <summary>
        /// Logs a warning log locally and on the server if possible.
        /// </summary>
        /// <param name="message">The message to log</param>
        public void LogWarningServer(string message) => LogServer(message, LogType.Warning);
        /// <summary>
        /// Logs an error log locally and on the server if possible.
        /// </summary>
        /// <param name="message">The message to log</param>
        public void LogErrorServer(string message) => LogServer(message, LogType.Error);

        private void LogServer(string message, LogType logType)
        {
            // Get the sender of the local log
            ulong localId = networkingManager.LocalClientId;

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

            if ( !networkingManager.IsServer && networkingManager.NetworkConfig.EnableNetworkLogs)
            {
                using (PooledBitStream stream = PooledBitStream.Get())
                {
                    using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                    {
                        writer.WriteByte((byte)logType);

                        writer.WriteStringPacked(message);

                        networkingManager.MessageSender.Send(networkingManager.ServerClientId, MLAPIConstants.MLAPI_SERVER_LOG, Channel.Internal, stream, SecuritySendFlags.None);
                    }
                }
            }
        }

        internal static void LogInfoServerLocal(string message, ulong sender) => Debug.Log("[MLAPI_SERVER Sender=" + sender + "] " + message);
        internal static void LogWarningServerLocal(string message, ulong sender) => Debug.LogWarning("[MLAPI_SERVER Sender=" + sender + "] " + message);
        internal static void LogErrorServerLocal(string message, ulong sender) => Debug.LogError("[MLAPI_SERVER Sender=" + sender + "] " + message);

        internal enum LogType
        {
            Info,
            Warning,
            Error,
            None
        }
    }
}
