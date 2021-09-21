using System;
using UnityEngine;

namespace Unity.Netcode
{
    internal struct BufferSerializerReader : IBufferSerializerImplementation
    {
        private Ref<FastBufferReader> m_Reader;

        public BufferSerializerReader(ref FastBufferReader reader)
        {
            m_Reader = new Ref<FastBufferReader>(ref reader);
        }

        public bool IsReader => true;
        public bool IsWriter => false;

        public ref FastBufferReader GetFastBufferReader()
        {
            return ref m_Reader.Value;
        }

        public ref FastBufferWriter GetFastBufferWriter()
        {
            throw new InvalidOperationException("Cannot retrieve a FastBufferWriter from a serializer where IsWriter = false");
        }

        public void SerializeValue(ref object value, Type type, bool isNullable = false)
        {
            m_Reader.Value.ReadObject(out value, type, isNullable);
        }

        public void SerializeValue(ref INetworkSerializable value)
        {
            m_Reader.Value.ReadNetworkSerializable(out value);
        }

        public void SerializeValue(ref GameObject value)
        {
            m_Reader.Value.ReadValueSafe(out value);
        }

        public void SerializeValue(ref NetworkObject value)
        {
            m_Reader.Value.ReadValueSafe(out value);
        }

        public void SerializeValue(ref NetworkBehaviour value)
        {
            m_Reader.Value.ReadValueSafe(out value);
        }

        public void SerializeValue(ref string s, bool oneByteChars = false)
        {
            m_Reader.Value.ReadValueSafe(out s, oneByteChars);
        }

        public void SerializeValue<T>(ref T[] array) where T : unmanaged
        {
            m_Reader.Value.ReadValueSafe(out array);
        }

        public void SerializeValue(ref byte value)
        {
            m_Reader.Value.ReadByteSafe(out value);
        }

        public void SerializeValue<T>(ref T value) where T : unmanaged
        {
            m_Reader.Value.ReadValueSafe(out value);
        }

        public void SerializeNetworkSerializable<T>(ref T value) where T : INetworkSerializable
        {
            m_Reader.Value.ReadNetworkSerializable(out value);
        }

        public bool PreCheck(int amount)
        {
            return m_Reader.Value.TryBeginRead(amount);
        }

        public void SerializeValuePreChecked(ref GameObject value)
        {
            m_Reader.Value.ReadValue(out value);
        }

        public void SerializeValuePreChecked(ref NetworkObject value)
        {
            m_Reader.Value.ReadValue(out value);
        }

        public void SerializeValuePreChecked(ref NetworkBehaviour value)
        {
            m_Reader.Value.ReadValue(out value);
        }

        public void SerializeValuePreChecked(ref string s, bool oneByteChars = false)
        {
            m_Reader.Value.ReadValue(out s, oneByteChars);
        }

        public void SerializeValuePreChecked<T>(ref T[] array) where T : unmanaged
        {
            m_Reader.Value.ReadValue(out array);
        }

        public void SerializeValuePreChecked(ref byte value)
        {
            m_Reader.Value.ReadValue(out value);
        }

        public void SerializeValuePreChecked<T>(ref T value) where T : unmanaged
        {
            m_Reader.Value.ReadValue(out value);
        }
    }
}
