namespace Unity.Netcode.EditorTests
{
    internal class NopMessageSender : INetworkMessageSender
    {
        public void Send(ulong clientId, NetworkDelivery delivery, FastBufferWriter batchData)
        {
        }
    }
}
