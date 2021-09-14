namespace Unity.Netcode
{
    public interface IMessageSender
    {
        void Send(ulong clientId, NetworkDelivery delivery, ref FastBufferWriter batchData);
    }
}
