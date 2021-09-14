using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Netcode
{
    public struct FastBufferReader : IDisposable
    {
        internal readonly unsafe byte* BufferPointer;
        internal int PositionInternal;
        internal readonly int LengthInternal;
        private readonly Allocator m_Allocator;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        internal int AllowedReadMark;
        private bool m_InBitwiseContext;
#endif

        /// <summary>
        /// Get the current read position
        /// </summary>
        public int Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => PositionInternal;
        }

        /// <summary>
        /// Get the total length of the buffer
        /// </summary>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => LengthInternal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CommitBitwiseReads(int amount)
        {
            PositionInternal += amount;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_InBitwiseContext = false;
#endif
        }

        public unsafe FastBufferReader(NativeArray<byte> buffer, Allocator allocator, int length = -1, int offset = 0)
        {
            LengthInternal = Math.Max(1, length == -1 ? buffer.Length : length);
            if (allocator == Allocator.None)
            {
                BufferPointer = (byte*) buffer.GetUnsafePtr() + offset;
            }
            else
            {
                void* bufferPtr = UnsafeUtility.Malloc(LengthInternal, UnsafeUtility.AlignOf<byte>(), allocator);
                UnsafeUtility.MemCpy(bufferPtr, (byte*)buffer.GetUnsafePtr() + offset, LengthInternal);
                BufferPointer = (byte*)bufferPtr;
            }
            PositionInternal = offset;
            m_Allocator = allocator;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            AllowedReadMark = 0;
            m_InBitwiseContext = false;
#endif
        }

        /// <summary>
        /// Create a FastBufferReader from an ArraySegment.
        /// A new buffer will be created using the given allocator and the value will be copied in.
        /// FastBufferReader will then own the data.
        /// </summary>
        /// <param name="buffer">The buffer to copy from</param>
        /// <param name="allocator">The allocator to use</param>
        /// <param name="length">The number of bytes to copy (all if this is -1)</param>
        /// <param name="offset">The offset of the buffer to start copying from</param>
        public unsafe FastBufferReader(ArraySegment<byte> buffer, Allocator allocator, int length = -1, int offset = 0)
        {
            LengthInternal = Math.Max(1, length == -1 ? (buffer.Count - offset) : length);
            if (allocator == Allocator.None)
            {
                throw new NotSupportedException("Allocator.None cannot be used with managed source buffers.");
            }
            else
            {
                void* bufferPtr = UnsafeUtility.Malloc(LengthInternal, UnsafeUtility.AlignOf<byte>(), allocator);
                fixed (byte* data = buffer.Array)
                {
                    UnsafeUtility.MemCpy(bufferPtr, data + offset, LengthInternal);
                }

                BufferPointer = (byte*) bufferPtr;
            }

            PositionInternal = 0;
            m_Allocator = allocator;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            AllowedReadMark = 0;
            m_InBitwiseContext = false;
#endif
        }

        /// <summary>
        /// Create a FastBufferReader from an existing byte array.
        /// A new buffer will be created using the given allocator and the value will be copied in.
        /// FastBufferReader will then own the data.
        /// </summary>
        /// <param name="buffer">The buffer to copy from</param>
        /// <param name="allocator">The allocator to use</param>
        /// <param name="length">The number of bytes to copy (all if this is -1)</param>
        /// <param name="offset">The offset of the buffer to start copying from</param>
        public unsafe FastBufferReader(byte[] buffer, Allocator allocator, int length = -1, int offset = 0)
        {
            LengthInternal = Math.Max(1, length == -1 ? (buffer.Length - offset) : length);
            if (allocator == Allocator.None)
            {
                throw new NotSupportedException("Allocator.None cannot be used with managed source buffers.");
            }
            else
            {
                void* bufferPtr = UnsafeUtility.Malloc(LengthInternal, UnsafeUtility.AlignOf<byte>(), allocator);
                fixed (byte* data = buffer)
                {
                    UnsafeUtility.MemCpy(bufferPtr, data + offset, LengthInternal);
                }

                BufferPointer = (byte*) bufferPtr;
            }

            PositionInternal = 0;
            m_Allocator = allocator;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            AllowedReadMark = 0;
            m_InBitwiseContext = false;
#endif
        }

        /// <summary>
        /// Create a FastBufferReader from an existing byte buffer.
        /// A new buffer will be created using the given allocator and the value will be copied in.
        /// FastBufferReader will then own the data.
        /// </summary>
        /// <param name="buffer">The buffer to copy from</param>
        /// <param name="allocator">The allocator to use</param>
        /// <param name="length">The number of bytes to copy</param>
        /// <param name="offset">The offset of the buffer to start copying from</param>
        public unsafe FastBufferReader(byte* buffer, Allocator allocator, int length, int offset = 0)
        {
            LengthInternal = Math.Max(1, length);
            if (allocator == Allocator.None)
            {
                    BufferPointer = buffer + offset;
            }
            else
            {
                void* bufferPtr = UnsafeUtility.Malloc(LengthInternal, UnsafeUtility.AlignOf<byte>(), allocator);
                UnsafeUtility.MemCpy(bufferPtr, buffer + offset, LengthInternal);
                BufferPointer = (byte*) bufferPtr;
            }

            PositionInternal = 0;
            m_Allocator = allocator;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            AllowedReadMark = 0;
            m_InBitwiseContext = false;
#endif
        }

        /// <summary>
        /// Create a FastBufferReader from a FastBufferWriter.
        /// A new buffer will be created using the given allocator and the value will be copied in.
        /// FastBufferReader will then own the data.
        /// </summary>
        /// <param name="writer">The writer to copy from</param>
        /// <param name="allocator">The allocator to use</param>
        /// <param name="length">The number of bytes to copy (all if this is -1)</param>
        /// <param name="offset">The offset of the buffer to start copying from</param>
        public unsafe FastBufferReader(ref FastBufferWriter writer, Allocator allocator, int length = -1, int offset = 0)
        {
            LengthInternal = Math.Max(1, length == -1 ? writer.Length : length);
            void* bufferPtr = UnsafeUtility.Malloc(LengthInternal, UnsafeUtility.AlignOf<byte>(), allocator);
            UnsafeUtility.MemCpy(bufferPtr, writer.GetUnsafePtr() + offset, LengthInternal);
            BufferPointer = (byte*)bufferPtr;
            PositionInternal = 0;
            m_Allocator = allocator;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            AllowedReadMark = 0;
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
        /// Move the read position in the stream
        /// </summary>
        /// <param name="where">Absolute value to move the position to, truncated to Length</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Seek(int where)
        {
            PositionInternal = Math.Min(Length, where);
        }

        /// <summary>
        /// Mark that some bytes are going to be read via GetUnsafePtr().
        /// </summary>
        /// <param name="amount">Amount that will be read</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="OverflowException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void MarkBytesRead(int amount)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
            if (PositionInternal + amount > AllowedReadMark)
            {
                throw new OverflowException("Attempted to read without first calling TryBeginRead()");
            }
#endif
            PositionInternal += amount;
        }

        /// <summary>
        /// Retrieve a BitReader to be able to perform bitwise operations on the buffer.
        /// No bytewise operations can be performed on the buffer until bitReader.Dispose() has been called.
        /// At the end of the operation, FastBufferReader will remain byte-aligned.
        /// </summary>
        /// <returns>A BitReader</returns>
        public BitReader EnterBitwiseContext()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_InBitwiseContext = true;
#endif
            return new BitReader(ref this);
        }

        /// <summary>
        /// Allows faster serialization by batching bounds checking.
        /// When you know you will be reading multiple fields back-to-back and you know the total size,
        /// you can call TryBeginRead() once on the total size, and then follow it with calls to
        /// ReadValue() instead of ReadValueSafe() for faster serialization.
        /// 
        /// Unsafe read operations will throw OverflowException in editor and development builds if you
        /// go past the point you've marked using TryBeginRead(). In release builds, OverflowException will not be thrown
        /// for performance reasons, since the point of using TryBeginRead is to avoid bounds checking in the following
        /// operations in release builds.
        /// </summary>
        /// <param name="bytes">Amount of bytes to read</param>
        /// <returns>True if the read is allowed, false otherwise</returns>
        /// <exception cref="InvalidOperationException">If called while in a bitwise context</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryBeginRead(int bytes)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif
            if (PositionInternal + bytes > LengthInternal)
            {
                return false;
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            AllowedReadMark = PositionInternal + bytes;
#endif
            return true;
        }

        /// <summary>
        /// Allows faster serialization by batching bounds checking.
        /// When you know you will be reading multiple fields back-to-back and you know the total size,
        /// you can call TryBeginRead() once on the total size, and then follow it with calls to
        /// ReadValue() instead of ReadValueSafe() for faster serialization.
        /// 
        /// Unsafe read operations will throw OverflowException in editor and development builds if you
        /// go past the point you've marked using TryBeginRead(). In release builds, OverflowException will not be thrown
        /// for performance reasons, since the point of using TryBeginRead is to avoid bounds checking in the following
        /// operations in release builds.
        /// </summary>
        /// <param name="value">The value you want to read</param>
        /// <returns>True if the read is allowed, false otherwise</returns>
        /// <exception cref="InvalidOperationException">If called while in a bitwise context</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryBeginReadValue<T>(in T value) where T : unmanaged
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif
            int len = sizeof(T);
            if (PositionInternal + len > LengthInternal)
            {
                return false;
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            AllowedReadMark = PositionInternal + len;
#endif
            return true;
        }

        /// <summary>
        /// Internal version of TryBeginRead.
        /// Differs from TryBeginRead only in that it won't ever move the AllowedReadMark backward.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryBeginReadInternal(int bytes)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif
            if (PositionInternal + bytes > LengthInternal)
            {
                return false;
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (PositionInternal + bytes > AllowedReadMark)
            {
                AllowedReadMark = PositionInternal + bytes;
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
        /// Read an INetworkSerializable
        /// </summary>
        /// <param name="value">INetworkSerializable instance</param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="NotImplementedException"></exception>
        public void ReadNetworkSerializable<T>(out T value) where T : INetworkSerializable, new()
        {
            value = new T();
            var bufferSerializer = new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(ref this));
            value.NetworkSerialize(bufferSerializer);
        }

        /// <summary>
        /// Read an array of INetworkSerializables
        /// </summary>
        /// <param name="value">INetworkSerializable instance</param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="NotImplementedException"></exception>
        public void ReadNetworkSerializable<T>(out T[] value) where T : INetworkSerializable, new()
        {
            ReadValueSafe(out int size);
            value = new T[size];
            for(var i = 0; i < size; ++i)
            {
                ReadNetworkSerializable(out value[i]);
            }
        }

        /// <summary>
        /// Reads a string
        /// NOTE: ALLOCATES
        /// </summary>
        /// <param name="s">Stores the read string</param>
        /// <param name="oneByteChars">Whether or not to use one byte per character. This will only allow ASCII</param>
        public unsafe void ReadValue(out string s, bool oneByteChars = false)
        {
            ReadValue(out uint length);
            s = "".PadRight((int)length);
            int target = s.Length;
            fixed (char* native = s)
            {
                if (oneByteChars)
                {
                    for (int i = 0; i < target; ++i)
                    {
                        ReadByte(out byte b);
                        native[i] = (char)b;
                    }
                }
                else
                {
                    ReadBytes((byte*)native, target * sizeof(char));
                }
            }
        }

        /// <summary>
        /// Reads a string.
        /// NOTE: ALLOCATES
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple reads at once by calling TryBeginRead.
        /// </summary>
        /// <param name="s">Stores the read string</param>
        /// <param name="oneByteChars">Whether or not to use one byte per character. This will only allow ASCII</param>
        public unsafe void ReadValueSafe(out string s, bool oneByteChars = false)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif

            if (!TryBeginReadInternal(sizeof(uint)))
            {
                throw new OverflowException("Reading past the end of the buffer");
            }

            ReadValue(out uint length);

            if (!TryBeginReadInternal((int)length * (oneByteChars ? 1 : sizeof(char))))
            {
                throw new OverflowException("Reading past the end of the buffer");
            }
            s = "".PadRight((int)length);
            int target = s.Length;
            fixed (char* native = s)
            {
                if (oneByteChars)
                {
                    for (int i = 0; i < target; ++i)
                    {
                        ReadByte(out byte b);
                        native[i] = (char)b;
                    }
                }
                else
                {
                    ReadBytes((byte*)native, target * sizeof(char));
                }
            }
        }

        /// <summary>
        /// Writes an unmanaged array
        /// NOTE: ALLOCATES
        /// </summary>
        /// <param name="array">Stores the read array</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadValue<T>(out T[] array) where T : unmanaged
        {
            ReadValue(out int sizeInTs);
            int sizeInBytes = sizeInTs * sizeof(T);
            array = new T[sizeInTs];
            fixed (T* native = array)
            {
                byte* bytes = (byte*)(native);
                ReadBytes(bytes, sizeInBytes);
            }
        }

        /// <summary>
        /// Reads an unmanaged array
        /// NOTE: ALLOCATES
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple reads at once by calling TryBeginRead.
        /// </summary>
        /// <param name="array">Stores the read array</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadValueSafe<T>(out T[] array) where T : unmanaged
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif

            if (!TryBeginReadInternal(sizeof(int)))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            ReadValue(out int sizeInTs);
            int sizeInBytes = sizeInTs * sizeof(T);
            if (!TryBeginReadInternal(sizeInBytes))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            array = new T[sizeInTs];
            fixed (T* native = array)
            {
                byte* bytes = (byte*)(native);
                ReadBytes(bytes, sizeInBytes);
            }
        }

        /// <summary>
        /// Read a partial value. The value is zero-initialized and then the specified number of bytes is read into it.
        /// </summary>
        /// <param name="value">Value to read</param>
        /// <param name="bytesToRead">Number of bytes</param>
        /// <param name="offsetBytes">Offset into the value to write the bytes</param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="OverflowException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadPartialValue<T>(out T value, int bytesToRead, int offsetBytes = 0) where T : unmanaged
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
            if (PositionInternal + bytesToRead > AllowedReadMark)
            {
                throw new OverflowException("Attempted to read without first calling TryBeginRead()");
            }
