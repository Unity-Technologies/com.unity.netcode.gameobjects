#define ARRAY_WRITE_PERMISSIVE // Allow attempt to write "packed" byte array (calls WriteByteArray())
#define ARRAY_RESOLVE_IMPLICIT // Include WriteArray() method with automatic type resolution
#define ARRAY_WRITE_PREMAP // Create a prefixed array diff mapping
#define ARRAY_DIFF_ALLOW_RESIZE // Whether or not to permit writing diffs of differently sized arrays

using System;
using System.Diagnostics;
using System.IO;
using MLAPI.Reflection;
using UnityEngine;

namespace MLAPI.Serialization
{
    // Improved version of NetworkWriter
    /// <summary>
    /// A BinaryWriter that can do bit wise manipulation when backed by a NetworkBuffer
    /// </summary>
    public class NetworkWriter
    {
        private Stream m_Sink;
        private NetworkBuffer m_NetworkSink;

        /// <summary>
        /// Creates a new NetworkWriter backed by a given stream
        /// </summary>
        /// <param name="stream">The stream to use for writing</param>
        public NetworkWriter(Stream stream)
        {
            m_Sink = stream;
            m_NetworkSink = stream as NetworkBuffer;
        }

        /// <summary>
        /// Changes the underlying stream the writer is writing to
        /// </summary>
        /// <param name="stream">The stream to write to</param>
        public void SetStream(Stream stream)
        {
            m_Sink = stream;
            m_NetworkSink = stream as NetworkBuffer;
        }

        internal Stream GetStream()
        {
            return m_Sink;
        }

        /// <summary>
        /// Writes a boxed object in a packed format
        /// </summary>
        /// <param name="value">The object to write</param>
        public void WriteObjectPacked(object value)
        {
            // Check unitys custom null checks
            bool isNull = value == null || (value is UnityEngine.Object && ((UnityEngine.Object)value) == null);

            if (isNull || value.GetType().IsNullable())
            {
                WriteBool(isNull);

                if (isNull)
                {
                    return;
                }
            }

            if (SerializationManager.TrySerialize(m_Sink, value))
            {
                return;
            }

            if (value is Array array)
            {
                var elementType = value.GetType().GetElementType();

                if (SerializationManager.IsTypeSupported(elementType))
                {
                    WriteInt32Packed(array.Length);

                    for (int i = 0; i < array.Length; i++)
                    {
                        WriteObjectPacked(array.GetValue(i));
                    }

                    return;
                }
            }
            else if (value is byte)
            {
                WriteByte((byte)value);
                return;
            }
            else if (value is sbyte)
            {
                WriteSByte((sbyte)value);
                return;
            }
            else if (value is ushort)
            {
                WriteUInt16Packed((ushort)value);
                return;
            }
            else if (value is short)
            {
                WriteInt16Packed((short)value);
                return;
            }
            else if (value is int)
            {
                WriteInt32Packed((int)value);
                return;
            }
            else if (value is uint)
            {
                WriteUInt32Packed((uint)value);
                return;
            }
            else if (value is long)
            {
                WriteInt64Packed((long)value);
                return;
            }
            else if (value is ulong)
            {
                WriteUInt64Packed((ulong)value);
                return;
            }
            else if (value is float)
            {
                WriteSinglePacked((float)value);
                return;
            }
            else if (value is double)
            {
                WriteDoublePacked((double)value);
                return;
            }
            else if (value is string)
            {
                WriteStringPacked((string)value);
                return;
            }
            else if (value is bool)
            {
                WriteBool((bool)value);
                return;
            }
            else if (value is Vector2)
            {
                WriteVector2Packed((Vector2)value);
                return;
            }
            else if (value is Vector3)
            {
                WriteVector3Packed((Vector3)value);
                return;
            }
            else if (value is Vector4)
            {
                WriteVector4Packed((Vector4)value);
                return;
            }
            else if (value is Color)
            {
                WriteColorPacked((Color)value);
                return;
            }
            else if (value is Color32)
            {
                WriteColor32((Color32)value);
                return;
            }
            else if (value is Ray)
            {
                WriteRayPacked((Ray)value);
                return;
            }
            else if (value is Quaternion)
            {
                WriteRotationPacked((Quaternion)value);
                return;
            }
            else if (value is char)
            {
                WriteCharPacked((char)value);
                return;
            }
            else if (value.GetType().IsEnum)
            {
                WriteInt32Packed((int)value);
                return;
            }
            else if (value is GameObject)
            {
                var networkObject = ((GameObject)value).GetComponent<NetworkObject>();
                if (ReferenceEquals(networkObject, null))
                {
                    throw new ArgumentException($"{nameof(NetworkWriter)} cannot write {nameof(GameObject)} types that does not has a {nameof(NetworkObject)} component attached. {nameof(GameObject)}: {((GameObject)value).name}");
                }

                if (!networkObject.IsSpawned)
                {
                    throw new ArgumentException($"{nameof(NetworkWriter)} cannot write {nameof(NetworkObject)} types that are not spawned. {nameof(GameObject)}: {((GameObject)value).name}");
                }

                WriteUInt64Packed(networkObject.NetworkObjectId);
                return;
            }
            else if (value is NetworkObject)
            {
                if (!((NetworkObject)value).IsSpawned)
                {
                    throw new ArgumentException($"{nameof(NetworkWriter)} cannot write {nameof(NetworkObject)} types that are not spawned. {nameof(GameObject)}: {((GameObject)value).name}");
                }

                WriteUInt64Packed(((NetworkObject)value).NetworkObjectId);
                return;
            }
            else if (value is NetworkBehaviour)
            {
                if (!((NetworkBehaviour)value).HasNetworkObject || !((NetworkBehaviour)value).NetworkObject.IsSpawned)
                {
                    throw new ArgumentException($"{nameof(NetworkWriter)} cannot write {nameof(NetworkBehaviour)} types that are not spawned. {nameof(GameObject)}: {((GameObject)value).name}");
                }

                WriteUInt64Packed(((NetworkBehaviour)value).NetworkObjectId);
                WriteUInt16Packed(((NetworkBehaviour)value).NetworkBehaviourId);
                return;
            }
            else if (value is INetworkSerializable)
            {
                ((INetworkSerializable)value).NetworkSerialize(new NetworkSerializer(this));
                return;
            }


            throw new ArgumentException($"{nameof(NetworkWriter)} cannot write type {value.GetType().Namespace}");
        }

