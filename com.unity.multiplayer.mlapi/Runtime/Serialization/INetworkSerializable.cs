namespace Unity.Netcode
{
    public interface INetworkSerializable
    {
        void NetworkSerialize(NetworkSerializer serializer);
    }
}
