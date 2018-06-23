using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using static MLAPI.NetworkingManagerComponents.Binary.Arithmetic;

namespace MLAPI.NetworkingManagerComponents.Binary
{
    public sealed class BitStream : Stream
    {
        private readonly double growthFactor;
        private byte[] target;

        /// <summary>
        /// A stream that supports writing data smaller than a single byte. This stream also has a built-in compression algorithm that can (optionally) be used to write compressed data.
        /// </summary>
        /// <param name="capacity">Initial capacity of buffer in bytes.</param>
        /// <param name="growthFactor">Factor by which buffer should grow when necessary.</param>
        public BitStream(int capacity = 16, double growthFactor = 2.0)
        {
            target = new byte[capacity];
            this.growthFactor = growthFactor <= 1 ? 1.5 : growthFactor;
            Resizable = true;
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

        /// <summary>
        /// Whether or not data can be read from the stream.
        /// </summary>
        public override bool CanRead => BitPosition < (ulong)target.LongLength;

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
            if (!Resizable) throw new CapacityException("Can't resize fixed-capacity buffer! Capacity (bytes): "+target.Length); // Don't do shit because fuck you
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
        /// Write data from the given buffer to the internal stream buffer,
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
        /// Grow buffer if possible. According to Max(bufferLength, 1) * growthFactor^Ceil(newContent/Max(bufferLength, 1))
        /// </summary>
        /// <param name="newContent">How many new values need to be accomodated (at least).</param>
        private void Grow(long newContent) => SetCapacity(Math.Max(target.LongLength, 1) * (long)Math.Pow(growthFactor, CeilingExact(newContent, Math.Max(target.LongLength, 1))));

        /// <summary>
        /// Write a single bit to the stream
        /// </summary>
        /// <param name="bit">Value of the bit. True represents 1, False represents 0</param>
        public void WriteBit(bool bit)
        {
            if (BitAligned && Position + 1 >= target.Length) Grow(1);
            int offset = (int)(BitPosition & 7);
            ulong pos = BitPosition >> 3;
            ++BitPosition;
            target[pos] = (byte)(bit ? (target[pos] & ~(1 << offset)) | (1 << offset) : (target[pos] & ~(1 << offset)));
            UpdateLength();
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
        /// An exception representing cases when buffer-capacity related errors occur.
        /// </summary>
        public sealed class CapacityException : Exception
        {
            public CapacityException() { }
            public CapacityException(string message) : base(message) { }
            public CapacityException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}