#endif

            var val = new T();
            byte* ptr = ((byte*)&val) + offsetBytes;
            byte* bufferPointer = BufferPointer + PositionInternal;
            UnsafeUtility.MemCpy(ptr, bufferPointer, bytesToRead);

            PositionInternal += bytesToRead;
            value = val;
        }

        /// <summary>
        /// Read a byte to the stream.
        /// </summary>
        /// <param name="value">Stores the read value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadByte(out byte value)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
            if (PositionInternal + 1 > AllowedReadMark)
            {
                throw new OverflowException("Attempted to read without first calling TryBeginRead()");
            }
#endif
            value = BufferPointer[PositionInternal++];
        }

        /// <summary>
        /// Read a byte to the stream.
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple reads at once by calling TryBeginRead.
        /// </summary>
        /// <param name="value">Stores the read value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadByteSafe(out byte value)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif

            if (!TryBeginReadInternal(1))
            {
                throw new OverflowException("Reading past the end of the buffer");
            }
            value = BufferPointer[PositionInternal++];
        }

        /// <summary>
        /// Read multiple bytes to the stream
        /// </summary>
        /// <param name="value">Pointer to the destination buffer</param>
        /// <param name="size">Number of bytes to read - MUST BE &lt;= BUFFER SIZE</param>
        /// <param name="offset">Offset of the byte buffer to store into</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadBytes(byte* value, int size, int offset = 0)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
            if (PositionInternal + size > AllowedReadMark)
            {
                throw new OverflowException("Attempted to read without first calling TryBeginRead()");
            }
