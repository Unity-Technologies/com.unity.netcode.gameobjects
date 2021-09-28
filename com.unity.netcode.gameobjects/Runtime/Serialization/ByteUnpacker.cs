using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.Netcode
{
    public static class ByteUnpacker
    {

#if UNITY_NETCODE_DEBUG_NO_PACKING
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadValuePacked<T>(FastBufferReader reader, out T value) where T: unmanaged => reader.ReadValueSafe(out value);
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void ReadValuePacked<TEnum>(FastBufferReader reader, out TEnum value) where TEnum : unmanaged, Enum
        {
            switch (sizeof(TEnum))
            {
                case sizeof(int):
                    ReadValuePacked(reader, out int asInt);
                    value = *(TEnum*)&asInt;
                    break;
                case sizeof(byte):
                    ReadValuePacked(reader, out byte asByte);
                    value = *(TEnum*)&asByte;
                    break;
                case sizeof(short):
                    ReadValuePacked(reader, out short asShort);
                    value = *(TEnum*)&asShort;
                    break;
                case sizeof(long):
                    ReadValuePacked(reader, out long asLong);
                    value = *(TEnum*)&asLong;
                    break;
                default:
                    throw new InvalidOperationException("Enum is a size that cannot exist?!");
            }
        }

        /// <summary>
        /// Read single-precision floating point value from the stream as a varint
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="value">Value to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(FastBufferReader reader, out float value)
        {
            ReadUInt32Packed(reader, out uint asUInt);
            value = ToSingle(asUInt);
        }

        /// <summary>
        /// Read double-precision floating point value from the stream as a varint
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="value">Value to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(FastBufferReader reader, out double value)
        {
            ReadUInt64Packed(reader, out ulong asULong);
            value = ToDouble(asULong);
        }

        /// <summary>
        /// Read a byte from the stream.
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="value">Value to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(FastBufferReader reader, out byte value) => reader.ReadByteSafe(out value);

        /// <summary>
        /// Read a signed byte from the stream.
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="value">Value to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(FastBufferReader reader, out sbyte value)
        {
            reader.ReadByteSafe(out byte byteVal);
            value = (sbyte)byteVal;
        }

        /// <summary>
        /// Read a boolean from the stream.
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="value">Value to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(FastBufferReader reader, out bool value) => reader.ReadValueSafe(out value);


        /// <summary>
        /// Read an usigned short (Int16) as a varint from the stream.
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="value">Value to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(FastBufferReader reader, out short value)
        {
            ReadUInt32Packed(reader, out uint readValue);
            value = (short)Arithmetic.ZigZagDecode(readValue);
        }

        /// <summary>
        /// Read an unsigned short (UInt16) as a varint from the stream.
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="value">Value to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(FastBufferReader reader, out ushort value)
        {
            ReadUInt32Packed(reader, out uint readValue);
            value = (ushort)readValue;
        }

        /// <summary>
        /// Read a two-byte character as a varint from the stream.
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="c">Value to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(FastBufferReader reader, out char c)
        {
            ReadUInt32Packed(reader, out uint readValue);
            c = (char)readValue;
        }

        /// <summary>
        /// Read a signed int (Int32) as a ZigZag encoded varint from the stream.
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="value">Value to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(FastBufferReader reader, out int value)
        {
            ReadUInt32Packed(reader, out uint readValue);
            value = (int)Arithmetic.ZigZagDecode(readValue);
        }

        /// <summary>
        /// Read an unsigned int (UInt32) from the stream.
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="value">Value to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(FastBufferReader reader, out uint value) => ReadUInt32Packed(reader, out value);

        /// <summary>
        /// Read an unsigned long (UInt64) from the stream.
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="value">Value to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(FastBufferReader reader, out ulong value) => ReadUInt64Packed(reader, out value);

        /// <summary>
        /// Read a signed long (Int64) as a ZigZag encoded varint from the stream.
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="value">Value to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(FastBufferReader reader, out long value)
        {
            ReadUInt64Packed(reader, out ulong readValue);
            value = Arithmetic.ZigZagDecode(readValue);
        }

        /// <summary>
        /// Convenience method that reads two packed Vector3 from the ray from the stream
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="ray">Ray to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(FastBufferReader reader, out Ray ray)
        {
            ReadValuePacked(reader, out Vector3 origin);
            ReadValuePacked(reader, out Vector3 direction);
            ray = new Ray(origin, direction);
        }

        /// <summary>
        /// Convenience method that reads two packed Vector2 from the ray from the stream
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="ray2d">Ray2D to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(FastBufferReader reader, out Ray2D ray2d)
        {
            ReadValuePacked(reader, out Vector2 origin);
            ReadValuePacked(reader, out Vector2 direction);
            ray2d = new Ray2D(origin, direction);
        }

        /// <summary>
        /// Convenience method that reads four varint floats from the color from the stream
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="color">Color to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(FastBufferReader reader, out Color color)
        {
            color = new Color();
            ReadValuePacked(reader, out color.r);
            ReadValuePacked(reader, out color.g);
            ReadValuePacked(reader, out color.b);
            ReadValuePacked(reader, out color.a);
        }

        /// <summary>
        /// Convenience method that reads four varint floats from the color from the stream
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="color">Color to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(FastBufferReader reader, out Color32 color)
        {
            color = new Color32();
            ReadValuePacked(reader, out color.r);
            ReadValuePacked(reader, out color.g);
            ReadValuePacked(reader, out color.b);
            ReadValuePacked(reader, out color.a);
        }

        /// <summary>
        /// Convenience method that reads two varint floats from the vector from the stream
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="vector2">Vector to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(FastBufferReader reader, out Vector2 vector2)
        {
            vector2 = new Vector2();
            ReadValuePacked(reader, out vector2.x);
            ReadValuePacked(reader, out vector2.y);
        }

        /// <summary>
        /// Convenience method that reads three varint floats from the vector from the stream
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="vector3">Vector to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(FastBufferReader reader, out Vector3 vector3)
        {
            vector3 = new Vector3();
            ReadValuePacked(reader, out vector3.x);
            ReadValuePacked(reader, out vector3.y);
            ReadValuePacked(reader, out vector3.z);
        }

        /// <summary>
        /// Convenience method that reads four varint floats from the vector from the stream
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="vector4">Vector to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(FastBufferReader reader, out Vector4 vector4)
        {
            vector4 = new Vector4();
            ReadValuePacked(reader, out vector4.x);
            ReadValuePacked(reader, out vector4.y);
            ReadValuePacked(reader, out vector4.z);
            ReadValuePacked(reader, out vector4.w);
        }

        /// <summary>
        /// Reads the rotation from the stream.
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="rotation">Rotation to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadValuePacked(FastBufferReader reader, out Quaternion rotation)
        {
            rotation = new Quaternion();
            ReadValuePacked(reader, out rotation.x);
            ReadValuePacked(reader, out rotation.y);
            ReadValuePacked(reader, out rotation.z);
            ReadValuePacked(reader, out rotation.w);
        }

        /// <summary>
        /// Reads a string in a packed format
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="s"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void ReadValuePacked(FastBufferReader reader, out string s)
        {
            ReadValuePacked(reader, out uint length);
            s = "".PadRight((int)length);
            int target = s.Length;
            fixed (char* c = s)
            {
                for (int i = 0; i < target; ++i)
                {
                    ReadValuePacked(reader, out c[i]);
                }
            }
        }
#endif

#if UNITY_NETCODE_DEBUG_NO_PACKING
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadValueBitPacked<T>(FastBufferReader reader, T value) where T: unmanaged => reader.ReadValueSafe(out value);
#else
        /// <summary>
        /// Read a bit-packed 14-bit signed short from the stream.
        /// See BytePacker.cs for a description of the format.
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="value">The value to read</param>
        public static void ReadValueBitPacked(FastBufferReader reader, out short value)
        {
            ReadValueBitPacked(reader, out ushort readValue);
            value = (short)Arithmetic.ZigZagDecode(readValue);
        }

        /// <summary>
        /// Read a bit-packed 15-bit unsigned short from the stream.
        /// See BytePacker.cs for a description of the format.
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="value">The value to read</param>
        public static unsafe void ReadValueBitPacked(FastBufferReader reader, out ushort value)
        {
            ushort returnValue = 0;
            byte* ptr = ((byte*)&returnValue);
            byte* data = reader.GetUnsafePtrAtCurrentPosition();
            int numBytes = (data[0] & 0b1) + 1;
            if (!reader.TryBeginReadInternal(numBytes))
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
                    *ptr = *data;
                    *(ptr + 1) = *(data + 1);
                    break;
                default:
                    throw new InvalidOperationException("Could not read bit-packed value: impossible byte count");
            }

            value = (ushort)(returnValue >> 1);
        }

        /// <summary>
        /// Read a bit-packed 29-bit signed int from the stream.
        /// See BytePacker.cs for a description of the format.
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="value">The value to read</param>
        public static void ReadValueBitPacked(FastBufferReader reader, out int value)
        {
            ReadValueBitPacked(reader, out uint readValue);
            value = (int)Arithmetic.ZigZagDecode(readValue);
        }

        /// <summary>
        /// Read a bit-packed 30-bit unsigned int from the stream.
        /// See BytePacker.cs for a description of the format.
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="value">The value to read</param>
        public static unsafe void ReadValueBitPacked(FastBufferReader reader, out uint value)
        {
            uint returnValue = 0;
            byte* ptr = ((byte*)&returnValue);
            byte* data = reader.GetUnsafePtrAtCurrentPosition();
            int numBytes = (data[0] & 0b11) + 1;
            if (!reader.TryBeginReadInternal(numBytes))
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
                    *ptr = *data;
                    *(ptr + 1) = *(data + 1);
                    break;
                case 3:
                    *ptr = *data;
                    *(ptr + 1) = *(data + 1);
                    *(ptr + 2) = *(data + 2);
                    break;
                case 4:
                    *ptr = *data;
                    *(ptr + 1) = *(data + 1);
                    *(ptr + 2) = *(data + 2);
                    *(ptr + 3) = *(data + 3);
                    break;
            }

            value = returnValue >> 2;
        }

        /// <summary>
        /// Read a bit-packed 60-bit signed long from the stream.
        /// See BytePacker.cs for a description of the format.
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="value">The value to read</param>
        public static void ReadValueBitPacked(FastBufferReader reader, out long value)
        {
            ReadValueBitPacked(reader, out ulong readValue);
            value = Arithmetic.ZigZagDecode(readValue);
        }

        /// <summary>
        /// Read a bit-packed 61-bit signed long from the stream.
        /// See BytePacker.cs for a description of the format.
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="value">The value to read</param>
        public static unsafe void ReadValueBitPacked(FastBufferReader reader, out ulong value)
        {
            ulong returnValue = 0;
            byte* ptr = ((byte*)&returnValue);
            byte* data = reader.GetUnsafePtrAtCurrentPosition();
            int numBytes = (data[0] & 0b111) + 1;
            if (!reader.TryBeginReadInternal(numBytes))
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
                    *ptr = *data;
                    *(ptr + 1) = *(data + 1);
                    break;
                case 3:
                    *ptr = *data;
                    *(ptr + 1) = *(data + 1);
                    *(ptr + 2) = *(data + 2);
                    break;
                case 4:
                    *ptr = *data;
                    *(ptr + 1) = *(data + 1);
                    *(ptr + 2) = *(data + 2);
                    *(ptr + 3) = *(data + 3);
                    break;
                case 5:
                    *ptr = *data;
                    *(ptr + 1) = *(data + 1);
                    *(ptr + 2) = *(data + 2);
                    *(ptr + 3) = *(data + 3);
                    *(ptr + 4) = *(data + 4);
                    break;
                case 6:
                    *ptr = *data;
                    *(ptr + 1) = *(data + 1);
                    *(ptr + 2) = *(data + 2);
                    *(ptr + 3) = *(data + 3);
                    *(ptr + 4) = *(data + 4);
                    *(ptr + 5) = *(data + 5);
                    break;
                case 7:
                    *ptr = *data;
                    *(ptr + 1) = *(data + 1);
                    *(ptr + 2) = *(data + 2);
                    *(ptr + 3) = *(data + 3);
                    *(ptr + 4) = *(data + 4);
                    *(ptr + 5) = *(data + 5);
                    *(ptr + 6) = *(data + 6);
                    break;
                case 8:
                    *ptr = *data;
                    *(ptr + 1) = *(data + 1);
                    *(ptr + 2) = *(data + 2);
                    *(ptr + 3) = *(data + 3);
                    *(ptr + 4) = *(data + 4);
                    *(ptr + 5) = *(data + 5);
                    *(ptr + 6) = *(data + 6);
                    *(ptr + 7) = *(data + 7);
                    break;
            }

            value = returnValue >> 3;
        }
#endif
        private static void ReadUInt64Packed(FastBufferReader reader, out ulong value)
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
            if (!reader.TryBeginReadInternal(numBytes))
            {
                throw new OverflowException("Reading past the end of the buffer");
            }
            reader.ReadPartialValue(out value, numBytes);
        }

        private static void ReadUInt32Packed(FastBufferReader reader, out uint value)
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
            if (!reader.TryBeginReadInternal(numBytes))
            {
                throw new OverflowException("Reading past the end of the buffer");
            }
            reader.ReadPartialValue(out value, numBytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe float ToSingle<T>(T value) where T : unmanaged
        {
            float* asFloat = (float*)&value;
            return *asFloat;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe double ToDouble<T>(T value) where T : unmanaged
        {
            double* asDouble = (double*)&value;
            return *asDouble;
        }
    }
}
