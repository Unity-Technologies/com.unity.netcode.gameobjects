namespace Unity.Netcode
{
    internal interface INetworkMessageSender
    {
        void Send(ulong clientId, NetworkDelivery delivery, FastBufferWriter batchData);
    }
}
