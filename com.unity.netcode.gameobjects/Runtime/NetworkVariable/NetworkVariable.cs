using UnityEngine;
using System;

namespace Unity.Netcode
{
    /// <summary>
    /// A variable that can be synchronized over the network.
    /// </summary>
    /// <typeparam name="T">the unmanaged type for <see cref="NetworkVariable{T}"/> </typeparam>
    [Serializable]
    public class NetworkVariable<T> : NetworkVariableBase
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
        /// Constructor for <see cref="NetworkVariable{T}"/>
        /// </summary>
        /// <param name="value">initial value set that is of type T</param>
        /// <param name="readPerm">the <see cref="NetworkVariableReadPermission"/> for this <see cref="NetworkVariable{T}"/></param>
        /// <param name="writePerm">the <see cref="NetworkVariableWritePermission"/> for this <see cref="NetworkVariable{T}"/></param>
        public NetworkVariable(T value = default,
            NetworkVariableReadPermission readPerm = DefaultReadPerm,
            NetworkVariableWritePermission writePerm = DefaultWritePerm)
            : base(readPerm, writePerm)
        {
            m_InternalValue = value;
        }

        /// <summary>
        /// The internal value of the NetworkVariable
        /// </summary>
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
                // Compare bitwise
                if (NetworkVariableSerialization<T>.AreEqual(ref m_InternalValue, ref value))
                {
                    return;
                }

                if (m_NetworkBehaviour && !CanClientWrite(m_NetworkBehaviour.NetworkManager.LocalClientId))
                {
                    throw new InvalidOperationException("Client is not allowed to write to this NetworkVariable");
                }

                Set(value);
            }
        }

        /// <summary>
        /// Sets the <see cref="Value"/>, marks the <see cref="NetworkVariable{T}"/> dirty, and invokes the <see cref="OnValueChanged"/> callback
        /// if there are subscribers to that event.
        /// </summary>
        /// <param name="value">the new value of type `T` to be set/></param>
        private protected void Set(T value)
        {
            SetDirty(true);
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
            // todo:
            // keepDirtyDelta marks a variable received as dirty and causes the server to send the value to clients
            // In a prefect world, whether a variable was A) modified locally or B) received and needs retransmit
            // would be stored in different fields

            T previousValue = m_InternalValue;
            NetworkVariableSerialization<T>.Read(reader, ref m_InternalValue);

            if (keepDirtyDelta)
            {
                SetDirty(true);
            }

            OnValueChanged?.Invoke(previousValue, m_InternalValue);
        }

        /// <inheritdoc />
        public override void ReadField(FastBufferReader reader)
        {
            NetworkVariableSerialization<T>.Read(reader, ref m_InternalValue);
        }

        /// <inheritdoc />
        public override void WriteField(FastBufferWriter writer)
        {
            NetworkVariableSerialization<T>.Write(writer, ref m_InternalValue);
        }
    }
}