        /// <summary>
        /// Write single-precision floating point value to the stream
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteSingle(float value)
        {
            WriteUInt32(new UIntFloat
            {
                FloatValue = value
            }.UIntValue);
        }

        /// <summary>
        /// Write double-precision floating point value to the stream
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteDouble(double value)
        {
            WriteUInt64(new UIntFloat
            {
                DoubleValue = value
            }.ULongValue);
        }

        /// <summary>
        /// Write single-precision floating point value to the stream as a varint
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteSinglePacked(float value)
        {
            WriteUInt32Packed(new UIntFloat
            {
                FloatValue = value
            }.UIntValue);
        }

        /// <summary>
        /// Write double-precision floating point value to the stream as a varint
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteDoublePacked(double value)
        {
            WriteUInt64Packed(new UIntFloat
            {
                DoubleValue = value
            }.ULongValue);
        }

        /// <summary>
        /// Convenience method that writes two non-packed Vector3 from the ray to the stream
        /// </summary>
        /// <param name="ray">Ray to write</param>
        public void WriteRay(Ray ray)
        {
            WriteVector3(ray.origin);
            WriteVector3(ray.direction);
        }

        /// <summary>
        /// Convenience method that writes two packed Vector3 from the ray to the stream
        /// </summary>
        /// <param name="ray">Ray to write</param>
        public void WriteRayPacked(Ray ray)
        {
            WriteVector3Packed(ray.origin);
            WriteVector3Packed(ray.direction);
        }

        /// <summary>
        /// Convenience method that writes two non-packed Vector2 from the ray to the stream
        /// </summary>
        /// <param name="ray2d">Ray2D to write</param>
        public void WriteRay2D(Ray2D ray2d)
        {
            WriteVector2(ray2d.origin);
            WriteVector2(ray2d.direction);
        }

        /// <summary>
        /// Convenience method that writes two packed Vector2 from the ray to the stream
        /// </summary>
        /// <param name="ray2d">Ray2D to write</param>
        public void WriteRay2DPacked(Ray2D ray2d)
        {
            WriteVector2Packed(ray2d.origin);
            WriteVector2Packed(ray2d.direction);
        }

        /// <summary>
        /// Convenience method that writes four non-varint floats from the color to the stream
        /// </summary>
        /// <param name="color">Color to write</param>
        public void WriteColor(Color color)
        {
            WriteSingle(color.r);
            WriteSingle(color.g);
            WriteSingle(color.b);
            WriteSingle(color.a);
        }

        /// <summary>
        /// Convenience method that writes four varint floats from the color to the stream
        /// </summary>
        /// <param name="color">Color to write</param>
        public void WriteColorPacked(Color color)
        {
            WriteSinglePacked(color.r);
            WriteSinglePacked(color.g);
            WriteSinglePacked(color.b);
            WriteSinglePacked(color.a);
        }

        /// <summary>
        /// Convenience method that writes four non-varint floats from the color to the stream
        /// </summary>
        /// <param name="color32">Color32 to write</param>
        public void WriteColor32(Color32 color32)
        {
            WriteByte(color32.r);
            WriteByte(color32.g);
            WriteByte(color32.b);
            WriteByte(color32.a);
        }

        /// <summary>
        /// Convenience method that writes two non-varint floats from the vector to the stream
        /// </summary>
        /// <param name="vector2">Vector to write</param>
        public void WriteVector2(Vector2 vector2)
        {
            WriteSingle(vector2.x);
            WriteSingle(vector2.y);
        }

        /// <summary>
        /// Convenience method that writes two varint floats from the vector to the stream
        /// </summary>
        /// <param name="vector2">Vector to write</param>
        public void WriteVector2Packed(Vector2 vector2)
        {
            WriteSinglePacked(vector2.x);
            WriteSinglePacked(vector2.y);
        }

        /// <summary>
        /// Convenience method that writes three non-varint floats from the vector to the stream
        /// </summary>
        /// <param name="vector3">Vector to write</param>
        public void WriteVector3(Vector3 vector3)
        {
            WriteSingle(vector3.x);
            WriteSingle(vector3.y);
            WriteSingle(vector3.z);
        }

        /// <summary>
        /// Convenience method that writes three varint floats from the vector to the stream
        /// </summary>
        /// <param name="vector3">Vector to write</param>
        public void WriteVector3Packed(Vector3 vector3)
        {
            WriteSinglePacked(vector3.x);
            WriteSinglePacked(vector3.y);
            WriteSinglePacked(vector3.z);
        }

        /// <summary>
        /// Convenience method that writes four non-varint floats from the vector to the stream
        /// </summary>
        /// <param name="vector4">Vector to write</param>
        public void WriteVector4(Vector4 vector4)
        {
            WriteSingle(vector4.x);
            WriteSingle(vector4.y);
            WriteSingle(vector4.z);
            WriteSingle(vector4.w);
        }

        /// <summary>
        /// Convenience method that writes four varint floats from the vector to the stream
        /// </summary>
        /// <param name="vector4">Vector to write</param>
        public void WriteVector4Packed(Vector4 vector4)
        {
            WriteSinglePacked(vector4.x);
            WriteSinglePacked(vector4.y);
            WriteSinglePacked(vector4.z);
            WriteSinglePacked(vector4.w);
        }

