using UnityEngine.Scripting.APIUpdating;

namespace Unity.Netcode
{
    [MovedFrom("MLAPI.Serialization")]
    public interface INetworkSerializable
    {
        void NetworkSerialize(NetworkSerializer serializer);
    }
}
