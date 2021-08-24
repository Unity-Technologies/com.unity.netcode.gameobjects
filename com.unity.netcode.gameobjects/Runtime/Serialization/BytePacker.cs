using System;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Netcode
{
    /// <summary>
    /// Utility class for packing values in serialization.
    /// </summary>
    public static class BytePacker
    {
        #region Managed TypePacking

        /// <summary>
        /// Writes a boxed object in a packed format
        /// Named differently from other WriteValuePacked methods to avoid accidental boxing.
        /// Don't use this method unless you have no other choice.
        /// </summary>
        /// <param name="writer">Writer to write to</param>
        /// <param name="value">The object to write</param>
        /// <param name="isNullable">
        /// If true, an extra byte will be written to indicate whether or not the value is null.
        /// Some types will always write this.
        /// </param>
        public static void WriteObjectPacked(ref FastBufferWriter writer, object value, bool isNullable = false)
        {
#if UNITY_NETCODE_DEBUG_NO_PACKING
            writer.WriteObject(value, isNullable);
            return;
#endif
            if (isNullable || value.GetType().IsNullable())
            {
                bool isNull = value == null || (value is UnityEngine.Object o && o == null);

                WriteValuePacked(ref writer, isNull);

                if (isNull)
                {
                    return;
                }
            }
            
            var type = value.GetType();
            var hasSerializer = SerializationTypeTable.SerializersPacked.TryGetValue(type, out var serializer);
            if (hasSerializer)
            {
                serializer(ref writer, value);
                return;
            }
            
            if (value is Array array)
            {
                WriteValuePacked(ref writer, array.Length);

                for (int i = 0; i < array.Length; i++)
                {
                    WriteObjectPacked(ref writer, array.GetValue(i));
                }
            }
            
            if (value.GetType().IsEnum)
            {
                switch (Convert.GetTypeCode(value))
                {
                    case TypeCode.Boolean:
                        WriteValuePacked(ref writer, (byte)value);
                        break;
                    case TypeCode.Char:
                        WriteValuePacked(ref writer, (char)value);
                        break;
                    case TypeCode.SByte:
                        WriteValuePacked(ref writer, (sbyte)value);
                        break;
                    case TypeCode.Byte:
                        WriteValuePacked(ref writer, (byte)value);
                        break;
                    case TypeCode.Int16:
                        WriteValuePacked(ref writer, (short)value);
                        break;
                    case TypeCode.UInt16:
                        WriteValuePacked(ref writer, (ushort)value);
                        break;
                    case TypeCode.Int32:
                        WriteValuePacked(ref writer, (int)value);
                        break;
                    case TypeCode.UInt32:
                        WriteValuePacked(ref writer, (uint)value);
                        break;
                    case TypeCode.Int64:
                        WriteValuePacked(ref writer, (long)value);
                        break;
                    case TypeCode.UInt64:
                        WriteValuePacked(ref writer, (ulong)value);
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

                WriteValuePacked(ref writer, networkObject.NetworkObjectId);
                return;
            }
            if (value is NetworkObject)
            {
                if (!((NetworkObject)value).IsSpawned)
                {
                    throw new ArgumentException($"{nameof(NetworkWriter)} cannot write {nameof(NetworkObject)} types that are not spawned. {nameof(GameObject)}: {((NetworkObject)value).gameObject.name}");
                }

                WriteValuePacked(ref writer, ((NetworkObject)value).NetworkObjectId);
                return;
            }
            if (value is NetworkBehaviour)
            {
                if (!((NetworkBehaviour)value).HasNetworkObject || !((NetworkBehaviour)value).NetworkObject.IsSpawned)
                {
                    throw new ArgumentException($"{nameof(NetworkWriter)} cannot write {nameof(NetworkBehaviour)} types that are not spawned. {nameof(GameObject)}: {((NetworkBehaviour)value).gameObject.name}");
                }

                WriteValuePacked(ref writer, ((NetworkBehaviour)value).NetworkObjectId);
                WriteValuePacked(ref writer, ((NetworkBehaviour)value).NetworkBehaviourId);
                return;
            }
            if (value is INetworkSerializable)
            {
                //TODO ((INetworkSerializable)value).NetworkSerialize(new NetworkSerializer(this));
                return;
            }

            throw new ArgumentException($"{nameof(NetworkWriter)} cannot write type {value.GetType().Name} - it does not implement {nameof(INetworkSerializable)}");
        }
        #endregion
        
        #region Unmanaged Type Packing
        
#if UNITY_NETCODE_DEBUG_NO_PACKING
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteValuePacked<T>(ref FastBufferWriter writer, T value) where T: unmanaged => writer.WriteValueSafe(value);
#else
        /// <summary>
        /// Write a packed enum value.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">The value to write</param>
        /// <typeparam name="TEnum">An enum type</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteValuePacked<TEnum>(ref FastBufferWriter writer, TEnum value) where TEnum : unmanaged, Enum
        {
            TEnum enumValue = value;
            switch (sizeof(TEnum))
            {
                case sizeof(int):
                    WriteValuePacked(ref writer, *(int*)&enumValue);
                    break;
                case sizeof(byte):
                    WriteValuePacked(ref writer, *(byte*)&enumValue);
                    break;
                case sizeof(short):
                    WriteValuePacked(ref writer, *(short*)&enumValue);
                    break;
                case sizeof(long):
                    WriteValuePacked(ref writer, *(long*)&enumValue);
                    break;
            }
        }
        
        /// <summary>
        /// Write single-precision floating point value to the buffer as a varint
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, float value)
        {
            WriteUInt32Packed(ref writer, ToUint(value));
        }

        /// <summary>
        /// Write double-precision floating point value to the buffer as a varint
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, double value)
        {
            WriteUInt64Packed(ref writer, ToUlong(value));
        }
        
        /// <summary>
        /// Write a byte to the buffer.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, byte value) => writer.WriteByteSafe(value);
        
        /// <summary>
        /// Write a signed byte to the buffer.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, sbyte value) => writer.WriteByteSafe((byte)value);
        
        /// <summary>
        /// Write a bool to the buffer.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, bool value) => writer.WriteValueSafe(value);


        /// <summary>
        /// Write a signed short (Int16) as a ZigZag encoded varint to the buffer.
        /// WARNING: If the value you're writing is > 2287, this will use MORE space
        /// (3 bytes instead of 2), and if your value is > 240 you'll get no savings at all.
        /// Only use this if you're certain your value will be small.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, short value) => WriteUInt32Packed(ref writer, (ushort)Arithmetic.ZigZagEncode(value));

        /// <summary>
        /// Write an unsigned short (UInt16) as a varint to the buffer.
        /// WARNING: If the value you're writing is > 2287, this will use MORE space
        /// (3 bytes instead of 2), and if your value is > 240 you'll get no savings at all.
        /// Only use this if you're certain your value will be small.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, ushort value) => WriteUInt32Packed(ref writer, value);

        /// <summary>
        /// Write a two-byte character as a varint to the buffer.
        /// WARNING: If the value you're writing is > 2287, this will use MORE space
        /// (3 bytes instead of 2), and if your value is > 240 you'll get no savings at all.
        /// Only use this if you're certain your value will be small.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="c">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, char c) => WriteUInt32Packed(ref writer, c);

        /// <summary>
        /// Write a signed int (Int32) as a ZigZag encoded varint to the buffer.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, int value) => WriteUInt32Packed(ref writer, (uint)Arithmetic.ZigZagEncode(value));

        /// <summary>
        /// Write an unsigned int (UInt32) to the buffer.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, uint value) => WriteUInt32Packed(ref writer, value);

        /// <summary>
        /// Write an unsigned long (UInt64) to the buffer.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, ulong value) => WriteUInt64Packed(ref writer, value);

        /// <summary>
        /// Write a signed long (Int64) as a ZigZag encoded varint to the buffer.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, long value) => WriteUInt64Packed(ref writer, Arithmetic.ZigZagEncode(value));

        /// <summary>
        /// Convenience method that writes two packed Vector3 from the ray to the buffer
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="ray">Ray to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, Ray ray)
        {
            WriteValuePacked(ref writer, ray.origin);
            WriteValuePacked(ref writer, ray.direction);
        }

        /// <summary>
        /// Convenience method that writes two packed Vector2 from the ray to the buffer
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="ray2d">Ray2D to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, Ray2D ray2d)
        {
            WriteValuePacked(ref writer, ray2d.origin);
            WriteValuePacked(ref writer, ray2d.direction);
        }

        /// <summary>
        /// Convenience method that writes four varint floats from the color to the buffer
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="color">Color to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, Color color)
        {
            WriteValuePacked(ref writer, color.r);
            WriteValuePacked(ref writer, color.g);
            WriteValuePacked(ref writer, color.b);
            WriteValuePacked(ref writer, color.a);
        }

        /// <summary>
        /// Convenience method that writes four varint floats from the color to the buffer
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="color">Color to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, Color32 color)
        {
            WriteValuePacked(ref writer, color.r);
            WriteValuePacked(ref writer, color.g);
            WriteValuePacked(ref writer, color.b);
            WriteValuePacked(ref writer, color.a);
        }

        /// <summary>
        /// Convenience method that writes two varint floats from the vector to the buffer
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="vector2">Vector to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, Vector2 vector2)
        {
            WriteValuePacked(ref writer, vector2.x);
            WriteValuePacked(ref writer, vector2.y);
        }

        /// <summary>
        /// Convenience method that writes three varint floats from the vector to the buffer
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="vector3">Vector to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, Vector3 vector3)
        {
            WriteValuePacked(ref writer, vector3.x);
            WriteValuePacked(ref writer, vector3.y);
            WriteValuePacked(ref writer, vector3.z);
        }

        /// <summary>
        /// Convenience method that writes four varint floats from the vector to the buffer
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="vector4">Vector to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, Vector4 vector4)
        {
            WriteValuePacked(ref writer, vector4.x);
            WriteValuePacked(ref writer, vector4.y);
            WriteValuePacked(ref writer, vector4.z);
            WriteValuePacked(ref writer, vector4.w);
        }

        /// <summary>
        /// Writes the rotation to the buffer.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="rotation">Rotation to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, Quaternion rotation)
        {
            WriteValuePacked(ref writer, rotation.x);
            WriteValuePacked(ref writer, rotation.y);
            WriteValuePacked(ref writer, rotation.z);
            WriteValuePacked(ref writer, rotation.w);
        }

        /// <summary>
        /// Writes a string in a packed format
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="s">The value to pack</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, string s)
        {
            WriteValuePacked(ref writer, (uint)s.Length);
            int target = s.Length;
            for (int i = 0; i < target; ++i)
            {
                WriteValuePacked(ref writer, s[i]);
            }
        }
