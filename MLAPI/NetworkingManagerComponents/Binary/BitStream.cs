#define ARRAY_WRITE_PERMISSIVE  // Allow attempt to write "packed" byte array (calls WriteByteArray())
#define ARRAY_RESOLVE_IMPLICIT  // Include WriteArray() method with automatic type resolution
#define ARRAY_WRITE_PREMAP      // Create a prefixed array diff mapping
#define ARRAY_DIFF_ALLOW_RESIZE // Whether or not to permit writing diffs of differently sized arrays

using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using static MLAPI.NetworkingManagerComponents.Binary.Arithmetic;


namespace MLAPI.NetworkingManagerComponents.Binary
{
    /// <summary>
    /// A stream that can be used at the bit level
    /// </summary>
    public sealed class BitStream : Stream
    {
    
        /// <summary>
        /// A struct with a explicit memory layout. The struct has 4 fields. float,uint,double and ulong.
        /// Every field has the same starting point in memory. If you insert a float value, it can be extracted as a uint.
        /// This is to allow for lockless & garbage free conversion from float to uint and double to ulong.
        /// This allows for VarInt encoding and other integer encodings.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        internal struct UIntFloat
        {
            [FieldOffset(0)]
            public float floatValue;

            [FieldOffset(0)]
            public uint uintValue;

            [FieldOffset(0)]
            public double doubleValue;

            [FieldOffset(0)]
            public ulong ulongValue;
        }
        
        const int initialCapacity = 16;
        const float initialGrowthFactor = 2.0f;
        private byte[] target;

        /// <summary>
        /// A stream that supports writing data smaller than a single byte. This stream also has a built-in compression algorithm that can (optionally) be used to write compressed data.
        /// </summary>
        /// <param name="capacity">Initial capacity of buffer in bytes.</param>
        /// <param name="growthFactor">Factor by which buffer should grow when necessary.</param>
        public BitStream(int capacity, float growthFactor)
        {
            target = new byte[capacity];
            GrowthFactor = growthFactor;
            Resizable = true;
        }

        /// <summary>
        /// A stream that supports writing data smaller than a single byte. This stream also has a built-in compression algorithm that can (optionally) be used to write compressed data.
        /// </summary>
        /// <param name="growthFactor">Factor by which buffer should grow when necessary.</param>
        public BitStream(float growthFactor) : this(initialCapacity, growthFactor) { }
        /// <summary>
        /// A stream that supports writing data smaller than a single byte. This stream also has a built-in compression algorithm that can (optionally) be used to write compressed data.
        /// </summary>
        /// <param name="capacity"></param>
        public BitStream(int capacity) : this(capacity, initialGrowthFactor) { }

        /// <summary>
        /// A stream that supports writing data smaller than a single byte. This stream also has a built-in compression algorithm that can (optionally) be used to write compressed data.
        /// </summary>
        public BitStream() : this(initialCapacity, initialGrowthFactor) { }

        /// <summary>
        /// A stream that supports writing data smaller than a single byte. This stream also has a built-in compression algorithm that can (optionally) be used to write compressed data.
        /// </summary>
        /// <param name="target">The buffer containing initial data</param>
        /// <param name="offset">The offset where the data begins</param>
        /// <param name="count">The amount of bytes to copy from the initial data buffer</param>
        public BitStream(byte[] target, int offset, int count) : this(count)
        {
            Buffer.BlockCopy(target, offset, this.target, 0, count);
            Resizable = false;
        }

        /// <summary>
        /// A stream that supports writing data smaller than a single byte. This stream also has a built-in compression algorithm that can (optionally) be used to write compressed data.
        /// NOTE: when using a pre-allocated buffer, the stream will not grow!
        /// </summary>
        /// <param name="target">Pre-allocated buffer to write to</param>
        public BitStream(byte[] target)
        {
            this.target = target;
            Resizable = false;
            BitLength = (ulong)(target.Length << 3);
        }

        /// <summary>
        /// Whether or not the stream will grow the buffer to accomodate more data.
        /// </summary>
        public bool Resizable { get; }

        private float _growthFactor;
        /// <summary>
        /// Factor by which buffer should grow when necessary.
        /// </summary>
        public float GrowthFactor { set { _growthFactor = value <= 1 ? 1.5f : value; } get { return _growthFactor; } }

        /// <summary>
        /// Whether or not data can be read from the stream.
        /// </summary>
        public override bool CanRead => Position < target.LongLength;

        /// <summary>
        /// Whether or not seeking is supported by this stream. (Always true)
        /// </summary>
        public override bool CanSeek => true;

        /// <summary>
        /// Whether or not this stream can accept new data. NOTE: this will return true even if only fewer than 8 bits can be written!
        /// </summary>
        public override bool CanWrite => !BitAligned || Position < target.LongLength || Resizable;

        /// <summary>
        /// Current buffer size. The buffer will not be resized (if possible) until Position is equal to Capacity and an attempt to write data is made.
        /// </summary>
        public long Capacity
        {
            get => target.LongLength; // Optimized CeilingExact
            set
            {
                if (value < Length) throw new ArgumentOutOfRangeException("New capcity too small!");
                SetCapacity(value);
            }
        }

        /// <summary>
        /// The current length of data considered to be "written" to the buffer.
        /// </summary>
        public override long Length { get => Div8Ceil(BitLength); }

        /// <summary>
        /// The index that will be written to when any call to write data is made to this stream.
        /// </summary>
        public override long Position { get => (long)(BitPosition >> 3); set => BitPosition = (ulong)value << 3; }

        /// <summary>
        /// Bit offset into the buffer that new data will be written to.
        /// </summary>
        public ulong BitPosition { get; set; }

        /// <summary>
        /// Length of data (in bits) that is considered to be written to the stream.
        /// </summary>
        public ulong BitLength { get; private set; }

        /// <summary>
        /// Whether or not the current BitPosition is evenly divisible by 8. I.e. whether or not the BitPosition is at a byte boundary.
        /// </summary>
        public bool BitAligned { get => (BitPosition & 7) == 0; }

        /// <summary>
        /// Flush stream. This does nothing since data is written directly to a byte buffer.
        /// </summary>
        public override void Flush() { } // NOP

        /// <summary>
        /// Read a misaligned byte. WARNING: If the current BitPosition <strong>isn't</strong> byte misaligned,
        /// avoid using this method as it <strong>may</strong> cause an IndexOutOfBoundsException in such a case.
        /// </summary>
        /// <returns>A byte extracted from up to two separate buffer indices.</returns>
        private byte ReadByteMisaligned()
        {
            int mod = (int)(BitPosition & 7);
            return (byte)((target[(int)Position] >> mod) | (target[(int)(BitPosition += 8) >> 3] << (8 - mod)));
        }
        /// <summary>
        /// Read an aligned byte from the buffer. It's recommended to not use this when the BitPosition is byte-misaligned.
        /// </summary>
        /// <returns>The byte stored at the current Position index</returns>
        private byte ReadByteAligned() => target[Position++];

        /// <summary>
        /// Read a byte as a byte. This is just for internal use so as to minimize casts (cuz they ugly af).
        /// </summary>
        /// <returns></returns>
        private byte _ReadByte() => BitAligned ? ReadByteAligned() : ReadByteMisaligned();

