#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MLAPI.Logging;

namespace MLAPI.Serialization
{
    [Obsolete]
    public sealed class BitWriterDeprecated : IDisposable
    {
        private struct Partial
        {
            public byte value;
            public byte count;
            public static readonly FieldInfo value_info = typeof(Partial).GetField("value");
            public static readonly FieldInfo count_info = typeof(Partial).GetField("count");

            public Partial(byte value, byte count)
            {
                this.value = value;
                this.count = count;
            }
        }

        private static readonly Queue<BitWriterDeprecated> writerPool = new Queue<BitWriterDeprecated>();
        
        private static readonly float[] holder_f = new float[1];
        private static readonly double[] holder_d = new double[1];
        private static readonly ulong[] holder_u = new ulong[1];
        private static readonly uint[] holder_i = new uint[1];
        private static readonly List<Type> supportedTypes = new List<Type>()
        {
            typeof(bool),
            typeof(byte),
            typeof(sbyte),
            typeof(char),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(Partial)
        };

        private static readonly FieldInfo
            dec_lo,
            dec_mid,
            dec_hi, 
            dec_flags;

        static BitWriterDeprecated()
        {
            dec_lo = typeof(decimal).GetField("lo", BindingFlags.NonPublic);
            dec_mid = typeof(decimal).GetField("mid", BindingFlags.NonPublic);
            dec_hi = typeof(decimal).GetField("hi", BindingFlags.NonPublic);
            dec_flags = typeof(decimal).GetField("flags", BindingFlags.NonPublic);

            for (int i = 0; i < 10; i++)
            {
                writerPool.Enqueue(new BitWriterDeprecated());
            }
        }

        private readonly List<object> collect = new List<object>();
        private bool outsidePool = false;

        /// <summary>
        /// Allocates a new binary collector. This is only used when there are no more writers in the pool
        /// </summary>
        private BitWriterDeprecated()
        {

        }

        /// <summary>
        /// Returns a BitWriter from the pool
        /// </summary>
        /// <returns></returns>
        public static BitWriterDeprecated Get()
        {
            if (writerPool.Count == 0)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("There are more than 10 BitWriters. Have you forgotten do dispose? (It will still work with worse performance)");
                return new BitWriterDeprecated() { outsidePool = true };
            }
            else
                return writerPool.Dequeue();
        }

