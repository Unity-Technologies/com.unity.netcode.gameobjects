using UnityEngine;
using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Netcode
{
    /// <summary>
    /// A variable that can be synchronized over the network.
    /// </summary>
    [Serializable]
    public class NetworkVariable<T> : NetworkVariableBase where T : unmanaged
    {
        // Functions that know how to serialize INetworkSerializable
        internal static void WriteNetworkSerializable<TForMethod>(FastBufferWriter writer, in TForMethod value)
            where TForMethod : INetworkSerializable, new()
        {
            writer.WriteNetworkSerializable(value);
        }
        internal static void ReadNetworkSerializable<TForMethod>(FastBufferReader reader, out TForMethod value)
            where TForMethod : INetworkSerializable, new()
        {
            reader.ReadNetworkSerializable(out value);
        }

        // Functions that serialize other types
        private static void WriteValue<TForMethod>(FastBufferWriter writer, in TForMethod value)
            where TForMethod : unmanaged
        {
            writer.WriteValueSafe(value);
        }

        private static void ReadValue<TForMethod>(FastBufferReader reader, out TForMethod value)
            where TForMethod : unmanaged
        {
            reader.ReadValueSafe(out value);
        }

        internal delegate void WriteDelegate<TForMethod>(FastBufferWriter writer, in TForMethod value);

        internal delegate void ReadDelegate<TForMethod>(FastBufferReader reader, out TForMethod value);

        // These static delegates provide the right implementation for writing and reading a particular network variable type.
        // For most types, these default to WriteValue() and ReadValue(), which perform simple memcpy operations.
        //
        // INetworkSerializableILPP will generate startup code that will set it to WriteNetworkSerializable()
        // and ReadNetworkSerializable() for INetworkSerializable types, which will call NetworkSerialize().
        //
        // In the future we may be able to use this to provide packing implementations for floats and integers to optimize bandwidth usage.
        //
        // The reason this is done is to avoid runtime reflection and boxing in NetworkVariable - without this,
        // NetworkVariable would need to do a `var is INetworkSerializable` check, and then cast to INetworkSerializable,
        // *both* of which would cause a boxing allocation. Alternatively, NetworkVariable could have been split into
        // NetworkVariable and NetworkSerializableVariable or something like that, which would have caused a poor
        // user experience and an API that's easier to get wrong than right. This is a bit ugly on the implementation
        // side, but it gets the best achievable user experience and performance.
        internal static WriteDelegate<T> Write = WriteValue;
        internal static ReadDelegate<T> Read = ReadValue;


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


        public NetworkVariable(T value = default,
            NetworkVariableReadPermission readPerm = DefaultReadPerm,
            NetworkVariableWritePermission writePerm = DefaultWritePerm)
            : base(readPerm, writePerm)
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
                // Compare bitwise
                if (ValueEquals(ref m_InternalValue, ref value))
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

        // Compares two values of the same unmanaged type by underlying memory
        // Ignoring any overriden value checks
        // Size is fixed
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool ValueEquals(ref T a, ref T b)
        {
            // get unmanaged pointers
            var aptr = UnsafeUtility.AddressOf(ref a);
            var bptr = UnsafeUtility.AddressOf(ref b);

            // compare addresses
            return UnsafeUtility.MemCmp(aptr, bptr, sizeof(T)) == 0;
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
            Read(reader, out m_InternalValue);

            if (keepDirtyDelta)
            {
                m_IsDirty = true;
            }

            OnValueChanged?.Invoke(previousValue, m_InternalValue);
        }

        /// <inheritdoc />
        public override void ReadField(FastBufferReader reader)
        {
            Read(reader, out m_InternalValue);
        }

        /// <inheritdoc />
        public override void WriteField(FastBufferWriter writer)
        {
            Write(writer, m_InternalValue);
        }
    }
}
