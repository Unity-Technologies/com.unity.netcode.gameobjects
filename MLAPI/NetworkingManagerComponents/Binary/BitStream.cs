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
        private readonly byte[] target;


        public BitStream(int capacity = 16, double growthFactor = 2.0)
        {
            target = new byte[capacity];
            this.growthFactor = growthFactor <= 1 ? 1.5 : growthFactor;
            Resizable = true;
        }
        public BitStream(byte[] target)
        {
            this.target = target;
            Resizable = false;
        }

        public bool Resizable { get; }
        public override bool CanRead => BitPosition < (ulong)target.LongLength;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public long Capacity {
            get => target.LongLength; // Optimized CeilingExact
            set
            {
                if (value < Length) throw new ArgumentOutOfRangeException("New capcity too small!");
                SetLength(value);
            }
        }
        
        public override long Length { get => (long)(BitLength>>8); }
        public override long Position { get => (long)((BitPosition >> 3) + ((BitPosition & 1UL) | ((BitPosition >> 1) & 1UL) | ((BitPosition >> 2) & 1UL))); set => BitPosition = (ulong)value << 3; }
        public ulong BitPosition { get; set; }
        public ulong BitLength { get; private set; }
        public bool BitAligned { get => (BitPosition & 7) == 0; }

        public override void Flush() { } // NOP

        private byte ReadByteMisaligned()
        {
            int mod = (int)(BitPosition & 7);
            return (byte)((target[(int)Position++] >> mod) | (target[(int)Position] << (8 - mod)));
        }
        private byte ReadByteAligned() => target[Position++];
        private byte _ReadByte() => BitAligned ? ReadByteAligned() : ReadByteMisaligned();
        public override int ReadByte() => CanRead ? BitAligned ? ReadByteAligned() : ReadByteMisaligned() : -1;

        public override int Read(byte[] buffer, int offset, int count)
        {
            int tLen = Math.Min(count, (int)(target.LongLength - Position) - ((BitPosition & 7) == 0 ? 0 : 1));
            for (int i = 0; i < tLen; ++i) buffer[offset + i] = _ReadByte();
            return tLen;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return (long)(
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
                    ));
        }

        public override void SetLength(long value)
        {
            if (!Resizable) throw new CapacityException("Can't resize fixed-capacity buffer! Capacity (bytes): "+target.Length); // Don't do shit because fuck you
            byte[] newTarg = new byte[value];
            long len = Math.Min(value, target.LongLength);
            for (long l = 0; l < len; ++l) newTarg[l] = target[l];
            if (value > target.LongLength) BitPosition = (ulong)value << 3;
        }

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
                for (int i = 0; i < count; ++i) WriteMisaligned(buffer[offset + i]);
            }
            if (BitPosition > BitLength) BitLength = BitPosition;
        }

        private void Grow(int newContent) => SetLength(Math.Max(target.LongLength, 1) * (long)Math.Pow(growthFactor, CeilingExact(newContent, Math.Max(target.Length, 1))));

        public void WriteBit(bool bit)
        {
            if (BitAligned && Position + 1 >= target.Length) Grow(1);
            int offset = (int)(BitPosition & 7);
            ++BitPosition;
            target[BitPosition] = (byte)(bit ? (target[BitPosition >> 3] & ~(1 << offset)) | (1 << offset) : (target[BitPosition >> 3] & ~(1 << offset)));
            UpdateLength();
        }

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
        public void WriteSByte(sbyte value) => WriteByte((byte)value);
        public void WriteUInt16(ushort value)
        {
            _WriteByte((byte)value);
            _WriteByte((byte)(value >> 8));
            UpdateLength();
        }
        public void WriteInt16(short value) => WriteUInt16((ushort)value);
        public void WriteUInt32(uint value)
        {
            _WriteByte((byte)value);
            _WriteByte((byte)(value >> 8));
            _WriteByte((byte)(value >> 16));
            _WriteByte((byte)(value >> 24));
            UpdateLength();
        }
        public void WriteInt32(int value) => WriteUInt32((uint)value);
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
        public void WriteInt64(long value) => WriteUInt64((ulong)value);

        public void WriteInt16Packed(short value) => WriteInt64Packed(value);
        public void WriteUInt16Packed(ushort value) => WriteUInt64Packed(value);
        public void WriteInt32Packed(int value) => WriteInt64Packed(value);
        public void WriteUInt32Packed(uint value) => WriteUInt64Packed(value);
        public void WriteInt64Packed(long value) => WriteUInt64Packed(ZigZagEncode(value));
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
        public ushort ReadUInt16()
        {
            return (ushort)(_ReadByte() | (_ReadByte() << 8));
        }
        public short ReadInt16() => (short)ReadUInt16();
        public uint ReadUInt32()
        {
            return (uint)(_ReadByte() | (_ReadByte() << 8) | (_ReadByte() << 16) | (_ReadByte() << 24));
        }
        public int ReadInt32() => (int)ReadUInt32();
        public ulong ReadUInt64()
        {
            return (ulong)(
                _ReadByte() |
                (_ReadByte() << 8) |
                (_ReadByte() << 16) |
                (_ReadByte() << 24) |
                (_ReadByte() << 32) |
                (_ReadByte() << 40) |
                (_ReadByte() << 48) |
                (_ReadByte() << 56)
                );
        }
        public long ReadInt64() => (long)ReadUInt64();

        public short ReadInt16Packed() => (short)ZigZagDecode(ReadUInt64Packed());
        public ushort ReadUInt16Packed() => (ushort)ReadUInt64Packed();
        public int ReadInt32Packed() => (int)ZigZagDecode(ReadUInt64Packed());
        public uint ReadUInt32Packed() => (uint)ReadUInt64Packed();
        public long ReadInt64Packed() => ZigZagDecode(ReadUInt64Packed());
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
        public bool ReadBit() => (target[Position] & (1 << (int)(BitPosition++ & 7))) != 0;


        private void _WriteULongByteMisaligned(ulong value) => _WriteMisaligned((byte)value);
        private void _WriteMisaligned(byte value)
        {
            int off = (int)(BitPosition & 7);
            int shift1 = 8 - off;
            target[Position + 1] = (byte)((target[Position + 1] & (0xFF >> off)) | (value >> shift1));
            target[Position] = (byte)((target[Position] & (0xFF >> shift1)) | (value << off));

            BitPosition += 8;
        }

        private void WriteULongByteMisaligned(ulong value) => WriteMisaligned((byte)value);
        private void WriteMisaligned(byte value)
        {
            _WriteMisaligned(value);
            UpdateLength();
        }

        private void _WriteIntByte(int value) => _WriteByte((byte)value);
        private void _WriteULongByte(ulong byteValue) => _WriteByte((byte)byteValue);
        private void _WriteByte(byte value)
        {
            if (Position == target.LongLength) Grow(1);
            if (BitAligned)
            {
                target[Position] = value;
                BitPosition += 8;
            }
            else _WriteMisaligned(value);
            UpdateLength();
        }

        private void WriteIntByte(int value) => WriteByte((byte)value);
        private void WriteULongByte(ulong byteValue) => WriteByte((byte)byteValue);
        public override void WriteByte(byte value)
        {
            _WriteByte(value);
            UpdateLength();
        }
        
        // Should be inlined
        private void UpdateLength()
        {
            if (BitPosition > BitLength) BitLength = BitPosition;
        }
        public byte[] GetBuffer() => target;


        public sealed class CapacityException : Exception
        {
            public CapacityException() { }
            public CapacityException(string message) : base(message) { }
            public CapacityException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}
