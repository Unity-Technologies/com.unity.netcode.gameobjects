using MLAPI.NetworkingManagerComponents.Binary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Tofvesson.Common
{
    public class BinaryDistributor
    {
        private static readonly float[] holder_f = new float[1];
        private static readonly double[] holder_d = new double[1];
        private static readonly ulong[] holder_u = new ulong[1];
        private static readonly uint[] holder_i = new uint[1];

        private readonly byte[] readFrom;
        private long bitCount = 0;
        public BinaryDistributor(byte[] readFrom) => this.readFrom = readFrom;

        public bool ReadBit()
        {
            bool result = (readFrom[bitCount / 8] & (byte)(1 << (int)(bitCount % 8))) != 0;
            ++bitCount;
            return result;
        }

        public byte ReadByte()
        {
            int shift = (int)(bitCount % 8);
            int index = (int)(bitCount / 8);
            byte lower_mask = (byte)(0xFF << shift);
            byte upper_mask = (byte)~lower_mask;
            byte result = (byte)(((readFrom[index] & lower_mask) >> shift) | (shift == 0 ? 0 : (readFrom[index + 1] & upper_mask) << (8 - shift)));
            bitCount += 8;
            return result;
        }

        public float ReadFloat() => ReadFloating<float>();
        public double ReadDouble() => ReadFloating<double>();
        public float[] ReadFloatArray() => ReadFloatingArray<float>();
        public double[] ReadDoubleArray() => ReadFloatingArray<double>();
        public ushort ReadUShort() => ReadUnsigned<ushort>();
        public uint ReadUInt() => ReadUnsigned<uint>();
        public ulong ReadULong() => ReadUnsigned<ulong>();
        public sbyte ReadSByte() => (sbyte)ZigZagDecode(ReadByte(), 1);
        public short ReadShort() => (short)ZigZagDecode(ReadUShort(), 2);
        public int ReadInt() => (int)ZigZagDecode(ReadUInt(), 4);
        public long ReadLong() => (long)ZigZagDecode(ReadULong(), 8);

        private T ReadUnsigned<T>()
        {
            dynamic header = ReadByte();
            if (header <= 240) return (T) header;
            if (header <= 248) return (T) (240 + 256 * (header - 241) + ReadByte());
            if (header == 249) return (T) (header = 2288 + 256 * ReadByte() + ReadByte());
            dynamic res = ReadByte() | ((long)ReadByte() << 8) | ((long)ReadByte() << 16);
            if(header > 250)
            {
                res |= (long) ReadByte() << 24;
                if(header > 251)
                {
                    res |= (long)ReadByte() << 32;
                    if(header > 252)
                    {
                        res |= (long)ReadByte() << 40;
                        if (header > 253)
                        {
                            res |= (long)ReadByte() << 48;
                            if (header > 254) res |= (long)ReadByte() << 56;
                        }
                    }
                }
            }
            return (T) res;
        }
        private T[] ReadFloatingArray<T>()
        {
            ushort size = ReadUShort();
            T[] result = new T[size];
            for (short s = 0; s < size; ++s)
                result[s] = ReadFloating<T>();
            return result;
        }

        private T ReadFloating<T>()
        {
            int size = Marshal.SizeOf(typeof(T));
            Array type_holder = size == 4 ? holder_f as Array : holder_d as Array;
            Array result_holder = size == 4 ? holder_i as Array : holder_u as Array;
            T result;
            lock(result_holder)
                lock (type_holder)
                {
                    //for (int i = 0; i < size; ++i)
                    //    holder.SetValue(ReadByte(), i);
                    if (size == 4) result_holder.SetValue(BinaryHelpers.SwapEndian(ReadUInt()), 0);
                    else result_holder.SetValue(BinaryHelpers.SwapEndian(ReadULong()), 0);
                    Buffer.BlockCopy(result_holder, 0, type_holder, 0, size);
                    result = (T)type_holder.GetValue(0);
                }
            return result;
        }
        private static long ZigZagDecode(ulong d, int bytes) => (long)(((d << (bytes * 8 - 1)) & 1) | (d >> 1));
    }
}
