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
        private delegate T Getter<T>();
        private static readonly float[] holder_f = new float[1];
        private static readonly double[] holder_d = new double[1];
        private static readonly ulong[] holder_u = new ulong[1];
        private static readonly uint[] holder_i = new uint[1];

        private readonly byte[] readFrom;
        private long bitCount = 0;
        public BinaryDistributor(byte[] readFrom) => this.readFrom = readFrom;

        public bool ReadBool()
        {
            bool result = (readFrom[bitCount / 8] & (byte)(1 << (int)(bitCount % 8))) != 0;
            ++bitCount;
            return result;
        }

        public float ReadFloat() => ReadFloating<float>();
        public double ReadDouble() => ReadFloating<double>();
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
        public ushort ReadUShort() => (ushort)ReadULong();
        public uint ReadUInt() => (uint)ReadULong();
        public sbyte ReadSByte() => (sbyte)ZigZagDecode(ReadByte(), 1);
        public short ReadShort() => (short)ZigZagDecode(ReadUShort(), 2);
        public int ReadInt() => (int)ZigZagDecode(ReadUInt(), 4);
        public long ReadLong() => ZigZagDecode(ReadULong(), 8);
        public float[] ReadFloatArray() => ReadArray(ReadFloat);
        public double[] ReadDoubleArray() => ReadArray(ReadDouble);
        public byte[] ReadByteArray() => ReadArray(ReadByte);
        public ushort[] ReadUShortArray() => ReadArray(ReadUShort);
        public uint[] ReadUIntArray() => ReadArray(ReadUInt);
        public ulong[] ReadULongArray() => ReadArray(ReadULong);
        public sbyte[] ReadSByteArray() => ReadArray(ReadSByte);
        public short[] ReadShortArray() => ReadArray(ReadShort);
        public int[] ReadIntArray() => ReadArray(ReadInt);
        public long[] ReadLongArray() => ReadArray(ReadLong);
        public string ReadString() => Encoding.UTF8.GetString(ReadByteArray());

        private ulong ReadULong()
        {
            ulong header = ReadByte();
            if (header <= 240) return header;
            if (header <= 248) return 240 + 256 * (header - 241) + ReadByte();
            if (header == 249) return 2288 + 256UL * ReadByte() + ReadByte();
            ulong res = ReadByte() | ((ulong)ReadByte() << 8) | ((ulong)ReadByte() << 16);
            if(header > 250)
            {
                res |= (ulong) ReadByte() << 24;
                if(header > 251)
                {
                    res |= (ulong)ReadByte() << 32;
                    if(header > 252)
                    {
                        res |= (ulong)ReadByte() << 40;
                        if (header > 253)
                        {
                            res |= (ulong)ReadByte() << 48;
                            if (header > 254) res |= (ulong)ReadByte() << 56;
                        }
                    }
                }
            }
            return res;
        }
        private T[] ReadArray<T>(Getter<T> g)
        {
            T[] result = new T[ReadUShort()];
            for (ushort s = 0; s < result.Length; ++s)
                result[s] = g();
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
