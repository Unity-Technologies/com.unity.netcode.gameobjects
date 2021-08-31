namespace Unity.Netcode
{
    public interface INetworkMessage
    {
        void Serialize(ref FastBufferWriter writer);
    }
}
