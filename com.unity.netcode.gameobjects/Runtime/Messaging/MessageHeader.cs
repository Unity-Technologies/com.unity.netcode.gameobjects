namespace Unity.Netcode
{
    public struct MessageHeader
    {
        public byte MessageType;
        public NetworkChannel NetworkChannel;
        public short MessageSize;
    }
}
