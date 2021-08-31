namespace Unity.Netcode
{
    public ref struct NetworkContext
    {
        public object SystemOwner;
        public ulong SenderId;
        public NetworkChannel ReceivingChannel;
        public float Timestamp;
        public MessageHeader Header;
    }
}