using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Netcode
{
    public struct FastBufferWriter : IDisposable
    {
        private NativeArray<byte> m_buffer;

        internal struct InternalData
        {
            public unsafe byte* BufferPointer;
            public int Position;
            public int Length;
            public int Capacity;
            public int MaxCapacity;
            public Allocator Allocator;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            public int AllowedWriteMark;
            public bool InBitwiseContext;
#endif
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void CommitBitwiseWrites(int amount)
            {
                Position += amount;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                InBitwiseContext = false;
#endif
            }
        }

        private InternalData m_InternalData;

        public unsafe FastBufferWriter(int size, Allocator allocator, int maxSize = -1)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            var buffer = new NativeArray<byte>(size, allocator, NativeArrayOptions.ClearMemory);
#else
            var buffer = new NativeArray<byte>(size, allocator, NativeArrayOptions.UninitializedMemory);
#endif
            m_buffer = buffer;
            m_InternalData = new InternalData
            {
                BufferPointer = (byte*) m_buffer.GetUnsafePtr(),
                Position = 0,
                Length = 0,
                Capacity = buffer.Length,
                Allocator = allocator,
                MaxCapacity = maxSize == -1 ? size : maxSize,
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                AllowedWriteMark = 0,
                InBitwiseContext = false,
#endif
            };
        }

        public void Dispose()
        {
            m_buffer.Dispose();
        }

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
            if (m_InternalData.Position > m_InternalData.Length && where < m_InternalData.Position)
            {
                m_InternalData.Length = m_InternalData.Position;
            }
            m_InternalData.Position = where;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Truncate(int where = -1)
        {
            if (where == -1)
            {
                where = Position;
            }

            if (m_InternalData.Position > where)
            {
                m_InternalData.Position = where;
            }
            if(m_InternalData.Length > where)
            {
                m_InternalData.Length = where;
            }
        }

        public unsafe BitWriter EnterBitwiseContext()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_InternalData.InBitwiseContext = true;
#endif
            return new BitWriter(ref m_InternalData);
        }
        
        public int Position => m_InternalData.Position;
        public int Capacity => m_InternalData.Capacity;
        public int Length => m_InternalData.Position > m_InternalData.Length ? m_InternalData.Position : m_InternalData.Length;

        private unsafe void Grow()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            var buffer = new NativeArray<byte>(Math.Min(m_InternalData.Capacity * 2, m_InternalData.MaxCapacity), m_InternalData.Allocator, NativeArrayOptions.ClearMemory);
#else
            var buffer = new NativeArray<byte>(m_InternalData.Capacity * 2, m_InternalData.Allocator, NativeArrayOptions.UninitializedMemory);