        public void Push<T>(T b)
        {
            if (b == null) collect.Add(b);
            else if (b is string || b.GetType().IsArray || IsSupportedType(b.GetType()))
                collect.Add(b is string ? Encoding.UTF8.GetBytes(b as string) : b as object);
            else
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The type \"" + b.GetType() + "\" is not supported by the Binary Serializer. It will be ignored");
        }

        // Just use Push() and PushArray()
        /*
        public void WriteGeneric<T>(T t)
        {
            if (t is bool) WriteBool((t as bool?).Value);
            else if (t is byte) WriteByte((t as byte?).Value);
            else if (t is sbyte) WriteSByte((t as sbyte?).Value);
            else if (t is ushort) WriteUShort((t as ushort?).Value);
            else if (t is short) WriteShort((t as short?).Value);
            else if (t is uint) WriteUInt((t as uint?).Value);
            else if (t is int) WriteInt((t as int?).Value);
            else if (t is ulong) WriteULong((t as ulong?).Value);
            else if (t is long) WriteLong((t as long?).Value);
            else if (t is float) WriteFloat((t as float?).Value);
            else if (t is double) WriteDouble((t as double?).Value);
            //else if (t is bool[]) WriteBoolArray(t as bool[]);
            else if (t is byte[]) WriteByteArray(t as byte[]);
            else if (t is sbyte[]) WriteSByteArray(t as sbyte[]);
            else if (t is ushort[]) WriteUShortArray(t as ushort[]);
            else if (t is short[]) WriteShortArray(t as short[]);
            else if (t is uint[]) WriteUIntArray(t as uint[]);
            else if (t is int[]) WriteIntArray(t as int[]);
            else if (t is ulong[]) WriteULongArray(t as ulong[]);
            else if (t is long[]) WriteLongArray(t as long[]);
            else if (t is float[]) WriteFloatArray(t as float[]);
            else if (t is double[]) WriteDoubleArray(t as double[]);
            else if (t is string) WriteString(t as string);
            else if (t is BitWriter) WriteWriter(t as BitWriter);
        }
        */

        private void PushPreZigZag<T>(T b)
        {
            if (b is sbyte) Push(ZigZagEncode((sbyte)(object)b)); //BOX
            if (b is ushort) Push(ZigZagEncode((ushort)(object)b)); //BOX
            if (b is uint) Push(ZigZagEncode((uint)(object)b)); //BOX
            if (b is ulong) Push(ZigZagEncode((long)(ulong)(object)b)); //BOX
            else Push(b);
        }

        public void WriteValueTypeOrString<T>(T t) => PushPreZigZag(t);
        public void WriteBool(bool b)               => Push(b);
        public void WriteFloat(float f)             => Push(f);
        public void WriteDouble(double d)           => Push(d);
        public void WriteByte(byte b)               => Push(b);
        public void WriteUShort(ushort s)           => Push(s);
        public void WriteUInt(uint i)               => Push(i);
        public void WriteULong(ulong l)             => Push(l);
        public void WriteSByte(sbyte b)             => Push(ZigZagEncode(b));
        public void WriteShort(short s)             => Push(ZigZagEncode(s));
        public void WriteInt(int i)                 => Push(ZigZagEncode(i));
        public void WriteLong(long l)               => Push(ZigZagEncode(l));
        public void WriteString(string s)           => Push(s);
        public void WriteAlignBits()                => Push<object>(null);
        public void WriteFloatArray(float[] f, bool known = false)                                  => PushArray(f, known);
        public void WriteFloatArray(float[] f, int startIndex, int length, bool known = false)      => PushArray(f, startIndex, length, known);
        public void WriteDoubleArray(double[] d, bool known = false)                                => PushArray(d, known);
        public void WriteDoubleArray(double[] d, int startIndex, int length, bool known = false)    => PushArray(d, startIndex, length, known);
        public void WriteByteArray(byte[] b, bool known = false)                                    => PushArray(b, known);
        public void WriteByteArray(byte[] b, int startIndex, int length, bool known = false)        => PushArray(b, startIndex, length, known);
        public void WriteUShortArray(ushort[] s, bool known = false)                                => PushArray(s, known);
        public void WriteUShortArray(ushort[] s, int startIndex, int length, bool known = false)    => PushArray(s, startIndex, length, known);
        public void WriteUIntArray(uint[] i, bool known = false)                                    => PushArray(i, known);
        public void WriteUIntArray(uint[] i, int startIndex, int length, bool known = false)        => PushArray(i, startIndex, length, known);
        public void WriteULongArray(ulong[] l, bool known = false)                                  => PushArray(l, known);
        public void WriteULongArray(ulong[] l, int startIndex, int length, bool known = false)      => PushArray(l, startIndex, length, known);
        public void WriteSByteArray(sbyte[] b, bool known = false)                                  => PushArray(b, known);
        public void WriteSByteArray(sbyte[] b, int startIndex, int length, bool known = false)      => PushArray(b, startIndex, length, known);
        public void WriteShortArray(short[] s, bool known = false)                                  => PushArray(s, known);
        public void WriteShortArray(short[] s, int startIndex, int length, bool known = false)      => PushArray(s, startIndex, length, known);
        public void WriteIntArray(int[] i, bool known = false)                                      => PushArray(i, known);
        public void WriteIntArray(int[] i, int startIndex, int length, bool known = false)          => PushArray(i, startIndex, length, known);
        public void WriteLongArray(long[] l, bool known = false)                                    => PushArray(l, known);
        public void WriteLongArray(long[] l, int startIndex, int length, bool known = false)        => PushArray(l, startIndex, length, known);
        public void WriteBits(byte value, int bits) => Push(new Partial(ReadNBits(value, 0, bits % 8), (byte)(bits%8))); // Suggestion: store (bits % 8) result?
        public void WriteWriter(BitWriterDeprecated writer)
        {
            for (int i = 0; i < writer.collect.Count; i++)
            {
                Push(writer.collect[i]);
            }
        }

        public void PushArray<T>(T[] t, bool knownSize = false)
        {
            if (!knownSize) Push((uint)t.Length);
            bool signed = IsSigned(t.GetType().GetElementType());
            //int size = Marshal.SizeOf(t.GetType().GetElementType());
            foreach (T t1 in t) Push(signed ? (object)ZigZagEncode(t1 as long? ?? t1 as int? ?? t1 as short? ?? t1 as sbyte? ?? 0) : (object)t1);
        }

        public void PushArray<T>(T[] t, int startIndex, int length, bool knownSize = false)
        {
            if (!knownSize) Push((uint)t.Length);
            bool signed = IsSigned(t.GetType().GetElementType());
            //int size = Marshal.SizeOf(t.GetType().GetElementType());
            for (int i = startIndex; i < length; i++) Push(signed ? (object)ZigZagEncode(t[i] as long? ?? t[i] as int? ?? t[i] as short? ?? t[i] as sbyte? ?? 0) : (object)t[i]);
        }

        /// <summary>
        /// Serializes data, allocates an array and returns it
        /// </summary>
        /// <returns>Allocated array with written data</returns>
        public byte[] Finalize()
        {
            long bitCount = 0;
            for (int i = 0; i < collect.Count; ++i) bitCount += collect[i] == null ? (8 - (bitCount % 8)) % 8 : GetBitCount(collect[i]);
            byte[] buffer = new byte[((bitCount / 8) + (bitCount % 8 == 0 ? 0 : 1))];

            long bitOffset = 0;
            bool isAligned = true;
            foreach (var item in collect)
                if (item == null)
                {
                    bitOffset += (8 - (bitOffset % 8)) % 8;
                    isAligned = true;
                }
                else Serialize(item, buffer, ref bitOffset, ref isAligned);

            return buffer;
        }

        //The ref is not needed. It's purley there to indicate that it's treated as a reference inside the method.
        /// <summary>
        /// Writes data to the given buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns>The amount of bytes written</returns>
        public long Finalize(ref byte[] buffer)
        {
            if(buffer == null)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("no buffer provided");
                return 0;
            }
            long bitCount = 0;
            for (int i = 0; i < collect.Count; ++i) bitCount += collect[i] == null ? (8 - (bitCount % 8)) % 8 : GetBitCount(collect[i]);

            if (buffer.Length < ((bitCount / 8) + (bitCount % 8 == 0 ? 0 : 1)))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The buffer size is not large enough");
                return 0;
            }
            long bitOffset = 0;
            bool isAligned = true;
            foreach (var item in collect)
                if (item == null)
                {
                    bitOffset += (8 - (bitOffset % 8)) % 8;
                    isAligned = true;
                }
                else Serialize(item, buffer, ref bitOffset, ref isAligned);

            return (bitCount / 8) + (bitCount % 8 == 0 ? 0 : 1);
        }

        /// <summary>
        /// Gets the size in bytes if you were to serialize now
        /// </summary>
        /// <returns>The size in bytes</returns>
        public long GetFinalizeSize()
        {
            long bitCount = 0;
            for (int i = 0; i < collect.Count; ++i) bitCount += collect[i] == null ? (8 - (bitCount % 8)) % 8 : GetBitCount(collect[i]);
            return ((bitCount / 8) + (bitCount % 8 == 0 ? 0 : 1));
        }

        private static void Serialize<T>(T t, byte[] writeTo, ref long bitOffset, ref bool isAligned)
        {
            Type type = t.GetType();
            bool size = false;
            if (type.IsArray)
            {
                var array = t as Array;
                Serialize((uint)array.Length, writeTo, ref bitOffset, ref isAligned);
                foreach (var element in array)
                    Serialize(element, writeTo, ref bitOffset, ref isAligned);
            }
            else if (type == typeof(Partial))
            {
                byte count;
                WriteByte(writeTo, (byte)Partial.value_info.GetValue(t), bitOffset, isAligned, count = (byte)Partial.count_info.GetValue(t));
                bitOffset += count;
                isAligned = bitOffset % 8 == 0;
                return;
            }
            else if (IsSupportedType(type))
            {
                long offset = t is bool ? 1 : BytesToRead(t) * 8;
                if (type == typeof(bool))
                {
                    WriteBit(writeTo, t as bool? ?? false, bitOffset);
                    bitOffset += offset;
                    isAligned = bitOffset % 8 == 0;
                }
                else if (type == typeof(decimal))
                {
                    WriteDynamic(writeTo, (int)dec_lo.GetValue(t), 4, bitOffset, isAligned);
                    WriteDynamic(writeTo, (int)dec_mid.GetValue(t), 4, bitOffset + 32, isAligned);
                    WriteDynamic(writeTo, (int)dec_hi.GetValue(t), 4, bitOffset + 64, isAligned);
                    WriteDynamic(writeTo, (int)dec_flags.GetValue(t), 4, bitOffset + 96, isAligned);
                    bitOffset += offset;
                }
                else if ((size = type == typeof(float)) || type == typeof(double))
                {
                    int bytes = size ? 4 : 8;
                    Array type_holder = size ? holder_f as Array : holder_d as Array; // Fetch the preallocated array
                    Array result_holder = size ? holder_i as Array : holder_u as Array;
                    lock (result_holder)
                        lock (type_holder)
                        {
                            // Clear artifacts
                            if (size) result_holder.SetValue(0U, 0);
                            else result_holder.SetValue(0UL, 0);
                            type_holder.SetValue(t, 0); // Insert the value to convert into the preallocated holder array
                            Buffer.BlockCopy(type_holder, 0, result_holder, 0, bytes); // Perform an internal copy to the byte-based holder

                            // Since floating point flag bits are seemingly the highest bytes of the floating point values
                            // and even very small values have them, we swap the endianness in the hopes of reducing the size
                            if (size) Serialize(BinaryHelpers.SwapEndian((uint)result_holder.GetValue(0)), writeTo, ref bitOffset, ref isAligned);
                            else Serialize(BinaryHelpers.SwapEndian((ulong)result_holder.GetValue(0)), writeTo, ref bitOffset, ref isAligned);
                        }
                }
                else
                {
                    ulong value;
                    if (t is byte)
                    {
                        WriteByte(writeTo, t as byte? ?? 0, bitOffset, isAligned);
                        bitOffset += 8;
                        return;
                    }
                    else if (t is ushort) value = t as ushort? ?? 0;
                    else if (t is uint) value = t as uint? ?? 0;
                    else value = t as ulong? ?? 0;

                    if (value <= 240) WriteByte(writeTo, (byte)value, bitOffset, isAligned);
                    else if (value <= 2287)
                    {
                        WriteByte(writeTo, (value - 240) / 256 + 241, bitOffset, isAligned);
                        WriteByte(writeTo, (value - 240) % 256, bitOffset + 8, isAligned);
                    }
                    else if (value <= 67823)
                    {
                        WriteByte(writeTo, 249, bitOffset, isAligned);
                        WriteByte(writeTo, (value - 2288) / 256, bitOffset + 8, isAligned);
                        WriteByte(writeTo, (value - 2288) % 256, bitOffset + 16, isAligned);
                    }
                    else
                    {
                        WriteByte(writeTo, value & 255, bitOffset + 8, isAligned);
                        WriteByte(writeTo, (value >> 8) & 255, bitOffset + 16, isAligned);
                        WriteByte(writeTo, (value >> 16) & 255, bitOffset + 24, isAligned);
                        if (value > 16777215)
                        {
                            WriteByte(writeTo, (value >> 24) & 255, bitOffset + 32, isAligned);
                            if (value > 4294967295)
                            {
                                WriteByte(writeTo, (value >> 32) & 255, bitOffset + 40, isAligned);
                                if (value > 1099511627775)
                                {
                                    WriteByte(writeTo, (value >> 40) & 255, bitOffset + 48, isAligned);
                                    if (value > 281474976710655)
                                    {
                                        WriteByte(writeTo, (value >> 48) & 255, bitOffset + 56, isAligned);
                                        if (value > 72057594037927935)
                                        {
                                            WriteByte(writeTo, 255, bitOffset, isAligned);
                                            WriteByte(writeTo, (value >> 56) & 255, bitOffset + 64, isAligned);
                                        }
                                        else WriteByte(writeTo, 254, bitOffset, isAligned);
                                    }
                                    else WriteByte(writeTo, 253, bitOffset, isAligned);
                                }
                                else WriteByte(writeTo, 252, bitOffset, isAligned);
                            }
                            else WriteByte(writeTo, 251, bitOffset, isAligned);
                        }
                        else WriteByte(writeTo, 250, bitOffset, isAligned);
                    }
                    bitOffset += BytesToRead(value) * 8;
                }
            }
        }

        private static byte Read7BitRange(byte higher, byte lower, int bottomBits) => (byte)((higher << bottomBits) & (lower & (0xFF << (8-bottomBits))));
        private static byte ReadNBits(byte from, int offset, int count) => (byte)(from & ((0xFF >> (8-count)) << offset));

        private static bool IsSigned(Type t) => t == typeof(sbyte) || t == typeof(short) || t == typeof(int) || t == typeof(long);

        private static Type GetUnsignedType(Type t) =>
            t == typeof(sbyte) ? typeof(byte) :
            t == typeof(short) ? typeof(ushort) :
            t == typeof(int) ? typeof(uint) :
            t == typeof(long) ? typeof(ulong) :
            null;
        
        public static ulong ZigZagEncode(long d) => (ulong)(((d >> 63) & 1) | (d << 1));

        public static long GetBitCount<T>(T t)
        {
            Type type = t.GetType();
            long count = 0;
            if (type.IsArray)
            {
                Type elementType = type.GetElementType();

                count += BytesToRead((t as Array).Length) * 8; // Int16 array size. Arrays shouldn't be syncing more than 65k elements

                if (elementType == typeof(bool)) count += (t as Array).Length;
                else
                    foreach (var element in t as Array)
                        count += GetBitCount(element);
            }
            else if (type == typeof(Partial)) return (byte)Partial.count_info.GetValue(t);
            else if (IsSupportedType(type))
            {
                long ba = t is bool ? 1 : BytesToRead(t)*8;
                if (ba == 0) count += Encoding.UTF8.GetByteCount(t as string);
                else if (t is bool || t is decimal) count += ba;
                else count += BytesToRead(t) * 8;
            }
            return count;
        }

        private static void WriteBit(byte[] b, bool bit, long index)
            => b[index / 8] = (byte)((b[index / 8] & ~(1 << (int)(index % 8))) | (bit ? 1 << (int)(index % 8) : 0));
        private static void WriteByte(byte[] b, ulong value, long index, bool isAligned) => WriteByte(b, (byte)value, index, isAligned);
        private static void WriteByte(byte[] b, byte value, long index, bool isAligned, byte bits = 8)
        {
            if (isAligned) b[index / 8] = value;
            else
            {
                int byteIndex = (int)(index / 8);
                int shift = (int)(index % 8);
                byte upper_mask = (byte)(0xFF << shift);

                b[byteIndex] = (byte)((b[byteIndex] & (byte)~upper_mask) | (value << shift));
                if((8-shift)<bits) b[byteIndex + 1] = (byte)((b[byteIndex + 1] & upper_mask) | (value >> (8 - shift)));
            }
        }
        private static void WriteDynamic(byte[] b, int value, int byteCount, long index, bool isAligned)
        {
            for (int i = 0; i < byteCount; ++i)
                WriteByte(b, (byte)((value >> (8 * i)) & 0xFF), index + (8 * i), isAligned);
        }

        private static int BytesToRead(object i)
        {
            if (i is byte) return 1;
            bool size;
            ulong integer;
            if (i is decimal) return BytesToRead((int)dec_flags.GetValue(i)) + BytesToRead((int)dec_lo.GetValue(i)) + BytesToRead((int)dec_mid.GetValue(i)) + BytesToRead((int)dec_hi.GetValue(i));
            if ((size = i is float) || i is double)
            {
                int bytes = size ? 4 : 8;
                Array type_holder = size ? holder_f as Array : holder_d as Array; // Fetch the preallocated array
                Array result_holder = size ? holder_i as Array : holder_u as Array;
                lock (result_holder)
                    lock (type_holder)
                    {
                        // Clear artifacts
                        if (size) result_holder.SetValue(0U, 0);
                        else result_holder.SetValue(0UL, 0);

                        type_holder.SetValue(i, 0); // Insert the value to convert into the preallocated holder array
                        Buffer.BlockCopy(type_holder, 0, result_holder, 0, bytes); // Perform an internal copy to the byte-based holder
                        if(size) integer = BinaryHelpers.SwapEndian((uint)result_holder.GetValue(0));
                        else integer = BinaryHelpers.SwapEndian((ulong)result_holder.GetValue(0));
                    }
            }
            else integer = i as ulong? ?? i as uint? ?? i as ushort? ?? i as byte? ?? 0;
            return
                integer <= 240 ? 1 :
                integer <= 2287 ? 2 :
                integer <= 67823 ? 3 :
                integer <= 16777215 ? 4 :
                integer <= 4294967295 ? 5 :
                integer <= 1099511627775 ? 6 :
                integer <= 281474976710655 ? 7 :
                integer <= 72057594037927935 ? 8 :
                9;
        }

        // Supported datatypes for serialization
        private static bool IsSupportedType(Type t) => supportedTypes.Contains(t);
        
        public void Dispose()
        {
            if (!outsidePool)
            {
                collect.Clear();
                writerPool.Enqueue(this);
            }
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
