using System;
using NUnit.Framework;
using Unity.Collections;
using MLAPI.Serialization;

namespace MLAPI.Serialization.Tests
{
    public class DataStreamTests
    {
        [Test]
        public void CreateStreamWithPartOfSourceByteArray()
        {
            byte[] byteArray =
            {
                (byte) 's', (byte) 'o', (byte) 'm', (byte) 'e',
                (byte) ' ', (byte) 'd', (byte) 'a', (byte) 't', (byte) 'a'
            };

            DataStreamWriter dataStream;
            dataStream = new DataStreamWriter(4, Allocator.Temp);
            dataStream.WriteBytes(new NativeArray<byte>(byteArray, Allocator.Temp).GetSubArray(0, 4));
            Assert.AreEqual(dataStream.Length, 4);
            var reader = new DataStreamReader(dataStream.AsNativeArray());
            for (int i = 0; i < dataStream.Length; ++i)
            {
                Assert.AreEqual(byteArray[i], reader.ReadByte());
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => { reader.ReadByte(); });
        }

        [Test]
        public void CreateStreamWithSourceByteArray()
        {
            byte[] byteArray = new byte[100];
            byteArray[0] = (byte) 'a';
            byteArray[1] = (byte) 'b';
            byteArray[2] = (byte) 'c';

            DataStreamWriter dataStream;
            dataStream = new DataStreamWriter(byteArray.Length, Allocator.Temp);
            dataStream.WriteBytes(new NativeArray<byte>(byteArray, Allocator.Temp));
            var reader = new DataStreamReader(dataStream.AsNativeArray());
            for (int i = 0; i < byteArray.Length; ++i)
            {
                Assert.AreEqual(byteArray[i], reader.ReadByte());
            }
        }

        [Test]
        public void ReadIntoExistingByteArray()
        {
            var byteArray = new NativeArray<byte>(100, Allocator.Temp);

            DataStreamWriter dataStream;
            dataStream = new DataStreamWriter(3, Allocator.Temp);
            {
                dataStream.WriteByte((byte) 'a');
                dataStream.WriteByte((byte) 'b');
                dataStream.WriteByte((byte) 'c');
                var reader = new DataStreamReader(dataStream.AsNativeArray());
                reader.ReadBytes(byteArray.GetSubArray(0, dataStream.Length));
                reader = new DataStreamReader(dataStream.AsNativeArray());
                for (int i = 0; i < reader.Length; ++i)
                {
                    Assert.AreEqual(byteArray[i], reader.ReadByte());
                }
            }
        }

        [Test]
        public void ReadingDataFromStreamWithSliceOffset()
        {
            var dataStream = new DataStreamWriter(100, Allocator.Temp);
            dataStream.WriteByte((byte) 'a');
            dataStream.WriteByte((byte) 'b');
            dataStream.WriteByte((byte) 'c');
            dataStream.WriteByte((byte) 'd');
            dataStream.WriteByte((byte) 'e');
            dataStream.WriteByte((byte) 'f');
            var reader = new DataStreamReader(dataStream.AsNativeArray().GetSubArray(3, 3));
            Assert.AreEqual('d', reader.ReadByte());
            Assert.AreEqual('e', reader.ReadByte());
            Assert.AreEqual('f', reader.ReadByte());
        }

        [Test]
        public void WriteOutOfBounds()
        {
            var dataStream = new DataStreamWriter(9, Allocator.Temp);
            Assert.IsTrue(dataStream.WriteInt(42));
            Assert.AreEqual(4, dataStream.Length);
            Assert.IsTrue(dataStream.WriteInt(42));
            Assert.AreEqual(8, dataStream.Length);
            Assert.IsFalse(dataStream.HasFailedWrites);
            Assert.IsFalse(dataStream.WriteInt(42));
            Assert.AreEqual(8, dataStream.Length);
            Assert.IsTrue(dataStream.HasFailedWrites);

            Assert.IsFalse(dataStream.WriteShort(42));
            Assert.AreEqual(8, dataStream.Length);
            Assert.IsTrue(dataStream.HasFailedWrites);

            Assert.IsTrue(dataStream.WriteByte(42));
            Assert.AreEqual(9, dataStream.Length);
            Assert.IsTrue(dataStream.HasFailedWrites);

            Assert.IsFalse(dataStream.WriteByte(42));
            Assert.AreEqual(9, dataStream.Length);
            Assert.IsTrue(dataStream.HasFailedWrites);
        }

        [Test]
        public void ReadWriteFixedString32()
        {
            var dataStream = new DataStreamWriter(300 * 4, Allocator.Temp);

            var src = new FixedString32("This is a string");
            dataStream.WriteFixedString32(src);

            //Assert.AreEqual(src.LengthInBytes+2, dataStream.Length);

            var reader = new DataStreamReader(dataStream.AsNativeArray());
            var dst = reader.ReadFixedString32();
            Assert.AreEqual(src, dst);
        }

        [Test]
        public void ReadWriteFixedString64()
        {
            var dataStream = new DataStreamWriter(300 * 4, Allocator.Temp);

            var src = new FixedString64("This is a string");
            dataStream.WriteFixedString64(src);

            //Assert.AreEqual(src.LengthInBytes+2, dataStream.Length);

            var reader = new DataStreamReader(dataStream.AsNativeArray());
            var dst = reader.ReadFixedString64();
            Assert.AreEqual(src, dst);
        }
     
        [Test]
        public void ReadWriteFixedString128()
        {
            var dataStream = new DataStreamWriter(300 * 4, Allocator.Temp);

            var src = new FixedString128("This is a string");
            dataStream.WriteFixedString128(src);

            //Assert.AreEqual(src.LengthInBytes+2, dataStream.Length);

            var reader = new DataStreamReader(dataStream.AsNativeArray());
            var dst = reader.ReadFixedString128();
            Assert.AreEqual(src, dst);
        }
   
        [Test]
        public void ReadWriteFixedString512()
        {
            var dataStream = new DataStreamWriter(300 * 4, Allocator.Temp);

            var src = new FixedString512("This is a string");
            dataStream.WriteFixedString512(src);

            //Assert.AreEqual(src.LengthInBytes+2, dataStream.Length);

            var reader = new DataStreamReader(dataStream.AsNativeArray());
            var dst = reader.ReadFixedString512();
            Assert.AreEqual(src, dst);
        }

        [Test]
        public void ReadWriteFixedString4096()
        {
            var dataStream = new DataStreamWriter(300 * 4, Allocator.Temp);

            var src = new FixedString4096("This is a string");
            dataStream.WriteFixedString4096(src);

            //Assert.AreEqual(src.LengthInBytes+2, dataStream.Length);

            var reader = new DataStreamReader(dataStream.AsNativeArray());
            var dst = reader.ReadFixedString4096();
            Assert.AreEqual(src, dst);
        }
        
    }
}
