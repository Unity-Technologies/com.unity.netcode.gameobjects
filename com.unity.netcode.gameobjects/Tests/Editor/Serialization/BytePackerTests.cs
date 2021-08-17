using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Multiplayer.Netcode;
using UnityEngine;

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

        private unsafe void CheckUnsignedPackedValue64(ref FastBufferWriter writer, ulong value)
        {
            byte* asBytes = writer.GetUnsafePtr();
            ulong readValue;
            if (asBytes[0] <= 240)
            {
                Assert.AreEqual(asBytes[0], value);
                return;
            }

            if (asBytes[0] <= 248)
            {
                readValue = 240UL + ((asBytes[0] - 241UL) << 8) + asBytes[1];
                Assert.AreEqual(readValue, value);
                return;
            }

            var numBytes = asBytes[0] - 247;
            readValue = 0;
            UnsafeUtility.MemCpy(&readValue, asBytes + 1, numBytes);
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

        private unsafe void CheckUnsignedPackedValue32(ref FastBufferWriter writer, uint value)
        {
            byte* asBytes = writer.GetUnsafePtr();
            ulong readValue;
            if (asBytes[0] <= 240)
            {
                Assert.AreEqual(asBytes[0], value);
                return;
            }

            if (asBytes[0] <= 248)
            {
                readValue = 240UL + ((asBytes[0] - 241UL) << 8) + asBytes[1];
                Assert.AreEqual(readValue, value);
                return;
            }

            var numBytes = asBytes[0] - 247;
            readValue = 0;
            UnsafeUtility.MemCpy(&readValue, asBytes + 1, numBytes);
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

        private unsafe void CheckSignedPackedValue64(ref FastBufferWriter writer, long value)
        {
            byte* asBytes = writer.GetUnsafePtr();
            ulong readValue;
            if (asBytes[0] <= 240)
            {
                Assert.AreEqual(Arithmetic.ZigZagDecode(asBytes[0]), value);
                return;
            }

            if (asBytes[0] <= 248)
            {
                readValue = 240UL + ((asBytes[0] - 241UL) << 8) + asBytes[1];
                Assert.AreEqual(Arithmetic.ZigZagDecode(readValue), value);
                return;
            }

            var numBytes = asBytes[0] - 247;
            readValue = 0;
            UnsafeUtility.MemCpy(&readValue, asBytes + 1, numBytes);
            Assert.AreEqual(Arithmetic.ZigZagDecode(readValue), value);
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

        private unsafe void CheckSignedPackedValue32(ref FastBufferWriter writer, int value)
        {
            byte* asBytes = writer.GetUnsafePtr();
            ulong readValue;
            if (asBytes[0] <= 240)
            {
                Assert.AreEqual(Arithmetic.ZigZagDecode(asBytes[0]), value);
                return;
            }

            if (asBytes[0] <= 248)
            {
                readValue = 240UL + ((asBytes[0] - 241UL) << 8) + asBytes[1];
                Assert.AreEqual(Arithmetic.ZigZagDecode(readValue), value);
                return;
            }

            var numBytes = asBytes[0] - 247;
            readValue = 0;
            UnsafeUtility.MemCpy(&readValue, asBytes + 1, numBytes);
            Assert.AreEqual(Arithmetic.ZigZagDecode(readValue), value);
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

        private unsafe ulong Get61BitEncodedValue(byte* data)
        {
            ulong returnValue = 0;
            byte* ptr = ((byte*) &returnValue);
            int numBytes = (data[0] & 0b111) + 1;
            switch (numBytes)
            {
                case 1:
                    *ptr = *data;
                    break;
                case 2:
                    *(ushort*) ptr = *(ushort*)data;
                    break;
                case 3:
                    *(ushort*) ptr = *(ushort*)data;
                    *(ptr+2) = *(data+2);
                    break;
                case 4:
                    *(uint*) ptr = *(uint*)data;
                    break;
                case 5:
                    *(uint*) ptr = *(uint*)data;
                    *(ptr+4) = *(data+4);
                    break;
                case 6:
                    *(uint*) ptr = *(uint*)data;
                    *(ushort*) (ptr+4) = *(ushort*)(data+4);
                    break;
                case 7:
                    *(uint*) ptr = *(uint*)data;
                    *(ushort*) (ptr+4) = *(ushort*)(data+4);
                    *(ptr+6) = *(data+6);
                    break;
                case 8:
                    *(ulong*) ptr = *(ulong*)data;
                    break;
            }

            return returnValue >> 3;
        }

        private unsafe uint Get30BitEncodedValue(byte* data)
        {
            uint returnValue = 0;
            byte* ptr = ((byte*) &returnValue);
            int numBytes = (data[0] & 0b11) + 1;
            switch (numBytes)
            {
                case 1:
                    *ptr = *data;
                    break;
                case 2:
                    *(ushort*) ptr = *(ushort*)data;
                    break;
                case 3:
                    *(ushort*) ptr = *(ushort*)data;
                    *(ptr+2) = *(data+2);
                    break;
                case 4:
                    *(uint*) ptr = *(uint*)data;
                    break;
            }

            return returnValue >> 2;
        }

        private unsafe ushort Get15BitEncodedValue(byte* data)
        {
            ushort returnValue = 0;
            byte* ptr = ((byte*) &returnValue);
            int numBytes = (data[0] & 0b1) + 1;
            switch (numBytes)
            {
                case 1:
                    *ptr = *data;
                    break;
                case 2:
                    *(ushort*) ptr = *(ushort*)data;
                    break;
            }

            return (ushort)(returnValue >> 1);
        }
        
        [Test]
        public unsafe void TestBitPacking61BitsUnsigned()
        {
            FastBufferWriter writer = new FastBufferWriter(9, Allocator.Temp);

            using (writer)
            {
                writer.VerifyCanWrite(8);
                ulong value = 0;
                BytePacker.WriteValueBitPacked(ref writer, value);
                byte* asByte = writer.GetUnsafePtr();
                Assert.AreEqual(1, writer.Position);
                Assert.AreEqual(0, asByte[0] & 0b111);
                Assert.AreEqual(value, Get61BitEncodedValue(asByte));
                
                for (var i = 0; i < 61; ++i)
                {
                    value = 1UL << i;
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValueBitPacked(ref writer, value);
                    Assert.AreEqual(GetByteCount61Bits(value), writer.Position, $"Failed on {value} ({i})");
                    Assert.AreEqual(GetByteCount61Bits(value)-1, asByte[0] & 0b111, $"Failed on {value} ({i})");
                    Assert.AreEqual(value, Get61BitEncodedValue(asByte));
                    
                    for (var j = 0; j < 8; ++j)
                    {
                        value = (1UL << i) | (1UL << j);
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValueBitPacked(ref writer, value);
                        Assert.AreEqual(GetByteCount61Bits(value), writer.Position, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(GetByteCount61Bits(value)-1, asByte[0] & 0b111, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(value, Get61BitEncodedValue(asByte));
                    }
                }

                Assert.Throws<ArgumentException>(() => { BytePacker.WriteValueBitPacked(ref writer, 1UL << 61); });
            }
        }
        
        [Test]
        public unsafe void TestBitPacking60BitsSigned()
        {
            FastBufferWriter writer = new FastBufferWriter(9, Allocator.Temp);

            using (writer)
            {
                writer.VerifyCanWrite(8);
                long value = 0;
                BytePacker.WriteValueBitPacked(ref writer, value);
                byte* asByte = writer.GetUnsafePtr();
                Assert.AreEqual(1, writer.Position);
                Assert.AreEqual(0, asByte[0] & 0b111);
                Assert.AreEqual(value, Arithmetic.ZigZagDecode(Get61BitEncodedValue(asByte)));
                
                for (var i = 0; i < 61; ++i)
                {
                    value = 1U << i;
                    ulong zzvalue = Arithmetic.ZigZagEncode(value);
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValueBitPacked(ref writer, value);
                    Assert.AreEqual(GetByteCount61Bits(zzvalue), writer.Position, $"Failed on {value} ({i})");
                    Assert.AreEqual(GetByteCount61Bits(zzvalue)-1, asByte[0] & 0b111, $"Failed on {value} ({i})");
                    Assert.AreEqual(value, Arithmetic.ZigZagDecode(Get61BitEncodedValue(asByte)));

                    value = -value;
                    zzvalue = Arithmetic.ZigZagEncode(value);
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValueBitPacked(ref writer, value);
                    Assert.AreEqual(GetByteCount61Bits(zzvalue), writer.Position, $"Failed on {value} ({i})");
                    Assert.AreEqual(GetByteCount61Bits(zzvalue)-1, asByte[0] & 0b111, $"Failed on {value} ({i})");
                    Assert.AreEqual(value, Arithmetic.ZigZagDecode(Get61BitEncodedValue(asByte)));
                    
                    for (var j = 0; j < 8; ++j)
                    {
                        value = (1U << i) | (1U << j);
                        zzvalue = Arithmetic.ZigZagEncode(value);
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValueBitPacked(ref writer, value);
                        Assert.AreEqual(GetByteCount61Bits(zzvalue), writer.Position, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(GetByteCount61Bits(zzvalue)-1, asByte[0] & 0b111, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(value, Arithmetic.ZigZagDecode(Get61BitEncodedValue(asByte)));

                        value = -value;
                        zzvalue = Arithmetic.ZigZagEncode(value);
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValueBitPacked(ref writer, value);
                        Assert.AreEqual(GetByteCount61Bits(zzvalue), writer.Position, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(GetByteCount61Bits(zzvalue)-1, asByte[0] & 0b111, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(value, Arithmetic.ZigZagDecode(Get61BitEncodedValue(asByte)));
                    }
                }

                Assert.Throws<ArgumentException>(() => { BytePacker.WriteValueBitPacked(ref writer, 1UL << 61); });
            }
        }
        
        [Test]
        public unsafe void TestBitPacking30BitsUnsigned()
        {
            FastBufferWriter writer = new FastBufferWriter(9, Allocator.Temp);

            using (writer)
            {
                writer.VerifyCanWrite(4);
                uint value = 0;
                BytePacker.WriteValueBitPacked(ref writer, value);
                byte* asByte = writer.GetUnsafePtr();
                Assert.AreEqual(1, writer.Position);
                Assert.AreEqual(0, asByte[0] & 0b11);
                Assert.AreEqual(value, Get30BitEncodedValue(asByte));
                
                for (var i = 0; i < 30; ++i)
                {
                    value = 1U << i;
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValueBitPacked(ref writer, value);
                    Assert.AreEqual(GetByteCount30Bits(value), writer.Position, $"Failed on {value} ({i})");
                    Assert.AreEqual(GetByteCount30Bits(value)-1, asByte[0] & 0b11, $"Failed on {value} ({i})");
                    Assert.AreEqual(value, Get30BitEncodedValue(asByte));
                    
                    for (var j = 0; j < 8; ++j)
                    {
                        value = (1U << i) | (1U << j);
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValueBitPacked(ref writer, value);
                        Assert.AreEqual(GetByteCount30Bits(value), writer.Position, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(GetByteCount30Bits(value)-1, asByte[0] & 0b11, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(value, Get30BitEncodedValue(asByte));
                    }
                }

                Assert.Throws<ArgumentException>(() => { BytePacker.WriteValueBitPacked(ref writer, 1U << 30); });
            }
        }
        
        [Test]
        public unsafe void TestBitPacking29BitsSigned()
        {
            FastBufferWriter writer = new FastBufferWriter(9, Allocator.Temp);

            using (writer)
            {
                writer.VerifyCanWrite(4);
                int value = 0;
                BytePacker.WriteValueBitPacked(ref writer, value);
                byte* asByte = writer.GetUnsafePtr();
                Assert.AreEqual(1, writer.Position);
                Assert.AreEqual(0, asByte[0] & 0b11);
                Assert.AreEqual(value, Get30BitEncodedValue(asByte));
                
                for (var i = 0; i < 29; ++i)
                {
                    value = 1 << i;
                    uint zzvalue = (uint)Arithmetic.ZigZagEncode(value);
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValueBitPacked(ref writer, value);
                    Assert.AreEqual(GetByteCount30Bits(zzvalue), writer.Position, $"Failed on {value} ({i})");
                    Assert.AreEqual(GetByteCount30Bits(zzvalue)-1, asByte[0] & 0b11, $"Failed on {value} ({i})");
                    Assert.AreEqual(value, Arithmetic.ZigZagDecode(Get30BitEncodedValue(asByte)));

                    value = -value;
                    zzvalue = (uint)Arithmetic.ZigZagEncode(value);
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValueBitPacked(ref writer, value);
                    Assert.AreEqual(GetByteCount30Bits(zzvalue), writer.Position, $"Failed on {value} ({i})");
                    Assert.AreEqual(GetByteCount30Bits(zzvalue)-1, asByte[0] & 0b11, $"Failed on {value} ({i})");
                    Assert.AreEqual(value, Arithmetic.ZigZagDecode(Get30BitEncodedValue(asByte)));
                    
                    for (var j = 0; j < 8; ++j)
                    {
                        value = (1 << i) | (1 << j);
                        zzvalue = (uint)Arithmetic.ZigZagEncode(value);
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValueBitPacked(ref writer, value);
                        Assert.AreEqual(GetByteCount30Bits(zzvalue), writer.Position, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(GetByteCount30Bits(zzvalue)-1, asByte[0] & 0b11, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(value, Arithmetic.ZigZagDecode(Get30BitEncodedValue(asByte)));

                        value = -value;
                        zzvalue = (uint)Arithmetic.ZigZagEncode(value);
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValueBitPacked(ref writer, value);
                        Assert.AreEqual(GetByteCount30Bits(zzvalue), writer.Position, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(GetByteCount30Bits(zzvalue)-1, asByte[0] & 0b11, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(value, Arithmetic.ZigZagDecode(Get30BitEncodedValue(asByte)));
                    }
                }
            }
        }
        
        [Test]
        public unsafe void TestBitPacking15BitsUnsigned()
        {
            FastBufferWriter writer = new FastBufferWriter(9, Allocator.Temp);

            using (writer)
            {
                writer.VerifyCanWrite(2);
                ushort value = 0;
                BytePacker.WriteValueBitPacked(ref writer, value);
                byte* asByte = writer.GetUnsafePtr();
                Assert.AreEqual(1, writer.Position);
                Assert.AreEqual(0, asByte[0] & 0b1);
                Assert.AreEqual(value, Get15BitEncodedValue(asByte));
                
                for (var i = 0; i < 15; ++i)
                {
                    value = (ushort)(1U << i);
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValueBitPacked(ref writer, value);
                    Assert.AreEqual(GetByteCount15Bits(value), writer.Position, $"Failed on {value} ({i})");
                    Assert.AreEqual(GetByteCount15Bits(value)-1, asByte[0] & 0b1, $"Failed on {value} ({i})");
                    Assert.AreEqual(value, Get15BitEncodedValue(asByte));
                    
                    for (var j = 0; j < 8; ++j)
                    {
                        value = (ushort)((1U << i) | (1U << j));
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValueBitPacked(ref writer, value);
                        Assert.AreEqual(GetByteCount15Bits(value), writer.Position, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(GetByteCount15Bits(value)-1, asByte[0] & 0b1, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(value, Get15BitEncodedValue(asByte));
                    }
                }

                Assert.Throws<ArgumentException>(() => { BytePacker.WriteValueBitPacked(ref writer, (ushort)(1U << 15)); });
            }
        }
        [Test]
        public unsafe void TestBitPacking14BitsSigned()
        {
            FastBufferWriter writer = new FastBufferWriter(9, Allocator.Temp);

            using (writer)
            {
                writer.VerifyCanWrite(2);
                short value = 0;
                BytePacker.WriteValueBitPacked(ref writer, value);
                byte* asByte = writer.GetUnsafePtr();
                Assert.AreEqual(1, writer.Position);
                Assert.AreEqual(0, asByte[0] & 0b1);
                Assert.AreEqual(value, Get15BitEncodedValue(asByte));
                
                for (var i = 0; i < 14; ++i)
                {
                    value = (short)(1 << i);
                    ushort zzvalue = (ushort)Arithmetic.ZigZagEncode(value);
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValueBitPacked(ref writer, value);
                    Assert.AreEqual(GetByteCount15Bits(zzvalue), writer.Position, $"Failed on {value} ({i})");
                    Assert.AreEqual(GetByteCount15Bits(zzvalue)-1, asByte[0] & 0b1, $"Failed on {value} ({i})");
                    Assert.AreEqual(value, Arithmetic.ZigZagDecode(Get15BitEncodedValue(asByte)));

                    value = (short)-value;
                    zzvalue = (ushort)Arithmetic.ZigZagEncode(value);
                    writer.Seek(0);
                    writer.Truncate();
                    BytePacker.WriteValueBitPacked(ref writer, value);
                    Assert.AreEqual(GetByteCount15Bits(zzvalue), writer.Position, $"Failed on {value} ({i})");
                    Assert.AreEqual(GetByteCount15Bits(zzvalue)-1, asByte[0] & 0b1, $"Failed on {value} ({i})");
                    Assert.AreEqual(value, Arithmetic.ZigZagDecode(Get15BitEncodedValue(asByte)));
                    
                    for (var j = 0; j < 8; ++j)
                    {
                        value = (short)((1 << i) | (1 << j));
                        zzvalue = (ushort)Arithmetic.ZigZagEncode(value);
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValueBitPacked(ref writer, value);
                        Assert.AreEqual(GetByteCount15Bits(zzvalue), writer.Position, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(GetByteCount15Bits(zzvalue)-1, asByte[0] & 0b1, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(value, Arithmetic.ZigZagDecode(Get15BitEncodedValue(asByte)));

                        value = (short)-value;
                        zzvalue = (ushort)Arithmetic.ZigZagEncode(value);
                        writer.Seek(0);
                        writer.Truncate();
                        BytePacker.WriteValueBitPacked(ref writer, value);
                        Assert.AreEqual(GetByteCount15Bits(zzvalue), writer.Position, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(GetByteCount15Bits(zzvalue)-1, asByte[0] & 0b1, $"Failed on {value} ({i}, {j})");
                        Assert.AreEqual(value, Arithmetic.ZigZagDecode(Get15BitEncodedValue(asByte)));
                    }
                }
            }
        }
    }
}