#endif
        #endregion
        
        #region Bit Packing

#if UNITY_NETCODE_DEBUG_NO_PACKING
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteValueBitPacked<T>(ref FastBufferWriter writer, T value) where T: unmanaged => writer.WriteValueSafe(value);
#else
        /// <summary>
        /// Writes a 14-bit signed short to the buffer in a bit-encoded packed format.
        /// The first bit indicates whether the value is 1 byte or 2.
        /// The sign bit takes up another bit.
        /// That leaves 14 bits for the value.
        /// A value greater than 2^14-1 or less than -2^14 will throw an exception in editor and development builds.
        /// In release builds builds the exception is not thrown and the value is truncated by losing its two
        /// most significant bits after zig-zag encoding.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">The value to pack</param>
        public static void WriteValueBitPacked(ref FastBufferWriter writer, short value) => WriteValueBitPacked(ref writer, (ushort) Arithmetic.ZigZagEncode(value));

        /// <summary>
        /// Writes a 15-bit unsigned short to the buffer in a bit-encoded packed format.
        /// The first bit indicates whether the value is 1 byte or 2.
        /// That leaves 15 bits for the value.
        /// A value greater than 2^15-1 will throw an exception in editor and development builds.
        /// In release builds builds the exception is not thrown and the value is truncated by losing its
        /// most significant bit.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">The value to pack</param>
        public static void WriteValueBitPacked(ref FastBufferWriter writer, ushort value)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (value >= 0b1000_0000_0000_0000)
            {
                throw new ArgumentException("BitPacked ushorts must be <= 15 bits");
            }
