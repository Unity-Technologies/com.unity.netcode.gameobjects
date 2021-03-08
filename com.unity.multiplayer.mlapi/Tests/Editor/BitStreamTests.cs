using System;
using NUnit.Framework;
using MLAPI.Serialization;
using System.Text;

namespace MLAPI.EditorTests
{
    public class BitStreamTests
    {
        [Test]
        public void TestEmptyStream()
        {
            var networkBuffer = new NetworkBuffer(new byte[100]);
            Assert.That(networkBuffer.Length, Is.EqualTo(100));
        }

        [Test]
        public void TestBool()
        {
            var networkBuffer = new NetworkBuffer(new byte[100]);
            networkBuffer.WriteBit(true);
            Assert.That(networkBuffer.Length, Is.EqualTo(100));
        }

        [Test]
        public void TestSetLength()
        {
            var networkBuffer = new NetworkBuffer(4);
            networkBuffer.SetLength(100);

            Assert.That(networkBuffer.Capacity, Is.GreaterThanOrEqualTo(100));
        }

        [Test]
        public void TestSetLength2()
        {
            var networkBuffer = new NetworkBuffer(4);

            networkBuffer.WriteByte(1);
            networkBuffer.WriteByte(1);
            networkBuffer.WriteByte(1);
            networkBuffer.WriteByte(1);

            networkBuffer.SetLength(0);

            // position should never go beyond length
            Assert.That(networkBuffer.Position, Is.EqualTo(0));
        }

        [Test]
        public void TestGrow()
        {
            // stream should not grow when given a buffer
            var networkBuffer = new NetworkBuffer(new byte[0]);
            var networkWriter = new NetworkWriter(networkBuffer);
            Assert.That(() => { networkWriter.WriteInt64(long.MaxValue); }, Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void TestInOutBool()
        {
            var buffer = new byte[100];

            var outNetworkBuffer = new NetworkBuffer(buffer);
            outNetworkBuffer.WriteBit(true);
            outNetworkBuffer.WriteBit(false);
            outNetworkBuffer.WriteBit(true);


            // the bit should now be stored in the buffer,  lets see if it comes out

            var inNetworkBuffer = new NetworkBuffer(buffer);

            Assert.That(inNetworkBuffer.ReadBit(), Is.True);
            Assert.That(inNetworkBuffer.ReadBit(), Is.False);
            Assert.That(inNetworkBuffer.ReadBit(), Is.True);
        }


        [Test]
        public void TestIntOutPacked16Bit()
        {
            short svalue = -31934;
            ushort uvalue = 64893;
            var outNetworkBuffer = new NetworkBuffer();
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);
            outNetworkWriter.WriteInt16Packed(svalue);
            outNetworkWriter.WriteUInt16Packed(uvalue);

            var inNetworkBuffer = new NetworkBuffer(outNetworkBuffer.GetBuffer());
            var inNetworkReader = new NetworkReader(inNetworkBuffer);
            Assert.That(inNetworkReader.ReadInt16Packed(), Is.EqualTo(svalue));
            Assert.That(inNetworkReader.ReadUInt16Packed(), Is.EqualTo(uvalue));
        }


        [Test]
        public void TestIntOutPacked32Bit()
        {
            int svalue = -100913642;
            uint uvalue = 1467867235;
            var outNetworkBuffer = new NetworkBuffer();
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);
            outNetworkWriter.WriteInt32Packed(svalue);
            outNetworkWriter.WriteUInt32Packed(uvalue);

            var inNetworkBuffer = new NetworkBuffer(outNetworkBuffer.GetBuffer());
            var inNetworkReader = new NetworkReader(inNetworkBuffer);
            Assert.That(inNetworkReader.ReadInt32Packed(), Is.EqualTo(svalue));
            Assert.That(inNetworkReader.ReadUInt32Packed(), Is.EqualTo(uvalue));
        }


        [Test]
        public void TestInOutPacked64Bit()
        {
            var buffer = new byte[100];

            long someNumber = -1469598103934656037;
            ulong uNumber = 81246971249124124;
            ulong uNumber2 = 2287;
            ulong uNumber3 = 235;

            var outNetworkBuffer = new NetworkBuffer(buffer);
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);
            outNetworkWriter.WriteInt64Packed(someNumber);
            outNetworkWriter.WriteUInt64Packed(uNumber);
            outNetworkWriter.WriteUInt64Packed(uNumber2);
            outNetworkWriter.WriteUInt64Packed(uNumber3);

