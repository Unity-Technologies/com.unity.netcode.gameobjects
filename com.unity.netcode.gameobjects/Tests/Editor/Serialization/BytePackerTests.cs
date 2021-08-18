using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Multiplayer.Netcode;

namespace Unity.Netcode.EditorTests
{
    public class BytePackerTests
    {
        private void CheckUnsignedPackedSize64(ref FastBufferWriter writer, ulong value)
        {
               
            if (value <= 240)
            {
                Assert.AreEqual(1, writer.Position);
            }
            else if (value <= 2287)
            {
                Assert.AreEqual(2, writer.Position);
            }
            else
            {
                Assert.AreEqual(BitCounter.GetUsedByteCount(value) + 1, writer.Position);
            }
        }

        private void CheckUnsignedPackedValue64(ref FastBufferWriter writer, ulong value)
        {
            var reader = new FastBufferReader(writer.GetNativeArray());
            ByteUnpacker.ReadValuePacked(ref reader, out ulong readValue);
            Assert.AreEqual(readValue, value);
        }
        
        private void CheckUnsignedPackedSize32(ref FastBufferWriter writer, uint value)
        {
               
            if (value <= 240)
            {
                Assert.AreEqual(1, writer.Position);
            }
            else if (value <= 2287)
            {
                Assert.AreEqual(2, writer.Position);
            }
            else
            {
                Assert.AreEqual(BitCounter.GetUsedByteCount(value) + 1, writer.Position);
            }
        }

        private void CheckUnsignedPackedValue32(ref FastBufferWriter writer, uint value)
        {
            var reader = new FastBufferReader(writer.GetNativeArray());
            ByteUnpacker.ReadValuePacked(ref reader, out uint readValue);
            Assert.AreEqual(readValue, value);
        }
        
        private void CheckSignedPackedSize64(ref FastBufferWriter writer, long value)
        {
            ulong asUlong = Arithmetic.ZigZagEncode(value);
               
            if (asUlong <= 240)
            {
                Assert.AreEqual(1, writer.Position);
            }
            else if (asUlong <= 2287)
            {
                Assert.AreEqual(2, writer.Position);
            }
            else
            {
                Assert.AreEqual(BitCounter.GetUsedByteCount(asUlong) + 1, writer.Position);
            }
        }

        private void CheckSignedPackedValue64(ref FastBufferWriter writer, long value)
        {
            var reader = new FastBufferReader(writer.GetNativeArray());
            ByteUnpacker.ReadValuePacked(ref reader, out long readValue);
            Assert.AreEqual(readValue, value);
        }
        
        private void CheckSignedPackedSize32(ref FastBufferWriter writer, int value)
        {
            ulong asUlong = Arithmetic.ZigZagEncode(value);
               
            if (asUlong <= 240)
            {
                Assert.AreEqual(1, writer.Position);
            }
            else if (asUlong <= 2287)
            {
                Assert.AreEqual(2, writer.Position);
            }
            else
            {
                Assert.AreEqual(BitCounter.GetUsedByteCount(asUlong) + 1, writer.Position);
            }
        }

        private void CheckSignedPackedValue32(ref FastBufferWriter writer, int value)
        {
            var reader = new FastBufferReader(writer.GetNativeArray());
            ByteUnpacker.ReadValuePacked(ref reader, out int readValue);
            Assert.AreEqual(readValue, value);
        }
        
        [Test]
        public void TestPacking64BitsUnsigned()
        {
            FastBufferWriter writer = new FastBufferWriter(9, Allocator.Temp);

            using (writer)
            {
                writer.VerifyCanWrite(9);
                ulong value = 0;
                BytePacker.WriteValuePacked(ref writer, value);
                Assert.AreEqual(1, writer.Position);

                for (var i = 0; i < 64; ++i)
                {
                    value = 1UL << i;
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValuePacked(ref writer, value);
                    CheckUnsignedPackedSize64(ref writer, value);
                    CheckUnsignedPackedValue64(ref writer, value);
                    for (var j = 0; j < 8; ++j)
                    {
                        value = (1UL << i) | (1UL << j);
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValuePacked(ref writer, value);
                        CheckUnsignedPackedSize64(ref writer, value);
                        CheckUnsignedPackedValue64(ref writer, value);
                    }
                }
            }
        }
        
