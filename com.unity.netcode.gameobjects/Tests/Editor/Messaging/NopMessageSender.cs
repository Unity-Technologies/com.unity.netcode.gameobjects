namespace Unity.Netcode.EditorTests
{
    class NopMessageSender : IMessageSender
    {
        public void Send(ulong clientId, NetworkDelivery delivery, ref FastBufferWriter batchData)
        {
        }
    }
}