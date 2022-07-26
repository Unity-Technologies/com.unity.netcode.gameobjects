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
            WriteUInt32Packed(writer, ToUint(value));
        }

        /// <summary>
        /// Write double-precision floating point value to the buffer as a varint
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, double value)
        {
            WriteUInt64Packed(writer, ToUlong(value));
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
        public static void WriteValuePacked(FastBufferWriter writer, short value) => WriteUInt32Packed(writer, (ushort)Arithmetic.ZigZagEncode(value));

        /// <summary>
        /// Write an unsigned short (UInt16) as a varint to the buffer.
        /// WARNING: If the value you're writing is > 2287, this will use MORE space
        /// (3 bytes instead of 2), and if your value is > 240 you'll get no savings at all.
        /// Only use this if you're certain your value will be small.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, ushort value) => WriteUInt32Packed(writer, value);

        /// <summary>
        /// Write a two-byte character as a varint to the buffer.
        /// WARNING: If the value you're writing is > 2287, this will use MORE space
        /// (3 bytes instead of 2), and if your value is > 240 you'll get no savings at all.
        /// Only use this if you're certain your value will be small.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="c">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, char c) => WriteUInt32Packed(writer, c);

        /// <summary>
        /// Write a signed int (Int32) as a ZigZag encoded varint to the buffer.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, int value) => WriteUInt32Packed(writer, (uint)Arithmetic.ZigZagEncode(value));

        /// <summary>
        /// Write an unsigned int (UInt32) to the buffer.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, uint value) => WriteUInt32Packed(writer, value);

        /// <summary>
        /// Write an unsigned long (UInt64) to the buffer.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, ulong value) => WriteUInt64Packed(writer, value);

        /// <summary>
        /// Write a signed long (Int64) as a ZigZag encoded varint to the buffer.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteValuePacked(FastBufferWriter writer, long value) => WriteUInt64Packed(writer, Arithmetic.ZigZagEncode(value));

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
        /// Maximum serializable value for a BitPacked ushort (minimum for unsigned is 0)
        /// </summary>
        public const ushort BitPackedUshortMax = (1 << 15) - 1;

        /// <summary>
        /// Maximum serializable value for a BitPacked short
        /// </summary>
        public const short BitPackedShortMax = (1 << 14) - 1;

        /// <summary>
        /// Minimum serializable value size for a BitPacked ushort
        /// </summary>
        public const short BitPackedShortMin = -(1 << 14);

        /// <summary>
        /// Maximum serializable value for a BitPacked uint (minimum for unsigned is 0)
        /// </summary>
        public const uint BitPackedUintMax = (1 << 30) - 1;

        /// <summary>
        /// Maximum serializable value for a BitPacked int
        /// </summary>
        public const int BitPackedIntMax = (1 << 29) - 1;

        /// <summary>
        /// Minimum serializable value size for a BitPacked int
        /// </summary>
        public const int BitPackedIntMin = -(1 << 29);

        /// <summary>
        /// Maximum serializable value for a BitPacked ulong (minimum for unsigned is 0)
        /// </summary>
        public const ulong BitPackedULongMax = (1L << 61) - 1;

        /// <summary>
        /// Maximum serializable value for a BitPacked long
        /// </summary>
        public const long BitPackedLongMax = (1L << 60) - 1;

        /// <summary>
        /// Minimum serializable value size for a BitPacked long
        /// </summary>
        public const long BitPackedLongMin = -(1L << 60);

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
        public static void WriteValueBitPacked(FastBufferWriter writer, short value) => WriteValueBitPacked(writer, (ushort)Arithmetic.ZigZagEncode(value));

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
        public static void WriteValueBitPacked(FastBufferWriter writer, ushort value)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (value >= BitPackedUshortMax)
            {
                throw new ArgumentException("BitPacked ushorts must be <= 15 bits");
            }
#endif

            if (value <= 0b0111_1111)
            {
                if (!writer.TryBeginWriteInternal(1))
                {
                    throw new OverflowException("Writing past the end of the buffer");
                }
                writer.WriteByte((byte)(value << 1));
                return;
            }

            if (!writer.TryBeginWriteInternal(2))
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
        public static void WriteValueBitPacked(FastBufferWriter writer, int value) => WriteValueBitPacked(writer, (uint)Arithmetic.ZigZagEncode(value));

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
        public static void WriteValueBitPacked(FastBufferWriter writer, uint value)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (value > BitPackedUintMax)
            {
                throw new ArgumentException("BitPacked uints must be <= 30 bits");
            }
#endif
            value <<= 2;
            var numBytes = BitCounter.GetUsedByteCount(value);
            if (!writer.TryBeginWriteInternal(numBytes))
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
        public static void WriteValueBitPacked(FastBufferWriter writer, long value) => WriteValueBitPacked(writer, Arithmetic.ZigZagEncode(value));

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
        public static void WriteValueBitPacked(FastBufferWriter writer, ulong value)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (value > BitPackedULongMax)
            {
                throw new ArgumentException("BitPacked ulongs must be <= 61 bits");
            }
#endif
            value <<= 3;
            var numBytes = BitCounter.GetUsedByteCount(value);
            if (!writer.TryBeginWriteInternal(numBytes))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            writer.WritePartialValue(value | (uint)(numBytes - 1), numBytes);
        }
#endif

        private static void WriteUInt64Packed(FastBufferWriter writer, ulong value)
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

            if (!writer.TryBeginWriteInternal(writeBytes + 1))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            writer.WriteByte((byte)(247 + writeBytes));
            writer.WritePartialValue(value, writeBytes);
        }

        // Looks like the same code as WriteUInt64Packed?
        // It's actually different because it will call the more efficient 32-bit version
        // of BytewiseUtility.GetUsedByteCount().
        private static void WriteUInt32Packed(FastBufferWriter writer, uint value)
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

            if (!writer.TryBeginWriteInternal(writeBytes + 1))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            writer.WriteByte((byte)(247 + writeBytes));
            writer.WritePartialValue(value, writeBytes);
        }

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
