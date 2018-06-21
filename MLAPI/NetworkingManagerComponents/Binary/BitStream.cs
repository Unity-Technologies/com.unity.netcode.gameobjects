using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static MLAPI.NetworkingManagerComponents.Binary.Arithmetic;

namespace MLAPI.NetworkingManagerComponents.Binary
{
    public sealed class BitStream : Stream
    {
        private const long SIGN_BIT = -9223372036854775808;


        private readonly double growthFactor;
        private readonly byte[] target;

        public BitStream(byte[] target, double growthFactor = 2.0)
        {
            this.target = target;
            this.growthFactor = growthFactor <= 1 ? 1.5 : growthFactor;
        }

        public override bool CanRead => BitPosition < (ulong) target.LongLength;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => (long) (BitLength>>3);

        public override long Position { get => (long)(BitPosition>>3); set => BitPosition = (ulong)value << 3; }
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
            return (long) (
                BitPosition =
                    (
                    origin == SeekOrigin.Current ?
                        offset > 0 ?
                            Math.Min(BitPosition + ((ulong)offset << 3), (ulong)target.Length << 3) :
                            (offset ^ SIGN_BIT) > Position ?
                                0UL :
                                BitPosition - (ulong)((offset ^ SIGN_BIT) << 3) :
                    origin == SeekOrigin.Begin ?
                        (ulong)Math.Max(0, offset) << 3 :
                        (ulong)Math.Max(target.Length - offset, 0) << 3
                    ));
        }

        public override void SetLength(long value)
        {
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
        }

        private void Grow(int newContent) => SetLength(target.LongLength * (long)Math.Pow(growthFactor, CeilingExact(newContent, target.Length)));

        public void WriteBit(bool bit)
        {
            if (BitAligned && Position + 1 >= target.Length) Grow(1);
            int offset = (int)(BitPosition & 7);
            ++BitPosition;
            target[BitPosition] = (byte)(bit ? (target[BitPosition >> 3] & ~(1 << offset)) | (1 << offset) : (target[BitPosition >> 3] & ~(1 << offset)));
        }

        public void WriteSByte(sbyte value) => WriteByte((byte)value);
        public void WriteUInt16(ushort value)
        {
            WriteByte((byte)value);
            WriteByte((byte)(value >> 8));
        }
        public void WriteInt16(short value) => WriteUInt16((ushort)value);
        public void WriteUInt32(uint value)
        {
            WriteByte((byte)value);
            WriteByte((byte)(value >> 8));
            WriteByte((byte)(value >> 16));
            WriteByte((byte)(value >> 24));
        }
        public void WriteInt32(int value) => WriteUInt32((uint)value);
        public void WriteUInt64(ulong value)
        {
            WriteByte((byte)value);
            WriteByte((byte)(value >> 8));
            WriteByte((byte)(value >> 16));
            WriteByte((byte)(value >> 24));
            WriteByte((byte)(value >> 32));
            WriteByte((byte)(value >> 40));
            WriteByte((byte)(value >> 48));
            WriteByte((byte)(value >> 56));
        }
        public void WriteInt64(long value) => WriteUInt64((ulong)value);

        public void WriteInt16Packed(short value) => WriteInt64Packed(value);
        public void WriteUInt16Packed(ushort value) => WriteUInt64Packed(value);
        public void WriteInt32Packed(int value) => WriteInt64Packed(value);
        public void WriteUInt32Packed(uint value) => WriteUInt64Packed(value);
        public void WriteInt64Packed(long value) => WriteUInt64(ZigZagEncode(value));
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
                for (int i = 0; i < max; ++i) WriteULongByte(value >> (i<<3));
            }
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
                (_ReadByte() << 8)  |
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
            int hdr = (int)(header-246);
            while (hdr > ++cmp) res |= (ulong)_ReadByte() << (cmp << 3);
            return res;
        }


        private void WriteULongByteMisaligned(ulong value) => WriteMisaligned((byte)value);
        private void WriteMisaligned(byte value)
        {
            int off = (int)(BitLength % 8);
            int shift1 = 8 - off;
            target[Position + 1] = (byte)((target[Position + 1] & (0xFF >> off)) | (value >> shift1));
            target[Position] = (byte)((target[Position] & (0xFF >> shift1)) | (value << off));

            BitPosition += 8;
        }

        private void WriteULongByte(ulong byteValue) => WriteByte((byte)byteValue);
        public override void WriteByte(byte value)
        {
            if (BitAligned)
            {
                target[Position] = value;
                ++Position;
            }
            else WriteMisaligned(value);
        }
    }
}
