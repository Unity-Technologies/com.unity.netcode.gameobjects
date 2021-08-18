using System;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Netcode
{
    public struct FastBufferReader
    {
        private NativeArray<byte> m_buffer;

        internal struct InternalData
        {
            public unsafe byte* BufferPointer;
            public int Position;
            public int Length;
            public int BitPosition;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            public int AllowedReadMark;
            public int AllowedBitwiseReadMark;
            public bool InBitwiseContext;
#endif
            /// <summary>
            /// Whether or not the current BitPosition is evenly divisible by 8. I.e. whether or not the BitPosition is at a byte boundary.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool BitAligned()
            {
                return (BitPosition & 7) == 0;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void CommitBitwiseReads(int amount)
            {
                Position += amount;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                InBitwiseContext = false;
#endif
            }
        }

        private InternalData m_InternalData;

        public unsafe FastBufferReader(NativeArray<byte> buffer, int offset = 0, int length = -1)
        {
            m_buffer = buffer;
            m_InternalData = new InternalData
            {
                BufferPointer = (byte*) m_buffer.GetUnsafePtr(),
                Position = offset,
                Length = length == -1 ? buffer.Length : length,
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                AllowedReadMark = 0,
                InBitwiseContext = false,
#endif
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Seek(int where)
        {
            m_InternalData.Position = where;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void MarkBytesRead(int amount)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
            if (m_InternalData.Position + amount > m_InternalData.AllowedReadMark)
            {
                throw new OverflowException("Attempted to read without first calling VerifyCanRead()");
            }
#endif
            m_InternalData.Position += amount;
        }

        public unsafe BitReader EnterBitwiseContext()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_InternalData.InBitwiseContext = true;
#endif
            return new BitReader(ref m_InternalData);
        }
        
        public int Position => m_InternalData.Position;
        public int Length => m_InternalData.Length;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool VerifyCanReadInternal(int bytes)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif
            if (m_InternalData.Position + bytes > m_InternalData.Length)
            {
                return false;
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.Position + bytes > m_InternalData.AllowedReadMark)
            {
                m_InternalData.AllowedReadMark = m_InternalData.Position + bytes;
            }
#endif
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool VerifyCanRead(int bytes)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif
            if (m_InternalData.Position + bytes > m_InternalData.Length)
            {
                return false;
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_InternalData.AllowedReadMark = m_InternalData.Position + bytes;
#endif
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool VerifyCanReadValue<T>(in T value) where T : unmanaged
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif
            int len = sizeof(T);
            if (m_InternalData.Position + len > m_InternalData.Length)
            {
                return false;
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_InternalData.AllowedReadMark = m_InternalData.Position + len;
#endif
            return true;
        }
        public bool VerifyCanReadBits(int bitCount)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (!m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bitwise mode while not in a bitwise context.");
            }
#endif
            var newBitPosition = m_InternalData.BitPosition + bitCount;
            var totalBytesWrittenInBitwiseContext = newBitPosition >> 3;
            if ((newBitPosition & 7) != 0)
            {
                // Accounting for the partial read
                ++totalBytesWrittenInBitwiseContext;
            }

            if (m_InternalData.Position + totalBytesWrittenInBitwiseContext > m_InternalData.Length)
            {
                return false;
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_InternalData.AllowedBitwiseReadMark = newBitPosition;
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
        /// Reads a boxed object in a standard format
        /// Named differently from other ReadValue methods to avoid accidental boxing
        /// </summary>
        /// <param name="value">The object to read</param>
        /// <param name="type">The type to be read</param>
        /// <param name="isNullable">Should a null value be encoded? Any type for which type.IsNullable() returns true will always encode it.</param>
        public void ReadObject(out object value, Type type, bool isNullable = false)
        {
            if (isNullable || type.IsNullable())
            {
                ReadValueSafe(out bool isNull);

                if (isNull)
                {
                    value = null;
                    return;
                }
            }
            
            var hasDeserializer = SerializationTypeTable.Deserializers.TryGetValue(type, out var deserializer);
            if (hasDeserializer)
            {
                deserializer(ref this, out value);
                return;
            }
            
            if (type.IsArray && type.HasElementType)
            {
                ReadValueSafe(out int length);

                var arr = Array.CreateInstance(type.GetElementType(), length);

                for (int i = 0; i < length; i++)
                {
                    ReadObject(out object item, type.GetElementType());
                    arr.SetValue(item, i);
                }

                value = arr;
                return;
            }
            
            if (type.IsEnum)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean:
                        ReadValueSafe(out byte boolVal);
                        value = Enum.ToObject(type, boolVal != 0);
                        return;
                    case TypeCode.Char:
                        ReadValueSafe(out char charVal);
                        value = Enum.ToObject(type, charVal);
                        return;
                    case TypeCode.SByte:
                        ReadValueSafe(out sbyte sbyteVal);
                        value = Enum.ToObject(type, sbyteVal);
                        return;
                    case TypeCode.Byte:
                        ReadValueSafe(out byte byteVal);
                        value = Enum.ToObject(type, byteVal);
                        return;
                    case TypeCode.Int16:
                        ReadValueSafe(out short shortVal);
                        value = Enum.ToObject(type, shortVal);
                        return;
                    case TypeCode.UInt16:
                        ReadValueSafe(out ushort ushortVal);
                        value = Enum.ToObject(type, ushortVal);
                        return;
                    case TypeCode.Int32:
                        ReadValueSafe(out int intVal);
                        value = Enum.ToObject(type, intVal);
                        return;
                    case TypeCode.UInt32:
                        ReadValueSafe(out uint uintVal);
                        value = Enum.ToObject(type, uintVal);
                        return;
                    case TypeCode.Int64:
                        ReadValueSafe(out long longVal);
                        value = Enum.ToObject(type, longVal);
                        return;
                    case TypeCode.UInt64:
                        ReadValueSafe(out ulong ulongVal);
                        value = Enum.ToObject(type, ulongVal);
                        return;
                }
            }
            
            if (type == typeof(GameObject))
            {
                ReadValueSafe(out GameObject go);
                value = go;
                return;
            }

            if (type == typeof(NetworkObject))
            {
                ReadValueSafe(out NetworkObject no);
                value = no;
                return;
            }

            if (typeof(NetworkBehaviour).IsAssignableFrom(type))
            {
                ReadValueSafe(out NetworkBehaviour nb);
                value = nb;
                return;
            }
            /*if (value is INetworkSerializable)
            {
                //TODO ((INetworkSerializable)value).NetworkSerialize(new NetworkSerializer(this));
                return;
            }*/

            throw new ArgumentException($"{nameof(FastBufferReader)} cannot read type {type.Name} - it does not implement {nameof(INetworkSerializable)}");
        }

        /*public void ReadValue<T>(ref T value) where T : INetworkSerializable
        {
            // TODO
        }*/

        public void ReadValue(out GameObject value)
        {
            ReadValue(out ulong networkObjectId);
            
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
            {
                value = networkObject.gameObject;
                return;
            }

            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
            {
                NetworkLog.LogWarning($"{nameof(FastBufferReader)} cannot find the {nameof(GameObject)} sent in the {nameof(NetworkSpawnManager.SpawnedObjects)} list, it may have been destroyed. {nameof(networkObjectId)}: {networkObjectId}");
            }

            value = null;
        }
        
        public void ReadValueSafe(out GameObject value)
        {
            ReadValueSafe(out ulong networkObjectId);
            
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
            {
                value = networkObject.gameObject;
                return;
            }

            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
            {
                NetworkLog.LogWarning($"{nameof(FastBufferReader)} cannot find the {nameof(GameObject)} sent in the {nameof(NetworkSpawnManager.SpawnedObjects)} list, it may have been destroyed. {nameof(networkObjectId)}: {networkObjectId}");
            }

            value = null;
        }

        public void ReadValue(out NetworkObject value)
        {
            ReadValue(out ulong networkObjectId);
            
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
            {
                value = networkObject;
                return;
            }

            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
            {
                NetworkLog.LogWarning($"{nameof(FastBufferReader)} cannot find the {nameof(GameObject)} sent in the {nameof(NetworkSpawnManager.SpawnedObjects)} list, it may have been destroyed. {nameof(networkObjectId)}: {networkObjectId}");
            }

            value = null;
        }

        public void ReadValueSafe(out NetworkObject value)
        {
            ReadValueSafe(out ulong networkObjectId);
            
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
            {
                value = networkObject;
                return;
            }

            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
            {
                NetworkLog.LogWarning($"{nameof(FastBufferReader)} cannot find the {nameof(GameObject)} sent in the {nameof(NetworkSpawnManager.SpawnedObjects)} list, it may have been destroyed. {nameof(networkObjectId)}: {networkObjectId}");
            }

            value = null;
        }

        public void ReadValue(out NetworkBehaviour value)
        {
            ReadValue(out ulong networkObjectId);
            ReadValue(out ushort networkBehaviourId);
            
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
            {
                value = networkObject.GetNetworkBehaviourAtOrderIndex(networkBehaviourId);
                return;
            }

            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
            {
                NetworkLog.LogWarning($"{nameof(FastBufferReader)} cannot find the {nameof(NetworkBehaviour)} sent in the {nameof(NetworkSpawnManager.SpawnedObjects)} list, it may have been destroyed. {nameof(networkObjectId)}: {networkObjectId}");
            }

            value = null;
        }

        public void ReadValueSafe(out NetworkBehaviour value)
        {
            ReadValueSafe(out ulong networkObjectId);
            ReadValueSafe(out ushort networkBehaviourId);
            
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
            {
                value = networkObject.GetNetworkBehaviourAtOrderIndex(networkBehaviourId);
                return;
            }

            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
            {
                NetworkLog.LogWarning($"{nameof(FastBufferReader)} cannot find the {nameof(NetworkBehaviour)} sent in the {nameof(NetworkSpawnManager.SpawnedObjects)} list, it may have been destroyed. {nameof(networkObjectId)}: {networkObjectId}");
            }

            value = null;
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
                        if (oneByteChars)
                        {
                            ReadByte(out byte b); 
                            native[i] = (char) b;
                        }
                    }
                }
                else
                {
                    ReadBytes((byte*) native, target * sizeof(char));
                }
            }
        }

        /// <summary>
        /// Reads a string.
        /// NOTE: ALLOCATES
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple reads at once by calling VerifyCanRead.
        /// </summary>
        /// <param name="s">Stores the read string</param>
        /// <param name="oneByteChars">Whether or not to use one byte per character. This will only allow ASCII</param>
        public unsafe void ReadValueSafe(out string s, bool oneByteChars = false)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif
            
            if (!VerifyCanReadInternal(sizeof(uint)))
            {
                throw new OverflowException("Reading past the end of the buffer");
            }
            
            ReadValue(out uint length);
            
            if (!VerifyCanReadInternal((int)length * (oneByteChars ? 1 : sizeof(char))))
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
                        if (oneByteChars)
                        {
                            ReadByte(out byte b); 
                            native[i] = (char) b;
                        }
                    }
                }
                else
                {
                    ReadBytes((byte*) native, target * sizeof(char));
                }
            }
        }

        /// <summary>
        /// Writes an unmanaged array
        /// NOTE: ALLOCATES
        /// </summary>
        /// <param name="array">Stores the read array</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadValue<T>(out T[] array) where T: unmanaged
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
        /// for multiple reads at once by calling VerifyCanRead.
        /// </summary>
        /// <param name="array">Stores the read array</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadValueSafe<T>(out T[] array) where T: unmanaged
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif
            
            if (!VerifyCanReadInternal(sizeof(int)))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            ReadValue(out int sizeInTs);
            int sizeInBytes = sizeInTs * sizeof(T);
            if (!VerifyCanReadInternal(sizeInBytes))
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
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadPartialValue<T>(out T value, int bytesToRead, int offsetBytes = 0) where T: unmanaged
        {
            // Switch statement to read small values with assignments
            // is considerably faster than calling UnsafeUtility.MemCpy
            // in all builds - editor, mono, and ILCPP
            
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
            if (m_InternalData.Position + bytesToRead > m_InternalData.AllowedReadMark)
            {
                throw new OverflowException("Attempted to read without first calling VerifyCanRead()");
            }
#endif

            T val = new T();
            byte* ptr = ((byte*) &val) + offsetBytes;
            byte* bufferPointer = m_InternalData.BufferPointer + m_InternalData.Position;
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
                    *(uint*) ptr = *(uint*)bufferPointer;
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

            m_InternalData.Position += bytesToRead;
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
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
            if (m_InternalData.Position + 1 > m_InternalData.AllowedReadMark)
            {
                throw new OverflowException("Attempted to read without first calling VerifyCanRead()");
            }
#endif
            value = m_InternalData.BufferPointer[m_InternalData.Position++];
        }

        /// <summary>
        /// Read a byte to the stream.
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple reads at once by calling VerifyCanRead.
        /// </summary>
        /// <param name="value">Stores the read value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadByteSafe(out byte value)
        {      
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif
            
            if (!VerifyCanReadInternal(1))
            {
                throw new OverflowException("Reading past the end of the buffer");
            }
            value = m_InternalData.BufferPointer[m_InternalData.Position++];
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
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
            if (m_InternalData.Position + size > m_InternalData.AllowedReadMark)
            {
                throw new OverflowException("Attempted to read without first calling VerifyCanRead()");
            }
#endif
            UnsafeUtility.MemCpy(value + offset, (m_InternalData.BufferPointer + m_InternalData.Position), size);
            m_InternalData.Position += size;
        }
        
        /// <summary>
        /// Read multiple bytes to the stream
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple reads at once by calling VerifyCanRead.
        /// </summary>
        /// <param name="value">Pointer to the destination buffer</param>
        /// <param name="size">Number of bytes to read - MUST BE &lt;= BUFFER SIZE</param>
        /// <param name="offset">Offset of the byte buffer to store into</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadBytesSafe(byte* value, int size, int offset = 0)
        {      
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif
            
            if (!VerifyCanReadInternal(size))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            UnsafeUtility.MemCpy(value + offset, (m_InternalData.BufferPointer + m_InternalData.Position), size);
            m_InternalData.Position += size;
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
        /// for multiple reads at once by calling VerifyCanRead.
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
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
            if (m_InternalData.Position + len > m_InternalData.AllowedReadMark)
            {
                throw new OverflowException("Attempted to write without first calling VerifyCanWrite()");
            }
#endif
            
            T* pointer = (T*)(m_InternalData.BufferPointer+m_InternalData.Position);
            value = *pointer;
            m_InternalData.Position += len;
        }
        
        /// <summary>
        /// Read a value of any unmanaged type to the buffer.
        /// It will be copied from the buffer exactly as it existed in memory on the writing end.
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple reads at once by calling VerifyCanRead.
        /// </summary>
        /// <param name="value">The read value</param>
        /// <typeparam name="T">Any unmanaged type</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadValueSafe<T>(out T value) where T : unmanaged
        {
            int len = sizeof(T);
            
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InternalData.InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferReader in bytewise mode while in a bitwise context.");
            }
#endif
            
            if (!VerifyCanReadInternal(len))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            
            T* pointer = (T*)(m_InternalData.BufferPointer+m_InternalData.Position);
            value = *pointer;
            m_InternalData.Position += len;
        }
    }
}