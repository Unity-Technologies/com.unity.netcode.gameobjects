using System;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;

namespace Unity.Multiplayer.Netcode
{
    public ref struct BitReader
    {
        private unsafe FastBufferReader.InternalData* m_InternalData;
        private unsafe byte* m_BufferPointer;
        private int m_Position;
        private const int BITS_PER_BYTE = 8;


        internal unsafe BitReader(ref FastBufferReader.InternalData internalData)
        {
            fixed (FastBufferReader.InternalData* internalDataPtr = &internalData)
            {
                m_InternalData = internalDataPtr;
            }

            m_BufferPointer = internalData.BufferPointer + internalData.Position;
            m_Position = internalData.Position;
            m_InternalData->BitPosition = 0;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_InternalData->AllowedBitwiseReadMark = (m_InternalData->AllowedReadMark - m_InternalData->Position) * BITS_PER_BYTE;
#endif
        }

        public unsafe void Dispose()
        {
            var bytesWritten = m_InternalData->BitPosition >> 3;
            if (!m_InternalData->BitAligned())
            {
                // Accounting for the partial read
                ++bytesWritten;
            }

            m_InternalData->CommitBitwiseReads(bytesWritten);
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
            
            int checkPos = (m_InternalData->BitPosition + bitCount);
            if (checkPos > m_InternalData->AllowedBitwiseReadMark)
            {
                throw new OverflowException("Attempted to read without first calling VerifyCanReadBits()");
            }
#endif
            ulong val = 0;

            int wholeBytes = bitCount / BITS_PER_BYTE;
            byte* asBytes = (byte*) &val;
            if (m_InternalData->BitAligned())
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
            int checkPos = (m_InternalData->BitPosition + bitCount);
            if (checkPos > m_InternalData->AllowedBitwiseReadMark)
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
            int checkPos = (m_InternalData->BitPosition + 1);
            if (checkPos > m_InternalData->AllowedBitwiseReadMark)
            {
                throw new OverflowException("Attempted to read without first calling VerifyCanReadBits()");
            }
#endif
            
            int offset = m_InternalData->BitPosition & 7;
            int pos = m_InternalData->BitPosition >> 3;
            bit = (m_BufferPointer[pos] & (1 << offset)) != 0;
            ++m_InternalData->BitPosition;
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

            m_InternalData->BitPosition += bytesToRead * BITS_PER_BYTE;
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
            int off = (int)(m_InternalData->BitPosition & 7);
            int pos = m_InternalData->BitPosition >> 3;
            int shift1 = 8 - off;
            
            value = (byte)((m_BufferPointer[(int)pos] >> shift1) | (m_BufferPointer[(int)(m_InternalData->BitPosition += 8) >> 3] << shift1));
        }
    }
}