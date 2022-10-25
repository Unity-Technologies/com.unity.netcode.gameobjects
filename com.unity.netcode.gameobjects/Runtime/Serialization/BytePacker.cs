using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Utility class for packing values in serialization.
    /// <seealso cref="ByteUnpacker"/> to unpack packed values.
    /// </summary>
    public static class BytePacker
    {
#if UNITY_NETCODE_DEBUG_NO_PACKING

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteValuePacked<T>(FastBufferWriter writer, T value) where T: unmanaged => writer.WriteValueSafe(value);
#else
        /// <summary>
        /// Write a packed enum value.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">The value to write</param>
        /// <typeparam name="TEnum">An enum type</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteValuePacked<TEnum>(FastBufferWriter writer, TEnum value) where TEnum : unmanaged, Enum
        {
            TEnum enumValue = value;
            switch (sizeof(TEnum))
            {
                case sizeof(int):
                    WriteValuePacked(writer, *(int*)&enumValue);
                    break;
                case sizeof(byte):
                    WriteValuePacked(writer, *(byte*)&enumValue);
                    break;
                case sizeof(short):
                    WriteValuePacked(writer, *(short*)&enumValue);
                    break;
                case sizeof(long):
                    WriteValuePacked(writer, *(long*)&enumValue);
                    break;
            }
        }

        /// <summary>
        /// Write single-precision floating point value to the buffer as a varint
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, float value)
        {
            WriteValueBitPacked(writer, ToUint(value));
        }

        /// <summary>
        /// Write double-precision floating point value to the buffer as a varint
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, double value)
        {
            WriteValueBitPacked(writer, ToUlong(value));
        }

        /// <summary>
        /// Write a byte to the buffer.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, byte value) => writer.WriteByteSafe(value);

        /// <summary>
        /// Write a signed byte to the buffer.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, sbyte value) => writer.WriteByteSafe((byte)value);

        /// <summary>
        /// Write a bool to the buffer.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, bool value) => writer.WriteValueSafe(value);


        /// <summary>
        /// Write a signed short (Int16) as a ZigZag encoded varint to the buffer.
        /// WARNING: If the value you're writing is > 2287, this will use MORE space
        /// (3 bytes instead of 2), and if your value is > 240 you'll get no savings at all.
        /// Only use this if you're certain your value will be small.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, short value) => WriteValueBitPacked(writer, value);

        /// <summary>
        /// Write an unsigned short (UInt16) as a varint to the buffer.
        /// WARNING: If the value you're writing is > 2287, this will use MORE space
        /// (3 bytes instead of 2), and if your value is > 240 you'll get no savings at all.
        /// Only use this if you're certain your value will be small.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, ushort value) => WriteValueBitPacked(writer, value);

        /// <summary>
        /// Write a two-byte character as a varint to the buffer.
        /// WARNING: If the value you're writing is > 2287, this will use MORE space
        /// (3 bytes instead of 2), and if your value is > 240 you'll get no savings at all.
        /// Only use this if you're certain your value will be small.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="c">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, char c) => WriteValueBitPacked(writer, c);

        /// <summary>
        /// Write a signed int (Int32) as a ZigZag encoded varint to the buffer.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, int value) => WriteValueBitPacked(writer, value);

        /// <summary>
        /// Write an unsigned int (UInt32) to the buffer.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, uint value) => WriteValueBitPacked(writer, value);

        /// <summary>
        /// Write an unsigned long (UInt64) to the buffer.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, ulong value) => WriteValueBitPacked(writer, value);

        /// <summary>
        /// Write a signed long (Int64) as a ZigZag encoded varint to the buffer.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, long value) => WriteValueBitPacked(writer, value);

        /// <summary>
        /// Convenience method that writes two packed Vector3 from the ray to the buffer
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="ray">Ray to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, Ray ray)
        {
            WriteValuePacked(writer, ray.origin);
            WriteValuePacked(writer, ray.direction);
        }

        /// <summary>
        /// Convenience method that writes two packed Vector2 from the ray to the buffer
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="ray2d">Ray2D to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, Ray2D ray2d)
        {
            WriteValuePacked(writer, ray2d.origin);
            WriteValuePacked(writer, ray2d.direction);
        }

        /// <summary>
        /// Convenience method that writes four varint floats from the color to the buffer
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="color">Color to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, Color color)
        {
            WriteValuePacked(writer, color.r);
            WriteValuePacked(writer, color.g);
            WriteValuePacked(writer, color.b);
            WriteValuePacked(writer, color.a);
        }

        /// <summary>
        /// Convenience method that writes four varint floats from the color to the buffer
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="color">Color to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, Color32 color)
        {
            WriteValuePacked(writer, color.r);
            WriteValuePacked(writer, color.g);
            WriteValuePacked(writer, color.b);
            WriteValuePacked(writer, color.a);
        }

        /// <summary>
        /// Convenience method that writes two varint floats from the vector to the buffer
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="vector2">Vector to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, Vector2 vector2)
        {
            WriteValuePacked(writer, vector2.x);
            WriteValuePacked(writer, vector2.y);
        }

        /// <summary>
        /// Convenience method that writes three varint floats from the vector to the buffer
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="vector3">Vector to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, Vector3 vector3)
        {
            WriteValuePacked(writer, vector3.x);
            WriteValuePacked(writer, vector3.y);
            WriteValuePacked(writer, vector3.z);
        }

        /// <summary>
        /// Convenience method that writes four varint floats from the vector to the buffer
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="vector4">Vector to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, Vector4 vector4)
        {
            WriteValuePacked(writer, vector4.x);
            WriteValuePacked(writer, vector4.y);
            WriteValuePacked(writer, vector4.z);
            WriteValuePacked(writer, vector4.w);
        }

        /// <summary>
        /// Writes the rotation to the buffer.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="rotation">Rotation to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, Quaternion rotation)
        {
            WriteValuePacked(writer, rotation.x);
            WriteValuePacked(writer, rotation.y);
            WriteValuePacked(writer, rotation.z);
            WriteValuePacked(writer, rotation.w);
        }

        /// <summary>
        /// Writes a string in a packed format
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="s">The value to pack</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, string s)
        {
            WriteValuePacked(writer, (uint)s.Length);
            int target = s.Length;
            for (int i = 0; i < target; ++i)
            {
                WriteValuePacked(writer, s[i]);
            }
        }
