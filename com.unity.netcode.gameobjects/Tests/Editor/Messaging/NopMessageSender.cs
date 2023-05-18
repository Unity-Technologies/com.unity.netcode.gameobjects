namespace Unity.Netcode.GameObjects.EditorTests
{
    internal class NopMessageSender : INetworkMessageSender
    {
        public void Send(ulong clientId, NetworkDelivery delivery, FastBufferWriter batchData)
        {
        }
    }
}
