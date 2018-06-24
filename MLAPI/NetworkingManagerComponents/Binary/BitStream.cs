using System;
using System.IO;
using UnityEngine;
using static MLAPI.NetworkingManagerComponents.Binary.Arithmetic;

namespace MLAPI.NetworkingManagerComponents.Binary
{
    public sealed class BitStream : Stream
    {
        const int initialCapacity = 16;
        const float initialGrowthFactor = 2.0f;
        private byte[] target;
        private static readonly float[] holder_f = new float[1];
        private static readonly double[] holder_d = new double[1];
        private static readonly uint[] holder_i = new uint[1];
        private static readonly ulong[] holder_l = new ulong[1];

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
            BitLength = (ulong) (target.Length << 3);
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
        public long Capacity {
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
        public override long Position { get => (long)(BitPosition>>3); set => BitPosition = (ulong)value << 3; }

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
            return (byte)((target[(int)Position++] >> mod) | (target[(int)Position] << (8 - mod)));
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
            if (value > Capacity) Grow(value-Capacity);
            BitLength = (ulong)value << 3;
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
            ulong pos = BitPosition >> 3;
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
            lock (holder_f)
                lock (holder_i)
                {
                    holder_f[0] = value;
                    Buffer.BlockCopy(holder_f, 0, holder_i, 0, 4);
                    WriteUInt32(holder_i[0]);
                }
        }

        /// <summary>
        /// Write double-precision floating point value to the stream
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteDouble(double value)
        {
            lock (holder_d)
                lock (holder_l)
                {
                    holder_d[0] = value;
                    Buffer.BlockCopy(holder_d, 0, holder_l, 0, 8);
                    WriteUInt64(holder_l[0]);
                }
        }

        /// <summary>
        /// Write single-precision floating point value to the stream as a varint
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteSinglePacked(float value)
        {
            lock(holder_f)
                lock (holder_i)
                {
                    holder_f[0] = value;
                    Buffer.BlockCopy(holder_f, 0, holder_i, 0, 4);
                    WriteUInt32Packed(BinaryHelpers.SwapEndian(holder_i[0]));
                }
        }

        /// <summary>
        /// Write double-precision floating point value to the stream as a varint
        /// </summary>
        /// <param name="value">Value to write</param>
        public void WriteDoublePacked(double value)
        {
            lock (holder_d)
                lock (holder_l)
                {
                    holder_d[0] = value;
                    Buffer.BlockCopy(holder_d, 0, holder_l, 0, 8);
                    WriteUInt64Packed(BinaryHelpers.SwapEndian(holder_l[0]));
                }
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
            uint result = (uint)(((value + minValue)/(maxValue+minValue))*((0x100*bytes) - 1));
            for (int i = 0; i < bytes; ++i) _WriteByte((byte)(result >> (i<<3)));
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
            ulong result = (ulong)(((value + minValue) / (maxValue+minValue)) * ((0x100 * bytes) - 1));
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
            if (bytesPerAngle==4) WriteVector3(rotation.eulerAngles);
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
            uint read = ReadUInt32();
            lock (holder_f)
                lock (holder_i)
                {
                    holder_i[0] = read;
                    Buffer.BlockCopy(holder_i, 0, holder_f, 0, 4);
                    return holder_f[0];
                }
        }


        /// <summary>
        /// Read a double-precision floating point value from the stream.
        /// </summary>
        /// <returns>The read value</returns>
        public double ReadDouble()
        {
            ulong read = ReadUInt64();
            lock (holder_d)
                lock (holder_l)
                {
                    holder_l[0] = read;
                    Buffer.BlockCopy(holder_l, 0, holder_d, 0, 8);
                    return holder_d[0];
                }
        }
        
        /// <summary>
        /// Read a single-precision floating point value from the stream from a varint
        /// </summary>
        /// <returns>The read value</returns>
        public float ReadSinglePacked()
        {
            uint read = ReadUInt32Packed();
            lock(holder_f)
                lock (holder_i)
                {
                    holder_i[0] = BinaryHelpers.SwapEndian(read);
                    Buffer.BlockCopy(holder_i, 0, holder_f, 0, 4);
                    return holder_f[0];
                }
        }

        /// <summary>
        /// Read a double-precision floating point value from the stream as a varint
        /// </summary>
        /// <returns>The read value</returns>
        public double ReadDoublePacked()
        {
            ulong read = ReadUInt64Packed();
            lock (holder_d)
                lock (holder_l)
                {
                    holder_l[0] = BinaryHelpers.SwapEndian(read);
                    Buffer.BlockCopy(holder_l, 0, holder_d, 0, 8);
                    return holder_d[0];
                }
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
        /// <param name="value">Value to write</param>
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
                if(offset > 4) target[Position + 1] = (byte)((target[Position + 1] & (0xFF << (offset & 3))) | (byte)(value >> offset_inv));
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
            if (BitPosition + (ulong)bitCount > ((ulong)target.LongLength << 3)) Grow(Div8Ceil(BitPosition+(ulong)bitCount));
            if (bitCount > 64) throw new ArgumentOutOfRangeException("Cannot read more than 64 bits from a 64-bit value!");
            if (bitCount < 0) throw new ArgumentOutOfRangeException("Cannot read fewer than 0 bits!");
            int count = -8;
            while (bitCount > (count+=8)) _WriteULongByte(value >> count);
            BitPosition += (ulong)count;
            if((bitCount & 7) != 0) _WriteBits((byte)(value >> count), bitCount & 7);
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
                    (value<<offset)                                         // Bits to write
                );
            if (bitCount + offset > 8)
                target[Position + 1] = (byte)(
                        (target[Position + 1] & (0xFF << ((bitCount + offset) & 7))) |  // Bits after upper part of value (higher)
                        (value >> (16 - bitCount - offset))                             // upper part of value
                    );
            BitPosition += (ulong)bitCount;
        }
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
        public ulong ReadUInt64() => (ulong)(
                _ReadByte() |
                (_ReadByte() << 8) |
                (_ReadByte() << 16) |
                (_ReadByte() << 24) |
                (_ReadByte() << 32) |
                (_ReadByte() << 40) |
                (_ReadByte() << 48) |
                (_ReadByte() << 56)
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
            target[Position + 1] = (byte)((target[Position + 1] & (0xFF >> off)) | (value >> shift1));
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
        /// <summary>
        /// Copy data from another stream
        /// </summary>
        /// <param name="s">Stream to copy from</param>
        /// <param name="count">How many bytes to read. Set to value less than one to read until ReadByte returns -1</param>
        public void CopyFrom(Stream s, int count = -1)
        {
            if(s is BitStream b) Write(b.target, 0, count < 0 ? (int)b.Length : count);
            else
            {
                int read;
                bool readToEnd = count < 0;
                while((readToEnd || count-- > 0) && (read = s.ReadByte()) != -1)
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
