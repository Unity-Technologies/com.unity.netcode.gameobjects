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
        private delegate void ByteWriteFunc(byte b);
        private delegate void ULongWriteFunc(ulong l);
        private delegate byte ByteReadFunc();


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

        public override long Length => (long) (BitLength/8);

        public override long Position { get => (long)(BitPosition/8); set => BitPosition = (ulong)value * 8UL; }
        public ulong BitPosition { get; set; }
        public ulong BitLength { get; private set; }
        public bool BitAligned { get => BitPosition % 8 == 0; }

        public override void Flush() { } // NOP

        private byte ReadByteMisaligned()
        {
            int mod = (int)(BitPosition % 8);
            return (byte)((target[(int)Position++] >> mod) | (target[(int)Position] << (8 - mod)));
        }
        private byte ReadByteAligned() => target[Position++];
        public new byte ReadByte() => BitAligned ? ReadByteAligned() : ReadByteMisaligned();

        public override int Read(byte[] buffer, int offset, int count)
        {
            int tLen = Math.Min(count, (int)(target.LongLength - Position) - (BitPosition % 8 == 0 ? 0 : 1));
            for (int i = 0; i < tLen; ++i) buffer[offset + i] = ReadByte();
            return tLen;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return (long) (
                BitPosition =
                    (
                    origin == SeekOrigin.Current ?
                        offset > 0 ?
                            Math.Min(BitPosition + ((ulong)offset * 8UL), (ulong)target.Length * 8UL) :
                            (ulong)(offset * -8L) > BitPosition ?
                                0UL :
                                BitPosition - (ulong)(offset * -8L) :
                    origin == SeekOrigin.Begin ?
                        (ulong)Math.Max(0, offset) * 8UL :
                        (ulong)Math.Max(target.Length - offset, 0) * 8UL
                    ));
        }

        public override void SetLength(long value)
        {
            byte[] newTarg = new byte[value];
            long len = Math.Min(value, target.LongLength);
            for (long l = 0; l < len; ++l) newTarg[l] = target[l];
            if (value > target.LongLength) BitPosition = (ulong)value * 8UL;
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
            int offset = (int)(BitPosition % 8);
            ++BitPosition;
            target[BitPosition] = (byte)(bit ? (target[BitPosition / 8] & ~(1 << offset)) | (1 << offset) : (target[BitPosition / 8] & ~(1 << offset)));
        }

        public void WriteSByte(sbyte value) => WriteByte((byte)value);
        public void WriteUInt16(ushort value)
        {
            ByteWriteFunc Write = SelectWriter();
            Write((byte)value);
            Write((byte)(value >> 8));
        }
        public void WriteInt16(short value) => WriteUInt16((ushort)value);
        public void WriteUInt32(uint value)
        {
            ByteWriteFunc Write = SelectWriter();
            Write((byte)value);
            Write((byte)(value >> 8));
            Write((byte)(value >> 16));
            Write((byte)(value >> 24));
        }
        public void WriteInt32(int value) => WriteUInt32((uint)value);
        public void WriteUInt64(ulong value)
        {
            ByteWriteFunc Write = SelectWriter();
            Write((byte)value);
            Write((byte)(value >> 8));
            Write((byte)(value >> 16));
            Write((byte)(value >> 24));
            Write((byte)(value >> 32));
            Write((byte)(value >> 40));
            Write((byte)(value >> 48));
            Write((byte)(value >> 56));
        }
        public void WriteInt64(long value) => WriteUInt64((ulong)value);

        public void WriteInt16Packed(short value) => WriteInt64Packed(value);
        public void WriteUInt16Packed(ushort value) => WriteUInt64Packed(value);
        public void WriteInt32Packed(int value) => WriteInt64Packed(value);
        public void WriteUInt32Packed(uint value) => WriteUInt64Packed(value);
        public void WriteInt64Packed(long value) => WriteUInt64(ZigZagEncode(value));
        public void WriteUInt64Packed(ulong value)
        {
            ULongWriteFunc Write = SelectUWriter();
            if (value <= 240) Write(value);
            else if (value <= 2287)
            {
                Write((value - 240) / 256 + 241);
                Write((value - 240) % 256);
            }
            else if (value <= 67823)
            {
                Write(249);
                Write((value - 2288) / 256);
                Write((value - 2288) % 256);
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
                Write(header);
                int max = (int)(header - 247);
                for (int i = 0; i < max; ++i) Write(value >> i);
            }
        }
        public ushort ReadUInt16() => (ushort)(ReadByte() | (ReadByte() << 8));
        public short ReadInt16() => (short)ReadUInt16();
        public uint ReadUInt32() => (uint)(ReadByte() | (ReadByte() << 8) | (ReadByte() << 16) | (ReadByte() << 24));
        public int ReadInt32() => (int)ReadUInt32();
        public ulong ReadUInt64() => (ulong)(ReadByte() | (ReadByte() << 8) | (ReadByte() << 16) | (ReadByte() << 24) | (ReadByte() << 32) | (ReadByte() << 40) | (ReadByte() << 48) | (ReadByte() << 56));
        public long ReadInt64() => (long)ReadUInt64();

        public short ReadInt16Packed() => (short)ZigZagDecode(ReadUInt64Packed());
        public ushort ReadUInt16Packed() => (ushort)ReadUInt64Packed();
        public int ReadInt32Packed() => (int)ZigZagDecode(ReadUInt64Packed());
        public uint ReadUInt32Packed() => (uint)ReadUInt64Packed();
        public long ReadInt64Packed() => ZigZagDecode(ReadUInt64Packed());
        public ulong ReadUInt64Packed()
        {
            ByteReadFunc Read = SelectReader();
            ulong header = Read();
            if (header <= 240) return header;
            if (header <= 248) return 240 + 256 * (header - 241) + Read();
            if (header == 249) return 2288 + 256UL * Read() + Read();
            ulong res = Read() | ((ulong)Read() << 8) | ((ulong)Read() << 16);
            int cmp = 2;
            int hdr = (int)(header-246);
            while (hdr > ++cmp) res |= (ulong)Read() << (8 * cmp);
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

        private ULongWriteFunc SelectUWriter() => BitAligned ? WriteULongByte : (ULongWriteFunc)WriteULongByteMisaligned;
        private ByteWriteFunc SelectWriter() => BitAligned ? WriteByte : (ByteWriteFunc)WriteMisaligned;
        private ByteReadFunc SelectReader() => BitAligned ? ReadByteAligned : (ByteReadFunc)ReadByteMisaligned;

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
