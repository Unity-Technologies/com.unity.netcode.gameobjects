#if MULTIPLAYER_TOOLS
using Unity.Multiplayer.Tools;
using UnityEngine;

namespace Unity.Netcode
{
    internal class NetworkObjectProvider : INetworkObjectProvider
    {
        private readonly NetworkManager m_NetworkManager;

        public NetworkObjectProvider(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
        }

        public Object GetNetworkObject(ulong networkObjectId)
        {
            if (m_NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject value))
            {
                return value;
            }

            return null;
        }
    }
}
#endif
