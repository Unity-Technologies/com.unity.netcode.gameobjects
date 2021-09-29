using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Netcode
{
    public struct FastBufferReader : IDisposable
    {
        internal struct ReaderHandle
        {
            internal unsafe byte* BufferPointer;
            internal int Position;
            internal int Length;
            internal Allocator Allocator;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            internal int AllowedReadMark;
            internal bool InBitwiseContext;
#endif
        }

        internal readonly unsafe ReaderHandle* Handle;

        /// <summary>
        /// Get the current read position
        /// </summary>
        public unsafe int Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Handle->Position;
        }

        /// <summary>
        /// Get the total length of the buffer
        /// </summary>
        public unsafe int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Handle->Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void CommitBitwiseReads(int amount)
        {
            Handle->Position += amount;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Handle->InBitwiseContext = false;
#endif
        }

        private static unsafe ReaderHandle* CreateHandle(byte* buffer, int length, int offset, Allocator allocator)
        {
            ReaderHandle* readerHandle = null;
            if (allocator == Allocator.None)
            {
                readerHandle = (ReaderHandle*)UnsafeUtility.Malloc(sizeof(ReaderHandle) + length, UnsafeUtility.AlignOf<byte>(), Allocator.Temp);
                readerHandle->BufferPointer = buffer;
                readerHandle->Position = offset;
            }
            else
            {
                readerHandle = (ReaderHandle*)UnsafeUtility.Malloc(sizeof(ReaderHandle) + length, UnsafeUtility.AlignOf<byte>(), allocator);
                UnsafeUtility.MemCpy(readerHandle + 1, buffer + offset, length);
                readerHandle->BufferPointer = (byte*)(readerHandle + 1);
                readerHandle->Position = 0;
            }

            readerHandle->Length = length;
            readerHandle->Allocator = allocator;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            readerHandle->AllowedReadMark = 0;
            readerHandle->InBitwiseContext = false;
#endif
            return readerHandle;
        }

        /// <summary>
        /// Create a FastBufferReader from a NativeArray.
        /// 
        /// A new buffer will be created using the given allocator and the value will be copied in.
        /// FastBufferReader will then own the data.
        ///
        /// The exception to this is when the allocator passed in is Allocator.None. In this scenario,
        /// ownership of the data remains with the caller and the reader will point at it directly.
        /// When created with Allocator.None, FastBufferReader will allocate some internal data using
        /// Allocator.Temp, so it should be treated as if it's a ref struct and not allowed to outlive
        /// the context in which it was created (it should neither be returned from that function nor
        /// stored anywhere in heap memory).
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="allocator"></param>
        /// <param name="length"></param>
        /// <param name="offset"></param>
        public unsafe FastBufferReader(NativeArray<byte> buffer, Allocator allocator, int length = -1, int offset = 0)
        {
            Handle = CreateHandle((byte*)buffer.GetUnsafePtr(), Math.Max(1, length == -1 ? buffer.Length : length), offset, allocator);
        }

        /// <summary>
        /// Create a FastBufferReader from an ArraySegment.
        /// 
        /// A new buffer will be created using the given allocator and the value will be copied in.
        /// FastBufferReader will then own the data.
        ///
        /// Allocator.None is not supported for byte[]. If you need this functionality, use a fixed() block
        /// and ensure the FastBufferReader isn't used outside that block.
        /// </summary>
        /// <param name="buffer">The buffer to copy from</param>
        /// <param name="allocator">The allocator to use</param>
        /// <param name="length">The number of bytes to copy (all if this is -1)</param>
        /// <param name="offset">The offset of the buffer to start copying from</param>
        public unsafe FastBufferReader(ArraySegment<byte> buffer, Allocator allocator, int length = -1, int offset = 0)
        {
            if (allocator == Allocator.None)
            {
                throw new NotSupportedException("Allocator.None cannot be used with managed source buffers.");
            }
            fixed (byte* data = buffer.Array)
            {
                Handle = CreateHandle(data, Math.Max(1, length == -1 ? buffer.Count : length), offset, allocator);
            }
        }

        /// <summary>
        /// Create a FastBufferReader from an existing byte array.
        /// 
        /// A new buffer will be created using the given allocator and the value will be copied in.
        /// FastBufferReader will then own the data.
        ///
        /// Allocator.None is not supported for byte[]. If you need this functionality, use a fixed() block
        /// and ensure the FastBufferReader isn't used outside that block.
        /// </summary>
        /// <param name="buffer">The buffer to copy from</param>
        /// <param name="allocator">The allocator to use</param>
        /// <param name="length">The number of bytes to copy (all if this is -1)</param>
        /// <param name="offset">The offset of the buffer to start copying from</param>
        public unsafe FastBufferReader(byte[] buffer, Allocator allocator, int length = -1, int offset = 0)
        {
            if (allocator == Allocator.None)
            {
                throw new NotSupportedException("Allocator.None cannot be used with managed source buffers.");
            }
            fixed (byte* data = buffer)
            {
                Handle = CreateHandle(data, Math.Max(1, length == -1 ? buffer.Length : length), offset, allocator);
            }
        }

        /// <summary>
        /// Create a FastBufferReader from an existing byte buffer.
        /// 
        /// A new buffer will be created using the given allocator and the value will be copied in.
        /// FastBufferReader will then own the data.
        ///
        /// The exception to this is when the allocator passed in is Allocator.None. In this scenario,
        /// ownership of the data remains with the caller and the reader will point at it directly.
        /// When created with Allocator.None, FastBufferReader will allocate some internal data using
        /// Allocator.Temp, so it should be treated as if it's a ref struct and not allowed to outlive
        /// the context in which it was created (it should neither be returned from that function nor
        /// stored anywhere in heap memory).
        /// </summary>
        /// <param name="buffer">The buffer to copy from</param>
        /// <param name="allocator">The allocator to use</param>
        /// <param name="length">The number of bytes to copy</param>
        /// <param name="offset">The offset of the buffer to start copying from</param>
        public unsafe FastBufferReader(byte* buffer, Allocator allocator, int length, int offset = 0)
        {
            Handle = CreateHandle(buffer, Math.Max(1, length), offset, allocator);
        }

        /// <summary>
        /// Create a FastBufferReader from a FastBufferWriter.
        /// 
        /// A new buffer will be created using the given allocator and the value will be copied in.
        /// FastBufferReader will then own the data.
        ///
        /// The exception to this is when the allocator passed in is Allocator.None. In this scenario,
        /// ownership of the data remains with the caller and the reader will point at it directly.
        /// When created with Allocator.None, FastBufferReader will allocate some internal data using
        /// Allocator.Temp, so it should be treated as if it's a ref struct and not allowed to outlive
        /// the context in which it was created (it should neither be returned from that function nor
        /// stored anywhere in heap memory).
        /// </summary>
        /// <param name="writer">The writer to copy from</param>
        /// <param name="allocator">The allocator to use</param>
        /// <param name="length">The number of bytes to copy (all if this is -1)</param>
        /// <param name="offset">The offset of the buffer to start copying from</param>
        public unsafe FastBufferReader(FastBufferWriter writer, Allocator allocator, int length = -1, int offset = 0)
        {
            Handle = CreateHandle(writer.GetUnsafePtr(), Math.Max(1, length == -1 ? writer.Length : length), offset, allocator);
        }

        /// <summary>
        /// Frees the allocated buffer
        /// </summary>
        public unsafe void Dispose()
        {
            UnsafeUtility.Free(Handle, Handle->Allocator);
        }

        /// <summary>
        /// Move the read position in the stream
        /// </summary>
        /// <param name="where">Absolute value to move the position to, truncated to Length</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Seek(int where)
        {
            Handle->Position = Math.Min(Length, where);
        }

        /// <summary>
        /// Mark that some bytes are going to be read via GetUnsafePtr().
        /// </summary>
        /// <param name="amount">Amount that will be read</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="OverflowException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void MarkBytesRead(int amount)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (Handle->InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
            if (Handle->Position + amount > Handle->AllowedReadMark)
            {
                throw new OverflowException("Attempted to read without first calling TryBeginRead()");
            }
#endif
            Handle->Position += amount;
        }

        /// <summary>
        /// Retrieve a BitReader to be able to perform bitwise operations on the buffer.
        /// No bytewise operations can be performed on the buffer until bitReader.Dispose() has been called.
        /// At the end of the operation, FastBufferReader will remain byte-aligned.
        /// </summary>
        /// <returns>A BitReader</returns>
        public unsafe BitReader EnterBitwiseContext()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Handle->InBitwiseContext = true;
