namespace MLAPI.Serialization
{
    public interface INetworkSerializable
    {
        void NetworkSerialize(BitSerializer serializer);
    }
}