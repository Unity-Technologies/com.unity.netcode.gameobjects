using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

namespace Unity.Netcode
{
    /// <summary>
    /// A variable that can be synchronized over the network.
    /// </summary>
    [Serializable]
    public class ClientNetworkVariable<T> : NetworkVariable<T> where T : unmanaged
    {
        /// <summary>
        /// The value of the ClientNetworkVariable container
        /// </summary>
        public new T Value
        {
            get => m_InternalValue;
            set
            {
                if (EqualityComparer<T>.Default.Equals(m_InternalValue, value))
                {
                    return;
                }

                m_IsDirty = true;
                T previousValue = m_InternalValue;
                m_InternalValue = value;
                OnValueChanged?.Invoke(previousValue, m_InternalValue);
            }
        }

        /// <inheritdoc />
        public bool CanClientRead(ulong clientId)
        {
            switch (Settings.ReadPermission)
            {
                case NetworkVariableReadPermission.Everyone:
                    return true;
                case NetworkVariableReadPermission.OwnerOnly:
                    return m_NetworkBehaviour.OwnerClientId == clientId;
            }
            return true;
        }

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
