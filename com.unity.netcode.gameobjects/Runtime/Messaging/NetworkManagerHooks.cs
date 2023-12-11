using System;
using Unity.Netcode.Transports.UTP;

namespace Unity.Netcode
{
    internal class NetworkManagerHooks : INetworkHooks
    {
        private NetworkManager m_NetworkManager;

        internal NetworkManagerHooks(NetworkManager manager)
        {
            m_NetworkManager = manager;
        }

        public void OnBeforeSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery) where T : INetworkMessage
        {
        }

        public void OnAfterSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery, int messageSizeBytes) where T : INetworkMessage
        {
        }

        public void OnBeforeReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
        {
        }

        public void OnAfterReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
        {
        }

        public void OnBeforeSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery)
        {
        }

        public void OnAfterSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery)
        {
        }

        public void OnBeforeReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes)
        {
        }

        public void OnAfterReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes)
        {
        }

        public bool OnVerifyCanSend(ulong destinationId, Type messageType, NetworkDelivery delivery)
        {
            return !m_NetworkManager.MessageManager.StopProcessing;
        }

        public bool OnVerifyCanReceive(ulong senderId, Type messageType, FastBufferReader messageContent, ref NetworkContext context)
        {
            if (m_NetworkManager.IsServer)
            {
                if (messageType == typeof(ConnectionApprovedMessage))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        var transportErrorMsg = GetTransportErrorMessage(messageContent, m_NetworkManager);
                        NetworkLog.LogError($"A {nameof(ConnectionApprovedMessage)} was received from a client on the server side. {transportErrorMsg}");
                    }

                    return false;
                }

                if (m_NetworkManager.ConnectionManager.PendingClients.TryGetValue(senderId, out PendingClient client) && (client.ConnectionState == PendingClient.State.PendingApproval || (client.ConnectionState == PendingClient.State.PendingConnection && messageType != typeof(ConnectionRequestMessage))))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"Message received from {nameof(senderId)}={senderId} before it has been accepted.");
                    }

                    return false;
                }

                if (m_NetworkManager.ConnectedClients.TryGetValue(senderId, out NetworkClient connectedClient) && messageType == typeof(ConnectionRequestMessage))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        var transportErrorMsg = GetTransportErrorMessage(messageContent, m_NetworkManager);
                        NetworkLog.LogError($"A {nameof(ConnectionRequestMessage)} was received from a client when the connection has already been established. {transportErrorMsg}");
                    }

                    return false;
                }
            }
            else
            {
                if (messageType == typeof(ConnectionRequestMessage))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        var transportErrorMsg = GetTransportErrorMessage(messageContent, m_NetworkManager);
                        NetworkLog.LogError($"A {nameof(ConnectionRequestMessage)} was received from the server on the client side. {transportErrorMsg}");
                    }

                    return false;
                }

                if (m_NetworkManager.IsConnectedClient && messageType == typeof(ConnectionApprovedMessage))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        var transportErrorMsg = GetTransportErrorMessage(messageContent, m_NetworkManager);
                        NetworkLog.LogError($"A {nameof(ConnectionApprovedMessage)} was received from the server when the connection has already been established. {transportErrorMsg}");
                    }

                    return false;
                }
            }

            return !m_NetworkManager.MessageManager.StopProcessing;
        }

        private static string GetTransportErrorMessage(FastBufferReader messageContent, NetworkManager networkManager)
        {
            if (networkManager.NetworkConfig.NetworkTransport is not UnityTransport)
            {
                return $"NetworkTransport: {networkManager.NetworkConfig.NetworkTransport.GetType()}. Please report this to the maintainer of transport layer.";
            }

            var transportVersion = GetTransportVersion(networkManager);
            return $"{transportVersion}. This should not happen. Please report this to the Netcode for GameObjects team at https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues and include the following data: Message Size: {messageContent.Length}. Message Content: {NetworkMessageManager.ByteArrayToString(messageContent.ToArray(), 0, messageContent.Length)}";
        }

        private static string GetTransportVersion(NetworkManager networkManager)
        {
            var transportVersion = "NetworkTransport: " + networkManager.NetworkConfig.NetworkTransport.GetType();
            if (networkManager.NetworkConfig.NetworkTransport is UnityTransport unityTransport)
            {
                transportVersion += " UnityTransportProtocol: " + unityTransport.Protocol;
            }

            return transportVersion;
        }

        public void OnBeforeHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
        {
        }

        public void OnAfterHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
        {
        }
    }
}
