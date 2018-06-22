using System;
namespace MLAPI.Tests.NetworkingManagerComponents.Binary
{
    using MLAPI.NetworkingManagerComponents.Binary;
    using NUnit.Framework;


    [TestFixture]
    public class BitStreamTest
    {

        [Test]
        public void TestEmptyStream()
        {
            
            BitStream bitStream = new BitStream(new byte[100]);
            // ideally an empty stream should take no space
            Assert.That(bitStream.Length, Is.EqualTo(0));
        }

        [Test]
        public void TestBool()
        {

            BitStream bitStream = new BitStream(new byte[100]);
            bitStream.WriteBit(true);

            // this is failing,  I just wrote something,  how is it possible
            // that the length is still 0?
            Assert.That(bitStream.Length, Is.EqualTo(1));
        }

        [Test]
        public void TestGrow()
        {
            // stream should grow to accomodate input
            BitStream bitStream = new BitStream(new byte[0]);
            bitStream.WriteInt64(long.MaxValue);

        }

        [Test]
        public void TestGrow2()
        {
            // stream should grow to accomodate input
            BitStream bitStream = new BitStream(new byte[1]);
            bitStream.WriteInt64(long.MaxValue);

        }

        [Test]
        public void TestInOutBool()
        {
            byte[] buffer = new byte[100];

            BitStream outStream = new BitStream(buffer);
            outStream.WriteBit(true);
            outStream.WriteBit(false);
            outStream.WriteBit(true);
            outStream.Flush();


            // the bit should now be stored in the buffer,  lets see if it comes out

            BitStream inStream = new BitStream(buffer);

            // Yeet
            Assert.That(inStream.ReadBit() && !inStream.ReadBit() && inStream.ReadBit(), "Incorrect ReadBit result");
        }


        [Test]
        public void TestInOutPacked64Bit()
        {
            byte[] buffer = new byte[100];
            
            long someNumber = 1469598103934656037;


            BitStream outStream = new BitStream(buffer);
            outStream.WriteInt64Packed(someNumber);
            outStream.Flush();


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
            outStream.Flush();


            // the bit should now be stored in the buffer,  lets see if it comes out

            BitStream inStream = new BitStream(buffer);
            Assert.That(inStream.ReadByte() == someNumber, "Read/Write mismatch in WriteByte() and/or ReadByte()");

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
            outStream.Flush();


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
            outStream.Flush();


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
            outStream.Flush();


            // the bit should now be stored in the buffer,  lets see if it comes out

            BitStream inStream = new BitStream(buffer);
            short result = inStream.ReadInt16();
            short result2 = inStream.ReadInt16();

            Assert.That(result, Is.EqualTo(someNumber));
            Assert.That(result2, Is.EqualTo(someNumber2));
        }
    }
}
