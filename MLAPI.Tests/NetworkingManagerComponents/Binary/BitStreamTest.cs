using System;
namespace MLAPI.Tests.NetworkingManagerComponents.Binary
{
    using MLAPI.NetworkingManagerComponents.Binary;
    using NUnit.Framework;
    using static MLAPI.NetworkingManagerComponents.Binary.BitStream;

    [TestFixture]
    public class BitStreamTest
    {

        [Test]
        public void TestEmptyStream()
        {
            BitStream bitStream = new BitStream(new byte[100]);
            // ideally an empty stream should take no space
            Assert.That(bitStream.Length, Is.EqualTo(100));
        }

        [Test]
        public void TestBool()
        {
            BitStream bitStream = new BitStream(new byte[100]);
            bitStream.WriteBit(true);

            // we only wrote 1 bit,  so the size should be as small as possible
            // which is 1 byte,   regardless of how big the buffer is
            Assert.That(bitStream.Length, Is.EqualTo(100));
        }

        [Test]
        public void TestGrow()
        {
            // stream should not grow when given a buffer
            BitStream bitStream = new BitStream(new byte[0]);
            Assert.That(
                () => { bitStream.WriteInt64(long.MaxValue); }, 
                Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void TestInOutBool()
        {
            byte[] buffer = new byte[100];

            BitStream outStream = new BitStream(buffer);
            outStream.WriteBit(true);
            outStream.WriteBit(false);
            outStream.WriteBit(true);


            // the bit should now be stored in the buffer,  lets see if it comes out

            BitStream inStream = new BitStream(buffer);

            Assert.That(inStream.ReadBit(), Is.True);
            Assert.That(inStream.ReadBit(), Is.False);
            Assert.That(inStream.ReadBit(), Is.True);
        }


        [Test]
        public void TestInOutPacked64Bit()
        {
            byte[] buffer = new byte[100];
            
            long someNumber = 1469598103934656037;


            BitStream outStream = new BitStream(buffer);
            outStream.WriteInt64Packed(someNumber);


            // the bit should now be stored in the buffer,  lets see if it comes out

            BitStream inStream = new BitStream(buffer);
            long result = inStream.ReadInt64Packed();

            Assert.That(result, Is.EqualTo(someNumber));
        }

        [Test]
        public void TestInOutBytes()
        {
            byte[] buffer = new byte[100];

            byte someNumber = 0xff;


            BitStream outStream = new BitStream(buffer);
            outStream.WriteByte(someNumber);


            // the bit should now be stored in the buffer,  lets see if it comes out

            BitStream inStream = new BitStream(buffer);
            Assert.That(inStream.ReadByte(), Is.EqualTo(someNumber));

            // wtf this is standard behaviour
            //Assert.Fail("Read byte should return byte,  but it returns int");
        }

        [Test]
        public void TestInOutInt16()
        {
            byte[] buffer = new byte[100];

            short someNumber = 23223;


            BitStream outStream = new BitStream(buffer);
            outStream.WriteInt16(someNumber);


            // the bit should now be stored in the buffer,  lets see if it comes out

            BitStream inStream = new BitStream(buffer);
            short result = inStream.ReadInt16();

            Assert.That(result, Is.EqualTo(someNumber));
        }

        [Test]
        public void TestInOutInt32()
        {
            byte[] buffer = new byte[100];

            int someNumber = 23234223;


            BitStream outStream = new BitStream(buffer);
            outStream.WriteInt32(someNumber);


            // the bit should now be stored in the buffer,  lets see if it comes out

            BitStream inStream = new BitStream(buffer);
            int result = inStream.ReadInt32();

            Assert.That(result, Is.EqualTo(someNumber));
        }

        [Test]
        public void TestInOutMultiple()
        {
            byte[] buffer = new byte[100];

            short someNumber = -12423;
            short someNumber2 = 9322;

            BitStream outStream = new BitStream(buffer);
            outStream.WriteInt16(someNumber);
            outStream.WriteInt16(someNumber2);


            // the bit should now be stored in the buffer,  lets see if it comes out

            BitStream inStream = new BitStream(buffer);
            short result = inStream.ReadInt16();
            short result2 = inStream.ReadInt16();

            Assert.That(result, Is.EqualTo(someNumber));
            Assert.That(result2, Is.EqualTo(someNumber2));
        }

        [Test]
        public void TestLength()
        {
            BitStream inStream = new BitStream(4);
            Assert.That(inStream.Length, Is.EqualTo(0));
            inStream.WriteByte(1);
            Assert.That(inStream.Length, Is.EqualTo(1));
            inStream.WriteByte(2);
            Assert.That(inStream.Length, Is.EqualTo(2));
            inStream.WriteByte(3);
            Assert.That(inStream.Length, Is.EqualTo(3));
            inStream.WriteByte(4);
            Assert.That(inStream.Length, Is.EqualTo(4));
        }

        [Test]
        public void TestCapacityGrowth()
        {
            BitStream inStream = new BitStream(4);
            Assert.That(inStream.Capacity, Is.EqualTo(4));

            inStream.WriteByte(1);
            inStream.WriteByte(2);
            inStream.WriteByte(3);
            inStream.WriteByte(4);
            inStream.WriteByte(5);

            // buffer should grow and the reported length
            // should not waste any space
            // note MemoryStream makes a distinction between Length and Capacity
            Assert.That(inStream.Length, Is.EqualTo(5));
            Assert.That(inStream.Capacity, Is.GreaterThanOrEqualTo(5));
        }
    }
}
