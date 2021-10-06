using System;

namespace Unity.Netcode
{
    internal struct BufferSerializerWriter : IReaderWriter
    {
        private FastBufferWriter m_Writer;

        public BufferSerializerWriter(FastBufferWriter writer)
        {
            m_Writer = writer;
        }

        public bool IsReader => false;
        public bool IsWriter => true;

        public FastBufferReader GetFastBufferReader()
        {
            throw new InvalidOperationException("Cannot retrieve a FastBufferReader from a serializer where IsReader = false");
        }

        public FastBufferWriter GetFastBufferWriter()
        {
            return m_Writer;
        }

        public void SerializeValue(ref string s, bool oneByteChars = false)
        {
            m_Writer.WriteValueSafe(s, oneByteChars);
        }

        public void SerializeValue<T>(ref T[] array) where T : unmanaged
        {
            m_Writer.WriteValueSafe(array);
        }

        public void SerializeValue(ref byte value)
        {
            m_Writer.WriteByteSafe(value);
        }

        public void SerializeValue<T>(ref T value) where T : unmanaged
        {
            m_Writer.WriteValueSafe(value);
        }

        public void SerializeNetworkSerializable<T>(ref T value) where T : INetworkSerializable, new()
        {
            m_Writer.WriteNetworkSerializable(value);
        }

        public bool PreCheck(int amount)
        {
            return m_Writer.TryBeginWrite(amount);
        }

        public void SerializeValuePreChecked(ref string s, bool oneByteChars = false)
        {
            m_Writer.WriteValue(s, oneByteChars);
        }

        public void SerializeValuePreChecked<T>(ref T[] array) where T : unmanaged
        {
            m_Writer.WriteValue(array);
        }

        public void SerializeValuePreChecked(ref byte value)
        {
            m_Writer.WriteByte(value);
        }

        public void SerializeValuePreChecked<T>(ref T value) where T : unmanaged
        {
            m_Writer.WriteValue(value);
        }
    }
}
