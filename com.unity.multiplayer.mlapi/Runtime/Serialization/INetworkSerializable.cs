namespace MLAPI.Serialization
{
    public interface INetworkSerializable
    {
        void NetworkRead(BitReader reader);
        void NetworkWrite(BitWriter writer);
    }
}
