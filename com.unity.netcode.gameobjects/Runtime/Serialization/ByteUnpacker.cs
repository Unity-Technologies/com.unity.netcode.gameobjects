using System;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Netcode
{
    public static class ByteUnpacker
    {
        #region Managed TypePacking
        /// <summary>
        /// Writes a boxed object in a packed format
        /// Named differently from other ReadValuePacked methods to avoid accidental boxing
        /// </summary>
        /// <param name="value">The object to write</param>
        public static void ReadObjectPacked(ref FastBufferReader reader, out object value, Type type, bool isNullable = false)
        {
#if UNITY_NETCODE_DEBUG_NO_PACKING
            reader.ReadObject(out value, type, isNullable);
            return;
#endif
            if (isNullable || type.IsNullable())
            {
                reader.ReadValueSafe(out bool isNull);

                if (isNull)
                {
                    value = null;
                    return;
                }
            }
            
            var hasDeserializer = SerializationTypeTable.DeserializersPacked.TryGetValue(type, out var deserializer);
            if (hasDeserializer)
            {
                deserializer(ref reader, out value);
                return;
            }
            
            if (type.IsArray && type.HasElementType)
            {
                ReadValuePacked(ref reader, out int length);

                var arr = Array.CreateInstance(type.GetElementType(), length);

                for (int i = 0; i < length; i++)
                {
                    ReadObjectPacked(ref reader, out object item, type.GetElementType());
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
                        ReadValuePacked(ref reader, out byte boolVal);
                        value = Enum.ToObject(type, boolVal != 0);
                        return;
                    case TypeCode.Char:
                        ReadValuePacked(ref reader, out char charVal);
                        value = Enum.ToObject(type, charVal);
                        return;
                    case TypeCode.SByte:
                        ReadValuePacked(ref reader, out byte sbyteVal);
                        value = Enum.ToObject(type, sbyteVal);
                        return;
                    case TypeCode.Byte:
                        ReadValuePacked(ref reader, out byte byteVal);
                        value = Enum.ToObject(type, byteVal);
                        return;
                    case TypeCode.Int16:
                        ReadValuePacked(ref reader, out short shortVal);
                        value = Enum.ToObject(type, shortVal);
                        return;
                    case TypeCode.UInt16:
                        ReadValuePacked(ref reader, out ushort ushortVal);
                        value = Enum.ToObject(type, ushortVal);
                        return;
                    case TypeCode.Int32:
                        ReadValuePacked(ref reader, out int intVal);
                        value = Enum.ToObject(type, intVal);
                        return;
                    case TypeCode.UInt32:
                        ReadValuePacked(ref reader, out uint uintVal);
                        value = Enum.ToObject(type, uintVal);
                        return;
                    case TypeCode.Int64:
                        ReadValuePacked(ref reader, out long longVal);
                        value = Enum.ToObject(type, longVal);
                        return;
                    case TypeCode.UInt64:
                        ReadValuePacked(ref reader, out ulong ulongVal);
                        value = Enum.ToObject(type, ulongVal);
                        return;
                }
            }
            
            if (type == typeof(GameObject))
            {
                reader.ReadValueSafe(out GameObject go);
                value = go;
                return;
            }

            if (type == typeof(NetworkObject))
            {
                reader.ReadValueSafe(out NetworkObject no);
                value = no;
                return;
            }

            if (typeof(NetworkBehaviour).IsAssignableFrom(type))
            {
                reader.ReadValueSafe(out NetworkBehaviour nb);
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
        #endregion
        
        #region Unmanaged Type Packing
        
#if UNITY_NETCODE_DEBUG_NO_PACKING
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadValuePacked<T>(ref FastBufferReader reader, out T value) where T: unmanaged => reader.ReadValueSafe(out value);
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void ReadValuePacked<TEnum>(ref FastBufferReader reader, out TEnum value) where TEnum : unmanaged, Enum
        {
            switch (sizeof(TEnum))
            {
                case sizeof(int):
                    ReadValuePacked(ref reader, out int asInt);
                    value = *(TEnum*)&asInt;
                    break;
                case sizeof(byte):
                    ReadValuePacked(ref reader, out byte asByte);
                    value = *(TEnum*)&asByte;
                    break;
                case sizeof(short):
                    ReadValuePacked(ref reader, out short asShort);
                    value = *(TEnum*)&asShort;
                    break;
                case sizeof(long):
                    ReadValuePacked(ref reader, out long asLong);
                    value = *(TEnum*)&asLong;
                    break;
                default:
                    throw new InvalidOperationException("Enum is a size that cannot exist?!");
            }
        }
        
        /// <summary>
        /// Write single-precision floating point value to the stream as a varint
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(ref FastBufferReader reader, out float value)
        {
            ReadUInt32Packed(ref reader, out uint asUInt);
            value = ToSingle(asUInt);
        }

        /// <summary>
        /// Write double-precision floating point value to the stream as a varint
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(ref FastBufferReader reader, out double value)
        {
            ReadUInt64Packed(ref reader, out ulong asULong);
            value = ToDouble(asULong);
        }
        
        /// <summary>
        /// Write a signed short (Int16) as a ZigZag encoded varint to the stream.
        /// WARNING: If the value you're writing is > 2287, this will use MORE space
        /// (3 bytes instead of 2), and if your value is > 240 you'll get no savings at all.
        /// Only use this if you're certain your value will be small.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(ref FastBufferReader reader, out byte value) => reader.ReadByteSafe(out value);

        /// <summary>
        /// Write a signed short (Int16) as a ZigZag encoded varint to the stream.
        /// WARNING: If the value you're writing is > 2287, this will use MORE space
        /// (3 bytes instead of 2), and if your value is > 240 you'll get no savings at all.
        /// Only use this if you're certain your value will be small.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(ref FastBufferReader reader, out sbyte value)
        {
            reader.ReadByteSafe(out byte byteVal);
            value = (sbyte) byteVal;
        }

        /// <summary>
        /// Write a signed short (Int16) as a ZigZag encoded varint to the stream.
        /// WARNING: If the value you're writing is > 2287, this will use MORE space
        /// (3 bytes instead of 2), and if your value is > 240 you'll get no savings at all.
        /// Only use this if you're certain your value will be small.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(ref FastBufferReader reader, out bool value) => reader.ReadValueSafe(out value);


        /// <summary>
        /// Write a signed short (Int16) as a ZigZag encoded varint to the stream.
        /// WARNING: If the value you're writing is > 2287, this will use MORE space
        /// (3 bytes instead of 2), and if your value is > 240 you'll get no savings at all.
        /// Only use this if you're certain your value will be small.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(ref FastBufferReader reader, out short value)
        {
            ReadUInt32Packed(ref reader, out uint readValue);
            value = (short)Arithmetic.ZigZagDecode(readValue);
        }

        /// <summary>
        /// Write an unsigned short (UInt16) as a varint to the stream.
        /// WARNING: If the value you're writing is > 2287, this will use MORE space
        /// (3 bytes instead of 2), and if your value is > 240 you'll get no savings at all.
        /// Only use this if you're certain your value will be small.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(ref FastBufferReader reader, out ushort value)
        {
            ReadUInt32Packed(ref reader, out uint readValue);
            value = (ushort)readValue;
        }

        /// <summary>
        /// Write a two-byte character as a varint to the stream.
        /// WARNING: If the value you're writing is > 2287, this will use MORE space
        /// (3 bytes instead of 2), and if your value is > 240 you'll get no savings at all.
        /// Only use this if you're certain your value will be small.
        /// </summary>
        /// <param name="c">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(ref FastBufferReader reader, out char c)
        {
            ReadUInt32Packed(ref reader, out uint readValue);
            c = (char)readValue;
        }

        /// <summary>
        /// Write a signed int (Int32) as a ZigZag encoded varint to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(ref FastBufferReader reader, out int value)
        {
            ReadUInt32Packed(ref reader, out uint readValue);
            value = (int)Arithmetic.ZigZagDecode(readValue);
        }
        
        /// <summary>
        /// Write an unsigned int (UInt32) to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(ref FastBufferReader reader, out uint value) => ReadUInt32Packed(ref reader, out value);

        /// <summary>
        /// Write an unsigned long (UInt64) to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(ref FastBufferReader reader, out ulong value) => ReadUInt64Packed(ref reader, out value);

        /// <summary>
        /// Write a signed long (Int64) as a ZigZag encoded varint to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(ref FastBufferReader reader, out long value)
        {
            ReadUInt64Packed(ref reader, out ulong readValue);
            value = Arithmetic.ZigZagDecode(readValue);
        }
        
        /// <summary>
        /// Convenience method that writes two packed Vector3 from the ray to the stream
        /// </summary>
        /// <param name="ray">Ray to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(ref FastBufferReader reader, out Ray ray)
        {
            ReadValuePacked(ref reader, out Vector3 origin);
            ReadValuePacked(ref reader, out Vector3 direction);
            ray = new Ray(origin, direction);
        }

        /// <summary>
        /// Convenience method that writes two packed Vector2 from the ray to the stream
        /// </summary>
        /// <param name="ray2d">Ray2D to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(ref FastBufferReader reader, out Ray2D ray2d)
        {
            ReadValuePacked(ref reader, out Vector2 origin);
            ReadValuePacked(ref reader, out Vector2 direction);
            ray2d = new Ray2D(origin, direction);
        }

        /// <summary>
        /// Convenience method that writes four varint floats from the color to the stream
        /// </summary>
        /// <param name="color">Color to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(ref FastBufferReader reader, out Color color)
        {
            color = new Color();
            ReadValuePacked(ref reader, out color.r);
            ReadValuePacked(ref reader, out color.g);
            ReadValuePacked(ref reader, out color.b);
            ReadValuePacked(ref reader, out color.a);
        }

        /// <summary>
        /// Convenience method that writes four varint floats from the color to the stream
        /// </summary>
        /// <param name="color">Color to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(ref FastBufferReader reader, out Color32 color)
        {
            color = new Color32();
            ReadValuePacked(ref reader, out color.r);
            ReadValuePacked(ref reader, out color.g);
            ReadValuePacked(ref reader, out color.b);
            ReadValuePacked(ref reader, out color.a);
        }

        /// <summary>
        /// Convenience method that writes two varint floats from the vector to the stream
        /// </summary>
        /// <param name="vector2">Vector to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(ref FastBufferReader reader, out Vector2 vector2)
        {
            vector2 = new Vector2();
            ReadValuePacked(ref reader, out vector2.x);
            ReadValuePacked(ref reader, out vector2.y);
        }

        /// <summary>
        /// Convenience method that writes three varint floats from the vector to the stream
        /// </summary>
        /// <param name="vector3">Vector to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(ref FastBufferReader reader, out Vector3 vector3)
        {
            vector3 = new Vector3();
            ReadValuePacked(ref reader, out vector3.x);
            ReadValuePacked(ref reader, out vector3.y);
            ReadValuePacked(ref reader, out vector3.z);
        }

        /// <summary>
        /// Convenience method that writes four varint floats from the vector to the stream
        /// </summary>
        /// <param name="vector4">Vector to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(ref FastBufferReader reader, out Vector4 vector4)
        {
            vector4 = new Vector4();
            ReadValuePacked(ref reader, out vector4.x);
            ReadValuePacked(ref reader, out vector4.y);
            ReadValuePacked(ref reader, out vector4.z);
            ReadValuePacked(ref reader, out vector4.w);
        }

        /// <summary>
        /// Writes the rotation to the stream.
        /// </summary>
        /// <param name="rotation">Rotation to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(ref FastBufferReader reader, out Quaternion rotation)
        {
            rotation = new Quaternion();
            ReadValuePacked(ref reader, out rotation.x);
            ReadValuePacked(ref reader, out rotation.y);
            ReadValuePacked(ref reader, out rotation.z);
            ReadValuePacked(ref reader, out rotation.w);
        }

        /// <summary>
        /// Writes a string in a packed format
        /// </summary>
        /// <param name="s"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void ReadValuePacked(ref FastBufferReader reader, out string s)
        {
            ReadValuePacked(ref reader, out uint length);
            s = "".PadRight((int)length);
            int target = s.Length;
            fixed (char* c = s)
            {
                for (int i = 0; i < target; ++i)
                {
                    ReadValuePacked(ref reader, out c[i]);
                }
            }
        }
#endif
        #endregion
        
        #region Bit Packing

#if UNITY_NETCODE_DEBUG_NO_PACKING
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadValueBitPacked<T>(ref FastBufferReader reader, T value) where T: unmanaged => reader.ReadValueSafe(out value);
#else
        public static void ReadValueBitPacked(ref FastBufferReader reader, out short value)
        {
            ReadValueBitPacked(ref reader, out ushort readValue);
            value = (short)Arithmetic.ZigZagDecode(readValue);
        }

        public static unsafe void ReadValueBitPacked(ref FastBufferReader reader, out ushort value)
        {
            ushort returnValue = 0;
            byte* ptr = ((byte*) &returnValue);
            byte* data = reader.GetUnsafePtrAtCurrentPosition();
            int numBytes = (data[0] & 0b1) + 1;
            if (!reader.VerifyCanReadInternal(numBytes))
            {
                throw new OverflowException("Reading past the end of the buffer");
            }
            reader.MarkBytesRead(numBytes);
            switch (numBytes)
            {
                case 1:
                    *ptr = *data;
                    break;
                case 2:
                    *(ushort*) ptr = *(ushort*)data;
                    break;
                default:
                    throw new InvalidOperationException("Could not read bit-packed value: impossible byte count");
            }

            value = (ushort)(returnValue >> 1);
        }

        public static void ReadValueBitPacked(ref FastBufferReader reader, out int value)
        {
            ReadValueBitPacked(ref reader, out uint readValue);
            value = (int)Arithmetic.ZigZagDecode(readValue);
        }
        public static unsafe void ReadValueBitPacked(ref FastBufferReader reader, out uint value)
        {
            uint returnValue = 0;
            byte* ptr = ((byte*) &returnValue);
            byte* data = reader.GetUnsafePtrAtCurrentPosition();
            int numBytes = (data[0] & 0b11) + 1;
            if (!reader.VerifyCanReadInternal(numBytes))
            {
                throw new OverflowException("Reading past the end of the buffer");
            }
            reader.MarkBytesRead(numBytes);
            switch (numBytes)
            {
                case 1:
                    *ptr = *data;
                    break;
                case 2:
                    *(ushort*) ptr = *(ushort*)data;
                    break;
                case 3:
                    *(ushort*) ptr = *(ushort*)data;
                    *(ptr+2) = *(data+2);
                    break;
                case 4:
                    *(uint*) ptr = *(uint*)data;
                    break;
            }

            value = returnValue >> 2;
        }

        public static void ReadValueBitPacked(ref FastBufferReader reader, out long value)
        {
            ReadValueBitPacked(ref reader, out ulong readValue);
            value = Arithmetic.ZigZagDecode(readValue);
        }
        public static unsafe void ReadValueBitPacked(ref FastBufferReader reader, out ulong value)
        {
            ulong returnValue = 0;
            byte* ptr = ((byte*) &returnValue);
            byte* data = reader.GetUnsafePtrAtCurrentPosition();
            int numBytes = (data[0] & 0b111) + 1;
            if (!reader.VerifyCanReadInternal(numBytes))
            {
                throw new OverflowException("Reading past the end of the buffer");
            }
            reader.MarkBytesRead(numBytes);
            switch (numBytes)
            {
                case 1:
                    *ptr = *data;
                    break;
                case 2:
                    *(ushort*) ptr = *(ushort*)data;
                    break;
                case 3:
                    *(ushort*) ptr = *(ushort*)data;
                    *(ptr+2) = *(data+2);
                    break;
                case 4:
                    *(uint*) ptr = *(uint*)data;
                    break;
                case 5:
                    *(uint*) ptr = *(uint*)data;
                    *(ptr+4) = *(data+4);
                    break;
                case 6:
                    *(uint*) ptr = *(uint*)data;
                    *(ushort*) (ptr+4) = *(ushort*)(data+4);
                    break;
                case 7:
                    *(uint*) ptr = *(uint*)data;
                    *(ushort*) (ptr+4) = *(ushort*)(data+4);
                    *(ptr+6) = *(data+6);
                    break;
                case 8:
                    *(ulong*) ptr = *(ulong*)data;
                    break;
            }

            value = returnValue >> 3;
        }
#endif
        #endregion

        #region Private Methods
        private static void ReadUInt64Packed(ref FastBufferReader reader, out ulong value)
        {
            reader.ReadByteSafe(out byte firstByte);
            if (firstByte <= 240)
            {
                value = firstByte;
                return;
            }

            if (firstByte <= 248)
            {
                reader.ReadByteSafe(out byte secondByte);
                value = 240UL + ((firstByte - 241UL) << 8) + secondByte;
                return;
            }

            var numBytes = firstByte - 247;
            if (!reader.VerifyCanReadInternal(numBytes))
            {
                throw new OverflowException("Reading past the end of the buffer");
            }
            reader.ReadPartialValue(out value, numBytes);
        }
        
        private static void ReadUInt32Packed(ref FastBufferReader reader, out uint value)
        {
            reader.ReadByteSafe(out byte firstByte);
            if (firstByte <= 240)
            {
                value = firstByte;
                return;
            }

            if (firstByte <= 248)
            {
                reader.ReadByteSafe(out byte secondByte);
                value = 240U + ((firstByte - 241U) << 8) + secondByte;
                return;
            }

            var numBytes = firstByte - 247;
            if (!reader.VerifyCanReadInternal(numBytes))
            {
                throw new OverflowException("Reading past the end of the buffer");
            }
            reader.ReadPartialValue(out value, numBytes);
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