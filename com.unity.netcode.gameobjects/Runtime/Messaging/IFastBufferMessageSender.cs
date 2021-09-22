namespace Unity.Netcode
{
    internal interface IFastBufferMessageSender
    {
        void Send(ulong clientId, NetworkDelivery delivery, FastBufferWriter batchData);
    }
}
