using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// A Client NetworkVariable is special in that
    ///  - only the owner of the variable can write to it
    ///  - not even the server can write to it
    ///  - it is not snapshotted
    ///
    /// This class may be removed in the future
    /// </summary>
    [Serializable]
    public class ClientNetworkVariable<T> : NetworkVariable<T> where T : unmanaged
    {
        public ClientNetworkVariable() { }

        public ClientNetworkVariable(NetworkVariableSettings settings) : base(settings) { }

        public override bool CanClientWrite(ulong clientId)
        {
            return m_NetworkBehaviour.OwnerClientId == clientId;
        }

        public override bool ShouldWrite(ulong clientId, bool isServer)
        {
            return m_IsDirty && !isServer && m_NetworkBehaviour.IsOwner;
        }

        /// <summary>
        /// The value of the NetworkVariable container
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
                if (m_NetworkBehaviour.NetworkManager.IsServer)
                {
                    throw new InvalidOperationException("Server not allowed to write to ClientNetworkVariables");
                }
                Set(value);
            }
        }
    }
}