        /// <summary>
        /// Write a single-precision floating point value to the stream. The value is between (inclusive) the minValue and maxValue.
        /// </summary>
        /// <param name="value">Value to write</param>
        /// <param name="minValue">Minimum value that this value could be</param>
        /// <param name="maxValue">Maximum possible value that this could be</param>
        /// <param name="bytes">How many bytes the compressed result should occupy. Must be between 1 and 4 (inclusive)</param>
        public void WriteRangedSingle(float value, float minValue, float maxValue, int bytes)
        {
            if (bytes < 1 || bytes > 4) throw new ArgumentOutOfRangeException("Result must occupy between 1 and 4 bytes!");
            if (value < minValue || value > maxValue) throw new ArgumentOutOfRangeException("Given value does not match the given constraints!");
            uint result = (uint)(((value + minValue) / (maxValue + minValue)) * ((0x100 * bytes) - 1));
            for (int i = 0; i < bytes; ++i) m_Sink.WriteByte((byte)(result >> (i << 3)));
        }

        /// <summary>
        /// Write a double-precision floating point value to the stream. The value is between (inclusive) the minValue and maxValue.
        /// </summary>
        /// <param name="value">Value to write</param>
        /// <param name="minValue">Minimum value that this value could be</param>
        /// <param name="maxValue">Maximum possible value that this could be</param>
        /// <param name="bytes">How many bytes the compressed result should occupy. Must be between 1 and 8 (inclusive)</param>
        public void WriteRangedDouble(double value, double minValue, double maxValue, int bytes)
        {
            if (bytes < 1 || bytes > 8) throw new ArgumentOutOfRangeException("Result must occupy between 1 and 8 bytes!");
            if (value < minValue || value > maxValue) throw new ArgumentOutOfRangeException("Given value does not match the given constraints!");
            ulong result = (ulong)(((value + minValue) / (maxValue + minValue)) * ((0x100 * bytes) - 1));
            for (int i = 0; i < bytes; ++i) WriteByte((byte)(result >> (i << 3)));
        }

        /// <summary>
        /// Writes the rotation to the stream.
        /// </summary>
        /// <param name="rotation">Rotation to write</param>
        public void WriteRotationPacked(Quaternion rotation)
        {
            if (Mathf.Sign(rotation.w) < 0)
            {
                WriteSinglePacked(-rotation.x);
                WriteSinglePacked(-rotation.y);
                WriteSinglePacked(-rotation.z);
            }
            else
            {
                WriteSinglePacked(rotation.x);
                WriteSinglePacked(rotation.y);
                WriteSinglePacked(rotation.z);
            }
        }

        /// <summary>
        /// Writes the rotation to the stream.
        /// </summary>
        /// <param name="rotation">Rotation to write</param>
        public void WriteRotation(Quaternion rotation)
        {
            WriteSingle(rotation.x);
            WriteSingle(rotation.y);
            WriteSingle(rotation.z);
            WriteSingle(rotation.w);
        }

        /// <summary>
        /// Writes a single bit
        /// </summary>
        /// <param name="bit"></param>
        public void WriteBit(bool bit)
        {
            if (m_NetworkSink == null) throw new InvalidOperationException($"Cannot write bits on a non-{nameof(NetworkBuffer)} stream");
            m_NetworkSink.WriteBit(bit);
        }

        /// <summary>
        /// Writes a bool as a single bit
        /// </summary>
        /// <param name="value"></param>
        public void WriteBool(bool value)
        {
            if (m_NetworkSink == null)
            {
                m_Sink.WriteByte(value ? (byte)1 : (byte)0);
            }
            else
            {
                // WriteBit(value); // old (buggy)
                WriteByte(value ? (byte)1 : (byte)0); // new (hotfix)
            }
        }

        /// <summary>
        /// Writes pad bits to make the underlying stream aligned
        /// </summary>
        public void WritePadBits()
        {
            while (!m_NetworkSink.BitAligned) WriteBit(false);
        }

        /// <summary>
        /// Write the lower half (lower nibble) of a byte.
        /// </summary>
        /// <param name="value">Value containing nibble to write.</param>
        public void WriteNibble(byte value) => WriteBits(value, 4);

        /// <summary>
        /// Write either the upper or lower nibble of a byte to the stream.
        /// </summary>
        /// <param name="value">Value holding the nibble</param>
        /// <param name="upper">Whether or not the upper nibble should be written. True to write the four high bits, else writes the four low bits.</param>
        public void WriteNibble(byte value, bool upper) => WriteNibble((byte)(value >> (upper ? 4 : 0)));

        /// <summary>
        /// Write s certain amount of bits to the stream.
        /// </summary>
        /// <param name="value">Value to get bits from.</param>
        /// <param name="bitCount">Amount of bits to write</param>
        public void WriteBits(ulong value, int bitCount)
        {
            if (m_NetworkSink == null) throw new InvalidOperationException($"Cannot write bits on a non-{nameof(NetworkBuffer)} stream");
            if (bitCount > 64) throw new ArgumentOutOfRangeException(nameof(bitCount), "Cannot read more than 64 bits from a 64-bit value!");
            if (bitCount < 0) throw new ArgumentOutOfRangeException(nameof(bitCount), "Cannot read fewer than 0 bits!");
            int count = 0;
            for (; count + 8 < bitCount; count += 8) m_NetworkSink.WriteByte((byte)(value >> count));
            for (; count < bitCount; ++count) m_NetworkSink.WriteBit((value & (1UL << count)) != 0);
        }


        /// <summary>
        /// Write bits to stream.
        /// </summary>
        /// <param name="value">Value to get bits from.</param>
        /// <param name="bitCount">Amount of bits to write.</param>
        public void WriteBits(byte value, int bitCount)
        {
            if (m_NetworkSink == null) throw new InvalidOperationException($"Cannot write bits on a non-{nameof(NetworkBuffer)} stream");
            for (int i = 0; i < bitCount; ++i) m_NetworkSink.WriteBit(((value >> i) & 1) != 0);
        }

        /// <summary>
        /// Write a signed byte to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteSByte(sbyte value) => WriteByte((byte)value);

        /// <summary>
        /// Write a single character to the stream.
        /// </summary>
        /// <param name="c">Character to write</param>
        public void WriteChar(char c) => WriteUInt16(c);

        /// <summary>
        /// Write an unsigned short (UInt16) to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteUInt16(ushort value)
        {
            m_Sink.WriteByte((byte)value);
            m_Sink.WriteByte((byte)(value >> 8));
        }