#endif
            UnsafeUtility.MemCpy(value + offset, (BufferPointer + PositionInternal), size);
            PositionInternal += size;
        }

        /// <summary>
        /// Read multiple bytes to the stream
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple reads at once by calling TryBeginRead.
        /// </summary>
        /// <param name="value">Pointer to the destination buffer</param>
        /// <param name="size">Number of bytes to read - MUST BE &lt;= BUFFER SIZE</param>
        /// <param name="offset">Offset of the byte buffer to store into</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadBytesSafe(byte* value, int size, int offset = 0)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif

            if (!TryBeginReadInternal(size))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            UnsafeUtility.MemCpy(value + offset, (BufferPointer + PositionInternal), size);
            PositionInternal += size;
        }

        /// <summary>
        /// Read multiple bytes from the stream
        /// </summary>
        /// <param name="value">Pointer to the destination buffer</param>
        /// <param name="size">Number of bytes to read - MUST BE &lt;= BUFFER SIZE</param>
        /// <param name="offset">Offset of the byte buffer to store into</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadBytes(ref byte[] value, int size, int offset = 0)
        {
            fixed (byte* ptr = value)
            {
                ReadBytes(ptr, size, offset);
            }
        }

        /// <summary>
        /// Read multiple bytes from the stream
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple reads at once by calling TryBeginRead.
        /// </summary>
        /// <param name="value">Pointer to the destination buffer</param>
        /// <param name="size">Number of bytes to read - MUST BE &lt;= BUFFER SIZE</param>
        /// <param name="offset">Offset of the byte buffer to store into</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadBytesSafe(ref byte[] value, int size, int offset = 0)
        {
            fixed (byte* ptr = value)
            {
                ReadBytesSafe(ptr, size, offset);
            }
        }

        /// <summary>
        /// Read a value of type FixedUnmanagedArray from the buffer.
        /// </summary>
        /// <param name="value">The value to copy</param>
        /// <typeparam name="T">Any unmanaged type</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadValue<TPropertyType, TStorageType>(out FixedUnmanagedArray<TPropertyType, TStorageType> value, int count) 
            where TPropertyType : unmanaged
            where TStorageType : unmanaged, IFixedArrayStorage
        {
            int len = sizeof(TPropertyType) * count;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
            if (PositionInternal + len > AllowedReadMark)
            {
                throw new OverflowException("Attempted to write without first calling TryBeginWrite()");
            }
#endif

            value = new FixedUnmanagedArray<TPropertyType, TStorageType>();
            BytewiseUtility.FastCopyBytes((byte*)value.GetArrayPtr(), BufferPointer + PositionInternal, len);
            value.Count = len;
            PositionInternal += len;
        }

        /// <summary>
        /// Read a value of type FixedUnmanagedArray from the buffer.
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple reads at once by calling TryBeginRead.
        /// </summary>
        /// <param name="value">The value to copy</param>
        /// <typeparam name="T">Any unmanaged type</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadValueSafe<TPropertyType, TStorageType>(out FixedUnmanagedArray<TPropertyType, TStorageType> value, int count) 
            where TPropertyType : unmanaged
            where TStorageType : unmanaged, IFixedArrayStorage
        {
            int len = sizeof(TPropertyType) * count;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
            if (PositionInternal + len > AllowedReadMark)
            {
                throw new OverflowException("Attempted to write without first calling TryBeginWrite()");
            }
#endif

            value = new FixedUnmanagedArray<TPropertyType, TStorageType>();
            BytewiseUtility.FastCopyBytes((byte*)value.GetArrayPtr(), BufferPointer + PositionInternal, len);
            value.Count = len;
            PositionInternal += len;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("FixedUnmanagedArray must be written/read using a count.")]
        public void ReadValue<TPropertyType, TStorageType>(out FixedUnmanagedArray<TPropertyType, TStorageType> value)
            where TPropertyType : unmanaged
            where TStorageType : unmanaged, IFixedArrayStorage
        {
            throw new NotSupportedException("FixedUnmanagedArray must be written/read using a count.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("FixedUnmanagedArray must be written/read using a count.")]
        public void ReadValueSafe<TPropertyType, TStorageType>(out FixedUnmanagedArray<TPropertyType, TStorageType> value)
            where TPropertyType : unmanaged
            where TStorageType : unmanaged, IFixedArrayStorage
        {
            throw new NotSupportedException("FixedUnmanagedArray must be written/read using a count.");
        }

        /// <summary>
        /// Read a value of any unmanaged type to the buffer.
        /// It will be copied from the buffer exactly as it existed in memory on the writing end.
        /// </summary>
        /// <param name="value">The read value</param>
        /// <typeparam name="T">Any unmanaged type</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadValue<T>(out T value) where T : unmanaged
        {
            int len = sizeof(T);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
            if (PositionInternal + len > AllowedReadMark)
            {
                throw new OverflowException("Attempted to read without first calling TryBeginRead()");
            }
#endif

            fixed (T* ptr = &value)
            {
                BytewiseUtility.FastCopyBytes((byte*)ptr, BufferPointer + PositionInternal, len);
            }
            PositionInternal += len;
        }

        /// <summary>
        /// Read a value of any unmanaged type to the buffer.
        /// It will be copied from the buffer exactly as it existed in memory on the writing end.
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple reads at once by calling TryBeginRead.
        /// </summary>
        /// <param name="value">The read value</param>
        /// <typeparam name="T">Any unmanaged type</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadValueSafe<T>(out T value) where T : unmanaged
        {
            int len = sizeof(T);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif

            if (!TryBeginReadInternal(len))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }


            fixed (T* ptr = &value)
            {
                BytewiseUtility.FastCopyBytes((byte*)ptr, BufferPointer + PositionInternal, len);
            }
            PositionInternal += len;
        }
    }
}
