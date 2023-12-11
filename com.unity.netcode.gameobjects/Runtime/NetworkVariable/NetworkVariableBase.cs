using System;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Interface for network value containers
    /// </summary>
    public abstract class NetworkVariableBase : IDisposable
    {
        /// <summary>
        /// The delivery type (QoS) to send data with
        /// </summary>
        internal const NetworkDelivery Delivery = NetworkDelivery.ReliableFragmentedSequenced;

        /// <summary>
        /// Maintains a link to the associated NetworkBehaviour
        /// </summary>
        private protected NetworkBehaviour m_NetworkBehaviour;

        public NetworkBehaviour GetBehaviour()
        {
            return m_NetworkBehaviour;
        }

        /// <summary>
        /// Initializes the NetworkVariable
        /// </summary>
        /// <param name="networkBehaviour">The NetworkBehaviour the NetworkVariable belongs to</param>
        public void Initialize(NetworkBehaviour networkBehaviour)
        {
            m_NetworkBehaviour = networkBehaviour;
        }

        /// <summary>
        /// The default read permissions
        /// </summary>
        public const NetworkVariableReadPermission DefaultReadPerm = NetworkVariableReadPermission.Everyone;

        /// <summary>
        /// The default write permissions
        /// </summary>
        public const NetworkVariableWritePermission DefaultWritePerm = NetworkVariableWritePermission.Server;

        /// <summary>
        /// The default constructor for <see cref="NetworkVariableBase"/> that can be used to create a
        /// custom NetworkVariable.
        /// </summary>
        /// <param name="readPerm">the <see cref="NetworkVariableReadPermission"/> access settings</param>
        /// <param name="writePerm">the <see cref="NetworkVariableWritePermission"/> access settings</param>
        protected NetworkVariableBase(
            NetworkVariableReadPermission readPerm = DefaultReadPerm,
            NetworkVariableWritePermission writePerm = DefaultWritePerm)
        {
            ReadPerm = readPerm;
            WritePerm = writePerm;
        }

        /// <summary>
        /// The <see cref="m_IsDirty"/> property is used to determine if the
        /// value of the `NetworkVariable` has changed.
        /// </summary>
        private bool m_IsDirty;

        /// <summary>
        /// Gets or sets the name of the network variable's instance
        /// (MemberInfo) where it was declared.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// The read permission for this var
        /// </summary>
        public readonly NetworkVariableReadPermission ReadPerm;

        /// <summary>
        /// The write permission for this var
        /// </summary>
        public readonly NetworkVariableWritePermission WritePerm;

        /// <summary>
        /// Sets whether or not the variable needs to be delta synced
        /// </summary>
        /// <param name="isDirty">Whether or not the var is dirty</param>
        public virtual void SetDirty(bool isDirty)
        {
            m_IsDirty = isDirty;

            if (m_IsDirty)
            {
                MarkNetworkBehaviourDirty();
            }
        }

        protected void MarkNetworkBehaviourDirty()
        {
            if (m_NetworkBehaviour == null)
            {
                Debug.LogWarning($"NetworkVariable is written to, but doesn't know its NetworkBehaviour yet. " +
                                 "Are you modifying a NetworkVariable before the NetworkObject is spawned?");
                return;
            }
            if (m_NetworkBehaviour.NetworkManager.ShutdownInProgress)
            {
                if (m_NetworkBehaviour.NetworkManager.LogLevel <= LogLevel.Developer)
                {
                    Debug.LogWarning($"NetworkVariable is written to during the NetworkManager shutdown! " +
                 "Are you modifying a NetworkVariable within a NetworkBehaviour.OnDestroy or NetworkBehaviour.OnDespawn method?");
                }
                return;
            }
            m_NetworkBehaviour.NetworkManager.BehaviourUpdater.AddForUpdate(m_NetworkBehaviour.NetworkObject);
        }

        /// <summary>
        /// Resets the dirty state and marks the variable as synced / clean
        /// </summary>
        public virtual void ResetDirty()
        {
            m_IsDirty = false;
        }

        /// <summary>
        /// Gets Whether or not the container is dirty
        /// </summary>
        /// <returns>Whether or not the container is dirty</returns>
        public virtual bool IsDirty()
        {
            return m_IsDirty;
        }

        /// <summary>
        /// Gets if a specific client has permission to read the var or not
        /// </summary>
        /// <param name="clientId">The client id</param>
        /// <returns>Whether or not the client has permission to read</returns>
        public bool CanClientRead(ulong clientId)
        {
            switch (ReadPerm)
            {
                default:
                case NetworkVariableReadPermission.Everyone:
                    return true;
                case NetworkVariableReadPermission.Owner:
                    return clientId == m_NetworkBehaviour.NetworkObject.OwnerClientId || NetworkManager.ServerClientId == clientId;
            }
        }

        /// <summary>
        /// Gets if a specific client has permission to write the var or not
        /// </summary>
        /// <param name="clientId">The client id</param>
        /// <returns>Whether or not the client has permission to write</returns>
        public bool CanClientWrite(ulong clientId)
        {
            switch (WritePerm)
            {
                default:
                case NetworkVariableWritePermission.Server:
                    return clientId == NetworkManager.ServerClientId;
                case NetworkVariableWritePermission.Owner:
                    return clientId == m_NetworkBehaviour.NetworkObject.OwnerClientId;
            }
        }

        /// <summary>
        /// Returns the ClientId of the owning client
        /// </summary>
        internal ulong OwnerClientId()
        {
            return m_NetworkBehaviour.NetworkObject.OwnerClientId;
        }

        /// <summary>
        /// Writes the dirty changes, that is, the changes since the variable was last dirty, to the writer
        /// </summary>
        /// <param name="writer">The stream to write the dirty changes to</param>
        public abstract void WriteDelta(FastBufferWriter writer);

        /// <summary>
        /// Writes the complete state of the variable to the writer
        /// </summary>
        /// <param name="writer">The stream to write the state to</param>
        public abstract void WriteField(FastBufferWriter writer);

        /// <summary>
        /// Reads the complete state from the reader and applies it
        /// </summary>
        /// <param name="reader">The stream to read the state from</param>
        public abstract void ReadField(FastBufferReader reader);

        /// <summary>
        /// Reads delta from the reader and applies them to the internal value
        /// </summary>
        /// <param name="reader">The stream to read the delta from</param>
        /// <param name="keepDirtyDelta">Whether or not the delta should be kept as dirty or consumed</param>
        public abstract void ReadDelta(FastBufferReader reader, bool keepDirtyDelta);

        /// <summary>
        /// Virtual <see cref="IDisposable"/> implementation
        /// </summary>
        public virtual void Dispose()
        {
        }
    }
}