        /// <summary>
        /// Write a signed short (Int16) to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteInt16(short value) => WriteUInt16((ushort)value);

        /// <summary>
        /// Write an unsigned int (UInt32) to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteUInt32(uint value)
        {
            m_Sink.WriteByte((byte)value);
            m_Sink.WriteByte((byte)(value >> 8));
            m_Sink.WriteByte((byte)(value >> 16));
            m_Sink.WriteByte((byte)(value >> 24));
        }

        /// <summary>
        /// Write a signed int (Int32) to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteInt32(int value) => WriteUInt32((uint)value);

        /// <summary>
        /// Write an unsigned long (UInt64) to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteUInt64(ulong value)
        {
            m_Sink.WriteByte((byte)value);
            m_Sink.WriteByte((byte)(value >> 8));
            m_Sink.WriteByte((byte)(value >> 16));
            m_Sink.WriteByte((byte)(value >> 24));
            m_Sink.WriteByte((byte)(value >> 32));
            m_Sink.WriteByte((byte)(value >> 40));
            m_Sink.WriteByte((byte)(value >> 48));
            m_Sink.WriteByte((byte)(value >> 56));
        }

        /// <summary>
        /// Write a signed long (Int64) to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteInt64(long value) => WriteUInt64((ulong)value);

        /// <summary>
        /// Write a signed short (Int16) as a ZigZag encoded varint to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteInt16Packed(short value) => WriteInt64Packed(value);

        /// <summary>
        /// Write an unsigned short (UInt16) as a varint to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteUInt16Packed(ushort value) => WriteUInt64Packed(value);

        /// <summary>
        /// Write a two-byte character as a varint to the stream.
        /// </summary>
        /// <param name="c">Value to write</param>
        public void WriteCharPacked(char c) => WriteUInt16Packed(c);

        /// <summary>
        /// Write a signed int (Int32) as a ZigZag encoded varint to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteInt32Packed(int value) => WriteInt64Packed(value);

        /// <summary>
        /// Write an unsigned int (UInt32) as a varint to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteUInt32Packed(uint value) => WriteUInt64Packed(value);

        /// <summary>
        /// Write a signed long (Int64) as a ZigZag encoded varint to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteInt64Packed(long value) => WriteUInt64Packed(Arithmetic.ZigZagEncode(value));

        /// <summary>
        /// Write an unsigned long (UInt64) as a varint to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteUInt64Packed(ulong value)
        {
            if (value <= 240) WriteULongByte(value);
            else if (value <= 2287)
            {
                WriteULongByte(((value - 240) >> 8) + 241);
                WriteULongByte(value - 240);
            }
            else if (value <= 67823)
            {
                WriteULongByte(249);
                WriteULongByte((value - 2288) >> 8);
                WriteULongByte(value - 2288);
            }
            else
            {
                ulong header = 255;
                ulong match = 0x00FF_FFFF_FFFF_FFFFUL;
                while (value <= match)
                {
                    --header;
                    match >>= 8;
                }

                WriteULongByte(header);
                int max = (int)(header - 247);
                for (int i = 0; i < max; ++i) WriteULongByte(value >> (i << 3));
            }
        }

        /// <summary>
        /// Write a byte (in an int format) to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        private void WriteIntByte(int value) => WriteByte((byte)value);

        /// <summary>
        /// Write a byte (in a ulong format) to the stream.
        /// </summary>
        /// <param name="byteValue">Value to write</param>
        private void WriteULongByte(ulong byteValue) => WriteByte((byte)byteValue);

        /// <summary>
        /// Write a byte to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteByte(byte value)
        {
            m_Sink.WriteByte(value);
        }

        // As it turns out, strings cannot be treated as char arrays, since strings use pointers to store data rather than C# arrays
        /// <summary>
        /// Writes a string
        /// </summary>
        /// <param name="s">The string to write</param>
        /// <param name="oneByteChars">Whether or not to use one byte per character. This will only allow ASCII</param>
        public void WriteString(string s, bool oneByteChars = false)
        {
            WriteUInt32Packed((uint)s.Length);
            int target = s.Length;
            for (int i = 0; i < target; ++i)
            {
                if (oneByteChars) WriteByte((byte)s[i]);
                else WriteChar(s[i]);
            }
        }

        /// <summary>
        /// Writes a string in a packed format
        /// </summary>
        /// <param name="s"></param>
        public void WriteStringPacked(string s)
        {
            WriteUInt32Packed((uint)s.Length);
            int target = s.Length;
            for (int i = 0; i < target; ++i) WriteCharPacked(s[i]);
        }

        /// <summary>
        /// Writes the diff between two strings
        /// </summary>
        /// <param name="write">The new array</param>
        /// <param name="compare">The previous array to use for diff</param>
        /// <param name="oneByteChars">Whether or not to use single byte chars. This will only allow ASCII characters</param>
        public void WriteStringDiff(string write, string compare, bool oneByteChars = false)
        {
#if !ARRAY_DIFF_ALLOW_RESIZE
            if (write.Length != compare.Length) throw new ArgumentException("Mismatched string lengths");
#endif
            WriteUInt32Packed((uint)write.Length);

            // Premapping
            int target;
#if ARRAY_WRITE_PREMAP
#if ARRAY_DIFF_ALLOW_RESIZE
            target = Math.Min(write.Length, compare.Length);
#else
            target = a1.Length;
#endif
            for (int i = 0; i < target; ++i) WriteBit(write[i] != compare[i]);
#else
            target = write.Length;
#endif
            for (int i = 0; i < target; ++i)
            {
                bool b = write[i] != compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b)
                {
                    if (oneByteChars) WriteByte((byte)write[i]);
                    else WriteChar(write[i]);
                }
            }
        }