        /// <summary>
        /// Read a byte from the buffer. This takes into account possible byte misalignment.
        /// </summary>
        /// <returns>A byte from the buffer or, if a byte can't be read, -1.</returns>
        public override int ReadByte() => CanRead ? BitAligned ? ReadByteAligned() : ReadByteMisaligned() : -1;

        /// <summary>
        /// Read a subset of the stream buffer and write the contents to the supplied buffer.
        /// </summary>
        /// <param name="buffer">Buffer to copy data to.</param>
        /// <param name="offset">Offset into the buffer to write data to.</param>
        /// <param name="count">How many bytes to attempt to read.</param>
        /// <returns>Amount of bytes read.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int tLen = Math.Min(count, (int)(target.LongLength - Position) - ((BitPosition & 7) == 0 ? 0 : 1));
            for (int i = 0; i < tLen; ++i) buffer[offset + i] = _ReadByte();
            return tLen;
        }

        /// <summary>
        /// Set position in stream to read from/write to.
        /// </summary>
        /// <param name="offset">Offset from position origin.</param>
        /// <param name="origin">How to calculate offset.</param>
        /// <returns>The new position in the buffer that data will be written to.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return (long)((
                BitPosition =
                    (
                    origin == SeekOrigin.Current ?
                        offset > 0 ?
                            Math.Min(BitPosition + ((ulong)offset << 3), (ulong)target.Length << 3) :
                            (offset ^ SIGN_BIT_64) > Position ?
                                0UL :
                                BitPosition - (ulong)((offset ^ SIGN_BIT_64) << 3) :
                    origin == SeekOrigin.Begin ?
                        (ulong)Math.Max(0, offset) << 3 :
                        (ulong)Math.Max(target.Length - offset, 0) << 3
                    )) >> 3) + (long)((BitPosition & 1UL) | ((BitPosition >> 1) & 1UL) | ((BitPosition >> 2) & 1UL));
        }

        /// <summary>
        /// Set the capacity of the internal buffer.
        /// </summary>
        /// <param name="value">New capacity of the buffer</param>
        private void SetCapacity(long value)
        {
            if (!Resizable) throw new NotSupportedException("Can't resize non resizable buffer"); // Don't do shit because fuck you (comment by @GabrielTofvesson -TwoTen)
            byte[] newTarg = new byte[value];
            long len = Math.Min(value, target.LongLength);
            Buffer.BlockCopy(target, 0, newTarg, 0, (int)len);
            if (value < target.LongLength) BitPosition = (ulong)value << 3;
            target = newTarg;
        }

        /// <summary>
        /// Set length of data considered to be "written" to the stream.
        /// </summary>
        /// <param name="value">New length of the written data.</param>
        public override void SetLength(long value)
        {
            if (value < 0) throw new IndexOutOfRangeException("Cannot set a negative length!");
            if (value > Capacity) Grow(value - Capacity);
            BitLength = (ulong)value << 3;
            BitPosition = Math.Min((ulong)value << 3, BitPosition);
        }

        /// <summary>
        /// Write data from the given buffer to the internal stream buffer.
        /// </summary>
        /// <param name="buffer">Buffer to write from.</param>
        /// <param name="offset">Offset in given buffer to start reading from.</param>
        /// <param name="count">Amount of bytes to read copy from given buffer to stream buffer.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            // Check bit alignment. If misaligned, each byte written has to be misaligned
            if (BitAligned)
            {
                if (Position + count >= target.Length) Grow(count);
                Array.Copy(buffer, offset, target, Position, count);
                Position += count;
            }
            else
            {
                if (Position + count + 1 >= target.Length) Grow(count);
                for (int i = 0; i < count; ++i) _WriteMisaligned(buffer[offset + i]);
            }
            if (BitPosition > BitLength) BitLength = BitPosition;
        }

        /// <summary>
        /// Write data from the given buffer to the internal stream buffer.
        /// </summary>
        /// <param name="buffer">Buffer to write from.</param>
        public void Write(byte[] buffer) => Write(buffer, 0, buffer.Length);

        /// <summary>
        /// Grow buffer if possible. According to Max(bufferLength, 1) * growthFactor^Ceil(newContent/Max(bufferLength, 1))
        /// </summary>
        /// <param name="newContent">How many new values need to be accomodated (at least).</param>
        private void Grow(long newContent) => SetCapacity(Math.Max(target.LongLength, 1) * (long)Math.Pow(GrowthFactor, CeilingExact(newContent, Math.Max(target.LongLength, 1))));

        /// <summary>
        /// Write a single bit to the stream
        /// </summary>
        /// <param name="bit">Value of the bit. True represents 1, False represents 0</param>
        public void WriteBit(bool bit)
        {
            if (BitAligned && Position == target.Length) Grow(1);
            int offset = (int)(BitPosition & 7);
            long pos = Position;
            ++BitPosition;
            target[pos] = (byte)(bit ? (target[pos] & ~(1 << offset)) | (1 << offset) : (target[pos] & ~(1 << offset)));
            UpdateLength();
        }

        /// <summary>
        /// Write single-precision floating point value to the stream
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteSingle(float value)
        {
            WriteUInt32(new UIntFloat
            {
                floatValue = value
            }.uintValue);
        }

        /// <summary>
        /// Write double-precision floating point value to the stream
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteDouble(double value)
        {
            WriteUInt64(new UIntFloat
            {
                doubleValue = value
            }.ulongValue);
        }

        /// <summary>
        /// Write single-precision floating point value to the stream as a varint
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteSinglePacked(float value)
        {
            WriteUInt32Packed(new UIntFloat
            {
                floatValue = value
            }.uintValue);
        }

        /// <summary>
        /// Write double-precision floating point value to the stream as a varint
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteDoublePacked(double value)
        {
            WriteUInt64Packed(new UIntFloat
            {
                doubleValue = value
            }.ulongValue);
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
            WriteSingle(color32.r);
            WriteSingle(color32.g);
            WriteSingle(color32.b);
            WriteSingle(color32.a);
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
            for (int i = 0; i < bytes; ++i) _WriteByte((byte)(result >> (i << 3)));
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
            for (int i = 0; i < bytes; ++i) _WriteByte((byte)(result >> (i << 3)));
        }

        /// <summary>
        /// Write a rotation to the stream.
        /// </summary>
        /// <param name="rotation">Rotation to write</param>
        /// <param name="bytesPerAngle">How many bytes each written angle should occupy. Must be between 1 and 4 (inclusive)</param>
        public void WriteRotation(Quaternion rotation, int bytesPerAngle)
        {
            if (bytesPerAngle < 1 || bytesPerAngle > 4) throw new ArgumentOutOfRangeException("Bytes per angle must be at least 1 byte and at most 4 bytes!");
            if (bytesPerAngle == 4) WriteVector3(rotation.eulerAngles);
            else
            {
                Vector3 rot = rotation.eulerAngles;
                WriteRangedSingle(rot.x, 0f, 360f, bytesPerAngle);
                WriteRangedSingle(rot.y, 0f, 360f, bytesPerAngle);
                WriteRangedSingle(rot.z, 0f, 360f, bytesPerAngle);
            }
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
            for (int i = 0; i < bytes; ++i) read |= (uint)_ReadByte() << (i << 3);
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
            for (int i = 0; i < bytes; ++i) read |= (ulong)_ReadByte() << (i << 3);
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
        /// Write the lower half (lower nibble) of a byte.
        /// </summary>
        /// <param name="value">Value containing nibble to write.</param>
        public void WriteNibble(byte value)
        {
            if (BitAligned)
            {
                WriteIntByte((value & 0x0F) | (target[Position] & 0xF0));
                BitPosition -= 4;
            }
            else
            {
                value &= 0x0F;
                int offset = (int)(BitPosition & 7), offset_inv = 8 - offset;
                target[Position] = (byte)((target[Position] & (0xFF >> offset_inv)) | (byte)(value << offset));
                if (offset > 4) target[Position + 1] = (byte)((target[Position + 1] & (0xFF << (offset & 3))) | (byte)(value >> offset_inv));
                BitPosition += 4;
            }
            UpdateLength();
        }
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
            if (BitPosition + (ulong)bitCount > ((ulong)target.LongLength << 3)) Grow(Div8Ceil(BitPosition + (ulong)bitCount));
            if (bitCount > 64) throw new ArgumentOutOfRangeException("Cannot read more than 64 bits from a 64-bit value!");
            if (bitCount < 0) throw new ArgumentOutOfRangeException("Cannot read fewer than 0 bits!");
            int count = 0;
            for(; count+8<bitCount; count += 8) _WriteULongByte(value >> count);
            BitPosition += (ulong)count;
            if ((bitCount & 7) != 0) _WriteBits((byte)(value >> count), bitCount & 7);
            BitPosition += (ulong)bitCount & 7UL;
            UpdateLength();
        }
        /// <summary>
        /// Write bits to stream. This does not update the current Length of the stream.
        /// </summary>
        /// <param name="value">Value to get bits from.</param>
        /// <param name="bitCount">Amount of bits to write.</param>
        private void _WriteBits(byte value, int bitCount)
        {
            if (BitPosition + (ulong)bitCount > ((ulong)target.LongLength << 3)) Grow(1);
            if (bitCount > 8) throw new ArgumentOutOfRangeException("Cannot read more than 8 bits from a 8-bit value!");
            if (bitCount < 0) throw new ArgumentOutOfRangeException("Cannot read fewer than 0 bits!");
            int offset = (int)(BitPosition & 7UL), offset_inv = 8 - offset;
            value &= (byte)(0xFF >> (8 - bitCount));
            target[Position] = (byte)(
                    (target[Position] & (0xFF >> offset_inv)) |             // Bits prior to value (lower)
                    (target[Position] & (0xFF << (offset + bitCount))) |    // Bits after value (higher)
                    (value << offset)                                         // Bits to write
                );
            if (bitCount + offset > 8)
                target[Position + 1] = (byte)(
                        (target[Position + 1] & (0xFF << ((bitCount + offset) & 7))) |  // Bits after upper part of value (higher)
                        (value >> (16 - bitCount - offset))                             // upper part of value
                    );
            BitPosition += (ulong)bitCount;
        }

        /// <summary>
        /// Read a certain amount of bits from the stream.
        /// </summary>
        /// <param name="bitCount">How many bits to read. Minimum 0, maximum 8.</param>
        /// <returns>The bits that were read</returns>
        public ulong ReadBits(int bitCount)
        {
            if (bitCount > 64) throw new ArgumentOutOfRangeException("Cannot read more than 64 bits into a 64-bit value!");
            if (bitCount < 0) throw new ArgumentOutOfRangeException("Cannot read fewer than 0 bits!");
            ulong read = 0;
            for(int i = 0; i+8<bitCount; i+=8) read |= (ulong)_ReadByte() << i;
            BitPosition += (ulong)bitCount & ~7UL;
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
            if (bitCount > 8) throw new ArgumentOutOfRangeException("Cannot read more than 8 bits into an 8-bit value!");
            if (bitCount < 0) throw new ArgumentOutOfRangeException("Cannot read fewer than 0 bits!");
            byte result = 0;
            for (int i = 0; i < 8; ++i) result |= (byte)(((target[(BitPosition + (ulong)i)>>3] >> (int)((BitPosition+(ulong)i) & 7)) & 1) << i);
            BitPosition += (ulong)bitCount;
            return result;
        }

        /// <summary>
        /// Read a nibble (4 bits) from the stream.
        /// </summary>
        /// <param name="asUpper">Whether or not the nibble should be left-shifted by 4 bits</param>
        /// <returns>The nibble that was read</returns>
        public byte ReadNibble(bool asUpper)
        {
            byte result = (byte)(
                ((target[BitPosition >> 3] >> (int)(BitPosition & 7UL)) & 1) |
                (((target[(BitPosition + 1UL) >> 3] >> (int)((BitPosition + 1UL) & 7UL)) & 1) << 1) |
                (((target[(BitPosition + 2UL) >> 3] >> (int)((BitPosition + 2UL) & 7UL)) & 1) << 2) |
                (((target[(BitPosition + 3UL) >> 3] >> (int)((BitPosition + 3UL) & 7UL)) & 1) << 3)
                );
            if (asUpper) result <<= 4;
            return result;
        }

        // Marginally faster than the one that accepts a bool
        /// <summary>
        /// Read a nibble (4 bits) from the stream.
        /// </summary>
        /// <returns>The nibble that was read</returns>
        public byte ReadNibble() => (byte)(
                ((target[BitPosition >> 3] >> (int)(BitPosition & 7UL)) & 1) |
                (((target[(BitPosition + 1UL) >> 3] >> (int)((BitPosition + 1UL) & 7UL)) & 1) << 1) |
                (((target[(BitPosition + 2UL) >> 3] >> (int)((BitPosition + 2UL) & 7UL)) & 1) << 2) |
                (((target[(BitPosition + 3UL) >> 3] >> (int)((BitPosition + 3UL) & 7UL)) & 1) << 3)
                );

        /// <summary>
        /// Write bits to stream.
        /// </summary>
        /// <param name="value">Value to get bits from.</param>
        /// <param name="bitCount">Amount of bits to write.</param>
        public void WriteBits(byte value, int bitCount)
        {
            _WriteBits(value, bitCount);
            UpdateLength();
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
            _WriteByte((byte)value);
            _WriteByte((byte)(value >> 8));
            UpdateLength();
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
            _WriteByte((byte)value);
            _WriteByte((byte)(value >> 8));
            _WriteByte((byte)(value >> 16));
            _WriteByte((byte)(value >> 24));
            UpdateLength();
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
            _WriteByte((byte)value);
            _WriteByte((byte)(value >> 8));
            _WriteByte((byte)(value >> 16));
            _WriteByte((byte)(value >> 24));
            _WriteByte((byte)(value >> 32));
            _WriteByte((byte)(value >> 40));
            _WriteByte((byte)(value >> 48));
            _WriteByte((byte)(value >> 56));
            UpdateLength();
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
        public void WriteInt64Packed(long value) => WriteUInt64Packed(ZigZagEncode(value));
        /// <summary>
        /// Write an unsigned long (UInt64) as a varint to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteUInt64Packed(ulong value)
        {
            int grow = VarIntSize(value);
            if (Position + grow > target.LongLength) Grow(grow);
            if (value <= 240) _WriteULongByte(value);
            else if (value <= 2287)
            {
                _WriteULongByte(((value - 240) >> 8) + 241);
                _WriteULongByte(value - 240);
            }
            else if (value <= 67823)
            {
                _WriteULongByte(249);
                _WriteULongByte((value - 2288) >> 8);
                _WriteULongByte(value - 2288);
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
                _WriteULongByte(header);
                int max = (int)(header - 247);
                for (int i = 0; i < max; ++i) _WriteULongByte(value >> (i << 3));
            }
            UpdateLength();
        }
        /// <summary>
        /// Read an unsigned short (UInt16) from the stream.
        /// </summary>
        /// <returns>Value read from stream.</returns>
        public ushort ReadUInt16() => (ushort)(_ReadByte() | (_ReadByte() << 8));
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
        public uint ReadUInt32() => (uint)(_ReadByte() | (_ReadByte() << 8) | (_ReadByte() << 16) | (_ReadByte() << 24));
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
                _ReadByte() |
                ((ulong)_ReadByte() << 8) |
                ((ulong)_ReadByte() << 16) |
                ((ulong)_ReadByte() << 24) |
                ((ulong)_ReadByte() << 32) |
                ((ulong)_ReadByte() << 40) |
                ((ulong)_ReadByte() << 48) |
                ((ulong)_ReadByte() << 56)
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
        public short ReadInt16Packed() => (short)ZigZagDecode(ReadUInt64Packed());
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
        public int ReadInt32Packed() => (int)ZigZagDecode(ReadUInt64Packed());
        /// <summary>
        /// Read a varint unsigned int (UInt32) from the stream.
        /// </summary>
        /// <returns>Un-varinted value.</returns>
        public uint ReadUInt32Packed() => (uint)ReadUInt64Packed();
        /// <summary>
        /// Read a ZigZag encoded varint signed long(Int64) from the stream.
        /// </summary>
        /// <returns>Decoded un-varinted value.</returns>
        public long ReadInt64Packed() => ZigZagDecode(ReadUInt64Packed());
        /// <summary>
        /// Read a varint unsigned long (UInt64) from the stream.
        /// </summary>
        /// <returns>Un-varinted value.</returns>
        public ulong ReadUInt64Packed()
        {
            ulong header = _ReadByte();
            if (header <= 240) return header;
            if (header <= 248) return 240 + ((header - 241) << 8) + _ReadByte();
            if (header == 249) return 2288UL + (ulong)(_ReadByte() << 8) + _ReadByte();
            ulong res = _ReadByte() | ((ulong)_ReadByte() << 8) | ((ulong)_ReadByte() << 16);
            int cmp = 2;
            int hdr = (int)(header - 247);
            while (hdr > ++cmp) res |= (ulong)_ReadByte() << (cmp << 3);
            return res;
        }
        /// <summary>
        /// Read a single bit from the stream.
        /// </summary>
        /// <returns>A bit in bool format. (True represents 1, False represents 0)</returns>
        public bool ReadBit() => (target[Position] & (1 << (int)(BitPosition++ & 7))) != 0;

        /// <summary>
        /// Helper method that casts a value to a byte and passes is to _WriteMisaligned. This does not update the current Length of the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        private void _WriteULongByteMisaligned(ulong value) => _WriteMisaligned((byte)value);
        /// <summary>
        /// Write a misaligned byte. NOTE: Using this when the bit position isn't byte-misaligned may cause an IndexOutOfBoundsException! This does not update the current Length of the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        private void _WriteMisaligned(byte value)
        {
            int off = (int)(BitPosition & 7);
            int shift1 = 8 - off;
            target[Position + 1] = (byte)((target[Position + 1] & (0xFF << off)) | (value >> shift1));
            target[Position] = (byte)((target[Position] & (0xFF >> shift1)) | (value << off));

            BitPosition += 8;
        }

        /// <summary>
        /// Helper method that casts a value to a byte and passes is to _WriteMisaligned
        /// </summary>
        /// <param name="value">Value to write</param>
        private void WriteULongByteMisaligned(ulong value) => WriteMisaligned((byte)value);
        /// <summary>
        /// Write a misaligned byte. NOTE: Using this when the bit position isn't byte-misaligned may cause an IndexOutOfBoundsException!
        /// </summary>
        /// <param name="value">Value to write</param>
        private void WriteMisaligned(byte value)
        {
            _WriteMisaligned(value);
            UpdateLength();
        }
        /// <summary>
        /// Write a byte (in an int format) to the stream. This does not update the current Length of the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        private void _WriteIntByte(int value) => _WriteByte((byte)value);
        /// <summary>
        /// Write a byte (in a ulong format) to the stream. This does not update the current Length of the stream.
        /// </summary>
        /// <param name="byteValue">Value to write</param>
        private void _WriteULongByte(ulong byteValue) => _WriteByte((byte)byteValue);
        /// <summary>
        /// Write a byte to the stream. This does not update the current Length of the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        private void _WriteByte(byte value)
        {
            if (Div8Ceil(BitPosition) == target.LongLength) Grow(1);
            if (BitAligned)
            {
                target[Position] = value;
                BitPosition += 8;
            }
            else _WriteMisaligned(value);
            UpdateLength();
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
        public override void WriteByte(byte value)
        {
            _WriteByte(value);
            UpdateLength();
        }

        // As it turns out, strings cannot be treated as char arrays, since strings use pointers to store data rather than C# arrays
        public void WriteString(string s, bool knownLength = false)
        {
            if (!knownLength) WriteUInt64Packed((ulong)s.Length);
            int target = s.Length;
            for (int i = 0; i < target; ++i) WriteChar(s[i]);
        }

        public void WriteStringPacked(string s, bool knownLength = false)
        {
            if (!knownLength) WriteUInt64Packed((ulong)s.Length);
            int target = s.Length;
            for (int i = 0; i < target; ++i) WriteCharPacked(s[i]);
        }

        public void WriteStringDiff(string write, string compare, bool knownLength = false)
        {

#if !ARRAY_DIFF_ALLOW_RESIZE
            if (write.Length != compare.Length) throw new ArgumentException("Mismatched string lengths");
#endif
            if (!knownLength) WriteUInt64Packed((ulong)write.Length);

            // Premapping
#if ARRAY_WRITE_PREMAP
            int target;
#if ARRAY_DIFF_ALLOW_RESIZE
            target = Math.Min(write.Length, compare.Length);
#else
            target = a1.Length;
#endif
            for (int i = 0; i < target; ++i) WriteBit(write[i] != compare[i]);
#endif
            for(int i = 0; i<target; ++i)
            {

                bool b = write[i] == compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteChar(write[i]);
            }
        }

        public void WriteStringPackedDiff(string write, string compare, bool knownLength = false)
        {

#if !ARRAY_DIFF_ALLOW_RESIZE
            if (write.Length != compare.Length) throw new ArgumentException("Mismatched string lengths");
#endif
            if (!knownLength) WriteUInt64Packed((ulong)write.Length);

            // Premapping
#if ARRAY_WRITE_PREMAP
            int target;
#if ARRAY_DIFF_ALLOW_RESIZE
            target = Math.Min(write.Length, compare.Length);
#else
            target = a1.Length;
#endif
            for (int i = 0; i < target; ++i) WriteBit(write[i] != compare[i]);
#endif
            for (int i = 0; i < target; ++i)
            {

                bool b = write[i] == compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteCharPacked(write[i]);
            }
        }

#if ARRAY_RESOLVE_IMPLICIT
        public void WriteArray(Array a, long count = -1, bool knownSize = false)
        {
            Type arrayType = a.GetType();

            if (arrayType == typeof(byte[])) WriteByteArray(a as byte[], count, knownSize);
            else if (arrayType == typeof(short[])) WriteShortArray(a as short[], count, knownSize);
            else if (arrayType == typeof(ushort[])) WriteUShortArray(a as ushort[], count, knownSize);
            else if (arrayType == typeof(char[])) WriteCharArray(a as char[], count, knownSize);
            else if (arrayType == typeof(int[])) WriteIntArray(a as int[], count, knownSize);
            else if (arrayType == typeof(uint[])) WriteUIntArray(a as uint[], count, knownSize);
            else if (arrayType == typeof(long[])) WriteLongArray(a as long[], count, knownSize);
            else if (arrayType == typeof(ulong[])) WriteULongArray(a as ulong[], count, knownSize);
            else if (arrayType == typeof(float[])) WriteFloatArray(a as float[], count, knownSize);
            else if (arrayType == typeof(double[])) WriteDoubleArray(a as double[], count, knownSize);
            else throw new InvalidDataException("Unknown array type! Please serialize manually!");
        }

        public void WriteArrayDiff(Array write, Array compare, long count = -1, bool knownSize = false)
        {
            Type arrayType = write.GetType();
            if (arrayType != compare.GetType()) throw new ArrayTypeMismatchException("Cannot write diff of two differing array types");

            if (arrayType == typeof(byte[])) WriteByteArrayDiff(write as byte[], compare as byte[], count, knownSize);
            else if (arrayType == typeof(short[])) WriteShortArrayDiff(write as short[], compare as short[], count, knownSize);
            else if (arrayType == typeof(ushort[])) WriteUShortArrayDiff(write as ushort[], compare as ushort[], count, knownSize);
            else if (arrayType == typeof(char[])) WriteCharArrayDiff(write as char[], compare as char[], count, knownSize);
            else if (arrayType == typeof(int[])) WriteIntArrayDiff(write as int[], compare as int[], count, knownSize);
            else if (arrayType == typeof(uint[])) WriteUIntArrayDiff(write as uint[], compare as uint[], count, knownSize);
            else if (arrayType == typeof(long[])) WriteLongArrayDiff(write as long[], compare as long[], count, knownSize);
            else if (arrayType == typeof(ulong[])) WriteULongArrayDiff(write as ulong[], compare as ulong[], count, knownSize);
            else if (arrayType == typeof(float[])) WriteFloatArrayDiff(write as float[], compare as float[], count, knownSize);
            else if (arrayType == typeof(double[])) WriteDoubleArrayDiff(write as double[], compare as double[], count, knownSize);
            else throw new InvalidDataException("Unknown array type! Please serialize manually!");
        }
#endif

        private void CheckLengths(Array a1, Array a2)
        {
#if !ARRAY_DIFF_ALLOW_RESIZE
            if (a1.LongLength != a2.LongLength) throw new ArgumentException("Mismatched array lengths");
#endif
        }
        [Conditional("ARRAY_WRITE_PREMAP")]
        private void WritePremap(Array a1, Array a2)
        {
            long target;
#if ARRAY_DIFF_ALLOW_RESIZE
            target = Math.Min(a1.LongLength, a2.LongLength);
#else
            target = a1.LongLength;
#endif
            for (long i = 0; i < target; ++i) WriteBit(a1.GetValue(i)!=a2.GetValue(i));
        }
        private ulong WriteArraySize(Array a1, Array a2, long length, bool known)
        {
            ulong write =
                (ulong)
                        (
                            length > 0 ?
                            length :
#if ARRAY_DIFF_ALLOW_RESIZE
                            Math.Min(a1.LongLength, a2 == null ? 0 : a2.LongLength)
#else
                            a1.LongLength
#endif
                        );
            if (!known)
            {

                if (length > a1.LongLength) throw new IndexOutOfRangeException("Cannot write more data than is available");
                WriteUInt64Packed(write);
            }
            return write;
        }


        public void WriteByteArray(byte[] b, long count = -1, bool knownSize = false)
        {
            ulong target = WriteArraySize(b, null, count, knownSize);
            for (ulong i = 0; i < target; ++i) _WriteByte(b[i]);
            UpdateLength();
        }

        public void WriteByteArrayDiff(byte[] write, byte[] compare, long count = -1, bool knownSize = false)
        {
            CheckLengths(write, compare);
            ulong target = WriteArraySize(write, compare, count, knownSize);
            WritePremap(write, compare);
            for (ulong i = 0; i < target; ++i)
            {
                bool b = write[i] == compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if(b) WriteByte(write[i]);
            }
        }

        public void WriteShortArray(short[] b, long count = -1, bool knownSize = false)
        {
            ulong target = WriteArraySize(b, null, count, knownSize);
            for (ulong i = 0; i < target; ++i) WriteInt16(b[i]);
        }

        public void WriteShortArrayDiff(short[] write, short[] compare, long count = -1, bool knownSize = false)
        {
            CheckLengths(write, compare);
            ulong target = WriteArraySize(write, compare, count, knownSize);
            WritePremap(write, compare);
            for (ulong i = 0; i < target; ++i)
            {
                bool b = write[i] == compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteInt16(write[i]);
            }
        }

        public void WriteUShortArray(ushort[] b, long count = -1, bool knownSize = false)
        {
            ulong target = WriteArraySize(b, null, count, knownSize);
            for (ulong i = 0; i < target; ++i) WriteUInt16(b[i]);
        }

        public void WriteUShortArrayDiff(ushort[] write, ushort[] compare, long count = -1, bool knownSize = false)
        {
            CheckLengths(write, compare);
            ulong target = WriteArraySize(write, compare, count, knownSize);
            WritePremap(write, compare);
            for (ulong i = 0; i < target; ++i)
            {
                bool b = write[i] == compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteUInt16(write[i]);
            }
        }

        public void WriteCharArray(char[] b, long count = -1, bool knownSize = false)
        {
            ulong target = WriteArraySize(b, null, count, knownSize);
            for (ulong i = 0; i < target; ++i) WriteChar(b[i]);
        }

        public void WriteCharArrayDiff(char[] write, char[] compare, long count = -1, bool knownSize = false)
        {
            CheckLengths(write, compare);
            ulong target = WriteArraySize(write, compare, count, knownSize);
            WritePremap(write, compare);
            for (ulong i = 0; i < target; ++i)
            {
                bool b = write[i] == compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteChar(write[i]);
            }
        }

        public void WriteIntArray(int[] b, long count = -1, bool knownSize = false)
        {
            ulong target = WriteArraySize(b, null, count, knownSize);
            for (ulong i = 0; i < target; ++i) WriteInt32(b[i]);
        }

        public void WriteIntArrayDiff(int[] write, int[] compare, long count = -1, bool knownSize = false)
        {
            CheckLengths(write, compare);
            ulong target = WriteArraySize(write, compare, count, knownSize);
            WritePremap(write, compare);
            for (ulong i = 0; i < target; ++i)
            {
                bool b = write[i] == compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteInt32(write[i]);
            }
        }

        public void WriteUIntArray(uint[] b, long count = -1, bool knownSize = false)
        {
            ulong target = WriteArraySize(b, null, count, knownSize);
            for (ulong i = 0; i < target; ++i) WriteUInt32(b[i]);
        }

        public void WriteUIntArrayDiff(uint[] write, uint[] compare, long count = -1, bool knownSize = false)
        {
            CheckLengths(write, compare);
            ulong target = WriteArraySize(write, compare, count, knownSize);
            WritePremap(write, compare);
            for (ulong i = 0; i < target; ++i)
            {
                bool b = write[i] == compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteUInt32(write[i]);
            }
        }

        public void WriteLongArray(long[] b, long count = -1, bool knownSize = false)
        {
            ulong target = WriteArraySize(b, null, count, knownSize);
            for (ulong i = 0; i < target; ++i) WriteInt64(b[i]);
        }

        public void WriteLongArrayDiff(long[] write, long[] compare, long count = -1, bool knownSize = false)
        {
            CheckLengths(write, compare);
            ulong target = WriteArraySize(write, compare, count, knownSize);
            WritePremap(write, compare);
            for (ulong i = 0; i < target; ++i)
            {
                bool b = write[i] == compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteInt64(write[i]);
            }
        }

        public void WriteULongArray(ulong[] b, long count = -1, bool knownSize = false)
        {
            ulong target = WriteArraySize(b, null, count, knownSize);
            for (ulong i = 0; i < target; ++i) WriteUInt64(b[i]);
        }

        public void WriteULongArrayDiff(ulong[] write, ulong[] compare, long count = -1, bool knownSize = false)
        {
            CheckLengths(write, compare);
            ulong target = WriteArraySize(write, compare, count, knownSize);
            WritePremap(write, compare);
            for (ulong i = 0; i < target; ++i)
            {
                bool b = write[i] == compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteUInt64(write[i]);
            }
        }

        public void WriteFloatArray(float[] b, long count = -1, bool knownSize = false)
        {
            ulong target = WriteArraySize(b, null, count, knownSize);
            for (ulong i = 0; i < target; ++i) WriteSingle(b[i]);
        }

        public void WriteFloatArrayDiff(float[] write, float[] compare, long count = -1, bool knownSize = false)
        {
            CheckLengths(write, compare);
            ulong target = WriteArraySize(write, compare, count, knownSize);
            WritePremap(write, compare);
            for (ulong i = 0; i < target; ++i)
            {
                bool b = write[i] == compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteSingle(write[i]);
            }
        }

        public void WriteDoubleArray(double[] b, long count = -1, bool knownSize = false)
        {
            ulong target = WriteArraySize(b, null, count, knownSize);
            for (ulong i = 0; i < target; ++i) WriteDouble(b[i]);
        }

        public void WriteDoubleArrayDiff(double[] write, double[] compare, long count = -1, bool knownSize = false)
        {
            CheckLengths(write, compare);
            ulong target = WriteArraySize(write, compare, count, knownSize);
            WritePremap(write, compare);
            for (ulong i = 0; i < target; ++i)
            {
                bool b = write[i] == compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteDouble(write[i]);
            }
        }





        // Packed arrays
#if ARRAY_RESOLVE_IMPLICIT
        public void WriteArrayPacked(Array a, long count = -1, bool knownSize = false)
        {
            Type arrayType = a.GetType();


#if ARRAY_WRITE_PERMISSIVE
            if (arrayType == typeof(byte[])) WriteByteArray(a as byte[], count, knownSize);
            else
#endif
            if (arrayType == typeof(short[])) WriteShortArrayPacked(a as short[], count, knownSize);
            else if (arrayType == typeof(ushort[])) WriteUShortArrayPacked(a as ushort[], count, knownSize);
            else if (arrayType == typeof(char[])) WriteCharArrayPacked(a as char[], count, knownSize);
            else if (arrayType == typeof(int[])) WriteIntArrayPacked(a as int[], count, knownSize);
            else if (arrayType == typeof(uint[])) WriteUIntArrayPacked(a as uint[], count, knownSize);
            else if (arrayType == typeof(long[])) WriteLongArrayPacked(a as long[], count, knownSize);
            else if (arrayType == typeof(ulong[])) WriteULongArrayPacked(a as ulong[], count, knownSize);
            else if (arrayType == typeof(float[])) WriteFloatArrayPacked(a as float[], count, knownSize);
            else if (arrayType == typeof(double[])) WriteDoubleArrayPacked(a as double[], count, knownSize);
            else throw new InvalidDataException("Unknown array type! Please serialize manually!");
        }

        public void WriteArrayPackedDiff(Array write, Array compare, long count = -1, bool knownSize = false)
        {
            Type arrayType = write.GetType();
            if (arrayType != compare.GetType()) throw new ArrayTypeMismatchException("Cannot write diff of two differing array types");

#if ARRAY_WRITE_PERMISSIVE
            if (arrayType == typeof(byte[])) WriteByteArrayDiff(write as byte[], compare as byte[], count, knownSize);
            else
#endif
            if (arrayType == typeof(short[])) WriteShortArrayPackedDiff(write as short[], compare as short[], count, knownSize);
            else if (arrayType == typeof(ushort[])) WriteUShortArrayPackedDiff(write as ushort[], compare as ushort[], count, knownSize);
            else if (arrayType == typeof(char[])) WriteCharArrayPackedDiff(write as char[], compare as char[], count, knownSize);
            else if (arrayType == typeof(int[])) WriteIntArrayPackedDiff(write as int[], compare as int[], count, knownSize);
            else if (arrayType == typeof(uint[])) WriteUIntArrayPackedDiff(write as uint[], compare as uint[], count, knownSize);
            else if (arrayType == typeof(long[])) WriteLongArrayPackedDiff(write as long[], compare as long[], count, knownSize);
            else if (arrayType == typeof(ulong[])) WriteULongArrayPackedDiff(write as ulong[], compare as ulong[], count, knownSize);
            else if (arrayType == typeof(float[])) WriteFloatArrayPackedDiff(write as float[], compare as float[], count, knownSize);
            else if (arrayType == typeof(double[])) WriteDoubleArrayPackedDiff(write as double[], compare as double[], count, knownSize);
            else throw new InvalidDataException("Unknown array type! Please serialize manually!");
        }
#endif

        public void WriteShortArrayPacked(short[] b, long count = -1, bool knownSize = false)
        {
            ulong target = WriteArraySize(b, null, count, knownSize);
            for (ulong i = 0; i < target; ++i) WriteInt16Packed(b[i]);
        }

        public void WriteShortArrayPackedDiff(short[] write, short[] compare, long count = -1, bool knownSize = false)
        {
            CheckLengths(write, compare);
            ulong target = WriteArraySize(write, compare, count, knownSize);
            WritePremap(write, compare);
            for (ulong i = 0; i < target; ++i)
            {
                bool b = write[i] == compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteInt16Packed(write[i]);
            }
        }

        public void WriteUShortArrayPacked(ushort[] b, long count = -1, bool knownSize = false)
        {
            ulong target = WriteArraySize(b, null, count, knownSize);
            for (ulong i = 0; i < target; ++i) WriteUInt16Packed(b[i]);
        }

        public void WriteUShortArrayPackedDiff(ushort[] write, ushort[] compare, long count = -1, bool knownSize = false)
        {
            CheckLengths(write, compare);
            ulong target = WriteArraySize(write, compare, count, knownSize);
            WritePremap(write, compare);
            for (ulong i = 0; i < target; ++i)
            {
                bool b = write[i] == compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteUInt16Packed(write[i]);
            }
        }

        public void WriteCharArrayPacked(char[] b, long count = -1, bool knownSize = false)
        {
            ulong target = WriteArraySize(b, null, count, knownSize);
            for (ulong i = 0; i < target; ++i) WriteCharPacked(b[i]);
        }

        public void WriteCharArrayPackedDiff(char[] write, char[] compare, long count = -1, bool knownSize = false)
        {
            CheckLengths(write, compare);
            ulong target = WriteArraySize(write, compare, count, knownSize);
            WritePremap(write, compare);
            for (ulong i = 0; i < target; ++i)
            {
                bool b = write[i] == compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteCharPacked(write[i]);
            }
        }

        public void WriteIntArrayPacked(int[] b, long count = -1, bool knownSize = false)
        {
            ulong target = WriteArraySize(b, null, count, knownSize);
            for (ulong i = 0; i < target; ++i) WriteInt32Packed(b[i]);
        }

        public void WriteIntArrayPackedDiff(int[] write, int[] compare, long count = -1, bool knownSize = false)
        {
            CheckLengths(write, compare);
            ulong target = WriteArraySize(write, compare, count, knownSize);
            WritePremap(write, compare);
            for (ulong i = 0; i < target; ++i)
            {
                bool b = write[i] == compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteInt32Packed(write[i]);
            }
        }

        public void WriteUIntArrayPacked(uint[] b, long count = -1, bool knownSize = false)
        {
            ulong target = WriteArraySize(b, null, count, knownSize);
            for (ulong i = 0; i < target; ++i) WriteUInt32Packed(b[i]);
        }

        public void WriteUIntArrayPackedDiff(uint[] write, uint[] compare, long count = -1, bool knownSize = false)
        {
            CheckLengths(write, compare);
            ulong target = WriteArraySize(write, compare, count, knownSize);
            WritePremap(write, compare);
            for (ulong i = 0; i < target; ++i)
            {
                bool b = write[i] == compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteUInt32Packed(write[i]);
            }
        }

        public void WriteLongArrayPacked(long[] b, long count = -1, bool knownSize = false)
        {
            ulong target = WriteArraySize(b, null, count, knownSize);
            for (ulong i = 0; i < target; ++i) WriteInt64Packed(b[i]);
        }

        public void WriteLongArrayPackedDiff(long[] write, long[] compare, long count = -1, bool knownSize = false)
        {
            CheckLengths(write, compare);
            ulong target = WriteArraySize(write, compare, count, knownSize);
            WritePremap(write, compare);
            for (ulong i = 0; i < target; ++i)
            {
                bool b = write[i] == compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteInt64Packed(write[i]);
            }
        }

        public void WriteULongArrayPacked(ulong[] b, long count = -1, bool knownSize = false)
        {
            ulong target = WriteArraySize(b, null, count, knownSize);
            for (ulong i = 0; i < target; ++i) WriteUInt64Packed(b[i]);
        }

        public void WriteULongArrayPackedDiff(ulong[] write, ulong[] compare, long count = -1, bool knownSize = false)
        {
            CheckLengths(write, compare);
            ulong target = WriteArraySize(write, compare, count, knownSize);
            WritePremap(write, compare);
            for (ulong i = 0; i < target; ++i)
            {
                bool b = write[i] == compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteUInt64Packed(write[i]);
            }
        }

        public void WriteFloatArrayPacked(float[] b, long count = -1, bool knownSize = false)
        {
            ulong target = WriteArraySize(b, null, count, knownSize);
            for (ulong i = 0; i < target; ++i) WriteSinglePacked(b[i]);
        }

        public void WriteFloatArrayPackedDiff(float[] write, float[] compare, long count = -1, bool knownSize = false)
        {
            CheckLengths(write, compare);
            ulong target = WriteArraySize(write, compare, count, knownSize);
            WritePremap(write, compare);
            for (ulong i = 0; i < target; ++i)
            {
                bool b = write[i] == compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteSinglePacked(write[i]);
            }
        }

        public void WriteDoubleArrayPacked(double[] b, long count = -1, bool knownSize = false)
        {
            ulong target = WriteArraySize(b, null, count, knownSize);
            for (ulong i = 0; i < target; ++i) WriteDoublePacked(b[i]);
        }

        public void WriteDoubleArrayPackedDiff(double[] write, double[] compare, long count = -1, bool knownSize = false)
        {
            CheckLengths(write, compare);
            ulong target = WriteArraySize(write, compare, count, knownSize);
            WritePremap(write, compare);
            for (ulong i = 0; i < target; ++i)
            {
                bool b = write[i] == compare[i];
#if !ARRAY_WRITE_PREMAP
                WriteBit(!b);
#endif
                if (b) WriteDoublePacked(write[i]);
            }
        }





        // Read arrays
        public byte[] ReadByteArray(byte[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new byte[knownLength];
            for (long i = 0; i < knownLength; ++i) readTo[i] = _ReadByte();
            return readTo;
        }

        public byte[] ReadByteArrayDiff(byte[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            byte[] writeTo = readTo == null || readTo.LongLength != knownLength ? new byte[knownLength] : readTo;
            for (long i = 0; i < knownLength; ++i)
            {
                if (ReadBit()) {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    BitPosition += (ulong)(knownLength - i + (i*8L));
#endif
                    // Read datum
                    writeTo[i] = _ReadByte();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    BitPosition -= (ulong)(knownLength - i -1 + (i * 8L) + 8);
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
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
            for(long i = 0; i<knownLength; ++i) readTo[i] = ReadInt16Packed();
            return readTo;
        }

        public short[] ReadShortArrayDiff(short[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new short[knownLength];
            for (long i = 0; i < knownLength; ++i)
            {
                if (ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    BitPosition += (ulong)(knownLength - i -1 + (i * 16L));
#endif
                    // Read datum
                    readTo[i] = ReadInt16();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    BitPosition -= (ulong)(knownLength - i -1 + (i * 16L) + 16);
#endif
                }
            }
            return readTo;
        }

        public short[] ReadShortArrayPackedDiff(short[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            short[] writeTo = readTo == null || readTo.LongLength != knownLength ? new short[knownLength] : readTo;
            ulong data = BitPosition + (ulong)knownLength;
            ulong rset;
            for (long i = 0; i < knownLength; ++i)
            {
                if (ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    rset = BitPosition;
                    BitPosition = data;
#endif
                    // Read datum
                    writeTo[i] = ReadInt16Packed();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    data = BitPosition;
                    BitPosition = rset;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
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
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new ushort[knownLength];
            for (long i = 0; i < knownLength; ++i)
            {
                if (ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    BitPosition += (ulong)(knownLength - i -1 + (i * 16L));
#endif
                    // Read datum
                    readTo[i] = ReadUInt16();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    BitPosition -= (ulong)(knownLength - i -1 + (i * 16L) + 16);
#endif
                }
            }
            return readTo;
        }

        public ushort[] ReadUShortArrayPackedDiff(ushort[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            ushort[] writeTo = readTo == null || readTo.LongLength != knownLength ? new ushort[knownLength] : readTo;
            ulong data = BitPosition + (ulong)knownLength;
            ulong rset;
            for (long i = 0; i < knownLength; ++i)
            {
                if (ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    rset = BitPosition;
                    BitPosition = data;
#endif
                    // Read datum
                    writeTo[i] = ReadUInt16Packed();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    data = BitPosition;
                    BitPosition = rset;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
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
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new int[knownLength];
            for (long i = 0; i < knownLength; ++i)
            {
                if (ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    BitPosition += (ulong)(knownLength - i -1 + (i * 32L));
#endif
                    // Read datum
                    readTo[i] = ReadInt32();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    BitPosition -= (ulong)(knownLength - i -1 + (i * 32L) + 32);
#endif
                }
            }
            return readTo;
        }

        public int[] ReadIntArrayPackedDiff(int[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            int[] writeTo = readTo == null || readTo.LongLength != knownLength ? new int[knownLength] : readTo;
            ulong data = BitPosition + (ulong)knownLength;
            ulong rset;
            for (long i = 0; i < knownLength; ++i)
            {
                if (ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    rset = BitPosition;
                    BitPosition = data;
#endif
                    // Read datum
                    writeTo[i] = ReadInt32Packed();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    data = BitPosition;
                    BitPosition = rset;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
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
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new uint[knownLength];
            for (long i = 0; i < knownLength; ++i)
            {
                if (ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    BitPosition += (ulong)(knownLength - i -1 + (i * 32L));
#endif
                    // Read datum
                    readTo[i] = ReadUInt32();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    BitPosition -= (ulong)(knownLength - i -1 + (i * 32L) + 32);
#endif
                }
            }
            return readTo;
        }

        public uint[] ReadUIntArrayPackedDiff(uint[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            uint[] writeTo = readTo == null || readTo.LongLength != knownLength ? new uint[knownLength] : readTo;
            ulong data = BitPosition + (ulong)knownLength;
            ulong rset;
            for (long i = 0; i < knownLength; ++i)
            {
                if (ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    rset = BitPosition;
                    BitPosition = data;
#endif
                    // Read datum
                    writeTo[i] = ReadUInt32Packed();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    data = BitPosition;
                    BitPosition = rset;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
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
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new long[knownLength];
            for (long i = 0; i < knownLength; ++i)
            {
                if (ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    BitPosition += (ulong)(knownLength - i -1 + (i * 64L));
#endif
                    // Read datum
                    readTo[i] = ReadInt64();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    BitPosition -= (ulong)(knownLength - i -1 + (i * 64L) + 64);
#endif
                }
            }
            return readTo;
        }

        public long[] ReadLongArrayPackedDiff(long[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            long[] writeTo = readTo == null || readTo.LongLength != knownLength ? new long[knownLength] : readTo;
            ulong data = BitPosition + (ulong)knownLength;
            ulong rset;
            for (long i = 0; i < knownLength; ++i)
            {
                if (ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    rset = BitPosition;
                    BitPosition = data;
#endif
                    // Read datum
                    writeTo[i] = ReadInt64Packed();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    data = BitPosition;
                    BitPosition = rset;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
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
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new ulong[knownLength];
            for (long i = 0; i < knownLength; ++i)
            {
                if (ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    BitPosition += (ulong)(knownLength - i -1 + (i * 64L));
#endif
                    // Read datum
                    readTo[i] = ReadUInt64();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    BitPosition -= (ulong)(knownLength - i -1 + (i * 64L) + 64);
#endif
                }
            }
            return readTo;
        }

        public ulong[] ReadULongArrayPackedDiff(ulong[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            ulong[] writeTo = readTo == null || readTo.LongLength != knownLength ? new ulong[knownLength] : readTo;
            ulong data = BitPosition + (ulong)knownLength;
            ulong rset;
            for (long i = 0; i < knownLength; ++i)
            {
                if (ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    rset = BitPosition;
                    BitPosition = data;
#endif
                    // Read datum
                    writeTo[i] = ReadUInt64Packed();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    data = BitPosition;
                    BitPosition = rset;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
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
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new float[knownLength];
            for (long i = 0; i < knownLength; ++i)
            {
                if (ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    BitPosition += (ulong)(knownLength - i -1 + (i * 32L));
#endif
                    // Read datum
                    readTo[i] = ReadSingle();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    BitPosition -= (ulong)(knownLength - i -1 + (i * 32L) + 32);
#endif
                }
            }
            return readTo;
        }

        public float[] ReadFloatArrayPackedDiff(float[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            float[] writeTo = readTo == null || readTo.LongLength != knownLength ? new float[knownLength] : readTo;
            ulong data = BitPosition + (ulong)knownLength;
            ulong rset;
            for (long i = 0; i < knownLength; ++i)
            {
                if (ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    rset = BitPosition;
                    BitPosition = data;
#endif
                    // Read datum
                    readTo[i] = ReadSinglePacked();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    data = BitPosition;
                    BitPosition = rset;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
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
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            if (readTo == null || readTo.LongLength != knownLength) readTo = new double[knownLength];
            for (long i = 0; i < knownLength; ++i)
            {
                if (ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    BitPosition += (ulong)(knownLength - i - 1 + (i * 64L));
#endif
                    // Read datum
                    readTo[i] = ReadDouble();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    BitPosition -= (ulong)(knownLength - i - 1 + (i * 64L) + 64);
#endif
                }
            }
            return readTo;
        }

        public double[] ReadDoubleArrayPackedDiff(double[] readTo = null, long knownLength = -1)
        {
            if (knownLength < 0) knownLength = (long)ReadUInt64Packed();
            double[] writeTo = readTo == null || readTo.LongLength != knownLength ? new double[knownLength] : readTo;
            ulong data = BitPosition + (ulong)knownLength;
            ulong rset;
            for (long i = 0; i < knownLength; ++i)
            {
                if (ReadBit())
                {
#if ARRAY_WRITE_PREMAP
                    // Move to data section
                    rset = BitPosition;
                    BitPosition = data;
#endif
                    // Read datum
                    writeTo[i] = ReadDoublePacked();
#if ARRAY_WRITE_PREMAP
                    // Return to mapping section
                    data = BitPosition;
                    BitPosition = rset;
#endif
                }
                else if (i < readTo.LongLength) writeTo[i] = readTo[i];
            }
            return writeTo;
        }




        /// <summary>
        /// Copy data from another stream
        /// </summary>
        /// <param name="s">Stream to copy from</param>
        /// <param name="count">How many bytes to read. Set to value less than one to read until ReadByte returns -1</param>
        public void CopyFrom(Stream s, int count = -1)
        {
            if (s is BitStream b) Write(b.target, 0, count < 0 ? (int)b.Length : count);
            else
            {
                int read;
                bool readToEnd = count < 0;
                while ((readToEnd || count-- > 0) && (read = s.ReadByte()) != -1)
                    _WriteIntByte(read);
                UpdateLength();
            }
        }

        // TODO: Implement CopyFrom() for BitStream with bitCount parameter

        /// <summary>
        /// Update length of data considered to be "written" to the stream.
        /// </summary>
        private void UpdateLength()
        {
            if (BitPosition > BitLength) BitLength = BitPosition;
        }
        /// <summary>
        /// Get the internal buffer being written to by this stream.
        /// </summary>
        /// <returns></returns>
        public byte[] GetBuffer() => target;

        /// <summary>
        /// Creates a copy of the internal buffer. This only contains the used bytes
        /// </summary>
        /// <returns>A copy of used bytes in the internal buffer</returns>
        public byte[] ToArray()
        {
            byte[] copy = new byte[Length];
            Buffer.BlockCopy(target, 0, copy, 0, (int)Length);
            return copy;
        }

        /// <summary>
        /// Returns hex encoded version of the buffer
        /// </summary>
        /// <returns>Hex encoded version of the buffer</returns>
        public override string ToString() => BitConverter.ToString(target, 0, (int)Length);
    }
}
