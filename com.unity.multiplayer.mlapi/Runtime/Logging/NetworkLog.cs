using MLAPI.Configuration;
using MLAPI.Messaging;
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
        /// <summary>
        /// Gets the current log level.
        /// </summary>
        /// <value>The current log level.</value>
        internal LogLevel CurrentLogLevel => m_NetworkManager != null ? m_NetworkManager.LogLevel : LogLevel.Normal;

        private NetworkManager m_NetworkManager;

        internal void SetNetworkManager(NetworkManager manager)
        {
            m_NetworkManager = manager;
        }

        // MLAPI internal logging
        internal void LogInfo(string message) => Debug.Log($"[MLAPI]{MessageTag} {message}");
        internal void LogWarning(string message) => Debug.LogWarning($"[MLAPI]{MessageTag} {message}");
        internal void LogError(string message) => Debug.LogError($"[MLAPI]{MessageTag} {message}");

        //static internal variants that just tag with [MLAPI]. Always use the nonstatic versions if you can! That way
        //users will see what kind of NetworkManager logged the message. 
        internal static void LogInfoStatic(string message) => Debug.Log($"[MLAPI] {message}");
        internal static void LogWarningStatic(string message) => Debug.Log($"[MLAPI] {message}");
        internal static void LogErrorStatic(string message) => Debug.Log($"[MLAPI] {message}");


        private string MessageTag =>
            m_NetworkManager==null    ? "[Unitialized]" :
            m_NetworkManager.IsHost   ? "[Host]" :
            m_NetworkManager.IsServer ? "[Server]" :
            m_NetworkManager.IsClient ? "[Client]" : "[Unitialized]";

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
            ulong localId = m_NetworkManager != null ? m_NetworkManager.LocalClientId : 0;

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

            if (m_NetworkManager != null && !m_NetworkManager.IsServer && m_NetworkManager.NetworkConfig.EnableNetworkLogs)
            {
                using (var buffer = PooledNetworkBuffer.Get())
                using (var writer = m_NetworkManager.NetworkWriterPool.GetWriter(buffer))
                {
                    writer.WriteByte((byte)logType);
                    writer.WriteStringPacked(message);

                    m_NetworkManager.InternalMessageSender.Send( m_NetworkManager.ServerClientId, NetworkConstants.SERVER_LOG, NetworkChannel.Internal, buffer);
                }
            }
        }

        internal static void LogInfoServerLocal(string message, ulong sender) => Debug.Log($"[MLAPI_SERVER Sender={sender}] {message}");
        internal static void LogWarningServerLocal(string message, ulong sender) => Debug.LogWarning($"[MLAPI_SERVER Sender={sender}] {message}");
        internal static void LogErrorServerLocal(string message, ulong sender) => Debug.LogError($"[MLAPI_SERVER Sender={sender}] {message}");

        internal enum LogType
        {
            Info,
            Warning,
            Error,
            None
        }
    }
}
