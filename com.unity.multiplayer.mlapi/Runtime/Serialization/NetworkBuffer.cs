using System;
using System.IO;
using static MLAPI.Serialization.Arithmetic;

namespace MLAPI.Serialization
{
    /// <summary>
    /// A buffer that can be used at the bit level
    /// </summary>
    public class NetworkBuffer : Stream
    {
        private const int k_InitialCapacity = 16;
        private const float k_InitialGrowthFactor = 2.0f;

        private byte[] m_Target;

        /// <summary>
        /// A buffer that supports writing data smaller than a single byte. This buffer also has a built-in compression algorithm that can (optionally) be used to write compressed data.
        /// </summary>
        /// <param name="capacity">Initial capacity of buffer in bytes.</param>
        /// <param name="growthFactor">Factor by which buffer should grow when necessary.</param>
        public NetworkBuffer(int capacity, float growthFactor)
        {
            m_Target = new byte[capacity];
            GrowthFactor = growthFactor;
            Resizable = true;
        }

        /// <summary>
        /// A buffer that supports writing data smaller than a single byte. This buffer also has a built-in compression algorithm that can (optionally) be used to write compressed data.
        /// </summary>
        /// <param name="growthFactor">Factor by which buffer should grow when necessary.</param>
        public NetworkBuffer(float growthFactor) : this(k_InitialCapacity, growthFactor) { }

        /// <summary>
        /// A buffer that supports writing data smaller than a single byte. This buffer also has a built-in compression algorithm that can (optionally) be used to write compressed data.
        /// </summary>
        /// <param name="capacity"></param>
        public NetworkBuffer(int capacity) : this(capacity, k_InitialGrowthFactor) { }

        /// <summary>
        /// A buffer that supports writing data smaller than a single byte. This buffer also has a built-in compression algorithm that can (optionally) be used to write compressed data.
        /// </summary>
        public NetworkBuffer() : this(k_InitialCapacity, k_InitialGrowthFactor) { }

        /// <summary>
        /// A buffer that supports writing data smaller than a single byte. This buffer also has a built-in compression algorithm that can (optionally) be used to write compressed data.
        /// NOTE: when using a pre-allocated buffer, the buffer will not grow!
        /// </summary>
        /// <param name="target">Pre-allocated buffer to write to</param>
        public NetworkBuffer(byte[] target)
        {
            m_Target = target;
            Resizable = false;
            BitLength = (ulong)(target.Length << 3);
        }

        internal void SetTarget(byte[] target)
        {
            m_Target = target;
            BitLength = (ulong)(target.Length << 3);
            Position = 0;
        }

        /// <summary>
        /// Whether or not the buffer will grow the buffer to accomodate more data.
        /// </summary>
        public bool Resizable { get; }

        private float m_GrowthFactor;

        /// <summary>
        /// Factor by which buffer should grow when necessary.
        /// </summary>
        public float GrowthFactor { set { m_GrowthFactor = value <= 1 ? 1.5f : value; } get { return m_GrowthFactor; } }

        /// <summary>
        /// Whether or not buffeer supports reading. (Always true)
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// Whether or not or there is any data to be read from the buffer.
        /// </summary>
        public bool HasDataToRead => Position < Length;

        /// <summary>
        /// Whether or not seeking is supported by this buffer. (Always true)
        /// </summary>
        public override bool CanSeek => true;

        /// <summary>
        /// Whether or not this buffer can accept new data. NOTE: this will return true even if only fewer than 8 bits can be written!
        /// </summary>
        public override bool CanWrite => !BitAligned || Position < m_Target.LongLength || Resizable;

