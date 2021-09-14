namespace Unity.Netcode
{
    public ref struct NetworkContext
    {
        public object SystemOwner;
        public ulong SenderId;
        public float Timestamp;
        public MessageHeader Header;
    }
}