        /// <summary>
        /// Writes the diff between two strings in a packed format
        /// </summary>
        /// <param name="write">The new string</param>
        /// <param name="compare">The previous string to use for diff</param>
        public void WriteStringPackedDiff(string write, string compare)
        {
#if !ARRAY_DIFF_ALLOW_RESIZE
            if (write.Length != compare.Length) throw new ArgumentException("Mismatched string lengths");
#endif
            WriteUInt32Packed((uint)write.Length);

            // Premapping
            int target;
#if ARRAY_WRITE_PREMAP
#if ARRAY_DIFF_ALLOW_RESIZE
            target = Math.Min(write.Length, compare.Length);
#else
            target = a1.Length;
#endif
            for (int i = 0; i < target; ++i) WriteBit(write[i] != compare[i]);
#else
            target = write.Length;
#endif
            for (int i = 0; i < target; ++i)
            {
                bool b = write[i] != compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteCharPacked(write[i]);
            }
        }

        private void CheckLengths(Array a1, Array a2) { }

        [Conditional("ARRAY_WRITE_PREMAP")]
        private void WritePremap(Array a1, Array a2)
        {
            long target;
            target = Math.Min(a1.LongLength, a2.LongLength);
            for (long i = 0; i < target; ++i) WriteBit(!a1.GetValue(i).Equals(a2.GetValue(i)));
            // TODO: Byte-align here
        }

        private ulong WriteArraySize(Array a1, Array a2, long length)
        {
            ulong write = (ulong)(length >= 0 ? length : a1.LongLength);
            if (length < 0)
            {
                if (length > a1.LongLength) throw new IndexOutOfRangeException("Cannot write more data than is available");
                WriteUInt64Packed(write);
            }

            return write;
        }

        /// <summary>
        /// Writes a byte array
        /// </summary>
        /// <param name="b">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteByteArray(byte[] b, long count = -1)
        {
            ulong target = WriteArraySize(b, null, count);
            for (ulong i = 0; i < target; ++i) m_Sink.WriteByte(b[i]);
        }


        /// <summary>
        /// WriteBytes
        /// Takes a byte array buffer and writes the bytes into the currently assigned stream at its current position
        /// This reduces the iterations required to write (n) bytes by a factor of up to 8x less iterations.
        /// for blocks of memory that exceed 8 bytes in size. It also doesn't require passing arrays over the stack.
        /// Ex:
        /// 256 bytes iterates 32 times vs 256 times ------------------------- 8x less iterations
        /// 64 bytes iterates 8 times vs 64 times----------------------------- 8x less iterations
        /// 22 bytes iterates 5 times ( 2-Int64 1-Int32 2-Byte) vs 22 times -- 4x less iterations
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="targetSize"></param>
        public void WriteBytes(byte[] buffer, long targetSize, int offset = 0)
        {
            long TargetSize = targetSize;
            long LargeInt64Blocks = TargetSize >> 3; //Divide by 8
            int IndexOffset = offset;
            //8 Byte blocks
            for (long i = 0; i < LargeInt64Blocks; i++)
            {
                WriteInt64(BitConverter.ToInt64(buffer, IndexOffset));
                IndexOffset += 8;
            }

            long Offset = LargeInt64Blocks * 8;
            long Remainder = TargetSize - Offset;

            //4 byte block
            if (Remainder >= 4)
            {
                WriteInt32(BitConverter.ToInt32(buffer, IndexOffset));
                IndexOffset += 4;
                Offset += 4;
            }

            //Remainder of bytes < 4
            if (TargetSize - Offset > 0)
            {
                for (long i = 0; i < (TargetSize - Offset); i++)
                {
                    WriteByte(buffer[IndexOffset + i]);
                }
            }
        }


        /// <summary>
        /// ReadAndWrite
        /// Uses a NetworkReader to read (targetSize) bytes and will write (targetSize) bytes to current stream.
        /// This reduces the iterations required to write (n) bytes by a factor of up to 8x less iterations.
        /// for blocks of memory that exceed 8 bytes in size. It also doesn't require passing arrays over the stack.
        /// Ex:
        /// 256 bytes iterates 32 times vs 256 times ------------------------- 8x less iterations
        /// 64 bytes iterates 8 times vs 64 times----------------------------- 8x less iterations
        /// 22 bytes iterates 5 times ( 2-Int64 1-Int32 2-Byte) vs 22 times -- 4x less iterations
        /// </summary>
        /// <param name="sourceReader"></param>
        /// <param name="targetSize"></param>
        public void ReadAndWrite(NetworkReader sourceReader, long targetSize)
        {
            long TargetSize = targetSize;
            long LargeInt64Blocks = TargetSize >> 3; //Divide by 8
            if (targetSize > 131072 || targetSize < 0)
            {
                return;
            }

            //8 Byte blocks
            for (long i = 0; i < LargeInt64Blocks; i++)
            {
                WriteInt64(sourceReader.ReadInt64());
            }

            long Offset = LargeInt64Blocks * 8;
            long Remainder = TargetSize - Offset;

            //4 byte block
            if (Remainder >= 4)
            {
                WriteInt32(sourceReader.ReadInt32());
                Offset += 4;
            }

            //Remainder of bytes < 4
            if (TargetSize - Offset > 0)
            {
                for (long i = 0; i < (TargetSize - Offset); i++)
                {
                    WriteByte(sourceReader.ReadByteDirect());
                }
            }
        }

        /// <summary>
        /// Writes the diff between two byte arrays
        /// </summary>
        /// <param name="write">The new array</param>
        /// <param name="compare">The previous array to use for diff</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteByteArrayDiff(byte[] write, byte[] compare, long count = -1)
        {
            CheckLengths(write, compare);
            long target = (long)WriteArraySize(write, compare, count);
            WritePremap(write, compare);
            for (long i = 0; i < target; ++i)
            {
                bool b = i >= compare.LongLength || write[i] != compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(b);
#endif
                if (b) WriteByte(write[i]);
            }
        }

        /// <summary>
        /// Writes a short array
        /// </summary>
        /// <param name="b">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteShortArray(short[] b, long count = -1)
        {
            ulong target = WriteArraySize(b, null, count);
            for (ulong i = 0; i < target; ++i) WriteInt16(b[i]);
        }

