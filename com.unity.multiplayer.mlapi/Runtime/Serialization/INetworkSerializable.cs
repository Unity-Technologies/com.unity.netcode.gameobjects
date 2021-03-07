namespace MLAPI.Serialization
{
    public interface INetworkSerializable
    {
        void NetworkSerialize(NetworkSerializer serializer);
    }
}