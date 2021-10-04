#if MULTIPLAYER_TOOLS
using Unity.Multiplayer.Tools;

namespace Unity.Netcode
{
    class ClientIdProvider : IClientIdProvider
    {
        private readonly NetworkManager m_NetworkManager;

        public ClientIdProvider(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
        }

        public ulong GetConnectionId()
        {
            return m_NetworkManager.LocalClientId;
        }
    }
}
#endif