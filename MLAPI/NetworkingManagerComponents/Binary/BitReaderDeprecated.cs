#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using MLAPI.Logging;

namespace MLAPI.Serialization
{
    [Obsolete]
    public class BitReaderDeprecated : IDisposable
    {
        private bool disposed;
        private delegate T Getter<T>();
        private static readonly float[] holder_f = new float[1];
        private static readonly double[] holder_d = new double[1];
        private static readonly ulong[] holder_u = new ulong[1];
        private static readonly uint[] holder_i = new uint[1];

        private byte[] readFrom;
        private long bitCount = 0;

        private static int pools = 0;
        private static readonly Queue<BitReaderDeprecated> readerPool = new Queue<BitReaderDeprecated>();

        public ulong Remaining
        {
            get
            {
                return BitLength - (ulong)bitCount;
            }
        }

        public ulong BitLength
        {
            get
            {
                return (ulong)readFrom.Length * 8UL;
            }
        }

        private BitReaderDeprecated(byte[] readFrom)
        {
            this.readFrom = readFrom;
            disposed = false;
        }

        public static BitReaderDeprecated Get(byte[] readFrom)
        {
            if (readerPool.Count == 0)
            {
                if (pools > 10)
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("There are more than 10 BitReaders. Have you forgotten do dispose? (More readers hurt performance)");
                BitReaderDeprecated reader = new BitReaderDeprecated(readFrom);
                reader.disposed = false;
                pools++;
                return reader;
            }
            else
            {
                BitReaderDeprecated reader = readerPool.Dequeue();
                reader.disposed = false;
                reader.readFrom = readFrom;
                return reader;
            }
        }

        public ValueType ReadValueType<T>()
        {
            if (typeof(T) == typeof(float))
                return ReadFloat();
            else if (typeof(T) == typeof(double))
                return ReadDouble();
            else if (typeof(T) == typeof(byte))
                return ReadByte();
            else if (typeof(T) == typeof(sbyte))
                return ReadSByte();
            else if (typeof(T) == typeof(short))
                return ReadShort();
            else if (typeof(T) == typeof(ushort))
                return ReadUShort();
            else if (typeof(T) == typeof(int))
                return ReadInt();
            else if (typeof(T) == typeof(uint))
                return ReadUInt();
            else if (typeof(T) == typeof(long))
                return ReadLong();
            else if (typeof(T) == typeof(ulong))
                return ReadULong();

            return default(ValueType);
        }

        public T ReadValueTypeOrString<T>()
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)ReadString(); //BOX
            }
            else if (typeof(T).IsValueType)
            {
                ValueType type = ReadValueType<T>();
                return (T)(object)type; //BOX
            }
            else
            {
                return default(T);
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
        public void SkipPadded()     => bitCount += (8 - (bitCount % 8)) % 8;
        public ushort ReadUShort()   => (ushort)ReadULong();
        public uint ReadUInt()       => (uint)ReadULong();
        public sbyte ReadSByte()     => (sbyte)ZigZagDecode(ReadULong());
        public short ReadShort()     => (short)ZigZagDecode(ReadULong());
        public int ReadInt()         => (int)ZigZagDecode(ReadULong());
        public long ReadLong()       => ZigZagDecode(ReadULong());

        public float[] ReadFloatArray(int known = -1)                   => ReadArray(ReadFloat, known);
        public uint ReadFloatArray(float[] buffer, int known = -1)      => ReadArray(ReadFloat, buffer, known);
        public double[] ReadDoubleArray(int known = -1)                 => ReadArray(ReadDouble, known);
        public uint ReadDoubleArray(double[] buffer, int known = -1)    => ReadArray(ReadDouble, buffer, known);
        public byte[] ReadByteArray(int known = -1)                     => ReadArray(ReadByte, known);
        public uint ReadByteArray(byte[] buffer, int known = -1)        => ReadArray(ReadByte, buffer, known);
        public ushort[] ReadUShortArray(int known = -1)                 => ReadArray(ReadUShort, known);
        public uint ReadUShortArray(ushort[] buffer, int known = -1)    => ReadArray(ReadUShort, buffer, known);
        public uint[] ReadUIntArray(int known = -1)                     => ReadArray(ReadUInt, known);
        public uint ReadUIntArray(uint[] buffer, int known = -1)        => ReadArray(ReadUInt, buffer, known);
        public ulong[] ReadULongArray(int known = -1)                   => ReadArray(ReadULong, known);
        public uint ReadULongArray(ulong[] buffer, int known = -1)      => ReadArray(ReadULong, buffer, known);
        public sbyte[] ReadSByteArray(int known = -1)                   => ReadArray(ReadSByte, known);
        public uint ReadSByteArray(sbyte[] buffer, int known = -1)      => ReadArray(ReadSByte, buffer, known);
        public short[] ReadShortArray(int known = -1)                   => ReadArray(ReadShort, known);
        public uint ReadShortArray(short[] buffer, int known = -1)      => ReadArray(ReadShort, buffer, known);
        public int[] ReadIntArray(int known = -1)                       => ReadArray(ReadInt, known);
        public uint ReadIntArray(int[] buffer, int known = -1)          => ReadArray(ReadInt, buffer, known);
        public long[] ReadLongArray(int known = -1)                     => ReadArray(ReadLong, known);
        public uint ReadLongArray(long[] buffer, int known = -1)        => ReadArray(ReadLong, buffer, known);
        public string ReadString()                                      => Encoding.UTF8.GetString(ReadByteArray());
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

        private uint ReadArray<T>(Getter<T> g, T[] buffer, int knownSize = -1)
        {
            uint size = knownSize > 0 ? (uint)knownSize : ReadUInt();
            /*
            if (buffer.Length < size)
                throw new ArgumentException("Buffer size is too small");
                */
            for (ushort s = 0; s < size; ++s)
            {
                if (s > buffer.Length)
                    break; //The buffer is too small. We still give the correct size so it can be re-read
                buffer[s] = g();   
            }
            return size;
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
        private static long ZigZagDecode(ulong d) => (long)(((d & 1) << 63) | (d >> 1));

        public void Dispose()
        {
            readFrom = null; //Give to GC
            bitCount = 0;
            if (disposed) return; //this is already in the pool
            disposed = true;
            readerPool.Enqueue(this);
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