        /// <summary>
        /// Writes the diff between two short arrays
        /// </summary>
        /// <param name="write">The new array</param>
        /// <param name="compare">The previous array to use for diff</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteShortArrayDiff(short[] write, short[] compare, long count = -1)
        {
            CheckLengths(write, compare);
            long target = (long)WriteArraySize(write, compare, count);
            WritePremap(write, compare);
            for (long i = 0; i < target; ++i)
            {
                bool b = i >= compare.LongLength || write[i] != compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteInt16(write[i]);
            }
        }

        /// <summary>
        /// Writes a ushort array
        /// </summary>
        /// <param name="b">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteUShortArray(ushort[] b, long count = -1)
        {
            ulong target = WriteArraySize(b, null, count);
            for (ulong i = 0; i < target; ++i) WriteUInt16(b[i]);
        }

        /// <summary>
        /// Writes the diff between two ushort arrays
        /// </summary>
        /// <param name="write">The new array</param>
        /// <param name="compare">The previous array to use for diff</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteUShortArrayDiff(ushort[] write, ushort[] compare, long count = -1)
        {
            CheckLengths(write, compare);
            long target = (long)WriteArraySize(write, compare, count);
            WritePremap(write, compare);
            for (long i = 0; i < target; ++i)
            {
                bool b = i >= compare.LongLength || write[i] != compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteUInt16(write[i]);
            }
        }

        /// <summary>
        /// Writes a char array
        /// </summary>
        /// <param name="b">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteCharArray(char[] b, long count = -1)
        {
            ulong target = WriteArraySize(b, null, count);
            for (ulong i = 0; i < target; ++i) WriteChar(b[i]);
        }

        /// <summary>
        /// Writes the diff between two char arrays
        /// </summary>
        /// <param name="write">The new array</param>
        /// <param name="compare">The previous array to use for diff</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteCharArrayDiff(char[] write, char[] compare, long count = -1)
        {
            CheckLengths(write, compare);
            long target = (long)WriteArraySize(write, compare, count);
            WritePremap(write, compare);
            for (long i = 0; i < target; ++i)
            {
                bool b = i >= compare.LongLength || write[i] != compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteChar(write[i]);
            }
        }

        /// <summary>
        /// Writes a int array
        /// </summary>
        /// <param name="b">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteIntArray(int[] b, long count = -1)
        {
            ulong target = WriteArraySize(b, null, count);
            for (ulong i = 0; i < target; ++i) WriteInt32(b[i]);
        }

        /// <summary>
        /// Writes the diff between two int arrays
        /// </summary>
        /// <param name="write">The new array</param>
        /// <param name="compare">The previous array to use for diff</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteIntArrayDiff(int[] write, int[] compare, long count = -1)
        {
            CheckLengths(write, compare);
            long target = (long)WriteArraySize(write, compare, count);
            WritePremap(write, compare);
            for (long i = 0; i < target; ++i)
            {
                bool b = i >= compare.LongLength || write[i] != compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteInt32(write[i]);
            }
        }

        /// <summary>
        /// Writes a uint array
        /// </summary>
        /// <param name="b">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteUIntArray(uint[] b, long count = -1)
        {
            ulong target = WriteArraySize(b, null, count);
            for (ulong i = 0; i < target; ++i) WriteUInt32(b[i]);
        }

        /// <summary>
        /// Writes the diff between two uint arrays
        /// </summary>
        /// <param name="write">The new array</param>
        /// <param name="compare">The previous array to use for diff</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteUIntArrayDiff(uint[] write, uint[] compare, long count = -1)
        {
            CheckLengths(write, compare);
            long target = (long)WriteArraySize(write, compare, count);
            WritePremap(write, compare);
            for (long i = 0; i < target; ++i)
            {
                bool b = i >= compare.LongLength || write[i] != compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteUInt32(write[i]);
            }
        }

        /// <summary>
        /// Writes a long array
        /// </summary>
        /// <param name="b">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteLongArray(long[] b, long count = -1)
        {
            ulong target = WriteArraySize(b, null, count);
            for (ulong i = 0; i < target; ++i) WriteInt64(b[i]);
        }

        /// <summary>
        /// Writes the diff between two long arrays
        /// </summary>
        /// <param name="write">The new array</param>
        /// <param name="compare">The previous array to use for diff</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteLongArrayDiff(long[] write, long[] compare, long count = -1)
        {
            CheckLengths(write, compare);
            long target = (long)WriteArraySize(write, compare, count);
            WritePremap(write, compare);
            for (long i = 0; i < target; ++i)
            {
                bool b = i >= compare.LongLength || write[i] != compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteInt64(write[i]);
            }
        }

        /// <summary>
        /// Writes a ulong array
        /// </summary>
        /// <param name="b">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteULongArray(ulong[] b, long count = -1)
        {
            ulong target = WriteArraySize(b, null, count);
            for (ulong i = 0; i < target; ++i) WriteUInt64(b[i]);
        }

        /// <summary>
        /// Writes the diff between two ulong arrays
        /// </summary>
        /// <param name="write">The new array</param>
        /// <param name="compare">The previous array to use for diff</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteULongArrayDiff(ulong[] write, ulong[] compare, long count = -1)
        {
            CheckLengths(write, compare);
            long target = (long)WriteArraySize(write, compare, count);
            WritePremap(write, compare);
            for (long i = 0; i < target; ++i)
            {
                bool b = i >= compare.LongLength || write[i] != compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteUInt64(write[i]);
            }
        }

        /// <summary>
        /// Writes a float array
        /// </summary>
        /// <param name="b">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteFloatArray(float[] b, long count = -1)
        {
            ulong target = WriteArraySize(b, null, count);
            for (ulong i = 0; i < target; ++i) WriteSingle(b[i]);
        }

        /// <summary>
        /// Writes the diff between two float arrays
        /// </summary>
        /// <param name="write">The new array</param>
        /// <param name="compare">The previous array to use for diff</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteFloatArrayDiff(float[] write, float[] compare, long count = -1)
        {
            CheckLengths(write, compare);
            long target = (long)WriteArraySize(write, compare, count);
            WritePremap(write, compare);
            for (long i = 0; i < target; ++i)
            {
                bool b = i >= compare.LongLength || write[i] != compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteSingle(write[i]);
            }
        }