        [Test]
        public void TestPacking32BitsUnsigned()
        {
            FastBufferWriter writer = new FastBufferWriter(9, Allocator.Temp);

            using (writer)
            {
                writer.VerifyCanWrite(9);
                uint value = 0;
                BytePacker.WriteValuePacked(ref writer, value);
                Assert.AreEqual(1, writer.Position);

                for (var i = 0; i < 64; ++i)
                {
                    value = 1U << i;
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValuePacked(ref writer, value);
                    CheckUnsignedPackedSize32(ref writer, value);
                    CheckUnsignedPackedValue32(ref writer, value);
                    for (var j = 0; j < 8; ++j)
                    {
                        value = (1U << i) | (1U << j);
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValuePacked(ref writer, value);
                        CheckUnsignedPackedSize32(ref writer, value);
                        CheckUnsignedPackedValue32(ref writer, value);
                    }
                }
            }
        }
        
        [Test]
        public void TestPacking64BitsSigned()
        {
            FastBufferWriter writer = new FastBufferWriter(9, Allocator.Temp);

            using (writer)
            {
                writer.VerifyCanWrite(9);
                long value = 0;
                BytePacker.WriteValuePacked(ref writer, value);
                Assert.AreEqual(1, writer.Position);

                for (var i = 0; i < 64; ++i)
                {
                    value = 1L << i;
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValuePacked(ref writer, value);
                    CheckSignedPackedSize64(ref writer, value);
                    CheckSignedPackedValue64(ref writer, value);
                    
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValuePacked(ref writer, -value);
                    CheckSignedPackedSize64(ref writer, -value);
                    CheckSignedPackedValue64(ref writer, -value);
                    for (var j = 0; j < 8; ++j)
                    {
                        value = (1L << i) | (1L << j);
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValuePacked(ref writer, value);
                        CheckSignedPackedSize64(ref writer, value);
                        CheckSignedPackedValue64(ref writer, value);
                        
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValuePacked(ref writer, -value);
                        CheckSignedPackedSize64(ref writer, -value);
                        CheckSignedPackedValue64(ref writer, -value);
                    }
                }
            }
        }
        
        [Test]
        public void TestPacking32BitsSigned()
        {
            FastBufferWriter writer = new FastBufferWriter(9, Allocator.Temp);

            using (writer)
            {
                writer.VerifyCanWrite(5);
                int value = 0;
                BytePacker.WriteValuePacked(ref writer, value);
                Assert.AreEqual(1, writer.Position);

                for (var i = 0; i < 64; ++i)
                {
                    value = 1 << i;
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValuePacked(ref writer, value);
                    CheckSignedPackedSize32(ref writer, value);
                    CheckSignedPackedValue32(ref writer, value);
                    
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValuePacked(ref writer, -value);
                    CheckSignedPackedSize32(ref writer, -value);
                    CheckSignedPackedValue32(ref writer, -value);
                    for (var j = 0; j < 8; ++j)
                    {
                        value = (1 << i) | (1 << j);
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValuePacked(ref writer, value);
                        CheckSignedPackedSize32(ref writer, value);
                        CheckSignedPackedValue32(ref writer, value);
                        
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValuePacked(ref writer, -value);
                        CheckSignedPackedSize32(ref writer, -value);
                        CheckSignedPackedValue32(ref writer, -value);
                    }
                }
            }
        }

