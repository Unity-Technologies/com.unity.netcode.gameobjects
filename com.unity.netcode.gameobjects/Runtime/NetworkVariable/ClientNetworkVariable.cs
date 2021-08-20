using System;

namespace Unity.Netcode
{
    /// <summary>
    /// A Client NetworkVariable is special in that
    ///  - only the owner of the variable can write to it
    ///  - not even the server can write to it
    ///  - it is not snapshotted
    /// </summary>
    [Serializable]
    public class ClientNetworkVariable<T> : NetworkVariable<T> where T : unmanaged
    {
        public override bool CanClientWrite(ulong clientId)
        {
            return m_NetworkBehaviour.OwnerClientId == clientId;
        }

        public override bool ShouldWrite(ulong clientId, bool isServer)
        {
            return m_IsDirty && !isServer && CanClientRead(clientId) && m_NetworkBehaviour.IsOwner;
        }
    }
}
