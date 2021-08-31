using System;

namespace Unity.Netcode
{
    public class NetworkManagerMessageSender : IMessageSender
    {
        private NetworkManager m_NetworkManager;

        public NetworkManagerMessageSender(NetworkManager manager)
        {
            m_NetworkManager = manager;
        }
        
        public void Send(ulong clientId, NetworkDelivery delivery, ref FastBufferWriter batchData)
        {
            
            var length = batchData.Length;
            //TODO: Transport needs to have a way to send it data without copying and allocating here.
            var bytes = batchData.ToArray();
            var sendBuffer = new ArraySegment<byte>(bytes, 0, length);

            //TODO: Transport needs to accept sends by NetworkDelivery instead of NetworkChannel
            NetworkChannel channel;
            switch (delivery)
            {
                case NetworkDelivery.Reliable:
                    channel = NetworkChannel.DefaultMessage;
                    break;
                case NetworkDelivery.Unreliable:
                    channel = NetworkChannel.SnapshotExchange;
                    break;
                case NetworkDelivery.ReliableSequenced:
                    channel = NetworkChannel.Internal;
                    break;
                case NetworkDelivery.UnreliableSequenced:
                    channel = NetworkChannel.UnreliableRpc;
                    break;
                case NetworkDelivery.ReliableFragmentedSequenced:
                    channel = NetworkChannel.Fragmented;
                    break;
                default:
                    channel = NetworkChannel.DefaultMessage;
                    break;
            }
            m_NetworkManager.NetworkConfig.NetworkTransport.Send(clientId, sendBuffer, channel);

        }
    }
}