        private int GetByteCount61Bits(ulong value)
        {
            
            if (value <= 0b0001_1111)
            {
                return 1;
            }

            if (value <= 0b0001_1111_1111_1111)
            {
                return 2;
            }

            if (value <= 0b0001_1111_1111_1111_1111_1111)
            {
                return 3;
            }

            if (value <= 0b0001_1111_1111_1111_1111_1111_1111_1111)
            {
                return 4;
            }

            if (value <= 0b0001_1111_1111_1111_1111_1111_1111_1111_1111_1111)
            {
                return 5;
            }

            if (value <= 0b0001_1111_1111_1111_1111_1111_1111_1111_1111_1111_1111_1111)
            {
                return 6;
            }

            if (value <= 0b0001_1111_1111_1111_1111_1111_1111_1111_1111_1111_1111_1111_1111_1111)
            {
                return 7;
            }

            return 8;
        }

        private int GetByteCount30Bits(uint value)
        {
            
            if (value <= 0b0011_1111)
            {
                return 1;
            }

            if (value <= 0b0011_1111_1111_1111)
            {
                return 2;
            }

            if (value <= 0b0011_1111_1111_1111_1111_1111)
            {
                return 3;
            }

            return 4;
        }

        private int GetByteCount15Bits(ushort value)
        {
            
            if (value <= 0b0111_1111)
            {
                return 1;
            }

            return 2;
        }

        private ulong Get61BitEncodedValue(NativeArray<byte> data)
        {
            FastBufferReader reader = new FastBufferReader(data);
            ByteUnpacker.ReadValueBitPacked(ref reader, out ulong value);
            return value;
        }

        private long Get60BitSignedEncodedValue(NativeArray<byte> data)
        {
            FastBufferReader reader = new FastBufferReader(data);
            ByteUnpacker.ReadValueBitPacked(ref reader, out long value);
            return value;
        }

        private uint Get30BitEncodedValue(NativeArray<byte> data)
        {
            FastBufferReader reader = new FastBufferReader(data);
            ByteUnpacker.ReadValueBitPacked(ref reader, out uint value);
            return value;
        }

        private int Get29BitSignedEncodedValue(NativeArray<byte> data)
        {
            FastBufferReader reader = new FastBufferReader(data);
            ByteUnpacker.ReadValueBitPacked(ref reader, out int value);
            return value;
        }

        private ushort Get15BitEncodedValue(NativeArray<byte> data)
        {
            FastBufferReader reader = new FastBufferReader(data);
            ByteUnpacker.ReadValueBitPacked(ref reader, out ushort value);
            return value;
        }

        private short Get14BitSignedEncodedValue(NativeArray<byte> data)
        {
            FastBufferReader reader = new FastBufferReader(data);
            ByteUnpacker.ReadValueBitPacked(ref reader, out short value);
            return value;
        }
        
