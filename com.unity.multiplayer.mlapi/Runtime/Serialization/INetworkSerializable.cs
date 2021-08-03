namespace Unity.Multiplayer.Netcode
{
    public interface INetworkSerializable
    {
        void NetworkSerialize(NetworkSerializer serializer);
    }
}
