namespace Unity.Multiplayer.Netcode.Serialization
{
    public interface INetworkSerializable
    {
        void NetworkSerialize(NetworkSerializer serializer);
    }
}