#endif
            
            if (value <= 0b0111_1111)
            {
                if (!writer.VerifyCanWriteInternal(1))
                {
                    throw new OverflowException("Writing past the end of the buffer");
                }
                writer.WriteByte((byte)(value << 1));
                return;
            }
            
            if (!writer.VerifyCanWriteInternal(2))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            writer.WriteValue((ushort)((value << 1) | 0b1));
        }

        /// <summary>
        /// Writes a 29-bit signed int to the buffer in a bit-encoded packed format.
        /// The first two bits indicate whether the value is 1, 2, 3, or 4 bytes.
        /// The sign bit takes up another bit.
        /// That leaves 29 bits for the value.
        /// A value greater than 2^29-1 or less than -2^29 will throw an exception in editor and development builds.
        /// In release builds builds the exception is not thrown and the value is truncated by losing its three
        /// most significant bits after zig-zag encoding.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">The value to pack</param>
        public static void WriteValueBitPacked(ref FastBufferWriter writer, int value) => WriteValueBitPacked(ref writer, (uint) Arithmetic.ZigZagEncode(value));

        /// <summary>
        /// Writes a 30-bit unsigned int to the buffer in a bit-encoded packed format.
        /// The first two bits indicate whether the value is 1, 2, 3, or 4 bytes.
        /// That leaves 30 bits for the value.
        /// A value greater than 2^30-1 will throw an exception in editor and development builds.
        /// In release builds builds the exception is not thrown and the value is truncated by losing its two
        /// most significant bits.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">The value to pack</param>
        public static void WriteValueBitPacked(ref FastBufferWriter writer, uint value)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (value >= 0b0100_0000_0000_0000_0000_0000_0000_0000)
            {
                throw new ArgumentException("BitPacked uints must be <= 30 bits");
            }
