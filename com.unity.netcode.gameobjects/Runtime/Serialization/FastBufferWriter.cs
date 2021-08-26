using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Netcode
{
    public struct FastBufferWriter : IDisposable
    {
        internal unsafe byte* BufferPointer;
        internal int PositionInternal;
        private int m_Length;
        internal int CapacityInternal;
        internal readonly int MaxCapacityInternal;
        private readonly Allocator m_Allocator;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        internal int AllowedWriteMark;
        private bool m_InBitwiseContext;
#endif

        /// <summary>
        /// The current write position
        /// </summary>
        public int Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => PositionInternal;
        }

        /// <summary>
        /// The current total buffer size
        /// </summary>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => CapacityInternal;
        }

        /// <summary>
        /// The maximum possible total buffer size
        /// </summary>
        public int MaxCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MaxCapacityInternal;
        }

        /// <summary>
        /// The total amount of bytes that have been written to the stream
        /// </summary>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => PositionInternal > m_Length ? PositionInternal : m_Length;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CommitBitwiseWrites(int amount)
        {
            PositionInternal += amount;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_InBitwiseContext = false;
#endif
        }

        /// <summary>
        /// Create a FastBufferWriter.
        /// </summary>
        /// <param name="size">Size of the buffer to create</param>
        /// <param name="allocator">Allocator to use in creating it</param>
        /// <param name="maxSize">Maximum size the buffer can grow to. If less than size, buffer cannot grow.</param>
        public unsafe FastBufferWriter(int size, Allocator allocator, int maxSize = -1)
        {
            void* buffer = UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<byte>(), allocator);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            UnsafeUtility.MemSet(buffer, 0, size);
#endif
            BufferPointer = (byte*)buffer;
            PositionInternal = 0;
            m_Length = 0;
            CapacityInternal = size;
            m_Allocator = allocator;
            MaxCapacityInternal = maxSize < size ? size : maxSize;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            AllowedWriteMark = 0;
            m_InBitwiseContext = false;
#endif
        }

        /// <summary>
        /// Frees the allocated buffer
        /// </summary>
        public unsafe void Dispose()
        {
            UnsafeUtility.Free(BufferPointer, m_Allocator);
        }

        /// <summary>
        /// Move the write position in the stream.
        /// Note that moving forward past the current length will extend the buffer's Length value even if you don't write.
        /// </summary>
        /// <param name="where">Absolute value to move the position to, truncated to Capacity</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Seek(int where)
        {
            // This avoids us having to synchronize length all the time.
            // Writing things is a much more common operation than seeking
            // or querying length. The length here is a high watermark of
            // what's been written. So before we seek, if the current position
            // is greater than the length, we update that watermark.
            // When querying length later, we'll return whichever of the two
            // values is greater, thus if we write past length, length increases
            // because position increases, and if we seek backward, length remembers
            // the position it was in.
            // Seeking forward will not update the length.
            where = Math.Min(where, CapacityInternal);
            if (PositionInternal > m_Length && where < PositionInternal)
            {
                m_Length = PositionInternal;
            }
            PositionInternal = where;
        }

        /// <summary>
        /// Truncate the stream by setting Length to the specified value.
        /// If Position is greater than the specified value, it will be moved as well.
        /// </summary>
        /// <param name="where">The value to truncate to. If -1, the current position will be used.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Truncate(int where = -1)
        {
            if (where == -1)
            {
                where = Position;
            }

            if (PositionInternal > where)
            {
                PositionInternal = where;
            }
            if (m_Length > where)
            {
                m_Length = where;
            }
        }

        /// <summary>
        /// Retrieve a BitWriter to be able to perform bitwise operations on the buffer.
        /// No bytewise operations can be performed on the buffer until bitWriter.Dispose() has been called.
        /// At the end of the operation, FastBufferWriter will remain byte-aligned.
        /// </summary>
        /// <returns>A BitWriter</returns>
        public BitWriter EnterBitwiseContext()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_InBitwiseContext = true;
