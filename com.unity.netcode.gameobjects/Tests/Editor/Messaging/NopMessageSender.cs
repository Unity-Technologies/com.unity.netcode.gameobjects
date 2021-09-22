namespace Unity.Netcode.EditorTests
{
    internal class NopFastBufferMessageSender : IFastBufferMessageSender
    {
        public void Send(ulong clientId, NetworkDelivery delivery, FastBufferWriter batchData)
        {
        }
    }
}