#endif
            return new BitReader(this);
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
        public unsafe bool TryBeginRead(int bytes)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (Handle->InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif
            if (Handle->Position + bytes > Handle->Length)
            {
                return false;
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Handle->AllowedReadMark = Handle->Position + bytes;
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
            if (Handle->InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif
            int len = sizeof(T);
            if (Handle->Position + len > Handle->Length)
            {
                return false;
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Handle->AllowedReadMark = Handle->Position + len;
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
        internal unsafe bool TryBeginReadInternal(int bytes)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (Handle->InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif
            if (Handle->Position + bytes > Handle->Length)
            {
                return false;
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (Handle->Position + bytes > Handle->AllowedReadMark)
            {
                Handle->AllowedReadMark = Handle->Position + bytes;
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
                UnsafeUtility.MemCpy(b, Handle->BufferPointer, Length);
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
            return Handle->BufferPointer;
        }

        /// <summary>
        /// Gets a direct pointer to the underlying buffer at the current read position
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe byte* GetUnsafePtrAtCurrentPosition()
        {
            return Handle->BufferPointer + Handle->Position;
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
            var bufferSerializer = new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(this));
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
            for (var i = 0; i < size; ++i)
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
            if (Handle->InBitwiseContext)
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
            if (Handle->InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif

            if (!TryBeginReadInternal(sizeof(int)))
            {
                throw new OverflowException("Reading past the end of the buffer");
            }
            ReadValue(out int sizeInTs);
            int sizeInBytes = sizeInTs * sizeof(T);
            if (!TryBeginReadInternal(sizeInBytes))
            {
                throw new OverflowException("Reading past the end of the buffer");
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
            if (Handle->InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
            if (Handle->Position + bytesToRead > Handle->AllowedReadMark)
            {
                throw new OverflowException($"Attempted to read without first calling {nameof(TryBeginRead)}()");
            }
#endif

            var val = new T();
            byte* ptr = ((byte*)&val) + offsetBytes;
            byte* bufferPointer = Handle->BufferPointer + Handle->Position;
            UnsafeUtility.MemCpy(ptr, bufferPointer, bytesToRead);

            Handle->Position += bytesToRead;
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
            if (Handle->InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
            if (Handle->Position + 1 > Handle->AllowedReadMark)
            {
                throw new OverflowException($"Attempted to read without first calling {nameof(TryBeginRead)}()");
            }
#endif
            value = Handle->BufferPointer[Handle->Position++];
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
            if (Handle->InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif

            if (!TryBeginReadInternal(1))
            {
                throw new OverflowException("Reading past the end of the buffer");
            }
            value = Handle->BufferPointer[Handle->Position++];
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
            if (Handle->InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
            if (Handle->Position + size > Handle->AllowedReadMark)
            {
                throw new OverflowException($"Attempted to read without first calling {nameof(TryBeginRead)}()");
            }
#endif
            UnsafeUtility.MemCpy(value + offset, (Handle->BufferPointer + Handle->Position), size);
            Handle->Position += size;
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
            if (Handle->InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif

            if (!TryBeginReadInternal(size))
            {
                throw new OverflowException("Reading past the end of the buffer");
            }
            UnsafeUtility.MemCpy(value + offset, (Handle->BufferPointer + Handle->Position), size);
            Handle->Position += size;
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
            if (Handle->InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
            if (Handle->Position + len > Handle->AllowedReadMark)
            {
                throw new OverflowException($"Attempted to read without first calling {nameof(TryBeginRead)}()");
            }
#endif

            fixed (T* ptr = &value)
            {
                UnsafeUtility.MemCpy((byte*)ptr, Handle->BufferPointer + Handle->Position, len);
            }
            Handle->Position += len;
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
            if (Handle->InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif

            if (!TryBeginReadInternal(len))
            {
                throw new OverflowException("Reading past the end of the buffer");
            }


            fixed (T* ptr = &value)
            {
                UnsafeUtility.MemCpy((byte*)ptr, Handle->BufferPointer + Handle->Position, len);
            }
            Handle->Position += len;
        }
    }
}
