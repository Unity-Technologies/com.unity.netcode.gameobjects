namespace Unity.Netcode
{
    internal interface INetworkMessageSender
    {
        public void Send(ulong clientId, NetworkDelivery delivery, FastBufferWriter batchData);
    }
}
