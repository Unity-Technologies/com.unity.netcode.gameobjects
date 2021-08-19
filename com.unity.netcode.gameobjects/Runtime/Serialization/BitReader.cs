using System;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;

namespace Unity.Multiplayer.Netcode
{
    public ref struct BitReader
    {
        private Ref<FastBufferReader> m_Reader;
        private unsafe byte* m_BufferPointer;
        private int m_Position;
        private const int BITS_PER_BYTE = 8;
        private int m_BitPosition;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private int m_AllowedBitwiseReadMark;
#endif

        /// <summary>
        /// Whether or not the current BitPosition is evenly divisible by 8. I.e. whether or not the BitPosition is at a byte boundary.
        /// </summary>
        public bool BitAligned
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (m_BitPosition & 7) == 0;
        }

        internal unsafe BitReader(ref FastBufferReader reader)
        {
            m_Reader = new Ref<FastBufferReader>(ref reader);

            m_BufferPointer = m_Reader.Value.m_BufferPointer + m_Reader.Value.Position;
            m_Position = m_Reader.Value.Position;
            m_BitPosition = 0;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_AllowedBitwiseReadMark = (m_Reader.Value.m_AllowedReadMark - m_Position) * BITS_PER_BYTE;
#endif
        }

        public unsafe void Dispose()
        {
            var bytesWritten = m_BitPosition >> 3;
            if (!BitAligned)
            {
                // Accounting for the partial read
                ++bytesWritten;
            }

            m_Reader.Value.CommitBitwiseReads(bytesWritten);
        }

        public unsafe bool VerifyCanReadBits(int bitCount)
        {
            var newBitPosition = m_BitPosition + bitCount;
            var totalBytesWrittenInBitwiseContext = newBitPosition >> 3;
            if ((newBitPosition & 7) != 0)
            {
                // Accounting for the partial read
                ++totalBytesWrittenInBitwiseContext;
            }

            if (m_Reader.Value.m_Position + totalBytesWrittenInBitwiseContext > m_Reader.Value.m_Length)
            {
                return false;
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_AllowedBitwiseReadMark = newBitPosition;
#endif
            return true;
        }

        /// <summary>
        /// Read a certain amount of bits from the stream.
        /// </summary>
        /// <param name="value">Value to store bits into.</param>
        /// <param name="bitCount">Amount of bits to read</param>
        public unsafe void ReadBits(out ulong value, int bitCount)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (bitCount > 64)
            {
                throw new ArgumentOutOfRangeException(nameof(bitCount), "Cannot read more than 64 bits from a 64-bit value!");
            }

            if (bitCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bitCount), "Cannot read fewer than 0 bits!");
            }
            
            int checkPos = (m_BitPosition + bitCount);
            if (checkPos > m_AllowedBitwiseReadMark)
            {
                throw new OverflowException("Attempted to read without first calling VerifyCanReadBits()");
            }
#endif
            ulong val = 0;

            int wholeBytes = bitCount / BITS_PER_BYTE;
            byte* asBytes = (byte*) &val;
            if (BitAligned)
            {
                if (wholeBytes != 0)
                {
                    ReadPartialValue(out val, wholeBytes);
                }
            }
            else
            {
                for (var i = 0; i < wholeBytes; ++i)
                {
                    ReadMisaligned(out asBytes[i]);
                }
            }
            
            val |= (ulong)ReadByteBits(bitCount & 7) << (bitCount & ~7);
            value = val;
        }
        
        /// <summary>
        /// Read bits from stream.
        /// </summary>
        /// <param name="value">Value to store bits into.</param>
        /// <param name="bitCount">Amount of bits to read.</param>
        public unsafe void ReadBits(out byte value, int bitCount)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            int checkPos = (m_BitPosition + bitCount);
            if (checkPos > m_AllowedBitwiseReadMark)
            {
                throw new OverflowException("Attempted to read without first calling VerifyCanReadBits()");
            }
#endif
            value = ReadByteBits(bitCount);
        }

        /// <summary>
        /// Read a single bit from the buffer
        /// </summary>
        /// <param name="bit">Out value of the bit. True represents 1, False represents 0</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadBit(out bool bit)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            int checkPos = (m_BitPosition + 1);
            if (checkPos > m_AllowedBitwiseReadMark)
            {
                throw new OverflowException("Attempted to read without first calling VerifyCanReadBits()");
            }
#endif
            
            int offset = m_BitPosition & 7;
            int pos = m_BitPosition >> 3;
            bit = (m_BufferPointer[pos] & (1 << offset)) != 0;
            ++m_BitPosition;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadPartialValue<T>(out T value, int bytesToRead, int offsetBytes = 0) where T: unmanaged
        {
            // Switch statement to read small values with assignments
            // is considerably faster than calling UnsafeUtility.MemCpy
            // in all builds - editor, mono, and ILCPP
            T val = new T();
            byte* ptr = ((byte*) &val) + offsetBytes;
            byte* bufferPointer = m_BufferPointer + m_Position;
            switch (bytesToRead)
            {
                case 1:
                    ptr[0] = *bufferPointer;
                    break;
                case 2:
                    *(ushort*) ptr = *(ushort*)bufferPointer;
                    break;
                case 3:
                    *(ushort*) ptr = *(ushort*)bufferPointer;
                    *(ptr+2) = *(bufferPointer+2);
                    break;
                case 4:
                    *(uint*) ptr = *(uint*)bufferPointer;
                    break;
                case 5:
                    *(uint*) ptr = *(uint*)bufferPointer;
                    *(ptr+4) = *(bufferPointer+4);
                    break;
                case 6:
                    *(uint*) ptr = *(uint*)bufferPointer;
                    *(ushort*) (ptr+4) = *(ushort*)(bufferPointer+4);
                    break;
                case 7:
                    *(uint*) ptr = *(uint*)ptr;
                    *(ushort*) (ptr+4) = *(ushort*)(bufferPointer+4);
                    *(ptr+6) = *(bufferPointer+6);
                    break;
                case 8:
                    *(ulong*) ptr = *(ulong*)bufferPointer;
                    break;
                default:
                    UnsafeUtility.MemCpy(ptr, bufferPointer, bytesToRead);
                    break;
            }

            m_BitPosition += bytesToRead * BITS_PER_BYTE;
            value = val;
        }
        
        /// <summary>
        /// Read a certain amount of bits from the stream.
        /// </summary>
        /// <param name="bitCount">How many bits to read. Minimum 0, maximum 64.</param>
        /// <returns>The bits that were read</returns>
        private byte ReadByteBits(int bitCount)
        {
            if (bitCount > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(bitCount), "Cannot read more than 8 bits into an 8-bit value!");
            }

            if (bitCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bitCount), "Cannot read fewer than 0 bits!");
            }

            int result = 0;
            var convert = new ByteBool();
            for (int i = 0; i < bitCount; ++i)
            {
                ReadBit(out bool bit);
                result |= convert.Collapse(bit) << i;
            }

            return (byte)result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void ReadMisaligned(out byte value)
        {
            int off = (int)(m_BitPosition & 7);
            int pos = m_BitPosition >> 3;
            int shift1 = 8 - off;
            
            value = (byte)((m_BufferPointer[(int)pos] >> shift1) | (m_BufferPointer[(int)(m_BitPosition += 8) >> 3] << shift1));
        }
    }
}