using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Multiplayer.Netcode;

namespace Unity.Netcode.EditorTests
{
    public class BitWriterTests
    {
        [Test]
        public unsafe void TestWritingOneBit()
        {
            FastBufferWriter writer = new FastBufferWriter(4, Allocator.Temp);
            using (writer)
            {
                int* asInt = (int*) writer.GetUnsafePtr();
                
                Assert.AreEqual(0, *asInt);

                Assert.IsTrue(writer.VerifyCanWrite(3));
                using (var bitWriter = writer.EnterBitwiseContext())
                {
                    bitWriter.WriteBit(true);
                    Assert.AreEqual(0b1, *asInt);
                    
                    bitWriter.WriteBit(true);
                    Assert.AreEqual(0b11, *asInt);
                    
                    bitWriter.WriteBit(false);
                    bitWriter.WriteBit(true);
                    Assert.AreEqual(0b1011, *asInt);
                    
                    bitWriter.WriteBit(false);
                    bitWriter.WriteBit(false);
                    bitWriter.WriteBit(false);
                    bitWriter.WriteBit(true);
                    Assert.AreEqual(0b10001011, *asInt);
                    
                    bitWriter.WriteBit(false);
                    bitWriter.WriteBit(true);
                    bitWriter.WriteBit(false);
                    bitWriter.WriteBit(true);
                    Assert.AreEqual(0b1010_10001011, *asInt);
                }
                
                Assert.AreEqual(2, writer.Position);
                Assert.AreEqual(0b1010_10001011, *asInt);
                
                writer.WriteByte(0b11111111);
                Assert.AreEqual(0b11111111_00001010_10001011, *asInt);
            }
        }
        
        [Test]
        public unsafe void TestWritingMultipleBits()
        {
            FastBufferWriter writer = new FastBufferWriter(4, Allocator.Temp);
            using (writer)
            {
                int* asInt = (int*) writer.GetUnsafePtr();
                
                Assert.AreEqual(0, *asInt);

                Assert.IsTrue(writer.VerifyCanWrite(3));
                using (var bitWriter = writer.EnterBitwiseContext())
                {
                    bitWriter.WriteBits(0b11111111, 1);
                    Assert.AreEqual(0b1, *asInt);
                    
                    bitWriter.WriteBits(0b11111111, 1);
                    Assert.AreEqual(0b11, *asInt);
                    
                    bitWriter.WriteBits(0b11111110, 2);
                    Assert.AreEqual(0b1011, *asInt);
                    
                    bitWriter.WriteBits(0b11111000, 4);
                    Assert.AreEqual(0b10001011, *asInt);
                    
                    bitWriter.WriteBits(0b11111010, 4);
                    Assert.AreEqual(0b1010_10001011, *asInt);
                }
                
                Assert.AreEqual(2, writer.Position);
                Assert.AreEqual(0b1010_10001011, *asInt);
                
                writer.WriteByte(0b11111111);
                Assert.AreEqual(0b11111111_00001010_10001011, *asInt);
            }
        }
        
        [Test]
        public unsafe void TestWritingMultipleBitsFromLongs()
        {
            FastBufferWriter writer = new FastBufferWriter(4, Allocator.Temp);
            using (writer)
            {
                int* asInt = (int*) writer.GetUnsafePtr();
                
                Assert.AreEqual(0, *asInt);

                Assert.IsTrue(writer.VerifyCanWrite(3));
                using (var bitWriter = writer.EnterBitwiseContext())
                {
                    bitWriter.WriteBits(0b11111111UL, 1);
                    Assert.AreEqual(0b1, *asInt);
                    
                    bitWriter.WriteBits(0b11111111UL, 1);
                    Assert.AreEqual(0b11, *asInt);
                    
                    bitWriter.WriteBits(0b11111110UL, 2);
                    Assert.AreEqual(0b1011, *asInt);
                    
                    bitWriter.WriteBits(0b11111000UL, 4);
                    Assert.AreEqual(0b10001011, *asInt);
                    
                    bitWriter.WriteBits(0b11111010UL, 4);
                    Assert.AreEqual(0b1010_10001011, *asInt);
                }
                
                Assert.AreEqual(2, writer.Position);
                Assert.AreEqual(0b1010_10001011, *asInt);
                
                writer.WriteByte(0b11111111);
                Assert.AreEqual(0b11111111_00001010_10001011, *asInt);
            }
        }
        
        [Test]
        public unsafe void TestWritingBitsThrowsIfVerifyCanWriteNotCalled()
        {
            FastBufferWriter writer = new FastBufferWriter(4, Allocator.Temp);
            using (writer)
            {
                int* asInt = (int*) writer.GetUnsafePtr();
                
                Assert.AreEqual(0, *asInt);

                Assert.Throws<OverflowException>(() =>
                {
                    using (var bitWriter = writer.EnterBitwiseContext())
                    {
                        bitWriter.WriteBit(true);
                    }
                });
                
                Assert.Throws<OverflowException>(() =>
                {
                    using (var bitWriter = writer.EnterBitwiseContext())
                    {
                        bitWriter.WriteBit(false);
                    }
                });
            
                Assert.Throws<OverflowException>(() =>
                {
                    using (var bitWriter = writer.EnterBitwiseContext())
                    {
                        bitWriter.WriteBits(0b11111111, 1);
                    }
                });
            
                Assert.Throws<OverflowException>(() =>
                {
                    using (var bitWriter = writer.EnterBitwiseContext())
                    {
                        bitWriter.WriteBits(0b11111111UL, 1);
                    }
                });
                
                Assert.AreEqual(0, writer.Position);
                Assert.AreEqual(0, *asInt);
                
                writer.WriteByteSafe(0b11111111);
                Assert.AreEqual(0b11111111, *asInt);
            }
        }
    }
}