        /// <summary>
        /// Writes a double array
        /// </summary>
        /// <param name="b">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteDoubleArray(double[] b, long count = -1)
        {
            ulong target = WriteArraySize(b, null, count);
            for (ulong i = 0; i < target; ++i) WriteDouble(b[i]);
        }

        /// <summary>
        /// Writes the diff between two double arrays
        /// </summary>
        /// <param name="write">The new array</param>
        /// <param name="compare">The previous array to use for diff</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteDoubleArrayDiff(double[] write, double[] compare, long count = -1)
        {
            CheckLengths(write, compare);
            long target = (long)WriteArraySize(write, compare, count);
            WritePremap(write, compare);
            for (long i = 0; i < target; ++i)
            {
                bool b = i >= compare.LongLength || write[i] != compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteDouble(write[i]);
            }
        }


        // Packed arrays
#if ARRAY_RESOLVE_IMPLICIT
        /// <summary>
        /// Writes an array in a packed format
        /// </summary>
        /// <param name="a">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteArrayPacked(Array a, long count = -1)
        {
            var arrayType = a.GetType();


#if ARRAY_WRITE_PERMISSIVE
            if (arrayType == typeof(byte[])) WriteByteArray(a as byte[], count);
            else
#endif
            if (arrayType == typeof(short[])) WriteShortArrayPacked(a as short[], count);
            else if (arrayType == typeof(ushort[])) WriteUShortArrayPacked(a as ushort[], count);
            else if (arrayType == typeof(char[])) WriteCharArrayPacked(a as char[], count);
            else if (arrayType == typeof(int[])) WriteIntArrayPacked(a as int[], count);
            else if (arrayType == typeof(uint[])) WriteUIntArrayPacked(a as uint[], count);
            else if (arrayType == typeof(long[])) WriteLongArrayPacked(a as long[], count);
            else if (arrayType == typeof(ulong[])) WriteULongArrayPacked(a as ulong[], count);
            else if (arrayType == typeof(float[])) WriteFloatArrayPacked(a as float[], count);
            else if (arrayType == typeof(double[])) WriteDoubleArrayPacked(a as double[], count);
            else throw new InvalidDataException("Unknown array type! Please serialize manually!");
        }

        /// <summary>
        /// Writes the diff between two arrays in a packed format
        /// </summary>
        /// <param name="write">The new array</param>
        /// <param name="compare">The previous array to use for diff</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteArrayPackedDiff(Array write, Array compare, long count = -1)
        {
            var arrayType = write.GetType();
            if (arrayType != compare.GetType()) throw new ArrayTypeMismatchException("Cannot write diff of two differing array types");

#if ARRAY_WRITE_PERMISSIVE
            if (arrayType == typeof(byte[])) WriteByteArrayDiff(write as byte[], compare as byte[], count);
            else
#endif
            if (arrayType == typeof(short[])) WriteShortArrayPackedDiff(write as short[], compare as short[], count);
            else if (arrayType == typeof(ushort[])) WriteUShortArrayPackedDiff(write as ushort[], compare as ushort[], count);
            else if (arrayType == typeof(char[])) WriteCharArrayPackedDiff(write as char[], compare as char[], count);
            else if (arrayType == typeof(int[])) WriteIntArrayPackedDiff(write as int[], compare as int[], count);
            else if (arrayType == typeof(uint[])) WriteUIntArrayPackedDiff(write as uint[], compare as uint[], count);
            else if (arrayType == typeof(long[])) WriteLongArrayPackedDiff(write as long[], compare as long[], count);
            else if (arrayType == typeof(ulong[])) WriteULongArrayPackedDiff(write as ulong[], compare as ulong[], count);
            else if (arrayType == typeof(float[])) WriteFloatArrayPackedDiff(write as float[], compare as float[], count);
            else if (arrayType == typeof(double[])) WriteDoubleArrayPackedDiff(write as double[], compare as double[], count);
            else throw new InvalidDataException("Unknown array type! Please serialize manually!");
        }
#endif

        /// <summary>
        /// Writes a short array in a packed format
        /// </summary>
        /// <param name="b">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteShortArrayPacked(short[] b, long count = -1)
        {
            ulong target = WriteArraySize(b, null, count);
            for (ulong i = 0; i < target; ++i) WriteInt16Packed(b[i]);
        }

        /// <summary>
        /// Writes the diff between two short arrays in a packed format
        /// </summary>
        /// <param name="write">The new array</param>
        /// <param name="compare">The previous array to use for diff</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteShortArrayPackedDiff(short[] write, short[] compare, long count = -1)
        {
            CheckLengths(write, compare);
            long target = (long)WriteArraySize(write, compare, count);
            WritePremap(write, compare);
            for (long i = 0; i < target; ++i)
            {
                bool b = i >= compare.LongLength || write[i] != compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteInt16Packed(write[i]);
            }
        }

        /// <summary>
        /// Writes a ushort array in a packed format
        /// </summary>
        /// <param name="b">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteUShortArrayPacked(ushort[] b, long count = -1)
        {
            ulong target = WriteArraySize(b, null, count);
            for (ulong i = 0; i < target; ++i) WriteUInt16Packed(b[i]);
        }

        /// <summary>
        /// Writes the diff between two ushort arrays in a packed format
        /// </summary>
        /// <param name="write">The new array</param>
        /// <param name="compare">The previous array to use for diff</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteUShortArrayPackedDiff(ushort[] write, ushort[] compare, long count = -1)
        {
            CheckLengths(write, compare);
            long target = (long)WriteArraySize(write, compare, count);
            WritePremap(write, compare);
            for (long i = 0; i < target; ++i)
            {
                bool b = i >= compare.LongLength || write[i] != compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteUInt16Packed(write[i]);
            }
        }

        /// <summary>
        /// Writes a char array in a packed format
        /// </summary>
        /// <param name="b">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteCharArrayPacked(char[] b, long count = -1)
        {
            ulong target = WriteArraySize(b, null, count);
            for (ulong i = 0; i < target; ++i) WriteCharPacked(b[i]);
        }

