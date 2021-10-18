using UnityEngine;
using System;

namespace Unity.Netcode
{
    /// <summary>
    /// A variable that can be synchronized over the network.
    /// </summary>
    [Serializable]
    public class NetworkVariable<T> : NetworkVariableBase where T : unmanaged
    {
        /// <summary>
        /// Delegate type for value changed event
        /// </summary>
        /// <param name="previousValue">The value before the change</param>
        /// <param name="newValue">The new value</param>
        public delegate void OnValueChangedDelegate(T previousValue, T newValue);
        /// <summary>
        /// The callback to be invoked when the value gets changed
        /// </summary>
        public OnValueChangedDelegate OnValueChanged;

        /// <summary>
        /// Creates a NetworkVariable with the default value and custom read permission
        /// </summary>
        /// <param name="readPerm">The read permission for the NetworkVariable</param>

        public NetworkVariable()
        {
        }

        /// <summary>
        /// Creates a NetworkVariable with the default value and custom read permission
        /// </summary>
        /// <param name="readPerm">The read permission for the NetworkVariable</param>
        public NetworkVariable(NetworkVariableReadPermission readPerm) : base(readPerm)
        {
        }

        /// <summary>
        /// Creates a NetworkVariable with a custom value and custom settings
        /// </summary>
        /// <param name="readPerm">The read permission for the NetworkVariable</param>
        /// <param name="value">The initial value to use for the NetworkVariable</param>
        public NetworkVariable(NetworkVariableReadPermission readPerm, T value) : base(readPerm)
        {
            m_InternalValue = value;
        }

        /// <summary>
        /// Creates a NetworkVariable with a custom value and the default read permission
        /// </summary>
        /// <param name="value">The initial value to use for the NetworkVariable</param>
        public NetworkVariable(T value)
        {
            m_InternalValue = value;
        }

        [SerializeField]
        private protected T m_InternalValue;

        /// <summary>
        /// The value of the NetworkVariable container
        /// </summary>
        public virtual T Value
        {
            get => m_InternalValue;
            set
            {
                // this could be improved. The Networking Manager is not always initialized here
                //  Good place to decouple network manager from the network variable

                // Also, note this is not really very water-tight, if you are running as a host
                //  we cannot tell if a NetworkVariable write is happening inside client-ish code
                if (m_NetworkBehaviour && (m_NetworkBehaviour.NetworkManager.IsClient && !m_NetworkBehaviour.NetworkManager.IsHost))
                {
                    throw new InvalidOperationException("Client can't write to NetworkVariables");
                }
                Set(value);
            }
        }

        private protected void Set(T value)
        {
            m_IsDirty = true;
            T previousValue = m_InternalValue;
            m_InternalValue = value;
            OnValueChanged?.Invoke(previousValue, m_InternalValue);
        }

        /// <summary>
        /// Writes the variable to the writer
        /// </summary>
        /// <param name="writer">The stream to write the value to</param>
        public override void WriteDelta(FastBufferWriter writer)
        {
            WriteField(writer);
        }


        /// <summary>
        /// Reads value from the reader and applies it
        /// </summary>
        /// <param name="reader">The stream to read the value from</param>
        /// <param name="keepDirtyDelta">Whether or not the container should keep the dirty delta, or mark the delta as consumed</param>
        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            T previousValue = m_InternalValue;
            reader.ReadValueSafe(out m_InternalValue);

            if (keepDirtyDelta)
            {
                m_IsDirty = true;
            }

            OnValueChanged?.Invoke(previousValue, m_InternalValue);
        }

        /// <inheritdoc />
        public override void ReadField(FastBufferReader reader)
        {
            reader.ReadValueSafe(out m_InternalValue);
        }

        /// <inheritdoc />
        public override void WriteField(FastBufferWriter writer)
        {
            writer.WriteValueSafe(m_InternalValue);
        }
    }
}