        [Test]
        public void TestBitPacking61BitsUnsigned()
        {
            FastBufferWriter writer = new FastBufferWriter(9, Allocator.Temp);

            using (writer)
            {
                writer.VerifyCanWrite(8);
                ulong value = 0;
                BytePacker.WriteValueBitPacked(ref writer, value);
                NativeArray<byte> nativeArray = writer.GetNativeArray();
                Assert.AreEqual(1, writer.Position);
                Assert.AreEqual(0, nativeArray[0] & 0b111);
                Assert.AreEqual(value, Get61BitEncodedValue(nativeArray));
                
                for (var i = 0; i < 61; ++i)
                {
                    value = 1UL << i;
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValueBitPacked(ref writer, value);
                    Assert.AreEqual(GetByteCount61Bits(value), writer.Position, $"Failed on {value} ({i})");
                    Assert.AreEqual(GetByteCount61Bits(value)-1, nativeArray[0] & 0b111, $"Failed on {value} ({i})");
                    Assert.AreEqual(value, Get61BitEncodedValue(nativeArray));
                    
                    for (var j = 0; j < 8; ++j)
                    {
                        value = (1UL << i) | (1UL << j);
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValueBitPacked(ref writer, value);
                        Assert.AreEqual(GetByteCount61Bits(value), writer.Position, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(GetByteCount61Bits(value)-1, nativeArray[0] & 0b111, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(value, Get61BitEncodedValue(nativeArray));
                    }
                }

                Assert.Throws<ArgumentException>(() => { BytePacker.WriteValueBitPacked(ref writer, 1UL << 61); });
            }
        }
        
        [Test]
        public void TestBitPacking60BitsSigned()
        {
            FastBufferWriter writer = new FastBufferWriter(9, Allocator.Temp);

            using (writer)
            {
                writer.VerifyCanWrite(8);
                long value = 0;
                BytePacker.WriteValueBitPacked(ref writer, value);
                NativeArray<byte> nativeArray = writer.GetNativeArray();
                Assert.AreEqual(1, writer.Position);
                Assert.AreEqual(0, nativeArray[0] & 0b111);
                Assert.AreEqual(value, Get60BitSignedEncodedValue(nativeArray));
                
                for (var i = 0; i < 61; ++i)
                {
                    value = 1U << i;
                    ulong zzvalue = Arithmetic.ZigZagEncode(value);
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValueBitPacked(ref writer, value);
                    Assert.AreEqual(GetByteCount61Bits(zzvalue), writer.Position, $"Failed on {value} ({i})");
                    Assert.AreEqual(GetByteCount61Bits(zzvalue)-1, nativeArray[0] & 0b111, $"Failed on {value} ({i})");
                    Assert.AreEqual(value, Get60BitSignedEncodedValue(nativeArray));

                    value = -value;
                    zzvalue = Arithmetic.ZigZagEncode(value);
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValueBitPacked(ref writer, value);
                    Assert.AreEqual(GetByteCount61Bits(zzvalue), writer.Position, $"Failed on {value} ({i})");
                    Assert.AreEqual(GetByteCount61Bits(zzvalue)-1, nativeArray[0] & 0b111, $"Failed on {value} ({i})");
                    Assert.AreEqual(value, Get60BitSignedEncodedValue(nativeArray));
                    
                    for (var j = 0; j < 8; ++j)
                    {
                        value = (1U << i) | (1U << j);
                        zzvalue = Arithmetic.ZigZagEncode(value);
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValueBitPacked(ref writer, value);
                        Assert.AreEqual(GetByteCount61Bits(zzvalue), writer.Position, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(GetByteCount61Bits(zzvalue)-1, nativeArray[0] & 0b111, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(value, Get60BitSignedEncodedValue(nativeArray));

                        value = -value;
                        zzvalue = Arithmetic.ZigZagEncode(value);
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValueBitPacked(ref writer, value);
                        Assert.AreEqual(GetByteCount61Bits(zzvalue), writer.Position, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(GetByteCount61Bits(zzvalue)-1, nativeArray[0] & 0b111, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(value, Get60BitSignedEncodedValue(nativeArray));
                    }
                }

                Assert.Throws<ArgumentException>(() => { BytePacker.WriteValueBitPacked(ref writer, 1UL << 61); });
            }
        }
        
        [Test]
        public void TestBitPacking30BitsUnsigned()
        {
            FastBufferWriter writer = new FastBufferWriter(9, Allocator.Temp);

            using (writer)
            {
                writer.VerifyCanWrite(4);
                uint value = 0;
                BytePacker.WriteValueBitPacked(ref writer, value);
                NativeArray<byte> nativeArray = writer.GetNativeArray();
                Assert.AreEqual(1, writer.Position);
                Assert.AreEqual(0, nativeArray[0] & 0b11);
                Assert.AreEqual(value, Get30BitEncodedValue(nativeArray));
                
                for (var i = 0; i < 30; ++i)
                {
                    value = 1U << i;
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValueBitPacked(ref writer, value);
                    Assert.AreEqual(GetByteCount30Bits(value), writer.Position, $"Failed on {value} ({i})");
                    Assert.AreEqual(GetByteCount30Bits(value)-1, nativeArray[0] & 0b11, $"Failed on {value} ({i})");
                    Assert.AreEqual(value, Get30BitEncodedValue(nativeArray));
                    
                    for (var j = 0; j < 8; ++j)
                    {
                        value = (1U << i) | (1U << j);
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValueBitPacked(ref writer, value);
                        Assert.AreEqual(GetByteCount30Bits(value), writer.Position, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(GetByteCount30Bits(value)-1, nativeArray[0] & 0b11, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(value, Get30BitEncodedValue(nativeArray));
                    }
                }

                Assert.Throws<ArgumentException>(() => { BytePacker.WriteValueBitPacked(ref writer, 1U << 30); });
            }
        }
        
        [Test]
        public void TestBitPacking29BitsSigned()
        {
            FastBufferWriter writer = new FastBufferWriter(9, Allocator.Temp);

            using (writer)
            {
                writer.VerifyCanWrite(4);
                int value = 0;
                BytePacker.WriteValueBitPacked(ref writer, value);
                NativeArray<byte> nativeArray = writer.GetNativeArray();
                Assert.AreEqual(1, writer.Position);
                Assert.AreEqual(0, nativeArray[0] & 0b11);
                Assert.AreEqual(value, Get30BitEncodedValue(nativeArray));
                
                for (var i = 0; i < 29; ++i)
                {
                    value = 1 << i;
                    uint zzvalue = (uint)Arithmetic.ZigZagEncode(value);
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValueBitPacked(ref writer, value);
                    Assert.AreEqual(GetByteCount30Bits(zzvalue), writer.Position, $"Failed on {value} ({i})");
                    Assert.AreEqual(GetByteCount30Bits(zzvalue)-1, nativeArray[0] & 0b11, $"Failed on {value} ({i})");
                    Assert.AreEqual(value, Get29BitSignedEncodedValue(nativeArray));

                    value = -value;
                    zzvalue = (uint)Arithmetic.ZigZagEncode(value);
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValueBitPacked(ref writer, value);
                    Assert.AreEqual(GetByteCount30Bits(zzvalue), writer.Position, $"Failed on {value} ({i})");
                    Assert.AreEqual(GetByteCount30Bits(zzvalue)-1, nativeArray[0] & 0b11, $"Failed on {value} ({i})");
                    Assert.AreEqual(value, Get29BitSignedEncodedValue(nativeArray));
                    
                    for (var j = 0; j < 8; ++j)
                    {
                        value = (1 << i) | (1 << j);
                        zzvalue = (uint)Arithmetic.ZigZagEncode(value);
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValueBitPacked(ref writer, value);
                        Assert.AreEqual(GetByteCount30Bits(zzvalue), writer.Position, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(GetByteCount30Bits(zzvalue)-1, nativeArray[0] & 0b11, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(value, Get29BitSignedEncodedValue(nativeArray));

                        value = -value;
                        zzvalue = (uint)Arithmetic.ZigZagEncode(value);
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValueBitPacked(ref writer, value);
                        Assert.AreEqual(GetByteCount30Bits(zzvalue), writer.Position, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(GetByteCount30Bits(zzvalue)-1, nativeArray[0] & 0b11, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(value, Get29BitSignedEncodedValue(nativeArray));
                    }
                }
            }
        }
        
        [Test]
        public void TestBitPacking15BitsUnsigned()
        {
            FastBufferWriter writer = new FastBufferWriter(9, Allocator.Temp);

            using (writer)
            {
                writer.VerifyCanWrite(2);
                ushort value = 0;
                BytePacker.WriteValueBitPacked(ref writer, value);
                NativeArray<byte> nativeArray = writer.GetNativeArray();
                Assert.AreEqual(1, writer.Position);
                Assert.AreEqual(0, nativeArray[0] & 0b1);
                Assert.AreEqual(value, Get15BitEncodedValue(nativeArray));
                
                for (var i = 0; i < 15; ++i)
                {
                    value = (ushort)(1U << i);
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValueBitPacked(ref writer, value);
                    Assert.AreEqual(GetByteCount15Bits(value), writer.Position, $"Failed on {value} ({i})");
                    Assert.AreEqual(GetByteCount15Bits(value)-1, nativeArray[0] & 0b1, $"Failed on {value} ({i})");
                    Assert.AreEqual(value, Get15BitEncodedValue(nativeArray));
                    
                    for (var j = 0; j < 8; ++j)
                    {
                        value = (ushort)((1U << i) | (1U << j));
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValueBitPacked(ref writer, value);
                        Assert.AreEqual(GetByteCount15Bits(value), writer.Position, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(GetByteCount15Bits(value)-1, nativeArray[0] & 0b1, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(value, Get15BitEncodedValue(nativeArray));
                    }
                }

                Assert.Throws<ArgumentException>(() => { BytePacker.WriteValueBitPacked(ref writer, (ushort)(1U << 15)); });
            }
        }
        [Test]
        public void TestBitPacking14BitsSigned()
        {
            FastBufferWriter writer = new FastBufferWriter(9, Allocator.Temp);

            using (writer)
            {
                writer.VerifyCanWrite(2);
                short value = 0;
                BytePacker.WriteValueBitPacked(ref writer, value);
                NativeArray<byte> nativeArray = writer.GetNativeArray();
                Assert.AreEqual(1, writer.Position);
                Assert.AreEqual(0, nativeArray[0] & 0b1);
                Assert.AreEqual(value, Get15BitEncodedValue(nativeArray));
                
                for (var i = 0; i < 14; ++i)
                {
                    value = (short)(1 << i);
                    ushort zzvalue = (ushort)Arithmetic.ZigZagEncode(value);
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValueBitPacked(ref writer, value);
                    Assert.AreEqual(GetByteCount15Bits(zzvalue), writer.Position, $"Failed on {value} ({i})");
                    Assert.AreEqual(GetByteCount15Bits(zzvalue)-1, nativeArray[0] & 0b1, $"Failed on {value} ({i})");
                    Assert.AreEqual(value, Get14BitSignedEncodedValue(nativeArray));

                    value = (short)-value;
                    zzvalue = (ushort)Arithmetic.ZigZagEncode(value);
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValueBitPacked(ref writer, value);
                    Assert.AreEqual(GetByteCount15Bits(zzvalue), writer.Position, $"Failed on {value} ({i})");
                    Assert.AreEqual(GetByteCount15Bits(zzvalue)-1, nativeArray[0] & 0b1, $"Failed on {value} ({i})");
                    Assert.AreEqual(value, Get14BitSignedEncodedValue(nativeArray));
                    
                    for (var j = 0; j < 8; ++j)
                    {
                        value = (short)((1 << i) | (1 << j));
                        zzvalue = (ushort)Arithmetic.ZigZagEncode(value);
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValueBitPacked(ref writer, value);
                        Assert.AreEqual(GetByteCount15Bits(zzvalue), writer.Position, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(GetByteCount15Bits(zzvalue)-1, nativeArray[0] & 0b1, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(value, Get14BitSignedEncodedValue(nativeArray));

                        value = (short)-value;
                        zzvalue = (ushort)Arithmetic.ZigZagEncode(value);
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValueBitPacked(ref writer, value);
                        Assert.AreEqual(GetByteCount15Bits(zzvalue), writer.Position, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(GetByteCount15Bits(zzvalue)-1, nativeArray[0] & 0b1, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(value, Get14BitSignedEncodedValue(nativeArray));
                    }
                }
            }
        }
    }
}
