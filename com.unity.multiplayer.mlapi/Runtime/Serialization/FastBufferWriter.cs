using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;

namespace Unity.Multiplayer.Netcode
{
    public struct FastBufferWriter
    {
        private NativeArray<byte> m_buffer;
        private readonly unsafe byte* m_bufferPointer;
        private int m_position;

        public unsafe FastBufferWriter(NativeArray<byte> buffer, int position = 0)
        {
            m_buffer = buffer;
            m_bufferPointer = (byte*) m_buffer.GetUnsafePtr();
            m_position = position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Seek(int where)
        {
            m_position = where;
        }

        public int Position => m_position;

        public NativeArray<byte> GetNativeArray()
        {
            return m_buffer;
        }

        public byte[] ToArray()
        {
            return m_buffer.ToArray();
        }

        public unsafe byte* GetUnsafePtr()
        {
            return m_bufferPointer;
        }

        public unsafe byte* GetUnsafePtrAtCurrentPosition()
        {
            return m_bufferPointer + m_position;
        }

        /// <summary>
        /// Write single-precision floating point value to the stream as a varint
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSinglePacked(float value)
        {
            WriteUInt32Packed(ToUint(value));
        }

        /// <summary>
        /// Write double-precision floating point value to the stream as a varint
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDoublePacked(double value)
        {
            WriteUInt64Packed(ToUlong(value));
        }

        /// <summary>
        /// Write a signed short (Int16) as a ZigZag encoded varint to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt16Packed(short value) => WriteInt64Packed(value);

        /// <summary>
        /// Write an unsigned short (UInt16) as a varint to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt16Packed(ushort value) => WriteUInt64Packed(value);

        /// <summary>
        /// Write a two-byte character as a varint to the stream.
        /// </summary>
        /// <param name="c">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteCharPacked(char c) => WriteUInt16Packed(c);

        /// <summary>
        /// Write a signed int (Int32) as a ZigZag encoded varint to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32Packed(int value) => WriteInt64Packed(value);

        /// <summary>
        /// Write an unsigned int (UInt32) as a varint to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt32Packed(uint value) => WriteUInt64Packed(value);

        /// <summary>
        /// Write a signed long (Int64) as a ZigZag encoded varint to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt64Packed(long value) => WriteUInt64Packed(Arithmetic.ZigZagEncode(value));

        /// <summary>
        /// Write an unsigned long (UInt64) as a varint to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteUInt64Packed(ulong value)
        {
            if (value <= 240)
            {
                WriteULongByte(value);
            }
            else if (value <= 2287)
            {
                WriteULongByte(((value - 240) >> 8) + 241);
                WriteULongByte(value - 240);
            }
            else if (value <= 67823)
            {
                WriteByte(249);
                WriteULongByte((value - 2288) >> 8);
                WriteULongByte(value - 2288);
            }
            else
            {
                ulong header = 255;
                ulong match = 0x00FF_FFFF_FFFF_FFFFUL;
                while (value <= match)
                {
                    --header;
                    match >>= 8;
                }

                WriteULongByte(header);
                int max = (int)(header - 247);
                for (int i = 0; i < max; ++i)
                {
                    WriteULongByte(value >> (i << 3));
                }
            }
        }

        /// <summary>
        /// Write a byte (in an int format) to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteIntByte(int value) => WriteByte((byte)value);

        /// <summary>
        /// Write a byte (in a ulong format) to the stream.
        /// </summary>
        /// <param name="byteValue">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteULongByte(ulong byteValue) => WriteByte((byte)byteValue);

        /// <summary>
        /// Write a byte to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteByte(byte value)
        {
            m_bufferPointer[m_position++] = value;
        }
        
        /// <summary>
        /// Write multiple bytes to the stream
        /// </summary>
        /// <param name="value">Value to write</param>
        /// <param name="size">Number of bytes to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteBytes(byte* value, int size)
        {
            UnsafeUtility.MemCpy((m_bufferPointer + m_position), value, size);
            m_position += size;
        }
        
        /// <summary>
        /// Copy the contents of this writer into another writer.
        /// The contents will be copied from the beginning of this writer to its current position.
        /// They will be copied to the other writer starting at the other writer's current position.
        /// </summary>
        /// <param name="other">Writer to copy to</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void CopyTo(FastBufferWriter other)
        {
            other.WriteBytes(m_bufferPointer, m_position);
        }
        
        /// <summary>
        /// Copy the contents of another writer into this writer.
        /// The contents will be copied from the beginning of the other writer to its current position.
        /// They will be copied to this writer starting at this writer's current position.
        /// </summary>
        /// <param name="other">Writer to copy to</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void CopyFrom(FastBufferWriter other)
        {
            WriteBytes(other.m_bufferPointer, other.m_position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe uint ToUint<T>(T value) where T : unmanaged
        {
            uint* asUint = (uint*) &value;
            return *asUint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe ulong ToUlong<T>(T value) where T : unmanaged
        {
            ulong* asUlong = (ulong*) &value;
            return *asUlong;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe float ToSingle<T>(T value) where T : unmanaged
        {
            float* asFloat = (float*) &value;
            return *asFloat;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe double ToDouble<T>(T value) where T : unmanaged
        {
            double* asDouble = (double*) &value;
            return *asDouble;
        }
        
        /// <summary>
        /// Write a value of any unmanaged type to the buffer.
        /// It will be copied into the buffer exactly as it exists in memory.
        /// </summary>
        /// <param name="value">The value to copy</param>
        /// <typeparam name="T">Any unmanaged type</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteValue<T>(in T value) where T : unmanaged
        {
            int len = sizeof(T);
            T* pointer = (T*)(m_bufferPointer+m_position);
            *pointer = value;
            m_position += len;
        }
    }
}