#endif
            UnsafeUtility.MemCpy(buffer.GetUnsafePtr(), m_InternalData.BufferPointer, Length);
            m_buffer.Dispose();
            m_buffer = buffer;
            m_InternalData.BufferPointer = (byte*) m_buffer.GetUnsafePtr();
            m_InternalData.Capacity = buffer.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool VerifyCanWrite(int bytes)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif
            if (m_InternalData.Position + bytes > m_InternalData.Capacity)
            {
                if (m_InternalData.Capacity < m_InternalData.MaxCapacity)
                {
                    Grow();
                }
                else
                {
                    return false;
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_InternalData.AllowedWriteMark = m_InternalData.Position + bytes;
#endif
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool VerifyCanWriteValue<T>(in T value) where T : unmanaged
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif
            int len = sizeof(T);
            if (m_InternalData.Position + len > m_InternalData.Capacity)
            {
                if (m_InternalData.Capacity < m_InternalData.MaxCapacity)
                {
                    Grow();
                }
                else
                {
                    return false;
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_InternalData.AllowedWriteMark = m_InternalData.Position + len;
#endif
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArray<byte> GetNativeArray()
        {
            return m_buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ToArray()
        {
            return m_buffer.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe byte* GetUnsafePtr()
        {
            return m_InternalData.BufferPointer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe byte* GetUnsafePtrAtCurrentPosition()
        {
            return m_InternalData.BufferPointer + m_InternalData.Position;
        }

        /// <summary>
        /// Writes a boxed object in a standard format
        /// Named differently from other WriteValue methods to avoid accidental boxing
        /// </summary>
        /// <param name="value">The object to write</param>
        public void WriteObject(object value, bool isNullable = false)
        {
            if (isNullable || value.GetType().IsNullable())
            {
                bool isNull = value == null || (value is UnityEngine.Object && ((UnityEngine.Object)value) == null);

                WriteValueSafe(isNull);

                if (isNull)
                {
                    return;
                }
            }
            
            var type = value.GetType();
            var hasSerializer = SerializationTypeTable.Serializers.TryGetValue(type, out var serializer);
            if (hasSerializer)
            {
                serializer(ref this, value);
                return;
            }
            
            if (value is Array array)
            {
                WriteValueSafe(array.Length);

                for (int i = 0; i < array.Length; i++)
                {
                    WriteObject(array.GetValue(i));
                }

                return;
            }
            
            if (value.GetType().IsEnum)
            {
                switch (Convert.GetTypeCode(value))
                {
                    case TypeCode.Boolean:
                        WriteValueSafe((byte)value);
                        break;
                    case TypeCode.Char:
                        WriteValueSafe((char)value);
                        break;
                    case TypeCode.SByte:
                        WriteValueSafe((sbyte)value);
                        break;
                    case TypeCode.Byte:
                        WriteValueSafe((byte)value);
                        break;
                    case TypeCode.Int16:
                        WriteValueSafe((short)value);
                        break;
                    case TypeCode.UInt16:
                        WriteValueSafe((ushort)value);
                        break;
                    case TypeCode.Int32:
                        WriteValueSafe((int)value);
                        break;
                    case TypeCode.UInt32:
                        WriteValueSafe((uint)value);
                        break;
                    case TypeCode.Int64:
                        WriteValueSafe((long)value);
                        break;
                    case TypeCode.UInt64:
                        WriteValueSafe((ulong)value);
                        break;
                }
                return;
            }
            if (value is GameObject)
            {
                var networkObject = ((GameObject)value).GetComponent<NetworkObject>();
                if (networkObject == null)
                {
                    throw new ArgumentException($"{nameof(NetworkWriter)} cannot write {nameof(GameObject)} types that does not has a {nameof(NetworkObject)} component attached. {nameof(GameObject)}: {((GameObject)value).name}");
                }

                if (!networkObject.IsSpawned)
                {
                    throw new ArgumentException($"{nameof(NetworkWriter)} cannot write {nameof(NetworkObject)} types that are not spawned. {nameof(GameObject)}: {((GameObject)value).name}");
                }

                WriteValueSafe(networkObject.NetworkObjectId);
                return;
            }
            if (value is NetworkObject)
            {
                if (!((NetworkObject)value).IsSpawned)
                {
                    throw new ArgumentException($"{nameof(NetworkWriter)} cannot write {nameof(NetworkObject)} types that are not spawned. {nameof(GameObject)}: {((NetworkObject)value).gameObject.name}");
                }

                WriteValueSafe(((NetworkObject)value).NetworkObjectId);
                return;
            }
            if (value is NetworkBehaviour)
            {
                if (!((NetworkBehaviour)value).HasNetworkObject || !((NetworkBehaviour)value).NetworkObject.IsSpawned)
                {
                    throw new ArgumentException($"{nameof(NetworkWriter)} cannot write {nameof(NetworkBehaviour)} types that are not spawned. {nameof(GameObject)}: {((NetworkBehaviour)value).gameObject.name}");
                }

                if (!VerifyCanWrite(sizeof(ulong) * 2))
                {
                    throw new OverflowException("Writing past the end of the buffer");
                }
                WriteValue(((NetworkBehaviour)value).NetworkObjectId);
                WriteValue(((NetworkBehaviour)value).NetworkBehaviourId);
                return;
            }
            if (value is INetworkSerializable)
            {
                //TODO ((INetworkSerializable)value).NetworkSerialize(new NetworkSerializer(this));
                return;
            }

            throw new ArgumentException($"{nameof(NetworkWriter)} cannot write type {value.GetType().Name} - it does not implement {nameof(INetworkSerializable)}");
        }

        public void WriteValue<T>(T value) where T : INetworkSerializable
        {
            // TODO
        }

        public void WriteValue(GameObject value)
        {
            var networkObject = ((GameObject)value).GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                throw new ArgumentException($"{nameof(NetworkWriter)} cannot write {nameof(GameObject)} types that does not has a {nameof(NetworkObject)} component attached. {nameof(GameObject)}: {((GameObject)value).name}");
            }

            if (!networkObject.IsSpawned)
            {
                throw new ArgumentException($"{nameof(NetworkWriter)} cannot write {nameof(NetworkObject)} types that are not spawned. {nameof(GameObject)}: {((GameObject)value).name}");
            }

            WriteValue(networkObject.NetworkObjectId);
        }
        
        public void WriteValueSafe(GameObject value)
        {
            var networkObject = ((GameObject)value).GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                throw new ArgumentException($"{nameof(NetworkWriter)} cannot write {nameof(GameObject)} types that does not has a {nameof(NetworkObject)} component attached. {nameof(GameObject)}: {((GameObject)value).name}");
            }

            if (!networkObject.IsSpawned)
            {
                throw new ArgumentException($"{nameof(NetworkWriter)} cannot write {nameof(NetworkObject)} types that are not spawned. {nameof(GameObject)}: {((GameObject)value).name}");
            }

            WriteValueSafe(networkObject.NetworkObjectId);
        }
        
        public static int GetWriteSize(GameObject value)
        {
            return sizeof(ulong);
        }

        public void WriteValue(in NetworkObject value)
        {
            if (!value.IsSpawned)
            {
                throw new ArgumentException($"{nameof(NetworkWriter)} cannot write {nameof(NetworkObject)} types that are not spawned. {nameof(GameObject)}: {value.name}");
            }

            WriteValue(value.NetworkObjectId);
        }

        public void WriteValueSafe(NetworkObject value)
        {
            if (!value.IsSpawned)
            {
                throw new ArgumentException($"{nameof(NetworkWriter)} cannot write {nameof(NetworkObject)} types that are not spawned. {nameof(GameObject)}: {value.name}");
            }
            WriteValueSafe(value.NetworkObjectId);
        }
        
        public static int GetWriteSize(NetworkObject value)
        {
            return sizeof(ulong);
        }

        public void WriteValue(NetworkBehaviour value)
        {
            if (!value.HasNetworkObject || !value.NetworkObject.IsSpawned)
            {
                throw new ArgumentException($"{nameof(NetworkWriter)} cannot write {nameof(NetworkBehaviour)} types that are not spawned. {nameof(GameObject)}: {((NetworkBehaviour)value).gameObject.name}");
            }

            WriteValue(value.NetworkObjectId);
            WriteValue(value.NetworkBehaviourId);
        }

        public void WriteValueSafe(NetworkBehaviour value)
        {
            if (!value.HasNetworkObject || !value.NetworkObject.IsSpawned)
            {
                throw new ArgumentException($"{nameof(NetworkWriter)} cannot write {nameof(NetworkBehaviour)} types that are not spawned. {nameof(GameObject)}: {((NetworkBehaviour)value).gameObject.name}");
            }

            if (!VerifyCanWrite(sizeof(ulong) + sizeof(ushort)))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            WriteValue(value.NetworkObjectId);
            WriteValue(value.NetworkBehaviourId);
        }
        
        public static int GetWriteSize(NetworkBehaviour value)
        {
            return sizeof(ulong) + sizeof(ushort);
        }
        
        // As it turns out, strings cannot be treated as char arrays, since strings use pointers to store data rather than C# arrays
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
                    if (oneByteChars)
                    {
                        WriteByte((byte) s[i]);
                    }
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

        // As it turns out, strings cannot be treated as char arrays, since strings use pointers to store data rather than C# arrays
        /// <summary>
        /// Writes a string
        /// </summary>
        /// <param name="s">The string to write</param>
        /// <param name="oneByteChars">Whether or not to use one byte per character. This will only allow ASCII</param>
        public unsafe void WriteValueSafe(string s, bool oneByteChars = false)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif
            
            int sizeInBytes = GetWriteSize(s, oneByteChars);
            
            if (!VerifyCanWrite(sizeInBytes))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            
            WriteValue((uint)s.Length);
            int target = s.Length;
            if (oneByteChars)
            {
                for (int i = 0; i < target; ++i)
                {
                    if (oneByteChars)
                    {
                        WriteByte((byte) s[i]);
                    }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetWriteSize(string s, bool oneByteChars = false)
        {
            return sizeof(int) + s.Length * (oneByteChars ? sizeof(byte) : sizeof(char));
        }

        /// <summary>
        /// Writes an unmanaged array
        /// </summary>
        /// <param name="array">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        /// <param name="offset">Where in the array to start</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteValue<T>(T[] array, int count = -1, int offset = 0) where T: unmanaged
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
        /// </summary>
        /// <param name="array">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        /// <param name="offset">Where in the array to start</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteValueSafe<T>(T[] array, int count = -1, int offset = 0) where T: unmanaged
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif
            
            int sizeInTs = count != -1 ? count : array.Length - offset;
            int sizeInBytes = sizeInTs * sizeof(T);
            
            if (!VerifyCanWrite(sizeInBytes + sizeof(int)))
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int GetWriteSize<T>(T[] array, int count = -1, int offset = 0) where T: unmanaged
        {
            int sizeInTs = count != -1 ? count : array.Length - offset;
            int sizeInBytes = sizeInTs * sizeof(T);
            return sizeof(int) + sizeInBytes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WritePartialValue<T>(T value, int bytesToWrite, int offsetBytes = 0) where T: unmanaged
        {
            // Switch statement to write small values with assignments
            // is considerably faster than calling UnsafeUtility.MemCpy
            // in all builds - editor, mono, and ILCPP
            
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
            if (m_InternalData.Position + bytesToWrite > m_InternalData.AllowedWriteMark)
            {
                throw new OverflowException("Attempted to write without first calling VerifyCanWrite()");
            }
#endif

            byte* ptr = ((byte*) &value) + offsetBytes;
            byte* bufferPointer = m_InternalData.BufferPointer + m_InternalData.Position;
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
                    *(uint*) bufferPointer = *(uint*)ptr;
                    *(ushort*) (bufferPointer+4) = *(ushort*)(ptr+4);
                    break;
                case 7:
                    *(uint*) bufferPointer = *(uint*)ptr;
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

            m_InternalData.Position += bytesToWrite;
        }

        /// <summary>
        /// Write a byte to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteByte(byte value)
        {      
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
            if (m_InternalData.Position + 1 > m_InternalData.AllowedWriteMark)
            {
                throw new OverflowException("Attempted to write without first calling VerifyCanWrite()");
            }
#endif
            m_InternalData.BufferPointer[m_InternalData.Position++] = value;
        }

        /// <summary>
        /// Write a byte to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteByteSafe(byte value)
        {      
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif
            
            if (!VerifyCanWrite(1))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            m_InternalData.BufferPointer[m_InternalData.Position++] = value;
        }
        
        /// <summary>
        /// Write multiple bytes to the stream
        /// </summary>
        /// <param name="value">Value to write</param>
        /// <param name="size">Number of bytes to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteBytes(byte* value, int size, int offset = 0)
        {      
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
            if (m_InternalData.Position + size > m_InternalData.AllowedWriteMark)
            {
                throw new OverflowException("Attempted to write without first calling VerifyCanWrite()");
            }
#endif
            UnsafeUtility.MemCpy((m_InternalData.BufferPointer + m_InternalData.Position), value + offset, size);
            m_InternalData.Position += size;
        }
        
        /// <summary>
        /// Write multiple bytes to the stream
        /// </summary>
        /// <param name="value">Value to write</param>
        /// <param name="size">Number of bytes to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteBytesSafe(byte* value, int size, int offset = 0)
        {      
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif
            
            if (!VerifyCanWrite(size))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            UnsafeUtility.MemCpy((m_InternalData.BufferPointer + m_InternalData.Position), value + offset, size);
            m_InternalData.Position += size;
        }
        
        /// <summary>
        /// Write multiple bytes to the stream
        /// </summary>
        /// <param name="value">Value to write</param>
        /// <param name="size">Number of bytes to write</param>
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
        /// </summary>
        /// <param name="value">Value to write</param>
        /// <param name="size">Number of bytes to write</param>
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
            other.WriteBytes(m_InternalData.BufferPointer, m_InternalData.Position);
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
            WriteBytes(other.m_InternalData.BufferPointer, other.m_InternalData.Position);
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
            
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
            if (m_InternalData.Position + len > m_InternalData.AllowedWriteMark)
            {
                throw new OverflowException("Attempted to write without first calling VerifyCanWrite()");
            }
#endif
            
            T* pointer = (T*)(m_InternalData.BufferPointer+m_InternalData.Position);
            *pointer = value;
            m_InternalData.Position += len;
        }
        
        /// <summary>
        /// Write a value of any unmanaged type to the buffer.
        /// It will be copied into the buffer exactly as it exists in memory.
        /// </summary>
        /// <param name="value">The value to copy</param>
        /// <typeparam name="T">Any unmanaged type</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteValueSafe<T>(in T value) where T : unmanaged
        {
            int len = sizeof(T);
            
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif
            
            if (!VerifyCanWrite(len))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            
            T* pointer = (T*)(m_InternalData.BufferPointer+m_InternalData.Position);
            *pointer = value;
            m_InternalData.Position += len;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int GetWriteSize<T>(in T value) where T : unmanaged
        {
            return sizeof(T);
        }
    }
}