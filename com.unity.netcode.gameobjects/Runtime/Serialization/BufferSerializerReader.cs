using System;

namespace Unity.Netcode
{
    internal struct BufferSerializerReader : IReaderWriter
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

        public void SerializeNetworkSerializable<T>(ref T value) where T : INetworkSerializable, new()
        {
            m_Reader.Value.ReadNetworkSerializable(out value);
        }

        public bool PreCheck(int amount)
        {
            return m_Reader.Value.TryBeginRead(amount);
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
