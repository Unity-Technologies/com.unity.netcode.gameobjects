namespace Unity.Netcode
{
    public interface INetworkSerializable
    {
        void NetworkSerialize<T>(BufferSerializer<T> serializer) where T: IBufferSerializerImplementation;
    }
}