            // the bit should now be stored in the buffer,  lets see if it comes out

            var inNetworkBuffer = new NetworkBuffer(buffer);
            var inNetworkReader = new NetworkReader(inNetworkBuffer);

            Assert.That(inNetworkReader.ReadInt64Packed(), Is.EqualTo(someNumber));
            Assert.That(inNetworkReader.ReadUInt64Packed(), Is.EqualTo(uNumber));
            Assert.That(inNetworkReader.ReadUInt64Packed(), Is.EqualTo(uNumber2));
            Assert.That(inNetworkReader.ReadUInt64Packed(), Is.EqualTo(uNumber3));
        }

        [Test]
        public void TestStreamCopy()
        {
            var inNetworkBuffer = new NetworkBuffer();
            var copyNetworkBuffer = new NetworkBuffer();

            byte initialValue1 = 56;
            byte initialValue2 = 24;

            inNetworkBuffer.WriteByte(initialValue1);
            inNetworkBuffer.WriteByte(initialValue2);

            byte copyValue1 = 27;
            byte copyValue2 = 100;

            copyNetworkBuffer.WriteByte(copyValue1);
            copyNetworkBuffer.WriteByte(copyValue2);

            inNetworkBuffer.CopyFrom(copyNetworkBuffer, 2);

            var outNetworkBuffer = new NetworkBuffer(inNetworkBuffer.ToArray());

            Assert.That(outNetworkBuffer.ReadByte(), Is.EqualTo(initialValue1));
            Assert.That(outNetworkBuffer.ReadByte(), Is.EqualTo(initialValue2));
            Assert.That(outNetworkBuffer.ReadByte(), Is.EqualTo(copyValue1));
            Assert.That(outNetworkBuffer.ReadByte(), Is.EqualTo(copyValue2));
        }

        [Test]
        public void TestToArray()
        {
            var inNetworkBuffer = new NetworkBuffer();
            inNetworkBuffer.WriteByte(5);
            inNetworkBuffer.WriteByte(6);
            Assert.That(inNetworkBuffer.ToArray().Length, Is.EqualTo(2));
        }


        [Test]
        public void TestInOutBytes()
        {
            var buffer = new byte[100];
            byte someNumber = 0xff;

            var outNetworkBuffer = new NetworkBuffer(buffer);
            outNetworkBuffer.WriteByte(someNumber);

            var inNetworkBuffer = new NetworkBuffer(buffer);
            Assert.That(inNetworkBuffer.ReadByte(), Is.EqualTo(someNumber));
        }

        [Test]
        public void TestInOutInt16()
        {
            var buffer = new byte[100];
            short someNumber = 23223;

            var outNetworkBuffer = new NetworkBuffer(buffer);
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);
            outNetworkWriter.WriteInt16(someNumber);

            var inNetworkBuffer = new NetworkBuffer(buffer);
            var inNetworkReader = new NetworkReader(inNetworkBuffer);
            short result = inNetworkReader.ReadInt16();

