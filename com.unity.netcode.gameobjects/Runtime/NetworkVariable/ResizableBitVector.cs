using System;
using Unity.Collections;

namespace Unity.Netcode
{
    internal struct ResizableBitVector : INetworkSerializable, IDisposable
    {
        private NativeList<byte> m_Bits;
        private const int k_Divisor = sizeof(byte) * 8;

        public ResizableBitVector(Allocator allocator)
        {
            m_Bits = new NativeList<byte>(allocator);
        }

        public void Dispose()
        {
            m_Bits.Dispose();
        }

        public int GetSerializedSize()
        {
            return FastBufferWriter.GetWriteSize(m_Bits);
        }

        public (int, int) GetBitData(int i)
        {
            var index = i / k_Divisor;
            var bitWithinIndex = i % k_Divisor;
            return (index, bitWithinIndex);
        }

        public void Set(int i)
        {
            var (index, bitWithinIndex) = GetBitData(i);
            if (index >= m_Bits.Length)
            {
                m_Bits.Resize(index + 1, NativeArrayOptions.ClearMemory);
            }

            m_Bits[index] |= (byte)(1 << bitWithinIndex);
        }

        public void Unset(int i)
        {
            var (index, bitWithinIndex) = GetBitData(i);
            if (index >= m_Bits.Length)
            {
                return;
            }

            m_Bits[index] &= (byte)~(1 << bitWithinIndex);
        }

        public bool IsSet(int i)
        {
            var (index, bitWithinIndex) = GetBitData(i);
            if (index >= m_Bits.Length)
            {
                return false;
            }

            return (m_Bits[index] & (byte)(1 << bitWithinIndex)) != 0;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref m_Bits);
        }
    }
}