#endif
            return new BitWriter(ref this);
        }

        internal unsafe void Grow()
        {
            var newSize = Math.Min(CapacityInternal * 2, MaxCapacityInternal);
            void* buffer = UnsafeUtility.Malloc(newSize, UnsafeUtility.AlignOf<byte>(), m_Allocator);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            UnsafeUtility.MemSet(buffer, 0, newSize);
#endif
            UnsafeUtility.MemCpy(buffer, BufferPointer, Length);
            UnsafeUtility.Free(BufferPointer, m_Allocator);
            BufferPointer = (byte*)buffer;
            CapacityInternal = newSize;
        }

        /// <summary>
        /// Allows faster serialization by batching bounds checking.
        /// When you know you will be writing multiple fields back-to-back and you know the total size,
        /// you can call TryBeginWrite() once on the total size, and then follow it with calls to
        /// WriteValue() instead of WriteValueSafe() for faster serialization.
        /// 
        /// Unsafe write operations will throw OverflowException in editor and development builds if you
        /// go past the point you've marked using TryBeginWrite(). In release builds, OverflowException will not be thrown
        /// for performance reasons, since the point of using TryBeginWrite is to avoid bounds checking in the following
        /// operations in release builds.
        /// </summary>
        /// <param name="bytes">Amount of bytes to write</param>
        /// <returns>True if the write is allowed, false otherwise</returns>
        /// <exception cref="InvalidOperationException">If called while in a bitwise context</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryBeginWrite(int bytes)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif
            if (PositionInternal + bytes > CapacityInternal)
            {
                if (CapacityInternal < MaxCapacityInternal)
                {
                    Grow();
                }
                else
                {
                    return false;
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            AllowedWriteMark = PositionInternal + bytes;
#endif
            return true;
        }

        /// <summary>
        /// Allows faster serialization by batching bounds checking.
        /// When you know you will be writing multiple fields back-to-back and you know the total size,
        /// you can call TryBeginWrite() once on the total size, and then follow it with calls to
        /// WriteValue() instead of WriteValueSafe() for faster serialization.
        /// 
        /// Unsafe write operations will throw OverflowException in editor and development builds if you
        /// go past the point you've marked using TryBeginWrite(). In release builds, OverflowException will not be thrown
        /// for performance reasons, since the point of using TryBeginWrite is to avoid bounds checking in the following
        /// operations in release builds. Instead, attempting to write past the marked position in release builds
        /// will write to random memory and cause undefined behavior, likely including instability and crashes.
        /// </summary>
        /// <param name="value">The value you want to write</param>
        /// <returns>True if the write is allowed, false otherwise</returns>
        /// <exception cref="InvalidOperationException">If called while in a bitwise context</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryBeginWriteValue<T>(in T value) where T : unmanaged
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif
            int len = sizeof(T);
            if (PositionInternal + len > CapacityInternal)
            {
                if (CapacityInternal < MaxCapacityInternal)
                {
                    Grow();
                }
                else
                {
                    return false;
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            AllowedWriteMark = PositionInternal + len;
#endif
            return true;
        }

        /// <summary>
        /// Internal version of TryBeginWrite.
        /// Differs from TryBeginWrite only in that it won't ever move the AllowedWriteMark backward.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryBeginWriteInternal(int bytes)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif
            if (PositionInternal + bytes > CapacityInternal)
            {
                if (CapacityInternal < MaxCapacityInternal)
                {
                    Grow();
                }
                else
                {
                    return false;
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (PositionInternal + bytes > AllowedWriteMark)
            {
                AllowedWriteMark = PositionInternal + bytes;
            }
#endif
            return true;
        }

        /// <summary>
        /// Returns an array representation of the underlying byte buffer.
        /// !!Allocates a new array!!
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe byte[] ToArray()
        {
            byte[] ret = new byte[Length];
            fixed (byte* b = ret)
            {
                UnsafeUtility.MemCpy(b, BufferPointer, Length);
            }
            return ret;
        }

        /// <summary>
        /// Gets a direct pointer to the underlying buffer
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe byte* GetUnsafePtr()
        {
            return BufferPointer;
        }

        /// <summary>
        /// Gets a direct pointer to the underlying buffer at the current read position
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe byte* GetUnsafePtrAtCurrentPosition()
        {
            return BufferPointer + PositionInternal;
        }

        /// <summary>
        /// Get the required size to write a string
        /// </summary>
        /// <param name="s">The string to write</param>
        /// <param name="oneByteChars">Whether or not to use one byte per character. This will only allow ASCII</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetWriteSize(string s, bool oneByteChars = false)
        {
            return sizeof(int) + s.Length * (oneByteChars ? sizeof(byte) : sizeof(char));
        }

        /// <summary>
        /// Writes a string
        /// </summary>
        /// <param name="s">The string to write</param>
        /// <param name="oneByteChars">Whether or not to use one byte per character. This will only allow ASCII</param>
        public unsafe void WriteValue(string s, bool oneByteChars = false)
        {
            WriteValue((uint)s.Length);
            int target = s.Length;
            if (oneByteChars)
            {
                for (int i = 0; i < target; ++i)
                {
                    WriteByte((byte)s[i]);
                }
            }
            else
            {
                fixed (char* native = s)
                {
                    WriteBytes((byte*)native, target * sizeof(char));
                }
            }
        }

        /// <summary>
        /// Writes a string
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple writes at once by calling TryBeginWrite.
        /// </summary>
        /// <param name="s">The string to write</param>
        /// <param name="oneByteChars">Whether or not to use one byte per character. This will only allow ASCII</param>
        public unsafe void WriteValueSafe(string s, bool oneByteChars = false)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif

            int sizeInBytes = GetWriteSize(s, oneByteChars);

            if (!TryBeginWriteInternal(sizeInBytes))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }

            WriteValue((uint)s.Length);
            int target = s.Length;
            if (oneByteChars)
            {
                for (int i = 0; i < target; ++i)
                {
                    WriteByte((byte)s[i]);
                }
            }
            else
            {
                fixed (char* native = s)
                {
                    WriteBytes((byte*)native, target * sizeof(char));
                }
            }
        }

        /// <summary>
        /// Get the required size to write an unmanaged array
        /// </summary>
        /// <param name="array">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        /// <param name="offset">Where in the array to start</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int GetWriteSize<T>(T[] array, int count = -1, int offset = 0) where T : unmanaged
        {
            int sizeInTs = count != -1 ? count : array.Length - offset;
            int sizeInBytes = sizeInTs * sizeof(T);
            return sizeof(int) + sizeInBytes;
        }

        /// <summary>
        /// Writes an unmanaged array
        /// </summary>
        /// <param name="array">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        /// <param name="offset">Where in the array to start</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteValue<T>(T[] array, int count = -1, int offset = 0) where T : unmanaged
        {
            int sizeInTs = count != -1 ? count : array.Length - offset;
            int sizeInBytes = sizeInTs * sizeof(T);
            WriteValue(sizeInTs);
            fixed (T* native = array)
            {
                byte* bytes = (byte*)(native + offset);
                WriteBytes(bytes, sizeInBytes);
            }
        }

        /// <summary>
        /// Writes an unmanaged array
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple writes at once by calling TryBeginWrite.
        /// </summary>
        /// <param name="array">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        /// <param name="offset">Where in the array to start</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteValueSafe<T>(T[] array, int count = -1, int offset = 0) where T : unmanaged
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif

            int sizeInTs = count != -1 ? count : array.Length - offset;
            int sizeInBytes = sizeInTs * sizeof(T);

            if (!TryBeginWriteInternal(sizeInBytes + sizeof(int)))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            WriteValue(sizeInTs);
            fixed (T* native = array)
            {
                byte* bytes = (byte*)(native + offset);
                WriteBytes(bytes, sizeInBytes);
            }
        }

        /// <summary>
        /// Write a partial value. The specified number of bytes is written from the value and the rest is ignored.
        /// </summary>
        /// <param name="value">Value to write</param>
        /// <param name="bytesToWrite">Number of bytes</param>
        /// <param name="offsetBytes">Offset into the value to begin reading the bytes</param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="OverflowException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WritePartialValue<T>(T value, int bytesToWrite, int offsetBytes = 0) where T : unmanaged
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
            if (PositionInternal + bytesToWrite > AllowedWriteMark)
            {
                throw new OverflowException("Attempted to write without first calling TryBeginWrite()");
            }
