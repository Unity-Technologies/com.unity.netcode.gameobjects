using System.Collections.Generic;

namespace Unity.Netcode
{
    internal interface IClientManager
    {
        ulong[] ConnectedClientsIds { get; }
        Dictionary<ulong, NetworkClient> ConnectedClients { get; }
        List<NetworkClient> ConnectedClientsList { get; }
        void DisconnectClient(ulong clientId);
    }
}
