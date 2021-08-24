using System.IO;

namespace Unity.Netcode
{
    /// <summary>
    /// Interface for network value containers
    /// </summary>
    public abstract class NetworkVariableBase
    {
        /// <summary>
        /// The name of the channel to be used for syncing
        /// </summary>
        public const NetworkChannel NetworkVariableChannel = NetworkChannel.NetworkVariable;

        protected NetworkVariableBase() { }

        protected NetworkVariableBase(NetworkVariableSettings settings)
        {
            Settings = settings;
        }

        // demolish me
        // or better setter?
        private protected NetworkBehaviour m_NetworkBehaviour;

        private protected bool m_IsDirty = false;

        /// <summary>
        /// Gets or sets the name of the network variable's instance
        /// (MemberInfo) where it was declared.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// The settings for this var
        /// </summary>
        public readonly NetworkVariableSettings Settings = new NetworkVariableSettings();

        /// <summary>
        /// Sets whether or not the variable needs to be delta synced
        /// </summary>
        public virtual void SetDirty(bool isDirty)
        {
            m_IsDirty = isDirty;
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

        public virtual bool ShouldWrite(ulong clientId, bool isServer)
        {
            return IsDirty() && isServer && CanClientRead(clientId);
        }

        /// <summary>
        /// Gets Whether or not a specific client can read to the varaible
        /// </summary>
        /// <param name="clientId">The clientId of the remote client</param>
        /// <returns>Whether or not the client can read to the variable</returns>
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

        /// <summary>
        /// Gets Whether or not a specific client can read to the varaible
        /// </summary>
        /// <param name="clientId">The clientId of the remote client</param>
        /// <returns>Whether or not the client can read to the variable</returns>
        public virtual bool CanClientWrite(ulong clientId)
        {
            return false;
        }

        /// <summary>
        /// Writes the dirty changes, that is, the changes since the variable was last dirty, to the writer
        /// </summary>
        /// <param name="stream">The stream to write the dirty changes to</param>
        public abstract void WriteDelta(Stream stream);

        /// <summary>
        /// Writes the complete state of the variable to the writer
        /// </summary>
        /// <param name="stream">The stream to write the state to</param>
        public abstract void WriteField(Stream stream);

        /// <summary>
        /// Reads the complete state from the reader and applies it
        /// </summary>
        /// <param name="stream">The stream to read the state from</param>
        /// <param name="localTick">The local network tick at which this var was written, on the machine it was written </param>
        /// <param name="remoteTick">The remote network tick at which this var was sent by the host </param>
        public abstract void ReadField(Stream stream);

        /// <summary>
        /// Reads delta from the reader and applies them to the internal value
        /// </summary>
        /// <param name="stream">The stream to read the delta from</param>
        /// <param name="keepDirtyDelta">Whether or not the delta should be kept as dirty or consumed</param>
        /// <param name="localTick">The local network tick at which this var was written, on the machine it was written </param>
        /// <param name="remoteTick">The remote network tick at which this var was sent by the host </param>
        public abstract void ReadDelta(Stream stream, bool keepDirtyDelta);

        /// <summary>
        /// Sets NetworkBehaviour the container belongs to.
        /// </summary>
        /// <param name="behaviour">The behaviour the container behaves to</param>
        public void SetNetworkBehaviour(NetworkBehaviour behaviour)
        {
            m_NetworkBehaviour = behaviour;
        }

    }
}
