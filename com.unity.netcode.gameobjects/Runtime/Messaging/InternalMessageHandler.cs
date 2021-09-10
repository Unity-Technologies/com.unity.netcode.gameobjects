using System.IO;
using UnityEngine;


namespace Unity.Netcode
{
    internal class InternalMessageHandler : IInternalMessageHandler
    {
        public NetworkManager NetworkManager => m_NetworkManager;
        private NetworkManager m_NetworkManager;

        public InternalMessageHandler(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
        }

        /// <summary>
        /// Called for all Scene Management related events
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="stream"></param>
        public void HandleSceneEvent(ulong clientId, Stream stream)
        {
            NetworkManager.SceneManager.HandleSceneEvent(clientId, stream);
        }

        /// <summary>
        /// Converts the stream to a PerformanceQueueItem and adds it to the receive queue
        /// </summary>
        public void MessageReceiveQueueItem(ulong clientId, Stream stream, float receiveTime, MessageQueueContainer.MessageType messageType)
        {
            if (NetworkManager.IsServer && clientId == NetworkManager.ServerClientId)
            {
                return;
            }

            if (messageType == MessageQueueContainer.MessageType.None)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                {
                    NetworkLog.LogError($"Message header contained an invalid type: {((int)messageType).ToString()}");
                }

                return;
            }

            if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
            {
                NetworkLog.LogInfo($"Data Header: {nameof(messageType)}={((int)messageType).ToString()}");
            }

            if (NetworkManager.PendingClients.TryGetValue(clientId, out PendingClient client) && (client.ConnectionState == PendingClient.State.PendingApproval || client.ConnectionState == PendingClient.State.PendingConnection))
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning($"Message received from {nameof(clientId)}={clientId.ToString()} before it has been accepted");
                }

                return;
            }

            var messageQueueContainer = NetworkManager.MessageQueueContainer;
            messageQueueContainer.AddQueueItemToInboundFrame(messageType, receiveTime, clientId, (NetworkBuffer)stream);
        }

        public void HandleUnnamedMessage(ulong clientId, Stream stream)
        {
            NetworkManager.CustomMessagingManager.InvokeUnnamedMessage(clientId, stream);
        }

        public void HandleNamedMessage(ulong clientId, Stream stream)
        {
            using var reader = PooledNetworkReader.Get(stream);
            ulong hash = reader.ReadUInt64Packed();

            NetworkManager.CustomMessagingManager.InvokeNamedMessage(hash, clientId, stream);
        }
    }
}