#endif

            byte* ptr = ((byte*)&value) + offsetBytes;
            byte* bufferPointer = BufferPointer + PositionInternal;
            UnsafeUtility.MemCpy(bufferPointer, ptr, bytesToWrite);

            PositionInternal += bytesToWrite;
        }

        /// <summary>
        /// Write a byte to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteByte(byte value)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
            if (PositionInternal + 1 > AllowedWriteMark)
            {
                throw new OverflowException("Attempted to write without first calling TryBeginWrite()");
            }
#endif
            BufferPointer[PositionInternal++] = value;
        }

        /// <summary>
        /// Write a byte to the stream.
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple writes at once by calling TryBeginWrite.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteByteSafe(byte value)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif

            if (!TryBeginWriteInternal(1))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            BufferPointer[PositionInternal++] = value;
        }

        /// <summary>
        /// Write multiple bytes to the stream
        /// </summary>
        /// <param name="value">Value to write</param>
        /// <param name="size">Number of bytes to write</param>
        /// <param name="offset">Offset into the buffer to begin writing</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteBytes(byte* value, int size, int offset = 0)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
            if (PositionInternal + size > AllowedWriteMark)
            {
                throw new OverflowException("Attempted to write without first calling TryBeginWrite()");
            }
#endif
            UnsafeUtility.MemCpy((BufferPointer + PositionInternal), value + offset, size);
            PositionInternal += size;
        }

        /// <summary>
        /// Write multiple bytes to the stream
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple writes at once by calling TryBeginWrite.
        /// </summary>
        /// <param name="value">Value to write</param>
        /// <param name="size">Number of bytes to write</param>
        /// <param name="offset">Offset into the buffer to begin writing</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteBytesSafe(byte* value, int size, int offset = 0)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif

            if (!TryBeginWriteInternal(size))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            UnsafeUtility.MemCpy((BufferPointer + PositionInternal), value + offset, size);
            PositionInternal += size;
        }

        /// <summary>
        /// Write multiple bytes to the stream
        /// </summary>
        /// <param name="value">Value to write</param>
        /// <param name="size">Number of bytes to write</param>
        /// <param name="offset">Offset into the buffer to begin writing</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteBytes(byte[] value, int size, int offset = 0)
        {
            fixed (byte* ptr = value)
            {
                WriteBytes(ptr, size, offset);
            }
        }

        /// <summary>
        /// Write multiple bytes to the stream
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple writes at once by calling TryBeginWrite.
        /// </summary>
        /// <param name="value">Value to write</param>
        /// <param name="size">Number of bytes to write</param>
        /// <param name="offset">Offset into the buffer to begin writing</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteBytesSafe(byte[] value, int size, int offset = 0)
        {
            fixed (byte* ptr = value)
            {
                WriteBytesSafe(ptr, size, offset);
            }
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
            other.WriteBytes(BufferPointer, PositionInternal);
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
            WriteBytes(other.BufferPointer, other.PositionInternal);
        }

        /// <summary>
        /// Get the size required to write an unmanaged value
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int GetWriteSize<T>(in T value) where T : unmanaged
        {
            return sizeof(T);
        }

        /// <summary>
        /// Get the size required to write an unmanaged value of type T
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static unsafe int GetWriteSize<T>() where T : unmanaged
        {
            return sizeof(T);
        }

        /// <summary>
        /// Write a value of any unmanaged type (including unmanaged structs) to the buffer.
        /// It will be copied into the buffer exactly as it exists in memory.
        /// </summary>
        /// <param name="value">The value to copy</param>
        /// <typeparam name="T">Any unmanaged type</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteValue<T>(in T value) where T : unmanaged
        {
            int len = sizeof(T);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
            if (PositionInternal + len > AllowedWriteMark)
            {
                throw new OverflowException("Attempted to write without first calling TryBeginWrite()");
            }
#endif

            fixed (T* ptr = &value)
            {
                BytewiseUtility.FastCopyBytes(BufferPointer + PositionInternal, (byte*)ptr, len);
            }
            PositionInternal += len;
        }

        /// <summary>
        /// Write a value of any unmanaged type (including unmanaged structs) to the buffer.
        /// It will be copied into the buffer exactly as it exists in memory.
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple writes at once by calling TryBeginWrite.
        /// </summary>
        /// <param name="value">The value to copy</param>
        /// <typeparam name="T">Any unmanaged type</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteValueSafe<T>(in T value) where T : unmanaged
        {
            int len = sizeof(T);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif

            if (!TryBeginWriteInternal(len))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }

            fixed (T* ptr = &value)
            {
                BytewiseUtility.FastCopyBytes(BufferPointer + PositionInternal, (byte*)ptr, len);
            }
            PositionInternal += len;
        }
    }
}
