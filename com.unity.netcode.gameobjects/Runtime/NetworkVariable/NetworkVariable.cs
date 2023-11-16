using System;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// A variable that can be synchronized over the network.
    /// </summary>
    /// <typeparam name="T">the unmanaged type for <see cref="NetworkVariable{T}"/> </typeparam>
    [Serializable]
    [GenerateSerializationForGenericParameter(0)]
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
            // Since we start with IsDirty = true, this doesn't need to be duplicated
            // right away. It won't get read until after ResetDirty() is called, and
            // the duplicate will be made there. Avoiding calling
            // NetworkVariableSerialization<T>.Duplicate() is important because calling
            // it in the constructor might not give users enough time to set the
            // DuplicateValue callback if they're using UserNetworkVariableSerialization
            m_PreviousValue = default;
        }

        /// <summary>
        /// The internal value of the NetworkVariable
        /// </summary>
        [SerializeField]
        private protected T m_InternalValue;

        private protected T m_PreviousValue;

        private bool m_HasPreviousValue;
        private bool m_IsDisposed;

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
                m_IsDisposed = false;
            }
        }

        internal ref T RefValue()
        {
            return ref m_InternalValue;
        }

        public override void Dispose()
        {
            if (m_IsDisposed)
            {
                return;
            }

            m_IsDisposed = true;
            if (m_InternalValue is IDisposable internalValueDisposable)
            {
                internalValueDisposable.Dispose();
            }

            m_InternalValue = default;
            if (m_HasPreviousValue && m_PreviousValue is IDisposable previousValueDisposable)
            {
                m_HasPreviousValue = false;
                previousValueDisposable.Dispose();
            }

            m_PreviousValue = default;
        }

        ~NetworkVariable()
        {
            Dispose();
        }

        /// <summary>
        /// Gets Whether or not the container is dirty
        /// </summary>
        /// <returns>Whether or not the container is dirty</returns>
        public override bool IsDirty()
        {
            // For most cases we can use the dirty flag.
            // This doesn't work for cases where we're wrapping more complex types
            // like INetworkSerializable, NativeList, NativeArray, etc.
            // Changes to the values in those types don't call the Value.set method,
            // so we can't catch those changes and need to compare the current value
            // against the previous one.
            if (base.IsDirty())
            {
                return true;
            }

            // Cache the dirty value so we don't perform this again if we already know we're dirty
            // Unfortunately we can't cache the NOT dirty state, because that might change
            // in between to checks... but the DIRTY state won't change until ResetDirty()
            // is called.
            var dirty = !NetworkVariableSerialization<T>.AreEqual(ref m_PreviousValue, ref m_InternalValue);
            SetDirty(dirty);
            return dirty;
        }

        /// <summary>
        /// Resets the dirty state and marks the variable as synced / clean
        /// </summary>
        public override void ResetDirty()
        {
            base.ResetDirty();
            // Resetting the dirty value declares that the current value is not dirty
            // Therefore, we set the m_PreviousValue field to a duplicate of the current
            // field, so that our next dirty check is made against the current "not dirty"
            // value.
            if (!m_HasPreviousValue || !NetworkVariableSerialization<T>.AreEqual(ref m_InternalValue, ref m_PreviousValue))
            {
                m_HasPreviousValue = true;
                NetworkVariableSerialization<T>.Duplicate(m_InternalValue, ref m_PreviousValue);
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
