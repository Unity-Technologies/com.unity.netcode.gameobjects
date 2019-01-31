#define ARRAY_WRITE_PERMISSIVE  // Allow attempt to write "packed" byte array (calls WriteByteArray())
#define ARRAY_RESOLVE_IMPLICIT  // Include WriteArray() method with automatic type resolution
#define ARRAY_WRITE_PREMAP      // Create a prefixed array diff mapping
#define ARRAY_DIFF_ALLOW_RESIZE // Whether or not to permit writing diffs of differently sized arrays

using System;
using System.IO;
using System.Text;
using MLAPI.Components;
using MLAPI.Internal;
using MLAPI.Logging;
using UnityEngine;

namespace MLAPI.Serialization
{
    /// <summary>
    /// A BinaryReader that can do bit wise manipulation when backed by a BitStream
    /// </summary>
    public class BitReader
    {
        private Stream source;
        private BitStream bitSource;

        /// <summary>
        /// Creates a new BitReader backed by a given stream
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        public BitReader(Stream stream)
        {
            source = stream;
            bitSource = stream as BitStream;
        }

        /// <summary>
        /// Changes the underlying stream the reader is reading from
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        public void SetStream(Stream stream)
        {
            source = stream;
            bitSource = stream as BitStream;
        }

        /// <summary>
        /// Reads a single byte
        /// </summary>
        /// <returns>The byte read as an integer</returns>
        public int ReadByte() => source.ReadByte();

        /// <summary>
        /// Reads a byte
        /// </summary>
        /// <returns>The byte read</returns>
        public byte ReadByteDirect() => (byte)source.ReadByte();

        /// <summary>
        /// Reads a single bit
        /// </summary>
        /// <returns>The bit read</returns>
        public bool ReadBit()
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            return bitSource.ReadBit();
        }

        /// <summary>
        /// Reads a single bit
        /// </summary>
        /// <returns>The bit read</returns>
        public bool ReadBool()
        {
            if (bitSource == null) return source.ReadByte() != 0;
            else return ReadBit();
        }