        /// <summary>
        /// Current buffer size. The buffer will not be resized (if possible) until Position is equal to Capacity and an attempt to write data is made.
        /// </summary>
        public long Capacity
        {
            get => m_Target.LongLength; // Optimized CeilingExact
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
        /// The index that will be written to when any call to write data is made to this buffer.
        /// </summary>
        public override long Position { get => (long)(BitPosition >> 3); set => BitPosition = (ulong)value << 3; }

        /// <summary>
        /// Bit offset into the buffer that new data will be written to.
        /// </summary>
        public ulong BitPosition { get; set; }

        /// <summary>
        /// Length of data (in bits) that is considered to be written to the buffer.
        /// </summary>
        public ulong BitLength { get; private set; }

        /// <summary>
        /// Whether or not the current BitPosition is evenly divisible by 8. I.e. whether or not the BitPosition is at a byte boundary.
        /// </summary>
        public bool BitAligned { get => (BitPosition & 7) == 0; }

        /// <summary>
        /// Flush buffer. This does nothing since data is written directly to a byte buffer.
        /// </summary>
        public override void Flush() { } // NOP

        /// <summary>
        /// Grow buffer if possible. According to Max(bufferLength, 1) * growthFactor^Ceil(newContent/Max(bufferLength, 1))
        /// </summary>
        /// <param name="newContent">How many new values need to be accomodated (at least).</param>
        //private void Grow(long newContent) => SetCapacity(Math.Max(target.LongLength, 1) * (long)Math.Pow(GrowthFactor, CeilingExact(newContent, Math.Max(target.LongLength, 1))));
        /*
        private void Grow(long newContent)
        {
            float grow = newContent / 64;
            if (((long)grow) != grow) grow += 1;
            SetCapacity((Capacity + 64) * (long)grow);
        }
        */
        private void Grow(long newContent)
        {
            long value = newContent + Capacity;
            long newCapacity = value;

            if (newCapacity < 256) newCapacity = 256;
            // We are ok with this overflowing since the next statement will deal
            // with the cases where _capacity*2 overflows.
            if (newCapacity < Capacity * 2) newCapacity = Capacity * 2;
            // We want to expand the array up to Array.MaxArrayLengthOneDimensional
            // And we want to give the user the value that they asked for
            if ((uint)(Capacity * 2) > int.MaxValue) newCapacity = value > int.MaxValue ? value : int.MaxValue;

            SetCapacity(newCapacity);
        }

        /// <summary>
        /// Read a misaligned byte. WARNING: If the current BitPosition <strong>isn't</strong> byte misaligned,
        /// avoid using this method as it <strong>may</strong> cause an IndexOutOfBoundsException in such a case.
        /// </summary>
        /// <returns>A byte extracted from up to two separate buffer indices.</returns>
        private byte ReadByteMisaligned()
        {
            int mod = (int)(BitPosition & 7);
            return (byte)((m_Target[(int)Position] >> mod) | (m_Target[(int)(BitPosition += 8) >> 3] << (8 - mod)));
        }

        /// <summary>
        /// Read an aligned byte from the buffer. It's recommended to not use this when the BitPosition is byte-misaligned.
        /// </summary>
        /// <returns>The byte stored at the current Position index</returns>
        private byte ReadByteAligned() => m_Target[Position++];

        /// <summary>
        /// Read a byte as a byte. This is just for internal use so as to minimize casts (cuz they ugly af).
        /// </summary>
        /// <returns></returns>
        internal byte ReadByteInternal() => BitAligned ? ReadByteAligned() : ReadByteMisaligned();

        /// <summary>
        /// Read a byte from the buffer. This takes into account possible byte misalignment.
        /// </summary>
        /// <returns>A byte from the buffer or, if a byte can't be read, -1.</returns>
        public override int ReadByte() => HasDataToRead ? BitAligned ? ReadByteAligned() : ReadByteMisaligned() : -1;

        /// <summary>
        /// Peeks a byte without advancing the position
        /// </summary>
        /// <returns>The peeked byte</returns>
        public int PeekByte() =>
            HasDataToRead
                ? BitAligned ? m_Target[Position] :
                (byte)((m_Target[(int)Position] >> (int)(BitPosition & 7)) | (m_Target[(int)(BitPosition + 8) >> 3] << (8 - (int)(BitPosition & 7))))
                : -1;

        /// <summary>
        /// Read a single bit from the buffer.
        /// </summary>
        /// <returns>A bit in bool format. (True represents 1, False represents 0)</returns>
        public bool ReadBit() => (m_Target[Position] & (1 << (int)(BitPosition++ & 7))) != 0;

        /// <summary>
        /// Read a subset of the buffer buffer and write the contents to the supplied buffer.
        /// </summary>
        /// <param name="buffer">Buffer to copy data to.</param>
        /// <param name="offset">Offset into the buffer to write data to.</param>
        /// <param name="count">How many bytes to attempt to read.</param>
        /// <returns>Amount of bytes read.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int tLen = Math.Min(count, (int)(m_Target.LongLength - Position) - ((BitPosition & 7) == 0 ? 0 : 1));
            for (int i = 0; i < tLen; ++i) buffer[offset + i] = ReadByteInternal();
            return tLen;
        }

