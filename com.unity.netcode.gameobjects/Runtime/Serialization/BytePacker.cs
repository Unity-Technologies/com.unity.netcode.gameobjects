using System;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Netcode
{
    public static class BytePacker
    {
        #region Managed TypePacking
        /// <summary>
        /// Writes a boxed object in a packed format
        /// Named differently from other WriteValuePacked methods to avoid accidental boxing
        /// </summary>
        /// <param name="value">The object to write</param>
        public static void WriteObjectPacked(ref FastBufferWriter writer, object value, bool isNullable = false)
        {
#if UNITY_NETCODE_DEBUG_NO_PACKING
            writer.WriteObject(value, isNullable);
            return;
#endif
            if (isNullable || value.GetType().IsNullable())
            {
                bool isNull = value == null || (value is UnityEngine.Object && ((UnityEngine.Object)value) == null);

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteValuePacked<TEnum>(ref FastBufferWriter writer, ref TEnum value) where TEnum : unmanaged, Enum
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
        /// Write single-precision floating point value to the stream as a varint
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, float value)
        {
            WriteUInt32Packed(ref writer, ToUint(value));
        }

        /// <summary>
        /// Write double-precision floating point value to the stream as a varint
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, double value)
        {
            WriteUInt64Packed(ref writer, ToUlong(value));
        }
        
        /// <summary>
        /// Write a signed short (Int16) as a ZigZag encoded varint to the stream.
        /// WARNING: If the value you're writing is > 2287, this will use MORE space
        /// (3 bytes instead of 2), and if your value is > 240 you'll get no savings at all.
        /// Only use this if you're certain your value will be small.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, byte value) => writer.WriteByteSafe(value);
        
        /// <summary>
        /// Write a signed short (Int16) as a ZigZag encoded varint to the stream.
        /// WARNING: If the value you're writing is > 2287, this will use MORE space
        /// (3 bytes instead of 2), and if your value is > 240 you'll get no savings at all.
        /// Only use this if you're certain your value will be small.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, bool value) => writer.WriteValueSafe(value);


        /// <summary>
        /// Write a signed short (Int16) as a ZigZag encoded varint to the stream.
        /// WARNING: If the value you're writing is > 2287, this will use MORE space
        /// (3 bytes instead of 2), and if your value is > 240 you'll get no savings at all.
        /// Only use this if you're certain your value will be small.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, short value) => WriteUInt32Packed(ref writer, (ushort)Arithmetic.ZigZagEncode(value));

        /// <summary>
        /// Write an unsigned short (UInt16) as a varint to the stream.
        /// WARNING: If the value you're writing is > 2287, this will use MORE space
        /// (3 bytes instead of 2), and if your value is > 240 you'll get no savings at all.
        /// Only use this if you're certain your value will be small.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, ushort value) => WriteUInt32Packed(ref writer, value);

        /// <summary>
        /// Write a two-byte character as a varint to the stream.
        /// WARNING: If the value you're writing is > 2287, this will use MORE space
        /// (3 bytes instead of 2), and if your value is > 240 you'll get no savings at all.
        /// Only use this if you're certain your value will be small.
        /// </summary>
        /// <param name="c">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, char c) => WriteUInt32Packed(ref writer, c);

        /// <summary>
        /// Write a signed int (Int32) as a ZigZag encoded varint to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, int value) => WriteUInt32Packed(ref writer, (uint)Arithmetic.ZigZagEncode(value));

        /// <summary>
        /// Write an unsigned int (UInt32) to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, uint value) => WriteUInt32Packed(ref writer, value);

        /// <summary>
        /// Write an unsigned long (UInt64) to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, ulong value) => WriteUInt64Packed(ref writer, value);

        /// <summary>
        /// Write a signed long (Int64) as a ZigZag encoded varint to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, long value) => WriteUInt64Packed(ref writer, (ulong)Arithmetic.ZigZagEncode(value));

        /// <summary>
        /// Convenience method that writes two packed Vector3 from the ray to the stream
        /// </summary>
        /// <param name="ray">Ray to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, Ray ray)
        {
            WriteValuePacked(ref writer, ray.origin);
            WriteValuePacked(ref writer, ray.direction);
        }

        /// <summary>
        /// Convenience method that writes two packed Vector2 from the ray to the stream
        /// </summary>
        /// <param name="ray2d">Ray2D to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, Ray2D ray2d)
        {
            WriteValuePacked(ref writer, ray2d.origin);
            WriteValuePacked(ref writer, ray2d.direction);
        }

        /// <summary>
        /// Convenience method that writes four varint floats from the color to the stream
        /// </summary>
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
        /// Convenience method that writes four varint floats from the color to the stream
        /// </summary>
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
        /// Convenience method that writes two varint floats from the vector to the stream
        /// </summary>
        /// <param name="vector2">Vector to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, Vector2 vector2)
        {
            WriteValuePacked(ref writer, vector2.x);
            WriteValuePacked(ref writer, vector2.y);
        }

        /// <summary>
        /// Convenience method that writes three varint floats from the vector to the stream
        /// </summary>
        /// <param name="vector3">Vector to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, Vector3 vector3)
        {
            WriteValuePacked(ref writer, vector3.x);
            WriteValuePacked(ref writer, vector3.y);
            WriteValuePacked(ref writer, vector3.z);
        }

        /// <summary>
        /// Convenience method that writes four varint floats from the vector to the stream
        /// </summary>
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
        /// Writes the rotation to the stream.
        /// </summary>
        /// <param name="rotation">Rotation to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(ref FastBufferWriter writer, Quaternion rotation)
        {
            if (Mathf.Sign(rotation.w) < 0)
            {
                WriteValuePacked(ref writer, -rotation.x);
                WriteValuePacked(ref writer, -rotation.y);
                WriteValuePacked(ref writer, -rotation.z);
            }
            else
            {
                WriteValuePacked(ref writer, rotation.x);
                WriteValuePacked(ref writer, rotation.y);
                WriteValuePacked(ref writer, rotation.z);
            }
        }

        /// <summary>
        /// Writes a string in a packed format
        /// </summary>
        /// <param name="s"></param>
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
        public static void WriteValueBitPacked(ref FastBufferWriter writer, short value) => WriteValueBitPacked(ref writer, (ushort) Arithmetic.ZigZagEncode(value));

        public static void WriteValueBitPacked(ref FastBufferWriter writer, ushort value)
        {
            if (value >= 0b1000_0000_0000_0000)
            {
                throw new ArgumentException("BitPacked ushorts must be <= 15 bits");
            }
            
            if (value <= 0b0111_1111)
            {
                if (!writer.VerifyCanWriteInternal(1))
                {
                    throw new OverflowException("Reading past the end of the buffer");
                }
                writer.WriteByte((byte)(value << 1));
                return;
            }
            
            if (!writer.VerifyCanWriteInternal(2))
            {
                throw new OverflowException("Reading past the end of the buffer");
            }
            writer.WriteValue((ushort)((value << 1) | 0b1));
        }

        public static void WriteValueBitPacked(ref FastBufferWriter writer, int value) => WriteValueBitPacked(ref writer, (uint) Arithmetic.ZigZagEncode(value));

        public static void WriteValueBitPacked(ref FastBufferWriter writer, uint value)
        {
            if (value >= 0b0100_0000_0000_0000_0000_0000_0000_0000)
            {
                throw new ArgumentException("BitPacked uints must be <= 30 bits");
            }
            
            if (value <= 0b0011_1111)
            {
                if (!writer.VerifyCanWriteInternal(1))
                {
                    throw new OverflowException("Reading past the end of the buffer");
                }
                writer.WriteByte((byte)(value << 2));
                return;
            }

            if (value <= 0b0011_1111_1111_1111)
            {
                if (!writer.VerifyCanWriteInternal(2))
                {
                    throw new OverflowException("Reading past the end of the buffer");
                }
                writer.WriteValue((ushort)((value << 2) | 0b01));
                return;
            }

            if (value <= 0b0011_1111_1111_1111_1111_1111)
            {
                if (!writer.VerifyCanWriteInternal(3))
                {
                    throw new OverflowException("Reading past the end of the buffer");
                }
                writer.WritePartialValue(((value << 2) | 0b10), 3);
                return;
            }
            
            if (!writer.VerifyCanWriteInternal(4))
            {
                throw new OverflowException("Reading past the end of the buffer");
            }
            writer.WriteValue(((value << 2) | 0b11));
        }

        public static void WriteValueBitPacked(ref FastBufferWriter writer, long value) => WriteValueBitPacked(ref writer, (ulong) Arithmetic.ZigZagEncode(value));

        public static void WriteValueBitPacked(ref FastBufferWriter writer, ulong value)
        {
            if (value >= 0b0010_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000)
            {
                throw new ArgumentException("BitPacked ulongs must be <= 61 bits");
            }
            
            if (value <= 0b0001_1111)
            {
                if (!writer.VerifyCanWriteInternal(1))
                {
                    throw new OverflowException("Reading past the end of the buffer");
                }
                writer.WriteByte((byte)(value << 3));
                return;
            }

            if (value <= 0b0001_1111_1111_1111)
            {
                if (!writer.VerifyCanWriteInternal(2))
                {
                    throw new OverflowException("Reading past the end of the buffer");
                }
                writer.WriteValue((ushort)((value << 3) | 0b001));
                return;
            }

            if (value <= 0b0001_1111_1111_1111_1111_1111)
            {
                if (!writer.VerifyCanWriteInternal(3))
                {
                    throw new OverflowException("Reading past the end of the buffer");
                }
                writer.WritePartialValue((value << 3) | 0b010, 3);
                return;
            }

            if (value <= 0b0001_1111_1111_1111_1111_1111_1111_1111)
            {
                if (!writer.VerifyCanWriteInternal(4))
                {
                    throw new OverflowException("Reading past the end of the buffer");
                }
                writer.WriteValue((uint)((value << 3) | 0b011));
                return;
            }

            if (value <= 0b0001_1111_1111_1111_1111_1111_1111_1111_1111_1111)
            {
                if (!writer.VerifyCanWriteInternal(5))
                {
                    throw new OverflowException("Reading past the end of the buffer");
                }
                writer.WritePartialValue((value << 3) | 0b100, 5);
                return;
            }

            if (value <= 0b0001_1111_1111_1111_1111_1111_1111_1111_1111_1111_1111_1111)
            {
                if (!writer.VerifyCanWriteInternal(6))
                {
                    throw new OverflowException("Reading past the end of the buffer");
                }
                writer.WritePartialValue((value << 3) | 0b101, 6);
                return;
            }

            if (value <= 0b0001_1111_1111_1111_1111_1111_1111_1111_1111_1111_1111_1111_1111_1111)
            {
                if (!writer.VerifyCanWriteInternal(7))
                {
                    throw new OverflowException("Reading past the end of the buffer");
                }
                writer.WritePartialValue((value << 3) | 0b110, 7);
                return;
            }
            
            if (!writer.VerifyCanWriteInternal(8))
            {
                throw new OverflowException("Reading past the end of the buffer");
            }
            writer.WriteValue((value << 3) | 0b111);
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
                throw new OverflowException("Reading past the end of the buffer");
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
                throw new OverflowException("Reading past the end of the buffer");
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe float ToSingle<T>(T value) where T : unmanaged
        {
            float* asFloat = (float*) &value;
            return *asFloat;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe double ToDouble<T>(T value) where T : unmanaged
        {
            double* asDouble = (double*) &value;
            return *asDouble;
        }
        #endregion
    }
}