            Assert.That(result, Is.EqualTo(someNumber));
        }

        [Test]
        public void TestInOutInt32()
        {
            var buffer = new byte[100];
            int someNumber = 23234223;

            var outNetworkBuffer = new NetworkBuffer(buffer);
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);
            outNetworkWriter.WriteInt32(someNumber);

            var inNetworkBuffer = new NetworkBuffer(buffer);
            var inNetworkReader = new NetworkReader(inNetworkBuffer);
            int result = inNetworkReader.ReadInt32();

            Assert.That(result, Is.EqualTo(someNumber));
        }

        [Test]
        public void TestInOutInt64()
        {
            var buffer = new byte[100];
            long someNumber = 4614256656552045848;

            var outNetworkBuffer = new NetworkBuffer(buffer);
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);
            outNetworkWriter.WriteInt64(someNumber);

            var inNetworkBuffer = new NetworkBuffer(buffer);
            var inNetworkReader = new NetworkReader(inNetworkBuffer);
            long result = inNetworkReader.ReadInt64();

            Assert.That(result, Is.EqualTo(someNumber));
        }

        [Test]
        public void TestInOutMultiple()
        {
            var buffer = new byte[100];
            short someNumber = -12423;
            short someNumber2 = 9322;

            var outNetworkBuffer = new NetworkBuffer(buffer);
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);
            outNetworkWriter.WriteInt16(someNumber);
            outNetworkWriter.WriteInt16(someNumber2);


            // the bit should now be stored in the buffer,  lets see if it comes out

            var inNetworkBuffer = new NetworkBuffer(buffer);
            var inNetworkReader = new NetworkReader(inNetworkBuffer);
            short result = inNetworkReader.ReadInt16();
            short result2 = inNetworkReader.ReadInt16();

            Assert.That(result, Is.EqualTo(someNumber));
            Assert.That(result2, Is.EqualTo(someNumber2));
        }

        [Test]
        public void TestLength()
        {
            var inNetworkBuffer = new NetworkBuffer(4);
            Assert.That(inNetworkBuffer.Length, Is.EqualTo(0));
            inNetworkBuffer.WriteByte(1);
            Assert.That(inNetworkBuffer.Length, Is.EqualTo(1));
            inNetworkBuffer.WriteByte(2);
            Assert.That(inNetworkBuffer.Length, Is.EqualTo(2));
            inNetworkBuffer.WriteByte(3);
            Assert.That(inNetworkBuffer.Length, Is.EqualTo(3));
            inNetworkBuffer.WriteByte(4);
            Assert.That(inNetworkBuffer.Length, Is.EqualTo(4));
        }

        [Test]
        public void TestCapacityGrowth()
        {
            var inNetworkBuffer = new NetworkBuffer(4);
            Assert.That(inNetworkBuffer.Capacity, Is.EqualTo(4));

            inNetworkBuffer.WriteByte(1);
            inNetworkBuffer.WriteByte(2);
            inNetworkBuffer.WriteByte(3);
            inNetworkBuffer.WriteByte(4);
            inNetworkBuffer.WriteByte(5);

            // buffer should grow and the reported length
            // should not waste any space
            // note MemoryStream makes a distinction between Length and Capacity
            Assert.That(inNetworkBuffer.Length, Is.EqualTo(5));
            Assert.That(inNetworkBuffer.Capacity, Is.GreaterThanOrEqualTo(5));
        }

        [Test]
        public void TestWriteSingle()
        {
            float somenumber = 0.1f;
            var outNetworkBuffer = new NetworkBuffer();
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);

            outNetworkWriter.WriteSingle(somenumber);
            var outBuffer = outNetworkBuffer.GetBuffer();

            var inNetworkBuffer = new NetworkBuffer(outBuffer);
            var inNetworkReader = new NetworkReader(inNetworkBuffer);

            Assert.That(inNetworkReader.ReadSingle(), Is.EqualTo(somenumber));
        }

        [Test]
        public void TestWriteDouble()
        {
            double somenumber = Math.PI;
            var outNetworkBuffer = new NetworkBuffer();
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);

            outNetworkWriter.WriteDouble(somenumber);
            var outBuffer = outNetworkBuffer.GetBuffer();

            var inNetworkBuffer = new NetworkBuffer(outBuffer);
            var inNetworkReader = new NetworkReader(inNetworkBuffer);

            Assert.That(inNetworkReader.ReadDouble(), Is.EqualTo(somenumber));
        }

        [Test]
        public void TestWritePackedSingle()
        {
            float somenumber = (float)Math.PI;
            var outNetworkBuffer = new NetworkBuffer();
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);

            outNetworkWriter.WriteSinglePacked(somenumber);
            var buffer = outNetworkBuffer.GetBuffer();

            var inNetworkBuffer = new NetworkBuffer(buffer);
            var inNetworkReader = new NetworkReader(inNetworkBuffer);

            Assert.That(inNetworkReader.ReadSinglePacked(), Is.EqualTo(somenumber));
        }

        [Test]
        public void TestWritePackedDouble()
        {
            double somenumber = Math.PI;
            var outNetworkBuffer = new NetworkBuffer();
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);

            outNetworkWriter.WriteDoublePacked(somenumber);
            var outBuffer = outNetworkBuffer.GetBuffer();

            var inNetworkBuffer = new NetworkBuffer(outBuffer);
            var inNetworkReader = new NetworkReader(inNetworkBuffer);

            Assert.That(inNetworkReader.ReadDoublePacked(), Is.EqualTo(somenumber));
        }

        [Test]
        public void TestWriteMisaligned()
        {
            var outNetworkBuffer = new NetworkBuffer();
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);
            outNetworkWriter.WriteBit(true);
            outNetworkWriter.WriteBit(false);
            // now the stream is misalligned,  lets write some bytes
            outNetworkWriter.WriteByte(244);
            outNetworkWriter.WriteByte(123);
            outNetworkWriter.WriteInt16(-5457);
            outNetworkWriter.WriteUInt64(4773753249);
            outNetworkWriter.WriteUInt64Packed(5435285812313212);
            outNetworkWriter.WriteInt64Packed(-5435285812313212);
            outNetworkWriter.WriteBit(true);
            outNetworkWriter.WriteByte(1);
            outNetworkWriter.WriteByte(0);

            var outBuffer = outNetworkBuffer.GetBuffer();

            var inNetworkBuffer = new NetworkBuffer(outBuffer);
            var inNetworkReader = new NetworkReader(inNetworkBuffer);

            Assert.That(inNetworkReader.ReadBit(), Is.True);
            Assert.That(inNetworkReader.ReadBit(), Is.False);
            Assert.That(inNetworkReader.ReadByte(), Is.EqualTo(244));
            Assert.That(inNetworkReader.ReadByte(), Is.EqualTo(123));
            Assert.That(inNetworkReader.ReadInt16(), Is.EqualTo(-5457));
            Assert.That(inNetworkReader.ReadUInt64(), Is.EqualTo(4773753249));
            Assert.That(inNetworkReader.ReadUInt64Packed(), Is.EqualTo(5435285812313212));
            Assert.That(inNetworkReader.ReadInt64Packed(), Is.EqualTo(-5435285812313212));
            Assert.That(inNetworkReader.ReadBit(), Is.True);
            Assert.That(inNetworkReader.ReadByte(), Is.EqualTo(1));
            Assert.That(inNetworkReader.ReadByte(), Is.EqualTo(0));
        }

        [Test]
        public void TestBits()
        {
            ulong somevalue = 0b1100101010011;

            var outNetworkBuffer = new NetworkBuffer();
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);
            outNetworkWriter.WriteBits(somevalue, 5);

            var outBuffer = outNetworkBuffer.GetBuffer();

            var inNetworkBuffer = new NetworkBuffer(outBuffer);
            var inNetworkReader = new NetworkReader(inNetworkBuffer);

            Assert.That(inNetworkReader.ReadBits(5), Is.EqualTo(0b10011));
        }

        [Test]
        public void TestNibble()
        {
            byte somevalue = 0b1010011;

            var outNetworkBuffer = new NetworkBuffer();
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);
            outNetworkWriter.WriteNibble(somevalue);

            var outBuffer = outNetworkBuffer.GetBuffer();

            var inNetworkBuffer = new NetworkBuffer(outBuffer);
            var inNetworkReader = new NetworkReader(inNetworkBuffer);

            Assert.That(inNetworkReader.ReadNibble(), Is.EqualTo(0b0011));
        }

        [Test]
        public void TestReadWriteMissaligned()
        {
            var outNetworkBuffer = new NetworkBuffer();
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);
            outNetworkWriter.WriteBit(true);
            var writeBuffer = new byte[16]
            {
                0,
                5,
                2,
                54,
                192,
                60,
                214,
                65,
                95,
                2,
                43,
                62,
                252,
                190,
                45,
                2
            };
            outNetworkBuffer.Write(writeBuffer);

            var inNetworkBuffer = new NetworkBuffer(outNetworkBuffer.GetBuffer());
            Assert.That(inNetworkBuffer.ReadBit(), Is.True);
            var readBuffer = new byte[16];
            inNetworkBuffer.Read(readBuffer, 0, 16);
            Assert.That(readBuffer, Is.EquivalentTo(writeBuffer));
        }

        [Test]
        public void TestArrays()
        {
            var outByteArray = new byte[]
            {
                1,
                2,
                13,
                37,
                69
            };
            var outIntArray = new int[]
            {
                1337,
                69420,
                12345,
                0,
                0,
                5
            };
            var outDoubleArray = new double[]
            {
                0.02,
                0.06,
                1E40,
                256.0
            };

            var outNetworkBuffer = new NetworkBuffer();
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);
            outNetworkWriter.WriteByteArray(outByteArray);
            outNetworkWriter.WriteIntArray(outIntArray);
            outNetworkWriter.WriteDoubleArray(outDoubleArray);

            var inNetworkBuffer = new NetworkBuffer(outNetworkBuffer.GetBuffer());
            var inNetworkReader = new NetworkReader(inNetworkBuffer);
            var inByteArray = inNetworkReader.ReadByteArray();
            var inIntArray = inNetworkReader.ReadIntArray();
            var inDoubleArray = inNetworkReader.ReadDoubleArray();

            Assert.That(outByteArray, Is.EqualTo(inByteArray));
            Assert.That(outIntArray, Is.EqualTo(inIntArray));
            Assert.That(outDoubleArray, Is.EqualTo(inDoubleArray));
        }

        [Test]
        public void TestArraysPacked()
        {
            var outShortArray = new short[]
            {
                1,
                2,
                13,
                37,
                69
            };
            var outIntArray = new int[]
            {
                1337,
                69420,
                12345,
                0,
                0,
                5
            };
            var outDoubleArray = new double[]
            {
                0.02,
                0.06,
                1E40,
                256.0
            };

            var outNetworkBuffer = new NetworkBuffer();
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);
            outNetworkWriter.WriteShortArrayPacked(outShortArray);
            outNetworkWriter.WriteIntArrayPacked(outIntArray);
            outNetworkWriter.WriteDoubleArrayPacked(outDoubleArray);

            var inNetworkBuffer = new NetworkBuffer(outNetworkBuffer.GetBuffer());
            var inNetworkReader = new NetworkReader(inNetworkBuffer);
            var inShortArray = inNetworkReader.ReadShortArrayPacked();
            var inIntArray = inNetworkReader.ReadIntArrayPacked();
            var inDoubleArray = inNetworkReader.ReadDoubleArrayPacked();

            Assert.That(outShortArray, Is.EqualTo(inShortArray));
            Assert.That(outIntArray, Is.EqualTo(inIntArray));
            Assert.That(outDoubleArray, Is.EqualTo(inDoubleArray));
        }

        [Test]
        public void TestArraysDiff()
        {
            // Values changed test
            var byteOutDiffData = new byte[]
            {
                1,
                2,
                13,
                29,
                44,
                15
            };
            var byteOutData = new byte[]
            {
                1,
                2,
                13,
                37,
                69
            };

            // No change test
            var intOutDiffData = new int[]
            {
                1337,
                69420,
                12345,
                0,
                0,
                5
            };
            var intOutData = new int[]
            {
                1337,
                69420,
                12345,
                0,
                0,
                5
            };

            // Array resize test
            var doubleOutDiffData = new double[]
            {
                0.2,
                6,
                1E39
            };
            var doubleOutData = new double[]
            {
                0.02,
                0.06,
                1E40,
                256.0
            };

            // Serialize
            var outNetworkBuffer = new NetworkBuffer();
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);
            outNetworkWriter.WriteByteArrayDiff(byteOutData, byteOutDiffData);
            outNetworkWriter.WriteIntArrayDiff(intOutData, intOutDiffData);
            outNetworkWriter.WriteDoubleArrayDiff(doubleOutData, doubleOutDiffData);

            // Deserialize
            var inNetworkBuffer = new NetworkBuffer(outNetworkBuffer.GetBuffer());
            var inNetworkReader = new NetworkReader(inNetworkBuffer);
            var byteInData = inNetworkReader.ReadByteArrayDiff(byteOutDiffData);
            var intInData = inNetworkReader.ReadIntArrayDiff(intOutDiffData);
            var doubleInData = inNetworkReader.ReadDoubleArrayDiff(doubleOutDiffData);

            // Compare
            Assert.That(byteInData, Is.EqualTo(byteOutData));
            Assert.That(intInData, Is.EqualTo(intOutData));
            Assert.That(doubleInData, Is.EqualTo(doubleOutData));
        }

        [Test]
        public void TestArraysPackedDiff()
        {
            // Values changed test
            var longOutDiffData = new long[]
            {
                1,
                2,
                13,
                29,
                44,
                15
            };
            var longOutData = new long[]
            {
                1,
                2,
                13,
                37,
                69
            };

            // No change test
            var intOutDiffData = new int[]
            {
                1337,
                69420,
                12345,
                0,
                0,
                5
            };
            var intOutData = new int[]
            {
                1337,
                69420,
                12345,
                0,
                0,
                5
            };

            // Array resize test
            var doubleOutDiffData = new double[]
            {
                0.2,
                6,
                1E39
            };
            var doubleOutData = new double[]
            {
                0.02,
                0.06,
                1E40,
                256.0
            };

            // Serialize
            var outNetworkBuffer = new NetworkBuffer();
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);
            outNetworkWriter.WriteLongArrayPackedDiff(longOutData, longOutDiffData);
            outNetworkWriter.WriteIntArrayPackedDiff(intOutData, intOutDiffData);
            outNetworkWriter.WriteDoubleArrayPackedDiff(doubleOutData, doubleOutDiffData);

            // Deserialize
            var inNetworkBuffer = new NetworkBuffer(outNetworkBuffer.GetBuffer());
            var inNetworkReader = new NetworkReader(inNetworkBuffer);
            var longInData = inNetworkReader.ReadLongArrayPackedDiff(longOutDiffData);
            var intInData = inNetworkReader.ReadIntArrayPackedDiff(intOutDiffData);
            var doubleInData = inNetworkReader.ReadDoubleArrayPackedDiff(doubleOutDiffData);

            // Compare
            Assert.That(longInData, Is.EqualTo(longOutData));
            Assert.That(intInData, Is.EqualTo(intOutData));
            Assert.That(doubleInData, Is.EqualTo(doubleOutData));
        }

        [Test]
        public void TestString()
        {
            var testString = "Hello, World";
            var outNetworkBuffer = new NetworkBuffer();
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);
            outNetworkWriter.WriteString(testString);
            outNetworkWriter.WriteString(testString, true);

            var inNetworkBuffer = new NetworkBuffer(outNetworkBuffer.GetBuffer());
            var inNetworkReader = new NetworkReader(inNetworkBuffer);
            StringBuilder readBuilder = inNetworkReader.ReadString();
            StringBuilder readBuilderSingle = inNetworkReader.ReadString(true);

            Assert.That(readBuilder.ToString(), Is.EqualTo(testString));
            Assert.That(readBuilderSingle.ToString(), Is.EqualTo(testString));
        }

        [Test]
        public void TestStringPacked()
        {
            var testString = "Hello, World";
            var outNetworkBuffer = new NetworkBuffer();
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);
            outNetworkWriter.WriteStringPacked(testString);

            var inNetworkBuffer = new NetworkBuffer(outNetworkBuffer.GetBuffer());
            var inNetworkReader = new NetworkReader(inNetworkBuffer);
            var readString = inNetworkReader.ReadStringPacked();

            Assert.That(readString, Is.EqualTo(testString));
        }

        [Test]
        public void TestStringDiff()
        {
            var testString = "Hello, World"; // The simulated "new" value of testString
            var originalString = "Heyo,  World"; // This is what testString supposedly changed *from*
            var outNetworkBuffer = new NetworkBuffer();
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);
            outNetworkWriter.WriteStringDiff(testString, originalString);
            outNetworkWriter.WriteStringDiff(testString, originalString, true);

            var inNetworkBuffer = new NetworkBuffer(outNetworkBuffer.GetBuffer());
            var inNetworkReader = new NetworkReader(inNetworkBuffer);
            // Read regular diff
            StringBuilder readBuilder = inNetworkReader.ReadStringDiff(originalString);

            // Read diff directly to StringBuilder
            inNetworkBuffer.BitPosition = 0;
            StringBuilder stringCompare = new StringBuilder(originalString);
            inNetworkReader.ReadStringDiff(stringCompare);

            // Read single-byte diff
            StringBuilder byteBuilder = inNetworkReader.ReadStringDiff(originalString, true);

            Assert.That(readBuilder.ToString(), Is.EqualTo(testString));
            Assert.That(stringCompare.ToString(), Is.EqualTo(testString));
            Assert.That(byteBuilder.ToString(), Is.EqualTo(testString));
        }

        [Test]
        public void TestStringPackedDiff()
        {
            var testString = "Hello, World"; // The simulated "new" value of testString
            var originalString = "Heyo,  World"; // This is what testString supposedly changed *from*
            var outNetworkBuffer = new NetworkBuffer();
            var outNetworkWriter = new NetworkWriter(outNetworkBuffer);
            outNetworkWriter.WriteStringPackedDiff(testString, originalString);

            var inNetworkBuffer = new NetworkBuffer(outNetworkBuffer.GetBuffer());
            var inNetworkReader = new NetworkReader(inNetworkBuffer);
            // Read regular diff
            StringBuilder readBuilder = inNetworkReader.ReadStringPackedDiff(originalString);

            // Read diff directly to StringBuilder
            inNetworkBuffer.BitPosition = 0;
            StringBuilder stringCompare = new StringBuilder(originalString);
            inNetworkReader.ReadStringPackedDiff(stringCompare);

            Assert.That(readBuilder.ToString(), Is.EqualTo(testString));
            Assert.That(stringCompare.ToString(), Is.EqualTo(testString));
        }
    }
}
