using System;
using System.Runtime.CompilerServices;

namespace Unity.Multiplayer.Netcode
{
    /// <summary>
    /// Helper class for doing bitwise writes for a FastBufferWriter.
    /// Ensures all bitwise writes end on proper byte alignment so FastBufferWriter doesn't have to be concerned
    /// with misaligned writes.
    /// </summary>
    public ref struct BitWriter
    {
        private Ref<FastBufferWriter> m_Writer;
        private unsafe byte* m_BufferPointer;
        private readonly int m_Position;
        private int m_BitPosition;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private int m_AllowedBitwiseWriteMark;
#endif
        private const int k_BitsPerByte = 8;

        /// <summary>
        /// Whether or not the current BitPosition is evenly divisible by 8. I.e. whether or not the BitPosition is at a byte boundary.
        /// </summary>
        public bool BitAligned
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (m_BitPosition & 7) == 0;
        }

        internal unsafe BitWriter(ref FastBufferWriter writer)
        {
            m_Writer = new Ref<FastBufferWriter>(ref writer);
            m_BufferPointer = writer.BufferPointer + writer.PositionInternal;
            m_Position = writer.PositionInternal;
            m_BitPosition = 0;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_AllowedBitwiseWriteMark = (m_Writer.Value.AllowedWriteMark - m_Writer.Value.Position) * k_BitsPerByte;
#endif
        }

        /// <summary>
        /// Pads the written bit count to byte alignment and commits the write back to the writer
        /// </summary>
        public void Dispose()
        {
            var bytesWritten = m_BitPosition >> 3;
            if (!BitAligned)
            {
                // Accounting for the partial write
                ++bytesWritten;
            }

            m_Writer.Value.CommitBitwiseWrites(bytesWritten);
        }

        /// <summary>
        /// Verifies the requested bit count can be written to the buffer.
        /// This exists as a separate method to allow multiple bit writes to be bounds checked with a single call.
        /// If it returns false, you may not write, and in editor and development builds, attempting to do so will
        /// throw an exception. In release builds, attempting to do so will write to random memory addresses and cause
        /// Bad Things(TM).
        /// </summary>
        /// <param name="bitCount">Number of bits you want to write, in total</param>
        /// <returns>True if you can write, false if that would exceed buffer bounds</returns>
        public unsafe bool VerifyCanWriteBits(int bitCount)
        {
            var newBitPosition = m_BitPosition + bitCount;
            var totalBytesWrittenInBitwiseContext = newBitPosition >> 3;
            if ((newBitPosition & 7) != 0)
            {
                // Accounting for the partial write
                ++totalBytesWrittenInBitwiseContext;
            }

            if (m_Position + totalBytesWrittenInBitwiseContext > m_Writer.Value.CapacityInternal)
            {
                if (m_Writer.Value.CapacityInternal < m_Writer.Value.MaxCapacityInternal)
                {
                    m_Writer.Value.Grow();
                    m_BufferPointer = m_Writer.Value.BufferPointer + m_Writer.Value.PositionInternal;
                }
                else
                {
                    return false;
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_AllowedBitwiseWriteMark = newBitPosition;
#endif
            return true;
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

            int checkPos = (m_BitPosition + bitCount);
            if (checkPos > m_AllowedBitwiseWriteMark)
            {
                throw new OverflowException("Attempted to write without first calling FastBufferWriter.VerifyCanWriteBits()");
            }
#endif

            int wholeBytes = bitCount / k_BitsPerByte;
            byte* asBytes = (byte*)&value;
            if (BitAligned)
            {
                if (wholeBytes != 0)
                {
                    WritePartialValue(value, wholeBytes);
                }
            }
            else
            {
                for (var i = 0; i < wholeBytes; ++i)
                {
                    WriteMisaligned(asBytes[i]);
                }
            }

            for (var count = wholeBytes * k_BitsPerByte; count < bitCount; ++count)
            {
                WriteBit((value & (1UL << count)) != 0);
            }
        }

        /// <summary>
        /// Write bits to stream.
        /// </summary>
        /// <param name="value">Value to get bits from.</param>
        /// <param name="bitCount">Amount of bits to write.</param>
        public void WriteBits(byte value, int bitCount)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            int checkPos = (m_BitPosition + bitCount);
            if (checkPos > m_AllowedBitwiseWriteMark)
            {
                throw new OverflowException("Attempted to write without first calling FastBufferWriter.VerifyCanWriteBits()");
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteBit(bool bit)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            int checkPos = (m_BitPosition + 1);
            if (checkPos > m_AllowedBitwiseWriteMark)
            {
                throw new OverflowException("Attempted to write without first calling FastBufferWriter.VerifyCanWriteBits()");
            }
#endif

            int offset = m_BitPosition & 7;
            int pos = m_BitPosition >> 3;
            ++m_BitPosition;
            m_BufferPointer[pos] = (byte)(bit ? (m_BufferPointer[pos] & ~(1 << offset)) | (1 << offset) : (m_BufferPointer[pos] & ~(1 << offset)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WritePartialValue<T>(T value, int bytesToWrite, int offsetBytes = 0) where T : unmanaged
        {
            byte* ptr = ((byte*)&value) + offsetBytes;
            byte* bufferPointer = m_BufferPointer + m_Position;
            BytewiseUtility.FastCopyBytes(bufferPointer, ptr, bytesToWrite);

            m_BitPosition += bytesToWrite * k_BitsPerByte;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WriteMisaligned(byte value)
        {
            int off = m_BitPosition & 7;
            int pos = m_BitPosition >> 3;
            int shift1 = 8 - off;
            m_BufferPointer[pos + 1] = (byte)((m_BufferPointer[pos + 1] & (0xFF << off)) | (value >> shift1));
            m_BufferPointer[pos] = (byte)((m_BufferPointer[pos] & (0xFF >> shift1)) | (value << off));

            m_BitPosition += 8;
        }
    }
}
