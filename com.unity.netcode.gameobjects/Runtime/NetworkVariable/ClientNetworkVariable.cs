using System;

namespace Unity.Netcode
{
    /// <summary>
    /// A ClientNetworkVariable is special in that:
    ///  - only the owner of the variable can write to it
    ///  - not even the server can write to it
    ///  - it is not snapshotted
    ///  - it must be sent reliably
    ///
    /// (This class may be removed in the future when integrated into NetworkVariable natively)
    /// </summary>
    [Serializable]
    public class ClientNetworkVariable<T> : NetworkVariable<T> where T : unmanaged
    {
        /// <summary>
        /// Creates a ClientNetworkVariable with the default read permission
        /// </summary>
        public ClientNetworkVariable()
        {
        }

        /// <summary>
        /// Creates a ClientNetworkVariable with a initial value
        /// </summary>
        /// <param name="value">The initial value to use for the ClientNetworkVariable</param>
        public ClientNetworkVariable(T value) : base(value)
        {
        }

        /// <summary>
        /// Creates a ClientNetworkVariable with a specified read permission
        /// </summary>
        /// <param name="readPerm">The readPermission to use</param>
        public ClientNetworkVariable(NetworkVariableReadPermission readPerm) : base(readPerm)
        {
        }

        /// <summary>
        /// Creates a ClientNetworkVariable with a initial value and the specified read permission
        /// </summary>
        /// <param name="readPerm">The initial read permission to use for the ClientNetworkVariable</param>
        /// <param name="value">The initial value to use for the ClientNetworkVariable</param>
        public ClientNetworkVariable(NetworkVariableReadPermission readPerm, T value) : base(readPerm, value)
        {
        }

        public override bool CanClientWrite(ulong clientId)
        {
            return m_NetworkBehaviour.OwnerClientId == clientId;
        }

        public override bool ShouldWrite(ulong clientId, bool isServer)
        {
            return m_IsDirty && !isServer && m_NetworkBehaviour.IsOwner;
        }

        /// <summary>
        /// The value of the ClientNetworkVariable container
        /// </summary>
        public override T Value
        {
            get => m_InternalValue;
            set
            {
                // this could be improved. The Networking Manager is not always initialized here
                //  Good place to decouple network manager from the network variable

                // Also, note this is not really very water-tight, if you are running as a host
                //  we cannot tell if a ClientNetworkVariable write is happening inside server-ish code
                if (m_NetworkBehaviour && m_NetworkBehaviour.NetworkManager.IsServer)
                {
                    throw new InvalidOperationException("Server not allowed to write to ClientNetworkVariables");
                }
                Set(value);
            }
        }
    }
}