#endif
            value <<= 2;
            var numBytes = BitCounter.GetUsedByteCount(value);
            if (!writer.VerifyCanWriteInternal(numBytes))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            writer.WritePartialValue(value | (uint)(numBytes - 1), numBytes);
        }

        /// <summary>
        /// Writes a 60-bit signed long to the buffer in a bit-encoded packed format.
        /// The first three bits indicate whether the value is 1, 2, 3, 4, 5, 6, 7, or 8 bytes.
        /// The sign bit takes up another bit.
        /// That leaves 60 bits for the value.
        /// A value greater than 2^60-1 or less than -2^60 will throw an exception in editor and development builds.
        /// In release builds builds the exception is not thrown and the value is truncated by losing its four
        /// most significant bits after zig-zag encoding.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">The value to pack</param>
        public static void WriteValueBitPacked(ref FastBufferWriter writer, long value) => WriteValueBitPacked(ref writer, Arithmetic.ZigZagEncode(value));

        /// <summary>
        /// Writes a 61-bit unsigned long to the buffer in a bit-encoded packed format.
        /// The first three bits indicate whether the value is 1, 2, 3, 4, 5, 6, 7, or 8 bytes.
        /// That leaves 31 bits for the value.
        /// A value greater than 2^61-1 will throw an exception in editor and development builds.
        /// In release builds builds the exception is not thrown and the value is truncated by losing its three
        /// most significant bits.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">The value to pack</param>
        public static void WriteValueBitPacked(ref FastBufferWriter writer, ulong value)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (value >= 0b0010_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000)
            {
                throw new ArgumentException("BitPacked ulongs must be <= 61 bits");
            }
#endif
            value <<= 3;
            var numBytes = BitCounter.GetUsedByteCount(value);
            if (!writer.VerifyCanWriteInternal(numBytes))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            writer.WritePartialValue(value | (uint)(numBytes - 1), numBytes);
        }
#endif
        #endregion

        #region Private Methods
        private static void WriteUInt64Packed(ref FastBufferWriter writer, ulong value)
        {
            if (value <= 240)
            {
                writer.WriteByteSafe((byte)value);
                return;
            }
            if (value <= 2287)
            {
                writer.WriteByteSafe((byte)(((value - 240) >> 8) + 241));
                writer.WriteByteSafe((byte)(value - 240));
                return;
            }
            var writeBytes = BitCounter.GetUsedByteCount(value);
            
            if (!writer.VerifyCanWriteInternal(writeBytes+1))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            writer.WriteByte((byte)(247 + writeBytes));
            writer.WritePartialValue(value, writeBytes);
        }
        
        // Looks like the same code as WriteUInt64Packed?
        // It's actually different because it will call the more efficient 32-bit version
        // of BytewiseUtility.GetUsedByteCount().
        private static void WriteUInt32Packed(ref FastBufferWriter writer, uint value)
        {
            if (value <= 240)
            {
                writer.WriteByteSafe((byte)value);
                return;
            }
            if (value <= 2287)
            {
                writer.WriteByteSafe((byte)(((value - 240) >> 8) + 241));
                writer.WriteByteSafe((byte)(value - 240));
                return;
            }
            var writeBytes = BitCounter.GetUsedByteCount(value);
            
            if (!writer.VerifyCanWriteInternal(writeBytes+1))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            writer.WriteByte((byte)(247 + writeBytes));
            writer.WritePartialValue(value, writeBytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint ToUint<T>(T value) where T : unmanaged
        {
            uint* asUint = (uint*) &value;
            return *asUint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong ToUlong<T>(T value) where T : unmanaged
        {
            ulong* asUlong = (ulong*) &value;
            return *asUlong;
        }
        #endregion
    }
}