        /// <summary>
        /// Writes the diff between two char arrays in a packed format
        /// </summary>
        /// <param name="write">The new array</param>
        /// <param name="compare">The previous array to use for diff</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteCharArrayPackedDiff(char[] write, char[] compare, long count = -1)
        {
            CheckLengths(write, compare);
            long target = (long)WriteArraySize(write, compare, count);
            WritePremap(write, compare);
            for (long i = 0; i < target; ++i)
            {
                bool b = i >= compare.LongLength || write[i] != compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteCharPacked(write[i]);
            }
        }

        /// <summary>
        /// Writes a int array in a packed format
        /// </summary>
        /// <param name="b">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteIntArrayPacked(int[] b, long count = -1)
        {
            ulong target = WriteArraySize(b, null, count);
            for (ulong i = 0; i < target; ++i) WriteInt32Packed(b[i]);
        }

        /// <summary>
        /// Writes the diff between two int arrays
        /// </summary>
        /// <param name="write">The new array</param>
        /// <param name="compare">The previous array to use for diff</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteIntArrayPackedDiff(int[] write, int[] compare, long count = -1)
        {
            CheckLengths(write, compare);
            long target = (long)WriteArraySize(write, compare, count);
            WritePremap(write, compare);
            for (long i = 0; i < target; ++i)
            {
                bool b = i >= compare.LongLength || write[i] != compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteInt32Packed(write[i]);
            }
        }

        /// <summary>
        /// Writes a uint array in a packed format
        /// </summary>
        /// <param name="b">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteUIntArrayPacked(uint[] b, long count = -1)
        {
            ulong target = WriteArraySize(b, null, count);
            for (ulong i = 0; i < target; ++i) WriteUInt32Packed(b[i]);
        }

        /// <summary>
        /// Writes the diff between two uing arrays in a packed format
        /// </summary>
        /// <param name="write">The new array</param>
        /// <param name="compare">The previous array to use for diff</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteUIntArrayPackedDiff(uint[] write, uint[] compare, long count = -1)
        {
            CheckLengths(write, compare);
            long target = (long)WriteArraySize(write, compare, count);
            WritePremap(write, compare);
            for (long i = 0; i < target; ++i)
            {
                bool b = i >= compare.LongLength || write[i] != compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteUInt32Packed(write[i]);
            }
        }

        /// <summary>
        /// Writes a long array in a packed format
        /// </summary>
        /// <param name="b">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteLongArrayPacked(long[] b, long count = -1)
        {
            ulong target = WriteArraySize(b, null, count);
            for (ulong i = 0; i < target; ++i) WriteInt64Packed(b[i]);
        }

        /// <summary>
        /// Writes the diff between two long arrays in a packed format
        /// </summary>
        /// <param name="write">The new array</param>
        /// <param name="compare">The previous array to use for diff</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteLongArrayPackedDiff(long[] write, long[] compare, long count = -1)
        {
            CheckLengths(write, compare);
            long target = (long)WriteArraySize(write, compare, count);
            WritePremap(write, compare);
            for (long i = 0; i < target; ++i)
            {
                bool b = i >= compare.LongLength || write[i] != compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteInt64Packed(write[i]);
            }
        }

        /// <summary>
        /// Writes a ulong array in a packed format
        /// </summary>
        /// <param name="b">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteULongArrayPacked(ulong[] b, long count = -1)
        {
            ulong target = WriteArraySize(b, null, count);
            for (ulong i = 0; i < target; ++i) WriteUInt64Packed(b[i]);
        }

        /// <summary>
        /// Writes the diff between two ulong arrays in a packed format
        /// </summary>
        /// <param name="write">The new array</param>
        /// <param name="compare">The previous array to use for diff</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteULongArrayPackedDiff(ulong[] write, ulong[] compare, long count = -1)
        {
            CheckLengths(write, compare);
            long target = (long)WriteArraySize(write, compare, count);
            WritePremap(write, compare);
            for (long i = 0; i < target; ++i)
            {
                bool b = i >= compare.LongLength || write[i] != compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteUInt64Packed(write[i]);
            }
        }

        /// <summary>
        /// Writes a float array in a packed format
        /// </summary>
        /// <param name="b">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteFloatArrayPacked(float[] b, long count = -1)
        {
            ulong target = WriteArraySize(b, null, count);
            for (ulong i = 0; i < target; ++i) WriteSinglePacked(b[i]);
        }

        /// <summary>
        /// Writes the diff between two float arrays in a packed format
        /// </summary>
        /// <param name="write">The new array</param>
        /// <param name="compare">The previous array to use for diff</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteFloatArrayPackedDiff(float[] write, float[] compare, long count = -1)
        {
            CheckLengths(write, compare);
            long target = (long)WriteArraySize(write, compare, count);
            WritePremap(write, compare);
            for (long i = 0; i < target; ++i)
            {
                bool b = i >= compare.LongLength || write[i] != compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteSinglePacked(write[i]);
            }
        }

        /// <summary>
        /// Writes a double array in a packed format
        /// </summary>
        /// <param name="b">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteDoubleArrayPacked(double[] b, long count = -1)
        {
            ulong target = WriteArraySize(b, null, count);
            for (ulong i = 0; i < target; ++i) WriteDoublePacked(b[i]);
        }

        /// <summary>
        /// Writes the diff between two double arrays in a packed format
        /// </summary>
        /// <param name="write">The new array</param>
        /// <param name="compare">The previous array to use for diff</param>
        /// <param name="count">The amount of elements to write</param>
        public void WriteDoubleArrayPackedDiff(double[] write, double[] compare, long count = -1)
        {
            CheckLengths(write, compare);
            long target = (long)WriteArraySize(write, compare, count);
            WritePremap(write, compare);
            for (long i = 0; i < target; ++i)
            {
                bool b = i >= compare.LongLength || write[i] != compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteDoublePacked(write[i]);
            }
        }
    }
}