        /// <summary>
        /// Skips pad bits and aligns the position to the next byte
        /// </summary>
        public void SkipPadBits()
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            while (!bitSource.BitAligned) ReadBit();
        }

        /// <summary>
        /// Reads a single boxed object of a given type in a packed format
        /// </summary>
        /// <param name="type">The type to read</param>
        /// <returns>Returns the boxed read object</returns>
        public object ReadObjectPacked(Type type)
        {
            if (type == typeof(byte))
                return ReadByteDirect();
            if (type == typeof(sbyte))
                return ReadSByte();
            if (type == typeof(ushort))
                return ReadUInt16Packed();
            if (type == typeof(short))
                return ReadInt16Packed();
            if (type == typeof(int))
                return ReadInt32Packed();
            if (type == typeof(uint))
                return ReadUInt32Packed();
            if (type == typeof(long))
                return ReadInt64Packed();
            if (type == typeof(ulong))
                return ReadUInt64Packed();
            if (type == typeof(float))
                return ReadSinglePacked();
            if (type == typeof(double))
                return ReadDoublePacked();
            if (type == typeof(string))
                return ReadStringPacked().ToString();
            if (type == typeof(bool))
                return ReadBool();
            if (type == typeof(Vector2))
                return ReadVector2Packed();
            if (type == typeof(Vector3))
                return ReadVector3Packed();
            if (type == typeof(Vector4))
                return ReadVector4Packed();
            if (type == typeof(Color))
                return ReadColorPacked();
            if (type == typeof(Color32))
                return ReadColor32();
            if (type == typeof(Ray))
                return ReadRayPacked();
            if (type == typeof(Quaternion))
                return ReadRotation(3);
            if (type == typeof(char))
                return ReadCharPacked();
            if (type.IsEnum)
                return ReadInt32Packed();
            if (type == typeof(GameObject))
            {
                uint networkId = ReadUInt32Packed();
                if (SpawnManager.SpawnedObjects.ContainsKey(networkId)) 
                {
                    return SpawnManager.SpawnedObjects[networkId].gameObject;
                }
                else 
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal)
                        LogHelper.LogWarning("BitReader canot find the GameObject sent in the SpawnedObjects list, it may have been destroyed. NetworkId: " + networkId.ToString());
                    return null;
                }
            }
            if (type == typeof(NetworkedObject))
            {
                uint networkId = ReadUInt32Packed();
                if (SpawnManager.SpawnedObjects.ContainsKey(networkId)) 
                {
                    return SpawnManager.SpawnedObjects[networkId];
                }
                else 
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal)
                        LogHelper.LogWarning("BitReader canot find the NetworkedObject sent in the SpawnedObjects list, it may have been destroyed. NetworkId: " + networkId.ToString());
                    return null;
                }
            }
            if (type == typeof(NetworkedBehaviour))
            {
                uint networkId = ReadUInt32Packed();
                ushort behaviourId = ReadUInt16Packed();
                if (SpawnManager.SpawnedObjects.ContainsKey(networkId)) 
                {
                    return SpawnManager.SpawnedObjects[networkId].GetBehaviourAtOrderIndex(behaviourId);
                }
                else 
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal)
                        LogHelper.LogWarning("BitReader canot find the NetworkedBehaviour sent in the SpawnedObjects list, it may have been destroyed. NetworkId: " + networkId.ToString());
                    return null;
                }
            }
            if (typeof(IBitWritable).IsAssignableFrom(type))
            {
                object instance = Activator.CreateInstance(type);
                ((IBitWritable)instance).Read(this.source);
                return instance;
            }
          
            throw new ArgumentException("BitReader cannot read type " + type.Name);
        }

        /// <summary>
        /// Read a single-precision floating point value from the stream.
        /// </summary>
        /// <returns>The read value</returns>
        public float ReadSingle()
        {
            return new UIntFloat
            {
                uintValue = ReadUInt32()
            }.floatValue;
        }


        /// <summary>
        /// Read a double-precision floating point value from the stream.
        /// </summary>
        /// <returns>The read value</returns>
        public double ReadDouble()
        {
            return new UIntFloat
            {
                ulongValue = ReadUInt64()
            }.doubleValue;
        }

        /// <summary>
        /// Read a single-precision floating point value from the stream from a varint
        /// </summary>
        /// <returns>The read value</returns>
        public float ReadSinglePacked()
        {
            return new UIntFloat
            {
                uintValue = ReadUInt32Packed()
            }.floatValue;
        }

        /// <summary>
        /// Read a double-precision floating point value from the stream as a varint
        /// </summary>
        /// <returns>The read value</returns>
        public double ReadDoublePacked()
        {
            return new UIntFloat
            {
                ulongValue = ReadUInt64Packed()
            }.doubleValue;
        }

        /// <summary>
        /// Read a Vector2 from the stream.
        /// </summary>
        /// <returns>The Vector2 read from the stream.</returns>
        public Vector2 ReadVector2() => new Vector2(ReadSingle(), ReadSingle());

        /// <summary>
        /// Read a Vector2 from the stream.
        /// </summary>
        /// <returns>The Vector2 read from the stream.</returns>
        public Vector2 ReadVector2Packed() => new Vector2(ReadSinglePacked(), ReadSinglePacked());

        /// <summary>
        /// Read a Vector3 from the stream.
        /// </summary>
        /// <returns>The Vector3 read from the stream.</returns>
        public Vector3 ReadVector3() => new Vector3(ReadSingle(), ReadSingle(), ReadSingle());

        /// <summary>
        /// Read a Vector3 from the stream.
        /// </summary>
        /// <returns>The Vector3 read from the stream.</returns>
        public Vector3 ReadVector3Packed() => new Vector3(ReadSinglePacked(), ReadSinglePacked(), ReadSinglePacked());

        /// <summary>
        /// Read a Vector4 from the stream.
        /// </summary>
        /// <returns>The Vector4 read from the stream.</returns>
        public Vector4 ReadVector4() => new Vector4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());

        /// <summary>
        /// Read a Vector4 from the stream.
        /// </summary>
        /// <returns>The Vector4 read from the stream.</returns>
        public Vector4 ReadVector4Packed() => new Vector4(ReadSinglePacked(), ReadSinglePacked(), ReadSinglePacked(), ReadSinglePacked());

        /// <summary>
        /// Read a Color from the stream.
        /// </summary>
        /// <returns>The Color read from the stream.</returns>
        public Color ReadColor() => new Color(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());

        /// <summary>
        /// Read a Color from the stream.
        /// </summary>
        /// <returns>The Color read from the stream.</returns>
        public Color ReadColorPacked() => new Color(ReadSinglePacked(), ReadSinglePacked(), ReadSinglePacked(), ReadSinglePacked());

        /// <summary>
        /// Read a Color32 from the stream.
        /// </summary>
        /// <returns>The Color32 read from the stream.</returns>
        public Color32 ReadColor32() => new Color32((byte)ReadByte(), (byte)ReadByte(), (byte)ReadByte(), (byte)ReadByte());

        /// <summary>
        /// Read a Ray from the stream.
        /// </summary>
        /// <returns>The Ray read from the stream.</returns>
        public Ray ReadRay() => new Ray(ReadVector3(), ReadVector3());

        /// <summary>
        /// Read a Ray from the stream.
        /// </summary>
        /// <returns>The Ray read from the stream.</returns>
        public Ray ReadRayPacked() => new Ray(ReadVector3Packed(), ReadVector3Packed());

        /// <summary>
        /// Read a single-precision floating point value from the stream. The value is between (inclusive) the minValue and maxValue.
        /// </summary>
        /// <param name="minValue">Minimum value that this value could be</param>
        /// <param name="maxValue">Maximum possible value that this could be</param>
        /// <param name="bytes">How many bytes the compressed value occupies. Must be between 1 and 4 (inclusive)</param>
        /// <returns>The read value</returns>
        public float ReadRangedSingle(float minValue, float maxValue, int bytes)
        {
            if (bytes < 1 || bytes > 4) throw new ArgumentOutOfRangeException("Result must occupy between 1 and 4 bytes!");
            uint read = 0;
            for (int i = 0; i < bytes; ++i) read |= (uint)ReadByte() << (i << 3);
            return (((float)read / ((0x100 * bytes) - 1)) * (minValue + maxValue)) - minValue;
        }

        /// <summary>
        /// read a double-precision floating point value from the stream. The value is between (inclusive) the minValue and maxValue.
        /// </summary>
        /// <param name="minValue">Minimum value that this value could be</param>
        /// <param name="maxValue">Maximum possible value that this could be</param>
        /// <param name="bytes">How many bytes the compressed value occupies. Must be between 1 and 8 (inclusive)</param>
        /// <returns>The read value</returns>
        public double ReadRangedDouble(double minValue, double maxValue, int bytes)
        {
            if (bytes < 1 || bytes > 8) throw new ArgumentOutOfRangeException("Result must occupy between 1 and 8 bytes!");
            ulong read = 0;
            for (int i = 0; i < bytes; ++i) read |= (ulong)ReadByte() << (i << 3);
            return (((double)read / ((0x100 * bytes) - 1)) * (minValue + maxValue)) - minValue;
        }

        /// <summary>
        /// Read a rotation from the stream.
        /// </summary>
        /// <param name="bytesPerAngle">How many bytes each angle occupies. Must be between 1 and 4 (inclusive)</param>
        /// <returns>The rotation read from the stream</returns>
        public Quaternion ReadRotation(int bytesPerAngle)
        {
            if (bytesPerAngle < 1 || bytesPerAngle > 4) throw new ArgumentOutOfRangeException("Bytes per angle must be at least 1 byte and at most 4 bytes!");
            if (bytesPerAngle == 4) return Quaternion.Euler(ReadVector3());
            else return Quaternion.Euler(
                ReadRangedSingle(0f, 360f, bytesPerAngle),  // X
                ReadRangedSingle(0f, 360f, bytesPerAngle),  // Y
                ReadRangedSingle(0f, 360f, bytesPerAngle)   // Z
                );
        }

        /// <summary>
        /// Read a certain amount of bits from the stream.
        /// </summary>
        /// <param name="bitCount">How many bits to read. Minimum 0, maximum 8.</param>
        /// <returns>The bits that were read</returns>
        public ulong ReadBits(int bitCount)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            if (bitCount > 64) throw new ArgumentOutOfRangeException("Cannot read more than 64 bits into a 64-bit value!");
            if (bitCount < 0) throw new ArgumentOutOfRangeException("Cannot read fewer than 0 bits!");
            ulong read = 0;
            for (int i = 0; i + 8 < bitCount; i += 8) read |= (ulong)ReadByte() << i;
            read |= (ulong)ReadByteBits(bitCount & 7) << (bitCount & ~7);
            return read;
        }

        /// <summary>
        /// Read a certain amount of bits from the stream.
        /// </summary>
        /// <param name="bitCount">How many bits to read. Minimum 0, maximum 64.</param>
        /// <returns>The bits that were read</returns>
        public byte ReadByteBits(int bitCount)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            if (bitCount > 8) throw new ArgumentOutOfRangeException("Cannot read more than 8 bits into an 8-bit value!");
            if (bitCount < 0) throw new ArgumentOutOfRangeException("Cannot read fewer than 0 bits!");
            
            int result = 0;
            ByteBool convert = new ByteBool();
            for (int i = 0; i < bitCount; ++i)
                result |= convert.Collapse(ReadBit()) << i;
            return (byte) result;
        }

        /// <summary>
        /// Read a nibble (4 bits) from the stream.
        /// </summary>
        /// <param name="asUpper">Whether or not the nibble should be left-shifted by 4 bits</param>
        /// <returns>The nibble that was read</returns>
        public byte ReadNibble(bool asUpper)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            ByteBool convert = new ByteBool();
            
            byte result = (byte) (
                convert.Collapse(ReadBit()) |
                (convert.Collapse(ReadBit()) << 1) |
                (convert.Collapse(ReadBit()) << 2) |
                (convert.Collapse(ReadBit()) << 3)
            );
            if (asUpper) result <<= 4;
            return result;

        }

        // Marginally faster than the one that accepts a bool
        /// <summary>
        /// Read a nibble (4 bits) from the stream.
        /// </summary>
        /// <returns>The nibble that was read</returns>
        public byte ReadNibble()
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            ByteBool convert = new ByteBool();
            return (byte) (
                convert.Collapse(ReadBit()) |
                (convert.Collapse(ReadBit()) << 1) |
                (convert.Collapse(ReadBit()) << 2) |
                (convert.Collapse(ReadBit()) << 3)
            );
        }

        /// <summary>
        /// Reads a signed byte
        /// </summary>
        /// <returns>Value read from stream.</returns>
        public sbyte ReadSByte() => (sbyte)ReadByte();
        /// <summary>
        /// Read an unsigned short (UInt16) from the stream.
        /// </summary>
        /// <returns>Value read from stream.</returns>
        public ushort ReadUInt16() => (ushort)(ReadByte() | (ReadByte() << 8));
        /// <summary>
        /// Read a signed short (Int16) from the stream.
        /// </summary>
        /// <returns>Value read from stream.</returns>
        public short ReadInt16() => (short)ReadUInt16();
        /// <summary>
        /// Read a single character from the stream
        /// </summary>
        /// <returns>Value read from stream.</returns>
        public char ReadChar() => (char)ReadUInt16();
        /// <summary>
        /// Read an unsigned int (UInt32) from the stream.
        /// </summary>
        /// <returns>Value read from stream.</returns>
        public uint ReadUInt32() => (uint)(ReadByte() | (ReadByte() << 8) | (ReadByte() << 16) | (ReadByte() << 24));
        /// <summary>
        /// Read a signed int (Int32) from the stream.
        /// </summary>
        /// <returns>Value read from stream.</returns>
        public int ReadInt32() => (int)ReadUInt32();
        /// <summary>
        /// Read an unsigned long (UInt64) from the stream.
        /// </summary>
        /// <returns>Value read from stream.</returns>
        public ulong ReadUInt64() => (
                ((uint)ReadByte()) |
                ((ulong)ReadByte() << 8) |
                ((ulong)ReadByte() << 16) |
                ((ulong)ReadByte() << 24) |
                ((ulong)ReadByte() << 32) |
                ((ulong)ReadByte() << 40) |
                ((ulong)ReadByte() << 48) |
                ((ulong)ReadByte() << 56)
                );
        /// <summary>
        /// Read a signed long (Int64) from the stream.
        /// </summary>
        /// <returns>Value read from stream.</returns>
        public long ReadInt64() => (long)ReadUInt64();

        /// <summary>
        /// Read a ZigZag encoded varint signed short (Int16) from the stream.
        /// </summary>
        /// <returns>Decoded un-varinted value.</returns>
        public short ReadInt16Packed() => (short)Arithmetic.ZigZagDecode(ReadUInt64Packed());
        /// <summary>
        /// Read a varint unsigned short (UInt16) from the stream.
        /// </summary>
        /// <returns>Un-varinted value.</returns>
        public ushort ReadUInt16Packed() => (ushort)ReadUInt64Packed();
        /// <summary>
        /// Read a varint two-byte character from the stream.
        /// </summary>
        /// <returns>Un-varinted value.</returns>
        public char ReadCharPacked() => (char)ReadUInt16Packed();
        /// <summary>
        /// Read a ZigZag encoded varint signed int (Int32) from the stream.
        /// </summary>
        /// <returns>Decoded un-varinted value.</returns>
        public int ReadInt32Packed() => (int)Arithmetic.ZigZagDecode(ReadUInt64Packed());
        /// <summary>
        /// Read a varint unsigned int (UInt32) from the stream.
        /// </summary>
        /// <returns>Un-varinted value.</returns>
        public uint ReadUInt32Packed() => (uint)ReadUInt64Packed();
        /// <summary>
        /// Read a ZigZag encoded varint signed long(Int64) from the stream.
        /// </summary>
        /// <returns>Decoded un-varinted value.</returns>
        public long ReadInt64Packed() => Arithmetic.ZigZagDecode(ReadUInt64Packed());
        /// <summary>
        /// Read a varint unsigned long (UInt64) from the stream.
        /// </summary>
        /// <returns>Un-varinted value.</returns>
        public ulong ReadUInt64Packed()
        {
            ulong header = ReadByteDirect();
            if (header <= 240) return header;
            if (header <= 248) return 240 + ((header - 241) << 8) + ReadByteDirect();
            if (header == 249) return 2288UL + (ulong)(ReadByte() << 8) + ReadByteDirect();
            ulong res = ReadByteDirect() | ((ulong)ReadByteDirect() << 8) | ((ulong)ReadByte() << 16);
            int cmp = 2;
            int hdr = (int)(header - 247);
            while (hdr > ++cmp) res |= (ulong)ReadByte() << (cmp << 3);
            return res;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        // Read arrays
        public StringBuilder ReadString(bool oneByteChars) => ReadString(null, oneByteChars);
        public StringBuilder ReadString(StringBuilder builder = null, bool oneByteChars = false)
        {
            int expectedLength = (int)ReadUInt32Packed();
            if (builder == null) builder = new StringBuilder(expectedLength);
            else if (builder.Capacity + builder.Length < expectedLength) builder.Capacity = expectedLength + builder.Length;
            for (int i = 0; i < expectedLength; ++i)
                builder.Insert(i, oneByteChars ? (char)ReadByte() : ReadChar());
            return builder;
        }

        public StringBuilder ReadStringPacked(StringBuilder builder = null)
        {
            int expectedLength = (int)ReadUInt32Packed();
            if (builder == null) builder = new StringBuilder(expectedLength);
            else if (builder.Capacity + builder.Length < expectedLength) builder.Capacity = expectedLength + builder.Length;
            for (int i = 0; i < expectedLength; ++i)
                builder.Insert(i, ReadCharPacked());
            return builder;
        }

        public StringBuilder ReadStringDiff(string compare, bool oneByteChars = false) => ReadStringDiff(null, compare, oneByteChars);
        public StringBuilder ReadStringDiff(StringBuilder builder, string compare, bool oneByteChars = false)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            int expectedLength = (int)ReadUInt32Packed();
            if (builder == null) builder = new StringBuilder(expectedLength);
            else if (builder.Capacity < expectedLength) builder.Capacity = expectedLength;
            ulong dBlockStart = bitSource.BitPosition + (ulong)(compare == null ? 0 : Math.Min(expectedLength, compare.Length));
            ulong mapStart;
            int compareLength = compare == null ? 0 : compare.Length;
            for (int i = 0; i < expectedLength; ++i)
            {
                if (i >= compareLength || ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    mapStart = bitSource.BitPosition;
                    bitSource.BitPosition = dBlockStart;
#endif
                    // Read datum
                    builder.Insert(i, oneByteChars ? (char)ReadByte() : ReadChar());
#if ARRAY_WRITE_PREMAP
                    dBlockStart = bitSource.BitPosition;
                    // Return to mapping section
                    bitSource.BitPosition = mapStart;
#endif
                }
                else if (i < compareLength) builder.Insert(i, compare[i]);
            }
            bitSource.BitPosition = dBlockStart;
            return builder;
        }

        public StringBuilder ReadStringDiff(StringBuilder compareAndBuffer, bool oneByteChars = false)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            int expectedLength = (int)ReadUInt32Packed();
            if (compareAndBuffer == null) throw new ArgumentNullException("Buffer cannot be null");
            else if (compareAndBuffer.Capacity < expectedLength) compareAndBuffer.Capacity = expectedLength;
            ulong dBlockStart = bitSource.BitPosition + (ulong)Math.Min(expectedLength, compareAndBuffer.Length);
            ulong mapStart;
            for (int i = 0; i < expectedLength; ++i)
            {
                if (i >= compareAndBuffer.Length || ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    mapStart = bitSource.BitPosition;
                    bitSource.BitPosition = dBlockStart;
#endif
                    // Read datum
                    compareAndBuffer.Remove(i, 1);
                    compareAndBuffer.Insert(i, oneByteChars ? (char)ReadByte() : ReadChar());
#if ARRAY_WRITE_PREMAP
                    dBlockStart = bitSource.BitPosition;
                    // Return to mapping section
                    bitSource.BitPosition = mapStart;
#endif
                }
            }
            bitSource.BitPosition = dBlockStart;
            return compareAndBuffer;
        }

        public StringBuilder ReadStringPackedDiff(string compare) => ReadStringPackedDiff(null, compare);
        public StringBuilder ReadStringPackedDiff(StringBuilder builder, string compare)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            int expectedLength = (int)ReadUInt32Packed();
            if (builder == null) builder = new StringBuilder(expectedLength);
            else if (builder.Capacity < expectedLength) builder.Capacity = expectedLength;
            ulong dBlockStart = bitSource.BitPosition + (ulong)(compare == null ? 0 : Math.Min(expectedLength, compare.Length));
            ulong mapStart;
            int compareLength = compare == null ? 0 : compare.Length;
            for (int i = 0; i < expectedLength; ++i)
            {
                if (i >= compareLength || ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    mapStart = bitSource.BitPosition;
                    bitSource.BitPosition = dBlockStart;
#endif
                    // Read datum
                    builder.Insert(i, ReadCharPacked());
#if ARRAY_WRITE_PREMAP
                    dBlockStart = bitSource.BitPosition;
                    // Return to mapping section
                    bitSource.BitPosition = mapStart;
#endif
                }
                else if (i < compareLength) builder.Insert(i, compare[i]);
            }
            bitSource.BitPosition = dBlockStart;
            return builder;
        }

        public StringBuilder ReadStringPackedDiff(StringBuilder compareAndBuffer)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            int expectedLength = (int)ReadUInt32Packed();
            if (compareAndBuffer == null) throw new ArgumentNullException("Buffer cannot be null");
            else if (compareAndBuffer.Capacity < expectedLength) compareAndBuffer.Capacity = expectedLength;
            ulong dBlockStart = bitSource.BitPosition + (ulong)Math.Min(expectedLength, compareAndBuffer.Length);
            ulong mapStart;
            for (int i = 0; i < expectedLength; ++i)
            {
                if (i >= compareAndBuffer.Length || ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    mapStart = bitSource.BitPosition;
                    bitSource.BitPosition = dBlockStart;
#endif
                    // Read datum
                    compareAndBuffer.Remove(i, 1);
                    compareAndBuffer.Insert(i, ReadCharPacked());
#if ARRAY_WRITE_PREMAP
                    dBlockStart = bitSource.BitPosition;
                    // Return to mapping section
                    bitSource.BitPosition = mapStart;
#endif
                }
            }
            bitSource.BitPosition = dBlockStart;
            return compareAndBuffer;
        }

        public byte[] ReadByteArray(byte[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new byte[knownLength];
            for (long i = 0; i < knownLength; ++i) readTo[i] = ReadByteDirect();
            return readTo;
        }

        public byte[] ReadByteArrayDiff(byte[] readTo = null, long knownLength = -1)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            byte[] writeTo = readTo == null || readTo.LongLength != knownLength ? new byte[knownLength] : readTo;
            ulong dBlockStart = bitSource.BitPosition + (ulong)(readTo == null ? 0 : Math.Min(knownLength, readTo.LongLength));
            ulong mapStart;
            long readToLength = readTo == null ? 0 : readTo.LongLength;
            for (long i = 0; i < knownLength; ++i)
            {
                if (i >= readToLength || ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    mapStart = bitSource.BitPosition;
                    bitSource.BitPosition = dBlockStart;
#endif
                    // Read datum
                    writeTo[i] = ReadByteDirect();
#if ARRAY_WRITE_PREMAP
                    dBlockStart = bitSource.BitPosition;
                    // Return to mapping section
                    bitSource.BitPosition = mapStart;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
            bitSource.BitPosition = dBlockStart;
            return writeTo;
        }

        public short[] ReadShortArray(short[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new short[knownLength];
            for (long i = 0; i < knownLength; ++i) readTo[i] = ReadInt16();
            return readTo;
        }

        public short[] ReadShortArrayPacked(short[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new short[knownLength];
            for (long i = 0; i < knownLength; ++i) readTo[i] = ReadInt16Packed();
            return readTo;
        }

        public short[] ReadShortArrayDiff(short[] readTo = null, long knownLength = -1)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            short[] writeTo = readTo == null || readTo.LongLength != knownLength ? new short[knownLength] : readTo;
            ulong dBlockStart = bitSource.BitPosition + (ulong)(readTo == null ? 0 : Math.Min(knownLength, readTo.LongLength));
            ulong mapStart;
            long readToLength = readTo == null ? 0 : readTo.LongLength;
            for (long i = 0; i < knownLength; ++i)
            {
                if (i >= readToLength || ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    mapStart = bitSource.BitPosition;
                    bitSource.BitPosition = dBlockStart;
#endif
                    // Read datum
                    writeTo[i] = ReadInt16();
#if ARRAY_WRITE_PREMAP
                    dBlockStart = bitSource.BitPosition;
                    // Return to mapping section
                    bitSource.BitPosition = mapStart;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
            bitSource.BitPosition = dBlockStart;
            return writeTo;
        }

        public short[] ReadShortArrayPackedDiff(short[] readTo = null, long knownLength = -1)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            short[] writeTo = readTo == null || readTo.LongLength != knownLength ? new short[knownLength] : readTo;
            ulong data = bitSource.BitPosition + (ulong)(readTo == null ? 0 : Math.Min(knownLength, readTo.LongLength));
            ulong rset;
            long readToLength = readTo == null ? 0 : readTo.LongLength;
            for (long i = 0; i < knownLength; ++i)
            {
                if (i >= readToLength || ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    rset = bitSource.BitPosition;
                    bitSource.BitPosition = data;
#endif
                    // Read datum
                    writeTo[i] = ReadInt16Packed();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    data = bitSource.BitPosition;
                    bitSource.BitPosition = rset;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
            bitSource.BitPosition = data;
            return writeTo;
        }

        public ushort[] ReadUShortArray(ushort[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new ushort[knownLength];
            for (long i = 0; i < knownLength; ++i) readTo[i] = ReadUInt16();
            return readTo;
        }

        public ushort[] ReadUShortArrayPacked(ushort[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new ushort[knownLength];
            for (long i = 0; i < knownLength; ++i) readTo[i] = ReadUInt16Packed();
            return readTo;
        }

        public ushort[] ReadUShortArrayDiff(ushort[] readTo = null, long knownLength = -1)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            ushort[] writeTo = readTo == null || readTo.LongLength != knownLength ? new ushort[knownLength] : readTo;
            ulong dBlockStart = bitSource.BitPosition + (ulong)(readTo == null ? 0 : Math.Min(knownLength, readTo.LongLength));
            ulong mapStart;
            long readToLength = readTo == null ? 0 : readTo.LongLength;
            for (long i = 0; i < knownLength; ++i)
            {
                if (i >= readToLength || ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    mapStart = bitSource.BitPosition;
                    bitSource.BitPosition = dBlockStart;
#endif
                    // Read datum
                    writeTo[i] = ReadUInt16();
#if ARRAY_WRITE_PREMAP
                    dBlockStart = bitSource.BitPosition;
                    // Return to mapping section
                    bitSource.BitPosition = mapStart;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
            bitSource.BitPosition = dBlockStart;
            return writeTo;
        }

        public ushort[] ReadUShortArrayPackedDiff(ushort[] readTo = null, long knownLength = -1)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            ushort[] writeTo = readTo == null || readTo.LongLength != knownLength ? new ushort[knownLength] : readTo;
            ulong data = bitSource.BitPosition + (ulong)(readTo == null ? 0 : Math.Min(knownLength, readTo.LongLength));
            ulong rset;
            long readToLength = readTo == null ? 0 : readTo.LongLength;
            for (long i = 0; i < knownLength; ++i)
            {
                if (i >= readToLength || ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    rset = bitSource.BitPosition;
                    bitSource.BitPosition = data;
#endif
                    // Read datum
                    writeTo[i] = ReadUInt16Packed();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    data = bitSource.BitPosition;
                    bitSource.BitPosition = rset;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
            bitSource.BitPosition = data;
            return writeTo;
        }

        public int[] ReadIntArray(int[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new int[knownLength];
            for (long i = 0; i < knownLength; ++i) readTo[i] = ReadInt32();
            return readTo;
        }

        public int[] ReadIntArrayPacked(int[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new int[knownLength];
            for (long i = 0; i < knownLength; ++i) readTo[i] = ReadInt32Packed();
            return readTo;
        }

        public int[] ReadIntArrayDiff(int[] readTo = null, long knownLength = -1)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            int[] writeTo = readTo == null || readTo.LongLength != knownLength ? new int[knownLength] : readTo;
            ulong dBlockStart = bitSource.BitPosition + (ulong)(readTo == null ? 0 : Math.Min(knownLength, readTo.LongLength));
            ulong mapStart;
            long readToLength = readTo == null ? 0 : readTo.LongLength;
            for (long i = 0; i < knownLength; ++i)
            {
                if (i >= readToLength || ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    mapStart = bitSource.BitPosition;
                    bitSource.BitPosition = dBlockStart;
#endif
                    // Read datum
                    writeTo[i] = ReadInt32();
#if ARRAY_WRITE_PREMAP
                    dBlockStart = bitSource.BitPosition;
                    // Return to mapping section
                    bitSource.BitPosition = mapStart;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
            bitSource.BitPosition = dBlockStart;
            return writeTo;
        }

        public int[] ReadIntArrayPackedDiff(int[] readTo = null, long knownLength = -1)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            int[] writeTo = readTo == null || readTo.LongLength != knownLength ? new int[knownLength] : readTo;
            ulong data = bitSource.BitPosition + (ulong)(readTo == null ? 0 : Math.Min(knownLength, readTo.LongLength));
            ulong rset;
            long readToLength = readTo == null ? 0 : readTo.LongLength;
            for (long i = 0; i < knownLength; ++i)
            {
                if (i >= readToLength || ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    rset = bitSource.BitPosition;
                    bitSource.BitPosition = data;
#endif
                    // Read datum
                    writeTo[i] = ReadInt32Packed();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    data = bitSource.BitPosition;
                    bitSource.BitPosition = rset;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
            bitSource.BitPosition = data;
            return writeTo;
        }

        public uint[] ReadUIntArray(uint[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new uint[knownLength];
            for (long i = 0; i < knownLength; ++i) readTo[i] = ReadUInt32();
            return readTo;
        }

        public uint[] ReadUIntArrayPacked(uint[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new uint[knownLength];
            for (long i = 0; i < knownLength; ++i) readTo[i] = ReadUInt32Packed();
            return readTo;
        }

        public uint[] ReadUIntArrayDiff(uint[] readTo = null, long knownLength = -1)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            uint[] writeTo = readTo == null || readTo.LongLength != knownLength ? new uint[knownLength] : readTo;
            ulong dBlockStart = bitSource.BitPosition + (ulong)(readTo == null ? 0 : Math.Min(knownLength, readTo.LongLength));
            ulong mapStart;
            long readToLength = readTo == null ? 0 : readTo.LongLength;
            for (long i = 0; i < knownLength; ++i)
            {
                if (i >= readToLength || ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    mapStart = bitSource.BitPosition;
                    bitSource.BitPosition = dBlockStart;
#endif
                    // Read datum
                    writeTo[i] = ReadUInt32();
#if ARRAY_WRITE_PREMAP
                    dBlockStart = bitSource.BitPosition;
                    // Return to mapping section
                    bitSource.BitPosition = mapStart;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
            bitSource.BitPosition = dBlockStart;
            return writeTo;
        }

        public long[] ReadLongArray(long[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new long[knownLength];
            for (long i = 0; i < knownLength; ++i) readTo[i] = ReadInt64();
            return readTo;
        }

        public long[] ReadLongArrayPacked(long[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new long[knownLength];
            for (long i = 0; i < knownLength; ++i) readTo[i] = ReadInt64Packed();
            return readTo;
        }

        public long[] ReadLongArrayDiff(long[] readTo = null, long knownLength = -1)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            long[] writeTo = readTo == null || readTo.LongLength != knownLength ? new long[knownLength] : readTo;
            ulong dBlockStart = bitSource.BitPosition + (ulong)(readTo == null ? 0 : Math.Min(knownLength, readTo.LongLength));
            ulong mapStart;
            long readToLength = readTo == null ? 0 : readTo.LongLength;
            for (long i = 0; i < knownLength; ++i)
            {
                if (i >= readToLength || ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    mapStart = bitSource.BitPosition;
                    bitSource.BitPosition = dBlockStart;
#endif
                    // Read datum
                    writeTo[i] = ReadInt64();
#if ARRAY_WRITE_PREMAP
                    dBlockStart = bitSource.BitPosition;
                    // Return to mapping section
                    bitSource.BitPosition = mapStart;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
            bitSource.BitPosition = dBlockStart;
            return writeTo;
        }

        public long[] ReadLongArrayPackedDiff(long[] readTo = null, long knownLength = -1)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            long[] writeTo = readTo == null || readTo.LongLength != knownLength ? new long[knownLength] : readTo;
            ulong data = bitSource.BitPosition + (ulong)(readTo == null ? 0 : Math.Min(knownLength, readTo.LongLength));
            ulong rset;
            long readToLength = readTo == null ? 0 : readTo.LongLength;
            for (long i = 0; i < knownLength; ++i)
            {
                if (i >= readToLength || ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    rset = bitSource.BitPosition;
                    bitSource.BitPosition = data;
#endif
                    // Read datum
                    writeTo[i] = ReadInt64Packed();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    data = bitSource.BitPosition;
                    bitSource.BitPosition = rset;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
            bitSource.BitPosition = data;
            return writeTo;
        }

        public ulong[] ReadULongArray(ulong[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new ulong[knownLength];
            for (long i = 0; i < knownLength; ++i) readTo[i] = ReadUInt64();
            return readTo;
        }

        public ulong[] ReadULongArrayPacked(ulong[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new ulong[knownLength];
            for (long i = 0; i < knownLength; ++i) readTo[i] = ReadUInt64Packed();
            return readTo;
        }

        public ulong[] ReadULongArrayDiff(ulong[] readTo = null, long knownLength = -1)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            ulong[] writeTo = readTo == null || readTo.LongLength != knownLength ? new ulong[knownLength] : readTo;
            ulong dBlockStart = bitSource.BitPosition + (ulong)(readTo == null ? 0 : Math.Min(knownLength, readTo.LongLength));
            ulong mapStart;
            long readToLength = readTo == null ? 0 : readTo.LongLength;
            for (long i = 0; i < knownLength; ++i)
            {
                if (i >= readToLength || ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    mapStart = bitSource.BitPosition;
                    bitSource.BitPosition = dBlockStart;
#endif
                    // Read datum
                    writeTo[i] = ReadUInt64();
#if ARRAY_WRITE_PREMAP
                    dBlockStart = bitSource.BitPosition;
                    // Return to mapping section
                    bitSource.BitPosition = mapStart;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
            bitSource.BitPosition = dBlockStart;
            return writeTo;
        }

        public ulong[] ReadULongArrayPackedDiff(ulong[] readTo = null, long knownLength = -1)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            ulong[] writeTo = readTo == null || readTo.LongLength != knownLength ? new ulong[knownLength] : readTo;
            ulong data = bitSource.BitPosition + (ulong)(readTo == null ? 0 : Math.Min(knownLength, readTo.LongLength));
            ulong rset;
            long readToLength = readTo == null ? 0 : readTo.LongLength;
            for (long i = 0; i < knownLength; ++i)
            {
                if (i >= readToLength || ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    rset = bitSource.BitPosition;
                    bitSource.BitPosition = data;
#endif
                    // Read datum
                    writeTo[i] = ReadUInt64Packed();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    data = bitSource.BitPosition;
                    bitSource.BitPosition = rset;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
            bitSource.BitPosition = data;
            return writeTo;
        }

        public float[] ReadFloatArray(float[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new float[knownLength];
            for (long i = 0; i < knownLength; ++i) readTo[i] = ReadSingle();
            return readTo;
        }

        public float[] ReadFloatArrayPacked(float[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new float[knownLength];
            for (long i = 0; i < knownLength; ++i) readTo[i] = ReadSinglePacked();
            return readTo;
        }

        public float[] ReadFloatArrayDiff(float[] readTo = null, long knownLength = -1)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            float[] writeTo = readTo == null || readTo.LongLength != knownLength ? new float[knownLength] : readTo;
            ulong dBlockStart = bitSource.BitPosition + (ulong)(readTo == null ? 0 : Math.Min(knownLength, readTo.LongLength));
            ulong mapStart;
            long readToLength = readTo == null ? 0 : readTo.LongLength;
            for (long i = 0; i < knownLength; ++i)
            {
                if (i >= readToLength || ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    mapStart = bitSource.BitPosition;
                    bitSource.BitPosition = dBlockStart;
#endif
                    // Read datum
                    writeTo[i] = ReadSingle();
#if ARRAY_WRITE_PREMAP
                    dBlockStart = bitSource.BitPosition;
                    // Return to mapping section
                    bitSource.BitPosition = mapStart;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
            bitSource.BitPosition = dBlockStart;
            return writeTo;
        }

        public float[] ReadFloatArrayPackedDiff(float[] readTo = null, long knownLength = -1)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            float[] writeTo = readTo == null || readTo.LongLength != knownLength ? new float[knownLength] : readTo;
            ulong data = bitSource.BitPosition + (ulong)(readTo == null ? 0 : Math.Min(knownLength, readTo.LongLength));
            ulong rset;
            long readToLength = readTo == null ? 0 : readTo.LongLength;
            for (long i = 0; i < knownLength; ++i)
            {
                if (i >= readToLength || ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    rset = bitSource.BitPosition;
                    bitSource.BitPosition = data;
#endif
                    // Read datum
                    readTo[i] = ReadSinglePacked();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    data = bitSource.BitPosition;
                    bitSource.BitPosition = rset;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
            bitSource.BitPosition = data;
            return writeTo;
        }

        public double[] ReadDoubleArray(double[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new double[knownLength];
            for (long i = 0; i < knownLength; ++i) readTo[i] = ReadDouble();
            return readTo;
        }

        public double[] ReadDoubleArrayPacked(double[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new double[knownLength];
            for (long i = 0; i < knownLength; ++i) readTo[i] = ReadDoublePacked();
            return readTo;
        }

        public double[] ReadDoubleArrayDiff(double[] readTo = null, long knownLength = -1)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            double[] writeTo = readTo == null || readTo.LongLength != knownLength ? new double[knownLength] : readTo;
            ulong dBlockStart = bitSource.BitPosition + (ulong)(readTo == null ? 0 : Math.Min(knownLength, readTo.LongLength));
            ulong mapStart;
            long readToLength = readTo == null ? 0 : readTo.LongLength;
            for (long i = 0; i < knownLength; ++i)
            {
                if (i >= readToLength || ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    mapStart = bitSource.BitPosition;
                    bitSource.BitPosition = dBlockStart;
#endif
                    // Read datum
                    writeTo[i] = ReadDouble();
#if ARRAY_WRITE_PREMAP
                    dBlockStart = bitSource.BitPosition;
                    // Return to mapping section
                    bitSource.BitPosition = mapStart;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
            bitSource.BitPosition = dBlockStart;
            return writeTo;
        }

        public double[] ReadDoubleArrayPackedDiff(double[] readTo = null, long knownLength = -1)
        {
            if (bitSource == null) throw new InvalidOperationException("Cannot read bits on a non BitStream stream");
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            double[] writeTo = readTo == null || readTo.LongLength != knownLength ? new double[knownLength] : readTo;
            ulong data = bitSource.BitPosition + (ulong)(readTo == null ? 0 : Math.Min(knownLength, readTo.LongLength));
            ulong rset;
            long readToLength = readTo == null ? 0 : readTo.LongLength;
            for (long i = 0; i < knownLength; ++i)
            {
                if (i >= readToLength || ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    rset = bitSource.BitPosition;
                    bitSource.BitPosition = data;
#endif
                    // Read datum
                    writeTo[i] = ReadDoublePacked();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    data = bitSource.BitPosition;
                    bitSource.BitPosition = rset;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
            bitSource.BitPosition = data;
            return writeTo;
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
