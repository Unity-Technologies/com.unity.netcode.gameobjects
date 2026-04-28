using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Unity.Netcode.Logging
{
    internal struct LogContextNetworkManager : ILogContext, IDisposable
    {
        private NetworkManager m_NetworkManager;
        private const string k_ServerString = "[Server]";
        private const string k_SessionOwnerString = "[Session Owner]";
        // This will get appended with the current clientID, doesn't need brackets
        private const string k_ClientString = "Client";

        public LogContextNetworkManager(bool useSingleton)
        {
            m_NetworkManager = null;
            if (!useSingleton)
            {
                return;
            }

            WatchForSingleton();
        }

        public LogContextNetworkManager([NotNull] NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
        }

        public readonly LogLevel LogLevel => m_NetworkManager?.LogLevel ?? LogLevel.Normal;

        public readonly void TrySendMessage(LogType logType, string message)
        {
            if (m_NetworkManager != null
                && m_NetworkManager.IsListening
                && (m_NetworkManager?.NetworkConfig.EnableNetworkLogs ?? false)
                && !m_NetworkManager.IsServer && !m_NetworkManager.LocalClient.IsSessionOwner)
            {
                var messageType = NetworkLog.GetMessageLogType(logType);

                var networkMessage = new ServerLogMessage
                {
                    LogType = messageType,
                    Message = message,
                    SenderId = m_NetworkManager.LocalClientId
                };
                var size = m_NetworkManager.ConnectionManager.SendMessage(ref networkMessage, MessageDeliveryType<ServerLogMessage>.DefaultDelivery, NetworkManager.ServerClientId);
                m_NetworkManager.NetworkMetrics.TrackServerLogSent(NetworkManager.ServerClientId, (uint)logType, size);
            }
        }

        private void WatchForSingleton()
        {
            if (NetworkManager.Singleton != null)
            {
                m_NetworkManager = NetworkManager.Singleton;
                NetworkManager.OnDestroying += OnManagerDestroying;
            }
            else
            {
                NetworkManager.OnSingletonReady += OnSingletonReady;
            }
        }

        private void OnSingletonReady()
        {
            m_NetworkManager = NetworkManager.Singleton;
            NetworkManager.OnSingletonReady -= OnSingletonReady;
        }

        private void OnManagerDestroying(NetworkManager manager)
        {
            if (m_NetworkManager != manager)
            {
                return;
            }
            m_NetworkManager = null;
            WatchForSingleton();
        }

        public readonly void AppendTo(LogBuilder builder)
        {
            if (m_NetworkManager == null)
            {
                return;
            }

            if (!m_NetworkManager.IsListening || !NetworkLog.Config.LogNetworkManagerRole)
            {
                return;
            }

            if (m_NetworkManager.LocalClient.IsSessionOwner)
            {
                // [Session Owner]
                builder.Append(k_SessionOwnerString);
            }
            else if (m_NetworkManager.IsServer)
            {
                // [Server]
                builder.Append(k_ServerString);
            }
            if (!m_NetworkManager.IsServer)
            {
                // [Client:1]
                builder.AppendInfo(k_ClientString, m_NetworkManager.LocalClientId);
            }
        }

        public void Dispose()
        {
            NetworkManager.OnDestroying -= OnManagerDestroying;
            NetworkManager.OnSingletonReady -= OnSingletonReady;
            m_NetworkManager = null;
        }
    }

}
