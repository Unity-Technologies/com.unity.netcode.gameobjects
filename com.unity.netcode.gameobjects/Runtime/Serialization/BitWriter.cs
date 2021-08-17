using System;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Multiplayer.Netcode
{
    public ref struct BitWriter
    {
        private unsafe FastBufferWriter.InternalData* m_InternalData;
        private unsafe byte* m_BufferPointer;
        private int m_Position;
        private int m_BitPosition;
        private const int BITS_PER_BYTE = 8;

        /// <summary>
        /// Whether or not the current BitPosition is evenly divisible by 8. I.e. whether or not the BitPosition is at a byte boundary.
        /// </summary>
        public bool BitAligned { get => (m_BitPosition & 7) == 0; }

        internal unsafe BitWriter(ref FastBufferWriter.InternalData internalData)
        {
            fixed (FastBufferWriter.InternalData* internalDataPtr = &internalData)
            {
                m_InternalData = internalDataPtr;
            }

            m_BufferPointer = internalData.BufferPointer + internalData.Position;
            m_Position = internalData.Position;
            m_BitPosition = 0;
        }

        public unsafe void Dispose()
        {
            var bytesWritten = m_BitPosition >> 3;
            if (!BitAligned)
            {
                // Accounting for the partial write
                ++bytesWritten;
            }

            m_InternalData->CommitBitwiseWrites(bytesWritten);
        }

        /// <summary>
        /// Write s certain amount of bits to the stream.
        /// </summary>
        /// <param name="value">Value to get bits from.</param>
        /// <param name="bitCount">Amount of bits to write</param>
        public unsafe void WriteBits(ulong value, int bitCount)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (bitCount > 64)
            {
                throw new ArgumentOutOfRangeException(nameof(bitCount), "Cannot write more than 64 bits from a 64-bit value!");
            }

            if (bitCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bitCount), "Cannot write fewer than 0 bits!");
            }
            
            int checkPos = (m_BitPosition + bitCount) >> 3;
            if (checkPos > m_InternalData->AllowedWriteMark)
            {
                throw new OverflowException("Attempted to write without first calling VerifyCanWrite()");
            }
#endif

            int wholeBytes = bitCount / BITS_PER_BYTE;
            byte* asBytes = (byte*) &value;
            if (BitAligned)
            {
                WritePartialValue(value, wholeBytes);
            }
            else
            {
                for (var i = 0; i < wholeBytes; ++i)
                {
                    WriteMisaligned(asBytes[i]);
                }
            }
            
            for (var count = wholeBytes * BITS_PER_BYTE; count < bitCount; ++count)
            {
                WriteBit((value & (1UL << count)) != 0);
            }
        }
        
        /// <summary>
        /// Write bits to stream.
        /// </summary>
        /// <param name="value">Value to get bits from.</param>
        /// <param name="bitCount">Amount of bits to write.</param>
        public unsafe void WriteBits(byte value, int bitCount)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            int checkPos = (m_BitPosition + bitCount) >> 3;
            if (checkPos >= m_InternalData->AllowedWriteMark)
            {
                throw new OverflowException("Attempted to write without first calling VerifyCanWrite()");
            }
#endif
            
            for (int i = 0; i < bitCount; ++i)
            {
                WriteBit(((value >> i) & 1) != 0);
            }
        }

        /// <summary>
        /// Write a single bit to the buffer
        /// </summary>
        /// <param name="bit">Value of the bit. True represents 1, False represents 0</param>
        public unsafe void WriteBit(bool bit)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            int checkPos = (m_BitPosition + 1) >> 3;
            if (checkPos >= m_InternalData->AllowedWriteMark)
            {
                throw new OverflowException("Attempted to write without first calling VerifyCanWrite()");
            }
#endif
            
            int offset = m_BitPosition & 7;
            int pos = m_BitPosition >> 3;
            ++m_BitPosition;
            m_BufferPointer[pos] = (byte)(bit ? (m_BufferPointer[pos] & ~(1 << offset)) | (1 << offset) : (m_BufferPointer[pos] & ~(1 << offset)));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WritePartialValue<T>(T value, int bytesToWrite, int offsetBytes = 0) where T: unmanaged
        {
            // Switch statement to write small values with assignments
            // is considerably faster than calling UnsafeUtility.MemCpy
            // in all builds - editor, mono, and ILCPP
            
            byte* ptr = ((byte*) &value) + offsetBytes;
            byte* bufferPointer = m_BufferPointer + m_Position;
            switch (bytesToWrite)
            {
                case 1:
                    bufferPointer[0] = *ptr;
                    break;
                case 2:
                    *(ushort*) bufferPointer = *(ushort*)ptr;
                    break;
                case 3:
                    *(ushort*) bufferPointer = *(ushort*)ptr;
                    *(bufferPointer+2) = *(ptr+2);
                    break;
                case 4:
                    *(uint*) bufferPointer = *(uint*)ptr;
                    break;
                case 5:
                    *(uint*) bufferPointer = *(uint*)ptr;
                    *(bufferPointer+4) = *(ptr+4);
                    break;
                case 6:
                    *(uint*) bufferPointer = *(uint*) &ptr;
                    *(ushort*) (bufferPointer+4) = *(ushort*)(ptr+4);
                    break;
                case 7:
                    *(uint*) bufferPointer = *(uint*) &value;
                    *(ushort*) (bufferPointer+4) = *(ushort*)(ptr+4);
                    *(bufferPointer+6) = *(ptr+6);
                    break;
                case 8:
                    *(ulong*) bufferPointer = *(ulong*)ptr;
                    break;
                default:
                    UnsafeUtility.MemCpy(bufferPointer, ptr, bytesToWrite);
                    break;
            }

            m_BitPosition += bytesToWrite * BITS_PER_BYTE;
        }
        
        private unsafe void WriteMisaligned(byte value)
        {
            int off = (int)(m_BitPosition & 7);
            int pos = m_BitPosition >> 3;
            int shift1 = 8 - off;
            m_BufferPointer[pos + 1] = (byte)((m_BufferPointer[pos + 1] & (0xFF << off)) | (value >> shift1));
            m_BufferPointer[pos] = (byte)((m_BufferPointer[pos] & (0xFF >> shift1)) | (value << off));

            m_BitPosition += 8;
        }
    }
}