#endif


#if UNITY_NETCODE_DEBUG_NO_PACKING

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteValueBitPacked<T>(FastBufferWriter writer, T value) where T: unmanaged => writer.WriteValueSafe(value);
#else
        /// <summary>
        /// Obsolete value that no longer carries meaning. Do not use.
        /// </summary>
        public const ushort BitPackedUshortMax = (1 << 15) - 1;

        /// <summary>
        /// Obsolete value that no longer carries meaning. Do not use.
        /// </summary>
        public const short BitPackedShortMax = (1 << 14) - 1;

        /// <summary>
        /// Obsolete value that no longer carries meaning. Do not use.
        /// </summary>
        public const short BitPackedShortMin = -(1 << 14);

        /// <summary>
        /// Obsolete value that no longer carries meaning. Do not use.
        /// </summary>
        public const uint BitPackedUintMax = (1 << 30) - 1;

        /// <summary>
        /// Obsolete value that no longer carries meaning. Do not use.
        /// </summary>
        public const int BitPackedIntMax = (1 << 29) - 1;

        /// <summary>
        /// Obsolete value that no longer carries meaning. Do not use.
        /// </summary>
        public const int BitPackedIntMin = -(1 << 29);

        /// <summary>
        /// Obsolete value that no longer carries meaning. Do not use.
        /// </summary>
        public const ulong BitPackedULongMax = (1L << 61) - 1;

        /// <summary>
        /// Obsolete value that no longer carries meaning. Do not use.
        /// </summary>
        public const long BitPackedLongMax = (1L << 60) - 1;

        /// <summary>
        /// Obsolete value that no longer carries meaning. Do not use.
        /// </summary>
        public const long BitPackedLongMin = -(1L << 60);

        /// <summary>
        /// Writes a 16-bit signed short to the buffer in a bit-encoded packed format.
        /// Zig-zag encoding is used to move the sign bit to the least significant bit, so that negative values
        /// are still able to be compressed.
        /// The first two bits indicate whether the value is 1, 2, or 3 bytes.
        /// If the value uses 14 bits or less, the remaining 14 bits contain the value.
        /// For performance, reasons, if the value is 15 bits or more, there will be six 0 bits, followed
        /// by the original unmodified 16-bit value in the next 2 bytes.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">The value to pack</param>
        public static void WriteValueBitPacked(FastBufferWriter writer, short value) => WriteValueBitPacked(writer, (ushort)Arithmetic.ZigZagEncode(value));

        /// <summary>
        /// Writes a 16-bit unsigned short to the buffer in a bit-encoded packed format.
        /// The first two bits indicate whether the value is 1, 2, or 3 bytes.
        /// If the value uses 14 bits or less, the remaining 14 bits contain the value.
        /// For performance, reasons, if the value is 15 bits or more, there will be six 0 bits, followed
        /// by the original unmodified 16-bit value in the next 2 bytes.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">The value to pack</param>
        public static void WriteValueBitPacked(FastBufferWriter writer, ushort value)
        {
            if (value > (1 << 14) - 1)
            {
                if (!writer.TryBeginWriteInternal(3))
                {
                    throw new OverflowException("Writing past the end of the buffer");
                }
                writer.WriteByte(3);
                writer.WriteValue(value);
                return;
            }

            value <<= 2;
            var numBytes = BitCounter.GetUsedByteCount(value);
            if (!writer.TryBeginWriteInternal(numBytes))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            writer.WritePartialValue(value | (ushort)(numBytes), numBytes);
        }

        /// <summary>
        /// Writes a 32-bit signed int to the buffer in a bit-encoded packed format.
        /// Zig-zag encoding is used to move the sign bit to the least significant bit, so that negative values
        /// are still able to be compressed.
        /// The first three bits indicate whether the value is 1, 2, 3, 4, or 5 bytes.
        /// If the value uses 29 bits or less, the remaining 29 bits contain the value.
        /// For performance, reasons, if the value is 30 bits or more, there will be five 0 bits, followed
        /// by the original unmodified 32-bit value in the next 4 bytes.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">The value to pack</param>
        public static void WriteValueBitPacked(FastBufferWriter writer, int value) => WriteValueBitPacked(writer, (uint)Arithmetic.ZigZagEncode(value));

        /// <summary>
        /// Writes a 32-bit unsigned int to the buffer in a bit-encoded packed format.
        /// The first three bits indicate whether the value is 1, 2, 3, 4, or 5 bytes.
        /// If the value uses 29 bits or less, the remaining 29 bits contain the value.
        /// For performance, reasons, if the value is 30 bits or more, there will be five 0 bits, followed
        /// by the original unmodified 32-bit value in the next 4 bytes.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">The value to pack</param>
        public static void WriteValueBitPacked(FastBufferWriter writer, uint value)
        {
            if (value > (1 << 29) - 1)
            {
                if (!writer.TryBeginWriteInternal(5))
                {
                    throw new OverflowException("Writing past the end of the buffer");
                }
                writer.WriteByte(5);
                writer.WriteValue(value);
                return;
            }

            value <<= 3;
            var numBytes = BitCounter.GetUsedByteCount(value);
            if (!writer.TryBeginWriteInternal(numBytes))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            writer.WritePartialValue(value | (uint)(numBytes), numBytes);
        }

        /// <summary>
        /// Writes a 64-bit signed long to the buffer in a bit-encoded packed format.
        /// Zig-zag encoding is used to move the sign bit to the least significant bit, so that negative values
        /// are still able to be compressed.
        /// The first four bits indicate whether the value is 1, 2, 3, 4, 5, 6, 7, 8, or 9 bytes.
        /// If the value uses 60 bits or less, the remaining 60 bits contain the value.
        /// For performance, reasons, if the value is 61 bits or more, there will be four 0 bits, followed
        /// by the original unmodified 64-bit value in the next 8 bytes.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">The value to pack</param>
        public static void WriteValueBitPacked(FastBufferWriter writer, long value) => WriteValueBitPacked(writer, Arithmetic.ZigZagEncode(value));

        /// <summary>
        /// Writes a 64-bit unsigned long to the buffer in a bit-encoded packed format.
        /// The first four bits indicate whether the value is 1, 2, 3, 4, 5, 6, 7, 8, or 9 bytes.
        /// If the value uses 60 bits or less, the remaining 60 bits contain the value.
        /// For performance, reasons, if the value is 61 bits or more, there will be four 0 bits, followed
        /// by the original unmodified 64-bit value in the next 8 bytes.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">The value to pack</param>
        public static void WriteValueBitPacked(FastBufferWriter writer, ulong value)
        {
            if (value > (1L << 60) - 1)
            {
                if (!writer.TryBeginWriteInternal(9))
                {
                    throw new OverflowException("Writing past the end of the buffer");
                }
                writer.WriteByte(9);
                writer.WriteValue(value);
                return;
            }

            value <<= 4;
            var numBytes = BitCounter.GetUsedByteCount(value);
            if (!writer.TryBeginWriteInternal(numBytes))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            writer.WritePartialValue(value | (uint)(numBytes), numBytes);
        }
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint ToUint<T>(T value) where T : unmanaged
        {
            uint* asUint = (uint*)&value;
            return *asUint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong ToUlong<T>(T value) where T : unmanaged
        {
            ulong* asUlong = (ulong*)&value;
            return *asUlong;
        }
    }
}
