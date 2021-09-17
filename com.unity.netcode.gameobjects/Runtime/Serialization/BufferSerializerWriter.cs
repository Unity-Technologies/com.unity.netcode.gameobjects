using System;

namespace Unity.Netcode
{
    internal struct BufferSerializerWriter : IBufferSerializerImplementation
    {
        private Ref<FastBufferWriter> m_Writer;

        public BufferSerializerWriter(ref FastBufferWriter writer)
        {
            m_Writer = new Ref<FastBufferWriter>(ref writer);
        }

        public bool IsReader => false;
        public bool IsWriter => true;

        public ref FastBufferReader GetFastBufferReader()
        {
            throw new InvalidOperationException("Cannot retrieve a FastBufferReader from a serializer where IsReader = false");
        }

        public ref FastBufferWriter GetFastBufferWriter()
        {
            return ref m_Writer.Value;
        }

        public void SerializeValue(ref string s, bool oneByteChars = false)
        {
            m_Writer.Value.WriteValueSafe(s, oneByteChars);
        }

        public void SerializeValue<T>(ref T[] array) where T : unmanaged
        {
            m_Writer.Value.WriteValueSafe(array);
        }

        public void SerializeValue(ref byte value)
        {
            m_Writer.Value.WriteByteSafe(value);
        }

        public void SerializeValue<T>(ref T value) where T : unmanaged
        {
            m_Writer.Value.WriteValueSafe(value);
        }

        public void SerializeNetworkSerializable<T>(ref T value) where T : INetworkSerializable, new()
        {
            m_Writer.Value.WriteNetworkSerializable(value);
        }

        public bool PreCheck(int amount)
        {
            return m_Writer.Value.TryBeginWrite(amount);
        }

        public void SerializeValuePreChecked(ref string s, bool oneByteChars = false)
        {
            m_Writer.Value.WriteValue(s, oneByteChars);
        }

        public void SerializeValuePreChecked<T>(ref T[] array) where T : unmanaged
        {
            m_Writer.Value.WriteValue(array);
        }

        public void SerializeValuePreChecked(ref byte value)
        {
            m_Writer.Value.WriteByte(value);
        }

        public void SerializeValuePreChecked<T>(ref T value) where T : unmanaged
        {
            m_Writer.Value.WriteValue(value);
        }
    }
}