        /// <summary>
        /// Set position in buffer to read from/write to.
        /// </summary>
        /// <param name="offset">Offset from position origin.</param>
        /// <param name="origin">How to calculate offset.</param>
        /// <returns>The new position in the buffer that data will be written to.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return (long)((
                BitPosition =
                (
                    origin == SeekOrigin.Current ? offset > 0 ? Math.Min(BitPosition + ((ulong)offset << 3), (ulong)m_Target.Length << 3) :
                    (offset ^ SIGN_BIT_64) > Position ? 0UL :
                    BitPosition - (ulong)((offset ^ SIGN_BIT_64) << 3) :
                    origin == SeekOrigin.Begin ? (ulong)Math.Max(0, offset) << 3 :
                    (ulong)Math.Max(m_Target.Length - offset, 0) << 3
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
            long len = Math.Min(value, m_Target.LongLength);
            Buffer.BlockCopy(m_Target, 0, newTarg, 0, (int)len);
            if (value < m_Target.LongLength) BitPosition = (ulong)value << 3;
            m_Target = newTarg;
        }

        /// <summary>
        /// Set length of data considered to be "written" to the buffer.
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
        /// Write data from the given buffer to the internal buffer.
        /// </summary>
        /// <param name="buffer">Buffer to write from.</param>
        /// <param name="offset">Offset in given buffer to start reading from.</param>
        /// <param name="count">Amount of bytes to read copy from given buffer to buffer.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            // Check bit alignment. If misaligned, each byte written has to be misaligned
            if (BitAligned)
            {
                if (Position + count >= m_Target.Length) Grow(count);
                Buffer.BlockCopy(buffer, offset, m_Target, (int)Position, count);
                Position += count;
            }
            else
            {
                if (Position + count + 1 >= m_Target.Length) Grow(count);
                for (int i = 0; i < count; ++i) WriteMisaligned(buffer[offset + i]);
            }

            if (BitPosition > BitLength) BitLength = BitPosition;
        }

        /// <summary>
        /// Write byte value to the internal buffer.
        /// </summary>
        /// <param name="value">The byte value to write.</param>
        public override void WriteByte(byte value)
        {
            // Check bit alignment. If misaligned, each byte written has to be misaligned
            if (BitAligned)
            {
                if (Position + 1 >= m_Target.Length) Grow(1);
                m_Target[Position] = value;
                Position += 1;
            }
            else
            {
                if (Position + 1 + 1 >= m_Target.Length) Grow(1);
                WriteMisaligned(value);
            }

            if (BitPosition > BitLength) BitLength = BitPosition;
        }

        /// <summary>
        /// Write a misaligned byte. NOTE: Using this when the bit position isn't byte-misaligned may cause an IndexOutOfBoundsException! This does not update the current Length of the buffer.
        /// </summary>
        /// <param name="value">Value to write</param>
        private void WriteMisaligned(byte value)
        {
            int off = (int)(BitPosition & 7);
            int shift1 = 8 - off;
            m_Target[Position + 1] = (byte)((m_Target[Position + 1] & (0xFF << off)) | (value >> shift1));
            m_Target[Position] = (byte)((m_Target[Position] & (0xFF >> shift1)) | (value << off));

            BitPosition += 8;
        }


        /// <summary>
        /// Write a byte (in an int format) to the buffer. This does not update the current Length of the buffer.
        /// </summary>
        /// <param name="value">Value to write</param>
        private void WriteIntByte(int value) => WriteBytePrivate((byte)value);

        /// <summary>
        /// Write a byte (in a ulong format) to the buffer. This does not update the current Length of the buffer.
        /// </summary>
        /// <param name="byteValue">Value to write</param>
        private void WriteULongByte(ulong byteValue) => WriteBytePrivate((byte)byteValue);

        /// <summary>
        /// Write a byte to the buffer. This does not update the current Length of the buffer.
        /// </summary>
        /// <param name="value">Value to write</param>
        private void WriteBytePrivate(byte value)
        {
            if (Div8Ceil(BitPosition) == m_Target.LongLength) Grow(1);
            if (BitAligned)
            {
                m_Target[Position] = value;
                BitPosition += 8;
            }
            else WriteMisaligned(value);

            UpdateLength();
        }

        /// <summary>
        /// Write data from the given buffer to the internal buffer.
        /// </summary>
        /// <param name="buffer">Buffer to write from.</param>
        public void Write(byte[] buffer) => Write(buffer, 0, buffer.Length);

