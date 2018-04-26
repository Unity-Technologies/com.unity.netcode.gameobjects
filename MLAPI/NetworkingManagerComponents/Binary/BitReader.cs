#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using MLAPI.NetworkingManagerComponents.Core;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace MLAPI.NetworkingManagerComponents.Binary
{
    public class BitReader : IDisposable
    {
        private delegate T Getter<T>();
        private static readonly float[] holder_f = new float[1];
        private static readonly double[] holder_d = new double[1];
        private static readonly ulong[] holder_u = new ulong[1];
        private static readonly uint[] holder_i = new uint[1];

        private byte[] readFrom;
        private long bitCount = 0;

        private static int pools = 0;
        private static readonly Queue<BitReader> readerPool = new Queue<BitReader>();

        private BitReader(byte[] readFrom)
        {
            this.readFrom = readFrom;
        }

        public static BitReader Get(byte[] readFrom)
        {
            if (readerPool.Count == 0)
            {
                if (pools > 10)
                    LogHelper.LogWarning("MLAPI: There are more than 10 BitReaders. Have you forgotten do dispose? (More readers hurt performance)", LogLevel.Normal);
                BitReader reader = new BitReader(readFrom);
                pools++;
                return reader;
            }
            else
            {
                BitReader reader = readerPool.Dequeue();
                reader.readFrom = readFrom;
                return reader;
            }
        }

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
        public void SkipPadded() => bitCount += (8 - (bitCount % 8)) % 8;
        public ushort ReadUShort() => (ushort)ReadULong();
        public uint ReadUInt() => (uint)ReadULong();
        public sbyte ReadSByte() => (sbyte)ZigZagDecode(ReadByte(), 1);
        public short ReadShort() => (short)ZigZagDecode(ReadUShort(), 2);
        public int ReadInt() => (int)ZigZagDecode(ReadUInt(), 4);
        public long ReadLong() => ZigZagDecode(ReadULong(), 8);
        public float[] ReadFloatArray(int known = -1) => ReadArray(ReadFloat, known);
        public double[] ReadDoubleArray(int known = -1) => ReadArray(ReadDouble, known);
        public byte[] ReadByteArray(int known = -1) => ReadArray(ReadByte, known);
        public ushort[] ReadUShortArray(int known = -1) => ReadArray(ReadUShort, known);
        public uint[] ReadUIntArray(int known = -1) => ReadArray(ReadUInt, known);
        public ulong[] ReadULongArray(int known = -1) => ReadArray(ReadULong, known);
        public sbyte[] ReadSByteArray(int known = -1) => ReadArray(ReadSByte, known);
        public short[] ReadShortArray(int known = -1) => ReadArray(ReadShort, known);
        public int[] ReadIntArray(int known = -1) => ReadArray(ReadInt, known);
        public long[] ReadLongArray(int known = -1) => ReadArray(ReadLong, known);
        public string ReadString() => Encoding.UTF8.GetString(ReadByteArray());
        public byte ReadBits(int bits)
        {
            byte b = 0;
            for (int i = 0; --bits >= 0; ++i)
                b |= (byte)((ReadBool() ? 1 : 0) << i);
            return b;
        }

        public ulong ReadULong()
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
        private T[] ReadArray<T>(Getter<T> g, int knownSize = -1)
        {
            T[] result = new T[knownSize > 0 ? (uint)knownSize : ReadUInt()];
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
                    if (size == 4) result_holder.SetValue(BinaryHelpers.SwapEndian(ReadUInt()), 0);
                    else result_holder.SetValue(BinaryHelpers.SwapEndian(ReadULong()), 0);
                    Buffer.BlockCopy(result_holder, 0, type_holder, 0, size);
                    result = (T)type_holder.GetValue(0);
                }
            return result;
        }
        private static long ZigZagDecode(ulong d, int bytes) => (long)(((d << (bytes * 8 - 1)) & 1) | (d >> 1));

        public void Dispose()
        {
            bitCount = 0;
            readerPool.Enqueue(this);
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
