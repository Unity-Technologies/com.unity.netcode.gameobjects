namespace Unity.Netcode.Serialization
{
    public interface INetworkSerializable
    {
        void NetworkSerialize(NetworkSerializer serializer);
    }
}