        /// <summary>
        /// Write a single bit to the buffer
        /// </summary>
        /// <param name="bit">Value of the bit. True represents 1, False represents 0</param>
        public void WriteBit(bool bit)
        {
            if (BitAligned && Position == m_Target.Length) Grow(1);
            int offset = (int)(BitPosition & 7);
            long pos = Position;
            ++BitPosition;
            m_Target[pos] = (byte)(bit ? (m_Target[pos] & ~(1 << offset)) | (1 << offset) : (m_Target[pos] & ~(1 << offset)));
            UpdateLength();
        }

        /// <summary>
        /// Copy data from another stream
        /// </summary>
        /// <param name="s">Stream to copy from</param>
        /// <param name="count">How many bytes to read. Set to value less than one to read until ReadByte returns -1</param>
        public void CopyFrom(Stream s, int count = -1)
        {
            if (s is NetworkBuffer b) Write(b.m_Target, 0, count < 0 ? (int)b.Length : count);
            else
            {
                long currentPosition = s.Position;
                s.Position = 0;

                int read;
                bool readToEnd = count < 0;
                while ((readToEnd || count-- > 0) && (read = s.ReadByte()) != -1)
                    WriteIntByte(read);
                UpdateLength();

                s.Position = currentPosition;
            }
        }

        /// <summary>
        /// Copies internal buffer to stream
        /// </summary>
        /// <param name="stream">The stream to copy to</param>
        /// <param name="count">The maximum amount of bytes to copy. Set to value less than one to copy the full length</param>
#if !NET35
        public new void CopyTo(Stream stream, int count = -1)
#else
        public void CopyTo(Stream stream, int count = -1)
#endif
        {
            stream.Write(m_Target, 0, count < 0 ? (int)Length : count);
        }

        /// <summary>
        /// Copies urnead bytes from the source stream
        /// </summary>
        /// <param name="s">The source stream to copy from</param>
        /// <param name="count">The max amount of bytes to copy</param>
        public void CopyUnreadFrom(Stream s, int count = -1)
        {
            long currentPosition = s.Position;

            int read;
            bool readToEnd = count < 0;
            while ((readToEnd || count-- > 0) && (read = s.ReadByte()) != -1) WriteIntByte(read);
            UpdateLength();

            s.Position = currentPosition;
        }

        // TODO: Implement CopyFrom() for NetworkBuffer with bitCount parameter
        /// <summary>
        /// Copys the bits from the provided NetworkBuffer
        /// </summary>
        /// <param name="buffer">The buffer to copy from</param>
        /// <param name="dataCount">The amount of data evel</param>
        /// <param name="copyBits">Whether or not to copy at the bit level rather than the byte level</param>
        public void CopyFrom(NetworkBuffer buffer, int dataCount, bool copyBits)
        {
            if (!copyBits)
            {
                CopyFrom(buffer, dataCount);
            }
            else
            {
                ulong count = dataCount < 0 ? buffer.BitLength : (ulong)dataCount;
                if (buffer.BitLength < count) throw new IndexOutOfRangeException("Attempted to read more data than is available");
                Write(buffer.GetBuffer(), 0, (int)(count >> 3));
                for (int i = (int)(count & 7); i >= 0; --i) WriteBit(buffer.ReadBit());
            }
        }

        /// <summary>
        /// Update length of data considered to be "written" to the buffer.
        /// </summary>
        private void UpdateLength()
        {
            if (BitPosition > BitLength) BitLength = BitPosition;
        }

        /// <summary>
        /// Get the internal buffer being written to by this buffer.
        /// </summary>
        /// <returns></returns>
        public byte[] GetBuffer() => m_Target;

        /// <summary>
        /// Creates a copy of the internal buffer. This only contains the used bytes
        /// </summary>
        /// <returns>A copy of used bytes in the internal buffer</returns>
        public byte[] ToArray()
        {
            byte[] copy = new byte[Length];
            Buffer.BlockCopy(m_Target, 0, copy, 0, (int)Length);
            return copy;
        }

        /// <summary>
        /// Writes zeros to fill the last byte
        /// </summary>
        public void PadBuffer()
        {
            while (!BitAligned)
            {
                WriteBit(false);
            }
        }

        /// <summary>
        /// Reads zeros until the the buffer is byte aligned
        /// </summary>
        public void SkipPadBits()
        {
            while (!BitAligned)
            {
                ReadBit();
            }
        }

        /// <summary>
        /// Returns hex encoded version of the buffer
        /// </summary>
        /// <returns>Hex encoded version of the buffer</returns>
        public override string ToString() => BitConverter.ToString(m_Target, 0, (int)Length);
    }
}