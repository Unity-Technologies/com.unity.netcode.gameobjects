namespace Unity.Netcode
{
    internal interface IMessageSender
    {
        void Send(ulong clientId, NetworkDelivery delivery, FastBufferWriter batchData);
    }
}
